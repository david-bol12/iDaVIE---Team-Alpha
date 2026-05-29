# Sub-team 3: Rendering Engine — Design Document
**Team Alpha | Cache Me If You Can**
*Sprint 2 | 25 May 2026*
*Brief reference: Section 9.2 Deliverable 2 + Section 6.3 (Rendering Layer Design, Shader/Asset Policy, Metrics Worksheet)*

---

## 1. Executive Summary

This document analyses the iDaVIE rendering layer using CK metrics and proposes a refactoring of `VolumeDataSetRenderer` — a 1,403-line God Class (WMC 44, CBO 45) — into five focused collaborating classes behind clean interfaces. No production code is changed; this is a design-only proposal.

---

## 2. Problem Statement

### 2.1 The Core Finding

`VolumeDataSetRenderer` spans 1,403 lines of C# and fails every measurable quality threshold:

| Metric | Day 2 Baseline | Target | Excess |
|--------|---------------|--------|--------|
| WMC (Weighted Methods per Class) | **44** | ≤ 20 | 2.2× over |
| CBO (Coupling Between Objects) | **45** | ≤ 14 | 3.2× over |
| RFC (Response For a Class) | **89** | ≤ 50 | 1.8× over |
| LCOM (Lack of Cohesion in Methods) | **0.81** | ≤ 0.5 | 1.6× over |
| DIT (Depth of Inheritance Tree) | 1 | ≤ 4 | ✅ |
| NOC (Number of Children) | 0 | ≤ 5 | ✅ |

All four failing metrics on the same class is the diagnostic signature of a **God Class**. The worst single method, `_startFunc`, carries CC = 28 across 185 lines. CBO = 45 means 45 other files must be considered on any edit.

### 2.2 Responsibility Inventory

| # | Responsibility | Proposed Owner |
|---|---------------|----------------|
| 1 | Shader keyword and material property management | `VolumeMaterialBinder` |
| 2 | 3D texture upload, caching, memory budget | `VolumeTextureManager` |
| 3 | Camera matrix calculation and clip planes | `VolumeCameraDriver` |
| 4 | Foveated sampling rate decisions | `FoveatedSamplingPolicy` |
| 5 | Mask mode branching | `IMaskMode` strategy implementations |
| 6–8 | Region selection, cursor tracking, FITS I/O | Out of scope for this refactor |

### 2.3 Render Pipeline Lock-In

Three calls in `VolumeDataSetRenderer` are incompatible with Unity 6 URP: `Graphics.DrawProceduralNow` (lines 1148, 1154), `OnRenderObject` (line 1142), and `Shader.EnableKeyword` (lines 1099, 1103). The refactored architecture isolates all pipeline-specific code behind `IRenderPipeline`, making the domain testable without a Unity player context.

---

## 3. Scope

**Sub-team 3 owns:** `VolumeRenderCoordinator`, `VolumeMaterialBinder`, `VolumeTextureManager`, `VolumeCameraDriver`, `FoveatedSamplingPolicy`, `IRenderPipeline`, `IMaskMode` + four implementations, `UrpRenderPipeline`, `HdrpRenderPipeline`, and the test doubles in `iDaVIE.Rendering.Tests`.

**Explicitly out of scope:** FITS data ingest (Sub-team 2), VR interaction and gaze SDK (Sub-team 4), application shell and session save/restore (Sub-team 7). The `RegionSelectionController` and `CursorPaintController` responsibilities in the current monolith are excluded from this sprint.

| Brief §6.3 Component | This Document | Content |
|---|---|---|
| Rendering Layer Design | §5 Design Decisions | DD-01 through DD-04, migration path |
| Shader/Asset Policy | §5.7 | Folder conventions, naming, variant selection |
| Metrics Worksheet | §6 CK Metrics Worksheet | Day 2 baseline, Day 13 projection, delta |

---

## 4. Requirements Recap

