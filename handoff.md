# Sub-team 5 — Feature System Handoff

**Branch:** `team5` | **Updated:** 2026-05-28 (post-audit pass)

---

## What Was Done

The `FeatureSetManager` God-class has been decomposed into three focused classes:

| Class | Namespace | Type | Lives in |
|---|---|---|---|
| `FeatureCatalog` | `iDaVIE.Domain.Feature` | Pure C# — domain registry | `Assets/Scripts/FeatureData/FeatureCatalog.cs` |
| `FeatureSetService` | `iDaVIE.Application.Feature` | Pure C# — use-case orchestrator | `Assets/Scripts/FeatureData/FeatureSetService.cs` |
| `FeatureVisualiser` | `iDaVIE.Infrastructure.Unity` | MonoBehaviour — Unity adapter | `Assets/Scripts/FeatureData/FeatureVisualiser.cs` |

All callers have been migrated (`VolumeDataSetRenderer`, `VolumeInputController`, `CanvassDesktop`, `FeatureMenuController`, `FeatureMenuCell`, `QuickMenuController`, `SpectralProfileHelper`, `FeatureAnchor`). `FeatureSetManager.cs` has been deleted.

New boundary interfaces introduced this sprint:

| Interface | Namespace | Implemented by |
|---|---|---|
| `IFeaturePersistenceService` | `iDaVIE.Domain.Feature` | WP7 (stub: `NullFeaturePersistenceService`) |
| `IFeatureRenderer` | `iDaVIE.Domain.Feature` | WP3 (`FeatureSetRenderer`) |
| `IFeatureTableLoader` | `iDaVIE.Application.Feature` | WP2 (stub: `NullFeatureTableLoader`) |
| `IFeatureStatisticsProvider` | `iDaVIE.Application.Feature` | DataAnalysis (stub: `DataAnalysisStatisticsProvider`) |
| `IVoTableExporter` | `iDaVIE.Domain.Feature` | `VoTableExportService` |
| `ICoordinateTransformer` | `iDaVIE.Domain.Feature` | WP2 / native plugin |
| `IAstFrame` | `iDaVIE.Domain.Feature` | Opaque handle — replaces raw `IntPtr` |

Two refactoring examples with full before/after code and CK metrics are in `refactoring-examples/sub-team-5/`.

---

## Compile Errors — All Resolved

All blocking compile errors have been fixed on this branch. Summary for reference:

| Error | Fix applied |
|---|---|
| `FeatureSetService` called `new Feature(cornerMin:, cornerMax:, color:, visible:...)` but `Feature` declared `cubeMin, cubeMax` etc. | `Feature.cs` constructor parameters renamed to match callers |
| `feature.ParentSet` did not exist | Replaced with `feature.FeatureSetParent` throughout |
| `Feature.BoundsContains`, `BoundsVolume` missing | Added inline to `Feature.cs` as aliases over existing methods |
| `Feature.IsOutsideVolume` was a stub returning `false` | Replaced with `IsOutsideVolume(Vec3 volumeMin, Vec3 volumeMax)` — real center-point bounds check; `volumeMin`/`volumeMax` threaded through `FeatureSetService.ImportFromFile` and `PopulateFromTable` as optional params |
| `FeatureFactory.FromTableRow` did not exist | `PopulateFromTable` implemented directly — no factory needed |
| `FeatureSetRenderer.AddFeature` and `SpawnFeaturesFromTable` assigned `feature.FeatureSetParent = this` where `this` is a `FeatureSetRenderer` (wrong type, private setter) | Both assignments removed; parent is set by `FeatureSet.Add()` in the domain layer |
| `FeatureMenuController` called `FeatureColor.ToUnityColor()` which did not exist | Replaced with inline `new Color(c.R, c.G, c.B, c.A)` in the caller |

---

## One Remaining Manual Step (Unity Editor)

1. Open the `VolumeDataSetRenderer` prefab in the Unity Editor
2. Clear the `FeatureSetManagerPrefab` field (set to `None`)
3. Delete `Assets/Prefabs/FeatureSet/FeatureSetManager.prefab`

---

## Known Issues (Non-Blocking)

| Issue | Location | Priority |
|---|---|---|
| `DataAnalysisStatisticsProvider.GetStatistics()` body is fully commented out — returns all-zero statistics silently. `SourceStats` has no W50 field; the old `W50 = stats.veloVsys` note was wrong (`veloVsys` is systemic velocity). Comment updated; confirm correct mapping with WP2. | `FeatureVisualiser.cs` | Medium — wire to `DataAnalysis.GetSourceStats` once native binding confirmed |
| `NullFeaturePersistenceService` is the only `IFeaturePersistenceService` implementation — export writes nothing and returns `string.Empty`. Callers must guard `if (!string.IsNullOrEmpty(path))` before displaying the path. | `FeatureVisualiser.cs` | WP7 dependency |
| `PopulateFromTable` handles box and cartesian coordinates; sky-coordinate transforms (Ra/Dec + Velo/Freq/Redshift) require `AstTool` — loader must pre-transform to pixel space | `FeatureSetService.cs` | WP2 dependency — comment in code documents the contract |
| `FeatureSetService.ImportFromFile` uses the new `IsOutsideVolume(volumeMin, volumeMax)` path; callers must pass the volume dimensions when `excludeExternal = true`. Current callers all go through `FeatureVisualiser.ImportFeatureSetFromTable` → `SpawnFeaturesFromTable` (which has its own bounds check) so this is only relevant once WP2 connects a real `IFeatureTableLoader`. | `FeatureSetService.cs` | WP2 dependency |
| `VoTableSaver.SaveFeatureSetAsVoTable` still takes `FeatureSetRenderer` (old Unity coupling) | `VoTable.cs:430` | Replace with `IVoTableExporter` when WP7 migration lands |
| `Graphics.DrawProceduralNow` is deprecated and removed in Unity 6 | `FeatureSetRenderer.cs:562`, `VolumeDataSetRenderer.cs:1146`, `CatalogDataSetRenderer.cs:677` | Pre-existing; WP3 concern |
| CK metrics Day 2 baseline and Day 13 final columns are marked `_to be filled_` | Both `ck-metrics.md` files | Needs Understand / SonarQube run |
| ADR files (ADR-001 through ADR-008) are referenced in CK metrics docs but do not exist anywhere | `refactoring-examples/sub-team-5/*/ck-metrics.md` | Documentation gap |

