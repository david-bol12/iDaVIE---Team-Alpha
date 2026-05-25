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
    /// The ViewModel receives only plain <see cref="FitsFileInfo"/> DTOs.
    ///
    /// Replaces the scattered FitsReader calls in CanvassDesktop._browseImageFile
    /// (lines 349–407) and UpdateHeaderFromFits (lines 539–568).
    /// </summary>
    public sealed class FitsServiceAdapter : IFitsService
    {
        public Task<FitsFileInfo> OpenImageAsync(string path, CancellationToken ct = default)
            => Task.Run(() => ReadFitsMetadata(path), ct);

        public Task<FitsFileInfo> OpenMaskAsync(string path, CancellationToken ct = default)
            => Task.Run(() => ReadFitsMetadata(path), ct);

        public Task<string> GetHeaderTextAsync(string path, int hduIndex, CancellationToken ct = default)
            => Task.Run(() => ReadHeaderText(path, hduIndex), ct);

        // ── Core P/Invoke logic (thread-pool, away from Unity main thread) ────

        private static FitsFileInfo ReadFitsMetadata(string path)
        {
            int status = 0;
            if (FitsReader.FitsOpenFile(out IntPtr fptr, path, out status, true) != 0)
                throw new InvalidOperationException($"FITS open failed (status {status}): {path}");

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

                return new FitsFileInfo
                {
                    FilePath   = path,
                    HduList    = hduList,
                    NAxis      = nAxis,
                    AxisSizes  = axisSizes,
                    HeaderText = headerSb.ToString(),
                };
            }
            finally
            {
                // Always close — no IntPtr outlives this method
                FitsReader.FitsCloseFile(fptr, out _);
            }
        }

        private static string ReadHeaderText(string path, int hduIndex)
        {
            int status = 0;
            if (FitsReader.FitsOpenFile(out IntPtr fptr, path, out status, true) != 0)
                throw new InvalidOperationException($"FITS open failed (status {status}): {path}");

            try
            {
                FitsReader.FitsMovabsHdu(fptr, hduIndex + 1, out _, out status);
                var headers = FitsReader.ExtractHeaders(fptr, out status);
                var sb = new StringBuilder();
                foreach (var kvp in headers)
                    sb.Append(kvp.Key).Append("\t\t ").Append(kvp.Value).Append('\n');
                return sb.ToString();
            }
            finally
            {
                FitsReader.FitsCloseFile(fptr, out _);
            }
        }
    }
}
