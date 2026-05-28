# Refactoring Example 1: VolumeDataSetRenderer Four-Class Split
**Team Alpha | Cache Me If You Can — Sub-team 3**  
*Brief reference: §9.2 Sub-team deliverable 3 — worked refactoring example with CK metrics; Section 6.3 Software Construction — SRP, DIP, ISP, GRASP*

---

## What This Example Shows

`VolumeDataSetRenderer` is a 1,402-line monolith with eight distinct responsibility clusters crammed into one class. This example demonstrates how to break it into four focused classes using the Single Responsibility Principle (SRP), backed by interfaces that satisfy the Dependency Inversion Principle (DIP) and Interface Segregation Principle (ISP).

The four extracted classes:

| New Class | Single Responsibility |
|-----------|----------------------|
| `VolumeMaterialBinder` | Shader keyword management, material property binding, colour-map application |
| `VolumeTextureManager` | 3D texture creation, GPU upload, memory-budget enforcement, cropped-region management |
| `VolumeCameraDriver` | Camera matrix extraction, clip-plane computation, coordinate conversion, projection mode |
| `FoveatedSamplingPolicy` | Per-frame foveation decisions: sample rates, LOD bias, reprojection mask, HMD-absent fallback |

A thin `VolumeRenderCoordinator` (pure C# orchestrator) + `VolumeRendererBehaviour` (MonoBehaviour shell ≤ 30 lines) replace the monolith's Unity lifecycle. The coordinator has no domain logic of its own — it drives the per-frame loop by delegating to the four injected interfaces.

---

## Before: The Monolith

See `before/VolumeDataSetRenderer.cs` — the full original source with 20+ inline violation markers.

### The Problem

The class has eight responsibility clusters in one 1,402-line file:

1. **FITS texture upload** — allocates `Texture3D`, enforces 368 MB budget
2. **Shader / material binding** — 20+ shader property IDs pushed to GPU each frame
3. **Mask modes** — if/else chain on `MaskMode` enum, 3 branches calling `EnableKeyword` directly
4. **Mask I/O** — native plugin calls, file-path logic, UI toasts — all in `SaveMask()`
5. **Coordinate conversion** — `transform.InverseTransformPoint` calls in domain math (DIP violation)
6. **Cursor / outline management** — data lookup, outline update, paint trigger mixed in one method
7. **Foveated rendering** — sample-rate decisions interleaved with material binding
8. **Unity lifecycle** — `Start`, `Update`, `OnDestroy` mixed with domain logic

### Before CK Metrics (Day 2 baseline — CK-equivalent)

| Metric | Measured | Target | Status |
|--------|----------|--------|--------|
| WMC | 44 | ≤ 20 | ❌ |
| DIT | 2 | ≤ 4 | ✅ |
| NOC | 0 | ≤ 5 | ✅ |
| CBO | 45 | ≤ 14 | ❌ |
| RFC | 89 | ≤ 50 | ❌ |
| LCOM | 0.81 | ≤ 0.5 | ❌ |

*Source: `diagrams/class-before.puml`; see `docs/metrics-worksheet.md` Section 1 for raw Understand tool output.*

Key observations:

- **CBO = 45** — Ce = 17 efferent, Ca = 28 afferent; 12 concrete class dependencies, zero interface dependencies. Inside a 46-file dependency cycle; propagation cost 39.8%.
- **LCOM = 0.81** — mask methods share no fields with camera methods; texture methods share no fields with either. Three unrelated clusters driving LCOM above 0.5.
- **WMC = 44** — four methods exceed CC = 5 individually (`_startFunc` CC=28, `SaveMask` CC=19); removing any one responsibility drops WMC by ~8.
- **152-member public API** — violates Interface Segregation Principle.
- `Graphics.DrawProceduralNow` in `OnRenderObject()` — URP-incompatible built-in API.

---

## After: Four Focused Classes

See `after/` for fully annotated implementations.

### The Solution

Each responsibility cluster is assigned to exactly one class. The coordinator re-composes the four domain classes each frame without containing any logic of its own:

```
VolumeRendererBehaviour  (MonoBehaviour shell — adapter layer only, ≤ 30 lines)
    │
    └── VolumeRenderCoordinator  (pure C# orchestrator — no domain logic)
            │
            ├── IVolumeMaterialBinder      →  VolumeMaterialBinder
            ├── IVolumeTextureManager      →  VolumeTextureManager
            ├── IVolumeCameraDriver        →  VolumeCameraDriver
            └── IFoveatedSamplingPolicy    →  FoveatedSamplingPolicy
```

`VolumeRenderCoordinator` never calls `new` on a concrete domain type — every collaborator is injected at construction time by `VolumeRendererBehaviour.Start()` (the composition root).

### After CK Metrics (Day 13 projected)

| Class | WMC | DIT | NOC | CBO | RFC | LCOM | Meets target? | Evidence file |
|-------|-----|-----|-----|-----|-----|------|---------------|---------------|
| `VolumeRenderCoordinator` | 12 | 0 | 0 | ≤ 6 | ≤ 20 | 0.0 | ✅ | `after/VolumeRenderCoordinator.cs` |
| `VolumeMaterialBinder` | 16 | 0 | 0 | ≤ 11 | ≤ 22 | 0.05 | ✅ | `after/VolumeMaterialBinder.cs` |
| `VolumeTextureManager` | 15 | 0 | 0 | ≤ 8 | ≤ 20 | 0.05 | ✅ | `after/VolumeTextureManager.cs` |
| `VolumeCameraDriver` | 9 | 0 | 0 | ≤ 4 | ≤ 12 | 0.0 | ✅ | `after/VolumeCameraDriver.cs` |
| `FoveatedSamplingPolicy` | 7 | 0 | 0 | 6 | ≤ 15 | 0.0 | ✅ | `after/FoveatedSamplingPolicy.cs` |

*Every after/ file header contains `[CBO]`, `[WMC]`, and `[LCOM]` annotations tracing each metric to specific lines.*

---

## CK Delta

| Metric | Before (VolumeDataSetRenderer) | After (peak across new classes) | Improvement |
|--------|-------------------------------|----------------------------------|-------------|
| WMC | 44 | 16 (`VolumeMaterialBinder`) | −64% on worst-case class; complexity is now local and bounded |
| CBO | 45 | ≤ 11 (`VolumeMaterialBinder`) | −76%; zero concrete domain class dependencies remain |
| RFC | 89 | ≤ 22 (`VolumeMaterialBinder`) | −75% |
| LCOM | 0.81 | 0.05 (`VolumeMaterialBinder`, `VolumeTextureManager`) | −94%; every class operates on a single field cluster |
| LOC | 1,402 | ~150–200 per class | 7× smaller per class |

The coordinator (WMC = 12) is the only class above WMC = 10. This is driven by five null-guard branches in its constructor. Extracting these to a `RequireArg<T>` helper reduces it to WMC = 8 — a Sprint 3 candidate if the risk register gate triggers.

---

## SOLID/GRASP Analysis

| Principle | Before | After |
|-----------|--------|-------|
| **SRP** | ❌ 8+ responsibilities in one class (V-01–V-03) | ✅ Each class has exactly one responsibility |
| **OCP** | ❌ New mask mode requires editing VDSR + 3 other files | ✅ Covered by `IMaskMode` Strategy (see Example 2) |
| **ISP** | ❌ 152 public members; consumers depend on the full surface (V-06) | ✅ 5 narrow interfaces, each ≤ 7 members |
| **DIP** | ❌ `Config.Instance`, `FindObjectOfType`, `transform.InverseTransformPoint` in domain (V-07–V-10) | ✅ All collaborators injected; `VolumeCoordinateService` takes `Matrix4x4` — zero Unity runtime dependency in domain math |
| **GRASP Controller** | ❌ `CropToRegion()` mixed input validation, data loading, material update, moment map (V-13) | ✅ Each concern delegated to its owner class; coordinator re-composes |
| **GRASP Low Coupling** | ❌ CBO = 45; 12 concrete deps, 0 interfaces (V-16) | ✅ CBO ≤ 11 worst case; every coupling is to an interface or value type |
| **GRASP High Cohesion** | ❌ LCOM = 0.81; three unrelated field clusters (V-17) | ✅ LCOM ≤ 0.05; single field cluster per class |

Full violation-to-design-decision mapping with code locations: `docs/design-document.md` §8.2.

---

## Key Design Decisions

### DD-03 — Four-Class Split (SRP)

The split follows the LCOM clusters: field sets with no shared access define the class boundaries. `VolumeMaterialBinder` owns the material/shader field cluster; `VolumeTextureManager` owns the texture/config cluster; `VolumeCameraDriver` owns the camera/transform cluster; `FoveatedSamplingPolicy` owns the gaze/zone config cluster.

### DD-01 — IRenderPipeline Boundary (DIP)

`VolumeMaterialBinder` and the coordinator never call `Shader.EnableKeyword` or `Graphics.DrawProceduralNow` directly. All pipeline calls go through `IRenderPipeline`. This satisfies the mandatory constraint in brief §4.2(3): domain rendering math must not transitively depend on `UnityEngine` or `SteamVR` types.

### VolumeCoordinateService (DIP — nested in VolumeCameraDriver)

`transform.InverseTransformPoint` in the before/ code (lines 713, 739, 857) is a `UnityEngine` API call inside domain math — untestable without a running scene. `VolumeCoordinateService.WorldToObjectSpace(Vector3, Matrix4x4)` performs identical arithmetic with zero Unity runtime dependency. The coordinator passes a matrix; no other class touches `Transform` or `Camera` directly.

### Constructor Injection throughout

`VolumeRendererBehaviour.Start()` is the sole composition root. It instantiates all four concrete classes and passes them to `VolumeRenderCoordinator` as interface references. No class inside `iDaVIE.Rendering` calls `FindObjectOfType`, `GetComponent`, or `AddComponent`.

---

## Test Impact

| Test type | Before | After |
|-----------|--------|-------|
| Unit test domain coordinate math | ❌ Requires live `Transform` in a running scene | ✅ `VolumeCoordinateService` takes `Matrix4x4`; pure NUnit, no Unity context needed |
| Unit test material binding | ❌ Requires `Material` asset + `Shader.EnableKeyword` (GPU) | ✅ Inject `NullRenderPipeline`; verify keyword calls without GPU |
| Unit test texture budget enforcement | ❌ Requires `Texture3D` allocation (GPU) | ✅ Inject `StubVolumeDataSource`; budget logic is pure integer arithmetic |
| Unit test foveated zone selection | ❌ Requires HMD hardware (`SteamVR.Init`) | ✅ Inject `StubGazeProvider`; zone selection is pure float arithmetic |
| Integration test full frame | ❌ Full Unity player required | ⚠️ `VolumeRendererBehaviour` still requires player; coordinator is testable in edit mode with all stubs injected |

Test doubles: `NullRenderPipeline` (`stubs/NullRenderPipeline.cs`), `StubGazeProvider` (`after/FoveatedSamplingPolicy.cs`), `StubCameraDriver` (`after/VolumeCameraDriver.cs`).

---

## Open Items (blocked on cross-team interface confirmation)

| Item | Blocker |
|------|---------|
| `VolumeTextureManager` uses `VolumeDataSet` directly | Sub-team 2 `RawVolumeData` struct (task S2-CO09) |
| `IGazeProvider` GazeFocusPoint coord space not confirmed | Sub-team 4 interface delivery (task S2-CO08) |

See `PROGRESS.md` blockers table for current status.
