# Integration Contract — Sub-team 3 (Rendering) × Sub-team 7 (Persistence)
**Team Alpha | Cache Me If You Can**
*Agreed: 26 May 2026 | Status: Pending Team 7 sign-off | Integration target: Sprint 3*

---

## Summary

Sub-team 7 owns session persistence (save/load to disk). Sub-team 3 owns the rendering
state that needs to be saved. The boundary between the two teams is a single shared struct
`VolumeSessionState` and a single interface `ISessionPersistenceService`.

Neither team needs to know anything about the other's internals.

---

## The Contract

### Shared struct — `VolumeSessionState`
> **Ownership:** Shared assembly (to be agreed — must not live in either team's own assembly)

```csharp
public readonly struct VolumeSessionState
{
    public string          FitsFilePath  { get; init; }
    public RenderingState  Rendering     { get; init; }
    public VolumeDataState VolumeData    { get; init; }
    public SpatialState    Spatial       { get; init; }
    public FoveationState  Foveation     { get; init; }
    public MaskState       Mask          { get; init; }
}
```

No `UnityEngine` types anywhere in this struct or its nested structs.
All spatial values are plain floats. This keeps the struct serialisable as pure C#
with no Unity context required — important for Team 7's file I/O and for unit testing.

### Nested state structs — owned by Sub-team 3

Each struct maps to exactly one of our four classes. That class is responsible for
populating it and restoring from it.

```csharp
// VolumeMaterialBinder owns this
public readonly struct RenderingState
{
    public float          ThresholdMin     { get; init; }
    public float          ThresholdMax     { get; init; }
    public ColorMap       ActiveColorMap   { get; init; }
    public ScalingType    Scaling          { get; init; }
    public ProjectionMode Projection       { get; init; }
    public MaskMode       ActiveMaskMode   { get; init; }
    public float          VignetteStrength { get; init; }
}

// VolumeTextureManager owns this
public readonly struct VolumeDataState
{
    public string FitsFilePath { get; init; }
    public int    CropMinX     { get; init; }
    public int    CropMinY     { get; init; }
    public int    CropMinZ     { get; init; }
    public int    CropMaxX     { get; init; }
    public int    CropMaxY     { get; init; }
    public int    CropMaxZ     { get; init; }
    public bool   IsCropped    { get; init; }
}

// VolumeCameraDriver owns this — no UnityEngine types
public readonly struct SpatialState
{
    public float PositionX  { get; init; }
    public float PositionY  { get; init; }
    public float PositionZ  { get; init; }
    public float RotationX  { get; init; }  // Euler angles
    public float RotationY  { get; init; }
    public float RotationZ  { get; init; }
    public float Scale      { get; init; }
    public int   RegionMinX { get; init; }
    public int   RegionMinY { get; init; }
    public int   RegionMinZ { get; init; }
    public int   RegionMaxX { get; init; }
    public int   RegionMaxY { get; init; }
    public int   RegionMaxZ { get; init; }
    public bool  HasRegion  { get; init; }
}

// FoveatedSamplingPolicy owns this
public readonly struct FoveationState
{
    public bool  FoveationEnabled     { get; init; }
    public float FovealRadius         { get; init; }
    public float ParafovealRadius     { get; init; }
    public float FovealSampleRate     { get; init; }
    public float ParafovealSampleRate { get; init; }
    public float PeripheralSampleRate { get; init; }
}

// MaskPersistenceService owns this — straddles both teams
public readonly struct MaskState
{
    public string SavedMaskFilePath { get; init; }
    public bool   HasUnsavedChanges { get; init; }
    public double RestFrequencyGHz  { get; init; }
    public int    RestFrequencyIndex { get; init; }
}
```

### Service interface — owned by Sub-team 7

```csharp
public interface ISessionPersistenceService
{
    void               Save(VolumeSessionState state, string path);
    VolumeSessionState Load(string path);
}
```

Team 7 implements this. Sub-team 3 depends on the interface only — never on the
concrete implementation. Serialisation format (JSON, binary, etc.) is entirely
Team 7's decision.

---

## Save / Restore Flow

### Save

```
UI triggers save
    → coordinator.SaveSession(path)
        → state = VolumeSessionState {
              Rendering  = materialBinder.CaptureState()
              VolumeData = textureManager.CaptureState()
              Spatial    = cameraDriver.CaptureState()
              Foveation  = foveationPolicy.CaptureState()
              Mask       = maskService.CaptureState()
          }
        → sessionService.Save(state, path)   // Team 7 takes over here
```

### Restore

```
UI triggers load
    → coordinator.LoadSession(path)
        → state = sessionService.Load(path)  // Team 7 returns the struct
        → materialBinder.RestoreState(state.Rendering)
        → textureManager.RestoreState(state.VolumeData)
        → cameraDriver.RestoreState(state.Spatial)
        → foveationPolicy.RestoreState(state.Foveation)
        → maskService.RestoreState(state.Mask)
```

### Important: RestoreState is not just field-setting

Each class must leave itself in a valid state after restore. For example:

- `VolumeMaterialBinder.RestoreState()` must call `pushToShader()` to sync the material
- `VolumeTextureManager.RestoreState()` may need to trigger a texture re-upload
- `VolumeCameraDriver.RestoreState()` must update the Unity transform

Team 7 does not need to know about any of this — it happens entirely on our side.

---

## What Exists in VolumeDataSetRenderer Today

For Team 7's awareness, these are the methods in the current monolith that touch
persistence. All of them will be extracted during refactoring:

| Method | Lines | What it does | Target after refactor |
|--------|-------|-------------|----------------------|
| `SaveMask()` | 1290–1378 | Writes mask buffer to FITS via native plugin (`idavie_native.dll`) | `MaskPersistenceService.Save()` |
| `SaveSubCube()` | 1261 | Exports cropped region to FITS via native plugin | `SubCubePersistenceService.Save()` |
| `GetMaskSavedFilePath()` | 1379 | Returns last saved mask path | Property on `MaskPersistenceService` |
| `LoadRegionData()` | 975 | Loads a subcube region from native plugin | `VolumeDataLoader` |

The FITS save methods call the native plugin via raw `IntPtr` handles. The agreed
approach is that Team 7's persistence service wraps this behind `ISessionPersistenceService`
so neither team reaches into the native plugin directly without a boundary.

---

## Open Questions (needs Team 7 sign-off)

| # | Question | Owner |
|---|---------|-------|
| 1 | Which shared assembly will own `VolumeSessionState` and the nested structs? | Both teams agree |
| 2 | Is `MaskState.RestFrequencyGHz` in the right place, or should WCS have its own section? | Both teams agree |
| 3 | Does Team 7 want a `FitsFilePath` at the top level of `VolumeSessionState`, or only inside `VolumeDataState`? | Team 7 preference |
| 4 | Will Team 7 wrap the native plugin FITS calls, or do we provide a `IFitsWriter` abstraction? | Both teams agree |

---

## Integration Timeline

- **Sprint 2 (now):** Contract designed and documented. No code coupling yet.
- **Sprint 3 (1–3 June):** Wire coordinator to `ISessionPersistenceService`. Implement `CaptureState` / `RestoreState` on each class. Integration test against Team 7's stub.
- **Artefact freeze:** Thu 4 June 11:00
