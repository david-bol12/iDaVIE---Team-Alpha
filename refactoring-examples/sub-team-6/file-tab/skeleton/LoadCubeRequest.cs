// brief §6.6 | File tab AFTER skeleton — LoadCubeRequest DTO
// Replaces the direct field-writes into VolumeDataSetRenderer in
// CanvassDesktop.LoadCubeCoroutine. No UnityEngine dependency.
namespace iDaVIE.Desktop.FileTab
{
    // The aspect-ratio choice from the Ratio_Dropdown on the file-load modal — the two options the original UI exposed (scope §1, §10 Anomaly #5).
    public enum RatioMode
    {
        // "X=Y=Z" — render with isotropic voxels (zScale = 1).
        Isotropic = 0,
        // "X=Y" — keep XY equal, scale Z proportional to its real axis size.
        ProportionalZ = 1,
    }

    // Immutable bundle of everything needed to load a cube, built by FileTabViewModel.LoadAsync() and handed across the ACL boundary to the VolumeServiceAdapter via IVolumeService.
    // The VM packages all the decisions here so the adapter just executes — it never re-reads the FITS metadata or inspects UI state.
    public sealed class LoadCubeRequest
    {
        // Absolute path of the image cube to load.
        public required string ImagePath { get; init; }
        // Absolute path of an optional mask, or null for none.
        public string? MaskPath { get; init; }

        // 1-based HDU index (FITS convention). Default 1 = primary HDU.
        public int HduIndex { get; init; } = 1;

        // Null means load the full cube; non-null activates subset (crop) loading.
        public SubsetBounds? Subset { get; init; }

        // Zero-based index into the Z-axis dropdown for 4+ axis cubes — which axis the user picked as the spectral/depth axis.
        public int ZAxisSelection { get; init; }

        // Pre-computed Z-axis scale factor applied to the cube's transform. 1.0f = isotropic. The VM computes it from RatioMode + axis sizes (replacing the inline zScale at CanvassDesktop.cs:1028-1039) so the adapter doesn't touch FITS metadata again.
        public float ZScale { get; init; } = 1f;
    }
}
