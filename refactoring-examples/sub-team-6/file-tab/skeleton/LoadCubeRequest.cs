// WE1-3 | File tab AFTER skeleton — LoadCubeRequest DTO
// Replaces the direct field-writes into VolumeDataSetRenderer in
// CanvassDesktop.LoadCubeCoroutine. No UnityEngine dependency.
namespace iDaVIE.Desktop.FileTab
{
    /// <summary>
    /// User-selected aspect-ratio mode from the Ratio_Dropdown control on the file-load modal.
    /// Maps to the two options exposed in the original UI (scope §1, §10 Anomaly #5).
    /// </summary>
    public enum RatioMode
    {
        /// <summary>"X=Y=Z" — render with isotropic voxels (zScale = 1).</summary>
        Isotropic = 0,
        /// <summary>"X=Y" — keep XY equal, scale Z proportional to its real axis size.</summary>
        ProportionalZ = 1,
    }

    /// <summary>
    /// Immutable request object for loading a FITS cube via <see cref="IVolumeService"/>.
    /// Created by FileTabViewModel.LoadAsync() and passed across the ACL boundary
    /// to the VolumeServiceAdapter.
    /// </summary>
    public sealed class LoadCubeRequest
    {
        public required string ImagePath { get; init; }
        public string? MaskPath { get; init; }

        /// <summary>1-based HDU index (FITS convention). Default 1 = primary HDU.</summary>
        public int HduIndex { get; init; } = 1;

        /// <summary>Null means load the full cube; non-null activates subset loading.</summary>
        public SubsetBounds? Subset { get; init; }

        /// <summary>
        /// Zero-based index into the Z-axis dropdown for 4+ axis cubes.
        /// Maps to the axis the user selected as the spectral/depth axis.
        /// </summary>
        public int ZAxisSelection { get; init; }

        /// <summary>
        /// Pre-computed Z-axis scale factor applied to the cube's transform.
        /// Replaces the inline zScale computation at CanvassDesktop.cs:1028-1039.
        /// 1.0f == isotropic; computed by the VM from <see cref="RatioMode"/> + axis sizes
        /// so the adapter does not need to inspect the FITS metadata again.
        /// </summary>
        public float ZScale { get; init; } = 1f;
    }
}
