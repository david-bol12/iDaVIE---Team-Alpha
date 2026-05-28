# Sub-team 5 — Feature System Handoff

**Branch:** `team5` | **Updated:** 2026-05-28

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
| `Feature.BoundsContains`, `BoundsVolume`, `IsOutsideVolume` missing | Added inline to `Feature.cs` as aliases over existing methods |
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
| `DataAnalysisStatisticsProvider.GetStatistics()` body is fully commented out — returns all-zero statistics silently | `FeatureVisualiser.cs` | Medium — wire to `DataAnalysis.GetSourceStats` once native binding confirmed |
| `NullFeaturePersistenceService` is the only `IFeaturePersistenceService` implementation — export currently writes nothing | `FeatureVisualiser.cs` | WP7 dependency |
| `PopulateFromTable` handles box and cartesian coordinates; sky-coordinate transforms (Ra/Dec + Velo/Freq/Redshift) require `AstTool` — loader must pre-transform to pixel space | `FeatureSetService.cs` | WP2 dependency — comment in code documents the contract |
| `VoTableSaver.SaveFeatureSetAsVoTable` still takes `FeatureSetRenderer` (old Unity coupling) | `VoTable.cs:430` | Replace with `IVoTableExporter` when WP7 migration lands |
| `Graphics.DrawProceduralNow` is deprecated and removed in Unity 6 | `FeatureSetRenderer.cs:562`, `VolumeDataSetRenderer.cs:1146`, `CatalogDataSetRenderer.cs:677` | Pre-existing; WP3 concern |
| CK metrics Day 2 baseline and Day 13 final columns are marked `_to be filled_` | Both `ck-metrics.md` files | Needs Understand / SonarQube run |
| ADR files (ADR-001 through ADR-008) are referenced in CK metrics docs but do not exist anywhere | `refactoring-examples/sub-team-5/*/ck-metrics.md` | Documentation gap |

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
