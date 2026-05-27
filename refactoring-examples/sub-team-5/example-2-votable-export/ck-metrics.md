# CK Metrics — Example 2: VOTable Export

Sub-team 5 | iDaVIE Refactoring Assignment | 27 May 2026

Acceptable thresholds from §7.1 of the assignment brief:  
WMC ≤ 20 (domain), CBO ≤ 14 (domain), RFC ≤ 50, LCOM ≤ 0.5

> **Note:** The Day 2 baseline and Day 13 final columns will be populated
> with actual Understand / SonarQube output during Sprint 2.
> Values marked **est.** are projections derived from reading the source.
> LCOM is undefined for static classes (VoTableSaver has no instance state).

---

## Before State

| Class | Role | WMC | CBO | RFC | LCOM | Threshold met? |
|---|---|:---:|:---:|:---:|:---:|:---:|
| `VoTableSaver` (static) | Export — before | est. 8 | est. 12 | est. 18 | n/a | CBO ⚠ high |

**Problems driving the high CBO:**
- Depends on `FeatureSetRenderer` (Unity `MonoBehaviour`)
- Depends on `VolumeDataSetRenderer` (via `featureSet.VolumeRenderer`)
- Depends on `AstTool` (P/Invoke DLL wrapper)
- Depends on `DataAnalysis.SourceStats` (DLL value type)
- Depends on `XDocument`, `XElement` (serialisation)
- No interface — callers directly couple to the static type

---

## After State

| Class | Role | WMC target | CBO target | RFC target | LCOM target | Threshold met? |
|---|---|:---:|:---:|:---:|:---:|:---:|
| `IVoTableExporter` | Export plug-in seam | 1 | 0 | 1 | 0 | ✓ |
| `ICoordinateTransformer` | WCS abstraction | 2 | 0 | 2 | 0 | ✓ |
| `VoTableExportService` | Concrete serialiser | ≤ 6 | ≤ 3 | ≤ 8 | 0 | ✓ |
| `FeatureCatalog` (excerpt) | Domain registry | ≤ 15 | ≤ 8 | ≤ 22 | ≤ 0.30 | ✓ |

---

## Delta Summary

| Metric | VoTableSaver (before) | VoTableExportService (after) | Δ |
|---|:---:|:---:|:---:|
| WMC | est. 8 | ≤ 6 | ↓ |
| CBO | est. 12 | ≤ 3 | ↓ ↓ ↓ |
| RFC | est. 18 | ≤ 8 | ↓ ↓ |
| LCOM | n/a (static) | 0 | — |

The largest gain is in **CBO**. By removing the `FeatureSetRenderer`,
`VolumeDataSetRenderer`, and `AstTool` dependencies and replacing them with
the `FeatureSet` domain object and `ICoordinateTransformer`, the exporter's
coupling drops from ~12 to ≤ 3.

---

## Delegation chain — FeatureCatalog CBO note

`FeatureCatalog` gains one new dependency (`IFeaturePersistenceService`) but
loses its former direct coupling to `VoTableSaver`. Net effect:

| Dependency added | Dependency removed |
|---|---|
| `IFeaturePersistenceService` (interface) | `VoTableSaver` (static class) |
| | `VolumeDataSetRenderer` |
| | `AstTool` |

`FeatureCatalog` CBO target ≤ 8 is achievable because its only dependencies
are now: `FeatureSet`, `Feature`, `IFeaturePersistenceService`, `FeatureColor`,
`FeatureSetType`, `ReadOnlyCollection<T>` (BCL), and `Action<T>` (BCL).
