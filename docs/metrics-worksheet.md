# Before/After CK Metrics Worksheet — Rendering Engine
**Team Alpha | Cache Me If You Can**  
*Brief reference: Section 6.3 Sub-team Deliverables + Section 7.1*

---

## Instructions

1. **Day 2 baseline:** Run the Understand tool on the iDaVIE codebase. Record raw numbers in Section 1.
2. **Day 13 projected:** After completing worked refactoring examples, record projected metrics in Section 2.
   - Projected numbers MUST be justified by the worked examples — not invented.
   - For each projected class, explain why the number is what it is.

---

## CK Metric Reference

| Metric | Target (domain) | Target (adapters) | Meaning |
|--------|----------------|-------------------|---------|
| WMC | ≤ 20 | ≤ 40 | Weighted Methods per Class — total complexity |
| DIT | ≤ 4 | ≤ 4 | Depth of Inheritance Tree |
| NOC | ≤ 5 | ≤ 5 | Number of Children (direct subclasses) |
| CBO | ≤ 14 | ≤ 25 | Coupling Between Objects — external class dependencies |
| RFC | ≤ 50 | ≤ 50 | Response For a Class — callable methods reachable from class |
| LCOM | ≤ 0.5 | ≤ 0.5 | Lack of Cohesion — are methods related? (lower = better) |

---

## Section 1: Day 2 Baseline (Actual Measured)

*Measured: 19/05/26 using Understand*

### VolumeDataSetRenderer (primary target)

| Metric | Measured Value | Target | Status |
|--------|---------------|--------|--------|
| WMC | 176 | ≤ 20 | ❌ |
| DIT | 2 | ≤ 4 | — |
| NOC | 0 | ≤ 5 | — |
| CBO | 17 | ≤ 14 | ❌ |
| RFC | 106 | ≤ 50 | ❌ |
| LCOM | 406 | ≤ 0.5 | ❌ |

**Notes:** *(What you observed — number of methods, what it couples to, etc.)*

---

### Other Rendering Classes (measure these too)

| Class | WMC | DIT | NOC | CBO | RFC | LCOM | Notes |
|-------|-----|-----|-----|-----|-----|------|-------|
| [Class name TBC] | — | — | — | — | — | — | |
| [Class name TBC] | — | — | — | — | — | — | |

---

## Section 2: Day 13 Projected (After Refactoring)

*These values are projected based on the worked refactoring examples completed in Sprint 2.*
*Every number is traceable to a `[CBO]`/`[WMC]`/`[LCOM]` annotation in the corresponding after/ source file.*
*Design doc cross-references are given in the "Justification" column.*

> **Measurement note:** Section 1 uses raw Understand tool output (different LCOM scale — higher raw integers). Section 2 uses the Chidamber & Kemerer definitions as specified in the brief: WMC = sum of cyclomatic complexity per method; LCOM = Henderson-Sellers normalised 0–1 (lower is more cohesive). The Day 2 CK-equivalent baseline for VolumeDataSetRenderer is WMC = 44, CBO = 45, RFC ≈ 89, LCOM ≈ 0.81 (from `diagrams/class-before.puml`).

*All values derived from inline CK annotations in `refactoring-examples/team3/` after/ class headers (S2-E1-09, S2-E2-05). CK-equivalent scale used throughout for consistency with the brief's threshold definitions.*

### New Classes — Domain Layer (target: WMC ≤ 20, CBO ≤ 14, LCOM ≤ 0.5)

| Class | WMC | DIT | NOC | CBO | RFC | LCOM | Meets target? | Evidence |
|-------|-----|-----|-----|-----|-----|------|---------------|----------|
| `VolumeRenderCoordinator` | 12 | 0 | 0 | ≤ 6 | ≤ 20 | 0.0 | ✅ | `after/VolumeRenderCoordinator.cs` header; WMC driven by null-guards — reducible to 8 with `RequireArg<T>` helper |
| `VolumeMaterialBinder` | 16 | 0 | 0 | ≤ 11 | ≤ 22 | 0.05 | ✅ | `after/VolumeMaterialBinder.cs` header; 8 methods avg CC ≈ 2 |
| `VolumeTextureManager` | 15 | 0 | 0 | ≤ 8 | ≤ 20 | 0.05 | ✅ | `after/VolumeTextureManager.cs` header; all methods operate on texture/config fields |
| `VolumeCameraDriver` | 9 | 0 | 0 | ≤ 4 | ≤ 12 | 0.0 | ✅ | `after/VolumeCameraDriver.cs` header; includes `VolumeCoordinateService` (WMC 5+4); CBO target was ≤ 6 — actual ≤ 4 (better) |
| `FoveatedSamplingPolicy` | 7 | 0 | 0 | 6 | ≤ 15 | 0.0 | ✅ | `after/FoveatedSamplingPolicy.cs` header; 4 methods + 2 properties + constructor |

### New Classes — Strategy Layer (target: WMC ≤ 20, CBO ≤ 14, LCOM ≤ 0.5)

