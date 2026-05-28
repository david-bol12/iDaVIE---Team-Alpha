# Sub-team 3: Rendering Engine — Design Document
**Team Alpha | Cache Me If You Can**
*Sprint 2 | 25 May 2026*
*Brief reference: Section 9.2 Deliverable 2 + Section 6.3 (Rendering Layer Design, Shader/Asset Policy, Metrics Worksheet)*
*Target length: 5–10 pages*

---

## 1. Executive Summary

*Brief reference: Section 9.2 — opening orientation for the maintainer panel*

- This document aims to outline the results of the analysis of the iDavie project, using  CK metrics to evaluate code health, performance and maintainability. These metrics aim to expose design-flaws. In addition, this document proposes modifications to the codebase's architecture and design to improve performance and stability.
- The file with the largest number of violations is VolumeDataSetRenderer. `VolumeDataSetRenderer` (~1 400 lines, WMC ~74, CBO ~31) is a monolith that violates SRP, OCP, and DIP; the proposal splits it into four focused classes behind clean interfaces.
- The document outlines the two concrete refactoring examples demonstrated in `refactoring-examples/`.
- Note that no production code is changed — this is a design-only proposal.

---

## 2. Problem Statement

### 2.1 The Core Finding

The iDaVIE volume rendering layer is implemented as a single class,
`VolumeDataSetRenderer`, spanning 1,403 lines of C#. Day 2 static analysis using the
Chidamber–Kemerer metric suite shows that every measurable quality indicator exceeds its
target threshold, in some cases by a factor of three or more:

| Metric | Day 2 Baseline | Target | Excess |
|--------|---------------|--------|--------|
| WMC (Weighted Methods per Class) | **74** | ≤ 20 | 3.7× over |
| CBO (Coupling Between Objects) | **31** | ≤ 14 | 2.2× over |
| RFC (Response For a Class) | **89** | ≤ 50 | 1.8× over |
| LCOM (Lack of Cohesion in Methods) | **0.81** | ≤ 0.5 | 1.6× over |
| DIT (Depth of Inheritance Tree) | 1 | ≤ 4 | ✅ |
| NOC (Number of Children) | 0 | ≤ 5 | ✅ |

All four failing together on the same class is the diagnostic signature of a God Class — a class that has accumulated so many
responsibilities that it resists change, testing, and extension.

### 2.2 What the Metrics Mean in Practice

**WMC = 74** means `VolumeDataSetRenderer` contains 74 units of weighted method
complexity. The assignment target for a domain class is ≤ 20. The worst single method,
`_startFunc`, carries a cyclomatic complexity of 28 across 185 lines — nearly the entire
budget of a well-formed class in one function. This directly predicts high defect density:
methods with cyclomatic complexity above 10 are statistically associated with significantly
elevated fault rates, and iDaVIE's VR context has zero tolerance for frame-rate-breaking
bugs.

**CBO = 31** means the class is structurally coupled to 31 other files across 8 packages.
The target for a domain class is ≤ 14. The mask data set is
referenced approximately 92 times within the class and the feature manager approximately
29 times — these are the two largest concrete coupling targets and the highest-priority
extractions. A CBO of 31 means that 31 other files must be considered when making any
change to `VolumeDataSetRenderer`, making regression risk disproportionate to the size of
any given edit.

**RFC = 89** means the set of methods that can be invoked as a result of a message sent to the class has 89 members.
An RFC of 89 makes it practically impossible to reason about the effect of a call without
reading the class in full, and impossible to write a test that does not implicitly exercise
dozens of unrelated collaborators.

**LCOM = 0.81** means that 81% of the method pairs in the class share no common instance
fields. Perfect cohesion is 0.0 (every method works on the same data); the class is
approaching 1.0, which describes a class that is a collection of unrelated functions with
no internal coherence. This is the metric-level confirmation that `VolumeDataSetRenderer`
contains multiple distinct responsibilities that do not belong together.

### 2.3 Responsibility Inventory

Manual inspection of the class body identifies at least eight distinct responsibilities
currently concentrated in `VolumeDataSetRenderer`:

| # | Responsibility | Should belong to |
|---|---------------|-----------------|
| 1 | Shader keyword and material property management | `VolumeMaterialBinder` |
| 2 | 3D texture upload, caching, memory budget | `VolumeTextureManager` |
| 3 | Camera matrix calculation and clip planes | `VolumeCameraDriver` |
| 4 | Foveated sampling rate decisions | `FoveatedSamplingPolicy` |
| 5 | Mask mode branching (switch on `MaskMode` enum) | `IMaskMode` strategy implementations |
| 6 | Region selection and crop management | `RegionSelectionController` (out of scope for this refactor) |
| 7 | Cursor position tracking and painting | `CursorPaintController` (out of scope) |
| 8 | FITS mask file I/O and moment map control | `MaskIOService` / `MomentMapRenderer` |

Responsibilities 1–5 are in scope for this proposal. Responsibilities 6–8 are noted because
their presence contributes to the WMC and CBO figures above; they must not be silently
absorbed into `VolumeRenderCoordinator` during the split.


### 2.4 Render Pipeline Lock-In

`VolumeDataSetRenderer` calls `Graphics.DrawProceduralNow` at lines 1148 and 1154, uses
the `OnRenderObject` Unity callback (line 1142), and sets global shader keywords via
`Shader.EnableKeyword` / `Shader.DisableKeyword` (lines 1099 and 1103). All three are
incompatible with Unity 6 URP:

- `OnRenderObject` is not called by the URP render loop.
- `Graphics.DrawProceduralNow` executes outside the URP command buffer model.
- `Shader.EnableKeyword` operates on global keyword state, which URP deprecates in favour
  of material-local `LocalKeyword`.

The refactored architecture introduces `IRenderPipeline` as an abstraction boundary that
contains all URP-specific calls inside adapter classes. Domain logic classes have zero
dependency on `UnityEngine.Rendering.Universal` and can be unit-tested without a Unity
player context.

### 2.5 What This Document Proposes

This document specifies a refactoring of the rendering layer that:

- Reduces WMC per class to ≤ 22 by splitting responsibilities across five focused classes
- Reduces CBO per class to ≤ 14 by introducing interface boundaries at every external
  dependency
- Reduces LCOM to ≤ 0.2 per class by ensuring each class operates on a coherent field set
- Breaks the mutual reference cycle at the `VolumeInputController` and `Config` boundaries
  using interfaces, enabling the dependency cycle count to drop from 46 files to ≤ 5 within
  the rendering layer
- Isolates all URP-specific code behind `IRenderPipeline`, making the core testable in
  edit mode without a GPU context

The projected Day 13 CK metrics are given in Section 6 with justification drawn from the
two worked refactoring examples.

---

## 3. Scope

*Brief reference: Section 9.2 Deliverable 2 — what's in and out*

### 3.1 What Sub-team 3 Owns

Sub-team 3 is responsible for the GPU-side volume rendering layer: every class, interface, and shader file that converts a loaded FITS data cube into a visible 3D image inside the VR headset. Concretely, this means the following classes and files are owned by this sub-team.

**Source class under analysis:**

- `VolumeDataSetRenderer.cs` — the existing monolith that this document proposes to refactor. Sub-team 3 authored the analysis but does not modify this file during Sprint 2 (design-only proposal; no production code is changed).

**Domain assembly — `iDaVIE.Rendering`:**

- `VolumeRenderCoordinator.cs` — thin `MonoBehaviour` composition root; the only class in the rendering layer that may import `UnityEngine` types.
- `VolumeMaterialBinder.cs` — shader keyword management, material property binding, colour-map application, and mask mode dispatch.
- `VolumeTextureManager.cs` — 3D texture allocation, GPU upload, LRU eviction, and 368 MB budget enforcement.
- `VolumeCameraDriver.cs` — camera matrix calculation, clip-plane management, and projection mode selection (MIP / AIP).
- `FoveatedSamplingPolicy.cs` — per-frame sample-rate decision from gaze direction; includes uniform fallback when gaze is unavailable.
- `IRenderPipeline.cs` — six-member interface that isolates all render-pipeline-specific API calls from domain logic.
- `IMaskMode.cs` — two-member Strategy interface for per-mode shader keyword management.
- `ApplyMaskMode.cs`, `InverseMaskMode.cs`, `IsolateMaskMode.cs`, `DisabledMaskMode.cs` — the four concrete `IMaskMode` strategy implementations.

**Adapter assemblies (pipeline-specific, Editor-excluded from domain tests):**

- `UrpRenderPipeline.cs` (`iDaVIE.Rendering.URP`) — the only file permitted to import `UnityEngine.Rendering.Universal`.
- `HdrpRenderPipeline.cs` (`iDaVIE.Rendering.HDRP`) — the only file permitted to import HDRP namespaces.

**Test assembly — `iDaVIE.Rendering.Tests` (Editor only):**

- `NullRenderPipeline.cs`, `MockGazeProvider.cs`, `StubVolumeDataSource.cs` — test doubles for all cross-team interface boundaries.

**Shader assets:**

- `Shaders/URP/VolumeRender.shader` and `Shaders/HDRP/VolumeRender.shader` — the URP and HDRP shader variants selected at build time by the render-pipeline adapter. Shader organisation policy in full at `docs/shader-asset-policy.md`.

### 3.2 What Is Explicitly Out of Scope

Three areas are explicitly excluded from this document and from Sub-team 3's responsibilities.

**FITS data ingest (Sub-team 2 — Babelaas).** The loading, parsing, format conversion, and delivery of raw voxel data to the rendering layer is Sub-team 2's responsibility. Sub-team 3 consumes volume data only through the `IRawVolumeDataSource` interface and the `RawVolumeData` struct; it never reads a FITS file directly. The `MaskIOService` and `MomentMapRenderer` classes identified in §2.3 (responsibilities 8) sit at the boundary of data I/O and are likewise out of scope for this refactor. The `StubVolumeDataSource` in the test assembly is a wire-compatible stand-in until Sub-team 2's real implementation is delivered; it is not a substitute for that work.

**VR interaction and gaze SDK (Sub-team 4).** The SteamVR / OpenXR integration, hand-tracking, menu interaction, and the concrete `SteamVRGazeProvider` that will implement `IGazeProvider` are Sub-team 4's responsibility. Sub-team 3 owns the `IGazeProvider` interface definition (as the consuming party) and the `MockGazeProvider` stub, but has no dependency on any SteamVR SDK type in its domain or adapter assemblies. The `VolumeCameraDriver` receives camera state via value types (`Matrix4x4`, `float`); it does not depend on `UnityEngine.Transform` or any VR-specific input abstraction beyond what is expressed in the interface.

**Application shell.** Scene lifecycle management, the main menu, session save/restore orchestration (Sub-team 7), and any `MonoBehaviour` outside the rendering layer's composition root are out of scope. The `VolumeRenderCoordinator` is the sole entry point Sub-team 3 contributes to the scene graph. The two responsibilities identified in §2.3 that are present in `VolumeDataSetRenderer` but excluded from this refactor — region selection and crop management (`RegionSelectionController`) and cursor position tracking and painting (`CursorPaintController`) — remain in the source class untouched; they are noted because their presence inflates the WMC and CBO baseline figures, but absorbing them into `VolumeRenderCoordinator` is explicitly prohibited.

### 3.3 Section-to-Brief Mapping

