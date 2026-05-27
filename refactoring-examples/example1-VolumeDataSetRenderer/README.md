# Example 1 — VolumeDataSetRenderer Split

This worked example demonstrates the refactoring of the monolithic `VolumeDataSetRenderer`
(~1400 lines) into four focused classes:

- `VolumeMaterialBinder`
- `VolumeTextureManager`
- `VolumeCameraDriver`
- `FoveatedSamplingPolicy`

## Before / After

See the `before/` and `after/` subdirectories for annotated code excerpts and CK metric comparisons.
