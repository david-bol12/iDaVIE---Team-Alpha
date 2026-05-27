# Rendering Layer Design Document
**Sub-team:** Cache Me If You Can — Sub-team 3, Team Alpha  
**Work package:** Rendering Engine  
**Version:** 0.1 (Sprint 2 draft)  
**Status:** In progress

---

## 1. Overview

The rendering layer is responsible for all GPU-side volume visualisation: ray-marching the FITS data cube into a visible 3D image, applying colour maps and scaling, handling mask modes, and maintaining the 90 fps performance floor inside the VR headset.

The current implementation is a single monolithic class (`VolumeDataSetRenderer`) that mixes texture management, material binding, camera driving, and foveated sampling into one unit. This document describes the target refactored shape: four focused classes behind clean interfaces, with no transitive dependency on `UnityEngine` or `SteamVR` types in the domain core.

---

## 2. Current State (Baseline)

**Class:** `VolumeDataSetRenderer.cs`

| CK Metric | Baseline (Day 2) | Target (Day 13) | Threshold |
|---|---|---|---|
| WMC | TBD | ≤ 20 (domain) | ≤ 40 adapters |
| CBO | TBD | ≤ 14 (domain) | ≤ 25 orchestrators |
| RFC | TBD | ≤ 50 | ≤ 50 |
| LCOM | TBD | ≤ 0.5 | ≤ 0.5 |
| DIT | TBD | ≤ 4 | ≤ 4 |

*Baseline numbers to be filled from Understand/SonarQube run on Day 2.*

**Known problems:**
- Single class carries texture upload, material property setting, camera-driven LOD, foveated sampling policy, mask mode switching, and Unity lifecycle — violates SRP.
- Mask modes implemented as a switch/if-else block — violates OCP; adding a new mode requires editing the class.
- Direct dependency on `UnityEngine.Texture3D` and `SteamVR` camera types inside domain logic — untestable outside Unity.
- No interface boundaries — no test doubles possible without Unity running.

---

## 3. Target Class Structure

The monolith is split into four classes, each with one responsibility, behind interfaces. A thin `VolumeRenderingCoordinator` (Unity MonoBehaviour) wires them together and owns the Unity lifecycle.

```
VolumeRenderingCoordinator (MonoBehaviour — Unity adapter only)
    │
    ├── IVolumeMaterialBinder      → VolumeMaterialBinder
    ├── IVolumeTextureManager      → VolumeTextureManager
    ├── IVolumeCameraDriver        → VolumeCameraDriver
    └── IFoveatedSamplingPolicy    → FoveatedSamplingPolicy
```

Mask modes are extracted to a Strategy:

```
IMaskMode
    ├── MaskDisabledMode
    ├── MaskEnabledMode
    ├── MaskInvertedMode
    └── MaskIsolatedMode
```

---

## 4. Class Responsibilities

### 4.1 `VolumeMaterialBinder` — implements `IVolumeMaterialBinder`

**Responsibility:** Owns all material property state. Translates domain render settings (colour map, scaling type, thresholds, mask mode, projection mode, bias/contrast/alpha/gamma, slice bounds, rest frequency) into GPU shader properties.

**API surface:**

```csharp
public interface IVolumeMaterialBinder
{
    void ApplyColourMap(ColourMap map);
    void ApplyScaling(ScalingType type, float bias, float contrast, float gamma);
    void ApplyThresholds(float min, float max);
    void ApplyMaskMode(IMaskMode mode);
    void ApplyProjectionMode(ProjectionMode mode);
    void ApplySliceBounds(float min, float max);
    void ApplyRestFrequency(float frequency, bool overrideEnabled);
}
```

**Lifecycle:**
- `Init(Material targetMaterial)` — called once by the coordinator on scene load.
- `ApplyXxx()` methods — called on user parameter change; no per-frame cost unless a setting changed.
- `Dispose()` — releases reference to material; no unmanaged resources owned here.

**Thread safety:** All `ApplyXxx()` calls must be made from the Unity main thread (GPU resource access). No background thread calls permitted. If a setting change arrives off-thread (e.g. from a server response), the coordinator marshals it via a main-thread dispatcher.

---

### 4.2 `VolumeTextureManager` — implements `IVolumeTextureManager`

