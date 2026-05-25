# File tab — "Open → file loaded → cube visible" (current path)

Raw code-side trace of the production behaviour as of branch `team6`. Every message below is anchored to a file and line in the live codebase so the resulting sequence diagram is defensible at the maintainer panel.

The full sequence diagram pairs this trace with a GUI walkthrough (the user-visible clicks, dialogs, panels). Sections marked **[GUI WALK]** are gaps to be filled by the walkthrough capture.

---

## Actors / lifelines

| Lifeline | Backing type | Notes |
|---|---|---|
| `User` | — | Desktop operator |
| `OpenButton` | Unity `Button` (scene asset) | Wired to `CanvassDesktop.BrowseImageFile()` **via Inspector**, not in code |
| `LoadButton` | Unity `Button` (scene asset) | Wired to `CanvassDesktop.LoadFileFromFileSystem()` via Inspector |
| `CanvassDesktop` | `Assets/Scripts/UI/CanvassDesktop.cs` | 1899-line god-class `MonoBehaviour` |
| `StandaloneFileBrowser` (SFB) | Third-party plug-in (`using SFB;` at line 31) | Async file dialog |
| `FitsReader` | `Assets/Scripts/PluginInterface/FitsReader.cs` | Thin static wrapper over `idavie_native.dll` |
| `idavie_native` | C/C++ native DLL | The plug-in boundary — every `[DllImport("idavie_native")]` symbol |
| `VolumeCommandController` | `Assets/Scripts/VolumeData/VolumeCommandController.cs` | Singleton-style, located via `FindObjectOfType<>` |
| `VolumeDataSetRenderer` | `Assets/Scripts/VolumeData/VolumeDataSetRenderer.cs` | Instantiated from `cubeprefab`, becomes the visible cube |
| `VolumeInputController` | — | Refreshed via `FindObjectOfType<>` toggle |

---

## Phase A — User picks a file (metadata read, no cube yet)

| # | Message | Source citation | Notes / smell |
|---|---|---|---|
| A1 | **[GUI WALK]** User clicks **File → Open** | scene asset | Need walkthrough: which menu / button label exactly |
| A2 | `OpenButton.onClick → CanvassDesktop.BrowseImageFile()` | `CanvassDesktop.cs:306` | Wiring lives in Inspector, not code → untestable in pure C# |
| A3 | `CanvassDesktop` reads `PlayerPrefs.GetString("LastPath")` | `CanvassDesktop.cs:309` | Direct global state read |
| A4 | `CanvassDesktop → StandaloneFileBrowser.OpenFilePanelAsync(..., callback)` | `CanvassDesktop.cs:317` | Async, returns immediately; callback invoked when user picks a file |
| A5 | **[GUI WALK]** Native OS file picker appears; user selects `*.fits` | SFB plug-in | Walkthrough: confirm dialog title, default folder |
| A6 | `StandaloneFileBrowser → callback(paths)` | `CanvassDesktop.cs:317–326` | Callback closes over `CanvassDesktop` instance |
| A7 | `callback → CanvassDesktop._browseImageFile(paths[0])` | `CanvassDesktop.cs:324` (call) / `:329` (def) | Private method, mixed concerns |
| A8 | `CanvassDesktop → FitsReader.FitsOpenFile(out fptr, _imagePath, out status, true)` | `CanvassDesktop.cs:349` | **★ The smell.** Direct call to native-plugin wrapper from the UI layer |
| A9 | `FitsReader → idavie_native.FitsOpenFileReadOnly(...)` | `FitsReader.cs:199–211` | `[DllImport("idavie_native")]` — literal P/Invoke boundary |
| A10 | `idavie_native` returns `fptr`, `status=0` | — | Unmanaged handle leaked into MonoBehaviour scope |
| A11 | `CanvassDesktop → FitsReader.FitsGetHduCount(fptr, out hduNum, out status)` | `CanvassDesktop.cs:358` | Second direct DLL hop |
| A12 | Loop ×`hduNum`: `FitsReader.FitsMovabsHdu` + `FitsReader.FitsReadKey` (EXTNAME / HDUNAME) | `CanvassDesktop.cs:361–379` | Domain logic (HDU enumeration) inlined in UI class |
| A13 | `FitsReader.FitsMovabsHdu(fptr, 1, ...)` reset to HDU 1 | `CanvassDesktop.cs:381` | |
| A14 | `CanvassDesktop` mutates HDU dropdown via `transform.Find("HeaderTitle_container/Hdu_container/Hdu_dropdown")` | `CanvassDesktop.cs:382–394` | Scene-hierarchy hardwiring → diagram should show this as an internal self-call |
| A15 | `CanvassDesktop.UpdateHeaderFromFits(fptr)` | `CanvassDesktop.cs:405` | Further DLL calls inside (header keyword reads) — collapse into one message |
| A16 | `CanvassDesktop → FitsReader.FitsCloseFile(fptr, out status)` | `CanvassDesktop.cs:407` | Manual lifetime management of unmanaged handle |
| A17 | `CanvassDesktop.IsLoadable()` validates axis count | `CanvassDesktop.cs:410, 431` | Inline domain rule |
| A18 | **[GUI WALK]** Loading button becomes interactable; subset selector appears | `CanvassDesktop.cs:412–415` | Walkthrough: which panel updates visibly |

