// WE1-3 | File tab AFTER skeleton — LoadCubeRequest DTO
// Replaces the direct field-writes into VolumeDataSetRenderer in
// CanvassDesktop.LoadCubeCoroutine. No UnityEngine dependency.
namespace iDaVIE.Desktop.FileTab
{
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
    }
}
