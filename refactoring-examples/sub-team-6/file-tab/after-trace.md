# File tab — AFTER trace ("Open & load → cube visible" via MVVM + service gateway)

This is the structural counterpart to [`before-trace.md`](before-trace.md). Every message below is anchored to a file and line in the skeleton (`skeleton/`) or adapter (`adapters/`) code that already lives in this folder, so the AFTER sequence diagram is defensible at the panel.

The single visible difference at the user level: the UI no longer needs two clicks. The `LoadCommand` is enabled the moment `IsLoadable` becomes true (see `FileTabViewModel.cs:54`), so the user can either click *Open* (which auto-validates and enables *Load*) or — once a recent path is remembered — click *Load* directly. Phases A and B are kept as separate sections below to preserve a row-by-row before/after mapping.

---

## Actors / lifelines

| Lifeline | Backing type | Notes |
|---|---|---|
| `User` | — | Desktop operator |
| `FileTabView` | `adapters/FileTabView.cs` | Thin Unity MonoBehaviour. Subscribes to VM commands; renders VM properties. Replaces the file-tab slice of `CanvassDesktop`. |
| `FileTabVM` | `skeleton/FileTabViewModel.cs` | Pure C# ViewModel. Owns selection state, validation, command logic. No `UnityEngine` reference. |
| `IFileDialogService` | `skeleton/IFileDialogService.cs` | Domain interface for OS file pickers. |
| `FileDialogAdapter` | `adapters/FileDialogServiceAdapter.cs` | Wraps `StandaloneFileBrowser`. Owns `PlayerPrefs` persistence. |
| `IFitsService` | `skeleton/IFitsService.cs` | Domain interface for FITS metadata. |
| `FitsAdapter` | `adapters/FitsServiceAdapter.cs` | Wraps `FitsReader` P/Invoke calls. The only code allowed to touch `IntPtr`. |
| `IVolumeService` | `skeleton/IVolumeService.cs` | Gateway to Sub-team 1's micro-kernel. |
| `VolumeAdapter` | `adapters/VolumeServiceAdapter.cs` | Owns the load coroutine, prefab instantiation, native-memory cleanup. |
| `VCC` | `VolumeCommandController` | Reached only via `VolumeAdapter`; never from the VM. |

The ACL boundary in the diagram is the vertical line between the *interfaces* and the *adapters* — the ViewModel sits entirely to the left of it.

---

## Phase A — User picks a file (metadata read, no cube yet)

