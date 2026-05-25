# WE1-6 — CK + ISO 25010 Delta Worksheet (File Tab & Debug Tab)

**Status:** Design proposal — all "after" figures are projected, not measured.  
**Before source:** `SK_BNCH.md` (Understand static analysis export, Day 2 baseline).  
**After sources:** `after-class-diagram.puml`, `after-dependency-graph.puml`, `after-dsm.md`,  
`uml-diagrams/after-debug-sequence-diagram.puml`, `refactor.md §3.2`.  
**Feeds:** T4 (architecture report §Metrics), T7 (integration & metrics), Pitch slot 3.

---

## 0. How to Read This Document

`CanvassDesktop` is the single God class that both worked examples extract from.
Its measured metrics (WMC = 63, CBO = 47, RFC = 118, LCOM = 0.955) are **partitioned once**
into three non-overlapping concerns:

| Concern | What it covers | "Before" row |
|---|---|---|
| **File tab** | The 16 methods that handle file/mask browse, FITS header read, subset validation, and cube load | §2 |
| **Debug tab** | The 40 `Debug.Log` call-sites embedded in existing methods — no dedicated class | §3 |
| **Shared residual** | All remaining CanvassDesktop methods: rendering, stats, sources, paint, lifecycle, navigation | §4 |

The same number does **not** appear in more than one "before" row. WMC = 63 is
explained by 16 (file) + 0 (debug) + 47 (shared) = 63.

**Note on class-level metrics.** WMC, RFC, and LCOM are computed by Understand at class level.
WMC in this report = method count (uniform weight 1 per Understand methodology, confirmed by
the LCOM section of `SK_BNCH.md`: "63 methods, 67 fields, 189 field-method accesses").
CBO partitions are derived from source inspection and marked **[derived]**.
RFC partitions are derived proportionally and marked **[derived]**.
LCOM cannot be cleanly split — it is reported once in §4 for the full class.

---

## 1. Before-State Partition of CanvassDesktop

Derived by counting methods per tab grouping and mapping the 47 CBO types to the
concern that primarily introduces them.

| Concern | WMC (method count) | CBO types attributed [derived] | RFC [derived] | LCOM | DIT |
|---|:---:|:---:|:---:|:---:|:---:|
| File tab (16 methods) | **16** | **~8** | **~52** | — | — |
| Debug tab (0 dedicated methods) | **0** | **0** | **0** | — | — |
| Shared residual (47 methods) | **47** | **~39** | **~66** | 0.955 | 1 |
| **CanvassDesktop total (measured)** | **63** | **47** | **118** | **0.955** | **1** |

**File tab CBO types (~8):** Types used exclusively or primarily in the 16 file tab methods:
`FitsReader`, `VolumeDataSetManager`, `IntPtr` (P/Invoke handle), `StringBuilder`
(header text), `SystemInfo` (`CheckMemSpaceForCubes`), `FeatureMenuController`
(`postLoadFileFileSystem`), `VolumePlayer` (`postLoadFileFileSystem`), `MenuBarBehaviour`
(`postLoadFileFileSystem`).

**Debug CBO = 0:** `Debug.Log` calls use `UnityEngine.Debug`, a member of the `UnityEngine`
namespace already counted in the shared CBO. The logging concern introduces no additional type.

**Shared residual CBO (~39):** All remaining types: `VolumeDataSetRenderer`,
`VolumeCommandController`, `VolumeInputController`, `HistogramHelper`, `DataAnalysis`,
`StandaloneFileBrowser`, `FeatureMapping`, `FeatureSetManager`, `FeatureTable`,
`SourceMappingOptions`, `ColorMapUtils`, `Config`, all 13 Unity/TMPro UI types
(`TMP_Dropdown`, `TMP_InputField`, `Toggle`, `Slider`, `Button`, `Sprite`, `TextMeshProUGUI`,
`GameObject`, `Transform`, `Coroutine`, `PlayerPrefs`, `OxyPlot..*`, `Marshal`),
all 4 Valve.VR types (`SteamVR_Init`, `OpenVR`, etc.), and remaining System types.

---

## 2. File Tab — Old vs New

### 2.1 Old File Tab

The 16 methods below are the WE1 scope inside `CanvassDesktop`. Line numbers are from
the current source; approximate cyclomatic complexity (CC) per method is estimated from
branch count in the source.

