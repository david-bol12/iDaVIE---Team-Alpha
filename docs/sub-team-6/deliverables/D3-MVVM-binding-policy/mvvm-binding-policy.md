# D3: MVVM Binding Policy

- **Status:** draft (scaffold — sections marked _TODO_ need sub-team decisions)
- **Date:** 2026-05-21
- **Authors:** _(Sub-team 6 — fill in)_
- **Backlog:** ARCH-9
- **Decision basis:** [ADR-0001 — MVVM split](../../adrs/0001-mvvm-split.md)
- **Consumes:** [ADR-0002 — Client–server transport](../D2-Architecture/client-server-transport.md)
- **Consumers:** [D4 File-tab worked example](../D4-worked-examples/ex1-file-tab/), [D4 Debug-tab worked example](../D4-worked-examples/ex2-debug-tab/), [D5 ViewModel unit tests](../D5-testing/viewmodel-unit-tests.md), [D5 UI Toolkit page-object pattern](../D5-testing/ui-toolkit.md)

## Purpose

ADR-0001 commits the desktop client to a three-assembly MVVM split (`View → ViewModel → Gateway`). This document specifies **how** Views bind to ViewModels, **what** patterns are mandatory, **what** is forbidden, and **how** the rules are enforced in CI. It is the operating manual that lets a reviewer accept or reject a PR on objective grounds.

If ADR-0001 answers *"are we MVVM?"*, this document answers *"is this PR's MVVM correct?"*

## Scope

**In scope**
- Property change notification mechanics (`INotifyPropertyChanged`)
- Command dispatch (`ICommand`, sync + async, error propagation)
- Collection bindings (Debug tab log stream is the driver)
- Threading model — what marshals onto the UI thread, where
- View↔ViewModel wiring at the composition root
- ViewModel lifecycle (construction, disposal, scoping)
- DTO contract between Gateway and ViewModel
- Forbidden patterns with concrete code examples
- CI enforcement (NDepend CQLinq rules + PR checklist)

**Out of scope**
- Server-side gateway implementation (Sub-team 1 owns)
- Transport wire format — see [ADR-0002](../D2-Architecture/client-server-transport.md)
- UI Toolkit USS/UXML styling conventions
- Per-panel widget choices

---

## 1. Property change notification

### 1.1 `INotifyPropertyChanged` is mandatory on observable ViewModels

_TODO — decide: hand-rolled `ViewModelBase` with `SetField<T>` helper using `[CallerMemberName]`, **or** adopt `CommunityToolkit.Mvvm` source generators. Trade-off: source-gen reduces boilerplate but adds a NuGet dependency that must be vetted against the Unity 2021.3 / Unity 6 toolchain._

Canonical shape (replace once §1.1 is decided):

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

### 1.2 Property naming

- PascalCase. No `m_` / `_` prefix on the public property.
- Backing fields: `_camelCase`.
- Booleans: `IsX` / `HasX` / `CanX`.
- No `Get`/`Set` verb prefix on properties (use methods or commands for behaviour).

### 1.3 Forbidden

- Bare public fields (`public T Field;`) — cannot raise `PropertyChanged`.
- Side-effects in getters — getters must be pure.
- Heavy work in setters — push to a command.

---

## 2. Commands

### 2.1 `ICommand` is the only way the View invokes ViewModel behaviour

_TODO — spec the concrete `RelayCommand` / `AsyncRelayCommand` shape (hand-rolled or CommunityToolkit). Define `CanExecute` semantics and parameter type rules._

### 2.2 Async commands

- Long-running work returns `Task`; the command surface remains `ICommand`.
- ViewModel exposes `IsBusy` so the View can disable controls / show progress.
- Cancellation: `CancellationToken` flows from a companion `CancelCommand`.

### 2.3 Error propagation

- Gateway exceptions are caught **inside** the command body.
- Surface to the View via a bound `ErrorMessage` (string) and a domain-typed `LastError` enum.
- ViewModels do **not** throw out of commands — the View has no handler.

### 2.4 Forbidden

- `event EventHandler` on the ViewModel as a substitute for `ICommand`.
- View code calling `viewModel.DoTheThing()` directly — must be a bound `ICommand`.