**Responsibility:** Owns the `Texture3D` lifecycle. Receives a voxel buffer from the Data I/O layer (Sub-team 2), uploads it to the GPU, and exposes a texture handle to the material binder. Also owns the 368 MB cube budget and the 4 GB Unity texture limit enforcement.

**API surface:**

```csharp
public interface IVolumeTextureManager
{
    void UploadCube(VoxelBuffer buffer, TextureFormat format);
    void UpdateSubregion(VoxelBuffer buffer, CubeBounds bounds);
    void Release();
    Texture3D ActiveTexture { get; }
    bool IsReady { get; }
}
```

**Lifecycle:**
- `UploadCube()` — allocates `Texture3D`, uploads data. Called when a FITS cube is loaded or reloaded on session restore.
- `UpdateSubregion()` — partial re-upload for subcube streaming; does not reallocate.
- `Release()` — destroys `Texture3D` and frees GPU memory. Called on scene teardown or when a new cube replaces the current one.
- `Dispose()` — calls `Release()`.

**Thread safety:** `UploadCube()` and `UpdateSubregion()` must run on the Unity main thread (Texture3D API requirement). Buffer preparation (format conversion, axis reordering from FITS NAXIS order to Unity XYZ) may be done on a background thread before the main-thread upload call. `VoxelBuffer` is immutable once handed to this class — Sub-team 2 must not modify it after passing it over.

**Memory ownership:** Sub-team 2 allocates the `VoxelBuffer`. `VolumeTextureManager` does not free it — Sub-team 2 owns and frees unmanaged memory after `UploadCube()` returns. This must be confirmed in the Sub-team 2 interface contract.

---

### 4.3 `VolumeCameraDriver` — implements `IVolumeCameraDriver`

**Responsibility:** Tracks the active VR camera transform and drives per-frame LOD selection (which mip level / downsampled resolution to sample). Receives camera state from the Interaction layer (Sub-team 4) via `IInputProvider`; does not depend on SteamVR types directly.

**API surface:**

```csharp
public interface IVolumeCameraDriver
{
    void UpdateCameraState(CameraState state);  // called by coordinator each frame
    float CurrentLodBias { get; }
    Vector3 ViewDirection { get; }
}
```

**Lifecycle:**
- Stateless between frames except for the current `CameraState`.
- No `Init()` required; coordinator passes state each frame.
- No `Dispose()` — no owned resources.

**Thread safety:** `UpdateCameraState()` and property reads are main-thread only. `CameraState` is a value type (struct) passed by copy — no shared mutable reference.

---

### 4.4 `FoveatedSamplingPolicy` — implements `IFoveatedSamplingPolicy`

**Responsibility:** Decides per-frame sampling resolution based on gaze direction (foveal vs peripheral regions). Consumes gaze data from the headset via an `IGazeProvider` abstraction — no direct SteamVR or OpenXR dependency.

**API surface:**

```csharp
public interface IFoveatedSamplingPolicy
{
    SamplingMap ComputeSamplingMap(GazeData gaze, RenderBudget budget);
    bool IsEnabled { get; set; }
}
```

**Lifecycle:**
- Stateless per call — `ComputeSamplingMap()` is a pure function given gaze + budget.
- `IsEnabled` is a global user preference, **not** per-session state (see Section 6 — State Contract).
- No `Dispose()`.

**Thread safety:** `ComputeSamplingMap()` is pure and thread-safe. `IsEnabled` reads/writes are main-thread only.

---

### 4.5 `IMaskMode` — Strategy for mask behaviour

**Responsibility:** Encapsulates the per-mode shader property differences. Adding a new mask mode means adding a new `IMaskMode` implementation, not editing existing code (Open–Closed Principle).

```csharp
public interface IMaskMode
{
    void Apply(IVolumeMaterialBinder binder);
}
```

Concrete implementations: `MaskDisabledMode`, `MaskEnabledMode`, `MaskInvertedMode`, `MaskIsolatedMode`.

---

### 4.6 `VolumeRenderingCoordinator` — MonoBehaviour (Unity adapter)

**Responsibility:** Composition root for the rendering layer inside Unity. Owns the Unity `Awake / Update / OnDestroy` lifecycle. Constructs and wires all four interfaces. Marshals main-thread calls. Has **no domain logic** — it only delegates.

