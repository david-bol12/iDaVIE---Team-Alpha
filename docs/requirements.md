# Sub-team 3: Rendering Engine — Requirements Document
**Team Alpha | Cache Me If You Can**
*Sprint 1 | 20 May 2026*
*Brief reference: Section 6.3 Requirements Engineering + Section 9.2 Deliverable 1*

---

## 1. Scope

This document defines the requirements that govern Sub-team 3's refactoring proposal for the
iDaVIE volume rendering layer. The primary subject of refactoring is `VolumeDataSetRenderer`,
a monolithic ~1 400-line Unity C# class that is currently responsible for shader/material
management, 3D texture upload and caching, camera matrix calculations, and foveated sampling
decisions.

The scope of this document is limited to the rendering subsystem. It does not cover data
ingest (Sub-team 2), user interaction or gaze tracking (Sub-team 4), or the wider application
shell. All requirements listed here **must be preserved** by the proposed refactoring; any
design choice that violates them is invalid.

---

## 2. Current Invariants

The following constraints are **non-negotiable**. They describe observable behaviour or hard
platform limits that must survive any restructuring of the rendering code.

| ID | Invariant | Rationale / Source |
|----|-----------|--------------------|
| INV-01 | Minimum sustained frame rate of **90 fps** on the reference VR test machine with the default volume loaded | VR comfort; below 90 fps causes nausea for users |
| INV-02 | Total 3D texture memory must not exceed **4 GB** (4,294,967,296 bytes) | Unity engine hard texture limit |
| INV-03 | Default volume cube memory budget is **368 MB** | Hardcoded constant in iDaVIE codebase |
| INV-04 | Texture filtering must use **nearest-neighbour (blocky)** mode | Astronomical data integrity — interpolation distorts voxel values |
| INV-05 | Foveated rendering must remain fully operational after refactoring | Existing shipped feature; required for 90 fps at acceptable resolution |
| INV-06 | All three mask modes (Apply, Inverse, Isolate) must produce identical output to the current implementation | Correctness — astronomers rely on mask region isolation |

---

## 3. Functional Requirements

| ID | Requirement |
|----|-------------|
| FR-01 | The renderer shall visualise a 3D FITS data cube by ray-marching through a GPU-resident 3D texture, accumulating colour and opacity per sample via the active colour map. |
| FR-02 | The renderer shall support three mask modes — **Apply** (show only masked voxels), **Inverse** (show only unmasked voxels), and **Isolate** (masked region at full opacity, rest at reduced opacity) — selectable at runtime without restarting the render pipeline. |
| FR-03 | The renderer shall apply a configurable colour map to map normalised voxel intensity values to RGBA output. Colour map changes must take effect within one rendered frame. |
| FR-04 | The renderer shall adjust the per-ray sample rate based on the user's gaze direction, using a higher rate at the gaze focus point and a lower rate at the periphery (foveated rendering). |
| FR-05 | The renderer shall respect the 368 MB default texture memory budget, evicting or re-using GPU texture slots when the budget would be exceeded. |
| FR-06 | The renderer shall expose a render-pipeline abstraction (`IRenderPipeline`) such that the domain rendering core does not transitively import `UnityEngine.Rendering.Universal` or `UnityEngine.Rendering.HighDefinition` types. |
| FR-07 | The renderer shall accept volume data from Sub-team 2 via a `RawVolumeData` struct and shall upload that data to the GPU as a `Texture3D` asset. |
| FR-08 | The renderer shall accept gaze direction from Sub-team 4 via an `IGazeProvider` interface. When gaze is unavailable (`IGazeProvider.IsGazeAvailable == false`), the renderer shall fall back to a uniform sample rate. |

---

## 4. Non-Functional Requirements

| ID | Requirement | ISO 25010 Quality | Acceptance Criterion |
|----|-------------|-------------------|----------------------|
| NFR-01 | Each new domain class (`VolumeMaterialBinder`, `VolumeTextureManager`, `VolumeCameraDriver`, `FoveatedSamplingPolicy`) must have **WMC ≤ 20** | Analysability | Measured in Understand tool; reported in metrics worksheet |
| NFR-02 | Each new domain class must have **CBO ≤ 14** | Modularity | Measured in Understand tool |
| NFR-03 | Each new domain class must have **LCOM ≤ 0.5** | Modularity | Measured in Understand tool |
| NFR-04 | There must be **no circular dependencies** between any rendering components | Modularity | Verified by NDepend rules; zero violations permitted |
| NFR-05 | All rendering domain logic must be **unit-testable without a Unity runtime context** | Testability | At least one passing test double per interface demonstrated in worked example |
| NFR-06 | Adding a new mask mode must require only adding a new class implementing `IMaskMode`; **no existing class may be modified** | Modifiability (OCP) | Demonstrated in refactoring example 2 |
| NFR-07 | Migrating from URP to HDRP (or vice versa) must be achievable by **swapping one adapter class** and touching no domain code | Modifiability | Demonstrated via `IRenderPipeline` adapter design in design document |
| NFR-08 | Every public API boundary between rendering sub-components must be **expressed as an interface** | Testability | Verified by code review; all cross-component calls go through interfaces |
| NFR-09 | **DIT ≤ 4** for all new classes | Analysability | Measured in Understand tool |
| NFR-10 | **RFC ≤ 50** for all new domain classes | Analysability | Measured in Understand tool |

