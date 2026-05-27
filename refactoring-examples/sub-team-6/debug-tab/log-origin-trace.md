# Debug Tab — Log Stream Origin Trace: CanvassDesktop & DataAnalysis

## TL;DR

**Where the 44 `Debug.Log*` calls actually come from.** Full catalogue across `CanvassDesktop.cs` (40 sites) + `DataAnalysis.cs` (4 sites), each classified by line, method, trigger, message text, and the after-state `source` value it would carry. Five categories: **E** native/plugin errors (E1–E10, 9 calls) · **W** warnings (W1–W3, 3 calls) · **I** load-lifecycle info (I1–I7, 8 calls) · **V** subset validation (24 calls, all in `checkSubsetBounds`) — these last 24 **disappear entirely** in the AFTER design because `SubsetBoundsViewModel` clamps in property setters and renders the corrected value inline. The four natural `source` values (`"FileTab"`, `"VolumeLoader"`, `"HistogramController"`, `"SourcesTab"`, `"DataAnalysis"`) provide direct evidence for the SRP split of `CanvassDesktop` and motivate adding `source` to `ILogStream.Publish(...)`. **E7–E10 are the cleanest ACL violations** — a P/Invoke wrapper calling `UnityEngine.Debug.Log` directly.

---

**Purpose:** Catalogue every `Debug.Log*` call site in `CanvassDesktop.cs` and
`DataAnalysis.cs`, classify each by level, trigger, and responsible concern, and
show why the before-state unstructured `(string, string, LogType)` is insufficient.
This feeds the after-state `ILogStream.Publish(LogLevel level, string source, string message)`
interface design and demonstrates that three of the four concerns mixed into
`CanvassDesktop` generate their own log streams that belong to separate ViewModels.

> Companion to `before-trace.md` (Phase C, smell S2 and S9).
> All line numbers are from branch `team6`.

---

## Summary counts

| Class | Total calls | Error | Warning | Info | Validation* |
|---|---|---|---|---|---|
| `CanvassDesktop.cs` | 40 | 5 | 3 | 8 | 24 |
| `DataAnalysis.cs` | 4 | 4 | 0 | 0 | 0 |
| **Total** | **44** | **9** | **3** | **8** | **24** |

\* The 24 "Validation" calls in `checkSubsetBounds` **disappear in the after-state** — they
are replaced by clamping property setters in `SubsetBoundsViewModel`. They are catalogued
here to make the elimination visible.

---

## Category E — Native / plugin errors (9 calls)

These fire when a P/Invoke function returns a non-zero error code or a `catch` block is hit.
In the before-state all are emitted as `Debug.Log` or `Debug.LogError` with no structured
`source` field — the Debug tab shows them mixed with info and validation noise.
In the after-state each maps to `ILogStream.Publish(LogLevel.Error, source, message)`.

| # | Line | Method / trigger | Message text | After-state source |
|---|---|---|---|---|
| E1 | `CanvassDesktop.cs:351` | `_browseImageFile` callback — `FitsReader.FitsOpenFile` returns non-zero | `"Fits open failure... code #" + status` | `"FileTab"` |
| E2 | `CanvassDesktop.cs:844` | `_browseMaskFile` callback — `FitsReader.FitsOpenFile` returns non-zero | `"Fits open failure... code #" + status` | `"FileTab"` |
| E3 | `CanvassDesktop.cs:1443` | `ChangeHduSelection` — `FitsReader.FitsOpenFile` returns non-zero on HDU re-open | `"Fits open failure... code #" + status` | `"FileTab"` |
| E4 | `CanvassDesktop.cs:1425` | `LoadSourceFile` catch block — exception loading CSV/VOTable mapping | `"Error while loading mapping file. Check that all mappings are included: " + ex.Message` | `"SourcesTab"` |
| E5 | `CanvassDesktop.cs:1792` | `SetPercentileScale` — `DataAnalysis.GetPercentileValuesFromHistogram` returns non-zero | `"Error calculating percentiles from histogram."` | `"HistogramController"` |
| E6 | `CanvassDesktop.cs:1802` | `SetPercentileScale` — `DataAnalysis.GetPercentileValuesFromData` returns non-zero | `"Error calculating percentiles from data."` | `"HistogramController"` |
| E7 | `DataAnalysis.cs:187` | `GetXProfileAsArray` — `GetXProfile` P/Invoke returns non-zero | `"Error finding profile"` | `"DataAnalysis"` |
| E8 | `DataAnalysis.cs:202` | `GetYProfileAsArray` — `GetYProfile` P/Invoke returns non-zero | `"Error finding profile"` | `"DataAnalysis"` |
| E9 | `DataAnalysis.cs:217` | `GetZProfileAsArray` — `GetZProfile` P/Invoke returns non-zero | `"Error finding profile"` | `"DataAnalysis"` |
| E10 | `DataAnalysis.cs:234` | `GetMaskedSourceArray` — `GetMaskedSources` P/Invoke returns non-zero | `"Error extracting sources"` | `"DataAnalysis"` |

