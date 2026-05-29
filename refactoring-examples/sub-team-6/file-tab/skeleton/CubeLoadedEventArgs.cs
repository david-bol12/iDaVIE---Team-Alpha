// WE1-3 | File tab AFTER skeleton — CubeLoadedEventArgs DTO
// Raised by IVolumeService.CubeLoaded once a cube has finished loading and is
// visible. Peer-tab ViewModels (Rendering, Stats, Sources, Paint) subscribe on
// construction and unsubscribe on Dispose — replaces the ad-hoc cross-tab
// choreography in CanvassDesktop.postLoadFileFileSystem (lines 935-987) and
// closes the rest-frequency subscription leak documented in scope §10 Anomaly #8.
// No UnityEngine dependency.
namespace iDaVIE.Desktop.FileTab
{
    /// <summary>
    /// Plain-data notification raised once a cube is fully loaded and visible.
    /// Carried by <see cref="IVolumeService.CubeLoaded"/>; peer tabs use it to
    /// rebind their UIs to the new dataset without the File tab having to reach
    /// into their scene-graph state.
    /// </summary>
    public sealed class CubeLoadedEventArgs : EventArgs
    {
        public required string ImagePath { get; init; }
        public string? MaskPath { get; init; }
        public bool HasMask => !string.IsNullOrEmpty(MaskPath);

        /// <summary>1-based FITS HDU index that was loaded.</summary>
        public int HduIndex { get; init; } = 1;
    }
}