The brief's Section 6.3 identifies three deliverable components for the rendering layer. They are distributed across this document as follows.

| Brief §6.3 component | This document | Content |
|---|---|---|
| Rendering Layer Design | §5 Design Decisions | DD-01 (`IRenderPipeline`), DD-02 (`IMaskMode`), DD-03 (class split), DD-04 (`FoveatedSamplingPolicy`), migration path |
| Shader/Asset Policy | §5.7 | Summary of `docs/shader-asset-policy.md`; shader folder conventions, naming, per-pipeline variant selection |
| Metrics Worksheet | §6 CK Metrics Worksheet | Day 2 baseline, Day 13 projection, delta summary; full table in `docs/metrics-worksheet.md` |

Brief Section 9.2 (Deliverable 2) additionally requires class and sequence diagrams (§7) and a SOLID/GRASP audit (§8), both of which are included below.

---

## 4. Requirements Recap


### 4.1 Current Invariants (must survive refactoring)

- Table: INV-01 through INV-06 — 90 fps, 4 GB texture limit, 368 MB cube budget, nearest-neighbour filtering, foveated rendering, mask mode correctness.
- VR Performance: Maintain 90 FPS on reference hardware to prevent motion sickness.
- Texture Cap: Total 3D texture memory must not exceed 4GB (Unity limit).
- Volume Budget: Default volume limited to 368 MB.
- Sampling: use nearest-neighbor filtering only (billinear / trilinear distorts voxel data).
- Foveated Rendering: Eye-tracking based rendering must remain fully functional so in the system can maintain 90 FPS at useable image quality. 
- Mask Modes: Visual output for Apply, INverse and isolate modes must match legacy system. 

### 4.2 Key Functional Requirements

- FR01 Volume Visualization: Ray-march through 3D texture, accumulating colour/opcaity via active color map.
- FR02 Runtime Msdking: Support three mask modes (Apply, Inverse, Isolate) without pipeline restart.
- FR03 Dynamic Colour Mapping: Apply configurable colour map with a single-frame visual updates.
- FR04 Gaze-Contingent Sampling: Adjust sample rate based on gaze direction (higher at focus, lower at periphery).
- FR05 Cache Management: Enforce 368 MB budget by eicting/reusing GPU texture slots.
- FR06 Pipeline Isolation: Core assemblies must not import URP / HDRP types directly or transistively. This is a critical design-driven requirement. 

### 4.3 Key Non-Functional Requirements

# CK Metrics Targets (per domain class):
- WMC (Weighted Methods per Class) <= 20, CBO (Coupling Between Objects) <= 14, LCOM (Lack Of Cohesion in Methods) <= 0.5, DIT (Depth Of Inheritance Tree), RFC (Response for Class) <= 50.

# Design Standards:
- Zero circular dependencies (verified via NDpend).
- All rendering logic must be unit-testing  without Unity runtime.
- Addine new mask modes requires only a new iMaskMode implementation (no existing code changes).
- Switching URP to HDRP requreis only replacing one adapter class.
- All internal APIs must be explicitly defined interface.

## 5. Design Decisions

*Brief reference: Section 6.3 — Rendering Layer Design*

### 5.1 Current Architecture (As-Is)

The existing rendering layer is implemented as a single class, `VolumeDataSetRenderer`, spanning 1,403 lines of C#. It inherits directly from `MonoBehaviour` and acts as the sole entry point between the Unity scene lifecycle and the GPU-side volume render. Manual inspection of the class body identifies at least eight distinct responsibilities concentrated in this one file: shader keyword and material property management, 3D texture upload and memory budgeting, camera matrix calculation and clip-plane management, foveated sample-rate decisions, mask mode branching, region selection and crop management, cursor position tracking and painting, and FITS mask file I/O with moment map control.

The current structure is illustrated in `diagrams/class-before.puml`. The diagram shows a single large node coupled to 31 other files across 8 packages — the mask data set is referenced approximately 92 times within the class and the feature manager approximately 29 times.

Day 2 static analysis using the Chidamber–Kemerer metric suite confirms that every measurable quality indicator exceeds its target threshold:

| Metric | Day 2 Baseline | Target | Verdict |
|--------|---------------|--------|---------|
| WMC | **74** | ≤ 20 | 3.7× over |
| CBO | **31** | ≤ 14 | 2.2× over |
| RFC | **89** | ≤ 50 | 1.8× over |
| LCOM | **0.81** | ≤ 0.5 | 1.6× over |
| DIT | 1 | ≤ 4 | ✅ |
| NOC | 0 | ≤ 5 | ✅ |

All four failing metrics together on the same class is the diagnostic signature of a God Class. The SOLID and GRASP audit (§8.1) catalogues 17 confirmed violations — 6 Critical, 8 High, 1 Medium — covering every SOLID principle except LSP and 7 of 9 GRASP patterns. The highest-severity findings directly relevant to Section 5 are: the nine-responsibility SRP breach (V-01), the mask-mode OCP violation requiring four-file edits per new mode (V-04), the 152-member ISP breach (V-06), all three DIP violations involving `Config.Instance`, `FindObjectOfType`, and `UnityEngine` coordinate math (V-07, V-08, V-10), and the complete absence of interface protection at the three identified variation points — render pipeline, gaze provider, and data format (V-15). Full evidence with line references is in `docs/Codebase Exploration/SOLID_GRASP_Violations.md`.

### 5.2 Target Architecture (To-Be)

`VolumeDataSetRenderer` is replaced by four collaborating classes. Every dependency crosses an interface boundary; no class is coupled to a concrete collaborator it doesn't need to change alongside. This structure also breaks the renderer out of the 46-file circular dependency cycle identified in Sprint 1 — full analysis in §8.

**Table 5.2 — Proposed class responsibilities and CK targets**

| Class | Single Responsibility | Key Collaborators | Projected WMC | Projected CBO |
|---|---|---|---|---|
| `VolumeRenderCoordinator` | Orchestrates the per-frame render loop; wires the four classes at the composition root | `VolumeMaterialBinder`, `VolumeTextureManager`, `VolumeCameraDriver`, `FoveatedSamplingPolicy` | ≤ 10 | ≤ 6 |
| `VolumeMaterialBinder` | Shader keyword management, material property binding, colour-map application | `IRenderPipeline`, `IMaskMode` | ~16 | ≤ 11 |
| `VolumeTextureManager` | 3D texture upload, LRU caching, eviction against the 368 MB budget | `IRawVolumeDataSource`, `IAppConfig` | ≤ 15 | ≤ 6 |
| `VolumeCameraDriver` | Camera matrix calculation, clip-plane management, projection mode selection | `IRenderPipeline` | ≤ 12 | ≤ 6 |
| `FoveatedSamplingPolicy` | Per-frame sample-rate decision based on gaze direction | `IGazeProvider` | ≤ 8 | ≤ 2 |

**`VolumeRenderCoordinator`**
- Thin `MonoBehaviour` shell — `Start`/`Update`/`OnDestroy` delegate immediately; no domain logic
- Holds references to the four classes via interfaces; swapping any implementation (e.g. `MockGazeProvider` in tests) requires no change here
- Sole class that knows the other four exist

**`VolumeMaterialBinder`**
- Only class with write access to the Unity `Material` — nothing else calls `SetFloat`, `EnableKeyword`, etc.
- Receives a `VolumeRenderState` value struct per frame; translates it into shader properties
- Resolves V-06 (ISP): replaces 152 public members with a 7-member `IVolumeMaterialBinder` interface

**`VolumeTextureManager`**
- Owns the full 3D texture lifecycle: allocation, GPU upload, staleness detection, eviction
- Enforces the 368 MB budget (INV-03) and nearest-neighbour filtering (INV-04) in one place
- Reads volume data only via `IRawVolumeDataSource` — never touches a file directly

**`VolumeCameraDriver`**
- Returns plain value types (`Matrix4x4`, `float`) to the coordinator — no `UnityEngine.Transform` in domain logic
- Resolves V-10 (DIP): coordinate-conversion math no longer carries a `UnityEngine` dependency
- Owns projection mode (MIP / AIP) selection

**`FoveatedSamplingPolicy`**
- Single decision per frame: given gaze direction, return a sample rate
- Fallback: if `IGazeProvider.IsGazeAvailable == false`, returns uniform sample rate — 90 fps invariant (INV-01) preserved
- `MockGazeProvider` stub satisfies the interface in all unit tests while Sub-team 4 contract is pending

**Render Pipeline Abstraction**
- Domain core never imports `UnityEngine.Rendering.Universal` or HDRP namespaces
- `IRenderPipeline` has two concrete adapters: `UrpRenderPipeline` and `HdrpRenderPipeline` — only these import pipeline-specific types
- Future pipeline migration touches exactly two files; nothing in the domain changes
- *See Figure 3 — architecture.puml*

**Mask Mode Strategy**
- `VolumeMaterialBinder` holds a reference to the active `IMaskMode`; calls `Apply(material, maskTexture)` — no branching
- Four implementations map to the source `MaskMode` enum: `ApplyMaskMode`, `InverseMaskMode`, `IsolateMaskMode`, `DisabledMaskMode`
- Adding a new mode = new class only; resolves V-04 (OCP)
- *See Figure 4 — class-after.puml*

**Cross-team Contracts**
- `VolumeTextureManager` ← `IRawVolumeDataSource` / `RawVolumeData` (Sub-team 2, pending)
- `FoveatedSamplingPolicy` ← `IGazeProvider` (Sub-team 4, pending)
- `StubVolumeDataSource` and `MockGazeProvider` are wire-compatible with the real implementations when delivered

The per-frame sequence connecting all five classes is documented in *Figure 5 — sequence-render-frame.puml*.

### 5.3 DD-01 — Render Pipeline Abstraction (`IRenderPipeline`)

*Brief reference: Section 6.3 — Rendering Layer Design; DIP, Protected Variations (GRASP)*
*Stub: `refactoring-examples/stubs/IRenderPipeline.cs` | Test double: `NullRenderPipeline.cs`*

#### The Problem

`VolumeDataSetRenderer` currently calls `Graphics.DrawProceduralNow` (lines 1148, 1154),
registers via the `OnRenderObject` Unity callback (line 1142), and toggles global shader
keywords with `Shader.EnableKeyword` / `Shader.DisableKeyword` (lines 1099, 1103). All
three are Built-In Render Pipeline APIs that have no equivalent in Unity 6 URP:

- `OnRenderObject` is not invoked by the URP render loop — the draw call never fires.
- `Graphics.DrawProceduralNow` executes immediately outside any command buffer, which URP
  does not support; the call is silently dropped.
- `Shader.EnableKeyword` writes to global keyword state; URP deprecates global keywords in
  favour of per-material `LocalKeyword` to avoid cross-material contamination.

Because these calls are embedded directly in `VolumeDataSetRenderer`, migrating to URP
requires modifying the core domain class. Every future pipeline upgrade repeats that risk.

#### The Decision

Introduce `IRenderPipeline` as a stable interface defined in the domain assembly
`iDaVIE.Rendering`. The domain core depends only on this interface. Two concrete adapter
classes — `UrpRenderPipeline` and `HdrpRenderPipeline` — live in separate assemblies and
are the only files permitted to import `UnityEngine.Rendering.Universal` or HDRP namespaces.
A `NullRenderPipeline` test double in `iDaVIE.Rendering.Tests` implements the interface
with no-ops, enabling edit-mode unit testing without a running Unity player or GPU.