---

## 3. Collections

### 3.1 `ObservableCollection<T>` for bound lists

_TODO — Debug tab is the load test: pick between vanilla `ObservableCollection<T>`, a bounded ring-buffer wrapper, or a virtualised incremental collection. Constraint: log stream can produce ≥ 1k entries/sec under tracing._

### 3.2 Mutation rules

- All mutations on the UI thread only (see §4).
- Bulk loads: clear + re-add atomically, not item-by-item, to avoid UI thrash.
- Avoid replacing the `ObservableCollection<T>` **instance** — prefer in-place mutation. Instance replacement breaks View bindings unless explicitly re-bound.

---

## 4. Threading

### 4.1 Single UI-thread rule

- Every bound property change and collection mutation fires on the Unity main thread.
- ViewModels receiving Gateway callbacks on background threads must marshal explicitly.

### 4.2 Marshalling primitive

_TODO — specify an `IUIDispatcher` interface (`Post(Action)`, `Invoke(Action)`). Concrete implementation lives in the View assembly; ViewModels take the interface via constructor injection. Avoids the `UnityMainThreadDispatcher` static singleton pattern._

### 4.3 Forbidden

- `UnityMainThreadDispatcher.Instance.Enqueue(...)` — singleton, untestable.
- Captured `SynchronizationContext` without explicit `ConfigureAwait(false)` discipline at Gateway boundaries.

---

## 5. View ↔ ViewModel wiring

### 5.1 Composition root owns instantiation

- One `MonoBehaviour` in `iDaVIE.Client.View` constructs ViewModels at scene start.
- **Constructor injection** for all dependencies. No `FindObjectOfType<>`, no service locator inside ViewModels.
- DataContext convention (UI Toolkit): `rootVisualElement.userData = viewModel` set once during composition.

### 5.2 No View references in ViewModel

- ViewModels do not know `VisualElement`, `UIDocument`, `Camera`, `Transform`, `GameObject`.
- View → ViewModel is a one-way reference. The reverse is forbidden.

### 5.3 Forbidden

- Static `Instance` accessors on ViewModels.
- ViewModels touching the scene graph (`GameObject.Find`, `transform.Find`, …).

---

## 6. ViewModel lifecycle

_TODO — define construction order, disposal trigger, and scope. Panel ViewModels are scoped to their `UIDocument`; the root ViewModel is scoped to the scene. Spell out who calls `Dispose()` and when._

- ViewModels implementing `IDisposable` are disposed by the composition root on scene unload.
- Subscriptions to `ILogStream`, `IFitsService`, etc. must be unsubscribed on dispose — leaks block deterministic unit tests.

---

## 7. DTO boundary (Gateway ↔ ViewModel)

### 7.1 DTOs are plain C# records

- Immutable (`record` types or `init`-only properties).
- Primitive, string, or nested DTO types only.
- **No** `UnityEngine.*`, **no** `Valve.VR.*`, **no** `[JsonProperty]` or other transport attributes (wire-format concerns live in Gateway).

### 7.2 Validation siting

- Wire-format validation → Gateway (rejects malformed JSON-RPC payloads).
- Domain validation → ViewModel (e.g. "selected axis index must be < axis count").

---

## 8. Forbidden patterns — concrete examples

_TODO — fill each row with a one-line code snippet and the rule it violates. The list below is the starting set; extend as PR reviews surface new anti-patterns._

| Pattern | Why forbidden | Replacement |
|---|---|---|
| `using UnityEngine;` in ViewModel file | ADR-0001 dependency direction; §4.2.3 | DTO with primitive fields |
| `Vector3` field on ViewModel | UnityEngine leak | `(float X, float Y, float Z)` record / tuple |
| Coroutine in ViewModel | Unity-bound | `async Task` + `IUIDispatcher` |
| `[SerializeField]` on ViewModel | Unity-bound | Plain property; configure via constructor |
| `FindObjectOfType<>` anywhere | Singleton coupling | Constructor injection |
| Static event on ViewModel | Lifecycle / test isolation | Instance event or `IObservable<T>` |
| `viewModel.Property = x` from View code | Bypasses commands | `ICommand` binding |
| `Thread.Sleep` / `WaitForSeconds` in ViewModel | Blocks UI thread or Unity-bound | `Task.Delay` + cancellation |