| Method | Line | CC [est.] | Concern | Code smell |
|---|:---:|:---:|---|---|
| `BrowseImageFile` | 306 | 1 | UI wiring → SFB async callback | Mixed UI event + I/O dispatch in one method |
| `_browseImageFile` | 329 | 4 | FITS open, HDU parse, header read, subset reset | 5 `FitsReader.*` P/Invoke calls in business logic |
| `IsLoadable` | 431 | 5 | NAXIS validation, axis-size extraction, Z-dropdown populate | Axis maths + UI manipulation in one method |
| `UpdateHeaderFromFits` | 538 | 2 | FITS header dump to `TextMeshProUGUI` scroll-view | `IntPtr` (native handle) crosses the UI layer |
| `onSubsetToggleSelected` | 575 | 2 | Show/hide subset row, focus first field | Pure UI logic sitting inside God class |
| `setSubsetBounds` | 595 | 1 | Reset all 6 subset input fields to axis maxima | Direct `TMP_InputField.text` writes in 6 places |
| `updateSubsetZMax` | 614 | 2 | Clamp Z-max when Z-axis dropdown changes | Direct field write + UI sync in same method |
| `checkSubsetBounds` | 641 | 7 | Validate + clamp all 6 subset input fields | 18 `Debug.Log` calls; re-validates all 6 fields on any single field edit |
| `BrowseMaskFile` | 807 | 1 | Wires mask button to SFB async callback | Same mixed-concern pattern as `BrowseImageFile` |
| `_browseMaskFile` | 829 | 3 | Reads mask headers, validates axis match against image | Axis comparison logic intermixed with UI updates |
| `CheckImgMaskAxisSize` | 905 | 2 | **Dead code** — axis comparison duplicate | Duplicate of `_browseMaskFile` logic; no callers found |
| `LoadFileFromFileSystem` | 927 | 1 | Entry point — starts `LoadCubeCoroutine` | Couples UI button directly to coroutine handle |
| `postLoadFileFileSystem` | 935 | 2 | Post-load tab enables, dropdown populates, event subscriptions | 7 unrelated UI side-effects triggered from one method |
| `CheckMemSpaceForCubes` | 995 | 2 | Calculates in-memory size vs `SystemInfo.systemMemorySize` | Checks *client* RAM for what is a server-owned resource |
| `LoadCubeCoroutine` | 1015 | 5 | Scene teardown, prefab instantiation, coroutine wait, post-load | File I/O + scene management + UI updates + coroutine lifecycle in one method |
| `ChangeHduSelection` | 1435 | 2 | Re-opens FITS file on each HDU dropdown change | Re-opens file from disk on every switch; `IntPtr` lifetime not scoped |

**File tab WMC = 16, RFC ≈ 52** (16 methods + ~36 distinct external method calls: 9 FitsReader
P/Invoke, 1 SFB async open, ~5 VolumeDataSetRenderer property writes, ~3 VolumeCommandController
calls, ~10 Unity built-in calls, ~2 PlayerPrefs, ~3 post-load populate helpers, ~3 others).

**File tab thresholds as a virtual standalone class (Orchestrator role):**

| Metric | Value [derived] | Threshold | Status |
|---|:---:|:---:|:---:|
| WMC | 16 | ≤ 40 (O) | ✅ |
| CBO | ~8 | ≤ 25 (O) | ✅ |
| RFC | ~52 | ≤ 50 | 🔴 +2 |

The RFC barely exceeds the threshold even for the extracted slice — confirming that
`FitsServiceAdapter` (which absorbs 9 of those 36 external calls) is the critical
interface to introduce.

### 2.2 New File Tab [projected]