---

## 5. Future Requirements (Must Not Be Precluded)

The following features are planned but out of scope for the current refactoring sprint. The
proposed architecture must not make them infeasible.

| ID | Future Feature | Required Architectural Openness |
|----|---------------|----------------------------------|
| FUT-01 | **Iso-contour / iso-surface rendering** | `IMaskMode` interface must be extensible (new implementation, no change to existing code); `IRenderPipeline` must support additional render pass types |
| FUT-02 | **Multi-cube data** (side-by-side volumes) | `VolumeTextureManager` design must not assume a singleton texture slot; multi-slot management must be an additive change |
| FUT-03 | **Time-series scrubbing** (animated FITS cubes) | `VolumeTextureManager` must support streaming/swapping texture data per frame without a full GPU re-upload if the voxel layout is unchanged |

---

## 6. Inter-Sub-team Dependencies

| ID | Dependency | Direction | Status |
|----|-----------|-----------|--------|
| DEP-01 | `RawVolumeData` struct — voxel layout, normalisation range, `TextureFormat` field | Sub-team 2 → Sub-team 3 | ⏳ Contract TBC |
| DEP-02 | `IGazeProvider` interface — `GazeDirection`, `GazeFocusPoint`, `IsGazeAvailable` members | Sub-team 4 → Sub-team 3 | ⏳ Contract TBC |
| DEP-03 | Camera state (clip planes, projection matrix, view matrix) | Sub-team 3 → Sub-team 4 | Sub-team 4 consumes via `VolumeCameraDriver` public interface |

Until DEP-01 and DEP-02 are finalised, Sub-team 3 will use stub implementations of both
contracts in the worked refactoring examples.

---

## 7. Constraints and Assumptions

**Platform constraints**
- Target runtime: Unity 6 (current codebase uses Unity 5-era rendering patterns)
- VR stack: SteamVR (current); Unity XR Toolkit is the target direction
- Render pipeline: Built-in RP (current) → URP or HDRP (target)
- Language: C# managed code; native DLL surface area must not grow

**Project constraints**
- This is a *design-only* refactoring proposal. No production code is changed.
- All before/after code fragments are illustrative C# pseudocode grounded in the real codebase.
- CK metrics for the "after" state are projected estimates derived from the proposed class designs; they will be labelled clearly as projected in the metrics worksheet.

**Assumptions**
- The 90 fps invariant is benchmarked on the project's agreed reference machine specification. If that machine is not available, the invariant is verified by extrapolation from profiler data.
- `RawVolumeData` delivers voxels pre-normalised to the 0–1 range; range rescaling is Sub-team 2's responsibility.
- Eye-tracking hardware is present and functional on the reference test machine; the `IGazeProvider.IsGazeAvailable` fallback covers the case where it is not.

---

## 8. Traceability Matrix

| Requirement | Maps to brief section | Verified by |
|-------------|----------------------|-------------|
| INV-01 – INV-06 | Section 6.3 — Current invariants; Section 4.1 performance budgets | Profiler output referenced in metrics worksheet |
| FR-01 – FR-08 | Section 6.3 — Behavioural requirements | Worked refactoring examples; design document |
| NFR-01 – NFR-03, NFR-09 – NFR-10 | Section 7.1 — CK metrics targets | `docs/metrics-worksheet.md` Day 13 projected snapshot |
| NFR-04 | Section 4.2 — Architectural constraint: no circular dependencies | NDepend rules; architecture diagram |
| NFR-05, NFR-08 | Section 4.2 — Every public API must be an interface with a test double | `docs/test-strategy.md`; worked examples |
| NFR-06 | Section 4.2 — OCP; Section 5.2 GRASP patterns | Refactoring example 2 (Mask Mode Strategy) |
| NFR-07 | Section 6.3 — Render pipeline abstraction | `docs/rendering-layer-design.md`; IRenderPipeline adapter design |
| FUT-01 – FUT-03 | Section 6.3 — Future requirements | Architecture diagram; design rationale in design document |
| DEP-01 – DEP-03 | Section 8 — Inter-sub-team contracts | Interface definitions in design document; stub implementations in worked examples |