**Current invariants (must survive refactoring):** 90 FPS (INV-01), 4 GB texture cap (INV-02), 368 MB volume budget (INV-03), nearest-neighbour filtering (INV-04), foveated rendering (INV-05), mask mode visual correctness (INV-06).

**Key functional requirements:** FR01 ray-march volume visualisation; FR02 three mask modes without pipeline restart; FR03 dynamic colour mapping; FR04 gaze-contingent sampling; FR05 368 MB cache enforcement; FR06 no transitive URP/HDRP imports in core assemblies.

**CK targets per domain class:** WMC ≤ 20, CBO ≤ 14, RFC ≤ 50, LCOM ≤ 0.5, DIT ≤ 4. Zero circular dependencies. All rendering logic unit-testable without Unity runtime.

---

## 5. Design Decisions

### 5.1 Current Architecture (As-Is)

The current structure is a single node coupled to 45 files across 8 packages. The SOLID/GRASP audit (§8) catalogues 17 confirmed violations — 6 Critical, 8 High, 1 Medium.

### 5.2 Target Architecture (To-Be)

`VolumeDataSetRenderer` is replaced by five collaborating classes, each behind its own interface:

| Class | Single Responsibility | Proj. WMC | Proj. CBO |
|---|---|---|---|
| `VolumeRenderCoordinator` | Orchestrates per-frame loop; wires the four domain classes | ≤ 10 | ≤ 6 |
| `VolumeMaterialBinder` | Shader keyword management, material binding, colour-map application | ~16 | ≤ 11 |
| `VolumeTextureManager` | 3D texture upload, LRU caching, 368 MB budget enforcement | ≤ 20 | ≤ 8 |
| `VolumeCameraDriver` | Camera matrix calculation, clip planes, projection mode | ≤ 12 | ≤ 6 |
| `FoveatedSamplingPolicy` | Per-frame sample-rate decision from gaze direction | ≤ 8 | ≤ 6 |

`VolumeRenderCoordinator` is the only class that inherits from `MonoBehaviour` and the only class that knows all four domain classes exist. It contains zero domain logic — all computation is delegated.

### 5.3 DD-01 — Render Pipeline Abstraction (`IRenderPipeline`)

`IRenderPipeline` is a six-member interface in `iDaVIE.Rendering`. The two concrete adapters — `UrpRenderPipeline` and `HdrpRenderPipeline` — are the only files permitted to import `UnityEngine.Rendering.Universal` or HDRP namespaces. A `NullRenderPipeline` test double enables edit-mode unit tests without a GPU.

```csharp
public interface IRenderPipeline {
    void AddCommandBuffer(Camera camera, CameraEvent evt, CommandBuffer buffer);
    void RemoveCommandBuffer(Camera camera, CameraEvent evt, CommandBuffer buffer);
    void SetPipelineKeyword(string keyword, bool enabled);
    bool DepthTextureAvailable { get; }
    void Initialise();
    void Dispose();
}
```

**DIP justification:** High-level policy depends on `IRenderPipeline`, not on `UnityEngine.Rendering.Universal`. Future pipeline migrations touch only the adapter — zero domain changes. This resolves V-10 and V-15 (§8.1).

### 5.4 DD-02 — Mask Mode Strategy Pattern (`IMaskMode`)

The current if/else chain at lines 1072–1094 is an OCP violation (V-04): adding a new mask mode requires editing four files. The Strategy pattern replaces it with a two-member interface:

```csharp
public interface IMaskMode {
    void Apply(Material material, Texture3D maskTexture);
    string ShaderKeyword { get; }
}
```

| Class | Shader Keyword | `_MaskAlpha` | Behaviour |
|---|---|---|---|
| `ApplyMaskMode` | `_MASK_APPLY` | 1.0 | Mask region rendered; outside hidden |
| `InverseMaskMode` | `_MASK_INVERSE` | 1.0 | Outside rendered; mask hidden |
| `IsolateMaskMode` | `_MASK_ISOLATE` | 0.15 | Mask full opacity; outside at 15% |
| `DisabledMaskMode` | *(none)* | 0.0 | Mask texture unbound |