**This is the only class in the rendering layer that may depend on `UnityEngine` types.**

---

## 5. Dependency Graph

```
[UnityEngine / SteamVR / URP]
        │
VolumeRenderingCoordinator   ← only Unity-dependent class
        │
        ├── IVolumeMaterialBinder
        ├── IVolumeTextureManager  ←── VoxelBuffer (from Sub-team 2)
        ├── IVolumeCameraDriver    ←── CameraState (from Sub-team 4)
        └── IFoveatedSamplingPolicy ←── GazeData (from IGazeProvider)

[Domain / no Unity dependency]
    ColourMap, ScalingType, MaskMode, ProjectionMode, RenderSettings
    IMaskMode + concrete strategies
```

Domain types (`RenderSettings`, `ColourMap` etc.) have zero dependency on `UnityEngine`. All Unity-specific types stop at the coordinator boundary.

---

## 6. State Contract (Persistence — Sub-team 7)

**Save these per session:**

| Field | Type | Notes |
|---|---|---|
| ColourMap | enum | e.g. Inferno, Plasma, Turbo |
| ScalingType | enum | Linear, Log, Sqrt, Square, Power, Gamma |
| ThresholdMin | float | relative to loaded cube data range |
| ThresholdMax | float | relative to loaded cube data range |
| MaskMode | enum | Disabled, Enabled, Inverted, Isolated |
| ProjectionMode | enum | MaximumIntensity, AverageIntensity |
| ScalingBias | float | |
| Contrast | float | |
| Alpha | float | |
| Gamma | float | |
| SliceMin | float | |
| SliceMax | float | |
| RestFrequency | float | |
| RestFrequencyOverride | bool | |

**Do NOT save:** GPU texture handles, frame buffers, transient per-frame sampling state — all rebuilt at runtime.

**Foveated rendering** (`IsEnabled`, parameters): treat as a **global user preference**, not session state. Persist separately from the workspace; restore regardless of which session is loaded.

**Restore ordering dependency:** ThresholdMin/Max and RestFrequency are relative to the data. Sub-team 2 (Babelaas) must reload the cube **before** Sub-team 7 applies our render settings bundle. Confirm restore ordering with Sub-team 7 and Sub-team 2.

**Restore call:** hand us one `RenderSettings` struct containing all fields above. We call `IVolumeMaterialBinder.ApplyXxx()` for each field once the texture is ready.

---

## 7. Unity 6 SRP Migration Notes

- The render pipeline abstraction (`IVolumeMaterialBinder`) is the containment boundary for the URP/HDRP migration. Domain code never references `UnityEngine.Rendering.Universal` types.
- Shader assets will be reorganised under `Assets/Rendering/Shaders/` with a clear naming policy (documented in the shader organisation policy deliverable).
- The `VolumeRenderingCoordinator` is the only class requiring changes when switching between URP and HDRP.

---

## 8. Testing Strategy

| Test type | What is tested | Framework | Unity required |
|---|---|---|---|
| Unit | `VolumeMaterialBinder`, `IMaskMode` strategies, `FoveatedSamplingPolicy.ComputeSamplingMap()` | NUnit / xUnit | No — pure C# |
| Unit | `VolumeCameraDriver` with mock `IInputProvider` | NUnit | No |
| Integration | `VolumeTextureManager` upload + release cycle | Unity Test Framework (Edit Mode) | Yes |
| Play mode | Full render frame, mask mode switching, colour map changes | Unity Test Framework (Play Mode) | Yes |
| Golden image | Regression across all mask modes and colour maps | Custom image-diff harness | Yes |

Coverage targets: ≥ 70% branch/line on domain classes; Unity-bound code tracked but not in strict target.

---

## 9. Open Questions

1. **Texture format agreement with Sub-team 2:** exact pixel format, axis order (FITS NAXIS vs Unity XYZ), and who does format conversion.
2. **Restore ordering:** confirm with Sub-team 7 that cube load always precedes render-settings apply.
3. **SRP target:** URP or HDRP? Pending Architecture Guild ADR.
4. **Gaze provider abstraction:** confirm `IGazeProvider` interface owned by Sub-team 4 or Sub-team 3.

---

*Last updated: Day 6, Sprint 2. Owner: Cache Me If You Can Tech Lead.*
