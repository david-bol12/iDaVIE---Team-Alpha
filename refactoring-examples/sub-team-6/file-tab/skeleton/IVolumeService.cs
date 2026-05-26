// WE1-3 | File tab AFTER skeleton — IVolumeService (Sub-team 1 gateway boundary)
// This is the seam between Sub-team 6 (Desktop Client) and Sub-team 1 (Micro-kernel).
// In local mode the adapter calls VolumeCommandController directly (in-process).
// In future remote mode the adapter sends a JSON-RPC / gRPC message over a named pipe.
// Satisfies ADR-0002 (transport contract) and ADR-0003 (ACL).
// No UnityEngine dependency.
namespace iDaVIE.Desktop.FileTab
{
    /// <summary>
    /// Gateway interface to the volume-data server kernel (Sub-team 1 scope).
    /// FileTabViewModel depends only on this interface — it never touches
    /// VolumeCommandController, VolumeDataSetRenderer, or coroutines directly.
    /// Replaces CanvassDesktop.LoadCubeCoroutine (which mixes scene management,
    /// file I/O, coroutine lifecycle, and UI updates in a single method).
    /// </summary>
    public interface IVolumeService
    {
        /// <summary>
        /// Returns true when a cube is currently loaded and the renderer is active.
        /// Used by FileTabViewModel command guards.
        /// </summary>
        bool IsCubeLoaded { get; }

        /// <summary>
        /// Loads a FITS cube into the active renderer.
        /// Progress is reported in the range [0, 1] via <paramref name="progress"/>.
        /// The adapter manages coroutine lifecycle and scene hierarchy internally.
        /// </summary>
        Task LoadCubeAsync(
            LoadCubeRequest request,
            IProgress<float>? progress = null,
            CancellationToken ct = default);

        /// <summary>
        /// Raised after a cube has finished loading and is visible to the user.
        /// Subscription point for peer-tab ViewModels (Rendering, Stats, Sources,
        /// Paint) — replaces the ad-hoc cross-tab choreography in
        /// CanvassDesktop.postLoadFileFileSystem (scope §5.7).
        ///
        /// Subscribers are responsible for their own lifetime: subscribe in the
        /// peer-tab VM constructor, unsubscribe in its Dispose. The service holds
        /// only the delegate reference, never the renderer instance — this is what
        /// closes the rest-frequency subscription leak in scope §10 Anomaly #8.
        /// </summary>
        event EventHandler<CubeLoadedEventArgs>? CubeLoaded;
    }
}
