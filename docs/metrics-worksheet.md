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

| Class | WMC | DIT | NOC | CBO | RFC | LCOM | Meets target? |
|-------|-----|-----|-----|-----|-----|------|---------------|
| `VolumeRenderCoordinator` | — | — | — | — | — | — | — |
| `VolumeMaterialBinder` | — | — | — | — | — | — | — |
| `VolumeTextureManager` | — | — | — | — | — | — | — |
| `VolumeCameraDriver` | — | — | — | — | — | — | — |
| `FoveatedSamplingPolicy` | — | — | — | — | — | — | — |
| `ApplyMaskMode` | — | — | — | — | — | — | — |
| `InverseMaskMode` | — | — | — | — | — | — | — |
| `IsolateMaskMode` | — | — | — | — | — | — | — |
| `URPAdapter` | — | — | — | — | — | — | — |

---

## Section 3: Delta Summary

| Metric | Before (VolumeDataSetRenderer) | After (average across new classes) | Improvement |
|--------|-------------------------------|-----------------------------------|-------------|
| WMC | — | — | — |
| CBO | — | — | — |
| RFC | — | — | — |
| LCOM | — | — | — |

---

## Section 4: Justification

### WMC Improvement
*(Explain: the original class had N methods. We split into M classes each with ≤ P methods.)*

### CBO Improvement
*(Explain: the original class depended on N external classes. After abstraction via IRenderPipeline and IMaskMode, each new class depends on ≤ M.)*

### LCOM Improvement
*(Explain: the original class had methods from 4 unrelated concerns. Each new class has methods serving one concern only, so LCOM drops to near 0.)*

---

## Section 5: Dependency Cycle Check

| Check | Before | After |
|-------|--------|-------|
| Circular dependencies in rendering namespace | — | 0 (required) |
| Architecture violations (rendering core importing URP types) | — | 0 (required) |