---

## Post-Audit Fixes (2026-05-28)

A code and diagram audit identified the following issues, all now resolved on this branch.

### Code fixes

| File | Issue | Fix |
|---|---|---|
| `Feature.cs` | `IsOutsideVolume()` was a stub returning `false` — `excludeExternal` filtering was silently broken | Replaced with `IsOutsideVolume(Vec3 volumeMin, Vec3 volumeMax)` implementing a real center-point bounds check |
| `FeatureSetService.cs` | `ImportFromFile` and `PopulateFromTable` passed no bounds to `IsOutsideVolume` | Added `Vec3 volumeMin = default, Vec3 volumeMax = default` optional parameters threaded through both methods |
| `FeatureVisualiser.cs` | `DataAnalysisStatisticsProvider` comment said `W50 = stats.veloVsys` — `veloVsys` is systemic velocity, not W50 | Comment corrected; `SourceStats` documented as having no W50 field |
| `FeatureVisualiser.cs` | `NullFeaturePersistenceService.ExportToVoTable` returned `string.Empty` with no warning to callers | Added XML doc on the null stub explaining the empty return and required caller guard |

### Diagram fixes — `refactoring-examples/sub-team-5/`

| Diagram | Issue | Fix |
|---|---|---|
| `example-1/diagram.puml` | `MomentMapRendererAdapter o--> IMomentMapService : <<inject>>` — code uses `new MomentMapService(this)` in `Awake`, not injection | Changed to `--> IMomentMapService : <<creates in Awake>>` |
| `example-1/diagram.puml` | `MomentMapRendererAdapter` missing `_kernelIndex`, `_kernelIndexMasked`, `_threadGroupX/Y`, `_momentMapMenuController`, `PixelsToTexture2D`, `ConvertFloatArrayToBytes` | All added to the class node |
| `example-1/diagram.puml` | `[RequireComponent(typeof(VolumeDataSetRenderer))]` scene dependency not shown | Added "Scene dependencies" note to the adapter |
| `example-2/diagram.puml` | `IFeaturePersistenceService ..> IVoTableExporter/ICoordinateTransformer : <<inject, uses>>` — an interface cannot inject; concrete class was missing from diagram | Added `FeaturePersistenceService` class to `iDaVIE.Infrastructure.Persistence`; injection arrows now originate from it |
| `example-2/diagram.puml` | `FeatureSetService --> FeatureCatalog` plain association — field is injected composition | Changed to `o--> FeatureCatalog : <<inject>>` |

### Documentation fixes

| File | Issue | Fix |
|---|---|---|
| `example-2/after/FeatureCatalog_after_excerpt.cs` | Declared `sealed partial class` but the production `FeatureCatalog.cs` is not `partial`; excerpt also duplicates fields and members | Added prominent XML doc block warning this is documentation-only and must never be moved into `Assets/` |
| `example-2/after/IAstFrame.cs` | Interface declared in two places — example copy silently diverges if the production file changes | Added header comment pointing to the authoritative production file |

---

## Namespace Layout (ADR-008)

Sub-team 5's new classes use the target namespaces from the assignment brief. Pre-existing types (`Feature`, `FeatureSet`, `FeatureSetRenderer`, `Vec3`, `FeatureColor`, etc.) remain in `DataFeatures` to avoid breaking the full codebase — migrating those is out of scope for a refactoring proposal.

| Namespace | Contains |
|---|---|
| `iDaVIE.Domain.Feature` | `FeatureCatalog`, `IAstFrame`, `ICoordinateTransformer`, `IFeaturePersistenceService`, `IFeatureRenderer`, `IVoTableExporter` |
| `iDaVIE.Application.Feature` | `FeatureSetService`, `IFeatureTableLoader`, `IFeatureStatisticsProvider`, `FeatureStatistics` |
| `iDaVIE.Infrastructure.Persistence` | `VoTableExportService` |
| `iDaVIE.Infrastructure.Unity` | `FeatureVisualiser`, null-object stubs |
| `DataFeatures` (unchanged) | Pre-existing domain and rendering types |

---

## Architecture at a Glance

```
VolumeDataSetRenderer
└── FeatureVisualiser  [iDaVIE.Infrastructure.Unity]  ← Unity entry point; always use this
    ├── .Catalog  (FeatureCatalog)  [iDaVIE.Domain.Feature]       ← domain registry; four set lists
    ├── .Service  (FeatureSetService) [iDaVIE.Application.Feature] ← selection, import, export, user-set
    └── [FeatureSetRenderer children]                              ← GPU rendering, one per FeatureSet
```

**Rules:**
- Reach `FeatureVisualiser` via `VolumeDataSetRenderer.FeatureVisualiser`
- All domain calls go through `.Service` or `.Catalog`
- Subscribe to `FeatureSetService.FeatureSelectionChanged` — do not poll

---

## Test Project

`tests/SubTeam5_Tests/UnitTest1.cs` — 3 NUnit test classes, 15 parameterised cases covering centroid-in-bounds, non-negative flux, and W20 ≥ W50. All tests construct `Feature` objects directly with no Unity runtime required.
