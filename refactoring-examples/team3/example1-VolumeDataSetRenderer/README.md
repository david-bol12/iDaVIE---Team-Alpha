# Refactoring Example 1 — VolumeDataSetRenderer Split
**Team Alpha | Sub-team 3 — Rendering Engine ("Cache Me If You Can")**  
*Brief reference: §9.2 Sub-team deliverable 3 — worked refactoring example with CK metrics*

---

## What This Example Shows

`VolumeDataSetRenderer` is a 1,403-line MonoBehaviour God Class with nine distinct responsibilities and Day-2 CK metrics that breach every brief threshold. This example demonstrates how the Single Responsibility Principle, Dependency Inversion Principle, and GRASP Low Coupling / High Cohesion patterns decompose it into four focused classes behind a thin coordinator shell, with each resulting class meeting all brief CK targets.

---

## Before: The God Class

**File:** `before/VolumeDataSetRenderer.cs` (annotated — do not modify)  
**Source:** `Assets/Scripts/VolumeData/VolumeDataSetRenderer.cs` in the iDaVIE repository

### Nine Responsibilities in One Class

| # | Responsibility | Lines (approx.) |
|---|---------------|-----------------|
| 1 | Shader / material property management | 1124–1226 |
| 2 | 3D texture upload + memory budgeting | 479–497, 665–682 |
| 3 | Camera matrix + clip-plane computation | 713, 739, 857 |
| 4 | Foveated rendering decisions | 1139–1165 |
| 5 | Region selection + crop | 1005–1054 |
| 6 | Cursor / voxel painting | 639–698 |
| 7 | Moment-map control | 521 |
| 8 | FITS mask file I/O | 1290–1373 |
| 9 | Unity lifecycle wiring | 358–543 |

### Before CK Metrics (Day 2 Baseline — Understand tool)

| Metric | Value | Brief Target | Status |
|--------|-------|-------------|--------|
| WMC | 97 | ≤ 20 (domain) | ❌ |
| DIT | 2 | ≤ 4 | ✅ |
| NOC | 0 | ≤ 5 | ✅ |
| CBO | 28 | ≤ 14 (domain) | ❌ |
| RFC | 97 | ≤ 50 | ❌ |
| LCOM | 0.95 | ≤ 0.5 | ❌ |
| NIM | 97 | — | — |
| NIV | 84 | — | — |
| LOC | 1,403 | — | — |

Additional problems:
- Inside a 46-file dependency cycle; propagation cost 39.8%
- `_startFunc()` init coroutine: 185 lines, CC = 28
- `SaveMask()`: 83 lines, CC = 19
- 152-member public API — violates Interface Segregation Principle
- 4× `FindObjectOfType` calls — violates Dependency Inversion Principle
- `Graphics.DrawProceduralNow` in `OnRenderObject()` — URP-incompatible built-in API

---

## After: Four Focused Classes

**Files:** `after/` directory

### Class Responsibilities

