// brief §6.6 | File tab AFTER — FitsServiceAdapter (client-side gateway proxy)

// Adapts IServiceGateway to IFitsService. Per the brief §6.6 ("direct file I/O that belongs server-side"), FITS reading happens in a server-side plug-in;
// This client-side adapter only translates IFitsService method calls into the JSON-RPC method catalogue defined in Gateway Contract v1 §"Method catalogue (v1)":

//   IFitsService.OpenImageAsync(path)              →  file.open       + dataset.getAxes
//   IFitsService.OpenMaskAsync(path)               →  file.open       + dataset.getAxes
//   IFitsService.GetHeaderTextAsync(handle, hdu)   →  dataset.getHeader
//   IFitsHandle.Dispose()                          →  file.close (best-effort)

// No UnityEngine, no [DllImport], no IntPtr. The dataset id is server-assigned and opaque to the ViewModel — RemoteFitsHandle carries it round-trip.

// Satisfies ADR-009 Decision §1 ("ViewModel commands → server calls via the transport contract"), ADR-002 (ACL — Unity-side coupling eliminated), and brief §6.6 ("from direct native-plugin call → ViewModel command via service gateway").

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using iDaVIE.Client.Gateway;
using iDaVIE.Desktop.FileTab;

namespace iDaVIE.Desktop.Adapters.FileTab
{
    // Client-side IFitsService that forwards every call through IServiceGateway — no [DllImport]/IntPtr ever exists client-side; FITS reading lives in a server-side plug-in. The ViewModel sees only the interface; whether the gateway is the real named-pipe transport (JsonRpcPipeGateway) or an in-memory FakeGateway is a composition-root decision (which is what makes the VM testable).
    public sealed class FitsServiceAdapter : IFitsService
    {
        // Gateway Contract v1 method names — single source of truth so a rename here matches
        // both the spec text and the unit tests in step 4.
        internal const string MethodFileOpen        = "file.open";
        internal const string MethodFileClose       = "file.close";
        internal const string MethodDatasetGetAxes  = "dataset.getAxes";
        internal const string MethodDatasetGetHeader = "dataset.getHeader";

        private readonly IServiceGateway _gateway;

        public FitsServiceAdapter(IServiceGateway gateway)
        {
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        }

        public Task<FitsFileInfo> OpenImageAsync(string path, CancellationToken ct = default)
            => OpenAsync(path, isMask: false, ct);

        public Task<FitsFileInfo> OpenMaskAsync(string path, CancellationToken ct = default)
            => OpenAsync(path, isMask: true, ct);

        private async Task<FitsFileInfo> OpenAsync(string path, bool isMask, CancellationToken ct)
        {
            // 1. file.open → datasetId (Gateway Contract v1 method catalogue).
            var openResult = await _gateway
                .SendAsync<FileOpenResult>(MethodFileOpen, new FileOpenParams(path, isMask), ct)
                .ConfigureAwait(false);

            // 2. dataset.getAxes → HDU list + axis metadata + primary-HDU header.
            //    Two calls instead of one because the catalogue separates "obtain a handle" from "read structural metadata"
            // This leaves room for a later "open without parsing" optimisation.
            try
            {
                var meta = await _gateway
                    .SendAsync<DatasetAxesResult>(MethodDatasetGetAxes, new DatasetIdParams(openResult.DatasetId), ct)
                    .ConfigureAwait(false);

                return new FitsFileInfo
                {
                    Handle         = new RemoteFitsHandle(_gateway, openResult.DatasetId, path),
                    FilePath       = path,
                    HduList        = meta.HduList,
                    NAxis          = meta.NAxis,
                    AxisSizes      = meta.AxisSizes,
                    HeaderText     = meta.HeaderText,
                    EstimatedBytes = meta.EstimatedBytes,
                };
            }
            catch
            {
                // dataset.getAxes failed: the server allocated a dataset id we
                // will never use. Fire a best-effort file.close so we do not
                // leak it server-side.
                BestEffortClose(openResult.DatasetId);
                throw;
            }
        }

        public Task<string> GetHeaderTextAsync(IFitsHandle handle, int hduIndex, CancellationToken ct = default)
        {
            if (handle is not RemoteFitsHandle rh)
                throw new ArgumentException("Handle was not produced by this adapter.", nameof(handle));
            if (rh.IsClosed)
                throw new ObjectDisposedException(nameof(IFitsHandle));

            return _gateway.SendAsync<string>(
                MethodDatasetGetHeader,
                new GetHeaderParams(rh.DatasetId, hduIndex),
                ct);
        }

        private void BestEffortClose(string datasetId)
        {
            // IDisposable.Dispose is synchronous and IFitsHandle.Dispose is
            // called from that path; we cannot await without deadlocking on
            // some sync contexts, so fire-and-forget and observe the task's
            // exception to avoid an unobserved-exception fault.
            _ = _gateway
                .SendAsync<object?>(MethodFileClose, new DatasetIdParams(datasetId))
                .ContinueWith(t => { _ = t.Exception; }, TaskScheduler.Default);
        }

        // Wire DTOs
        // Private to the adapter — they exist only to shape the JSON on the wire and never escape. System.Text.Json with the gateway's camelCase policy produces the field names documented in Gateway Contract v1 §"Message shape" (e.g. params: { "path": "...", "isMask": false }).

        private sealed record FileOpenParams(string Path, bool IsMask);

        private sealed record DatasetIdParams(string DatasetId);

        private sealed record GetHeaderParams(string DatasetId, int HduIndex);

        private sealed record FileOpenResult(string DatasetId);

        private sealed record DatasetAxesResult(
            IReadOnlyList<HduInfo> HduList,
            int NAxis,
            IReadOnlyDictionary<int, long> AxisSizes,
            string HeaderText,
            long EstimatedBytes);

        // Handle implementation
        // Wraps a server-assigned dataset id; the ViewModel sees only IFitsHandle. Disposal issues a best-effort file.close — see BestEffortClose above.

        private sealed class RemoteFitsHandle : IFitsHandle
        {
            private readonly IServiceGateway _gateway;
            private string? _datasetId;

            public RemoteFitsHandle(IServiceGateway gateway, string datasetId, string filePath)
            {
                _gateway   = gateway;
                _datasetId = datasetId;
                FilePath   = filePath;
            }

            public string FilePath { get; }

            internal string DatasetId
                => _datasetId ?? throw new ObjectDisposedException(nameof(IFitsHandle));

            internal bool IsClosed => _datasetId is null;

            public void Dispose()
            {
                var id = _datasetId;
                if (id is null) return;
                _datasetId = null;

                _ = _gateway
                    .SendAsync<object?>(MethodFileClose, new DatasetIdParams(id))
                    .ContinueWith(t => { _ = t.Exception; }, TaskScheduler.Default);
            }
        }
    }
}
