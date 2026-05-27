# MVVM Strategy — Desktop Client Shell

- **Status:** final
- **Date:** 2026-05-27
- **Authors:** Sub-team 6 (Desktop GUI & Client Shell)
- **Backlog:** ARCH-1, ARCH-9
- **Supersedes:** ADR-009 draft (decision rationale folded in)
- **Related:** [ADR-0002 — Client–server transport](../D2-Architecture/client-server-transport.md)

---

## 1. Why MVVM: The Problem We Are Solving

### 1.1 CanvassDesktop is a God-Object

`Assets/Scripts/UI/CanvassDesktop.cs` is a 1,899-line `MonoBehaviour` that mixes at least eight distinct concerns in a single class:

| Concern | Evidence in file |
|---|---|
| FITS file I/O and HDU parsing | `_browseImageFile`, `UpdateHeaderFromFits`, `FitsReader.*` calls |
| Scene-hierarchy wiring | 20+ `transform.Find("A/B/C").GetComponent<T>()` chains in `Start()` |
| Rendering state sync | `Update()` pushes threshold/colormap values from renderer into sliders every frame |
| Subset bounds validation logic | `checkSubsetBounds`, `updateSubsetZMax`, `IsLoadable` |
| File-dialog orchestration | `BrowseImageFile`, `BrowseMaskFile` — direct `StandaloneFileBrowser` calls |
| Coroutine lifecycle | `_loadCubeCoroutine`, `_showLoadDialogCoroutine` managed inline |
| Singleton hunting | `FindObjectOfType<VolumeInputController>()` etc. in `Start()` |
| Paint-mode state | `inPaintMode`, `PaintSelectionContainer` toggle logic |

### 1.2 Measured CK Violations (Day 2 Baseline)

The Day 2 CK measurement (SK_BNCH Sprint 1) confirms the problem is not just qualitative:

| Metric | CanvassDesktop (measured) | §7.1 threshold | Status |
|---|---|---|---|
| WMC | 63 | ≤ 40 (orchestrator) | **violation** |
| DIT | 1 | ≤ 4 | within |
| NOC | 0 | ≤ 5 | within |
| CBO | 47 | ≤ 25 (orchestrator) | **violation** |
| RFC | 118 | ≤ 50 | **violation** |
| LCOM_HS | 0.955 | ≤ 0.50 | **violation** |

CBO of 47 breaks down as: 23 project types (e.g. `VolumeDataSetRenderer`, `FitsReader`, `DataAnalysis`, `FeatureMapping`), 13 Unity/TMPro UI types, 7 System library types, and 4 `Valve.VR` types. Three fields (`_restFrequency`, `inPaintMode`, `_tabsManager`) are declared but never accessed — dead weight confirmed by the LCOM field-access count of 189 across 63 methods and 67 fields.

### 1.3 Forces That Drive the Decision

Three forces make the status quo unsustainable:

**Testability vs MonoBehaviour lifecycle.** `MonoBehaviour` cannot be instantiated without a live engine, so any logic inside it cannot be unit-tested with NUnit. CanvassDesktop's mocking-difficulty index of 205 (163 scene-graph traversals + 36 P/Invoke calls) makes the ≥ 70% branch/line target (NFR-TST-1) unreachable without decomposition.

**Unity 2021.3 → Unity 6 migration.** The legacy Canvas system must migrate to UI Toolkit. Any UI code not isolated behind an interface must be rewritten in full; mixing view logic and domain logic in one `MonoBehaviour` doubles that migration surface.

**Assignment constraints (non-negotiable).**
- §4.2.3: Domain code must not transitively depend on `UnityEngine` or `SteamVR`.
- §4.2.4: Every public API boundary expressed as an interface with at least one test double.
- §6.6: MVVM split is explicitly prescribed.
- §4.2.2: Zero circular dependencies between top-level components.

### 1.4 Alternatives Considered and Rejected