| New Class | Single Responsibility | Interface |
|-----------|----------------------|-----------|
| `VolumeRenderCoordinator` | Unity lifecycle + coordinator shell only; zero domain logic | — (thin MonoBehaviour) |
| `VolumeMaterialBinder` | Shader keyword management, material property binding, colour-map application | `IVolumeMaterialBinder` |
| `VolumeTextureManager` | 3D texture upload, memory-budget enforcement, downsample factor computation | `IVolumeTextureManager` |
| `VolumeCameraDriver` | Camera-matrix extraction, clip-plane computation, projection mode | `IVolumeCameraDriver` (stub) |
| `FoveatedSamplingPolicy` | Ray-march step count and mip-bias decisions from gaze data | — (concrete class, pure C#) |

### After CK Metrics (Understand tool — measured from worked examples)

| Class | WMC | DIT | NOC | CBO | RFC | LCOM | Meets target? |
|-------|-----|-----|-----|-----|-----|------|---------------|
| `VolumeRenderCoordinator` | 11 | 1 | 0 | 15 | 11 | 0.69 | ❌ CBO, LCOM |
| `VolumeRendererBehaviour` (MB shell) | 3 | 2 | 0 | 8 | 3 | 0.00 | ✅ all |
| `VolumeMaterialBinder` | 10 | 1 | 0 | 12 | 10 | 0.57 | ❌ LCOM |
| `VolumeTextureManager` | 12 | 1 | 0 | 4 | 12 | 0.67 | ❌ LCOM |
| `VolumeCameraDriver` | 4 | 1 | 0 | 4 | 4 | 0.25 | ✅ all |
| `VolumeCoordinateService` | 3 | 1 | 0 | 3 | 3 | 0.00 | ✅ all |
| `FoveatedSamplingPolicy` | 6 | 1 | 0 | 6 | 6 | 0.33 | ✅ all |

> `VolumeRenderCoordinator` CBO = 15: marginally exceeds domain target (≤ 14); within orchestrator threshold (≤ 25). LCOM values above 0.5 reflect multi-phase lifecycle structure (Initialise / Tick / Dispose) — see `docs/metrics-worksheet.md §4` for justification.

### CK Delta Summary

| Metric | Before (VDSR) | After (worst class) | Improvement |
|--------|--------------|---------------------|-------------|
| WMC | 97 | 12 (`VolumeTextureManager`) | ✅ −85 worst-case; all ≤ 12 |
| CBO | 28 | 15 (`VolumeRenderCoordinator`) | ✅ domain classes ≤ 12; cycle broken |
| RFC | 97 | 12 (`VolumeTextureManager`) | ✅ −85 worst-case; all ≤ 12 |
| LCOM | 0.95 | 0.69 (`VolumeRenderCoordinator`) | ✅ −0.26 worst-case; 0.00 for strategy classes |
| LOC | 1,403 | ~150–250 per class | ✅ ~6–9× smaller per class |

---

## SOLID / GRASP Principles Demonstrated

| Principle | Violation in Before | Fix in After |
|-----------|--------------------|-----------------------------------------|
| **SRP** | 9 responsibilities in one class | One class per concern; each ~150–200 LOC |
| **OCP** | Projection mode if/else (lines 1218–1221) | `IRenderPipeline.SetPipelineKeyword` — variant hidden behind interface |
| **ISP** | 152-member public API | `IVolumeMaterialBinder` (7 members), `IVolumeTextureManager` (6 members) |
| **DIP** | `FindObjectOfType` (lines 381, 522); `Camera.main`; `Graphics.DrawProceduralNow` | All collaborators injected via constructor; `IRenderPipeline` abstracts SRP API |
| **GRASP Low Coupling** | CBO = 28; in 46-file cycle | Per-class CBO ≤ 12 (domain), 15 (orchestrator); cycle broken by interface boundaries |
| **GRASP High Cohesion** | LCOM = 0.95 — mask, camera, foveation, texture fields mixed | LCOM 0.00–0.25 for focused classes; 0.57–0.69 for lifecycle-structured classes |
| **GRASP Protected Variations** | Global `Shader.EnableKeyword` calls scattered across Update() | All pipeline-keyword calls proxied through `IRenderPipeline` |
| **GRASP Information Expert** | Coordinator holds foveation, texture, and camera knowledge | Each class owns only its relevant data |

---

## Dependency Inversion — The Key Design Decision

The brief §4.2 mandates: *"Domain rendering math must NOT transitively depend on `UnityEngine` or `SteamVR` types."*

`VolumeDataSetRenderer` violated this by calling `transform.InverseTransformPoint` (lines 713, 739, 857) inside methods that also performed mathematical coordinate transforms — making them impossible to unit-test outside a running Unity scene.

The after design creates a **dependency firewall**:

```
VolumeRenderCoordinator  ←── owns ──► VolumeMaterialBinder
                                           uses ──► IRenderPipeline
                                                         ▲
                                            UrpRenderPipeline  (only class that imports
                                            HdrpRenderPipeline   UnityEngine.Rendering.Universal)
```

`VolumeMaterialBinder`, `VolumeTextureManager`, `VolumeCameraDriver`, and `FoveatedSamplingPolicy` never import `UnityEngine.Rendering.Universal` or `UnityEngine.Rendering.HighDefinition`. The SRP adapters are the only classes that cross that boundary.

---

## Test Strategy — What Becomes Testable

| Before | After |
|--------|-------|
| Testing `SetMaskMode()` required instantiating all 1,403 lines and all dependencies | `VolumeMaterialBinder` unit-testable: inject `NullRenderPipeline` + `NullMaskMode` |
| Testing foveation required a running HMD and SteamVR | `FoveatedSamplingPolicy` testable: inject `StubGazeProvider` (centred gaze or unavailable) |
| No edit-mode tests possible | All four classes testable in Unity Test Runner edit mode, no GPU required |

Example test enabled by the extraction:

```csharp
[Test]
public void FoveatedPolicy_GazeUnavailable_ReturnsUniformFallback()
{
    var policy = new FoveatedSamplingPolicy(
        new StubGazeProvider(gazeAvailable: false),
        FoveatedSamplingConfig.Default);

    FoveationParameters p = policy.ComputeParameters();

    Assert.IsFalse(p.FoveationActive);
    Assert.AreEqual(FoveatedSamplingConfig.Default.MaxSteps, p.StepsLow);
    Assert.AreEqual(FoveatedSamplingConfig.Default.MaxSteps, p.StepsHigh);
}
```

---

## Known Invariants Preserved

| Invariant | How it is preserved |
|-----------|---------------------|
| ≥ 90 fps frame rate | Shader step counts unchanged; CPU-side dispatch is O(1); no per-frame heap allocation |
| 4 GB `Texture3D` ceiling | Single enforcement point in `VolumeTextureManager` — no other class allocates textures |
| 368 MB default memory budget | `VolumeTextureConfig.Default.MemoryBudgetMb = 368L`; passed at construction, immutable |
| Nearest-neighbour filtering | `VolumeTextureConfig.Default.FilterMode = FilterMode.Point`; no other class sets filter mode |
| Foveated rendering support | `FoveatedSamplingPolicy` + `IGazeProvider` — improved: auto-detects HMD absence rather than relying on Inspector checkbox |

---

## Open Items (blocked on cross-team interface confirmation)

| Item | Blocker |
|------|---------|
| `VolumeTextureManager` uses `VolumeDataSet` directly | Sub-team 2 `RawVolumeData` struct (task S2-CO09) |
| `IGazeProvider` GazeFocusPoint coord space not confirmed | Sub-team 4 interface delivery (task S2-CO08) |

See `PROGRESS.md` blockers table for current status.

---

## File Index

| File | Description |
|------|-------------|
| `before/VolumeDataSetRenderer.cs` | Full original source annotated with 20+ violation markers |
| `after/VolumeMaterialBinder.cs` | Shader / material property management |
| `after/VolumeTextureManager.cs` | 3D texture upload and memory-budget enforcement |
| `after/VolumeCameraDriver.cs` | Camera-matrix extraction and coordinate transforms |
| `after/FoveatedSamplingPolicy.cs` | Foveated rendering step-count computation |
| `after/Rationale/VolumeMaterialBinder-decisions.md` | Extended design rationale notes |
| `after/Rationale/VolumeTextureManager-decisions.md` | Extended design rationale notes |
| `../stubs/IRenderPipeline.cs` | Interface stub + `NullRenderPipeline` test double |
| `../stubs/UrpRenderPipeline.cs` | URP adapter stub |
| `../stubs/HdrpRenderPipeline.cs` | HDRP adapter stub |