Adding a new mode = one new class, zero existing files changed. Worked example in `refactoring-examples/example2-MaskModes/`.

### 5.5 DD-03 — `VolumeDataSetRenderer` Split (SRP)

Colour-coding analysis of the class body identified four dense, well-separated clusters. Each maps to an extracted class:

| Cluster | Lines (approx.) | Extracted To |
|---|---|---|
| Shader keyword and material property writes | ~180 | `VolumeMaterialBinder` |
| 3D texture allocation, upload, eviction | ~210 | `VolumeTextureManager` |
| Camera matrix, clip planes, projection mode | ~95 | `VolumeCameraDriver` |
| Foveated sample-rate calculation | ~40 | `FoveatedSamplingPolicy` |
| Orchestration shell | Thin | `VolumeRenderCoordinator` |

Each extracted class operates on a single field cluster — the structural guarantee that LCOM approaches zero.

### 5.6 DD-04 — Foveated Rendering Extraction (`FoveatedSamplingPolicy`)

The current code calls the SteamVR gaze API directly inside `Update()` — a DIP breach (V-07). `FoveatedSamplingPolicy` takes an `IGazeProvider` interface at construction. When `IsGazeAvailable == false`, it falls back to a uniform sample rate, preserving INV-01. Sub-team 4 will implement the concrete `SteamVRGazeProvider`; `MockGazeProvider` covers all unit tests until then.

### 5.7 Shader/Asset Organisation Policy

Full policy in `docs/shader-asset-policy.md`. Summary:

- **Folder structure:** All rendering assets under `Assets/Rendering/`. Shader source under `Assets/Rendering/Shaders/Volume/`; colour map LUTs in `Assets/Rendering/Shaders/ColourMaps/`.
- **Naming:** `PascalCase` noun phrases for shaders (e.g. `VolumeRaymarch.shader`); `ColourMap_{Name}.shader` for LUTs.
- **Pipeline variants:** URP and HDRP variants selected at build time by the `IRenderPipeline` adapter — no domain class references the shader asset directly.
- **Variant stripping:** Only the three active mask mode keywords and active colour map variants ship. Variant collection committed to version control; reviewed on any shader keyword change.
- **Runtime vs. baked:** `Texture3D` volumes are runtime-only (never saved as `.asset`). Colour map LUT textures are baked 256×1 RGBA PNGs. `VolumeMaterial.mat` is a committed baked asset.

### 5.8 Migration Path — Strangler Fig (7 Phases)

No big-bang rewrite. Each phase extracts one responsibility; the codebase compiles and runs at 90 FPS after every phase. `VolumeDataSetRenderer` is not deleted until Phase 6 is verified.

| Phase | What Is Extracted | Highest Risk | Rollback Cost |
|-------|------------------|-------------|---------------|
| 0 | Interfaces + test doubles | None | Delete 8 files |
| 1 | Mask mode branching → `IMaskMode` strategies | Low | 1 file restored |
| 2 | Foveation calculation → `FoveatedSamplingPolicy` | Low | 1 file restored |
| 3 | Shader property writes → `VolumeMaterialBinder` | Medium | 1 file restored |
| 4 | Camera matrix math → `VolumeCameraDriver` | Medium | 1 file restored |
| 5 | Texture lifecycle → `VolumeTextureManager` | High | 1 file restored |
| 6 | `VolumeRenderCoordinator` introduced; `VolumeDataSetRenderer` retired | Medium | Restore prefab reference |
| 7 | `UrpRenderPipeline` replaces `BuiltInRenderPipeline` | High | Swap Graphics Settings asset |

---

## 6. CK Metrics Worksheet

*Full table with raw Understand formula values in `docs/metrics-worksheet.md`.*

### 6.1 Day 2 Baseline (Measured)