| #  | Message | Source citation | Notes / improvement vs BEFORE |
|----|---|---|---|
| A1 | User clicks **Browse Image** | `FileTabView` button binding | Same UX, but the button is wired to `BrowseImageCommand.ExecuteAsync` in code (`FileTabView.BindTo`), not in the Inspector. Replaces BEFORE A1. |
| A2 | `FileTabView → FileTabVM.BrowseImageCommand.ExecuteAsync()` | `IFileTabViewModel.cs:49`, `FileTabViewModel.cs:52` | Code-side subscription is unit-testable; replaces BEFORE A2 (Inspector wiring). |
| A3 | `FileTabVM → IFileDialogService.PickFileAsync(title, "", ["fits","fit"])` | `FileTabViewModel.cs:168` | The VM doesn't read `PlayerPrefs`; the adapter does (`FileDialogServiceAdapter.cs:28-30`). Replaces BEFORE A3+A4. |
| A4 | `FileDialogAdapter` reads `PlayerPrefs.GetString("LastPath")` and calls `StandaloneFileBrowser.OpenFilePanelAsync(...)` | `FileDialogServiceAdapter.cs:28-41` | Unity-side state contained to the adapter. |
| A5 | OS native picker shown; user selects `*.fits` | SFB plug-in | Same UX. |
| A6 | `FileDialogAdapter` writes `PlayerPrefs.SetString("LastPath", ...)` and resolves the `TaskCompletionSource` | `FileDialogServiceAdapter.cs:41-53` | The async callback is converted to a `Task<string?>` — the VM `await`s it; no callback closure into a `MonoBehaviour`. |
| A7 | `FileTabVM` sets `IsLoading = true`; clears `ValidationMessage` | `FileTabViewModel.cs:172-173` | `INotifyPropertyChanged` drives spinner / disabled buttons declaratively. |
| A8 | `FileTabVM → IFitsService.OpenImageAsync(path)` | `FileTabViewModel.cs:176` | **★ The replacement.** No P/Invoke from the VM. Returns a plain `FitsFileInfo` DTO. |
| A9 | `FitsAdapter` runs `Task.Run(ReadFitsMetadata)` — calls `FitsReader.FitsOpenFile / FitsGetHduCount / FitsMovabsHdu / FitsReadKey / FitsCloseFile` inside a `try { ... } finally { FitsCloseFile }` block | `FitsServiceAdapter.cs:28-108` | All `IntPtr` lifetime contained to one method. **RAII achieved by `try/finally`**: no `IntPtr` outlives `ReadFitsMetadata` (`FitsServiceAdapter.cs:103-107`). Replaces BEFORE A8–A16. |
| A10 | `FitsAdapter` returns `new FitsFileInfo { FilePath, HduList, NAxis, AxisSizes, HeaderText }` | `FitsServiceAdapter.cs:94-101`, `FitsFileInfo.cs:14-25` | Immutable DTO with `required` init-only properties. |
| A11 | `FileTabVM` populates `_hduOptions`, `HeaderText`, Z-axis options; resets `Subset` to axis maxima; invalidates any prior mask | `FileTabViewModel.cs:177-200` | All state mutation goes through setters that raise `PropertyChanged` — the View updates automatically. Replaces BEFORE A14 (transform.Find HDU dropdown mutation). |
| A12 | `FileTabVM` computes `IsLoadable` and sets `ValidationMessage` | `FileTabViewModel.cs:136-152, 202` | Pure C# axis-count rule, fully unit-testable. Replaces BEFORE A17. |
| A13 | `FileTabVM` sets `IsLoading = false`; `LoadCommand.CanExecute()` becomes true | `FileTabViewModel.cs:210, 54` | View re-evaluates button enablement via `CanExecuteChanged`. No `transform.Find` to manually toggle interactability. |
| A14 | `FileTabView` re-renders: HDU dropdown populated, header text shown, Load button enabled, subset panel visible | `FileTabView.BindTo` | All driven by `PropertyChanged` events; the View has no domain logic. Replaces BEFORE A18. |

**At end of Phase A:** the FITS metadata has been read on a background thread (via `Task.Run` in the adapter), the native file pointer is closed inside the adapter, no `IntPtr` exists in the VM, and the UI is bound declaratively. The user *can* click Load now — but does not have to wait through a separate "open" round-trip if validation already passed.

---

## Phase B — User confirms load (cube becomes visible)