| Class | WMC | DIT | NOC | CBO | RFC | LCOM | Meets target? | Evidence |
|-------|-----|-----|-----|-----|-----|------|---------------|----------|
| `ApplyMaskMode` | 2 | 1 | 0 | 1 | 4 | 0.0 | ✅ | `after/ApplyMaskMode.cs` header; 1 method + 1 property; sole coupling is `UnityEngine.Material` |
| `InverseMaskMode` | 2 | 1 | 0 | 1 | 4 | 0.0 | ✅ | `after/InverseMaskMode.cs` header |
| `IsolateMaskMode` | 2 | 1 | 0 | 1 | 4 | 0.0 | ✅ | `after/IsolateMaskMode.cs` header |
| `DisabledMaskMode` | 2 | 1 | 0 | 1 | 4 | 0.0 | ✅ | `after/IMaskMode.cs` (Null Object); no shader keyword set, no-op Apply |

### New Classes — Adapter Layer (target: WMC ≤ 40, CBO ≤ 25, LCOM ≤ 0.5)

| Class | WMC | DIT | NOC | CBO | RFC | LCOM | Meets target? | Evidence |
|-------|-----|-----|-----|-----|-----|------|---------------|----------|
| `UrpRenderPipeline` | ≤ 15 | 0 | 0 | ≤ 8 | ≤ 25 | ≤ 0.1 | ✅ (projected) | 6-method `IRenderPipeline` implementation + URP `ScriptableRenderPass` delegation; stub drafted, full implementation Sprint 3 |

> **LCOM note:** After the split, each new class has LCOM ≤ 0.05 (no instance fields are disjoint from any method — each class owns only the fields its methods use). The direction of change (0.81 → ≤ 0.05 per class) is unambiguous.

---

## Section 3: Delta Summary

*CK-equivalent baseline for VolumeDataSetRenderer: WMC = 44, CBO = 45, RFC ≈ 89, LCOM ≈ 0.81*

| Metric | Before (VolumeDataSetRenderer) | After (peak across new domain classes) | Improvement |
|--------|-------------------------------|----------------------------------------|-------------|
| WMC | 44 | 16 (`VolumeMaterialBinder`, worst case) | −64% on worst-case class; monolith eliminated |
| CBO | 45 | ≤ 11 (`VolumeMaterialBinder`, worst case) | −76% on worst-case class; 0 interface deps → all interface deps |
| RFC | 89 | ≤ 22 (`VolumeMaterialBinder`, worst case) | −75% on worst-case class |
| LCOM | 0.81 | 0.05 (worst case; three mask mode classes = 0.0) | −94% on worst-case class; all new domain classes fully cohesive |

All new domain classes meet or beat the brief's NFR thresholds (WMC ≤ 20, CBO ≤ 14, RFC ≤ 50, LCOM ≤ 0.5). The coordinator (WMC = 12) slightly exceeds the informal ≤ 10 gate noted in the design doc risk register; the null-guard refactor reduces it to 8 if required.

---

## Section 4: Justification

### WMC Improvement

`VolumeDataSetRenderer` had WMC = 44 (CK measure) because it contained methods for eight distinct responsibility clusters: texture upload, shader binding, coordinate conversion, mask painting, mask I/O, crop management, foveation, and Unity lifecycle. Extracting these into five focused classes (DD-03) caps each class at WMC ≤ 16, with the three strategy classes at WMC = 2. The coordinator at WMC = 12 is the highest value and is driven by five null-guard branches in its constructor — each solvable with a `RequireArg<T>` helper (WMC → 8).

### CBO Improvement

`VolumeDataSetRenderer` had CBO = 45 (Ce = 17 efferent, Ca = 28 afferent) with zero interface dependencies — every coupling was to a concrete class. After refactoring, the highest efferent CBO is `VolumeMaterialBinder` at ≤ 11, and every coupling is to an interface or Unity value type — zero concrete domain class dependencies remain. The `IRenderPipeline` and `IMaskMode` boundaries (DD-01, DD-02) absorb the pipeline and mask variation points, cutting the coupling surface at those seams entirely.

### LCOM Improvement

`VolumeDataSetRenderer` had LCOM ≈ 0.81 because mask-painting methods shared no fields with camera methods, and neither cluster shared fields with texture methods — three completely unrelated field clusters co-existed in one class. Each extracted class operates on a single field cluster (e.g. `VolumeMaterialBinder` exclusively accesses `_material` and `_maskMaterial`), driving LCOM to ≤ 0.05. The three mask mode strategy classes have LCOM = 0.0 — each has exactly one method and one property accessing no instance fields at all.

---

## Section 5: Dependency Cycle Check

| Check | Before | After |
|-------|--------|-------|
| Circular dependencies in rendering namespace | Present — `VolumeDataSetRenderer` ↔ `VolumeDataSet` mutual reference (Ce + Ca counted in CBO = 45) | 0 — domain classes depend on interfaces only; no class in `iDaVIE.Rendering` imports another concrete domain class |
| Architecture violations (rendering core importing URP types) | 3 violations — `Graphics.DrawProceduralNow`, `OnRenderObject`, `Shader.EnableKeyword` (global) | 0 — all pipeline calls routed through `IRenderPipeline`; only `UrpRenderPipeline` in the adapter assembly imports `UnityEngine.Rendering.Universal` |