| Class | WMC | DIT | NOC | CBO | RFC | LCOM |
|-------|-----|-----|-----|-----|-----|------|
| `VolumeDataSetRenderer` | **44** | 1 | 0 | **45** | **89** | **0.81** |
| Target | ≤ 20 | ≤ 4 | ≤ 5 | ≤ 14 | ≤ 50 | ≤ 0.5 |

### 6.2 Day 13 Projection (Proposed)

| Class | WMC | CBO | RFC | LCOM | Meets Target? |
|-------|-----|-----|-----|------|---------------|
| `VolumeRenderCoordinator` | 3 | 6 | 12 | 0.00 | ✅ all |
| `VolumeMaterialBinder` | 16 | 11 | 22 | 0.05 | ✅ all |
| `VolumeTextureManager` | 20 | 8 | 20 | 0.05 | ✅ all |
| `VolumeCameraDriver` | 9 | 4 | 18 | 0.00 | ✅ all |
| `FoveatedSamplingPolicy` | 7 | 6 | 14 | 0.00 | ✅ all |
| `ApplyMaskMode` / `InverseMaskMode` / `IsolateMaskMode` / `DisabledMaskMode` | 2 each | 1 each | 3 each | 0.00 | ✅ all |
| `UrpRenderPipeline` (adapter) | 8 | 14 | 20 | 0.00 | ✅ (adapter ≤ 40/≤ 25) |

### 6.3 Delta Summary

Splitting `VolumeDataSetRenderer` eliminates every CK threshold violation. WMC drops from 44 to ≤ 20 per class. CBO drops from 45 to ≤ 11 per domain class, breaking the 46-file dependency cycle (39.8% propagation cost). LCOM collapses from 0.81 to ≤ 0.05 per class.

---

## 7. Class and Sequence Diagrams

All diagrams are PlantUML source in `diagrams/`:

- **`class-before.puml`** — current `VolumeDataSetRenderer` with 45 couplings annotated.
- **`class-after.puml`** — five-class target architecture with interface boundaries.
- **`architecture.puml`** — component diagram annotating the `IRenderPipeline` boundary and cross-team contracts.
- **`sequence-render-frame.puml`** — 8-step per-frame sequence: Update → camera → foveation → texture → material → mask → pipeline → GPU.

---

## 8. SOLID/GRASP Audit

*Full evidence with code line references: `docs/Codebase Exploration/SOLID_GRASP_Violations.md`*

### 8.1 Violations in Current Code

| ID | Principle | Violation | Severity |
|----|-----------|-----------|----------|
| V-01 | SRP | 9 distinct responsibilities in one class | 🔴 Critical |
| V-04 | OCP | New mask mode requires 4 files, 8 code blocks | 🔴 Critical |
| V-06 | ISP | 152 public members; consumers use only 3–5 | 🔴 Critical |
| V-07 | DIP | `Config.Instance` singleton accessed directly | 🟠 High |
| V-08 | DIP | `FindObjectOfType<VolumeInputController>()` — concrete scene-search | 🟠 High |
| V-10 | DIP | `transform.InverseTransformPoint()` inside domain method | 🔴 Critical |
| V-13 | GRASP Controller | `CropToRegion()` spans validation, data load, material update, outline update | 🔴 Critical |
| V-14 | GRASP Indirection | No abstraction layer between Unity lifecycle hooks and domain logic | 🔴 Critical |
| V-15 | GRASP Prot. Variations | Render pipeline, input provider, data format have zero interface protection | 🔴 Critical |
| V-16 | GRASP Low Coupling | CBO ~45; 12 concrete dependencies, 0 interface dependencies | 🔴 Critical |
| V-17 | GRASP High Cohesion | LCOM ~0.81; unrelated field clusters share one class | 🔴 Critical |

*6 Critical, 8 High, 1 Medium total — see `SOLID_GRASP_Violations.md` for full 17-row table including V-02, V-03, V-05, V-09, V-11, V-12.*

### 8.2 Fixes in Proposed Design