| Type | Layer | WMC | DIT | NOC | CBO | RFC | LCOM | Role threshold |
|---|---|:---:|:---:|:---:|:---:|:---:|:---:|---|
| `FileTabView` | View (Unity) | **8** | 1 | 0 | **5** | **18** | **0.05** | WMC≤40 / CBO≤25 ✅ |
| `FileTabViewModel` | ViewModel (pure C#) | **12** | 0 | 0 | **5** | **22** | **0.20** | WMC≤20 / CBO≤14 ✅ |
| `SubsetBoundsViewModel` | ViewModel (pure C#) | **8** | 0 | 0 | **1** | **11** | **0.15** | WMC≤20 / CBO≤14 ✅ |
| `FitsServiceAdapter` | Adapter (Unity asm) | **10** | 0 | 0 | **6** | **20** | **0.15** | WMC≤40 / CBO≤25 ✅ |
| `StandaloneFileDialogAdapter` | Adapter (Unity asm) | **4** | 1 | 0 | **4** | **8** | **0.05** | WMC≤40 / CBO≤25 ✅ |
| `VolumeServiceAdapter` | Adapter (Unity asm) | **6** | 1 | 0 | **7** | **13** | **0.15** | WMC≤40 / CBO≤25 ✅ |
| **Total / max** | | **48 total / 12 max** | | | **28 total / 7 max** | **92 total / 22 max** | **0.20 max** | |

**All 6 types: 0 CK violations.**

**Method derivation:**
- `FileTabView` (WMC=8): `BindTo`, `OnBrowseImageClicked`, `OnBrowseMaskClicked`, `OnLoadClicked`, `OnHduDropdownChanged`, `OnSubsetToggleChanged` (6 from `after-class-diagram.puml`) + `OnEnable`, `OnDisable` (Unity binding lifecycle). Every method touches `_vm : IFileTabViewModel` → LCOM ≈ 0.05.
- `FileTabViewModel` (WMC=12): `BrowseImageAsync`, `BrowseMaskAsync`, `LoadAsync`, `MaskAxesMatchImage`, `PopulateZAxisOptions`, `UpdateZAxisMax` + `IsLoadable` computed getter + constructor + `OnPropertyChanged` + 3 observable property setters. Matches "WMC ≈ 12" annotation in `after-dependency-graph.puml`. CBO=5: `IFitsService`, `IFileDialogService`, `IVolumeService`, `SubsetBoundsViewModel`, `FitsFileInfo`.
- `SubsetBoundsViewModel` (WMC=8): `ResetToAxisMaxima`, `UpdateZAxisMax`, `ToDto` + 5 clamping property setters. Replaces `checkSubsetBounds` (70 lines, 18 `Debug.Log` calls), `setSubsetBounds`, and `updateSubsetZMax`. CBO=1 (`SubsetBounds` DTO). Matches "WMC ≈ 8, CBO ≈ 1" annotation.
- `FitsServiceAdapter` (WMC=10): `OpenImageAsync`, `OpenMaskAsync`, `GetHeaderTextAsync` + constructor + 6 private helpers (`FitsOpen`, `ReadHduList`, `ReadAxisSizes`, `ExtractHeaderText`, `FitsClose`, `ThrowIfFitsError`). Absorbs all 9 `FitsReader` P/Invoke calls from `CanvassDesktop`. No `UnityEngine` import needed. CBO=6: `FitsReader`, `FitsFileInfo`, `HduInfo`, `Task`, `CancellationToken`, `IFitsService`.
- `StandaloneFileDialogAdapter` (WMC=4): `PickFileAsync` + constructor + `OnFileSelected` + `PersistLastDirectory`. CBO=4: `StandaloneFileBrowser`, `PlayerPrefs`, `Task<string?>`, `IFileDialogService`.
- `VolumeServiceAdapter` (WMC=6): `LoadCubeAsync`, `IsCubeLoaded` getter + constructor + `RunLoadCoroutine` + `OnLoadComplete` + `MapRequest`. CBO=7: `VolumeCommandController`, `UnityEngine`, `LoadCubeRequest`, `IProgress<float>`, `CancellationToken`, `Task`, `IVolumeService`.

### 2.3 File Tab Delta

| Metric | Old (file tab slice) | New (worst-case class) | Δ | Threshold met after? |
|---|:---:|:---:|:---:|:---:|
| WMC (max per class) | 16 *(slice, not full class)* | **12** (`FileTabViewModel`) | −4 per class | ✅ ≤ 20 (VM) |
| CBO (max per class) | ~8 *(exclusive types)* | **7** (`VolumeServiceAdapter`) | −1 per class | ✅ ≤ 25 (Adapter) |
| RFC (max per class) | ~52 *(estimated slice total)* | **22** (`FileTabViewModel`) | −30 per class | ✅ ≤ 50 |
| LCOM (max per class) | *(class-level — see §4)* | **0.20** (`FileTabViewModel`) | — | ✅ ≤ 0.50 |
| Circular dependencies | 2 (`↔ VolumeCommandController`, `↔ DesktopPaintController`) | **0** | −2 | ✅ |
| `UnityEngine` in domain code | Yes (`CanvassDesktop`) | **No** (ViewModel + DTO layers) | − | ✅ |
| Interfaces backing file tab APIs | 0 | **4** (`IFileTabViewModel`, `IFitsService`, `IFileDialogService`, `IVolumeService`) | +4 | ✅ |
| Dead methods | 1 (`CheckImgMaskAxisSize`) | **0** | −1 | ✅ |
| File tab testable without Unity runner | 0 / 16 methods | **5 / 6 types** (`FileTabViewModel`, `SubsetBoundsViewModel`, `FitsServiceAdapter` + their tests) | +5 types | ✅ |

---

## 3. Debug Tab — Old vs New

### 3.1 Old Debug Tab

There is no Debug tab class in the current codebase. The before-state is an absence of
design: **40 `Debug.Log` / `Debug.LogWarning` / `Debug.LogError` call-sites** embedded
inline in existing `CanvassDesktop` methods, with no structure, no UI panel, and no
interception point for testing.

**WMC = 0** (no dedicated methods).  
**CBO = 0** (no additional types — `UnityEngine.Debug` is already counted in the shared CBO).

**Call-site map (all 40 sites):**

| Method (line range) | Sites | Level(s) | Representative message |
|---|:---:|:---:|---|
| `_browseImageFile` (349–407) | 2 | Log | `"Fits open failure... code #" + status`; `"Could not find EXTNAME or HDUNAME in HDU " + i` |
| `IsLoadable` (431–537) | 1 | Log | `"The list has " + count + " items, dropdown index " + idx` |
| `checkSubsetBounds` — XMax (641–667) | 4 | Log | min violation; max violation; lower-bound violation; not-a-number |
| `checkSubsetBounds` — YMax (668–694) | 4 | Log | same 4 conditions for Y axis max |
| `checkSubsetBounds` — ZMax (695–720) | 4 | Log | same 4 conditions for Z axis max |
| `checkSubsetBounds` — XMin (721–746) | 4 | Log | same 4 conditions for X axis min |
| `checkSubsetBounds` — YMin (747–772) | 4 | Log | same 4 conditions for Y axis min |
| `checkSubsetBounds` — ZMin (773–798) | 4 | Log | same 4 conditions for Z axis min |
| `_browseMaskFile` (829–904) | 1 | Log | `"Fits open failure... code #" + status` |
| `CheckMemSpaceForCubes` (995–1014) | 2 | Log, LogWarning | size OK message; RAM exceeded warning |
| `LoadCubeCoroutine` (1015–1133) | 4 | Log | loading start; replacing cube; instantiating prefab; complete message |
| `_browseMappingFile` (1322–1434) | 1 | LogError | `"Error while loading mapping file: " + ex.Message` |
| `ChangeHduSelection` (1435–1465) | 1 | Log | `"Fits open failure... code #" + status` |
| `LoadSourcesFile` (1579–1612) | 1 | Log | `"Minimal source mappings not set!"` |
| `SetMaxMinPercentile` (1767–1813) | 3 | Log, LogError ×2 | percentile success; histogram error; data error |
| **Total** | **40** | | |

24 of 40 sites (60 %) are in `checkSubsetBounds` alone, repeating the same 4-condition
pattern for each of 6 subset-bounds fields. This inflates `checkSubsetBounds` from a
logical CC of ~4 (the 4 branching conditions) to an observed CC of ~7 because each
condition triggers an additional `Debug.Log` statement with string concatenation.

**Before-state quality gaps:**

| Gap | Detail |
|---|---|
| No structured format | Raw string concatenation — no `Level` enum, no `Source` tag, no `Timestamp` |
| Log output invisible to end user | Unity console only; no in-app panel |
| No interception point for tests | Cannot assert on a `Debug.Log` call in a unit test without Unity runner + console parsing |
| `UnityEngine.Debug` is a hidden transitive dependency | Every method that calls `Debug.Log` is coupled to `UnityEngine` in its static call graph, even if it imports no other Unity types |
| 24 / 40 sites produce structurally identical messages | The 4-condition repeat across 6 fields is a template, not 24 independent decisions — a single parameterised method would reduce them to 1 call per field |

### 3.2 New Debug Tab [projected]

| Type | Layer | WMC | DIT | NOC | CBO | RFC | LCOM | Role threshold |
|---|---|:---:|:---:|:---:|:---:|:---:|:---:|---|
| `DebugTabView` | View (Unity) | **5** | 1 | 0 | **3** | **10** | **0.05** | WMC≤40 / CBO≤25 ✅ |
| `DebugTabViewModel` | ViewModel (pure C#) | **7** | 0 | 0 | **3** | **12** | **0.10** | WMC≤20 / CBO≤14 ✅ |
| `UnityLogStreamAdapter` | Adapter (Unity asm) | **5** | 0 | 0 | **4** | **10** | **0.10** | WMC≤40 / CBO≤25 ✅ |
| **Total / max** | | **17 total / 7 max** | | | **10 total / 4 max** | **32 total / 12 max** | **0.10 max** | |

**All 3 types: 0 CK violations.**

**Interfaces introduced:**

| Interface | Methods | Purpose |
|---|:---:|---|
| `ILogStream` | 3 (`Subscribe`, `Unsubscribe`, `Publish(level, source, message)`) | Log producer contract; the only interface any log caller needs to know |
| `ILogObserver` | 1 (`OnNext(LogEntry)`) | Observer contract; `DebugTabViewModel` implements this |
| `IDebugTabViewModel` | 4 (`LogEntries`, `ClearCommand`, `AutoScrollEnabled`, `FilterLevel`) | View binding contract |

**`LogEntry` DTO** (replaces raw string concatenation):
`Level : LogLevel` (enum: Debug / Info / Warning / Error),
`Source : string` (e.g. `"FitsServiceAdapter"`),
`Message : string`,
`Timestamp : DateTimeOffset`.

**Method derivation:**
- `DebugTabView` (WMC=5): `BindTo`, `OnEnable`, `OnDisable` + `AppendEntryToScrollView` (private, called on binding update) + `ScrollToBottom`. All 5 touch `_vm : IDebugTabViewModel` → LCOM ≈ 0.05. CBO=3: `IDebugTabViewModel`, `UnityEngine` (UI element), `LogEntry`.
- `DebugTabViewModel` (WMC=7): `OnNext(LogEntry)` (implements `ILogObserver`) + `LogEntries` getter + `AutoScrollEnabled` getter/setter + `FilterLevel` getter/setter + `ClearCommand.Execute` + constructor. CBO=3: `ILogStream`, `ILogObserver` (self-implements), `LogEntry`. **Zero `UnityEngine` imports.** Consistent with "WMC ≈ 5, CBO ≈ 2" in `refactor.md §9` (+2 for filter setter and property-changed notification).
- `UnityLogStreamAdapter` (WMC=5): `Subscribe`, `Unsubscribe`, `Publish` (3 public, implementing `ILogStream`) + `ForwardToUnityConsole` (private — maintains parity with Unity IDE console) + constructor. CBO=4: `ILogStream`, `ILogObserver`, `LogEntry`, `UnityEngine.Debug`. **This is the only class that calls `UnityEngine.Debug.*`.** All 40 former call-sites in `CanvassDesktop` become `_logStream.Publish(level, source, message)` — compiled against `ILogStream` only.

### 3.3 Debug Tab Delta

| Metric | Old | New | Δ |
|---|:---:|:---:|:---:|
| Dedicated debug class | 0 | **3** (View, ViewModel, Adapter) | +3 |
| WMC (dedicated debug class) | 0 | **7** (`DebugTabViewModel`) | +7 *(new focused class)* |
| CBO (dedicated debug class) | 0 | **4** (`UnityLogStreamAdapter`, max) | +4 *(new isolated class)* |
| CK violations in debug types | 0 (no class) | **0** (all within threshold) | 0 |
| Unstructured `Debug.Log` call-sites | **40** | **0** (replaced by `ILogStream.Publish`) | **−40** |
| Classes importing `UnityEngine.Debug` | ≥ 1 (`CanvassDesktop` + any other) | **1** (`UnityLogStreamAdapter` only) | **−N+1** |
| `checkSubsetBounds` CC (debug contribution) | ~7 (18 log branches) | **~2** (`SubsetBoundsViewModel` clamping setters — no logging) | **−5** |
| Interfaces backing log stream | 0 | **3** (`ILogStream`, `ILogObserver`, `IDebugTabViewModel`) | **+3** |
| Log call-sites testable without Unity runner | **0** | **40** (all behind `ILogStream.Publish`) | **+40** |
| Structured log fields (level, source, timestamp) | **0** | **4** (`LogEntry` DTO) | **+4** |

---

## 4. Shared Residual — Old vs New

### 4.1 Old Shared (47 Methods)

The 47 methods left after removing the 16 file tab methods. These cover the remaining
tab concerns and the class lifecycle. They share CanvassDesktop's single LCOM and
DIT measurement since those are class-level only.

| Sub-group | Methods | Concerns |
|---|:---:|---|
| Lifecycle & helpers | 13 | `Awake`, `Start`, `OnDestroy`, `Update`, `OnGUI`, `ShowGUI`, `CheckCubesDataSet`, `GetFirstActiveRenderer`, `SetInputIndex`, `DismissFileLoad`, `Exit`, `getActiveDataSet`, `getActiveMaskSet` |
| Rendering tab | 14 | `PopulateRestfreqencyDropdown`, `OnRatioDropdownValueChanged`, `OnRestFrequency*` ×6, `SetRestFrequency*` ×3, `populateColorMapDropdown`, `ChangeColorMap`, `UpdateThresholdMin`, `UpdateThresholdMax`, `ResetThresholds` |
| Stats tab | 8 | `populateStatsValue`, `UpdateUI`, `UpdateSigma`, `RestoreDefaults`, `UpdateScale`, `SetMaxMinPercentile`, `UpdateScaleMin`, `UpdateScaleMax` |
| Sources tab | 10 | `BrowseSourcesFile`, `_browseSourcesFile`, `BrowseMappingFile`, `_browseMappingFile`, `SaveMappingFile`, `_saveMappingFile`, `ChangeSourceMapping`, `AreMappingsIncompatible`, `AreMinimalMappingsSet`, `LoadSourcesFile` |
| Paint tab | 2 | `paintTabSelected`, `paintTabLeft` |
| **Total** | **47** | |

**Shared residual metrics [derived]:**

| Metric | Value | Threshold (Orchestrator) | Status |
|---|:---:|:---:|:---:|
| WMC | **47** | ≤ 40 | 🔴 +7 |
| CBO | **~39** | ≤ 25 | 🔴 +14 |
| RFC | **~66** | ≤ 50 | 🔴 +16 |
| LCOM (full class) | **0.955** | ≤ 0.50 | 🔴 |
| DIT | 1 | ≤ 4 | ✅ |

The shared residual still violates all four thresholds even after the file tab
extraction, confirming that the rendering, stats, and sources tab methods need
the same MVVM extraction pattern (outside the scope of WE1 and WE2, but
demonstrated by the same design).

### 4.2 New Shared (CanvassDesktop Shell) [projected]

After extracting ALL tabs (file in WE1, debug in WE2, rendering/stats/sources/paint
by the same pattern), `CanvassDesktop` is reduced to a **composition root**:

| Method | Concern |
|---|---|
| `Awake` | Construct adapters (`FitsServiceAdapter`, `UnityLogStreamAdapter`, etc.) |
| `Start` | Construct ViewModels, call `BindTo` on each View |
| `OnDestroy` | Unsubscribe all bindings, dispose adapters |
| `WireFileTab` (private) | Instantiate `FileTabViewModel`, pass to `FileTabView.BindTo` |
| `WireDebugTab` (private) | Instantiate `DebugTabViewModel`, pass to `DebugTabView.BindTo` |
| `WireRenderingTab` (private) | Same pattern for rendering (out of WE1/WE2 scope) |
| `WireSourcesTab` (private) | Same pattern for sources (out of WE1/WE2 scope) |
| `TearDown` (private) | Null out all references |

| Metric | Value [projected] | Threshold (Orchestrator) | Status |
|---|:---:|:---:|:---:|
| WMC | **8** | ≤ 40 | ✅ −55 |
| CBO | **4** | ≤ 25 | ✅ −43 |
| RFC | **12** | ≤ 50 | ✅ −106 |
| LCOM | **0.10** | ≤ 0.50 | ✅ −0.855 |
| DIT | 1 | ≤ 4 | ✅ |

CBO = 4 reflects composition-root scope (`FileTabView`, `DebugTabView`,
`FileTabViewModel`, `DebugTabViewModel`). The full extraction adds `RenderingTabView`,
`StatsTabView`, `SourcesTabView` etc. bringing CBO to ~8, still well within threshold.

### 4.3 Shared Delta

| Metric | Old shared | New shared (shell) | Δ |
|---|:---:|:---:|:---:|
| WMC | 47 | **8** | **−39** |
| CBO | ~39 | **4–8** | **−31 to −35** |
| RFC | ~66 | **12** | **−54** |
| LCOM | 0.955 | **0.10** | **−0.855** |
| `FindObjectOfType<>` singleton hunts | 3 | **0** | **−3** |
| Methods with per-frame `Update()` polling | 1 (`Update` syncs thresholds + tab key) | **0** (rendering ViewModel subscribes to renderer events) | **−1** |
| CK violations | 4 (WMC/CBO/RFC/LCOM) | **0** | **−4** |

---

## 5. ISO 25010 Maintainability Mapping

### 5.1 File Tab

| Sub-char | Before rating | After [proj] | Key evidence |
|---|:---:|:---:|---|
| **Modularity** | Poor | Good | `FileTabViewModel` CBO = 5 (interfaces only); both circular cycles eliminated. A `FitsReader` ABI change touches only `FitsServiceAdapter`. |
| **Reusability** | Poor | Acceptable | `IFitsService`, `IFileDialogService`, `IVolumeService`, `IFileTabViewModel` are explicit swap seams. Future remote-server mode replaces adapters without touching ViewModels. |
| **Analysability** | Poor | Good | RFC 52 → 22 (max per class). Tracing "why did the load button stay disabled?" requires reading 12 methods in `FileTabViewModel`, not scanning 1 899 lines. |
| **Modifiability** | Poor | Good | CBO 8 exclusive → 7 max per new class. `ChangeHduSelection` re-opening the file on each switch is replaced by `IFitsService.GetHeaderTextAsync(handle, hduIndex)` — no reopen, handle is server-resident. |
| **Testability** | Very Poor | Good | `FileTabViewModel` and `SubsetBoundsViewModel` are `object` subclasses. NUnit test: `new FileTabViewModel(mockFits, mockDialog, mockVolume)`. `checkSubsetBounds` (70 lines + 18 log calls + Unity input fields) becomes property setters on `SubsetBoundsViewModel` — `Assert.Equal(expected, vm.Subset.XMax)`. |

**Testability blockers resolved (WE1):**

| Blocker | Resolved by |
|---|---|
| `MonoBehaviour` ancestry — requires Unity test runner | `FileTabViewModel` / `SubsetBoundsViewModel` have `object` as base |
| `FindObjectOfType<VolumeCommandController>()` requires full scene | Constructor-injected `IVolumeService` |
| `FitsReader` P/Invoke requires native `.dll` | Isolated behind `IFitsService`; test double returns `FitsFileInfo` DTOs |
| 25+ `transform.Find(…).GetComponent<T>()` chains | `FileTabView` owns all hierarchy traversal; ViewModel never touches `Transform` |
| `StandaloneFileBrowser` async callbacks not mockable | `IFileDialogService.PickFileAsync()` — stubbed with `Task.FromResult("file.fits")` |
| `CheckMemSpaceForCubes` tests client RAM | Moved to `IVolumeService.CanFit()` — adapter answers its own capacity question |

### 5.2 Debug Tab

| Sub-char | Before rating | After [proj] | Key evidence |
|---|:---:|:---:|---|
| **Modularity** | Very Poor | Good | 40 scattered sites → 0. Adding a new log source (`FitsServiceAdapter`, `VolumeServiceAdapter`) requires one `_logStream.Publish()` call with no change to `DebugTabViewModel`. Adding a second consumer (e.g. file logger) requires only a new `ILogObserver` implementation. |
| **Reusability** | Very Poor | Good | `ILogStream` / `ILogObserver` have no Unity dependencies. `DebugTabViewModel` can be embedded in any future shell (CLI, VR overlay) that provides an `ILogStream`. |
| **Analysability** | Poor | Good | `checkSubsetBounds` CC drops from ~7 to ~2. Remaining real errors (`FitsOpen` failure, mapping load error) surface as `LogEntry { Level=Error, Source="FitsServiceAdapter", Message=..., Timestamp=... }` — no string parsing required to read them. |
| **Modifiability** | Poor | Good | Changing the debug panel from a Unity ScrollView to a UI Toolkit `ListView` requires modifying only `DebugTabView`. `DebugTabViewModel` exposes `IReadOnlyList<LogEntry>` with no UI type in its public surface. |
| **Testability** | Very Poor | Good | `DebugTabViewModel.OnNext(entry)` is a pure method: `vm.OnNext(new LogEntry(LogLevel.Error, "FitsOpen", "status=4", DateTimeOffset.UtcNow)); Assert.Single(vm.LogEntries);` — zero Unity imports needed. 40 log sites are now behind `ILogStream.Publish()`, assertable with a capturing `ILogObserver` stub. |

**Testability blockers resolved (WE2):**

| Blocker | Resolved by |
|---|---|
| `UnityEngine.Debug` is static — cannot be mocked or redirected | All log calls go through `ILogStream.Publish()`; `UnityLogStreamAdapter` is the only `UnityEngine.Debug` caller |
| 24 log calls in `checkSubsetBounds` — no assertion target | `SubsetBoundsViewModel` property setters replace inline validation+logging; test asserts on property value |
| No end-user visibility of log output | `DebugTabView` scrolls `LogEntry` list; smoke-testable by running the app |
| No log level or source filtering | `IDebugTabViewModel.FilterLevel` property filters by `LogLevel` enum in ViewModel without touching View |

---

## 6. Aggregate Violation Summary

Counts across CanvassDesktop before vs all WE1 + WE2 successor types combined.

| Rule | Before | After | Δ |
|---|:---:|:---:|:---:|
| CK violations (WMC / CBO / RFC / LCOM) | **4** | **0** | −4 |
| Circular dependency cycles | **2** | **0** | −2 |
| Classes with `UnityEngine` in domain / ViewModel code | **1** | **0** | −1 |
| Public API boundaries backed by interfaces | **0** | **7** | +7 |
| Dead methods | **1** | **0** | −1 |
| Unstructured `Debug.Log` call-sites | **40** | **0** | −40 |
| Types pure-C# unit-testable (no Unity assemblies, no native DLL) | **0** | **3** (`FileTabViewModel`, `SubsetBoundsViewModel`, `DebugTabViewModel`) | +3 |
| Adapters integration-testable behind interface seams | **0** | **2** (`FitsServiceAdapter` via `IFitsService` test double; `UnityLogStreamAdapter` via `ILogStream` test double) | +2 |

---

## 7. Thresholds Reference (Assignment Spec §7.1)

| Role | WMC | DIT | NOC | CBO | RFC | LCOM |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| Domain / ViewModel | ≤ 20 | ≤ 4 | ≤ 5 | ≤ 14 | ≤ 50 | ≤ 0.50 |
| Orchestrator / Adapter | ≤ 40 | ≤ 4 | ≤ 5 | ≤ 25 | ≤ 50 | ≤ 0.50 |

Cycles are forbidden across all top-level components (see §6, row "Circular dependency cycles"). DIT and NOC thresholds apply uniformly across both roles. Observed DIT and NOC across all WE1/WE2 successor types stay at 0–1 (see §2.2 and §3.2) — well under threshold.

Role assignments:
- **Domain / ViewModel:** `FileTabViewModel`, `SubsetBoundsViewModel`, `DebugTabViewModel`
- **Adapter / Orchestrator:** `FileTabView`, `FitsServiceAdapter`, `StandaloneFileDialogAdapter`, `VolumeServiceAdapter`, `DebugTabView`, `UnityLogStreamAdapter`, `CanvassDesktop` shell

---

## 8. Traceability

| Artefact | Path |
|---|---|
| CK baseline (Understand export) | `docs/sub-team-6/deliverables/other/D9-ck-baseline/SK_BNCH.md` |
| NDepend-equivalent baseline | `docs/sub-team-6/deliverables/other/D9-ck-baseline/ndepend-equivalent-baseline.md` |
| File tab after-state class diagram | `after-class-diagram.puml` (this directory) |
| File tab after-state dependency graph | `after-dependency-graph.puml` (this directory) |
| File tab after-state DSM | `after-dsm.md` (this directory) |
| File tab before-state class diagram | `before-class-diagram.puml` (this directory) |
| File tab before-state DSM | `before-dsm.md` (this directory) |
| Debug tab after-state sequence diagram | `docs/sub-team-6/uml-diagrams/after-debug-sequence-diagram.puml` |
| CanvassDesktop method reference + log sites | `docs/sub-team-6/CanvassDesktop.md` |
| MVVM refactoring proposal (§3.2 Debug tab) | `docs/sub-team-6/refactor.md` |
| MVVM ADR | `docs/sub-team-6/adrs/0001-mvvm-split.md` |
| Deliverables checklist | `docs/sub-team-6/deliverables/deliverables-checklist.md` |
| Feeds T4 | Consolidated architecture report §Metrics chapter |
| Feeds T7 | Integration & metrics deliverable — ISO 25010 evidence section |
| Feeds Pitch slot 3 | Worked examples slide — §6 aggregate violation table is the pitch-ready summary |