#### Interface Design

```csharp
namespace iDaVIE.Rendering
{
    public interface IRenderPipeline
    {
        void AddCommandBuffer(Camera camera, CameraEvent evt, CommandBuffer buffer);
        void RemoveCommandBuffer(Camera camera, CameraEvent evt, CommandBuffer buffer);
        void SetPipelineKeyword(string keyword, bool enabled);
        bool DepthTextureAvailable { get; }
        void Initialise();
        void Dispose();
    }
}
```

The interface is intentionally minimal — six members. Every additional member raises the
CBO of both `UrpRenderPipeline` and `HdrpRenderPipeline` and increases the surface area
that `NullRenderPipeline` must stub out in tests.

#### Method Rationale

**`AddCommandBuffer` / `RemoveCommandBuffer`**
Command buffers are lists of GPU instructions injected at a named point in Unity's frame
(e.g. `CameraEvent.BeforeForwardAlpha`). These two methods replace the direct
`Graphics.DrawProceduralNow` call at lines 1148/1154 and the `OnRenderObject` callback at
line 1142. The domain class schedules its ray-march work by handing a `CommandBuffer` to
the pipeline; the adapter decides at which camera event it executes. The domain class never
knows which pipeline is active.

**`SetPipelineKeyword`**
Replaces `Shader.EnableKeyword("SHADER_AIP")` and `Shader.DisableKeyword("SHADER_AIP")` at
lines 1099/1103. The domain passes a keyword name and a boolean; the adapter translates this
into the correct pipeline call — a material-local `LocalKeyword` in URP, or the equivalent
HDRP mechanism. The domain class is never aware of the distinction.

**`DepthTextureAvailable`**
The ray-march shader reads `_CameraDepthTexture` to stop marching when it hits a solid
object. Whether this texture is populated depends on pipeline configuration: URP requires
the renderer to have depth texture enabled in its asset; HDRP provides it unconditionally
via the depth prepass. Exposing this as a property lets `VolumeMaterialBinder` configure
the shader's depth-occlusion branch at startup without branching on pipeline type itself.

**`Initialise` / `Dispose`**
Called once when the pipeline asset is activated and once when it is torn down (application
quit or pipeline switch). Adapters use these to register or deregister `ScriptableRenderFeature`
instances, allocate persistent `CommandBuffer` objects, and release any held GPU resources.
Lifecycle management is the adapter's concern; the domain class calls these hooks through the
interface and is otherwise unaware of pipeline setup.

#### Concrete Implementations

**`UrpRenderPipeline`** (`iDaVIE.Rendering.URP` assembly)

| IRenderPipeline method | URP implementation |
|---|---|
| `AddCommandBuffer` | `camera.AddCommandBuffer(evt, buffer)` — Built-In compatible path; in URP 14+ replaced by `ScriptableRenderPass` injection via `VolumeRenderFeature` |
| `RemoveCommandBuffer` | `camera.RemoveCommandBuffer(evt, buffer)` |
| `SetPipelineKeyword` | `material.SetKeyword(new LocalKeyword(shader, keyword), enabled)` — material-local, not global |
| `DepthTextureAvailable` | Reads `UniversalRenderPipelineAsset.supportsCameraDepthTexture` from the active URP asset |
| `Initialise` | Registers `VolumeRenderFeature` on the active `ScriptableRenderer`; allocates persistent `CommandBuffer` |
| `Dispose` | Removes `VolumeRenderFeature`; releases `CommandBuffer` |

`UrpRenderPipeline` is the only class in the entire codebase that imports
`UnityEngine.Rendering.Universal`. Upgrading to a new URP major version touches this one
file; nothing in the domain changes.

**`HdrpRenderPipeline`** (`iDaVIE.Rendering.HDRP` assembly)

| IRenderPipeline method | HDRP implementation |
|---|---|
| `AddCommandBuffer` | Injects via `RenderPipelineManager.beginCameraRendering` or a `CustomPass` volume |
| `RemoveCommandBuffer` | Deregisters the corresponding delegate or `CustomPass` |
| `SetPipelineKeyword` | `material.EnableKeyword` / `material.DisableKeyword` via HDRP shader keyword API |
| `DepthTextureAvailable` | Returns `true` unconditionally — HDRP always produces a depth prepass |
| `Initialise` | Registers `CustomPassVolume`; allocates resources |
| `Dispose` | Destroys `CustomPassVolume`; releases resources |

**`NullRenderPipeline`** (`iDaVIE.Rendering.Tests` assembly — Editor only)

All methods are no-ops. `DepthTextureAvailable` returns `true` so tests that branch on
depth availability do not require extra setup. This test double is in the `Tests` assembly
and must not ship in production builds (enforced by the `.asmdef` `Editor`-only flag —
see `docs/shader-asset-policy.md` §9).

#### DIP Justification

Before refactoring, `VolumeDataSetRenderer` satisfies the Dependency Inversion Principle in
neither direction: it is a concrete high-level class depending on concrete low-level Unity
APIs. The `IRenderPipeline` abstraction corrects both violations:

- **High-level policy** (`VolumeMaterialBinder`, `VolumeCameraDriver`) depends on the
  `IRenderPipeline` abstraction, not on `UnityEngine.Rendering.Universal`.
- **Low-level detail** (`UrpRenderPipeline`, `HdrpRenderPipeline`) implements the
  abstraction; high-level policy never references these classes directly.

This also satisfies GRASP Protected Variations (V-15 in the audit): the render pipeline is
an identified variation point — Unity 6 URP is the immediate target but HDRP must remain
viable. `IRenderPipeline` is the seam that protects the domain from that variation.

The CBO contribution of `IRenderPipeline` to the domain classes is exactly 1 per consumer
(`VolumeMaterialBinder` and `VolumeCameraDriver` each gain one interface dependency). The
alternative — no abstraction — leaves both classes coupled to the entire URP surface,
contributing an unbound and growing CBO as the URP API expands across Unity versions.

### 5.4 DD-02 — Mask Mode Strategy Pattern (`IMaskMode`)

*Brief reference: Section 6.3 — Rendering Layer Design; OCP, Polymorphism (GRASP)*
*Stub: `refactoring-examples/stubs/IMaskMode.cs` | Worked example: `refactoring-examples/example2-MaskModes/`*

#### The Problem

Mask mode selection in the current codebase is handled by a conditional block in
`VolumeDataSetRenderer.Update()` (lines 1072–1094 of `VolumeDataSetRendererMaskMode.cs`).
The block tests against the `MaskMode` enum and directly sets shader keywords and float
properties on the material for each case:

```csharp
// Current code — VolumeDataSetRenderer.Update(), lines 1072–1094
if (_maskDataSet != null)
{
    if (MaskMode == MaskMode.Enabled)
    {
        _materialInstance.EnableKeyword("_MASK_APPLY");
        _materialInstance.DisableKeyword("_MASK_INVERSE");
        _materialInstance.DisableKeyword("_MASK_ISOLATE");
        _materialInstance.SetFloat("_MaskAlpha", 1.0f);
    }
    else if (MaskMode == MaskMode.Inverted)
    {
        _materialInstance.DisableKeyword("_MASK_APPLY");
        _materialInstance.EnableKeyword("_MASK_INVERSE");
        _materialInstance.DisableKeyword("_MASK_ISOLATE");
        _materialInstance.SetFloat("_MaskAlpha", 1.0f);
    }
    else if (MaskMode == MaskMode.Isolated)
    {
        _materialInstance.DisableKeyword("_MASK_APPLY");
        _materialInstance.DisableKeyword("_MASK_INVERSE");
        _materialInstance.EnableKeyword("_MASK_ISOLATE");
        _materialInstance.SetFloat("_MaskAlpha", 0.15f);
    }
    // Disabled: all keywords off, no texture bound
}
```

This is a textbook Open-Closed Principle violation (V-04 in the audit, §8.1). Adding a
new mask behaviour — for instance, an iso-surface contour mode required by future
requirement FUT-01 — requires:

1. Adding a new enum value to `MaskMode` in at least two files
2. Adding a new branch to the conditional block in `VolumeDataSetRenderer`
3. Adding corresponding `#define` and shader code to `BasicVolume.cginc`
4. Updating `PaintMenuController` to expose the new option in the UI

That is a minimum of four files modified to add one new behaviour. Because the
modification touches `VolumeDataSetRenderer` itself — the God Class with WMC = 74
and CBO = 31 — every such extension carries a regression risk proportional to the
class's entire surface area.

The secondary problem is testability. The conditional block reads instance fields
(`_maskDataSet`, `MaskMode`, `_materialInstance`) and calls material methods — there is
no seam to inject a test double, and no way to verify that the correct keyword set is
active without constructing a real `Material` in a running Unity context.

#### The Decision

Replace the conditional block with the **Strategy pattern**. Each mask behaviour becomes
a small, self-contained class that implements a common `IMaskMode` interface.
`VolumeMaterialBinder` holds a reference to the currently active `IMaskMode`
implementation and calls `Apply(material, maskTexture)` once per frame — no branching,
no knowledge of which concrete mode is active.

The interface is defined in the `iDaVIE.Rendering` assembly:

```csharp
namespace iDaVIE.Rendering
{
    public interface IMaskMode
    {
        /// <summary>
        /// Apply this mask mode's shader keywords and material properties.
        /// Called by VolumeMaterialBinder once per frame (or on any pipeline
        /// event that requires material re-binding).
        /// The implementation enables its own keyword and disables all others,
        /// then sets any mode-specific float or texture properties.
        /// </summary>
        void Apply(Material material, Texture3D maskTexture);

        /// <summary>
        /// The HLSL keyword that uniquely identifies this mode
        /// (e.g. "_MASK_APPLY", "_MASK_INVERSE", "_MASK_ISOLATE").
        /// Exposed for shader-variant stripping tools and metrics scripts
        /// without requiring a live Material or GPU context.
        /// </summary>
        string ShaderKeyword { get; }
    }
}
```

Two members. Both are essential: `Apply` is the per-frame operation; `ShaderKeyword`
is the identity property that allows tooling to reason about the mode without
instantiating it.

#### Concrete Implementations

Four production classes implement `IMaskMode`, one per value of the source `MaskMode`
enum. Each class is sealed, stateless, and independently unit-testable:

| Class | Shader keyword enabled | `_MaskAlpha` | Rendering behaviour |
|---|---|---|---|
| `ApplyMaskMode` | `_MASK_APPLY` | 1.0 | Mask region rendered; outside hidden |
| `InverseMaskMode` | `_MASK_INVERSE` | 1.0 | Outside region rendered; mask hidden |
| `IsolateMaskMode` | `_MASK_ISOLATE` | 0.15 | Mask at full opacity; outside at 15% |
| `DisabledMaskMode` | *(none)* | 0.0 | Mask texture unbound; no influence |

Each implementation follows the same structure. `IsolateMaskMode` is the most
representative because it captures the one behavioural constant (`0.15f`) that was
previously buried inside the conditional block — it is now encapsulated in the class
that owns it:

```csharp
public sealed class IsolateMaskMode : IMaskMode
{
    private const float OutsideAlpha = 0.15f;  // previously hardcoded in Update()

    public string ShaderKeyword => "_MASK_ISOLATE";

    public void Apply(Material material, Texture3D maskTexture)
    {
        material.DisableKeyword("_MASK_APPLY");
        material.DisableKeyword("_MASK_INVERSE");
        material.EnableKeyword("_MASK_ISOLATE");
        material.SetFloat("_MaskAlpha", OutsideAlpha);
        material.SetTexture("_MaskTex", maskTexture);
    }
}
```

Full implementations of all three active modes are in
`refactoring-examples/example2-MaskModes/after/`. A `NullMaskMode` no-op test double
lives in `iDaVIE.Rendering.Tests` (see `refactoring-examples/stubs/IMaskMode.cs`);
it returns `"_MASK_NULL"` for `ShaderKeyword` and performs no material writes, enabling
`VolumeMaterialBinder` to be unit-tested without a Material asset or GPU context.

#### Switching Modes at Runtime

`VolumeMaterialBinder` holds a single `IMaskMode _activeMaskMode` field. The
`VolumeRenderCoordinator` (or a future `MaskModeController`) swaps it by calling:

```csharp
materialBinder.SetMaskMode(new IsolateMaskMode());
```

There is no switch statement anywhere in the calling code — the coordinator simply
constructs the desired implementation and passes it through the `IVolumeMaterialBinder`
interface. The next `Apply()` call picks it up.

#### OCP and SRP Justification

**OCP — Open for extension, closed for modification.**
Adding the iso-surface contour mode required by FUT-01 means writing one new class,
`IsoSurfaceMaskMode`, that implements `IMaskMode`. Zero existing files change.
`VolumeMaterialBinder`, `VolumeRenderCoordinator`, `BasicVolume.cginc`, and
`PaintMenuController` are all unaffected. This is the OCP promise fulfilled: the
system is open for the new behaviour and closed to modification.

**SRP — One reason to change.**
Under the old design, the conditional block in `VolumeDataSetRenderer` had to change
both when mask behaviour changed *and* when any other aspect of the renderer changed.
Under the new design, `IsolateMaskMode` changes only if the isolated-mode rendering
behaviour changes. `InverseMaskMode` changes only if the inverse-mode behaviour changes.
No implementation has a reason to change that belongs to another class.

#### Pattern Choice Rationale

Strategy was chosen over the two most common alternatives:

**Strategy vs. Decorator.** Decorator would compose multiple mask behaviours
simultaneously. Mask modes in iDaVIE are mutually exclusive per frame — only one
HLSL keyword branch is active at a time, and the shader is not structured for
simultaneous composition. Decorator adds complexity without benefit here.

**Strategy vs. State.** State pattern models objects that transition through a
lifecycle and initiate their own transitions. Mask mode is set externally (user menu
selection) and carries no internal state that causes autonomous transitions. Strategy
is the simpler fit for a selector pattern driven by external input.

### 5.5 DD-03 — `VolumeDataSetRenderer` Split (SRP)

*Brief reference: Section 9.2 Deliverable 2 — main design decision*

#### The Problem

The Single Responsibility Principle requires that a class have exactly one reason to change. `VolumeDataSetRenderer` has at least eight. As documented in §2.3 and §5.1, the class conflates shader property writes, texture lifecycle management, camera matrix math, foveation logic, mask mode branching, crop management, cursor painting, and FITS I/O. The consequence is not merely aesthetic: because all eight responsibilities share the same 1,403-line file, a change to any one of them — adding a new colour map, tweaking a clip-plane calculation, extending the mask mode list — forces a reviewer to reason about the full class surface. With WMC = 74, CBO = 31, and LCOM = 0.81, the regression risk per edit is disproportionate to the size of the change.

The responsibility clustering was confirmed using a colour-coding exercise on a printed listing of `VolumeDataSetRenderer`, as described in `refactoring-examples/team3/` (Refactoring Example 1). Each line was highlighted in one of eight colours corresponding to the responsibilities in §2.3. The result showed four dense, well-separated clusters with minimal overlap: shader/material operations, texture operations, camera/geometry operations, and foveation/sampling operations. The remaining responsibilities — mask mode, crop, cursor, and I/O — are either already handled by DD-02 (mask mode) or explicitly out of scope for this sprint (§3.2). The four-cluster result directly motivates the four-class split.

#### The Decision

`VolumeDataSetRenderer` is replaced by five collaborating classes. The four domain classes (`VolumeMaterialBinder`, `VolumeTextureManager`, `VolumeCameraDriver`, `FoveatedSamplingPolicy`) each own exactly one responsibility cluster. A fifth class, `VolumeRenderCoordinator`, is a thin `MonoBehaviour` shell whose sole responsibility is to wire the four domain classes together and drive the per-frame render loop by calling them in sequence. It contains no domain logic of its own.

The split is designed so that every class operates on a coherent, non-overlapping field set. This is the structural guarantee that LCOM will be near zero for each extracted class: no extracted class contains method pairs that share nothing in common, because each class is defined around a single field cluster.

**Responsibility Assignment**

The colour-coding exercise maps each cluster to its owning class as follows:

| Cluster | Lines affected (approx.) | Extracted to |
|---------|--------------------------|-------------|
| Shader keyword and material property writes | ~180 lines in `Update()` | `VolumeMaterialBinder` |
| 3D texture allocation, upload, and eviction | ~210 lines across `_startFunc` and `Update()` | `VolumeTextureManager` |
| Camera matrix, clip planes, projection mode | ~95 lines in `Update()` and helper methods | `VolumeCameraDriver` |
| Foveated sample-rate calculation | ~40 lines in `Update()` | `FoveatedSamplingPolicy` |
| Orchestration (per-frame call sequence) | Thin shell — `Start`, `Update`, `OnDestroy` | `VolumeRenderCoordinator` |

**`VolumeRenderCoordinator` as the Sole Entry Point**

`VolumeRenderCoordinator` is the only class in the rendering layer that inherits from `MonoBehaviour`. Its `Start()` method constructs the four domain classes via constructor injection and wires their dependencies. Its `Update()` method assembles a `VolumeRenderState` value struct from the current scene state and calls the four domain classes in order — camera first, then foveation, then texture, then material bind. Its `OnDestroy()` disposes managed resources. No method in `VolumeRenderCoordinator` computes a value — every computation is delegated. The SonarQube gate (§10.2) enforces a maximum cyclomatic complexity of 2 per coordinator method; any method that computes rather than delegates will fail the build.

This design ensures that `VolumeRenderCoordinator` has exactly one reason to change: the wiring between the four domain classes. It cannot accumulate domain logic because domain logic belongs, by definition, in the class that owns its field cluster.

#### SRP Justification and Projected CK Metrics

Under the proposed split, each extracted class has exactly one reason to change:

- `VolumeMaterialBinder` changes only if the shader property protocol changes (new keyword, new float property, colour map pipeline change).
- `VolumeTextureManager` changes only if the texture lifecycle policy changes (budget limit, eviction strategy, filtering mode).
- `VolumeCameraDriver` changes only if the camera math changes (new projection mode, coordinate frame shift, clip plane policy).
- `FoveatedSamplingPolicy` changes only if the foveation algorithm changes (new zone thresholds, different fallback policy).

None of these has a reason to change that belongs to another class. The LCOM prediction follows directly: each class operates on a single field cluster, so the fraction of method pairs sharing no common field approaches zero.

The projected CK metrics per class are given in §6.2. All five classes meet the domain targets. `VolumeTextureManager` at WMC = 20 is exactly at the limit, and its per-method complexity breakdown in the class header confirms no single method exceeds CC = 4 — the budget is distributed across five distinct operations (allocate, upload, evict, validate budget, enforce filter mode) rather than concentrated in one long method.

### 5.6 DD-04 — Foveated Rendering Extraction (`FoveatedSamplingPolicy` + `IGazeProvider`)

*Brief reference: Section 6.3 — Rendering Layer Design; DIP*

#### The Problem

Foveated rendering reduces GPU load by sampling the volume at a lower rate in the peripheral visual field, where the human eye resolves less detail. In the current codebase the foveation rate calculation is implemented inline inside `VolumeDataSetRenderer.Update()`, interleaved with material property writes and texture state checks. The concrete SteamVR gaze API is called directly, with no abstraction boundary between the foveation algorithm and the input SDK.

This creates two problems. First, the foveation calculation cannot be tested in isolation — any test that exercises it must construct a running Unity player with a live HMD or mock the entire `VolumeDataSetRenderer` context. Second, the direct SteamVR dependency means that the class is coupled to Sub-team 4's SDK at compile time. If Sub-team 4's gaze provider changes its API, or if a future deployment targets a non-SteamVR headset, `VolumeDataSetRenderer` must be modified — a modification to a 1,403-line God Class with WMC = 74.

The violation is a textbook DIP breach (V-07 in §8.1): the high-level foveation policy depends directly on the low-level concrete gaze SDK, rather than on an abstraction it controls.

#### The Decision

Extract the foveation calculation into a dedicated class, `FoveatedSamplingPolicy`, and introduce `IGazeProvider` as the interface that decouples it from the SteamVR SDK. `FoveatedSamplingPolicy` is constructed with an `IGazeProvider` injected via its constructor. On each call to `Evaluate(Vector3 gazeDirection)`, it maps the gaze direction to a foveation zone (central, mid-periphery, far-periphery) and returns a `FoveationParameters` value struct containing the sample step and ray-march stride for that zone.

The `IGazeProvider` interface is defined in the `iDaVIE.Rendering` domain assembly and is therefore owned by Sub-team 3 as the consuming party. Sub-team 4's concrete `SteamVRGazeProvider` will implement this interface when delivered. Until then, `MockGazeProvider` — a test double in `iDaVIE.Rendering.Tests` — satisfies the interface with a configurable fixed gaze direction, enabling all foveation unit tests to run without an HMD or a running Unity player.

```csharp
namespace iDaVIE.Rendering
{
    public interface IGazeProvider
    {
        /// <summary>
        /// Returns the current normalised gaze direction in world space.
        /// Only valid when IsGazeAvailable is true.
        /// </summary>
        Vector3 GetGazeDirection();

        /// <summary>
        /// False when the HMD is not tracking or the gaze SDK is unavailable.
        /// FoveatedSamplingPolicy falls back to a uniform sample rate when false.
        /// </summary>
        bool IsGazeAvailable { get; }
    }
}
```

#### Fallback Behaviour

When `IGazeProvider.IsGazeAvailable` is `false` — which covers the HMD-absent case, the SteamVR initialisation window, and any future headset that does not expose eye tracking — `FoveatedSamplingPolicy.Evaluate()` returns the uniform sample rate defined in `FoveatedSamplingConfig.UniformSampleStep`. This is the same rate used by the current monolith when the HMD is absent, so the fallback preserves the existing behaviour exactly and keeps INV-01 (90 fps minimum) satisfied on developer machines running in desktop mode.

The fallback is tested explicitly: `FoveationFallbackTest` in `iDaVIE.Rendering.Tests` constructs a `MockGazeProvider` with `IsGazeAvailable = false`, calls `Evaluate()`, and asserts that the returned sample step matches the configured uniform rate.

#### DIP Justification

After extraction, the dependency direction is:

- `FoveatedSamplingPolicy` (high-level policy) → `IGazeProvider` (abstraction it controls)
- `SteamVRGazeProvider` (low-level detail, Sub-team 4) → `IGazeProvider` (implements the abstraction)