| Pattern | Why rejected |
|---|---|
| **Status quo (God-canvas `MonoBehaviour`)** | Fails §4.2.3 (domain depends on `UnityEngine` and native P/Invoke) and §4.2.4 (no testable interfaces). Not a valid target. |
| **MVP (Model–View–Presenter)** | Presenter holds a direct reference to the View interface, making View replacement harder. UI Toolkit's binding model is built around observable properties, not presenter calls; MVVM is the natural fit. |
| **MVU / Elm-style unidirectional flow** | Elegant but mismatches UI Toolkit's two-way binding primitives. Adds unfamiliarity risk for a first-year team defending the pattern under interview. Could be revisited for a future sub-system. |
| **Classical MVC** | Controller and View conflate in Unity practice (the `MonoBehaviour` ends up being both). Drawing a clean ACL boundary is harder when the controller still holds scene references. |
| **Reactive MVVM (Rx/UniRx)** | Compelling fit for the Debug tab's log push model, but Rx adds a library dependency and steep learning curve. Deferred: the `ILogStream` event model in WE2 is compatible with an Rx migration later without changing the ViewModel interface. |

---

## 2. How We Are Doing It: Architecture

### 2.1 Three-Assembly MVVM Split

The decision (ADR-0001, 2026-05-19) is to adopt a three-layer split, each in its own C# assembly:

| Assembly | Contents | Unity dependency |
|---|---|---|
| `iDaVIE.Client.View` | `UIDocument` components, USS/UXML, Unity event wiring | Yes — allowed |
| `iDaVIE.Client.ViewModel` | Observable properties, `ICommand` implementations, validation logic | **None** |
| `iDaVIE.Client.Gateway` | `IServiceGateway` adapters, transport, anti-corruption layer | Adapter layer only |

**Dependency direction (CI-enforced via NDepend):**

```
iDaVIE.Client.View  →  iDaVIE.Client.ViewModel  →  iDaVIE.Client.Gateway
```

Reverse-direction references are a CI failure. This enforces §4.2.2 (no cycles) and §4.2.3 (no `UnityEngine` in domain).

### 2.2 Layer Map

```
┌──────────────────────────────────────────────────────────┐
│  View Layer  (Unity 6 UI Toolkit / MonoBehaviour thin)   │
│  FileTabView  ·  DebugTabView  ·  RenderingTabView  etc. │
│  Only: bind to VM properties, forward UI events          │
└────────────────────┬─────────────────────────────────────┘
                     │ data-binding / ICommand
┌────────────────────▼─────────────────────────────────────┐
│  ViewModel Layer  (pure C#, no UnityEngine references)   │
│  FileTabViewModel  ·  DebugTabViewModel  ·  etc.         │
│  Owns: state, validation, commands, INotifyPropertyChanged│
└────────────────────┬─────────────────────────────────────┘
                     │ calls IServiceGateway interfaces
┌────────────────────▼─────────────────────────────────────┐
│  Service Gateway  (interface + adapter)                  │
│  IFitsService  ·  IVolumeService  ·  ILogService         │
│  Translates domain calls → server RPC / native plugin    │
└──────────────────────────────────────────────────────────┘
```

---

## 3. How We Are Doing It: Per-Tab ViewModel Design

### 3.1 FileTabViewModel

**Owns:**
- `ImagePath`, `MaskPath`, `SourcesPath` (observable properties)
- `HduOptions` list + `SelectedHduIndex`
- `SubsetBounds` (XMin/XMax/YMin/YMax/ZMin/ZMax)
- `SubsetEnabled` toggle
- `IsLoadable` computed property (currently inline logic in `IsLoadable()`)
- `LoadCommand`, `BrowseImageCommand`, `BrowseMaskCommand` (`ICommand`)

**Does NOT own:**
- Any `Transform.Find(...)` wiring — stays in the View adapter
- The `StandaloneFileBrowser` call — goes behind `IFileDialogService`
- The `FitsReader` P/Invoke — goes behind `IFitsService`

**Split from current code:**