**At end of Phase A:** the FITS file has been opened, its metadata inspected, the native handle closed, and the UI is primed. **No cube exists yet.** User must click a second button.

---

## Phase B — User confirms load (cube becomes visible)

| # | Message | Source citation | Notes / smell |
|---|---|---|---|
| B1 | **[GUI WALK]** User clicks the **Load** button in the File tab | scene asset | Walkthrough: confirm label & location |
| B2 | `LoadButton.onClick → CanvassDesktop.LoadFileFromFileSystem()` | `CanvassDesktop.cs:927` | Inspector-wired |
| B3 | `CanvassDesktop.StartCoroutine(LoadCubeCoroutine(_imagePath, _maskPath, hdu))` | `CanvassDesktop.cs:929` | Unity coroutine — diagram needs `activate` bar to show async lifetime |
| B4 | `LoadCubeCoroutine`: shows progress bar, `CheckMemSpaceForCubes(...)` | `CanvassDesktop.cs:1015–1022` | Domain check (RAM vs cube size) in UI class |
| B5 | `_volumeCommandController.RemoveDataSet(firstActiveRenderer)` (if previous cube) | `CanvassDesktop.cs:1050` | Singleton coupling |
| B6 | `FindObjectOfType<VolumeInputController>()` and toggle on/off to "reset" | `CanvassDesktop.cs:1054–1056` | **Smell:** state reset via SetActive(false/true) is fragile |
| B7 | `_volumeCommandController.DisablePaintMode() / endThresholdEditing() / endZAxisEditing()` | `CanvassDesktop.cs:1058–1060` | |
| B8 | `firstActiveRenderer.Data.CleanUp(...)` ; `firstActiveRenderer.Mask?.CleanUp(false)` ; `Destroy(firstActiveRenderer)` | `CanvassDesktop.cs:1068–1070` | UI class managing native memory cleanup |
| B9 | `Instantiate(cubeprefab, ...)` → `newCube` (GameObject) | `CanvassDesktop.cs:1078` | Prefab instantiation |
| B10 | `newCube.GetComponent<VolumeDataSetRenderer>()` → `volDSRender` | `CanvassDesktop.cs:1085` | |
| B11 | Direct field assignment: `volDSRender.subsetBounds = ...; .FileName = _imagePath; .MaskFileName = _maskPath; .SelectedHdu = ...; .loadText = ...; .progressBar = ...; .CubeDepthAxis = ...; .FileChanged = false` | `CanvassDesktop.cs:1086–1094` | **Smell:** no encapsulation, no domain command — UI pokes the renderer's public fields |
| B12 | `_volumeCommandController.AddDataSet(volDSRender)` | `CanvassDesktop.cs:1113` | |
| B13 | `CanvassDesktop.StartCoroutine(volDSRender._startFunc())` | `CanvassDesktop.cs:1114` | Fires the renderer's own init coroutine |
| B14 | `VolumeDataSetRenderer._startFunc()` runs — texture upload, GPU resources | `VolumeDataSetRenderer.cs:358` | Internal to renderer (sub-team 3 owns the detail). Diagram should treat as one message and one return |
| B15 | Polling loop in caller: `while (!volDSRender.started) yield return WaitForSeconds(0.1f)` | `CanvassDesktop.cs:1116–1119` | **Smell:** busy-wait via yield — async-as-pseudo-sync |
| B16 | `volDSRender.started = true` set when renderer finishes init | `VolumeDataSetRenderer.cs:541` | Signals visibility |
| B17 | **[GUI WALK]** Cube appears in the scene | `volDSRender` GameObject is now active and rendered | Walkthrough: confirm any animation, loading text disappearance |
| B18 | `CanvassDesktop.postLoadFileFileSystem()` | `CanvassDesktop.cs:935, called at :1131` | Final UI sync |
| B19 | Inside `postLoadFileFileSystem`: hides `LoadingText`, `progressBar`, `WelcomeMenu`; enables Rendering/Stats/Sources/Paint tab buttons; auto-invokes `Stats_Button.onClick` | `CanvassDesktop.cs:962–976` | UI cascade — collapse to one self-message in diagram |
| B20 | **[GUI WALK]** Welcome menu disappears, tabs come alive, Stats tab is shown | — | Walkthrough confirms |