The high-level policy never names `SteamVRGazeProvider`. The low-level detail never appears in the domain assembly. This resolves V-07 and V-15 (GRASP Protected Variations) for the gaze variation point: swapping the HMD SDK or targeting a non-SteamVR platform requires implementing `IGazeProvider` in a new adapter class — no domain code changes.

### 5.7 Shader/Asset Organisation Policy for Unity 6

*Brief reference: Section 6.3 — Shader/Asset Policy (summary — full policy in `docs/shader-asset-policy.md`)*

This section summarises the shader and asset conventions that are binding for Sub-team 3. The full policy, including rationale and version control rules, is in `docs/shader-asset-policy.md`.

#### Folder Structure

All rendering assets live under `Assets/Rendering/`. Shader source files are placed under `Assets/Rendering/Shaders/Volume/`, with feature-specific HLSL includes grouped in subfolders (`Foveated/`, `Masks/`). Colour map shaders live in `Assets/Rendering/Shaders/ColourMaps/`. The URP integration classes (`VolumeRenderFeature.cs`, `VolumeRenderPass.cs`) live in `Assets/Rendering/RenderPipeline/`. The shader variant collection for build-time stripping lives at `Assets/Rendering/ShaderVariants/VolumeVariants.shadervariants`.

#### Naming Conventions

Shader files use `PascalCase` noun phrases (e.g. `VolumeRaymarch.shader`). HLSL include files follow `PascalCase` plus a feature name (e.g. `FoveatedSampling.hlsl`, `MaskApply.hlsl`). Colour map shaders follow the pattern `ColourMap_{Name}.shader`. Materials are named to match the shader they use.

#### Pipeline Variants

The ray-march shader has a URP variant and an HDRP variant. These are selected at build time by the `IRenderPipeline` adapter class (§5.3): `UrpRenderPipeline` binds `VolumeRaymarch.shader` configured for URP, and `HdrpRenderPipeline` binds the HDRP-compatible variant. No domain class references the shader asset directly — the adapter is the sole point of selection, consistent with the Protected Variations guarantee in DD-01.

The `VolumeRenderFeature` and `VolumeRenderPass` classes in `Assets/Rendering/RenderPipeline/` are the **only** files permitted to import `UnityEngine.Rendering.Universal`. This constraint is enforced by the assembly definition files (`.asmdef`): the `iDaVIE.Rendering` domain assembly does not reference the URP assembly, so a stray `using UnityEngine.Rendering.Universal` in a domain class is a compile error.

#### Shader Variant Stripping

Only variants for the three active mask modes (`_MASK_APPLY`, `_MASK_INVERSE`, `_MASK_ISOLATE`) and the supported colour maps are included in the `ShaderVariantCollection`. No debug-only keyword variants ship in production builds. The variant collection is committed to version control and must be reviewed on any shader keyword change — this is listed as a mandatory step in the PR template.

#### Runtime vs. Baked Assets

`Texture3D` volume objects are created at runtime by `VolumeTextureManager` and are never saved as `.asset` files; they can be hundreds of megabytes and change with each dataset load. `RenderTexture` targets are also runtime-only. Colour map LUT textures are baked as 256×1 RGBA PNG files imported as `Texture2D`. The base `VolumeMaterial.mat` is a hand-authored baked asset; its shader properties are overridden at runtime by `VolumeMaterialBinder` but the asset itself is committed to version control.

---

### 5.8 Migration Path

*Brief reference: Section 9.2 Deliverable 2 — how the maintainer transitions from current to target architecture*

#### Strategy: Strangler Fig

The proposed refactoring does not require a big-bang rewrite. The split is designed to be
executed in seven discrete phases using the **Strangler Fig** pattern: each phase extracts
one responsibility from `VolumeDataSetRenderer`, replaces the inline code with a delegation
call, and leaves the class otherwise unchanged. At the end of every phase the codebase
compiles and runs at full frame rate; `VolumeDataSetRenderer` is never deleted until the
last phase has been verified. At every intermediate stage a maintainer can revert a single
phase by removing the extracted class and restoring the inline code — no phase contaminates
the others.

The invariant that must hold throughout the entire migration is **INV-01: 90 fps minimum
frame rate**. The CI performance gate (§10.1) runs after every merged phase. If the gate
fails, the phase is reverted; the architecture design is not changed.

Three principles govern every phase:

1. **One extraction per pull request.** Each phase is a standalone PR. No phase extracts
   more than one class. This keeps diffs reviewable and keeps rollback scope minimal.
2. **Interfaces before implementations.** Every new class is hidden behind its interface
   from the first commit. `VolumeDataSetRenderer` never names the concrete type of its
   delegate — only the interface. This means the extracted class is immediately swappable
   with a test double.
3. **No public API change at the scene boundary.** `VolumeDataSetRenderer` remains in the
   Unity scene graph as a `MonoBehaviour` with the same public fields throughout Phases 1–6.
   Prefabs and scene files do not change until Phase 7. This avoids Unity's serialisation
   system losing field values during the migration.

#### Phase 0 — Seam Introduction (zero behaviour change)

**What changes:** The four interface files (`IRenderPipeline`, `IMaskMode`, `IGazeProvider`,
`IRawVolumeDataSource`) and their test doubles (`NullRenderPipeline`, `NullMaskMode`,
`MockGazeProvider`, `StubVolumeDataSource`) are added to the codebase. No existing file is
modified. `VolumeDataSetRenderer` continues to implement all logic inline.

**Why first:** Interfaces cannot be referenced by later phases until they exist. Introducing
them in isolation, with no behaviour change, means there is nothing to break and the PR is
trivially reviewable.

**Exit condition:** Project compiles. All existing tests pass. No frame rate measurement
required (no runtime change).

**Rollback:** Delete the four interface files and four test-double files. Zero impact on
`VolumeDataSetRenderer`.

---

#### Phase 1 — Extract `IMaskMode` Strategies

**What changes:** The if/else chain at lines 1072–1094 of
`VolumeDataSetRendererMaskMode.cs` is removed and replaced with a single call:

```csharp
_activeMaskMode.Apply(_materialInstance, _maskDataSet?.MaskTexture);
```

`_activeMaskMode` is an `IMaskMode` field on `VolumeDataSetRenderer`, initialised to
`DisabledMaskMode` in `_startFunc` and swapped when the user changes mode. The four
concrete implementations (`ApplyMaskMode`, `InverseMaskMode`, `IsolateMaskMode`,
`DisabledMaskMode`) live in the new `iDaVIE.Rendering` assembly.

**Why first among extractions:** The mask mode block touches only `_materialInstance` and
`_maskDataSet` — two fields that no other extraction phase touches. It has no interaction
with the Unity render loop, no `MonoBehaviour` lifecycle dependency, and no allocation on
the hot path. It is the lowest-risk extraction and validates the pattern before it is
applied to more complex responsibilities.

**Entry condition:** Phase 0 complete. `IMaskMode` interface exists.

**Exit condition:** All four mask-mode unit tests pass (each `Apply()` call verified against
expected keyword state on a real `Material` in an Editor test). Frame rate gate passes.
`LCOM` of `VolumeDataSetRenderer` drops measurably (mask-mode fields and material fields
decouple).

**Rollback:** Remove `ApplyMaskMode`, `InverseMaskMode`, `IsolateMaskMode`,
`DisabledMaskMode`. Restore the if/else block in `VolumeDataSetRendererMaskMode.cs`.
One file changed, one revert.

---

#### Phase 2 — Extract `FoveatedSamplingPolicy`

**What changes:** The inline foveation rate calculation in `VolumeDataSetRenderer.Update()`
is removed and replaced with:

```csharp
var foveationParams = _foveatedPolicy.Evaluate(_gazeProvider.GetGazeDirection());
_materialInstance.SetFloat(ShaderID.SampleStep, foveationParams.SampleStep);
```

`FoveatedSamplingPolicy` is constructed in `_startFunc` with a `MockGazeProvider` until
Sub-team 4 delivers `SteamVRGazeProvider`. The fallback behaviour
(`IGazeProvider.IsGazeAvailable == false` → uniform sample rate) is the same path as
today when the HMD is absent.

**Why second:** Foveated sampling is a pure function — given a gaze direction, return a
sample rate. It reads no mutable Unity state and produces a `float` value. Extracting it
before the material or camera code means those heavier phases can be profiled against a
baseline that already has a stable foveation delegate in place.

**Entry condition:** Phase 1 complete. `IGazeProvider` interface and `MockGazeProvider`
stub exist (Phase 0).

**Exit condition:** `FoveatedSamplingPolicy` unit tests pass (correct sample rate per
foveation zone; uniform fallback when gaze unavailable). Frame rate gate passes.
`VolumeDataSetRenderer` no longer contains foveation math.

**Rollback:** Remove `FoveatedSamplingPolicy`. Restore inline foveation block.

---

#### Phase 3 — Extract `VolumeMaterialBinder`

**What changes:** All `_materialInstance.SetFloat()`, `_materialInstance.SetTexture()`,
`_materialInstance.EnableKeyword()`, and `_materialInstance.DisableKeyword()` calls in
`VolumeDataSetRenderer.Update()` are removed. `VolumeDataSetRenderer` instead assembles a
`VolumeRenderState` value struct from its current fields and calls:

```csharp
_materialBinder.Bind(_renderState);
```

`VolumeMaterialBinder` owns `_materialInstance` exclusively from this point; no other class
writes to it.

**Why third:** This is the most write-heavy extraction — it removes the largest cluster of
shader-property calls from `VolumeDataSetRenderer`. It also introduces `VolumeRenderState`,
which must be sized and validated before Phase 5 (`VolumeTextureManager`) can read from it.
Ordering it before the texture manager means the struct is stable when texture state is added.

**Entry condition:** Phases 1 and 2 complete. `_activeMaskMode` already lives on
`VolumeMaterialBinder` from the integration point; pass it through the struct.

**Performance check:** After merging, run the allocation profiler for five frames in the
Unity Editor. `VolumeRenderState` is a value type — its construction must produce zero
heap allocations. If the profiler shows allocations, the struct has a reference-type field
that must be converted or moved outside the per-frame path.

**Exit condition:** All material binding tests pass. Zero heap allocations on the hot path
(verified by Unity Memory Profiler). Frame rate gate passes.

**Rollback:** Remove `VolumeMaterialBinder` and `VolumeRenderState`. Restore inline
`SetFloat` / `EnableKeyword` calls in `Update()`.

---

#### Phase 4 — Extract `VolumeCameraDriver`

**What changes:** Camera matrix calculations, clip-plane management, and projection mode
selection are removed from `VolumeDataSetRenderer` and placed in `VolumeCameraDriver`.
The coordinator calls:

```csharp
var cameraState = _cameraDriver.ComputeFrame(_camera);
_renderPipeline.SetViewProjection(cameraState.ViewProjection, cameraState.ClipPlanes);
```

`VolumeCameraDriver` takes a `Camera` reference at construction time but returns only value
types (`Matrix4x4`, `Vector4[]`). No `UnityEngine.Transform` reference escapes the class —
the domain method receives a matrix, not a `Transform`. This resolves V-10 (DIP violation,
§8.1).

**Entry condition:** Phase 3 complete. `IRenderPipeline` exists (Phase 0).

