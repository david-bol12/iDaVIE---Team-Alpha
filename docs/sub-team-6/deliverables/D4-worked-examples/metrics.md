# WE1-6 — File Tab + Debug Tab: CK + ISO 25010 Delta Worksheet

**Status:** Design proposal — all "after" figures are projected, not measured.  
**Before source:** `SK_BNCH.md` (Understand static analysis export, Day 2 baseline).  
**After source:** `after-class-diagram.puml`, `after-dependency-graph.puml`, `after-dsm.md`,  
`uml-diagrams/after-debug-sequence-diagram.puml`, `refactor.md §3.2`.  
**Feeds:** T4 (architecture report §Metrics), T7 (integration & metrics), Pitch slot 3.

---

## 1. Shared Before-State Baseline

Both worked examples extract responsibility from the same God class. These are the exact
measured numbers from `SK_BNCH.md`.

| Metric | `CanvassDesktop` (before) | Threshold (Orchestrator) | Status |
|---|:---:|:---:|:---:|
| WMC | **63** | ≤ 40 | 🔴 +23 |
| DIT | 1 | ≤ 4 | ✅ |
| NOC | 0 | ≤ 5 | ✅ |
| CBO | **47** | ≤ 25 | 🔴 +22 |
| RFC | **118** | ≤ 50 | 🔴 +68 |
| LCOM (H-S) | **0.955** | ≤ 0.50 | 🔴 |

**CanvassDesktop CBO breakdown (47 types):** 23 project classes (e.g. `VolumeDataSetRenderer`,
`FitsReader`, `DataAnalysis`, `FeatureMapping`) + 13 Unity/TMPro UI types + 7 `System.*` types +
4 `Valve.VR` types.

**LCOM explanation:** 63 methods, 67 fields, only 189 field–method accesses. Methods operate on
small, largely disjoint field subsets — textbook signature of a class that should be split.
Three fields (`_restFrequency`, `inPaintMode`, `_tabsManager`) are declared but never accessed
by any method (dead fields).

---

## 2. Worked Example 1 — File Tab

### 2.1 Before State: File Tab Methods in CanvassDesktop

The 16 methods below are the File tab's responsibility slice of `CanvassDesktop`. They account
for **~25 % of WMC** and are the primary driver of CBO through direct `FitsReader` P/Invoke,
`StandaloneFileBrowser` callbacks, and three `FindObjectOfType<>` singleton hunts.

| Method | Lines (approx) | Concern | Code smell |
|---|:---:|---|---|
| `BrowseImageFile()` | ~5 | Wires UI button to SFB async callback | Mixed UI event + I/O dispatch |
| `_browseImageFile(string)` | ~55 | FITS open, HDU parse, header read, subset reset | P/Invoke inside business logic; 5 `FitsReader.*` calls |
| `IsLoadable()` | ~40 | NAXIS validation, axis-size extraction, Z-dropdown populate | Axis maths + UI manipulation in one method |
| `UpdateHeaderFromFits(IntPtr)` | ~25 | FITS header dump to scroll-view `TextMeshProUGUI` | `IntPtr` (P/Invoke handle) crosses the UI layer |
| `ChangeHduSelection(TMP_Dropdown)` | ~10 | Re-opens FITS file on each HDU dropdown change | Re-opens file on every switch; `IntPtr` lifetime not scoped |
| `BrowseMaskFile()` | ~5 | Wires mask button to SFB callback | Same mixed-concern pattern as `BrowseImageFile` |
| `_browseMaskFile(string)` | ~20 | Reads mask headers, validates axis match | Axis comparison logic embedded with UI updates |
| `CheckImgMaskAxisSize()` | ~15 | **Dead code** — axis comparison duplicate | Duplicate of `_browseMaskFile` validation; no callers found |
| `onSubsetToggleSelected(bool)` | ~10 | Show/hide subset row + focus first field | Pure UI, but lives in God class |
| `setSubsetBounds()` | ~10 | Reset all 6 subset input fields to axis maxima | Updates 6 `TMP_InputField.text` strings inline |
| `updateSubsetZMax(int)` | ~10 | Clamp Z-max when Z-axis dropdown changes | Direct field write + UI sync in same method |
| `checkSubsetBounds(string)` | ~70 | Validate + clamp all 6 subset input fields | 18 `Debug.Log` calls; re-validates all fields on any single edit |
| `LoadFileFromFileSystem()` | ~5 | Entry point — starts `LoadCubeCoroutine` | Trivial, but couples UI button directly to coroutine |
| `CheckMemSpaceForCubes(string,string)` | ~15 | Calculates in-memory size vs `SystemInfo.systemMemorySize` | Checks *client* RAM for a server-owned resource |
| `LoadCubeCoroutine(string,string,int)` | ~80 | Scene teardown, prefab instantiation, coroutine wait | File I/O + scene management + UI updates + coroutine lifecycle in one method; clearest SRP violation |
| `postLoadFileFileSystem()` | ~40 | Post-load tab-button enables, dropdown populates, event subscriptions | Triggers 7 unrelated post-load UI side-effects |

