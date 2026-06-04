# CK Metrics — Before & After (All Sub-Teams)

This document collates the Chidamber & Kemerer (CK) metrics recorded by each of the
seven sub-teams, for the class(es) they refactored, in their **before** (god-class /
baseline) and **after** (refactored) states.

**Every number below is copied verbatim from the team's own deliverable. Nothing is
estimated or invented.** Where a team did not record a value, it is shown as a gap
rather than filled in. Provenance (file + location) is given under each team.

## CK suite & acceptable ranges (project spec)

| Metric | Meaning | Acceptable range |
|---|---|---|
| **WMC** | Weighted Methods per Class | ≤ 20 domain; ≤ 40 adapters |
| **DIT** | Depth of Inheritance Tree | ≤ 4 |
| **NOC** | Number of Children | ≤ 5 |
| **CBO** | Coupling Between Object classes | ≤ 14 domain; ≤ 25 orchestrators/adapters; cycles forbidden |
| **RFC** | Response For a Class | ≤ 50 |
| **LCOM** | Lack of Cohesion in Methods (Henderson-Sellers) | ≤ 0.5 |

> Note on LCOM units: teams reported LCOM on either the 0–1 Henderson-Sellers scale or
> as a percentage (Understand's "Percent Lack of Cohesion"). Each team's value is shown
> in the unit its own source uses.

## Coverage summary

| Sub-team | Subject refactored | Before | After | Status |
|---|---|---|---|---|
| 1 | `VolumeDataSetRenderer` god class | ✅ | ✅ (projected) | Complete |
| 2 | `FitsReader` (FITS plugin) | ✅ | ✅ (NDepend) | Complete |
| 3 | `VolumeDataSetRenderer` + `MaskMode` | ✅ | ✅ (projected) | Complete |
| 4 | `VolumeInputController` (koffiewinkel) | ✅ | ❌ not recorded | **Before only** |
| 5 | `MomentMapRenderer` + `VoTableSaver` | ✅ | ✅ | Complete |
| 6 | File tab + Debug tab | ✅ | ✅ | Complete |
| 7 | Persistence (`Config` / `ExitController`) | ❌ targets only | ❌ targets only | **No measurements** |

---

## Sub-team 1 — `VolumeDataSetRenderer`

**Source:** `refactoring-examples/subteam-1/VolumeDataSetRenderer/metrics.md`
**Provenance:** "before" figures are reported as approximations (`≈`); "after" figures
are per-class **projections** of the proposed split.

### Before — `VolumeDataSetRenderer` (god class, 1,403 lines, 45 methods)

| Metric | WMC | DIT | NOC | CBO | RFC | LCOM |
|---|---|---|---|---|---|---|
| Value | ≈ 138 | 1 | not reported | ≈ 17 | ≈ 72 | ≈ 0.82 |

### After — thin orchestrator + six services (projected)

| Class | WMC | DIT | CBO | RFC | LCOM |
|---|---|---|---|---|---|
| `VolumeDataSetRenderer` (thin) | 18 | 1 | 9 | 22 | 0.20 |
| `VolumeRenderingService` | 22 | 0 | 8 | 24 | 0.15 |
| `MaskControllerService` | 12 | 0 | 4 | 14 | 0.10 |
| `VolumeCoordinateMapper` | 10 | 0 | 3 | 12 | 0.05 |
| `RegionControllerService` | 18 | 0 | 6 | 22 | 0.18 |
| `RestFrequencyService` | 9 | 0 | 3 | 11 | 0.08 |
| `VolumeDataExportService` | 10 | 0 | 5 | 18 | 0.12 |

**Delta (god class → worst single class after):** WMC 138 → 22 (−84%); CBO 17 → 9
(−47%); RFC 72 → 24 (−67%); LCOM 0.82 → 0.20 (−76%).

---

## Sub-team 2 — `FitsReader` (FITS plugin)

**Source:** `refactoring-examples/Sub-Team-2/Fits Reader/metrics/Metrics Comparison.pdf`
(NDepend tables, page 1). Baseline narrative also in `ai-tool-log/sub-team-2.md`.
**Provenance:** tool-measured (NDepend). `DIT = N/A` is shown by NDepend for static
classes. LCOM reported on the 0–1 scale.

### Before — `FitsMetrics` assembly (22 types)

| Class | WMC | DIT | NOC | CBO | RFC | LCOM | Methods |
|---|---|---|---|---|---|---|---|
| `AstTool` | 0 | 1 | 0 | 11 | 0 | 0 | 23 |
| `DataAnalysis` | 13 | N/A | 0 | 39 | 22 | 1.0 | 4 |
| `FitsReader` | 55 | 1 | 0 | 27 | 126 | 1.0 | 61 |

### After — `iDaVIE.PluginInterface` assembly (33 types)

| Class | WMC | DIT | NOC | CBO | RFC | LCOM | Methods |
|---|---|---|---|---|---|---|---|
| `DefaultExecutionOrderAttribute` | 1 | 2 | 0 | 6 | 0 | 0 | 2 |
| `FitsFile` | 2 | N/A | 0 | 7 | 2 | 0 | 16 |
| `FitsHeader` | 5 | N/A | 0 | 13 | 20 | 0 | 15 |
| `FitsImage` | 0 | N/A | 0 | 7 | 0 | 0 | 15 |
| `FitsTable` | 7 | N/A | 0 | 12 | 28 | 0 | 5 |
| `FitsMask` | 41 | N/A | 0 | 19 | 82 | 0 | 9 |
| `AstTool` | 0 | 1 | 0 | 9 | 0 | 0 | 23 |
| `DataAnalysis` | 12 | N/A | 0 | 38 | 25 | 0 | 5 |

**Delta (`FitsReader` → split classes):** the 55-WMC / 126-RFC / 1.0-LCOM `FitsReader`
was recast into `FitsFile`, `FitsHeader`, `FitsImage`, `FitsTable`, and `FitsMask`; the
heaviest resulting class (`FitsMask`) is WMC 41 / RFC 82, and LCOM drops to 0 across all
split classes.

---

## Sub-team 3 — `VolumeDataSetRenderer` + `MaskMode`

**Source:** `docs/team3/Deliverables/D2-MetricsWorksheet/metrics-worksheet.md`
(cross-referenced by `refactoring-examples/team3/example1-…/README.md` and
`…/example2-MaskModes/README.md`).
**Provenance:** "before" measured 19/05/26 with the Understand tool; "after" figures are
Day-13 per-class **projections**. LCOM normalised to the 0–1 (Henderson-Sellers) scale.

### Before — `VolumeDataSetRenderer` (1,402 lines)

| Metric | WMC | DIT | NOC | CBO | RFC | LCOM |
|---|---|---|---|---|---|---|
| Value | 97 | 2 | 0 | 28 | 97 | 0.95 |

### After — four-class split + MaskMode strategies

| Class | WMC | DIT | NOC | CBO | RFC | LCOM |
|---|---|---|---|---|---|---|
| `VolumeRenderCoordinator` | 11 | 1 | 0 | 15 | 11 | 0.69 |
| `VolumeRendererBehaviour` (MB shell) | 3 | 2 | 0 | 8 | 3 | 0.00 |
| `VolumeMaterialBinder` | 10 | 1 | 0 | 12 | 10 | 0.57 |
| `VolumeTextureManager` | 12 | 1 | 0 | 4 | 12 | 0.67 |
| `VolumeCameraDriver` | 4 | 1 | 0 | 4 | 4 | 0.25 |
| `VolumeCoordinateService` (static) | 3 | 1 | 0 | 3 | 3 | 0.00 |
| `FoveatedSamplingPolicy` | 6 | 1 | 0 | 6 | 6 | 0.33 |
| `ApplyMaskMode` | 2 | 1 | 0 | 2 | 2 | 0.00 |
| `InverseMaskMode` | 2 | 1 | 0 | 2 | 2 | 0.00 |
| `IsolateMaskMode` | 2 | 1 | 0 | 2 | 2 | 0.00 |
| `DisabledMaskMode` | 2 | 1 | 0 | 2 | 2 | 0.00 |
| `IMaskMode` (interface) | 0 | 0 | 5 | 4 | 0 | 0.00 |

**Delta (before → worst single class after):** WMC 97 → 12 (−85); CBO 28 → 15; RFC
97 → 12 (−85); LCOM 0.95 → 0.69.

---

## Sub-team 4 — `VolumeInputController` (koffiewinkel)

**Source (before):** `ai-tool-log/sub-team-4.md` (cross-checked against the Understand
tool output and the Day-2 baseline per that log).
**Status:** ⚠️ **Before only — no "after" CK metrics were recorded.** The Team-4 worked
examples list "NDepend: compare `VolumeInputController` WMC/RFC before vs after on same
build" as an outstanding verification step, and otherwise give only qualitative
expectations. No measured after-values exist to report.

### Before — `VolumeInputController` (god class)

| Metric | WMC | DIT | NOC | CBO | RFC | LCOM |
|---|---|---|---|---|---|---|
| Value | 79 | not recorded | not recorded | 31 | 79 | not recorded |

### After

> **Not recorded.** Team 4 refactored `VolumeCommandController` (voice dispatch) and
> decomposed `VolumeInputController` into seven collaborators (Locomotion / Interaction /
> Brush / Vignette controllers, `CursorInfoFormatter`, `QuickMenuPositioner`,
> `CameraGazeProvider`), but did not measure or document post-refactor CK values.

---

## Sub-team 5 — `MomentMapRenderer` + `VoTableSaver`

**Source:** `refactoring-examples/sub-team-5/example-1-moment-maps/CK_Metrics.md` and
`refactoring-examples/sub-team-5/example-2-votable-export/CK_Metrics.md`.
**Provenance:** RFC values for the "after" classes are recorded as approximate
(`~`, declared methods + calls); `–` means the source left the cell blank (interfaces).

### Example 1 — `VolumeData.MomentMapRenderer`

**Before**

| Metric | WMC | DIT | NOC | CBO | RFC | LCOM |
|---|---|---|---|---|---|---|
| Value | 27 | 2 | 0 | 17 | ~22 | 0.48 |

**After**

| Class | WMC | DIT | NOC | CBO | RFC | LCOM |
|---|---|---|---|---|---|---|
| `MomentMapCalculator` (Domain, static) | 22 | 1 | 0 | 2 | ~3 | 0.0 |
| `MomentMapService` (Application) | 4 | 1 | 0 | 5 | ~2 | 0.0 |
| `MomentMapRequest` (Domain DTO) | 6 | 1 | 0 | 2 | ~9 | 0.0 |
| `MomentMapResult` (Domain DTO) | 5 | 1 | 0 | 2 | ~9 | 0.0 |
| `MomentMapRendererAdapter` (Infra) | 19 | 2 | 0 | 14 | ~8 | 0.75 |
| `IMomentMapAdapter` (interface) | 0 | 0 | 0 | 1 | – | – |
| `IMomentMapService` (interface) | 0 | 0 | 0 | 2 | – | – |

### Example 2 — `VoTableReader.VoTableSaver`

**Before**

| Metric | WMC | DIT | NOC | CBO | RFC | LCOM |
|---|---|---|---|---|---|---|
| Value | 7 | 1 | 0 | 6 | ~1 | 0.0 |

**After**

| Class | WMC | DIT | NOC | CBO | RFC | LCOM |
|---|---|---|---|---|---|---|
| `FeatureCatalog` (Domain) | 4 | 1 | 0 | 3 | ~2 | 0.0 |
| `VoTableExportService` (Infra) | 14 | 1 | 0 | 8 | ~4 | 0.0 |
| `IVoTableExporter` (interface) | 0 | 0 | 1 | 2 | – | – |
| `ICoordinateTransformer` (interface) | 0 | 0 | 0 | 1 | – | – |
| `IAstFrame` (interface) | 0 | 0 | 0 | 0 | – | – |

---

## Sub-team 6 — File tab + Debug tab

**Source:** `docs/sub-team-6/deliverables/collated_deliverable/file-tab-refactor/ck-metrics.md`
and `…/debug-tab-refactor/ck-metrics.md` (canonical copies; the
`refactoring-examples/sub-team-6/` copies carry identical numbers).
**Provenance:** Tool-verified (Understand export, Day 13). RFC column is the tool's
RFC (= method count); the source notes traditional CK RFC runs ~2–4× higher. LCOM
reported as a percentage ("Percent Lack of Cohesion", Henderson-Sellers × 100). Complete
per-class tables are transcribed below (file tab = 11 classes; debug tab = 9 types,
including 3 interfaces not measured for WMC).

### Example 1 — File tab

**Before — `CanvassDesktop`** (entire class; the file-tab slice cannot be measured in isolation)

| Metric | WMC | DIT | NOC | CBO | RFC (tool) | LCOM |
|---|---|---|---|---|---|---|
| Value | 63 | 2 | 0 | 30 | 63 | 95% |
| Pass? | ❌ | ✅ | ✅ | ❌ | ❌ | ❌ |

> LOC = 1899. RFC: the tool defines RFC = total method count (= WMC = 63); traditional CK
> RFC (methods + external calls) ≈ 210. Fails 3 of 5 measured metrics (WMC, CBO, LCOM%).

**After — full 11-class slice**

| Class | Layer | WMC | DIT | NOC | CBO | RFC (tool) | LCOM % | Pass? |
|---|---|---|---|---|---|---|---|---|
| `FileTabViewModel` | orchestrator | 40 | 1 | 0 | 19 | 40 | 91% | ❌ LCOM |
| `SubsetBoundsViewModel` | domain | 20 | 1 | 0 | 1 | 20 | 77% | ❌ LCOM |
| `FitsMetadataHelper` | utility (static) | 3 | 0 | 0 | 2 | 3 | 0% | ✅ |
| `AsyncRelayCommand` | domain | 4 | 1 | 0 | 3 | 4 | 50% | ⚠ at limit |
| `RelayCommand` | domain | 4 | 1 | 0 | 3 | 4 | 50% | ⚠ at limit |
| `FitsServiceAdapter` | adapter | 6 | 1 | 0 | 7 | 6 | 33% | ✅ |
| `FileDialogServiceAdapter` | adapter | 1 | 1 | 0 | 1 | 1 | 0% | ✅ |
| `VolumeServiceAdapter` | adapter | 5 | 2 | 0 | 9 | 5 | 65% | ❌ LCOM |
| `MemoryProbeAdapter` | adapter | 1 | 1 | 0 | 0 | 1 | 0% | ✅ |
| `FileTabView` | adapter | 8 | 2 | 0 | 14 | 8 | 69% | ❌ LCOM |
| `FileTabCompositionRoot` | adapter | 2 | 2 | 0 | 12 | 2 | 33% | ✅ |
| **Σ slice** | — | **98 total / 40 max** | max 2 | 0 | max 19 | max 40 | 91% max | 6/11 pass all |

**Delta (god class → worst successor):** WMC 63 → 40 (−37%); CBO 30 → 19 (−37%);
RFC 63 → 40 (−37%); LCOM 95% → 91% (−4 pp; the source notes a different structural
cause — MVVM property-backing-field pattern, not disjoint concerns).

### Example 2 — Debug tab

**Before — `DebugLogging`**

| Metric | WMC | DIT | NOC | CBO | RFC | LCOM hs |
|---|---|---|---|---|---|---|
| Value | 8 | 4 | 0 | ~10 | ~25 | ≈ 0.95 (95%) |
| Pass? | ✅ | ✅ (at limit) | ✅ | ✅ | ✅ | ❌ |

> LOC = 255. The source frames this as a *testability* refactor, not a metric one:
> `DebugLogging` already passes 5/6 thresholds — only LCOM hs ≈ 0.95 fails (four disjoint
> concern clusters). Hand-count WMC = 8 (method count); McCabe-weighted WMC = 21 (still ✅).

**After — full 9-type slice**

| Class | Layer | WMC | DIT | NOC | CBO | RFC (tool) | LCOM % | Pass? |
|---|---|---|---|---|---|---|---|---|
| `DebugTabViewModel` | domain | 6 | 1 | 0 | 2 | 6 | 66% | ❌ LCOM |
| `LogStream` | domain | 4 | 1 | 0 | 3 | 4 | 25% | ✅ |
| `LogEntry` (record) | domain | 0 | 1 | 0 | 1 | 0 | 0% | ✅ |
| `IDebugTabViewModel` | domain (interface) | — | — | — | — | — | — | n/a |
| `ILogStream` | domain (interface) | — | — | — | — | — | — | n/a |
| `ILogObserver` | domain (interface) | — | — | — | — | — | — | n/a |
| `GatewayLogStreamAdapter` | adapter | 8 | 1 | 0 | 5 | 8 | 72% | ❌ LCOM |
| `DebugTabView` | adapter | 3 | 2 | 0 | 7 | 3 | 41% | ✅ |
| `DebugTabCompositionRoot` | adapter | 3 | 2 | 0 | 6 | 3 | 41% | ✅ |
| **Σ slice** | — | **24 total / 8 max** | max 2 | 0 | max 7 | max 8 | 72% max | 4/6 pass all |

**Delta (god class → worst successor):** WMC 8 → 8 (same size, responsibility re-split);
CBO ~10 → 7 (−30%); RFC (tool def.) 8 → 8 (unchanged); LCOM 95% → 72% (−23 pp; the
source notes a different cause — single-concern classes with sparse field access, not
disjoint concerns).

---

## Sub-team 7 — Persistence (`Config` / `ExitController`)

**Source:** `docs/team7/Requirements Document.md` (NFR3, §7.1).
**Status:** ⚠️ **No measured CK metrics exist — before or after.** The requirements
mandate that "baseline metrics for `Config.cs` and `ExitController.cs` must be recorded on
Day 2 as the 'before' figures" and call for a "Day 2 baseline + Day 13 projected delta",
but no such measurements were found in any Team-7 deliverable. Only **target thresholds**
were defined:

### Target CK thresholds (NFR3 — not measurements)

| Component | WMC target | CBO target | LCOM target |
|---|---|---|---|
| Workspace snapshot aggregate | ≤ 5 | 0 | ≤ 0.1 |
| Snapshot serialiser / repository | ≤ 10 | ≤ 5 | ≤ 0.3 |
| State collection orchestrator | ≤ 15 | ≤ 8 | ≤ 0.4 |
| Autosave service | ≤ 8 | ≤ 4 | ≤ 0.3 |
| `Config` (post-split) | ≤ 3 | 0 | ≤ 0.1 |

### Before / After

> **Not recorded.** No measured baseline (`Config.cs`, `ExitController.cs`) and no
> post-refactor values were documented. DIT, NOC, and RFC targets were not specified
> either.

---

## Sources index

| Sub-team | File |
|---|---|
| 1 | `refactoring-examples/subteam-1/VolumeDataSetRenderer/metrics.md` |
| 2 | `refactoring-examples/Sub-Team-2/Fits Reader/metrics/Metrics Comparison.pdf` |
| 3 | `docs/team3/Deliverables/D2-MetricsWorksheet/metrics-worksheet.md` |
| 4 | `ai-tool-log/sub-team-4.md` (before only) |
| 5 | `refactoring-examples/sub-team-5/example-1-moment-maps/CK_Metrics.md`, `…/example-2-votable-export/CK_Metrics.md` |
| 6 | `docs/sub-team-6/deliverables/collated_deliverable/{file-tab-refactor,debug-tab-refactor}/ck-metrics.md` |
| 7 | `docs/team7/Requirements Document.md` (targets only) |