| Current (CanvassDesktop) | Proposed home |
|---|---|
| `BrowseImageFile()` UI wiring | `FileTabView.OnBrowseClicked()` |
| `_browseImageFile(path)` FITS open + HDU parse | `IFitsService.OpenImageAsync(path)` |
| `IsLoadable()` axis dimension logic | `FileTabViewModel.IsLoadable` (pure C#, testable) |
| `checkSubsetBounds()` clamping | `SubsetBoundsViewModel.Validate()` |
| `updateSubsetZMax()` dropdown sync | `FileTabViewModel.OnZAxisChanged(int)` |
| `UpdateHeaderFromFits()` scroll view write | `FileTabView` bound to `HeaderText` property |

### 3.2 DebugTabViewModel (Observer Pattern)

The debug console becomes a **passive observer of a structured log stream**, not a direct writer.

```
ILogStream  ──publishes──►  DebugTabViewModel  ──binds──►  DebugTabView
                             (subscribes via event/Rx)     (scrolling text area)
```

- `ILogStream` exposes `event Action<LogEntry> OnLogEntry`
- `DebugTabViewModel` subscribes; maintains `ObservableCollection<LogEntry> Entries`
- The View scrolls to bottom when `Entries` changes — no manual scroll logic in ViewModel
- Existing `Debug.Log` calls in CanvassDesktop are replaced by injecting `ILogStream` and calling `ILogStream.Publish(level, message)`

**Split from current code:**

| Current | Proposed home |
|---|---|
| `Debug.Log(...)` scattered across CanvassDesktop | `ILogStream.Publish()` calls |
| Manual scroll/text updates | `DebugTabView` reactive binding |
| No structured log context | `LogEntry { Level, Source, Message, Timestamp }` |

### 3.3 RenderingTabViewModel

**Owns:**
- `MinThreshold`, `MaxThreshold` (float, two-way bindable)
- `ActiveColorMap` (enum)
- `RestFrequencyOptions` list
- Commands: `ApplyThreshold`, `ChangeColorMap`

The current `Update()` loop polls `firstActiveRenderer.ThresholdMin` every frame and pushes it to a slider. In the refactored design:
- `IVolumeService` raises a `ThresholdChanged` event
- `RenderingTabViewModel` subscribes and updates its own properties
- `RenderingTabView` binds sliders to those properties — no per-frame sync needed

**Split from current code:**

| Current (CanvassDesktop) | Proposed home |
|---|---|
| `Update()` polls `firstActiveRenderer.ThresholdMin/Max` every frame | `IVolumeService.ThresholdChanged` event → `RenderingTabViewModel` subscribes |
| Slider wiring in `Start()` | `RenderingTabView` bound to `MinThreshold` / `MaxThreshold` |
| `_restFrequency` field (declared, never read) | Removed — dead code confirmed by LCOM field-access analysis |
| Colormap dropdown wiring in `Start()` | `RenderingTabView.OnColorMapChanged()` → `ChangeColorMapCommand` |

### 3.4 InformationTabViewModel

**Owns:**
- `HeaderText` (string) — formatted FITS header dump
- `AxisInfo` (list of axis key/value pairs)
- `NAxis`, `ImageSize`, `MaskSize`

Populated by `IFitsService.GetHeaderAsync(path)` returning a plain DTO — no Unity types.

**Split from current code:**

| Current (CanvassDesktop) | Proposed home |
|---|---|
| `UpdateHeaderFromFits(fptr)` writes raw string directly to scroll view | `InformationTabView` bound to `HeaderText` property |
| FITS header parsed inline inside `_browseImageFile()` | `IFitsService.GetHeaderAsync(path)` returns `FitsHeaderDto` |
| Axis dimensions extracted ad-hoc; no structured type | `InformationTabViewModel.AxisInfo` list populated from `FitsHeaderDto` |

---

## 4. How We Are Doing It: Binding Mechanics

### 4.1 Property Change Notification

`INotifyPropertyChanged` is mandatory on every observable ViewModel. The canonical shape uses a `ViewModelBase` helper with a `SetField<T>` helper and `[CallerMemberName]`:

```csharp
public sealed class FileTabViewModel : ViewModelBase
{
    private string _selectedPath;
    public string SelectedPath
    {
        get => _selectedPath;
        set => SetField(ref _selectedPath, value);
    }
}
```

**Naming rules:**
- Public properties: PascalCase, no `m_` / `_` prefix.
- Backing fields: `_camelCase`.
- Booleans: `IsX` / `HasX` / `CanX`.
- No `Get`/`Set` verb prefix on properties.

**Forbidden:**
- Bare public fields (`public T Field;`) — cannot raise `PropertyChanged`.
- Side-effects in getters — getters must be pure.
- Heavy work in setters — push to a command.

### 4.2 Commands

`ICommand` is the **only** way the View invokes ViewModel behaviour. The View must not call `viewModel.DoTheThing()` directly.

**Async commands:**
- Long-running work returns `Task`; the command surface remains `ICommand`.
- ViewModel exposes `IsBusy` so the View can disable controls / show progress.
- Cancellation: `CancellationToken` flows from a companion `CancelCommand`.

**Error propagation:**
- Gateway exceptions are caught **inside** the command body.
- Surfaced to the View via a bound `ErrorMessage` (string) and a domain-typed `LastError` enum.
- ViewModels do **not** throw out of commands — the View has no handler.

### 4.3 Collections

`ObservableCollection<T>` is used for all bound lists. The Debug tab is the load test: the log stream can produce ≥ 1k entries/sec under tracing, so a bounded ring-buffer wrapper or virtualised incremental collection may be needed.

**Mutation rules:**
- All mutations on the UI thread only (see §4.4).
- Bulk loads: clear + re-add atomically, not item-by-item, to avoid UI thrash.
- Do not replace the `ObservableCollection<T>` **instance** — prefer in-place mutation. Instance replacement breaks View bindings unless explicitly re-bound.

### 4.4 Threading Model

Every bound property change and collection mutation must fire on the Unity main thread. ViewModels receiving Gateway callbacks on background threads must marshal explicitly via an `IUIDispatcher` interface:

```csharp
public interface IUIDispatcher
{
    void Post(Action action);
    void Invoke(Action action);
}
```

The concrete implementation lives in the View assembly; ViewModels take the interface via constructor injection. This avoids the `UnityMainThreadDispatcher` static singleton pattern.

**Forbidden:**
- `UnityMainThreadDispatcher.Instance.Enqueue(...)` — singleton, untestable.
- Captured `SynchronizationContext` without explicit `ConfigureAwait(false)` discipline at Gateway boundaries.

### 4.5 Data-Binding Approach (Unity Version Transition)

Unity 6 UI Toolkit supports `INotifyValueChanged<T>` and data binding natively. For the legacy Canvas system (current Unity 2021.3) the shim is:

1. ViewModel implements `INotifyPropertyChanged` (standard .NET).
2. A thin `UnityBinder<T>` helper subscribes to `PropertyChanged` and sets the UI element value.
3. Views register bindings in `Awake` / `OnEnable` and unregister in `OnDisable`.

```csharp
// View assembly only — never imported by ViewModel
public sealed class UnityBinder<T> : IDisposable
{
    private readonly INotifyPropertyChanged _source;
    private readonly string   _propertyName;
    private readonly Func<T>   _getter;
    private readonly Action<T> _setter;

    public UnityBinder(
        INotifyPropertyChanged source, string propertyName,
        Func<T> getter, Action<T> setter)
    {
        _source = source; _propertyName = propertyName;
        _getter = getter; _setter = setter;
        source.PropertyChanged += OnChanged;
    }

    private void OnChanged(object _, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == _propertyName) _setter(_getter());
    }

    public void Dispose() => _source.PropertyChanged -= OnChanged;
}

// Usage in FileTabView.Awake():
_binders = new List<IDisposable>
{
    new UnityBinder<bool>(_vm, nameof(_vm.IsLoadable),
        () => _vm.IsLoadable,   v => _loadButton.interactable = v),
    new UnityBinder<string>(_vm, nameof(_vm.HeaderText),
        () => _vm.HeaderText,   v => _headerScrollText.text = v),
};
```

This eliminates the manual `transform.Find(...)` chains and the per-frame `Update()` sync.

---

## 5. How We Are Doing It: Composition Root

### 5.1 Purpose

The existing `CanvassDesktop.Start()` does singleton hunting via `FindObjectOfType<>`. In the refactored design a single **composition root** `MonoBehaviour` wires everything at startup:

```csharp
// Composition root (pseudo-code — design only)
var fitsService    = new FitsServiceAdapter();        // wraps FitsReader P/Invoke
var volumeService  = new VolumeServiceAdapter();      // wraps VolumeCommandController
var fileDialogSvc  = new StandaloneFileDialogAdapter();
var logStream      = new UnityLogStreamAdapter();     // wraps Debug.Log
var dispatcher     = new UnityMainThreadDispatcher();

var fileVM    = new FileTabViewModel(fitsService, fileDialogSvc, volumeService);
var debugVM   = new DebugTabViewModel(logStream, dispatcher);
var renderVM  = new RenderingTabViewModel(volumeService);
var infoVM    = new InformationTabViewModel(fitsService);

fileTabView.BindTo(fileVM);
debugTabView.BindTo(debugVM);
renderingTabView.BindTo(renderVM);
informationTabView.BindTo(infoVM);
```

### 5.2 Wiring Rules

- One `MonoBehaviour` in `iDaVIE.Client.View` constructs ViewModels at scene start.
- **Constructor injection** for all dependencies. No `FindObjectOfType<>`, no service locator inside ViewModels.
- DataContext convention (UI Toolkit): `rootVisualElement.userData = viewModel` set once during composition.
- ViewModels do not hold a reference to their View. View → ViewModel is one-way.

### 5.3 Lifecycle

- ViewModels implementing `IDisposable` are disposed by the composition root on scene unload.
- Subscriptions to `ILogStream`, `IFitsService`, etc. must be unsubscribed on dispose — leaks block deterministic unit tests.
- Panel ViewModels are scoped to their `UIDocument`; the root ViewModel is scoped to the scene.

---

## 6. How We Are Doing It: Anti-Corruption Layer

The ACL lives at the Service Gateway boundary only:

```
Domain code (ViewModels)         Service adapters (Unity-dependent)
─────────────────────────        ──────────────────────────────────
IFitsService                ◄──── FitsServiceAdapter   (FitsReader P/Invoke)
IVolumeService              ◄──── VolumeServiceAdapter (VolumeCommandController)
IFileDialogService          ◄──── StandaloneFileDialogAdapter
ILogStream                  ◄──── UnityLogStreamAdapter (Debug.Log)
```

Rule: nothing left of the `◄────` line may `using UnityEngine`, `using Valve.VR`, or `[DllImport]`.

**DTO contract (Gateway ↔ ViewModel):**
- DTOs are immutable plain C# records.
- Primitive, string, or nested DTO types only.
- **No** `UnityEngine.*`, **no** `Valve.VR.*`, **no** `[JsonProperty]` or other transport attributes.
- Wire-format validation lives in the Gateway; domain validation lives in the ViewModel.

---

## 7. Worked Examples

### 7.1 File Tab — Command-Driven Round-Trip

**Before (current CanvassDesktop):**

```
CanvassDesktop.BrowseImageFile()
  └─ StandaloneFileBrowser.OpenFilePanelAsync(...)
       └─ _browseImageFile(path)
            ├─ FitsReader.FitsOpenFile(...)         // P/Invoke — untestable
            ├─ FitsReader.FitsGetHduCount(...)
            ├─ transform.Find("...Hdu_dropdown").GetComponent<TMP_Dropdown>()  // scene coupling
            ├─ IsLoadable()                         // axis math buried in 1899-line class
            └─ UpdateHeaderFromFits(fptr)           // scroll view write
```

**After (MVVM, design only):**

```
FileTabView.OnBrowseButtonClicked()
  └─ FileTabViewModel.BrowseImageCommand.Execute()
       └─ IFileDialogService.PickFileAsync(...)
            └─ IFitsService.OpenImageAsync(path)
                 returns: FitsFileInfo { HduList, NAxis, AxisSizes, HeaderText }
            └─ FileTabViewModel updates: HduOptions, IsLoadable, HeaderText

[data bindings — no transform.Find]
FileTabView.HduDropdown  ◄── FileTabViewModel.HduOptions
FileTabView.LoadButton   ◄── FileTabViewModel.IsLoadable   (computed, NUnit-testable)
FileTabView.HeaderText   ◄── FileTabViewModel.HeaderText
```

Full before/after UML and dependency graph: [`refactoring-examples/sub-team-6/file-tab/`](../../../../refactoring-examples/sub-team-6/file-tab/)

### 7.2 Debug Tab — Observer / Push Collection Binding

**Before:**

```csharp
// Scattered across CanvassDesktop:
Debug.Log("Fits open failure... code #" + status);
Debug.Log(val + " is less than the minimum...");
// No structured format, no UI component, no stream abstraction
```

**After:**

```csharp
// In ViewModels / Services:
_logStream.Publish(LogLevel.Error, "FitsOpen", $"Open failure status={status}");

// DebugTabViewModel (pure C#, no Unity):
_logStream.OnLogEntry += entry => _dispatcher.Post(() => Entries.Add(entry));

// DebugTabView (Unity):
// Bound to DebugTabViewModel.Entries — ScrollView refreshes automatically
```

Full before/after UML and dependency graph: [`refactoring-examples/sub-team-6/debug-tab/`](../../../../refactoring-examples/sub-team-6/debug-tab/)

---

## 8. Benefits

### 8.1 CK Metric Projections (Achieved by Decomposition)

| Class | WMC now | WMC target | CBO now | CBO target |
|---|---|---|---|---|
| CanvassDesktop (shell only) | 63 | ~15 | 47 | ~4 |
| FileTabViewModel | — | ~12 | — | ~5 |
| RenderingTabViewModel | — | ~8 | — | ~4 |
| DebugTabViewModel | — | ~5 | — | ~2 |
| FitsServiceAdapter | — | ~10 | — | ~6 |

All target values fall within §7.1 thresholds. Full before/after delta tables are in the D4 worked examples.

### 8.2 Testability

- `iDaVIE.Client.ViewModel` is unit-testable with NUnit + Moq, no Unity Editor required — satisfies LO6 and NFR-TST-1 (≥ 70% branch/line on domain code).
- Mocking-difficulty drops from 205 (status quo: 163 scene-graph traversals + 36 static P/Invoke) to **0** in the ViewModel assembly — all dependencies are injected interfaces.
- Test doubles for `IFitsService`, `IFileDialogService`, `ILogStream` can be in-memory stubs running without a GPU, VR headset, or file system.

### 8.3 Migration Safety

- Unity 2021.3 → Unity 6 UI Toolkit migration is scoped to `iDaVIE.Client.View` only.
- ViewModels and Gateway adapters survive unchanged across the engine upgrade.

### 8.4 Explicit Dependencies

- Composition root replaces all `FindObjectOfType<>` singleton lookups.
- Every dependency is visible at construction time — no hidden global state.

### 8.5 Long-Term Extensibility

- The `IServiceGateway` façade can run local (in-process) or over JSON-RPC named pipes without changing ViewModels — satisfying the roadmap driver for Python console and workspace-save features (ARQ-1, ARQ-2).
- A thin `IMenuRouter` interface at the VR/desktop boundary lets Sub-team 4 (Interaction System) share the command vocabulary without coupling to Unity UI types.

---

## 9. Risks and Trade-offs

| Risk | Mitigation |
|---|---|
| Unity 2021.3 has no native UI Toolkit binding | Use lightweight `UnityBinder<T>` shim; migrate to UI Toolkit binding when upgrading to Unity 6. |
| Extra ceremony: three assemblies, interface boilerplate | Code skeletons in `refactoring-examples/sub-team-6/` serve as the canonical pattern. |
| VR-side menus (Sub-team 4 dependency) | Thin `IMenuRouter` interface at the boundary — both VR and desktop implement it. |
| IPC/RPC with Sub-team 1's service gateway (DEPS-1) | `IVolumeService` façade can run local or over named pipes without changing ViewModels. Risk tracked as R01 on integration risk register. |
| "Leaky" ViewModels accidentally importing `UnityEngine` via transitive references | NDepend CQLinq rule fails the build on any forbidden import. |
| `INotifyPropertyChanged` more verbose than Unity's `[SerializeField]` + `OnValidate` | `ViewModelBase.SetField<T>` helper reduces per-property boilerplate to one line. |
| Frame-rate-sensitive rendering sync | `RenderingTabViewModel` can expose a `Tick(float dt)` method called only by the View — keeps the VM testable while frame sync stays in the adapter. |
| Log stream throughput (≥ 1k entries/sec) | Evaluate bounded ring-buffer wrapper or virtualised `ObservableCollection` for Debug tab. |

---

## 10. Forbidden Patterns

| Pattern | Why forbidden | Replacement |
|---|---|---|
| Any `UnityEngine` type in ViewModel (`using UnityEngine`, `Vector3` field, `[SerializeField]`) | Violates §4.2.3; domain code must not depend on Unity | DTO with primitives; plain C# properties; constructor injection |
| Coroutine in ViewModel | Unity-bound execution model | `async Task` + `IUIDispatcher` |
| `FindObjectOfType<>` anywhere | Singleton coupling | Constructor injection at composition root |
| `static` state on ViewModels (mutable field or event) | Breaks test isolation; shared state leaks between test runs | Instance state or instance event; inject shared state via constructor |
| `viewModel.Property = x` from View code | Bypasses command layer | `ICommand` binding |
| `Thread.Sleep` / `WaitForSeconds` in ViewModel | Blocks UI thread or Unity-bound | `Task.Delay` + `CancellationToken` |
| `event EventHandler` on ViewModel as substitute for `ICommand` | Circumvents binding contract | `ICommand` |

---

## 11. CI Enforcement

### 11.1 NDepend CQLinq — ViewModel Purity Rule

```csharp
// warnif count > 0
from t in Application.Assemblies
   .WithNameLike("iDaVIE.Client.ViewModel")
   .ChildTypes()
where t.IsUsing("UnityEngine")
   || t.IsUsing("Valve.VR")
   || t.IsUsing("System.Runtime.InteropServices")
select new {
    t,
    Issue = "ViewModel assembly forbids Unity/SteamVR/native interop (ADR-0001)"
}
```

### 11.2 PR Checklist (Reviewer-Facing)

- [ ] No `UnityEngine` / `Valve.VR` / `System.Runtime.InteropServices` import in any file under `iDaVIE.Client.ViewModel`.
- [ ] Every new public ViewModel property fires `PropertyChanged` (or is a `record` DTO field on a different type).
- [ ] Every command is bound via `ICommand`, not via direct method invocation from the View.
- [ ] No `static` mutable state on ViewModels.
- [ ] All `IDisposable` ViewModel dependencies subscribed in the constructor are disposed in `Dispose()`.
- [ ] Background-thread Gateway callbacks marshal to UI thread via `IUIDispatcher`.

---

## 12. Glossary

| Term | Meaning in this codebase |
|---|---|
| View | UI Toolkit `UIDocument` + USS/UXML; the only place `UnityEngine` types are allowed. |
| ViewModel | Pure C# class implementing `INotifyPropertyChanged`. No Unity, no SteamVR, no `DllImport`. |
| Model | Domain data; in our slice usually a DTO returned by the Gateway. |
| Gateway | The `IServiceGateway` adapter; talks to the server over the transport defined in ADR-0002. |
| DTO | Immutable transfer object across the Gateway ↔ ViewModel boundary; primitive + string + nested DTO fields only. |
| Command | `ICommand` implementation invoked by a View binding; sync or async. |
| Composition root | The single `MonoBehaviour` in the View assembly that wires ViewModels to their dependencies at scene start. |
| ACL | Anti-corruption layer — the Gateway assembly that prevents Unity/SteamVR types from leaking into domain code. |
| `IUIDispatcher` | Interface for marshalling work onto the Unity main thread from background Gateway callbacks. |

---

## 13. References

- [ADR-0002 — Client–server transport](../D2-Architecture/client-server-transport.md)
- [D4 — File-tab worked example](../D4-worked-examples/README.md#example-1--file-tab)
- [D4 — Debug-tab worked example](../D4-worked-examples/README.md#example-2--debug-tab)
- [D5 — ViewModel unit tests](../D5-testing/viewmodel-unit-tests.md)
- [D5 — UI Toolkit page-object pattern](../D5-testing/ui-toolkit.md)
- [Integration risk register](../../../team-alpha/integration-risk-register.md) — R01 (Sub-team 1 gateway contract), R04 (Sub-team 4 VR menus)
- Assignment spec §4.1 (target architectural style), §4.2.2 (no cycles), §4.2.3 (no Unity in domain), §4.2.4 (interfaces on public APIs), §6.6 (MVVM prescription + binding-policy deliverable), §7.1 (CK thresholds).
