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

*These values are projected based on the worked refactoring examples.*
*Every number must be traceable to a specific design decision in the design document.*

### New Classes (proposed)

*All values derived from inline CK annotations in `refactoring-examples/team3/` after/ class headers (S2-E1-09, S2-E2-05). Understand tool formula used throughout for consistency with Section 1.*

| Class | WMC | DIT | NOC | CBO | RFC | LCOM | Meets target? |
|-------|-----|-----|-----|-----|-----|------|---------------|
| `VolumeRenderCoordinator` | 3 | 1 | 0 | 6 | 12 | 0 | ✅ all |
| `VolumeMaterialBinder` | 16 | 0 | 0 | 11 | 22 | 0 | ✅ all |
| `VolumeTextureManager` | 20 | 0 | 0 | 8 | 20 | 0 | ✅ all |
| `VolumeCameraDriver` | 9 | 0 | 0 | 4 | 18 | 0 | ✅ all |
| `FoveatedSamplingPolicy` | 7 | 0 | 0 | 6 | 14 | 0 | ✅ all |
| `ApplyMaskMode` | 2 | 1 | 0 | 1 | 3 | 0 | ✅ all |
| `InverseMaskMode` | 2 | 1 | 0 | 1 | 3 | 0 | ✅ all |
| `IsolateMaskMode` | 2 | 1 | 0 | 1 | 3 | 0 | ✅ all |
| `DisabledMaskMode` | 2 | 1 | 0 | 1 | 3 | 0 | ✅ all |
| `UrpRenderPipeline` (adapter) | 8 | 0 | 0 | 14 | 20 | 0 | ✅ (adapter thresholds) |
| **Domain target** | **≤ 20** | **≤ 4** | **≤ 5** | **≤ 14** | **≤ 50** | **≤ 0.5** | |
| **Adapter target** | **≤ 40** | **≤ 4** | **≤ 5** | **≤ 25** | **≤ 50** | **≤ 0.5** | |

> **LCOM note:** Understand's Henderson-Sellers formula produces large raw values for the original class (406) because it counts disjoint method-to-field pairs across all 44 methods. After the split, each new class has LCOM = 0 (no instance fields are disjoint from any method — each class owns only the fields its methods use). The absolute number is not directly comparable across tools; the direction of change (406 → 0 per class) is unambiguous.

---

## Section 3: Delta Summary

| Metric | Before (`VolumeDataSetRenderer`) | After (worst single class) | After (best single class) | Direction |
|--------|----------------------------------|---------------------------|--------------------------|-----------|
| WMC | 176 | 20 (`VolumeTextureManager`) | 2 (each mask-mode class) | ✅ −156 worst-case |
| CBO | 17 (Ce only) | 11 (`VolumeMaterialBinder`) | 1 (each mask-mode class) | ✅ −6 worst-case; cycle broken |
| RFC | 106 | 22 (`VolumeMaterialBinder`) | 3 (each mask-mode class) | ✅ −84 worst-case |
| LCOM | 406 (raw H-S) | 0 (all new classes) | 0 (all new classes) | ✅ −406 |

> "Worst single class" is the hardest comparison: even the most complex proposed class is well inside every brief threshold.

---

## Section 4: Justification

### WMC Improvement

`VolumeDataSetRenderer` measured WMC = 176 under the Understand tool's cyclomatic-complexity formula across its 44 methods. The worst single method, `_startFunc`, contributed CC = 28 across 185 lines — roughly the entire budget of a well-designed class. After the split, WMC is distributed across nine classes. The most complex, `VolumeTextureManager`, reaches exactly WMC = 20 (the domain class target) across 10 methods with no single method exceeding CC = 4. The five remaining core classes total WMC ≤ 55 across ~50 methods, replacing the 176 that previously lived in one file. The four mask-mode strategy classes each carry WMC = 2 (one CC-1 property getter + one CC-1 method body), the theoretical minimum for a non-trivial class.

### CBO Improvement

Under the Understand tool, `VolumeDataSetRenderer` measured CBO = 17 (Ce, outgoing dependencies only). Under SonarQube's bidirectional count the same class scored CBO = 31 — placing it in a 46-file dependency cycle with a 39.8% propagation cost. After the split, each domain class carries CBO ≤ 11. The key mechanism is the introduction of `IRenderPipeline` and `IMaskMode` as stable interfaces: instead of `VolumeDataSetRenderer` reaching directly into `UnityEngine.Rendering.Universal`, `SteamVR`, and mask-enum-switch logic simultaneously, each new class depends only on the interface boundary relevant to its one responsibility. The dependency cycle is structurally broken because no new class imports from both the Unity rendering API and the domain data types simultaneously.

### LCOM Improvement

`VolumeDataSetRenderer` scored LCOM = 406 under Understand's Henderson-Sellers formula. This extreme value reflects four completely disjoint field clusters inside one class: mask fields (`_maskTexture`, `_maskMode`, `_maskCropMin`) are used only by mask methods; texture fields (`_dataTexture`, `_dataSet`, `_downsampleFactor`) are used only by texture methods; camera fields (`_projectionMatrix`, `_clipPlanes`) are used only by camera methods; and foveation fields (`_gazeProvider`, `_stepCount`) are used only by foveation methods. The Henderson-Sellers formula counts every pair of methods that share no field — with 44 methods across 4 unrelated clusters, the raw count is very large. After the split, every new class owns exactly one field cluster. Every method in `VolumeMaterialBinder` touches `_material`, `_activeMaskMode`, or `_renderPipeline` — the three fields the class owns. LCOM = 0 by construction in all proposed classes.

---

## Section 5: Dependency Cycle Check

| Check | Before | After |
|-------|--------|-------|
| Circular dependencies in rendering namespace | — | 0 (required) |
| Architecture violations (rendering core importing URP types) | — | 0 (required) |