| #  | Message | Source citation | Notes / improvement vs BEFORE |
|----|---|---|---|
| B1 | User clicks **Load** | `FileTabView` button binding | Same UX. |
| B2 | `FileTabView → FileTabVM.LoadCommand.ExecuteAsync()` | `IFileTabViewModel.cs:51`, `FileTabViewModel.cs:54` | Code-side subscription. `CanExecute()` guard prevents double-clicks (`AsyncRelayCommand.CanExecute`, `FileTabViewModel.cs:381`). |
| B3 | `FileTabVM` builds plain `LoadCubeRequest` DTO | `FileTabViewModel.cs:263-270`, `LoadCubeRequest.cs` | One immutable request crosses the ACL — replaces 8 direct field writes onto `VolumeDataSetRenderer` in BEFORE B11. |
| B4 | `FileTabVM → IVolumeService.LoadCubeAsync(request, progress)` | `FileTabViewModel.cs:271` | The VM `await`s a `Task` — no `StartCoroutine`, no busy-wait, no `WaitForSeconds(0.1f)`. Replaces BEFORE B3, B15. |
| B5 | `VolumeAdapter.LoadCubeAsync` creates `TaskCompletionSource<bool>`; starts `LoadCubeCoroutine(request, progress, tcs)` | `VolumeServiceAdapter.cs:43-51` | Coroutine is contained inside the adapter; the VM never sees it. |
| B6 | (coroutine, Phase 1) `VolumeAdapter` tears down any active renderer: `SetActive(false)` → `VCC.RemoveDataSet` → input-controller reset → `DisablePaintMode / endThresholdEditing / endZAxisEditing` → `Data.CleanUp`, `Mask?.CleanUp`, `Destroy(renderer)` | `VolumeServiceAdapter.cs:62-86` | Same logical steps as BEFORE B5–B8, but contained behind `IVolumeService`. The VM is unaffected by this internal recipe — Sub-team 1 can replace it. |
| B7 | (coroutine, Phase 2) `Instantiate(_cubePrefab)` → `GetComponent<VolumeDataSetRenderer>()`; field assignment from `request` (`FileName`, `MaskFileName`, `SelectedHdu`, `CubeDepthAxis`, optional `subsetBounds`/`trueBounds`) | `VolumeServiceAdapter.cs:108-124` | Field writes happen inside the Unity assembly. The VM never sees `VolumeDataSetRenderer`. Replaces BEFORE B9–B11. |
| B8 | `VolumeAdapter` reports `progress.Report(0.4f)`; toggles `VolumeInputController` | `VolumeServiceAdapter.cs:132-140` | Same "reset" trick, but the VM-side spinner is driven by `IProgress<float>` instead of a coroutine on the UI lifeline. |
| B9 | `VolumeAdapter → VCC.AddDataSet(renderer)`; `StartCoroutine(renderer._startFunc())` | `VolumeServiceAdapter.cs:142-143` | `VCC` access is encapsulated in the adapter (constructor-resolved via `FindObjectOfType` once in `Awake`, `VolumeServiceAdapter.cs:34-39`). Replaces BEFORE B5, B12, B13. |
| B10 | `VolumeAdapter` polls `while (!renderer.started) yield return WaitForSeconds(0.1f)` then `tcs.TrySetResult(true)` | `VolumeServiceAdapter.cs:147-151` | The busy-wait still exists — but only inside the adapter, returned as a completed `Task` to the VM. The smell is contained, not eliminated. Listed under [Known limitations](#known-limitations). |
| B11 | `IVolumeService.LoadCubeAsync` task resolves; `FileTabVM` sets `IsLoading = false` | `FileTabViewModel.cs:279` | All command-state notifications fire via `PropertyChanged` / `CanExecuteChanged`. |
| B11½ | `VolumeAdapter` raises `IVolumeService.CubeLoaded(CubeLoadedEventArgs)` exactly once | `VolumeServiceAdapter.cs` (after `tcs.TrySetResult`) | **★ The replacement for `postLoadFileFileSystem`.** Peer-tab ViewModels (Rendering, Stats, Sources, Paint) subscribed at construction and receive a plain DTO — no renderer reference crosses the boundary. Replaces BEFORE B18-B20 cross-tab cascade. See [Anomaly #8 mitigation](#anomaly-8-rest-frequency-subscription-leak--how-the-new-architecture-prevents-it). |
| B12 | Peer-tab VMs (Rendering / Stats / Sources / Paint) receive `CubeLoaded` and rebind their own state. Each peer VM is responsible for unsubscribing from any *previous* renderer event in its own handler — the File tab no longer reaches into other panels. | each peer tab's `OnCubeLoaded` handler | The mask-dropdown enable, colormap repopulate, stats populate, rest-frequency repopulate, and tab-unlock cascade of BEFORE all become subscriber-driven; the File tab knows nothing about them. |
| B13 | User sees cube in scene | `VDSR.started == true` triggers Unity-side render | Same UX. |

**At end of Phase B:** the cube is rendered. The `FileTabVM` is reachable from a unit test (`tests/FileTabViewModelTests.cs:99-101`); the only Unity-bound code on the path is the View (thin), the three adapters, and the cube renderer itself.

---

## Smells eliminated, contained, or remaining

| ID (from before-trace) | Smell | Status | Where it now lives |
|---|---|---|---|
| S1 | Direct `[DllImport]` from UI | **eliminated for VM** | Confined to `FitsServiceAdapter.cs:42-107`. VM has no `using System.Runtime.InteropServices`. |
| S2 | God class | **eliminated** | Split into `FileTabView` (Unity), `FileTabViewModel` (domain), `SubsetBoundsViewModel` (domain), three adapters. |
| S3 | `transform.Find` chains | **eliminated for File-tab slice** | View binds by reference (Inspector-assigned fields on `FileTabView`); no string-path lookups. |
| S4 | `FindObjectOfType<>` singletons | **contained** | Two calls remain inside `VolumeServiceAdapter.Awake` (`VolumeServiceAdapter.cs:37-38`). Acceptable per ACL: adapter-only, not domain. |
| S5 | Public mutable field writes onto renderer | **contained** | Still happens inside `VolumeServiceAdapter.LoadCubeCoroutine` (`VolumeServiceAdapter.cs:115-124`). One place to refactor when Sub-team 3 encapsulates `VolumeDataSetRenderer`. |
| S6 | Busy-wait on `started` | **contained** | Still in `VolumeServiceAdapter.cs:147-148`. Surfaced to the VM as an `IProgress<float>` + completed `Task` — listed as a follow-up. |
| S7 | Inspector-wired button handlers | **eliminated** | `FileTabView.BindTo` subscribes in code. Tests construct the VM directly. |
| S8 | Unmanaged `fptr` lifetime spread across UI | **eliminated** | `try { ... } finally { FitsCloseFile }` blocks in `FitsServiceAdapter` ensure RAII (`FitsServiceAdapter.cs:103-107, 125-128`). |

---

## Anomaly #8 (rest-frequency subscription leak) — how the new architecture prevents it

Scope §10 Anomaly #8 documents a real cross-tab leak in BEFORE: every cube reload adds two handlers to the new active renderer (`RestFrequencyGHzListIndexChanged`, `RestFrequencyGHzChanged`) **without unsubscribing the previous renderer's**, because the only unsubscribe sits in `CanvassDesktop.OnDestroy` against the renderer captured once in `Awake`.

The AFTER design closes this defect structurally:

1. **The File tab no longer subscribes to peer-tab events.** `CanvassDesktop.postLoadFileFileSystem` is replaced by `IVolumeService.CubeLoaded` (skeleton/IVolumeService.cs). Peer tabs subscribe themselves on their own construction; the File tab never names the renderer or its events.
2. **Subscribers own their own lifetime.** Each peer-tab ViewModel is constructed by its own composition root and `IDisposable`-disposes its `CubeLoaded` handler on OnDestroy. There is exactly one subscribe per VM instance and exactly one unsubscribe — no per-reload accumulation.
3. **The event payload is a plain DTO**, not the renderer itself. `CubeLoadedEventArgs` (skeleton/CubeLoadedEventArgs.cs) carries `ImagePath`, `MaskPath`, `HduIndex` — no `VolumeDataSetRenderer` reference. A peer-tab VM that wants to rebind to renderer events must do so via its own service abstraction, where the same Dispose-on-rebind pattern applies. No previous-renderer reference can leak via this event.
4. **Tested.** `tests/FileTabViewModelTests.cs::Load_PeerTabUnsubscribes_DoesNotFireForFutureLoads` shows that a peer-tab subscriber who calls `CubeLoaded -= handler` stops receiving subsequent loads — the exact behaviour that was missing in BEFORE.

The leak is therefore *eliminated* (not merely contained), and the elimination is visible in the public API surface, not buried in implementation discipline.

---

## Known limitations

Two smells are **contained, not removed**, and the panel should expect questions:

1. **`VolumeServiceAdapter` still polls `renderer.started`** (`VolumeServiceAdapter.cs:147`). Proper fix: `VolumeDataSetRenderer` should expose an `event` or `Task` signalling readiness. That requires Sub-team 3 to change the renderer contract — out of scope for our slice, listed as a hand-off item.
2. **`VolumeServiceAdapter` still pokes public fields on `VolumeDataSetRenderer`** (`VolumeServiceAdapter.cs:115-124`). Same fix vector: when Sub-team 3 introduces an `IRendererCommand` interface, the adapter starts emitting commands instead.

Both items live behind the `IVolumeService` interface and can be swapped without touching `FileTabViewModel` or any of its 27 unit tests.

---

## How this becomes the Mermaid diagram

See [`after-sequence.md`](after-sequence.md). The conversion follows the same rules as [`before-trace.md` §97-104](before-trace.md):

- Phases A and B drawn as one continuous diagram with a `Note` separator labelled "validation passed — Load enabled."
- `activate` bars only on `FileTabVM` (during command execution) and `VolumeAdapter` (during the load coroutine). The View is stateless on the critical path — no `activate` bar.
- The ACL boundary is rendered as a `box` around `[FileDialogAdapter, FitsAdapter, VolumeAdapter, VCC]` so the panel sees at a glance that no message originates *from* the VM *to* anything inside the box without going through an interface.
- The two contained smells (B6 field writes, B10 busy-wait) are annotated with `Note right of VolumeAdapter` so the AFTER diagram is honest about what was kept.