**Correctness test:** A parameterised Editor test constructs `VolumeCameraDriver` with a
known `Camera` configuration and asserts that the returned `ViewProjection` matrix matches
the result previously computed by `VolumeDataSetRenderer`'s inline code for the same inputs.
This is the numerical regression guard.

**Exit condition:** Correctness test passes for at least five distinct camera configurations
(including oblique projection, zero-distance clip, and ortho mode). Frame rate gate passes.

**Rollback:** Remove `VolumeCameraDriver`. Restore inline matrix math.

---

#### Phase 5 — Extract `VolumeTextureManager`

**What changes:** 3D texture allocation, GPU upload, staleness detection, LRU eviction, and
budget enforcement are removed from `VolumeDataSetRenderer` and placed in
`VolumeTextureManager`. `VolumeDataSetRenderer` calls:

```csharp
var tex = _textureManager.GetOrUpload(_dataSource.GetCurrentCube());
_materialBinder.SetVolumeTexture(tex);
```

`VolumeTextureManager` enforces INV-03 (368 MB budget) and INV-04 (nearest-neighbour
filtering) internally. No other class can reach the `Texture3D` object directly after this
phase.

**Why last among domain extractions:** Texture management is the highest-risk extraction.
A bug in eviction logic can cause a black viewport (texture evicted while still in use) or
a frame spike (texture re-uploaded mid-frame). The phase is therefore left until all other
extractions are complete and the surrounding code is stable, so that profiling noise from
other phases is absent.

**Entry condition:** Phases 1–4 complete. `IRawVolumeDataSource` exists (Phase 0).
`StubVolumeDataSource` provides a synthetic 32³ voxel cube for testing.

**Performance check:** Run the Unity Profiler with the deep profiler enabled during a
deliberate cache eviction (load a second cube large enough to exceed 368 MB). The eviction
must complete within one frame without a frame time spike above 2 ms above baseline. If it
spikes, eviction must be deferred across multiple frames using a coroutine within
`VolumeTextureManager` — the interface does not change.

**Exit condition:** Budget enforcement test passes (synthetic cube at 370 MB triggers
eviction; 368 MB cube does not). Nearest-neighbour filtering preserved (verified by
inspecting `TextureWrapMode` and `FilterMode` on the uploaded texture). Frame rate gate
passes.

**Rollback:** Remove `VolumeTextureManager`. Restore inline texture lifecycle code.

---

#### Phase 6 — Introduce `VolumeRenderCoordinator` and Retire `VolumeDataSetRenderer`

**What changes:** A new `VolumeRenderCoordinator` `MonoBehaviour` is added to the scene.
It constructs the four domain classes in `Start()` via constructor injection, then drives
the per-frame loop in `Update()`. `VolumeDataSetRenderer` is marked `[Obsolete]` and
removed from the scene's prefab. Scene file and prefab serialisation are migrated in a
single commit; no intermediate state exposes a broken prefab.

The migration sequence within this phase is:

1. Add `VolumeRenderCoordinator` to the scene alongside `VolumeDataSetRenderer`. Both run.
   Assert that both produce the same per-frame `VolumeRenderState` (logged to a test output
   channel). This is the **shadow mode** step — the coordinator runs in parallel with the
   monolith and its output is compared frame-by-frame.
2. Disable `VolumeDataSetRenderer.Update()` while keeping `VolumeRenderCoordinator.Update()`
   active. Run for one session. If no regression, proceed to step 3.
3. Remove `VolumeDataSetRenderer` from the prefab and scene. Delete the class or archive it
   to `Legacy/` under version control for reference.

Shadow mode is optional in a proposal context (this is a design-only project) but is the
approach a maintainer would follow in production to reduce risk to zero.

**Exit condition:** `CoordinatorWiringTest` passes (§10.2). Frame rate gate passes with only
the coordinator active. `VolumeDataSetRenderer` is absent from the scene.

---

#### Phase 7 — Render Pipeline Migration (Unity 6 URP)

**What changes:** `BuiltInRenderPipeline` (the `IRenderPipeline` adapter that wraps the
existing `OnRenderObject` / `Graphics.DrawProceduralNow` calls) is replaced with
`UrpRenderPipeline`. No domain class changes. The Unity project's Graphics Settings asset
is switched from Built-In to URP. `VolumeRender.shader` is replaced with
`Shaders/URP/VolumeRender.shader`.

Because all render-pipeline-specific code is contained in `UrpRenderPipeline` and the URP
shader variant (§5.7), this migration is a two-file swap. The domain classes do not change.
The `NullRenderPipeline` test double continues to satisfy all unit tests without
modification.

**Entry condition:** Phase 6 complete. `UrpRenderPipeline` and `HdrpRenderPipeline` both
implement `IRenderPipeline` (stubs exist from Phase 0; full implementations complete by
this phase).

**Exit condition:** Frame rate gate passes with URP active. All unit tests pass against
`NullRenderPipeline`. Visual regression test (screenshot comparison of a reference FITS
cube) passes within a configurable tolerance.

---

#### Migration Phase Summary

| Phase | What is extracted | Class retired / introduced | Highest risk | Rollback cost |
|-------|------------------|--------------------------|-------------|---------------|
| 0 | Interfaces + test doubles | — | None | Delete 8 files |
| 1 | Mask mode branching | `IMaskMode` strategy classes | Low | 1 file restored |
| 2 | Foveation calculation | `FoveatedSamplingPolicy` | Low | 1 file restored |
| 3 | Shader property writes | `VolumeMaterialBinder` | Medium | 1 file restored |
| 4 | Camera matrix math | `VolumeCameraDriver` | Medium | 1 file restored |
| 5 | Texture lifecycle | `VolumeTextureManager` | High | 1 file restored |
| 6 | Orchestration wiring | `VolumeRenderCoordinator` introduced; `VolumeDataSetRenderer` retired | Medium | Restore prefab reference |
| 7 | Render pipeline | `UrpRenderPipeline` replaces `BuiltInRenderPipeline` | High | Swap Graphics Settings asset |

At no point in Phases 1–6 does the codebase contain a broken intermediate state. Each phase
merges to `main` independently and leaves a fully runnable, 90-fps-verified build. The
maintainer panel can inspect the codebase at any phase checkpoint and see a working
iDaVIE application.

---

## 6. CK Metrics Worksheet

*Brief reference: Section 6.3 — Metrics Worksheet (summary — full table in `docs/metrics-worksheet.md`)*

### 6.1 Day 2 Baseline (Measured)

| Class | WMC | DIT | NOC | CBO | RFC | LCOM |
|-------|-----|-----|-----|-----|-----|------|
| `VolumeDataSetRenderer` | **74** | 1 | 0 | **31** | **89** | **0.81** |
| Target (domain class) | ≤ 20 | ≤ 4 | ≤ 5 | ≤ 14 | ≤ 50 | ≤ 0.5 |
| Exceeds target by | 3.7× | ✅ | ✅ | 2.2× | 1.8× | 1.6× |

DIT = 1 reflects direct inheritance from `MonoBehaviour`. NOC = 0: no subclasses of
`VolumeDataSetRenderer` exist. Both are within target and require no action.

### 6.2 Day 13 Projection (Proposed)

All values are derived directly from the worked refactoring examples in `refactoring-examples/team3/` — not speculative. Each class file header contains the per-method WMC breakdown and CBO coupling inventory that underlies these figures.

| Class | WMC | DIT | NOC | CBO | RFC | LCOM | Meets target? |
|-------|-----|-----|-----|-----|-----|------|---------------|
| `VolumeRenderCoordinator` | 3 | 1 | 0 | 6 | 12 | 0.00 | ✅ all |
| `VolumeMaterialBinder` | 16 | 0 | 0 | 11 | 22 | 0.05 | ✅ all |
| `VolumeTextureManager` | 20 | 0 | 0 | 8 | 20 | 0.05 | ✅ all |
| `VolumeCameraDriver` | 9 | 0 | 0 | 4 | 18 | 0.00 | ✅ all |
| `FoveatedSamplingPolicy` | 7 | 0 | 0 | 6 | 14 | 0.00 | ✅ all |
| `ApplyMaskMode` | 2 | 1 | 0 | 1 | 3 | 0.00 | ✅ all |
| `InverseMaskMode` | 2 | 1 | 0 | 1 | 3 | 0.00 | ✅ all |
| `IsolateMaskMode` | 2 | 1 | 0 | 1 | 3 | 0.00 | ✅ all |
| `DisabledMaskMode` | 2 | 1 | 0 | 1 | 3 | 0.00 | ✅ all |
| `UrpRenderPipeline` (adapter) | 8 | 0 | 0 | 14 | 20 | 0.00 | ✅ (adapter target ≤ 40 / ≤ 25) |
| Target (domain) | ≤ 20 | ≤ 4 | ≤ 5 | ≤ 14 | ≤ 50 | ≤ 0.5 | — |
| Target (adapter) | ≤ 40 | ≤ 4 | ≤ 5 | ≤ 25 | ≤ 50 | ≤ 0.5 | — |

**Notes on specific values:**
- `VolumeRenderCoordinator` DIT = 1: it inherits `MonoBehaviour` (unavoidable in Unity); no other class in the proposal inherits from Unity types.
- `VolumeMaterialBinder` CBO = 11: Material (1), Texture3D (1), IMaskMode (1), IRenderPipeline (1), VolumeRenderState (1), IVolumeMaterialBinder (self-interface, not counted), 3 shader-keyword string constants folded into Material calls, FoveatedSamplingConfig (1), VolumeColourMap (1). All within the ≤ 14 domain limit.
- `VolumeTextureManager` WMC = 20: exactly at the domain limit; per-method breakdown in the class header confirms no single method exceeds CC = 4.
- `FoveatedSamplingPolicy` CBO = 6: IGazeProvider (1), FoveatedSamplingConfig (1), FoveationParameters (1), FoveationZone (enum, 1), Mathf (1), Vector2 (1). All Unity math types — unavoidable in a foveation policy class; within the ≤ 14 limit.

### 6.3 Delta Summary

Splitting `VolumeDataSetRenderer` into nine focused classes eliminates every CK threshold
violation. Total WMC falls from 74 to a maximum of 20 per class (and no more than 60
summed across the five core classes, replacing a single class that carried 74 alone).
CBO drops from 31 to a maximum of 11 per domain class, breaking the 46-file dependency
cycle that carried a 39.8% propagation cost. LCOM collapses from 0.81 — the signature of
four unrelated field clusters in one class — to ≤ 0.05 per class, each of which operates
on a single field cluster by construction. Every proposed class meets or beats the brief's
NFR thresholds. The two classes closest to their limits (`VolumeMaterialBinder` at CBO = 11
and `VolumeTextureManager` at WMC = 20) have documented per-member inventories in their
file headers; neither has headroom to accumulate further technical debt without crossing
the target line, which is an intentional design signal.

Full before/after numbers using the Understand tool's raw formula are in
`docs/metrics-worksheet.md §2–3`.

---

## 7. Class and Sequence Diagrams

*Brief reference: Section 9.2 Deliverable 2 — diagrams must be PlantUML/Mermaid source*

### 7.1 Before: Current Class Structure

- Reference `diagrams/class-before.puml` — embed rendered image if panel permits; otherwise reference file.
- Caption: highlight the single class and its coupling count.

### 7.2 After: Proposed Class Structure

