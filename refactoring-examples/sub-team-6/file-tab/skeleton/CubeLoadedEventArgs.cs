// brief §6.6 | File tab AFTER skeleton — CubeLoadedEventArgs DTO
// Used by IVolumeService.CubeLoaded once a cube has finished loading and is visible.
// Peer-tab ViewModels (Rendering, Stats, Sources, Paint) subscribe on construction and unsubscribe on Dispose
// No UnityEngine dependency.
namespace iDaVIE.Desktop.FileTab
{
    // Plain-data payload carried by IVolumeService.CubeLoaded once a cube is fully loaded and visible. Peer tabs read it to rebind their UIs to the new dataset without the File tab reaching into their scene-graph state.
    // init-only so it's immutable once raised — every subscriber sees the same snapshot.
    public sealed class CubeLoadedEventArgs : EventArgs
    {
        // Absolute path of the loaded image. Required: an event can't be raised without it.
        public required string ImagePath { get; init; }
        // Absolute path of the mask, or null when none was loaded.
        public string? MaskPath { get; init; }
        // Convenience flag for subscribers — true when a mask is present.
        public bool HasMask => !string.IsNullOrEmpty(MaskPath);

        // 1-based FITS HDU index that was loaded.
        public int HduIndex { get; init; } = 1;
    }
}
