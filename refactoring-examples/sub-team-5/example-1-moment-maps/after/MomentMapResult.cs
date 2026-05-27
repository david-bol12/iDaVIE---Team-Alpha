/*
 * REFACTORING EXAMPLE 1 — Moment-Map Generation
 * Sub-team 5: Feature System and Domain Model
 *
 * MomentMapResult.cs — AFTER STATE (new file, design-level example)
 * ==================================================================
 * Design role: immutable value object describing the OUTPUT of
 *              IMomentMapService.GenerateMomentMaps().
 *
 * NAMESPACE
 * ─────────
 * iDaVIE.Application.Feature  (ADR-008)
 *
 * WHAT THIS REPLACES
 * ──────────────────
 * In the before state, MomentMapRenderer exposed output as Unity RenderTexture
 * properties (Moment0Map, Moment1Map, ImageOutput) that could only be consumed
 * inside a running Unity scene. Bounds were stored in private Vector2 fields
 * (_moment0Bounds, _moment1Bounds) with no external accessor.
 *
 * MomentMapResult packages the same outputs as plain C# arrays and scalar
 * bounds — all consumable without UnityEngine. The Infrastructure adapter
 * (MomentMapRendererAdapter) receives the result and uploads it to GPU
 * textures and UI elements in the Unity-aware layer.
 *
 * Zero UnityEngine types — the result can be asserted in unit tests with
 * standard float array comparisons.
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
    /// Immutable value object containing the computed moment-0 and moment-1
    /// pixel arrays and their display bounds.
    /// All data is expressed as plain C# arrays — no Unity types.
    /// </summary>
    public sealed class MomentMapResult
    {
        /// <summary>
        /// Flat float array of moment-0 (integrated intensity) pixel values,
        /// ordered [y, x]. Length = <see cref="Width"/> × <see cref="Height"/>.
        /// </summary>
        public float[] Moment0Pixels { get; }

        /// <summary>
        /// Flat float array of moment-1 (flux-weighted mean velocity) pixel values,
        /// ordered [y, x]. Length = <see cref="Width"/> × <see cref="Height"/>.
        /// Pixels with zero integrated flux are <see cref="float.NaN"/>.
        /// </summary>
        public float[] Moment1Pixels { get; }

        /// <summary>Map width in pixels.</summary>
        public int Width { get; }

        /// <summary>Map height in pixels.</summary>
        public int Height { get; }

        /// <summary>Minimum finite value in <see cref="Moment0Pixels"/> (NaN excluded).</summary>
        public float Moment0Min { get; }

        /// <summary>Maximum finite value in <see cref="Moment0Pixels"/> (NaN excluded).</summary>
        public float Moment0Max { get; }

        /// <summary>Minimum finite value in <see cref="Moment1Pixels"/> (NaN excluded).</summary>
        public float Moment1Min { get; }

        /// <summary>Maximum finite value in <see cref="Moment1Pixels"/> (NaN excluded).</summary>
        public float Moment1Max { get; }

        /// <summary>
        /// Constructs a new moment-map result.
        /// </summary>
        public MomentMapResult(
            float[] moment0Pixels,
            float[] moment1Pixels,
            int     width,
            int     height,
            float   moment0Min,
            float   moment0Max,
            float   moment1Min,
            float   moment1Max)
        {
            Moment0Pixels = moment0Pixels ?? throw new ArgumentNullException(nameof(moment0Pixels));
            Moment1Pixels = moment1Pixels ?? throw new ArgumentNullException(nameof(moment1Pixels));
            if (width  <= 0) throw new ArgumentOutOfRangeException(nameof(width),  "Must be > 0.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Must be > 0.");

            Width      = width;
            Height     = height;
            Moment0Min = moment0Min;
            Moment0Max = moment0Max;
            Moment1Min = moment1Min;
            Moment1Max = moment1Max;
        }
    }
}