- Reference `diagrams/class-after.puml`.
- Caption: five classes, interfaces, and adapter pattern visible.

### 7.3 Component / Architecture Diagram

- Reference `diagrams/architecture.puml`.
- Annotates inward/outward dependencies and the `IRenderPipeline` boundary.

### 7.4 Render-Frame Sequence

- Reference `diagrams/sequence-render-frame.puml`.
- Summarise the 8-step render-frame sequence (Update → camera → foveation → texture → material → mask → pipeline → GPU).

---

## 8. SOLID/GRASP Audit

*Brief reference: Section 9.2 Deliverable 2 — architectural constraints Section 4.2*
*Full evidence catalogue with code line references: `docs/Codebase Exploration/SOLID_GRASP_Violations.md`*

### 8.1 Violations in Current Code

The table below summarises every confirmed SOLID and GRASP violation in `VolumeDataSetRenderer.cs`
at commit `1cd729f`. Severity is rated Critical / High / Medium based on blast radius and
the number of files that must change to address the violation.

| ID | Principle | Violation | Class / File | Lines | Severity |
|----|-----------|-----------|-------------|-------|----------|
| V-01 | SRP | 9 distinct responsibilities in one class (FITS load, texture upload, shader binding, mask paint, mask I/O, coordinate conversion, crop management, WCS, Unity lifecycle) | `VolumeDataSetRenderer.cs` | 1–1,402 | 🔴 Critical |
| V-02 | SRP | `SaveMask()` mixes file-path logic, native plugin calls, and UI toasts in 90 lines | `VolumeDataSetRenderer.cs` | 1290–1378 | 🟠 High |
| V-03 | SRP | `SetCursorPosition()` conflates coordinate conversion, data lookup, outline update, and paint trigger in 60 lines | `VolumeDataSetRenderer.cs` | 639–698 | 🟠 High |
| V-04 | OCP | Adding a new mask mode requires editing `VolumeDataSetRenderer.cs`, `BasicVolume.cginc`, and `PaintMenuController.cs` — 4 files, 8 code blocks | `VolumeDataSetRenderer.cs`, `BasicVolume.cginc` | 47, 1072, shader lines 281–430 | 🔴 Critical |
| V-05 | OCP | `ProjectionMode` toggle is a binary keyword if/else — a third projection mode requires modifying existing code | `VolumeDataSetRenderer.cs` | 1099–1103 | 🟡 Medium |
| V-06 | ISP | 152 public members exposed on one class; consumers such as `PaintMenuController` and `RenderingController` depend on the full surface but use only 3–5 members | `VolumeDataSetRenderer.cs` | All | 🔴 Critical |
| V-07 | DIP | `Config.Instance` singleton accessed directly — 8 field reads scattered across `_startFunc` and `Update` | `VolumeDataSetRenderer.cs` | 361, 553, 644 | 🟠 High |
| V-08 | DIP | `FindObjectOfType<VolumeInputController>()` and `FindObjectOfType<VolumeCommandController>()` — concrete scene-search; removed in Unity 6 | `VolumeDataSetRenderer.cs` | 381, 522 | 🟠 High |
| V-09 | DIP | `AddComponent(typeof(MomentMapRenderer))` — renderer instantiates a concrete collaborator at runtime; untestable and unswappable | `VolumeDataSetRenderer.cs` | 517 | 🟠 High |
| V-10 | DIP | Coordinate-conversion domain logic calls `transform.InverseTransformPoint()` — `UnityEngine` type dependency inside domain method; violates the brief's mandatory constraint | `VolumeDataSetRenderer.cs` | 616, 627 | 🔴 Critical |
| V-11 | GRASP Information Expert | Data value lookup (`GetDataValue`) performed by the renderer, not by `VolumeDataSet` which owns the data | `VolumeDataSetRenderer.cs` | 657 | 🟠 High |
| V-12 | GRASP Creator | `_startFunc` constructs 5 outline objects, 1 moment-map renderer, and 2 material instances — the renderer acts as a factory for objects it does not semantically own | `VolumeDataSetRenderer.cs` | 410–517 | 🟠 High |
| V-13 | GRASP Controller | `CropToRegion()` performs input validation, data loading, material state update, and outline update in one method — use-case and domain logic collapsed together | `VolumeDataSetRenderer.cs` | 909–940 | 🔴 Critical |
| V-14 | GRASP Indirection | No abstraction layer between Unity lifecycle hooks and domain logic; domain code calls `FindObjectOfType`, `GetComponentInChildren`, `Config.Instance` directly | `VolumeDataSetRenderer.cs` | 381–382, 644 | 🔴 Critical |
| V-15 | GRASP Protected Variations | Three confirmed variation points (render pipeline, input provider, data format) have zero interface protection; pipeline migration guaranteed by Unity 6 target | Renderer + shader files | 379, 381, 1142 | 🔴 Critical |
| V-16 | GRASP Low Coupling | CBO ~31 (Day 2 baseline); 12 concrete class dependencies, 0 interface dependencies | `VolumeDataSetRenderer.cs` | Multiple | 🔴 Critical |
| V-17 | GRASP High Cohesion | LCOM ~0.81; mask-painting methods share zero fields with coordinate methods — unrelated clusters coexist in one class | `VolumeDataSetRenderer.cs` | Multiple | 🔴 Critical |

**Summary counts:** 6 Critical, 8 High, 1 Medium violations across all SOLID principles and 7 of 9 GRASP patterns. LSP is not violated in the current code (no domain inheritance hierarchy). Pure Fabrication is not applicable at this stage.

---

### 8.2 Fixes in Proposed Design

Each violation is resolved by one of the four architecture decisions (DD-01 to DD-04) or by the four-class split (DD-03) directly. The mapping is:

| Violation | Resolved by | How |
|-----------|------------|-----|
| V-01 — SRP (9 responsibilities) | DD-03 | Four-class split distributes each responsibility to exactly one class; `VolumeRenderCoordinator` is the only class with more than one concern and it is a pure coordinator with no domain logic |
| V-02 — SRP (`SaveMask`) | DD-03 | `MaskPersistenceService` owns all file-path logic and native plugin calls; events notify the UI layer |
| V-03 — SRP (`SetCursorPosition`) | DD-03 | `VolumeCameraDriver` handles coordinate conversion and outline update; `MaskEditor` handles data lookup and paint trigger |
| V-04 — OCP (mask modes) | DD-02 | `IMaskMode` Strategy pattern — new mode = new class, zero existing files changed; worked example in `refactoring-examples/example2-MaskModes/` |
| V-05 — OCP (projection mode) | DD-02 | `IProjectionMode` interface applies the same Strategy pattern; outside worked-example scope but documented as an extension point |
| V-06 — ISP (152 public members) | DD-03 | Four narrow interfaces introduced: `IVolumeRenderer` (5 members), `IMaskEditor` (4), `ICursorProvider` (4), `ICropService` (3); all within the brief's ≤ 7 member target |
| V-07 — DIP (`Config.Instance`) | DD-03 | `IAppConfig` injected via constructor into `VolumeTextureManager`; no class reads a global singleton |
| V-08 — DIP (`FindObjectOfType`) | DD-03 | All collaborators injected at composition root; `FindObjectOfType` calls removed |
| V-09 — DIP (`AddComponent`) | DD-03 | `IMomentMapRenderer` injected; concrete `MomentMapRenderer` wired externally |
| V-10 — DIP (`UnityEngine` in domain) | DD-01 + DD-03 | `VolumeCoordinateService` takes a `Matrix4x4` value type parameter; `IRenderPipeline` boundary ensures the domain core never imports `UnityEngine.Rendering.*` |
| V-11 — GRASP Information Expert | DD-03 | `VolumeCoordinateService` delegates data queries to the data layer via `IVolumeDataSource`; the renderer receives results, not raw data |
| V-12 — GRASP Creator | DD-03 | Composition root constructs all collaborators; `VolumeRenderCoordinator` accepts them via constructor injection |
| V-13 — GRASP Controller | DD-03 | `CropService` owns the crop use-case; `VolumeTextureManager` handles data loading; `VolumeMaterialBinder` handles material update; no single method spans all three |
| V-14 — GRASP Indirection | DD-01 + DD-03 | `IRenderPipeline` is the anti-corruption layer between the domain and Unity rendering APIs; `IAppConfig` is the indirection for configuration |
| V-15 — GRASP Protected Variations | DD-01 | `IRenderPipeline` is the stable interface around the render-pipeline variation point; `IVolumeDataSource` protects the data-format variation point; `IInputProvider` (Sub-team 4) protects the input variation point |
| V-16 — GRASP Low Coupling | DD-01 + DD-03 | Per-class CBO targets: `VolumeMaterialBinder` ≤ 8, `VolumeTextureManager` ≤ 6, `VolumeCameraDriver` ≤ 6, `FoveatedSamplingPolicy` = 0; all well within brief thresholds |
| V-17 — GRASP High Cohesion | DD-03 | Each extracted class operates on a single field cluster; projected LCOM < 0.10 per class |

---

### 8.3 Remaining Trade-offs

Two violations cannot be fully eliminated given the brief's scope constraints; both are documented here as required by brief §4.2.

**Trade-off T-01 — `VolumeRenderCoordinator` orchestrator coupling (CBO ~6)**
`VolumeRenderCoordinator` is the composition root for the four sub-classes. It holds references to `VolumeMaterialBinder`, `VolumeTextureManager`, `VolumeCameraDriver`, and `FoveatedSamplingPolicy` — giving it a CBO of ~6 even after the split. This is acceptable: the brief permits CBO ≤ 25 for orchestrators, and the coupling is to interfaces, not concrete classes. The coordinator has no domain logic of its own, so its reason-to-change is limited to wiring changes.

**Trade-off T-02 — `MonoBehaviour` lifecycle coupling**
A thin `MonoBehaviour` shell must remain as the Unity entry point (`Start`, `Update`, `OnDestroy`). This class will have a `UnityEngine` dependency by definition — there is no way to remove it while targeting the Unity platform. The mitigation is to keep this shell to under 30 lines with zero domain logic; all actual work is delegated immediately to the coordinator. This is documented as an acceptable `UnityEngine` dependency at the adapter layer, consistent with the brief's constraint that *domain* code must not transitively depend on `UnityEngine`, not that no class may ever use it.

---

## 9. Sub-team Dependencies

*Brief reference: Section 9.2 — cross-team interface contracts*

### 9.1 Dependency on Sub-team 2 (Data I/O)

- `VolumeTextureManager` consumes volume data via `IRawVolumeDataSource` / `RawVolumeData` struct.
- Show the interface contract (or stub) and confirm status (pending as of 25 May).

### 9.2 Dependency on Sub-team 4 (Interaction)

- `FoveatedSamplingPolicy` consumes gaze data via `IGazeProvider`.
- Show the interface contract (or stub) and confirm status (pending as of 25 May; `MockGazeProvider` in place).

### 9.3 What We Provide to Other Sub-teams

- Camera state exposed by `VolumeCameraDriver` (consumed by Sub-team 4 if needed).
- Confirm no upstream dependencies from us on Sub-teams 1, 5, 6, or 7.

---

## 10. Risks and Trade-offs

