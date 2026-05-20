# MVVM Refactoring Proposal — CanvassDesktop

**Scope:** Design ideas only. No production code is changed.  
**Target file:** `Assets/Scripts/UI/CanvassDesktop.cs` (1899 lines, single `MonoBehaviour`)  
**Date:** 2026-05-20

---

## 1. Why CanvassDesktop is a God-Object

The current class owns all of the following in a single `MonoBehaviour`:

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

CK snapshot (estimated from the single file):

| Metric | Current | Target (domain) |
|---|---|---|
| WMC | ~120 | ≤ 20 |
| CBO | ~18 | ≤ 14 |
| RFC | ~90 | ≤ 50 |
| LCOM | ~0.9 | ≤ 0.5 |

---

## 2. Target Layer Map

```
┌──────────────────────────────────────────────────────────┐
│  View Layer  (Unity 6 UI Toolkit / MonoBehaviour thin)   │
│  FileTabView  ·  DebugTabView  ·  RenderingTabView  etc. │
│  Only: bind to VM properties, forward UI events          │
└────────────────────┬─────────────────────────────────────┘
                     │ data-binding / events
┌────────────────────▼─────────────────────────────────────┐
│  ViewModel Layer  (pure C#, no UnityEngine references)   │
│  FileTabViewModel  ·  DebugTabViewModel  ·  etc.         │
│  Owns: state, validation, commands, INotifyPropertyChanged│
└────────────────────┬─────────────────────────────────────┘
                     │ calls IServiceGateway
┌────────────────────▼─────────────────────────────────────┐
│  Service Gateway  (interface + adapter)                  │
│  IFitsService  ·  IVolumeService  ·  ILogService         │
│  Translates domain calls → server RPC / native plugin    │
└──────────────────────────────────────────────────────────┘
```

The anti-corruption layer lives at the Service Gateway boundary: domain ViewModels call interfaces; only the adapters import `UnityEngine`, `SteamVR`, or native P/Invoke.

---

## 3. Splitting CanvassDesktop by Tab

Each panel/tab becomes its own ViewModel + thin View component.

### 3.1 FileTabViewModel

**Owns:**
- `ImagePath`, `MaskPath`, `SourcesPath` (observable properties)
- `HduOptions` list + `SelectedHduIndex`
- `SubsetBounds` (XMin/XMax/YMin/YMax/ZMin/ZMax)
- `SubsetEnabled` toggle
- `IsLoadable` computed property (currently inline logic in `IsLoadable()`)
- `LoadCommand`, `BrowseImageCommand`, `BrowseMaskCommand` (ICommand)

**Does NOT own:**
- Any `Transform.Find(...)` wiring — that stays in the View adapter
- The `StandaloneFileBrowser` call — goes behind `IFileDialogService`
- The `FitsReader` P/Invoke — goes behind `IFitsService`

**Key split from current code:**

