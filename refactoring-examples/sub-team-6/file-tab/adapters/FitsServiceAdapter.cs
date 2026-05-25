// WE1-3 | File tab AFTER — FitsServiceAdapter (Unity-assembly adapter)
// Wraps FitsReader P/Invoke calls. Lives in the Unity assembly; the ViewModel
// never references this class. Satisfies ADR-0003 (ACL) and ADR-0001 (DIP).
// NOTE: requires UnityEngine and FitsReader — does not compile in the standalone
// skeleton project. The skeleton's IFitsService is the boundary; this is the plug.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using iDaVIE.Desktop.FileTab;
using UnityEngine;

namespace iDaVIE.Desktop.Adapters.FileTab
{
    /// <summary>
    /// Concrete adapter for <see cref="IFitsService"/>.
    /// All P/Invoke calls are confined here — no IntPtr escapes this class.
    /// The ViewModel receives only plain <see cref="FitsFileInfo"/> DTOs that
    /// carry an opaque <see cref="IFitsHandle"/> for subsequent HDU reads.
    ///
    /// Replaces the scattered FitsReader calls in CanvassDesktop._browseImageFile
    /// (lines 349–407), UpdateHeaderFromFits (lines 539–568), and
    /// ChangeHduSelection (lines 1435–1465) — the last of which reopened the file
    /// from disk on every dropdown selection.
    /// </summary>
    public sealed class FitsServiceAdapter : IFitsService
    {
        public Task<FitsFileInfo> OpenImageAsync(string path, CancellationToken ct = default)
            => Task.Run(() => OpenAndReadMetadata(path), ct);

        public Task<FitsFileInfo> OpenMaskAsync(string path, CancellationToken ct = default)
            => Task.Run(() => OpenAndReadMetadata(path), ct);

        public Task<string> GetHeaderTextAsync(IFitsHandle handle, int hduIndex, CancellationToken ct = default)
            => Task.Run(() => ReadHeaderText(handle, hduIndex), ct);

        // ── Core P/Invoke logic (thread-pool, away from Unity main thread) ────

        private static FitsFileInfo OpenAndReadMetadata(string path)
        {
            int status = 0;
            if (FitsReader.FitsOpenFile(out IntPtr fptr, path, out status, true) != 0)
                throw new InvalidOperationException($"FITS open failed (status {status}): {path}");

            // From here on we own fptr. On any failure path we must close it
            // before rethrowing — only the success path passes ownership to the
            // FitsHandle returned inside FitsFileInfo.
            try
            {
                // 1. Enumerate HDUs for the dropdown
                FitsReader.FitsGetHduCount(fptr, out int hduNum, out status);
                var hduList = new List<HduInfo>(hduNum);
                var sb      = new StringBuilder(80);

                for (int i = 0; i < hduNum; i++)
                {
                    FitsReader.FitsMovabsHdu(fptr, i + 1, out _, out status);
                    sb.Clear();

                    // Try EXTNAME, fall back to HDUNAME, then synthesise a name
                    if (FitsReader.FitsReadKey(fptr, (int)FitsReader.DataType.TSTRING,
                            "EXTNAME", sb, IntPtr.Zero, out status) != 0)
                    {
                        status = 0;
                        if (FitsReader.FitsReadKey(fptr, (int)FitsReader.DataType.TSTRING,
                                "HDUNAME", sb, IntPtr.Zero, out status) != 0)
                        {
                            status = 0;
                            sb.Append($"HDU {i + 1}");
                        }
                    }
                    hduList.Add(new HduInfo(i + 1, sb.ToString(), "IMAGE"));
                }

                // 2. Read NAXIS keywords from HDU 1 (same as CanvassDesktop.UpdateHeaderFromFits)
                FitsReader.FitsMovabsHdu(fptr, 1, out _, out status);
                var headers = FitsReader.ExtractHeaders(fptr, out status);

                int  nAxis     = 0;
                var  axisSizes = new Dictionary<int, long>();
                var  headerSb  = new StringBuilder();

                foreach (var kvp in headers)
                {
                    headerSb.Append(kvp.Key).Append("\t\t ").Append(kvp.Value).Append('\n');

                    if (kvp.Key.Length < 5 || kvp.Key.Substring(0, 5) != "NAXIS")
                        continue;

                    string suffix = kvp.Key.Substring(5);
                    if (suffix == "")
                        nAxis = (int)Convert.ToDouble(kvp.Value, CultureInfo.InvariantCulture);
                    else if (int.TryParse(suffix, out int axisNum))
                        axisSizes[axisNum] = (long)Convert.ToDouble(kvp.Value, CultureInfo.InvariantCulture);
                }

                // Success: transfer ownership of fptr into the handle.
                var handle = new FitsHandle(fptr, path);

                // Cube footprint estimate — product of NAXIS sizes × sizeof(float).
                // Replaces the FileInfo.Length + nelem*sizeof(float|short) computation
                // in CanvassDesktop.CheckMemSpaceForCubes (lines 995-1013).
                long estimatedBytes = sizeof(float);
                foreach (var size in axisSizes.Values)
                    estimatedBytes = checked(estimatedBytes * Math.Max(1, size));

                return new FitsFileInfo
                {
                    Handle         = handle,
                    FilePath       = path,
                    HduList        = hduList,
                    NAxis          = nAxis,
                    AxisSizes      = axisSizes,
                    HeaderText     = headerSb.ToString(),
                    EstimatedBytes = estimatedBytes,
                };
            }
            catch
            {
                // Failure: close the file before letting the exception escape so we
                // do not leak the native pointer.
                FitsReader.FitsCloseFile(fptr, out _);
                throw;
            }
        }

        private static string ReadHeaderText(IFitsHandle handle, int hduIndex)
        {
            if (handle is not FitsHandle fh)
                throw new ArgumentException("Handle was not produced by this adapter.", nameof(handle));
            if (fh.IsClosed)
                throw new ObjectDisposedException(nameof(IFitsHandle));

            int status = 0;
            FitsReader.FitsMovabsHdu(fh.Ptr, hduIndex, out _, out status);
            var headers = FitsReader.ExtractHeaders(fh.Ptr, out status);
            var sb = new StringBuilder();
            foreach (var kvp in headers)
                sb.Append(kvp.Key).Append("\t\t ").Append(kvp.Value).Append('\n');
            return sb.ToString();
        }

        // ── Handle implementation — sealed inside the adapter ─────────────────
        //
        // The ViewModel sees only IFitsHandle; the IntPtr never escapes this class.
        // Dispose closes the native pointer exactly once and is safe to call twice.

        private sealed class FitsHandle : IFitsHandle
        {
            private IntPtr _ptr;

            public FitsHandle(IntPtr ptr, string filePath)
            {
                _ptr     = ptr;
                FilePath = filePath;
            }

            public string FilePath { get; }

            internal IntPtr Ptr      => _ptr;
            internal bool   IsClosed => _ptr == IntPtr.Zero;

            public void Dispose()
            {
                if (_ptr == IntPtr.Zero) return;
                FitsReader.FitsCloseFile(_ptr, out _);
                _ptr = IntPtr.Zero;
            }
        }
    }
}