| Violation | Resolved By | How |
|-----------|------------|-----|
| V-01 (SRP) | DD-03 | Four-class split; each class has one reason to change |
| V-04 (OCP) | DD-02 | `IMaskMode` Strategy — new mode = new class only |
| V-06 (ISP) | DD-03 | Four narrow interfaces (≤ 7 members each) |
| V-07/V-08/V-09 (DIP) | DD-03 | All collaborators injected at composition root |
| V-10 (DIP) | DD-01 + DD-03 | `VolumeCameraDriver` takes `Matrix4x4`; `IRenderPipeline` removes `UnityEngine.Rendering.*` from domain |
| V-15 (Prot. Variations) | DD-01 | `IRenderPipeline` is the stable seam around the render pipeline variation point |
| V-16/V-17 | DD-01 + DD-03 | Per-class CBO ≤ 11; per-class LCOM ≤ 0.05 |

### 8.3 Remaining Trade-offs

**T-01 — Coordinator coupling (CBO ~6):** `VolumeRenderCoordinator` couples to four interfaces — acceptable (CBO ≤ 25 for orchestrators per brief). Coupling is to interfaces, not concrete classes.

**T-02 — `MonoBehaviour` lifecycle:** A thin Unity entry-point shell must remain. Mitigation: ≤ 30 lines, zero domain logic, enforced by SonarQube (CC > 2 = build failure).

---

## 9. Sub-team Dependencies

| Dependency | Interface | Status |
|---|---|---|
| Sub-team 2 (Data I/O) | `IRawVolumeDataSource` / `RawVolumeData` | Pending — `StubVolumeDataSource` in place |
| Sub-team 4 (Interaction / Gaze) | `IGazeProvider` | Pending — `MockGazeProvider` in place |

Sub-team 3 exposes camera state via `VolumeCameraDriver` (available to Sub-team 4 if needed). No upstream dependencies on Sub-teams 1, 5, 6, or 7.

---

## 10. Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| R-01: `IGazeProvider` not delivered before freeze | Medium | Low | `MockGazeProvider` covers all unit tests |
| R-02: `RawVolumeData` struct changes post-implementation | Medium | Medium | Interface adaptor — one conversion point to update |
| R-03: URP command buffer API changes in Unity 6 LTS | Low | High | `IRenderPipeline` isolates changes to `UrpRenderPipeline` only |
| R-04: `VolumeRenderState` struct grows beyond 64 bytes | Low | Medium | NFR-08 caps size; SonarQube flags field additions |
| R-05: `VolumeRenderCoordinator` accumulates domain logic | Medium | High | WMC ≤ 10 + CC > 2 = build failure in SonarQube |
| R-06: Interface semantic drift across sub-teams | Medium | Medium | `[InterfaceVersion]` attribute + contract test suite |
| R-07: CK targets not achievable within 90 fps | Low | High | CI performance gate: fail build if per-frame CPU > 2 ms |
| R-08: Proposal rejected by maintainer panel | Low | Medium | Data-driven case; all trade-offs documented |

---

## 11. Appendices

**Appendix A — Diagram Source Files:** `diagrams/class-before.puml`, `class-after.puml`, `architecture.puml`, `sequence-render-frame.puml`, `vdsr-dependencies.puml`.

**Appendix B — Interface Stubs:** `refactoring-examples/stubs/IRenderPipeline.cs`, `NullRenderPipeline.cs`, `IMaskMode.cs`, `NullMaskMode.cs`, `IGazeProvider.cs`, `IRawVolumeDataSource.cs`.

**Appendix C — Glossary:** WMC (Weighted Methods per Class), CBO (Coupling Between Objects), RFC (Response For a Class), LCOM (Lack of Cohesion in Methods), DIT (Depth of Inheritance Tree), NOC (Number of Children); ray-marching, foveated rendering, FITS cube, voxel, mask mode.

**Appendix D — References:** iDaVIE GitHub: https://github.com/idia-astro/iDaVIE. Brief §4.2 (Architectural Constraints), §6.3 (Deliverables), §9.2 (Deliverable 2). CK thresholds: `docs/requirements.md`.
