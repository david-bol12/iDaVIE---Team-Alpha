// Sub-team 6 — IInteractionGateway (R04 / DEPS-2 · ADR-010 Sub-team 4 · ADR-009 §C3).
//
// The contract boundary between the Desktop GUI (Sub-team 6) and the VR
// Interaction System (Sub-team 4). The desktop ViewModel layer depends on THIS
// interface to issue the handful of commands that are shared with VR, and to
// observe shared-state changes that VR originates.
//
// Both surfaces (desktop controls + VR voice/quick-menu) must drive ONE canonical
// command per user intent — see CurrentGUIStateDoc.md §9/§11 for the shared-state
// touchpoints this replaces. The real implementation is an adapter over
// IServiceGateway: commands are JSON-RPC requests (C->S) and notifications are
// JSON-RPC notifications (S->C) on the ADR-0002 transport. The wire verb for each
// member is named in its doc comment so the adapter and the server agree.
//
// Pure C#. No UnityEngine reference, and no dependency on the service-gateway
// assembly — this contract stands alone in iDaVIE.Client.Interaction. The real
// implementation is an adapter that bridges to IServiceGateway (../contracts).
// Test double: FakeInteractionGateway.
//
// STATUS: STRAWMAN — proposed by Sub-team 6 (2026-05-29), awaiting Sub-team 4
// ratification. The following are OPEN pending Sub-team 4 sign-off and do NOT
// block the desktop side coding against this interface:
//   O1. Verb names / arg field names below — Sub-team 4 may rename.
//   O2. Does Sub-team 4's ADR-010 ICommand wrap a separate wire DTO, or is the
//       reified command itself the payload? (Affects nothing on our side; we
//       depend on this interface, not their ICommand type.)
//   O3. Will Sub-team 4 emit the five notifications below on VR-originated
//       changes? If not, defect B-03 (stale desktop sliders) stays unfixable.
//   O4. Ownership: mask painting + mask save are assumed VR-only (no member here).
//   O5. Units: colour map as name string (assumed) vs index; threshold as raw
//       float (assumed) vs normalised 0-1. (B-05 shows units already drift.)

using System;
using System.Threading;
using System.Threading.Tasks;

namespace iDaVIE.Client.Interaction
{
    /// <summary>
    /// Shared command + notification contract between the Desktop GUI and the VR
    /// Interaction System. Consumed by desktop ViewModels; fulfilled on the server
    /// side by Sub-team 1 (transport) + Sub-team 4 (interaction semantics).
    /// Commands flow Desktop -> server -> VR; notifications flow VR -> server -> Desktop.
    /// </summary>
    public interface IInteractionGateway
    {
        // ---- Commands (Client -> Server). Issued by either surface. ----

        /// <summary>Set the rendering intensity window. Wire verb: <c>render.setThreshold</c>.</summary>
        Task SetThresholdAsync(string datasetId, float min, float max, CancellationToken ct = default);

        /// <summary>Set the active colour map by name (e.g. "Viridis"). Wire verb: <c>render.setColorMap</c>. (OPEN O5: name vs index.)</summary>
        Task SetColorMapAsync(string datasetId, string colorMap, CancellationToken ct = default);

        /// <summary>Set the intensity scaling function. Wire verb: <c>render.setScalingFunction</c>.</summary>
        Task SetScalingFunctionAsync(string datasetId, ScalingFunction scaling, CancellationToken ct = default);

        /// <summary>Flag a source in the shared source list. Wire verb: <c>sources.flag</c>.</summary>
        Task FlagSourceAsync(int sourceId, string flag, CancellationToken ct = default);

        /// <summary>Add a source to the New List. Wire verb: <c>sources.addToNewList</c>.</summary>
        Task AddToNewListAsync(int sourceId, CancellationToken ct = default);

        /// <summary>Crop the cube to a voxel subregion. Wire verb: <c>cube.crop</c>.</summary>
        Task CropCubeAsync(string datasetId, SubsetBounds bounds, CancellationToken ct = default);

        /// <summary>Set how a mask is applied. Wire verb: <c>mask.setApplyMode</c>.</summary>
        Task SetMaskApplyModeAsync(MaskApplyMode mode, CancellationToken ct = default);

        /// <summary>Request application exit. Wire verb: <c>app.exit</c>. <paramref name="confirm"/> false asks the server to prompt first (ties to defect B-01).</summary>
        Task ExitAsync(bool confirm, CancellationToken ct = default);

        // ---- Notifications (Server -> Client). Raised on VR-originated changes. ----

        /// <summary>VR changed the threshold. Wire verb: <c>render.thresholdChanged</c>. Structural fix for B-03.</summary>
        event Action<ThresholdChangedArgs>? ThresholdChanged;

        /// <summary>VR changed the colour map. Wire verb: <c>render.colorMapChanged</c>.</summary>
        event Action<ColorMapChangedArgs>? ColorMapChanged;

        /// <summary>VR changed the scaling function. Wire verb: <c>render.scalingChanged</c>.</summary>
        event Action<ScalingChangedArgs>? ScalingChanged;

        /// <summary>VR changed the source list (flag / New List / load). Wire verb: <c>sources.listChanged</c>.</summary>
        event Action<SourceListChangedArgs>? SourceListChanged;

        /// <summary>VR changed the mask apply mode. Wire verb: <c>mask.applyModeChanged</c>.</summary>
        event Action<MaskApplyModeChangedArgs>? MaskApplyModeChanged;
    }

    /// <summary>Intensity scaling function. Mirrors the VR Settings Window options (CurrentGUIStateDoc.md §9.2).</summary>
    public enum ScalingFunction { Linear, Log, Sqrt, Square, Power, Gamma }

    /// <summary>How a painted/loaded mask is applied. OPEN O1: Sub-team 4 to confirm members.</summary>
    public enum MaskApplyMode { Set, Add, Erase }

    /// <summary>Why a source list changed — lets the SOURCES tab scope its refresh.</summary>
    public enum SourceListChangeReason { Flagged, AddedToNewList, Loaded, Removed }

    /// <summary>Inclusive 1-indexed voxel bounds for <see cref="IInteractionGateway.CropCubeAsync"/>. Mirrors file-tab/skeleton/SubsetBounds.cs. (OPEN O5: confirm indexing — B-05 risk.)</summary>
    public sealed record SubsetBounds(int XMin, int XMax, int YMin, int YMax, int ZMin, int ZMax);

    // ---- Notification payloads (Server -> Client). Field names = wire field names. ----

    public sealed record ThresholdChangedArgs(string DatasetId, float Min, float Max);
    public sealed record ColorMapChangedArgs(string DatasetId, string ColorMap);
    public sealed record ScalingChangedArgs(string DatasetId, ScalingFunction Scaling);
    public sealed record SourceListChangedArgs(int[] Changed, SourceListChangeReason Reason);
    public sealed record MaskApplyModeChangedArgs(MaskApplyMode Mode);
}