**At end of Phase B:** the cube is rendered and the desktop UI is in its "loaded" state.

---

## Smell summary (feeds the SOLID/GRASP audit + CK deltas)

| # | Smell | Lines | Principle violated |
|---|---|---|---|
| S1 | Direct `[DllImport]` call from UI layer (`FitsOpenFile`, `FitsGetHduCount`, `FitsMovabsHdu`, `FitsReadKey`, `FitsCloseFile`) | `CanvassDesktop.cs:349,358,363,365,369,381,407` | Dependency Inversion; Section 4.2 ACL rule (domain code must not depend on native types transitively) |
| S2 | God class — file I/O + FITS axis logic + UI mutation + coroutine lifecycle + memory checks + native handle management all in one `MonoBehaviour` | `CanvassDesktop.cs` (whole file) | Single Responsibility |
| S3 | `transform.Find("A/B/C/...").GetComponent<T>()` chains hardwired to scene hierarchy | `CanvassDesktop.cs:163–191, 382–394, 402, 412–423, 945–976` | Open/Closed; testability (cannot construct outside Unity scene) |
| S4 | `FindObjectOfType<>` singletons | `CanvassDesktop.cs:157–159, 1054, 1105` | Dependency Inversion; Information Hiding |
| S5 | Public mutable field assignment on `VolumeDataSetRenderer` | `CanvassDesktop.cs:1086–1094` | Encapsulation; Command/Query separation |
| S6 | Busy-wait polling on `started` flag | `CanvassDesktop.cs:1116–1119` | Async hygiene |
| S7 | Inspector-wired button handlers (no in-code subscription) | `BrowseImageFile`, `LoadFileFromFileSystem` | Testability — cannot exercise the flow without loading the Unity scene |
| S8 | Unmanaged `fptr` lifetime spans many UI mutations between open (line 349) and close (line 407) | `CanvassDesktop.cs:349–407` | RAII / resource ownership unclear |

---

## How this becomes the Mermaid diagram

Convert each row above to a `sequenceDiagram` message:

- Phase A and B can be drawn as one continuous diagram OR split with a `Note over User: clicks Load button` separator between them.
- Use `activate` / `deactivate` bars on `CanvassDesktop` to show that the dialog callback (A6) and the coroutine (B3) re-enter the same lifeline asynchronously.
- The DLL boundary (A9, and the implicit calls inside A11–A16) is the visual centrepiece — a thick arrow into `idavie_native` is what the "after" diagram replaces with a single `serviceGateway.openFits(...)` message.
- Collapse the `UpdateHeaderFromFits` internal DLL calls into one self-call on `CanvassDesktop` labelled "read header keywords via FitsReader" — keeps the diagram readable without losing fidelity.

---

## What's missing before this can become the final diagram

1. **GUI walkthrough notes** to fill the `[GUI WALK]` rows (A1, A5, A18, B1, B17, B20). Capture: exact button labels, menu paths, panels that appear/disappear, screenshots of key moments.
2. Decision: single combined diagram (Phase A + B) or two diagrams. Recommend one combined diagram with a `Note` separator — keeps the "two-click" reality of the current UX visible, which itself is part of the proposal's argument.
3. Once Mermaid is drafted, save as `before-sequence.md` in this folder. This trace doc stays as `before-trace.md` for the panel evidence.