**ACL note (S9):** E7–E10 are the clearest ACL violations — `DataAnalysis.cs` is a P/Invoke
wrapper in `Assets/Scripts/PluginInterface/` that calls directly into `UnityEngine.Debug`.
Domain-layer native errors surface through Unity's global log sink, making them
untestable and invisible until the Debug tab UI is running. In the after-state, the
`DataAnalysis` adapter publishes to `ILogStream`; the domain layer (`IDataAnalysisService`)
never sees `UnityEngine`.

---

## Category W — Warnings (3 calls)

Non-fatal conditions a developer or operator should notice.

| # | Line | Method / trigger | Message text | After-state source |
|---|---|---|---|---|
| W1 | `CanvassDesktop.cs:372` | `_browseImageFile` — `FitsReadKey` cannot find `EXTNAME`/`HDUNAME` in an HDU; falls back to `"HDU N"` | `"Could not find EXTNAME or HDUNAME in HDU " + (i+1) + "! Using default name."` | `"FileTab"` |
| W2 | `CanvassDesktop.cs:1008` | `CheckRAMSize` — cube + mask bytes exceed detected RAM | `"Cube and mask size (" + MB + " MB) exceed RAM size (" + ramMB + " MB)!"` (this is `Debug.LogWarning`) | `"VolumeLoader"` |
| W3 | `CanvassDesktop.cs:1589` | `LoadSources` — `AreMinimalMappingsSet()` returns false | `"Minimal source mappings not set!"` | `"SourcesTab"` |

**W2 detail:** This is the only call in the file that uses `Debug.LogWarning` correctly —
the before-state `[Log] :` prefix in `HandleLog` discards the LogType, so the Debug tab
displays it identically to an info message. The after-state `LogLevel.Warning` preserves
the distinction and allows the UI to colour-code it.

---

## Category I — Load lifecycle / info (8 calls)

Progress and state-change messages emitted during the `LoadCubeCoroutine` and
histogram scale operations. These are the messages most visible to the operator
during normal use — they form the "happy path" trace in the Debug tab.

| # | Line | Method / trigger | Message text | After-state source |
|---|---|---|---|---|
| I1 | `CanvassDesktop.cs:521` | `IsLoadable` — Z-axis dropdown index diagnostic fired every time `IsLoadable` is evaluated | `"The list has " + n + " items, and the dropdown points to index " + idx + "!"` | `"FileTab"` (or remove — diagnostic noise) |
| I2 | `CanvassDesktop.cs:1011` | `CheckRAMSize` — successful size check | `"Loading cube and mask of size " + MB + " MB with RAM size " + ramMB + " MB."` | `"VolumeLoader"` |
| I3 | `CanvassDesktop.cs:1024` | `LoadCubeCoroutine` start | `"Loading image " + imagePath + " and mask " + maskPath + "."` | `"VolumeLoader"` |
| I4 | `CanvassDesktop.cs:1047` | `LoadCubeCoroutine` — existing cube found, replacing | `"Replacing data cube..."` | `"VolumeLoader"` |
| I5 | `CanvassDesktop.cs:1075` | `LoadCubeCoroutine` — `Instantiate(cubeprefab, ...)` | `"Instantiating new cube prefab."` | `"VolumeLoader"` |
| I6 | `CanvassDesktop.cs:1128` | `LoadCubeCoroutine` — load complete | `completeMessage` (`"Loading image X and mask Y complete!"`) | `"VolumeLoader"` |
| I7 | `CanvassDesktop.cs:1806` | `SetPercentileScale` — percentile values resolved, `UpdateScale` called | `"Setting histogram scale min to percentiles: X% and Y% with values: a and b."` | `"HistogramController"` |

**I1 note:** This call fires inside `IsLoadable`, which is a property getter evaluated
repeatedly during UI updates. It is diagnostic noise from development and should be
removed in any refactor — it does not map to any after-state `ILogStream` call.