---

## 9. Worked binding examples

Cross-references to the two D4 worked refactoring examples — these are where the policy is exercised end-to-end. The skeleton code in `refactoring-examples/sub-team-6/` is the canonical, defensible reference.

### 9.1 File tab — command-driven round-trip

_TODO — walk through `BrowseImageCommand` → `IFileDialogService` → `IFitsService` → `FileTabViewModel.SelectedDataset` property change → View refresh. Cite the specific files in [`refactoring-examples/sub-team-6/file-tab/`](../../../../refactoring-examples/sub-team-6/file-tab/)._

### 9.2 Debug tab — observer/push collection binding

_TODO — walk through `ILogStream.Emit` → `DebugTabViewModel.LogEntries.Add` (marshalled to UI thread) → `ListView` virtualised refresh. Cite the specific files in [`refactoring-examples/sub-team-6/debug-tab/`](../../../../refactoring-examples/sub-team-6/debug-tab/)._

---

## 10. CI enforcement

### 10.1 NDepend CQLinq — ViewModel purity rule

_TODO — finalise rule wording with the Quality Guild. Draft sketch:_

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

### 10.2 Roslyn analyzer / source-gen

_TODO — pick analyzer set. If `CommunityToolkit.Mvvm` is adopted in §1.1, its `[ObservableProperty]` source generator removes a class of boilerplate bugs. Otherwise, a custom analyzer on `INotifyPropertyChanged` adherence._

### 10.3 PR checklist (reviewer-facing)

- [ ] No `UnityEngine` / `Valve.VR` / `System.Runtime.InteropServices` import in any file under `iDaVIE.Client.ViewModel`.
- [ ] Every new public ViewModel property fires `PropertyChanged` (or is a `record` DTO field on a different type).
- [ ] Every command is bound via `ICommand`, not via direct method invocation from the View.
- [ ] No `static` mutable state on ViewModels.
- [ ] All `IDisposable` ViewModel dependencies subscribed in the constructor are disposed in `Dispose()`.
- [ ] Background-thread Gateway callbacks marshal to UI thread via `IUIDispatcher`.

---

## 11. Glossary

| Term | Meaning in this codebase |
|---|---|
| View | UI Toolkit `UIDocument` + USS/UXML; the only place `UnityEngine` types are allowed. |
| ViewModel | Pure C# class implementing `INotifyPropertyChanged`. No Unity, no SteamVR, no `DllImport`. |
| Model | Domain data; in our slice usually a DTO returned by the Gateway. |
| Gateway | The `IServiceGateway` adapter; talks to the server over the transport defined in [ADR-0002](../D2-Architecture/client-server-transport.md). |
| DTO | Immutable transfer object across the Gateway ↔ ViewModel boundary; primitive + string + nested DTO fields only. |
| Command | `ICommand` implementation invoked by a View binding; sync or async. |
| Composition root | The single `MonoBehaviour` in the View assembly that wires ViewModels to their dependencies at scene start. |

---

## References

- [ADR-0001 — MVVM split for the desktop client shell](../../adrs/0001-mvvm-split.md)
- [ADR-0002 — Client–server transport](../D2-Architecture/client-server-transport.md)
- [D2 — Desktop client architecture](../../architecture.md) — this policy is D2's operational companion
- [D4 — File-tab worked example](../D4-worked-examples/ex1-file-tab/)
- [D4 — Debug-tab worked example](../D4-worked-examples/ex2-debug-tab/)
- [D5 — ViewModel unit tests](../D5-testing/viewmodel-unit-tests.md)
- [D5 — UI Toolkit page-object pattern](../D5-testing/ui-toolkit.md)
- Assignment spec §4.2.3 (no Unity/SteamVR in domain), §4.2.4 (interfaces + test doubles on public API boundaries), §6.6 (MVVM prescription + binding-policy deliverable), §7.1 (CK thresholds).
