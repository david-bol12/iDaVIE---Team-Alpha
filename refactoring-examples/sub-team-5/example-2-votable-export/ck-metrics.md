# CK Metrics — Example 2: VOTable Export

Sub-team 5 | iDaVIE Refactoring Assignment | 27 May 2026

Acceptable thresholds from §7.1 of the assignment brief:  
WMC ≤ 20 (domain), CBO ≤ 14 (domain), RFC ≤ 50, LCOM ≤ 0.5

> **Note:** The Day 2 baseline and Day 13 final columns will be populated
> with actual Understand / SonarQube output during Sprint 2.
> Values marked **est.** are projections derived from reading the source.
> LCOM is undefined for static classes (VoTableSaver has no instance state).

---

## Namespace Mapping (ADR-008)

| Class | Before namespace | After namespace | Dependency direction |
|---|---|---|---|
| `VoTableSaver` (static) | `VoTableReader` | _removed_ | — |
| `IVoTableExporter` | _new_ | `iDaVIE.Domain.Feature` | inward (domain boundary) |
| `IAstFrame` | _new_ | `iDaVIE.Domain.Feature` | inward (domain boundary) |
| `ICoordinateTransformer` | _new_ | `iDaVIE.Domain.Feature` | inward (domain boundary) |
| `IFeaturePersistenceService` | `DataFeatures` | `iDaVIE.Domain.Feature` | inward (domain boundary) |
| `FeatureCatalog` | `DataFeatures` | `iDaVIE.Domain.Feature` | domain aggregate |
| `VoTableExportService` | `DataFeatures` | `iDaVIE.Infrastructure.Persistence` | depends inward on Domain ✓ |
| `AstFrameHandle` | _new_ | `iDaVIE.Infrastructure.NativePlugins` | wraps IntPtr, implements IAstFrame |
| `FeatureSetService` | `DataFeatures` | `iDaVIE.Application.Feature` | orchestrates domain |

---

## Before State

| Class | Role | WMC | CBO | RFC | LCOM | Threshold met? |
|---|---|:---:|:---:|:---:|:---:|:---:|
| `VoTableSaver` (static) | Export — before | est. 8 | est. 12 | est. 18 | n/a | CBO ⚠ high |

**Problems driving the high CBO:**
- Depends on `FeatureSetRenderer` (Unity `MonoBehaviour`) — ADR-002 violation
- Depends on `VolumeDataSetRenderer` (via `featureSet.VolumeRenderer`) — LoD violation
- Depends on `AstTool` (P/Invoke DLL wrapper) — ADR-002 violation
- Depends on `DataAnalysis.SourceStats` (DLL value type)
- Passes bare `IntPtr` at call sites — ADR-002 / ADR-004 violation
- No interface — callers depend directly on the static type — ADR-003 violation

---

## After State

| Class | Namespace | Role | WMC target | CBO target | RFC target | LCOM target | Threshold met? |
|---|---|---|:---:|:---:|:---:|:---:|:---:|
| `IAstFrame` | `iDaVIE.Domain.Feature` | Opaque handle (replaces IntPtr) | 0 | 0 | 0 | 0 | ✓ |
| `IVoTableExporter` | `iDaVIE.Domain.Feature` | Export plug-in seam | 1 | 0 | 1 | 0 | ✓ |
| `ICoordinateTransformer` | `iDaVIE.Domain.Feature` | WCS abstraction | 2 | 1 | 2 | 0 | ✓ |
| `VoTableExportService` | `iDaVIE.Infrastructure.Persistence` | Concrete serialiser | ≤ 6 | ≤ 3 | ≤ 8 | 0 | ✓ |
| `FeatureCatalog` (excerpt) | `iDaVIE.Domain.Feature` | Domain registry | ≤ 15 | ≤ 8 | ≤ 22 | ≤ 0.30 | ✓ |

**Day 2 baseline (Understand / SonarQube):** _to be filled_  
**Day 13 final (post full-team refactor):** _to be filled_

---

## Delta Summary

| Metric | VoTableSaver (before) | VoTableExportService (after) | Δ |
|---|:---:|:---:|:---:|
| WMC | est. 8 | ≤ 6 | ↓ |
| CBO | est. 12 | ≤ 3 | ↓ ↓ ↓ |
| RFC | est. 18 | ≤ 8 | ↓ ↓ |
| LCOM | n/a (static) | 0 | — |

The largest gain is in **CBO**. By removing `FeatureSetRenderer`,
`VolumeDataSetRenderer`, and the direct `AstTool` + `IntPtr` dependencies,
and replacing them with `FeatureSet` (domain object) and `ICoordinateTransformer`
(injected interface), the exporter's coupling drops from ~12 to ≤ 3.

---

## Delegation chain — FeatureCatalog CBO note

`FeatureCatalog` gains one new dependency (`IFeaturePersistenceService`) but
loses its former direct coupling to `VoTableSaver`. Net effect:

| Dependency added | Dependency removed |
|---|---|
| `IFeaturePersistenceService` (interface) | `VoTableSaver` (static class) |
| | `VolumeDataSetRenderer` |
| | `AstTool` |
| | `IntPtr` at domain call sites |

`FeatureCatalog` CBO target ≤ 8 is achievable because its only dependencies
are: `FeatureSet`, `Feature`, `IFeaturePersistenceService`, `FeatureColor`,
`FeatureSetType`, `ReadOnlyCollection<T>` (BCL), and `Action<T>` (BCL).

---

## ADR-002 IntPtr violation — resolution

| Location | Before | After |
|---|---|---|
| `ICoordinateTransformer.Transform()` first param | `IntPtr astFrame` | `IAstFrame frame` |
| `ICoordinateTransformer.Normalise()` first param | `IntPtr astFrame` | `IAstFrame frame` |
| `FeatureSet.AstFrame` property type | `IntPtr` | `IAstFrame` |
| `VoTableExportService` call sites | pass raw `IntPtr` | pass `IAstFrame` |
| `AstFrameHandle` (new, Infrastructure) | _absent_ | wraps `IntPtr`, implements `IAstFrame` |