| Current (CanvassDesktop) | Proposed home |
|---|---|
| `BrowseImageFile()` UI wiring | `FileTabView.OnBrowseClicked()` |
| `_browseImageFile(path)` FITS open + HDU parse | `IFitsService.OpenImageAsync(path)` |
| `IsLoadable()` axis dimension logic | `FileTabViewModel.IsLoadable` (pure C#) |
| `checkSubsetBounds()` clamping | `SubsetBoundsViewModel.Validate()` |
| `updateSubsetZMax()` dropdown sync | `FileTabViewModel.OnZAxisChanged(int)` |
| `UpdateHeaderFromFits()` scroll view write | `FileTabView` bound to `HeaderText` property |

### 3.2 DebugTabViewModel (Observer pattern)

The debug console should be a **passive observer of a structured log stream**, not a direct writer.

**Proposed design:**
```
ILogStream  ──publishes──►  DebugTabViewModel  ──binds──►  DebugTabView
                             (subscribes via event/Rx)     (scrolling text area)
```

- `ILogStream` exposes `event Action<LogEntry> OnLogEntry`
- `DebugTabViewModel` subscribes; maintains `ObservableCollection<LogEntry> Entries`
- The View scrolls to bottom when Entries changes — no manual scroll logic in ViewModel
- Existing `Debug.Log` calls in CanvassDesktop are replaced by injecting `ILogStream` and calling `ILogStream.Publish(level, message)`

**Key split from current code:**

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

The current `Update()` loop in CanvassDesktop polls `firstActiveRenderer.ThresholdMin` every frame and pushes it to the slider. This should become:

- `VolumeDataSetRenderer` raises a `ThresholdChanged` event (or ViewModel polls via `IVolumeService`)
- `RenderingTabViewModel` subscribes and updates its own properties
- `RenderingTabView` binds sliders to those properties — no per-frame sync needed

### 3.4 InformationTabViewModel

**Owns:**
- `HeaderText` (string) — formatted FITS header dump
- `AxisInfo` (list of axis key/value pairs)
- `NAxis`, `ImageSize`, `MaskSize`

Populated by `IFitsService.GetHeaderAsync(path)` returning a plain DTO — no Unity types.

---

## 4. Composition Root

The existing `CanvassDesktop.Start()` does singleton hunting via `FindObjectOfType<>`. In the refactored design a **client composition root** (a single Unity `MonoBehaviour` or `ScriptableObject`) wires everything at startup:

```csharp
// Composition root (pseudo-code, design only)
var fitsService    = new FitsServiceAdapter();       // wraps FitsReader P/Invoke
var volumeService  = new VolumeServiceAdapter();     // wraps VolumeCommandController
var fileDialogSvc  = new StandaloneFileDialogAdapter();
var logStream      = new UnityLogStreamAdapter();    // wraps Debug.Log

var fileVM    = new FileTabViewModel(fitsService, fileDialogSvc, volumeService);
var debugVM   = new DebugTabViewModel(logStream);
var renderVM  = new RenderingTabViewModel(volumeService);
var infoVM    = new InformationTabViewModel(fitsService);

// Each View receives its ViewModel via a public property or constructor
fileTabView.BindTo(fileVM);
debugTabView.BindTo(debugVM);
// etc.
```

This eliminates all `FindObjectOfType` calls from the Views and ViewModels.

---

## 5. Anti-Corruption Layer Boundaries

```
Domain code (ViewModels)         Service adapters (Unity-dependent)
─────────────────────────        ──────────────────────────────────
IFitsService                ◄──── FitsServiceAdapter  (uses FitsReader P/Invoke)
IVolumeService              ◄──── VolumeServiceAdapter (uses VolumeCommandController)
IFileDialogService          ◄──── StandaloneFileDialogAdapter (uses SFB)
ILogStream                  ◄──── UnityLogStreamAdapter (uses Debug.Log)
```

Rule: nothing left of the `◄────` line may `using UnityEngine`, `using Valve.VR`, or `[DllImport]`.

---

## 6. Data-Binding Approach

Unity 6 UI Toolkit supports `INotifyValueChanged<T>` and data binding natively. For the legacy Canvas system (current Unity 2021.3) the simplest shim is:

1. ViewModel implements `INotifyPropertyChanged` (standard .NET)
2. A thin `UnityBinder<T>` helper class subscribes to `PropertyChanged` and sets the UI element value
3. Views register bindings in `Awake` / `OnEnable` and unregister in `OnDisable`

This avoids the manual `transform.Find(...)` chains and the per-frame `Update()` sync.

---

## 7. Worked Example — File Tab (Before → After)

### Before (current CanvassDesktop, simplified)

```
CanvassDesktop.BrowseImageFile()
  └─ StandaloneFileBrowser.OpenFilePanelAsync(...)
       └─ _browseImageFile(path)
            ├─ FitsReader.FitsOpenFile(...)         // P/Invoke
            ├─ FitsReader.FitsGetHduCount(...)
            ├─ transform.Find("...Hdu_dropdown").GetComponent<TMP_Dropdown>()  // scene coupling
            ├─ IsLoadable()                         // axis math
            └─ UpdateHeaderFromFits(fptr)           // scroll view write
```

All of this lives in one 1899-line class.

### After (MVVM, design only)

```
FileTabView.OnBrowseButtonClicked()
  └─ FileTabViewModel.BrowseImageCommand.Execute()
       └─ IFileDialogService.PickFileAsync(...)
            └─ IFitsService.OpenImageAsync(path)
                 returns: FitsFileInfo { HduList, NAxis, AxisSizes, HeaderText }
            └─ FileTabViewModel updates: HduOptions, IsLoadable, HeaderText
  [binding]
FileTabView.HduDropdown ◄── FileTabViewModel.HduOptions  (no transform.Find)
FileTabView.LoadButton  ◄── FileTabViewModel.IsLoadable   (computed, testable)
FileTabView.HeaderText  ◄── FileTabViewModel.HeaderText
```

---

## 8. Worked Example — Debug Tab (Before → After)

### Before

```
// Scattered across CanvassDesktop methods:
Debug.Log("Fits open failure... code #" + status);
Debug.Log(val + " is less than the minimum...");
// No structured format, no UI display component, no stream abstraction
```

### After

```
// In ViewModels / Services:
_logStream.Publish(LogLevel.Error, "FitsOpen", $"Open failure status={status}");

// DebugTabViewModel (pure C#):
_logStream.OnLogEntry += entry => Entries.Add(entry);

// DebugTabView (Unity):
// Bound to DebugTabViewModel.Entries — appends to ScrollView automatically
```

---

## 9. CK Metric Projections

| Class | WMC now | WMC after | CBO now | CBO after |
|---|---|---|---|---|
| CanvassDesktop | ~120 | ~15 (shell only) | ~18 | ~4 |
| FileTabViewModel | — | ~12 | — | ~5 |
| RenderingTabViewModel | — | ~8 | — | ~4 |
| DebugTabViewModel | — | ~5 | — | ~2 |
| FitsServiceAdapter | — | ~10 | — | ~6 |

---

## 10. Risks and Trade-offs

| Risk | Mitigation |
|---|---|
| Unity 2021.3 has no native UI Toolkit binding | Use lightweight `UnityBinder<T>` shim; migrate to UI Toolkit when upgrading to Unity 6 |
| VR-side menus (Sub-team 4 dependency) | Keep a thin `IMenuRouter` interface at the boundary; both VR and desktop implement it |
| IPC/RPC with Sub-team 1's service gateway | Use `IVolumeService` as a façade that can run local (in-process) or over named pipes without changing ViewModels |
| Frame-rate sensitive rendering sync | `RenderingTabViewModel` can still expose a `Tick(float dt)` method that only the View calls — keeps the VM testable, frame-sync stays in the adapter |

---

## 11. Files to Create (Design Artefacts)

| Artefact | Path |
|---|---|
| Interface definitions | `refactoring-examples/sub-team-6/interfaces/` |
| Before/after PlantUML diagrams | `refactoring-examples/sub-team-6/uml/` |
| FileTabViewModel skeleton | `refactoring-examples/sub-team-6/FileTabViewModel.cs` |
| DebugTabViewModel skeleton | `refactoring-examples/sub-team-6/DebugTabViewModel.cs` |
| Test stubs for ViewModels | `refactoring-examples/sub-team-6/tests/` |

These are **example files**, not changes to `Assets/`.