**WMC contributed by File tab slice:** 16 methods × avg cyclomatic complexity ≈ 1.5 = **~24** of CanvassDesktop's total WMC 63.

**CBO coupling introduced by File tab slice:**
- `FitsReader` (5 P/Invoke call-sites)
- `StandaloneFileBrowser` (2 async callback sites)
- `VolumeDataSetRenderer` (direct field writes in `LoadCubeCoroutine`)
- `VolumeCommandController`, `VolumeInputController`, `HistogramHelper` (3 `FindObjectOfType<>` in `Start()`, used by File tab post-load flow)
- `PlayerPrefs` (last-path persistence)
- `IntPtr`, `Marshal`, `StringBuilder` (3 System types for P/Invoke handles)

### 2.2 After State: File Tab Successor Types [projected]

| Type | Layer | Inherits | WMC | DIT | NOC | CBO | RFC | LCOM | Threshold |
|---|---|---|:---:|:---:|:---:|:---:|:---:|:---:|---|
| `FileTabView` | View (Unity) | `MonoBehaviour` | **8** | 1 | 0 | **5** | **18** | **0.05** | WMC≤40 / CBO≤25 |
| `FileTabViewModel` | ViewModel (pure C#) | `object` | **12** | 0 | 0 | **5** | **22** | **0.20** | WMC≤20 / CBO≤14 |
| `SubsetBoundsViewModel` | ViewModel (pure C#) | `object` | **8** | 0 | 0 | **1** | **11** | **0.15** | WMC≤20 / CBO≤14 |
| `FitsServiceAdapter` | Adapter (Unity asm) | `object` | **10** | 0 | 0 | **6** | **20** | **0.15** | WMC≤40 / CBO≤25 |
| `StandaloneFileDialogAdapter` | Adapter (Unity asm) | `MonoBehaviour` | **4** | 1 | 0 | **4** | **8** | **0.05** | WMC≤40 / CBO≤25 |
| `VolumeServiceAdapter` | Adapter (Unity asm) | `MonoBehaviour` | **6** | 1 | 0 | **7** | **13** | **0.15** | WMC≤40 / CBO≤25 |
| `CanvassDesktop` (shell) | Composition root | `MonoBehaviour` | **8** | 1 | 0 | **4** | **12** | **0.10** | WMC≤40 / CBO≤25 |

**All 7 types: 0 CK violations.**

#### Method-count derivation

**`FileTabView` WMC = 8**  
`BindTo`, `OnBrowseImageClicked`, `OnBrowseMaskClicked`, `OnLoadClicked`,
`OnHduDropdownChanged`, `OnSubsetToggleChanged` (6 from `after-class-diagram.puml`)
+ `OnEnable`, `OnDisable` (required for safe binding registration in Unity lifecycle).  
All methods have cyclomatic complexity 1 (bind, unbind, or forward event). LCOM ≈ 0.05
because every method touches the single `_vm : IFileTabViewModel` field.

**`FileTabViewModel` WMC = 12**  
`BrowseImageAsync`, `BrowseMaskAsync`, `LoadAsync`, `MaskAxesMatchImage`,
`PopulateZAxisOptions`, `UpdateZAxisMax` (6 from diagram) + `IsLoadable` computed getter +
constructor + `OnPropertyChanged` helper + 3 observable property setters
(`SelectedHduIndex`, `SubsetEnabled`, `SelectedZAxisIndex`). Consistent with "WMC ≈ 12"
annotation in `after-dependency-graph.puml`.  
CBO = 5: `IFitsService`, `IFileDialogService`, `IVolumeService`, `SubsetBoundsViewModel`,
`FitsFileInfo`. No `UnityEngine`, no `FitsReader`. Consistent with "CBO ≈ 5" annotation.

**`SubsetBoundsViewModel` WMC = 8**  
`ResetToAxisMaxima`, `UpdateZAxisMax`, `ToDto` + 5 clamping property setters
(`XMin`, `XMax`, `YMin`, `YMax`, `ZMin` — `ZMax` clamped via `UpdateZAxisMax`).  
Replaces `checkSubsetBounds` (70 lines, 18 `Debug.Log` calls), `setSubsetBounds`,
and `updateSubsetZMax` from `CanvassDesktop`. CBO = 1 (`SubsetBounds` DTO only).  
LCOM ≈ 0.15 because all 8 methods access the 9 int state fields (XMin/XMax/YMin/YMax/ZMin/ZMax + MaxX/MaxY/MaxZ).

**`FitsServiceAdapter` WMC = 10**  
`OpenImageAsync`, `OpenMaskAsync`, `GetHeaderTextAsync` (3 public, implementing `IFitsService`)
+ constructor + 6 private helpers (`FitsOpen`, `ReadHduList`, `ReadAxisSizes`, `ExtractHeaderText`,
`FitsClose`, `ThrowIfFitsError`). All 10 methods use the same `FitsReader` static facade —
LCOM ≈ 0.15. CBO = 6: `FitsReader`, `FitsFileInfo`, `HduInfo`, `Task`, `CancellationToken`,
`IFitsService`. No `UnityEngine` import needed — pure P/Invoke wrapper.

**`StandaloneFileDialogAdapter` WMC = 4**  
`PickFileAsync` (public, implementing `IFileDialogService`) + constructor +
`OnFileSelected` (private SFB callback adapter) + `PersistLastDirectory` (private).
CBO = 4: `StandaloneFileBrowser`, `PlayerPrefs`, `Task<string?>`, `IFileDialogService`.

**`VolumeServiceAdapter` WMC = 6**  
`LoadCubeAsync` (public, implementing `IVolumeService`) + `IsCubeLoaded` getter +
constructor + `RunLoadCoroutine` (private) + `OnLoadComplete` (private callback) +
`MapRequest` (private DTO translation). CBO = 7: `VolumeCommandController`, `UnityEngine`
(Coroutine), `LoadCubeRequest`, `IProgress<float>`, `CancellationToken`, `Task`,
`IVolumeService`.

**`CanvassDesktop` shell WMC = 8**  
`Awake`, `Start`, `OnDestroy` (Unity lifecycle) + constructor logic + 4 private wiring helpers
(`WireFileTab`, `WireDebugTab`, `WireRenderingTab`, `TearDown`). No domain logic — only
instantiation and `BindTo()` calls. CBO = 4: `FileTabView`, `DebugTabView`,
`FileTabViewModel`, `DebugTabViewModel` (no concrete adapters — composition root receives
them via injection).

### 2.3 File Tab CK Delta

| Metric | Before (`CanvassDesktop`) | After (worst-case class) | Δ | Threshold met? |
|---|:---:|:---:|:---:|:---:|
| WMC (max per class) | 63 | 12 (`FileTabViewModel`) | **−51** | ✅ ≤ 20 |
| CBO (max per class) | 47 | 7 (`VolumeServiceAdapter`) | **−40** | ✅ ≤ 25 |
| RFC (max per class) | 118 | 22 (`FileTabViewModel`) | **−96** | ✅ ≤ 50 |
| LCOM (max per class) | 0.955 | 0.20 (`FileTabViewModel`) | **−0.755** | ✅ ≤ 0.50 |
| DIT | 1 | 1 (View / Adapters) | 0 | ✅ ≤ 4 |
| NOC | 0 | 0 | 0 | ✅ ≤ 5 |

| | Before | After | Δ |
|---|:---:|:---:|:---:|
| CK violations | 4 | 0 | **−4** |
| Interfaces backing File tab APIs | 0 | 4 | **+4** |
| Circular dependencies involving File tab | 2 cycles | 0 | **−2** |
| Classes with `UnityEngine` in domain code | 1 | 0 | **−1** |
| Dead methods (File tab) | 1 (`CheckImgMaskAxisSize`) | 0 | **−1** |

---

## 3. Worked Example 2 — Debug Tab

### 3.1 Before State: Logging in CanvassDesktop

There is no Debug tab class in the current codebase. The "Debug tab" before-state is
an *absence of design*: 40 `Debug.Log` / `Debug.LogWarning` / `Debug.LogError` call-sites
embedded inline in `CanvassDesktop`'s existing methods, with no structure, no UI display,
and no way to intercept or test them.

**Scatter map of log call-sites in `CanvassDesktop.cs`:**

| Method | Line(s) | Level | Message (abbreviated) |
|---|---|:---:|---|
| `_browseImageFile` | 351 | Log | `"Fits open failure... code #" + status` |
| `_browseImageFile` | 372 | Log | `"Could not find EXTNAME or HDUNAME in HDU " + i` |
| `IsLoadable` | 521 | Log | `"The list has " + count + " items, dropdown index " + idx` |
| `checkSubsetBounds` | 649, 654, 659, 665 | Log | XMax: min violation, max violation, lower-bound violation, not-a-number |
| `checkSubsetBounds` | 675, 680, 685, 691 | Log | YMax: same 4 conditions |
| `checkSubsetBounds` | 701, 706, 711, 717 | Log | ZMax: same 4 conditions |
| `checkSubsetBounds` | 727, 732, 737, 743 | Log | XMin: same 4 conditions |
| `checkSubsetBounds` | 753, 758, 763, 769 | Log | YMin: same 4 conditions |
| `checkSubsetBounds` | 779, 784, 789, 795 | Log | ZMin: same 4 conditions |
| `_browseMaskFile` | 844 | Log | `"Fits open failure... code #" + status` |
| `CheckMemSpaceForCubes` | 1008 | LogWarning | `"Cube and mask size ... exceed RAM size ..."` |
| `CheckMemSpaceForCubes` | 1011 | Log | `"Loading cube and mask of size ... MB"` |
| `LoadCubeCoroutine` | 1024 | Log | `"Loading image " + path + " and mask " + mask` |
| `LoadCubeCoroutine` | 1047 | Log | `"Replacing data cube..."` |
| `LoadCubeCoroutine` | 1075 | Log | `"Instantiating new cube prefab."` |
| `LoadCubeCoroutine` | 1128 | Log | `completeMessage` |
| `_browseMappingFile` | 1425 | LogError | `"Error while loading mapping file: " + ex.Message` |
| `ChangeHduSelection` | 1443 | Log | `"Fits open failure... code #" + status` |
| `LoadSourcesFile` | 1589 | Log | `"Minimal source mappings not set!"` |
| `SetMaxMinPercentile` | 1792, 1802 | LogError | `"Error calculating percentiles from histogram/data"` |
| `SetMaxMinPercentile` | 1806 | Log | `"Setting histogram scale min to percentiles..."` |

**Total call-sites:** 40 (24 in `checkSubsetBounds` alone, from the same 4-condition pattern
repeated for each of the 6 subset bounds fields).

**Before-state problems:**

| Problem | Evidence |
|---|---|
| No structured format | Raw string concatenation — no level enum, no source tag, no timestamp |
| Log output invisible to end user | Unity console only; no in-app Debug tab panel |
| 24 / 40 calls come from `checkSubsetBounds` | One method emits 24 unstructured logs; none are user-visible |
| No interception point | Cannot assert on log output in a unit test without Unity test runner + console parsing |
| `UnityEngine.Debug` is a static class | Every log call is a hidden transitive `UnityEngine` dependency in the method that calls it |

**WMC contribution:** The 40 call-sites are inline in existing methods; they do not add new
methods (no WMC addition) but they inflate the cyclomatic complexity of methods like
`checkSubsetBounds` (18 calls inside one 70-line method → cyclomatic complexity ≥ 7
just from subset-bounds branches). There is no dedicated Debug tab class: **WMC before = 0,
CBO before = 0** (for the Debug tab concern in isolation — its coupling is absorbed into
`CanvassDesktop`'s existing CBO-47 total).

### 3.2 After State: Debug Tab Successor Types [projected]

The after state introduces an Observer pattern around a structured log stream,
per `uml-diagrams/after-debug-sequence-diagram.puml` and `refactor.md §3.2`.

| Type | Layer | Inherits | WMC | DIT | NOC | CBO | RFC | LCOM | Threshold |
|---|---|---|:---:|:---:|:---:|:---:|:---:|:---:|---|
| `DebugTabView` | View (Unity) | `MonoBehaviour` | **5** | 1 | 0 | **3** | **10** | **0.05** | WMC≤40 / CBO≤25 |
| `DebugTabViewModel` | ViewModel (pure C#) | `object` | **7** | 0 | 0 | **3** | **12** | **0.10** | WMC≤20 / CBO≤14 |
| `UnityLogStreamAdapter` | Adapter (Unity asm) | `object` | **5** | 0 | 0 | **4** | **10** | **0.10** | WMC≤40 / CBO≤25 |

**All 3 types: 0 CK violations.**

**Interfaces introduced:**

| Interface | Methods | Implemented by |
|---|:---:|---|
| `ILogStream` | 3 (`Subscribe`, `Unsubscribe`, `Publish`) | `UnityLogStreamAdapter` |
| `ILogObserver` | 1 (`OnNext(LogEntry)`) | `DebugTabViewModel` |
| `IDebugTabViewModel` | 4 (`LogEntries`, `ClearCommand`, `AutoScrollEnabled`, `FilterLevel`) | `DebugTabViewModel` |

**`LogEntry` DTO fields:** `Level` (enum: Debug/Info/Warning/Error), `Source` (string),
`Message` (string), `Timestamp` (DateTimeOffset). Replaces raw `Debug.Log` string concatenation.

#### Method-count derivation

**`DebugTabView` WMC = 5**  
`BindTo`, `OnEnable`, `OnDisable` (Unity lifecycle + binding registration) +
`AppendEntryToScrollView` (private, called on binding update) + `ScrollToBottom` (private).
All 5 methods access `_vm : IDebugTabViewModel` → LCOM ≈ 0.05. CBO = 3:
`IDebugTabViewModel`, `UnityEngine` (ScrollView / UI Toolkit element), `LogEntry` (display formatting).

**`DebugTabViewModel` WMC = 7**  
Implements `ILogObserver.OnNext` (appends entry to `Entries`) +
`IDebugTabViewModel` properties: `LogEntries` getter, `AutoScrollEnabled` getter/setter,
`FilterLevel` getter/setter + `ClearCommand.Execute` + constructor (subscribes to `ILogStream`).
Consistent with "WMC ≈ 5" estimate in `refactor.md §9` (difference is +2 for property
changed notification and filter setter).  
CBO = 3: `ILogStream`, `ILogObserver` (self-implements), `LogEntry`. **Zero `UnityEngine` imports.**

**`UnityLogStreamAdapter` WMC = 5**  
`Subscribe(ILogObserver)`, `Unsubscribe(ILogObserver)`, `Publish(LogLevel, string, string)` +
`ForwardToUnityConsole` (private — calls `UnityEngine.Debug.Log`/`LogWarning`/`LogError` for
IDE console parity) + constructor.  
CBO = 4: `ILogStream`, `ILogObserver`, `LogEntry`, `UnityEngine.Debug`.  
This is the **only** class that imports `UnityEngine.Debug` for logging. All 40 former
`Debug.Log` call-sites in `CanvassDesktop` become `_logStream.Publish(...)` calls, which
compile against `ILogStream` only.

### 3.3 Debug Tab CK Delta

The before-state comparator is not a single class but the aggregate logging burden
absorbed into `CanvassDesktop`. The meaningful "before" figures are:

| Metric | Before (absorbed in CanvassDesktop) | After (worst-case new class) | Δ |
|---|:---:|:---:|:---:|
| WMC (dedicated debug class) | 0 (no class — embedded) | 7 (`DebugTabViewModel`) | +7 *(new focused class)* |
| CBO (dedicated debug class) | 0 (no class — coupling absorbed into CBO-47) | 4 (`UnityLogStreamAdapter`) | +4 *(new isolated class)* |
| `checkSubsetBounds` cyclomatic complexity | ≥ 7 (18 log calls + branch structure) | ≤ 2 (clamping setters in `SubsetBoundsViewModel` — logging replaced by property feedback) | **−5** |
| Unstructured log call-sites | 40 | 0 (replaced by `_logStream.Publish()`) | **−40** |
| Classes importing `UnityEngine.Debug` for logging | ≥ 1 (`CanvassDesktop` + any other class calling `Debug.Log`) | 1 (`UnityLogStreamAdapter` only) | **−N+1** |
| Interfaces backing log stream | 0 | 3 (`ILogStream`, `ILogObserver`, `IDebugTabViewModel`) | **+3** |
| Log call-sites testable without Unity runner | 0 | 40 (all moved behind `ILogStream.Publish`) | **+40** |

---

## 4. Cross-Cutting Violation Summary

Counts across both worked examples combined, relative to CanvassDesktop as the shared before-state.

| Rule | Before | After (all WE1 + WE2 types) | Δ |
|---|:---:|:---:|:---:|
| CK violations (WMC/CBO/RFC/LCOM) | 4 | **0** | −4 |
| Circular dependency cycles | 2 | **0** | −2 |
| Classes with `UnityEngine` in domain/ViewModel code | 1 | **0** | −1 |
| Public API boundaries backed by interfaces | 0 | **7** (`IFileTabViewModel`, `IFitsService`, `IFileDialogService`, `IVolumeService`, `ILogStream`, `ILogObserver`, `IDebugTabViewModel`) | +7 |
| Dead methods | 1 (`CheckImgMaskAxisSize`) | **0** | −1 |
| Unstructured log call-sites | 40 | **0** | −40 |
| Classes unit-testable without Unity test runner | 0 | **5** (`FileTabViewModel`, `SubsetBoundsViewModel`, `FitsServiceAdapter`, `DebugTabViewModel`, `UnityLogStreamAdapter`) | +5 |

---

## 5. ISO 25010 Maintainability Mapping

ISO 25010:2011 §4.2.7 defines five sub-characteristics. The table below rates each
per worked example with the specific evidence.

### 5.1 File Tab (WE1)

| Sub-char | Before rating | After rating [proj] | CK driver | Evidence |
|---|:---:|:---:|---|---|
| **Modularity** | Poor | Good | CBO 47→7, 2 cycles→0 | `FileTabViewModel` couples only to 3 service interfaces. Both confirmed circular dependencies (`CanvassDesktop`↔`VolumeCommandController` and `CanvassDesktop`↔`DesktopPaintController`) are broken because neither adapter holds a back-reference to the View. Any change to `FitsReader`'s P/Invoke ABI is contained inside `FitsServiceAdapter` — blast radius = 1 class. |
| **Reusability** | Poor | Acceptable | 0 interfaces → 4 | `IFitsService`, `IFileDialogService`, `IVolumeService`, and `IFileTabViewModel` are explicit re-use seams. A CLI entrypoint, a test harness, or a future remote-server mode can swap any adapter without modifying `FileTabViewModel`. The "remote-browse RPC" case (`CanvassDesktop.md §3`) requires only a new `IFileDialogService` implementation, not a rewrite of the ViewModel. |
| **Analysability** | Poor | Good | WMC 63→12, RFC 118→22, LCOM 0.955→0.20 | `FileTabViewModel` has 12 focused methods and one responsibility (file-path lifecycle). Tracing "why did the load button stay disabled?" requires reading one 12-method class rather than scanning 1 899 lines for the `IsLoadable` return value. RFC 22 means at most 22 execution paths enter or leave `FileTabViewModel` — down from 118 in the God class. |
| **Modifiability** | Poor | Good | CBO 47→7, RFC 118→22 | File tab modifications are now scoped: a new HDU type requires changes in `FitsServiceAdapter` and `FitsFileInfo` only; `FileTabViewModel` sees only the updated `FitsFileInfo` DTO. Before, the same change touched the P/Invoke calls, the dropdown-populate logic, and the header-display logic all inside `CanvassDesktop`. |
| **Testability** | Very Poor | Good | LCOM 0.955→0.20, 0→4 interfaces, `UnityEngine` removed from domain | **`FileTabViewModel`** has zero `MonoBehaviour` ancestry — instantiated in NUnit with `new FileTabViewModel(mockFits, mockDialog, mockVolume)`. `checkSubsetBounds` (previously 70 lines + 18 `Debug.Log` + Unity input fields) becomes property setters on **`SubsetBoundsViewModel`** — asserted with plain `Assert.Equal`. **`FitsServiceAdapter`** tests use a mocked `FitsReader` ABI stub, running without a Unity runtime. |

**Testability blocker inventory (WE1):**

| Blocker in `CanvassDesktop` | Resolved by |
|---|---|
| `MonoBehaviour` ancestry — requires Unity test runner | `FileTabViewModel` / `SubsetBoundsViewModel` are `object` subclasses |
| `FindObjectOfType<VolumeCommandController>()` — requires full scene | Constructor-injected `IVolumeService` |
| `FitsReader` P/Invoke — requires native `.dll` on test machine | Isolated behind `IFitsService` — test double returns `FitsFileInfo` DTOs |
| 25+ `transform.Find(…).GetComponent<T>()` chains | `FileTabView` owns all hierarchy traversal; ViewModel never touches `Transform` |
| `StandaloneFileBrowser` async callbacks — not mockable | `IFileDialogService.PickFileAsync()` — stubbed with `Task.FromResult("path/to/file.fits")` |
| `CheckMemSpaceForCubes` checks `SystemInfo.systemMemorySize` | Moved to `IVolumeService.CanFit()` — server (adapter) answers its own capacity question |

### 5.2 Debug Tab (WE2)

| Sub-char | Before rating | After rating [proj] | CK driver | Evidence |
|---|:---:|:---:|---|---|
| **Modularity** | Very Poor | Good | 40 log sites → 0, 0 interfaces → 3 | All 40 `Debug.Log` call-sites are replaced by `_logStream.Publish(level, source, message)`. Log producers (`FitsServiceAdapter`, `SubsetBoundsViewModel`, etc.) depend only on `ILogStream` — they have no knowledge of the display consumer. Adding a second log consumer (e.g. a file logger for diagnostics) means implementing `ILogObserver` in a new class, with zero changes to producers. |
| **Reusability** | Very Poor | Good | 0 → 3 interfaces | `ILogStream` and `ILogObserver` are generic interfaces with no Unity dependencies. `DebugTabViewModel` can be embedded in any future shell (CLI, web dashboard, VR overlay) that provides an `ILogStream` implementation. |
| **Analysability** | Poor | Good | `checkSubsetBounds` cyclomatic ≥ 7 → ≤ 2 | The 18 `Debug.Log` calls in `checkSubsetBounds` disappear entirely: `SubsetBoundsViewModel`'s clamping setters return corrected values without logging — validation outcomes are expressed as observable property changes, not console noise. Remaining real errors (FITS open failure, mapping load error) surface as structured `LogEntry` records with `Level`, `Source`, and `Timestamp` fields, parseable without string inspection. |
| **Modifiability** | Poor | Good | 40 → 0 direct `UnityEngine.Debug` dependencies | Adding a new log source (e.g. `VolumeServiceAdapter`) requires one `_logStream.Publish()` call. Changing the log display from a Unity ScrollView to a UI Toolkit `ListView` requires modifying only `DebugTabView` — `DebugTabViewModel` is unchanged because it exposes `IReadOnlyList<LogEntry>` with no UI type in its public surface. |
| **Testability** | Very Poor | Good | 0 → 3 interfaces, `UnityEngine.Debug` isolated | `DebugTabViewModel.OnNext(entry)` is a pure method with no `UnityEngine` import — asserted directly in NUnit: call `OnNext(new LogEntry(LogLevel.Error, "FitsOpen", "status=4", ...))`, assert `Entries.Count == 1`. `UnityLogStreamAdapter` is the only class that calls `UnityEngine.Debug.*` — other 39 former call-sites are testable in a standard .NET test project. |

**Testability blocker inventory (WE2):**

| Blocker in `CanvassDesktop` | Resolved by |
|---|---|
| `Debug.Log` is `UnityEngine.Debug` — static, untestable | All log calls go through `ILogStream.Publish()`; `UnityLogStreamAdapter` is the only `UnityEngine.Debug` caller |
| 24 log calls in `checkSubsetBounds` — no assertion target | `SubsetBoundsViewModel` property setters express validation without logging; test asserts on property value |
| No end-user visibility of log output | `DebugTabView` scrolls `LogEntry` list — smoke-testable by running the app |
| No log level filtering | `IDebugTabViewModel.FilterLevel` property — filter by `LogLevel` enum in ViewModel without touching the View |

---

## 6. Thresholds Reference

From assignment specification §7.1:

| Role assigned to | WMC | CBO | RFC | LCOM |
|---|:---:|:---:|:---:|:---:|
| Domain / ViewModel | ≤ 20 | ≤ 14 | ≤ 50 | ≤ 0.50 |
| Orchestrator / Adapter | ≤ 40 | ≤ 25 | ≤ 50 | ≤ 0.50 |

Role assignments for after-state types:

| Type | Role applied |
|---|---|
| `FileTabViewModel`, `SubsetBoundsViewModel`, `DebugTabViewModel` | **Domain / ViewModel** (pure C#, no framework) |
| `FileTabView`, `DebugTabView`, `FitsServiceAdapter`, `StandaloneFileDialogAdapter`, `VolumeServiceAdapter`, `UnityLogStreamAdapter`, `CanvassDesktop` shell | **Adapter / Orchestrator** (Unity assembly) |

---

## 7. Traceability

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
| Feeds Pitch slot 3 | Worked examples slide — before/after violation count and testability blocker tables |
