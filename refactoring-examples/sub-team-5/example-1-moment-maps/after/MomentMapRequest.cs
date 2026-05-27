/*
 * REFACTORING EXAMPLE 1 — Moment-Map Generation
 * Sub-team 5: Feature System and Domain Model
 *
 * MomentMapRequest.cs — AFTER STATE (new file, design-level example)
 * ===================================================================
 * Design role: immutable value object describing the INPUT to
 *              IMomentMapService.GenerateMomentMaps().
 *
 * NAMESPACE
 * ─────────
 * iDaVIE.Application.Feature  (ADR-008)
 *
 * WHAT THIS REPLACES
 * ──────────────────
 * In the before state (MomentMapRenderer), the inputs were scattered across
 * MonoBehaviour fields: _dataCube (Texture3D), _maskCube (Texture3D),
 * MomentMapThreshold (float), UseMask (bool), and the spectrum was assembled
 * inline from _parentVolumeDataSetRenderer.
 * None of these inputs could be constructed or passed around without a
 * running Unity scene.
 *
 * MomentMapRequest replaces all of those with a plain C# value object:
 *   • DataVoxels  — flat float[] copied from the data cube (no Texture3D)
 *   • MaskVoxels  — flat float[] (optional; null when UseMask = false)
 *   • SpectrumZ   — float[] of physical Z-axis values (pre-computed by adapter)
 *   • Width/Height/Depth — spatial dimensions
 *   • Threshold   — float threshold value
 *   • UseMask     — whether to apply the mask voxel array
 *
 * Zero UnityEngine types — the request can be constructed in a unit test
 * with raw float arrays and fixed dimensions.
 *
 * CK METRICS (target)
 * ───────────────────
 * WMC  = 1   (one constructor)
 * CBO  = 0   (no dependencies beyond BCL)
 * RFC  = 1
 * LCOM = 0
 */

using System;

namespace iDaVIE.Application.Feature
{
    /// <summary>
    /// Immutable value object describing the inputs to a moment-map generation
    /// request. All data is expressed as plain C# arrays — no Unity types.
    /// </summary>
    public sealed class MomentMapRequest
    {
        /// <summary>Flat float array of voxel values, ordered [z, y, x].</summary>
        public float[] DataVoxels { get; }

        /// <summary>
        /// Flat float array of mask voxel values, ordered [z, y, x].
        /// May be <c>null</c> when <see cref="UseMask"/> is <c>false</c>.
        /// </summary>
        public float[] MaskVoxels { get; }

        /// <summary>
        /// Physical Z-axis values (velocity, frequency, or redshift) for each
        /// channel, pre-computed by the Infrastructure adapter from AstTool.
        /// Length must equal <see cref="Depth"/>.
        /// </summary>
        public float[] SpectrumZ { get; }

        /// <summary>Width of the data cube in pixels.</summary>
        public int Width { get; }

        /// <summary>Height of the data cube in pixels.</summary>
        public int Height { get; }

        /// <summary>Number of spectral channels (Z axis).</summary>
        public int Depth { get; }

        /// <summary>
        /// Threshold below which voxel values are excluded from moment-map
        /// computation. Ignored when <see cref="UseMask"/> is <c>true</c>.
        /// </summary>
        public float Threshold { get; }

        /// <summary>
        /// When <c>true</c>, <see cref="MaskVoxels"/> is used to select
        /// contributing voxels instead of <see cref="Threshold"/>.
        /// </summary>
        public bool UseMask { get; }

        /// <summary>
        /// Constructs a new moment-map request.
        /// </summary>
        /// <param name="dataVoxels">Flat voxel float array [z,y,x]. Must not be null.</param>
        /// <param name="spectrumZ">Physical Z values per channel. Length must equal <paramref name="depth"/>.</param>
        /// <param name="width">Cube width in pixels. Must be &gt; 0.</param>
        /// <param name="height">Cube height in pixels. Must be &gt; 0.</param>
        /// <param name="depth">Number of spectral channels. Must be &gt; 0.</param>
        /// <param name="threshold">Voxel threshold (used when <paramref name="useMask"/> is false).</param>
        /// <param name="useMask">Whether to use <paramref name="maskVoxels"/> for selection.</param>
        /// <param name="maskVoxels">Optional mask voxel array [z,y,x]. May be null when <paramref name="useMask"/> is false.</param>
        public MomentMapRequest(
            float[] dataVoxels,
            float[] spectrumZ,
            int     width,
            int     height,
            int     depth,
            float   threshold,
            bool    useMask   = false,
            float[] maskVoxels = null)
        {
            DataVoxels = dataVoxels ?? throw new ArgumentNullException(nameof(dataVoxels));
            SpectrumZ  = spectrumZ  ?? throw new ArgumentNullException(nameof(spectrumZ));
            if (width  <= 0) throw new ArgumentOutOfRangeException(nameof(width),  "Must be > 0.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Must be > 0.");
            if (depth  <= 0) throw new ArgumentOutOfRangeException(nameof(depth),  "Must be > 0.");

            Width      = width;
            Height     = height;
            Depth      = depth;
            Threshold  = threshold;
            UseMask    = useMask;
            MaskVoxels = maskVoxels;
        }
    }
}
