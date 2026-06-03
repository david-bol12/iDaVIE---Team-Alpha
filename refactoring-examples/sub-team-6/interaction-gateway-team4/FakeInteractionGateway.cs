// Sub-team 6 — FakeInteractionGateway (in-process IInteractionGateway for tier-1 tests).
//
// Lets desktop ViewModel tests assert on which shared commands were issued, and
// drive the VM from synthetic VR-originated notifications — without a real
// transport or the VR Interaction System present. Satisfies the Section 4.2 #4
// non-negotiable: the IInteractionGateway boundary has at least one test double.
//
// Mirrors FakeGateway's recording style: every command call is appended to a
// read-only log; each notification has a Raise* helper that fires the event
// synchronously on the calling thread.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace iDaVIE.Client.Interaction
{
    // In-process IInteractionGateway for unit tests. Records issued
    // commands in Sent; raises notifications via the Raise* helpers.
    public sealed class FakeInteractionGateway : IInteractionGateway
    {
        private readonly List<SentCommand> _sent = new();

        // Ordered log of every command issued through this gateway.
        public IReadOnlyList<SentCommand> Sent => _sent;

        public event Action<ThresholdChangedArgs>? ThresholdChanged;
        public event Action<ColorMapChangedArgs>? ColorMapChanged;
        public event Action<ScalingChangedArgs>? ScalingChanged;
        public event Action<SourceListChangedArgs>? SourceListChanged;
        public event Action<MaskApplyModeChangedArgs>? MaskApplyModeChanged;

        public Task SetThresholdAsync(string datasetId, float min, float max, CancellationToken ct = default)
            => Record("render.setThreshold", new { datasetId, min, max });

        public Task SetColorMapAsync(string datasetId, string colorMap, CancellationToken ct = default)
            => Record("render.setColorMap", new { datasetId, colorMap });

        public Task SetScalingFunctionAsync(string datasetId, ScalingFunction scaling, CancellationToken ct = default)
            => Record("render.setScalingFunction", new { datasetId, scaling });

        public Task FlagSourceAsync(int sourceId, string flag, CancellationToken ct = default)
            => Record("sources.flag", new { sourceId, flag });

        public Task AddToNewListAsync(int sourceId, CancellationToken ct = default)
            => Record("sources.addToNewList", new { sourceId });

        public Task CropCubeAsync(string datasetId, SubsetBounds bounds, CancellationToken ct = default)
            => Record("cube.crop", new { datasetId, bounds });

        public Task SetMaskApplyModeAsync(MaskApplyMode mode, CancellationToken ct = default)
            => Record("mask.setApplyMode", new { mode });

        public Task ExitAsync(bool confirm, CancellationToken ct = default)
            => Record("app.exit", new { confirm });

        // Send Notifications on changes

        public void RaiseThresholdChanged(string datasetId, float min, float max)
            => ThresholdChanged?.Invoke(new ThresholdChangedArgs(datasetId, min, max));

        public void RaiseColorMapChanged(string datasetId, string colorMap)
            => ColorMapChanged?.Invoke(new ColorMapChangedArgs(datasetId, colorMap));

        public void RaiseScalingChanged(string datasetId, ScalingFunction scaling)
            => ScalingChanged?.Invoke(new ScalingChangedArgs(datasetId, scaling));

        public void RaiseSourceListChanged(int[] changed, SourceListChangeReason reason)
            => SourceListChanged?.Invoke(new SourceListChangedArgs(changed, reason));

        public void RaiseMaskApplyModeChanged(MaskApplyMode mode)
            => MaskApplyModeChanged?.Invoke(new MaskApplyModeChangedArgs(mode));

        private Task Record(string verb, object args)
        {
            _sent.Add(new SentCommand(verb, args));
            return Task.CompletedTask;
        }

        //One recorded command: its wire verb and the args object passed.
        public sealed record SentCommand(string Verb, object Args);
    }
}