This section covers risks introduced by the proposed architecture, including three structural
risks that require the most detailed treatment: the performance overhead of the new abstraction
layer, the risk of coordinator complexity growth, and the long-term versioning risk on published
interfaces. Each risk is followed by a concrete mitigation; the full risk register is summarised
in the table at §10.4.

### 10.1 Performance Overhead of the Abstraction Layer

#### The Risk

The proposed architecture introduces four interface boundaries where the current code has none:
`IRenderPipeline`, `IMaskMode`, `IGazeProvider`, and `IRawVolumeDataSource`. Every call that
crosses an interface in C# is a virtual dispatch — the runtime resolves the concrete
implementation via an indirect function call rather than a direct inlined call. In a
standard desktop application the cost of a virtual dispatch is negligible (typically 1–3 ns).
In iDaVIE it is not automatically negligible, because the two most latency-sensitive call sites
are on the per-frame hot path: `IMaskMode.Apply()` is called every `Update()`, and
`IGazeProvider.GetGazeDirection()` is called by `FoveatedSamplingPolicy` every `Update()`.

At 90 fps the total frame budget is approximately 11.1 ms. If virtual dispatch overhead on the
hot path accumulates — for example, if `VolumeMaterialBinder.Bind()` makes ten separate
interface calls within a single frame — the aggregate cost could theoretically approach 30–50 ns,
which is below the frame budget by more than three orders of magnitude. However, the Unity IL2CPP
ahead-of-time compiler de-virtualises interface calls at build time when the concrete type is
sealed and can be statically determined; all four concrete domain classes in the proposal are
`sealed`, which enables this optimisation on all target platforms.

The more credible performance risk is not virtual dispatch overhead but **indirection in the
material binding path**. Under the current monolith, `VolumeDataSetRenderer.Update()` writes
shader properties directly via `_materialInstance.SetFloat()`, `_materialInstance.SetTexture()`,
and `Shader.EnableKeyword()`. The refactored path adds one extra method call per property write
(through `IVolumeMaterialBinder.Bind(VolumeRenderState)`). The risk is that the `VolumeRenderState`
value struct, which is passed by value across the interface on every frame, incurs a copy cost
if it grows too large. At its current specified size (seven fields: one `int`, four `float`s, one
`bool`, one `Texture3D` reference), the struct is 36 bytes — well within the 64-byte L1 cache
line threshold below which struct-copy overhead is unmeasurable in Unity IL2CPP builds.

#### Mitigation

Three controls are in place. First, `VolumeRenderState` is constrained by design decision
(§5.2) to carry only the fields that `VolumeMaterialBinder` reads per frame; all other state
lives in the individual classes. The struct size must not exceed 64 bytes; this is documented
as a NFR in `docs/requirements.md` (NFR-08). Second, every `IMaskMode` and
`IVolumeMaterialBinder` concrete implementation is `sealed`, enabling IL2CPP de-virtualisation.
Third, the 90 fps invariant (INV-01) is enforced at the integration test level: a Unity
Editor performance test benchmarks the full render loop (stubbed GPU) and fails the build if
per-frame CPU time exceeds 2 ms on the reference development machine. This gate catches
regressions at CI time before they reach the maintainer panel or a production build.

### 10.2 Coordinator Complexity

#### The Risk

`VolumeRenderCoordinator` is the composition root for the four domain classes. Every wiring
decision in the architecture — which `IMaskMode` is active, whether `IGazeProvider` has
delivered a valid direction this frame, whether the texture needs evicting — ultimately
passes through the coordinator. This creates a risk that `VolumeRenderCoordinator` accumulates
orchestration logic over time and evolves into a second God Class.

The Sprint 1 CK baseline for `VolumeDataSetRenderer` — WMC = 44, CBO = 45, LCOM = 0.81 —
was not the result of a single bad design decision; it was the result of incremental
accumulation over 7 years and 123 commits by 9 authors. Each individual addition to the
class was locally reasonable. The aggregate was not. The same accumulation dynamic applies
to `VolumeRenderCoordinator` if the class is not actively constrained.

The secondary risk is **wiring correctness**: the coordinator constructs all four domain
classes in its `Start()` method. If construction order, null-check ordering, or fallback
assignment contains a bug, all four classes fail simultaneously with a single root cause.
Under the current monolith, at least the failure is local to one class. Under the proposed
coordinator pattern, a wiring bug propagates to everything the coordinator touches.

#### Mitigation

Three constraints prevent coordinator bloat. First, `VolumeRenderCoordinator` is explicitly
prohibited from containing domain logic: the class specification (§5.2) states that its
`Start`/`Update`/`OnDestroy` methods must delegate immediately, with a target WMC ≤ 10 and
a maximum line count of 100 (excluding blank lines and comments). Any method that computes
a value — rather than delegating its computation — belongs in one of the four domain classes.
This constraint is enforced in code review using the SonarQube gate, which is configured to
fail on any new method in `VolumeRenderCoordinator` with cyclomatic complexity > 2.

Second, construction is validated by a dedicated integration test: `CoordinatorWiringTest`
in `iDaVIE.Rendering.Tests` constructs a `VolumeRenderCoordinator` with all four test doubles
injected (see §5.2), calls `Start()`, asserts that each double received exactly one
initialization call, and verifies that `Update()` produces the expected sequence of calls
without throwing. This test catches wiring-order bugs at CI time.

Third, the coordinator's CBO is bounded to ≤ 6 by the architecture: it knows the four domain
class interfaces and the `MonoBehaviour` base — nothing else. If a new dependency is proposed
for the coordinator, the design must first justify why no existing domain class should own it.
This is documented as a review checklist item in `docs/test-strategy.md`.

### 10.3 Interface Versioning Risk

#### The Risk

The proposed architecture exposes four interfaces at assembly boundaries:
`IRenderPipeline` (between domain and Unity adapters), `IMaskMode` (between
`VolumeMaterialBinder` and mask implementations), `IGazeProvider` (between rendering and
Sub-team 4's SteamVR integration), and `IRawVolumeDataSource` (between rendering and
Sub-team 2's data layer). The brief (§4.2 constraint 5) requires that plug-in contracts
be semantically versioned and ABI-stable within a major version. This constraint
creates a versioning obligation that the current monolith does not have — precisely
because the current monolith has no interfaces at all.

The risk manifests in two scenarios. In the near term, Sub-team 2 or Sub-team 4 may
deliver their concrete implementations with method signatures that differ from our stubs.
The `IGazeProvider` stub (as of 27 May) exposes `GetGazeDirection()` returning a `Vector3`
and `IsGazeAvailable` as a bool property; if Sub-team 4's SteamVR wrapper names these
differently or uses a `Quaternion` return type, both the interface and every consumer must
be updated simultaneously. In the longer term, Unity 6 LTS may deprecate the
`CameraEvent` enum used as the second parameter of `IRenderPipeline.AddCommandBuffer()`;
a Unity major version upgrade would require a breaking change to `IRenderPipeline` that
invalidates both `UrpRenderPipeline` and `HdrpRenderPipeline`.

The secondary versioning risk is **semantic drift**: an interface method's name and
signature can remain identical while its documented contract changes. If Sub-team 4
changes the coordinate frame in which `GetGazeDirection()` is expressed (for example,
from world-space to head-local-space), `FoveatedSamplingPolicy` will produce incorrect
sample rates silently — no compilation error, no test failure unless the contract
semantics are separately enforced.

#### Mitigation

All four interfaces carry a `[InterfaceVersion("1.0")]` attribute defined in
`iDaVIE.Rendering` (see `refactoring-examples/stubs/`). The version attribute is
checked at assembly-load time by a reflection guard in `VolumeRenderCoordinator.Start()`:
if the injected concrete type's assembly declares a mismatched version, a
`RenderingInterfaceVersionException` is thrown immediately with a diagnostic message
identifying the caller. This converts a silent mismatch into a loud, actionable error.

For cross-team interfaces specifically (`IGazeProvider` and `IRawVolumeDataSource`),
the agreed contract is expressed as a shared stub file held in a neutral location
(`refactoring-examples/stubs/`), not in either sub-team's private assembly. Changes to
these stubs require a review comment in the shared file header before any implementation
is updated; this procedure is documented in `docs/test-strategy.md` §4.2
(cross-team contract change process).

For the `IRenderPipeline` Unity-API risk, the mitigation is the adapter pattern itself:
a future `CameraEvent` deprecation touches only the `AddCommandBuffer` implementation
in `UrpRenderPipeline`, not the interface signature or any domain consumer. The interface
is defined in terms of domain concepts (command buffer injection, keyword control), not
Unity API terms, so most Unity version changes map to implementation details rather than
interface changes.

Semantic drift is mitigated by contract tests: `IGazeProviderContractTest` and
`IRawVolumeDataSourceContractTest` verify that each concrete implementation obeys the
documented coordinate conventions and return-value invariants. These run against the
stub implementations today and will run against the real implementations when delivered.

### 10.4 Risk Register Summary

| Risk ID | Description | Likelihood | Impact | Mitigation |
|---------|-------------|-----------|--------|------------|
| R-01 | `IGazeProvider` contract not delivered by Sub-team 4 before artefact freeze | Medium | Low | `MockGazeProvider` stub covers all unit tests; design does not change |
| R-02 | `RawVolumeData` struct changes after `VolumeTextureManager` is written | Medium | Medium | Interface adaptor pattern; only one conversion point to update |
| R-03 | URP command buffer API changes in Unity 6 LTS | Low | High | `IRenderPipeline` abstraction isolates changes to `UrpRenderPipeline` only — see §10.3 |
| R-04 | `VolumeRenderState` struct grows beyond 64 bytes, introducing copy overhead on hot path | Low | Medium | NFR-08 caps struct size; SonarQube gate flags field additions — see §10.1 |
| R-05 | `VolumeRenderCoordinator` accumulates domain logic over time (second God Class) | Medium | High | WMC ≤ 10 gate in SonarQube; CC > 2 per method is a build failure — see §10.2 |
| R-06 | Interface semantic drift between sub-teams (`IGazeProvider` coordinate frame change) | Medium | Medium | `[InterfaceVersion]` attribute + reflection guard; contract test suite — see §10.3 |
| R-07 | Projected CK metrics not achievable within 90 fps constraint | Low | High | Performance CI gate benchmarks full render loop at 90 fps; CK targets are not met unless this gate passes — see §10.1 |
| R-08 | Design proposal rejected by maintainer panel at pitch | Low | Medium | SOLID/GRASP audit and CK evidence make a data-driven case; all trade-offs are documented here and in §8.3 |

---

## 11. Appendices

### Appendix A — Diagram Source Files

- List of all `.puml` files in `diagrams/` with one-line description each.

### Appendix B — Interface Stubs

- Reference to `refactoring-examples/stubs/` — `IRenderPipeline.cs`, `NullRenderPipeline.cs`, `IMaskMode.cs`, `NullMaskMode.cs`.

### Appendix C — Glossary

- CK metrics abbreviations (WMC, DIT, NOC, CBO, RFC, LCOM).
- Domain terms: ray-marching, foveated rendering, FITS cube, voxel, mask mode.

### Appendix D — References

- iDaVIE GitHub repository: https://github.com/idia-astro/iDaVIE
- Brief sections: 4.2 (Architectural Constraints), 6.3 (Section 6.3 Deliverables), 9.2 (Sub-team Deliverables).
- CK metric thresholds: brief §NFR table (replicated in `docs/requirements.md`).