---

## Category V — Subset validation (24 calls, eliminated in after-state)

All 24 calls live inside `checkSubsetBounds()` (`CanvassDesktop.cs:~630–800`), a single
~80-line method that manually validates six `TMP_InputField` values (XMin, XMax, YMin,
YMax, ZMin, ZMax) for the subset selector. Each of the six fields has four error conditions:

1. Value below absolute minimum (`< _subsetMin`)
2. Value above axis maximum (`> _subsetMax_X/Y/Z`)
3. Value inverted relative to the other bound
4. Value is not a parseable integer

This produces 6 × 4 = 24 `Debug.Log` calls. **None of these belong in the Debug tab.**
They are input-validation feedback that the user should see inline (red border, message
label) not buried in a scrollable log console.

| Lines | Field | Conditions logged |
|---|---|---|
| `649, 654, 659, 665` | XMax | below min · above axisMax · below XMin · not a number |
| `675, 680, 685, 691` | YMax | below min · above axisMax · below YMin · not a number |
| `701, 706, 711, 717` | ZMax | below min · above axisMax · below ZMin · not a number |
| `727, 732, 737, 743` | XMin | below min · above axisMax · above XMax · not a number |
| `753, 758, 763, 769` | YMin | below min · above axisMax · above YMax · not a number |
| `779, 784, 789, 795` | ZMin | below min · above axisMax · above ZMax · not a number |

**After-state:** `SubsetBoundsViewModel` (already written at
`file-tab/skeleton/SubsetBoundsViewModel.cs`) replaces all 24 calls. Each setter
self-clamps (`Math.Max(lo, Math.Min(hi, v))`) and raises `PropertyChanged` — the View
binds to it two-way and shows the corrected value immediately. No log calls needed.
The `checkSubsetBounds` method is deleted entirely.

---

## What the origin trace proves about the after-state design

### 1. `ILogStream.Publish` needs a `source` field

The before-state `(string, string, LogType)` tuple has no `source` field — all 44 calls
arrive in `DebugLogging.HandleLog` with identical structure. The Debug tab cannot
distinguish a `DataAnalysis` P/Invoke failure (E7–E10) from a load progress message
(I3–I6). The `source` parameter on `ILogStream.Publish(LogLevel, string source, string message)`
is the minimal addition that makes these filterable.

### 2. The 24 validation calls confirm `SubsetBoundsViewModel` is the right split

The single largest cluster of log calls (55%) is subset-bounds validation noise.
Extracting `SubsetBoundsViewModel` from `CanvassDesktop` eliminates them entirely —
they never enter `ILogStream` at all.

### 3. `DataAnalysis` log calls confirm the ACL requirement

E7–E10 are P/Invoke errors from a static plugin-wrapper class that should have no
awareness of Unity or UI. The fact that they call `UnityEngine.Debug.Log` directly is
the canonical ACL violation. The after-state adapter pattern moves these calls to
`UnityLogStreamAdapter`, which is the only class allowed to reference `UnityEngine.Debug`.

### 4. Four distinct `source` values map to four split responsibilities

| Source | Calls | Responsibility → after-state type |
|---|---|---|
| `"FileTab"` | E1, E2, E3, W1, I1 | File/mask loading, HDU selection → `FileTabViewModel` |
| `"VolumeLoader"` | W2, I2–I6 | Cube load pipeline, RAM check → `IVolumeService` adapter |
| `"HistogramController"` | E5, E6, I7 | Percentile scale ops → `HistogramMenuController` / separate service |
| `"SourcesTab"` | E4, W3 | Source catalogue loading → `SourcesTabViewModel` |
| `"DataAnalysis"` | E7–E10 | Native profile/source extraction → `IDataAnalysisService` adapter |

This is direct evidence for the SRP split of `CanvassDesktop` proposed in the
after-state class diagram.

---

## How this feeds the before-state class diagram

The class diagram in [`class-diagram.md`](class-diagram.md) should show:

- `CanvassDesktop` → `UnityEngine.Debug` (40 dependency arrows, or one labelled ×40)
- `DataAnalysis` → `UnityEngine.Debug` (4 arrows, or one labelled ×4)
- `UnityEngine.Debug` → `Application.logMessageReceived` → `DebugLogging.HandleLog`
- The **missing** `source` field is the key label to add to the `CanvassDesktop → Debug`
  arrow — it carries no structured metadata, only a raw string.
