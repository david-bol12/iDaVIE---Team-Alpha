// brief §6.6 | File tab AFTER skeleton — IVolumeService (Sub-team 1 gateway boundary)
// This is the seam between Sub-team 6 (Desktop Client) and Sub-team 1 (Micro-kernel).
// In local mode the adapter calls VolumeCommandController directly (in-process).
// In future remote mode the adapter sends a JSON-RPC / gRPC message over a named pipe.
// Satisfies ADR-009 (transport contract) and ADR-002 (ACL).
// No UnityEngine dependency.
namespace iDaVIE.Desktop.FileTab
{
    // The gateway to the volume-data server kernel (Sub-team 1's scope) and the seam between our client and theirs. The VM depends only on this — never on VolumeCommandController, VolumeDataSetRenderer, or coroutines directly.
    // Replaces CanvassDesktop.LoadCubeCoroutine, which crammed scene management, file I/O, coroutine lifecycle, and UI updates into one method.
    public interface IVolumeService
    {
        // True when a cube is loaded and the renderer is active. Used by the VM's command guards.
        bool IsCubeLoaded { get; }

        // Loads a FITS cube into the active renderer. Progress is reported in [0, 1] via progress. The adapter handles coroutine lifecycle and scene hierarchy internally.
        Task LoadCubeAsync(LoadCubeRequest request, IProgress<float>? progress = null, CancellationToken ct = default);

        // Raised once a cube has finished loading and is visible. The subscription point for peer-tab VMs (Rendering, Stats, Sources, Paint) — replaces the ad-hoc cross-tab choreography in CanvassDesktop.postLoadFileFileSystem (scope §5.7).
        // Subscribers own their lifetime: subscribe in the peer-tab VM constructor, unsubscribe in its Dispose. The service holds only the delegate, never the renderer instance — that's what closes the rest-frequency subscription leak in scope §10 Anomaly #8.
        event EventHandler<CubeLoadedEventArgs>? CubeLoaded;
    }
}
