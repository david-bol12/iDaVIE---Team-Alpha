/*
 * REFACTORING EXAMPLE 1 — Moment-Map Generation
 * Sub-team 5: Feature System and Domain Model
 *
 * MomentMapCalculator.cs — AFTER STATE (new file, design-level example)
 * =======================================================================
 * Design role: pure-C# domain math for moment-map post-processing.
 *
 * NAMESPACE
 * ─────────
 * iDaVIE.Domain.Feature  (ADR-008)
 *
 * WHY DOMAIN, NOT APPLICATION
 * ───────────────────────────
 * This class contains only stateless mathematical operations on float arrays
 * (bounds extraction, moment-0, moment-1). It has no orchestration, no I/O,
 * and no framework dependency. Pure math belongs in the Domain layer as a
 * shared utility — Application services call it, but the Domain layer owns it.
 *
 * This is consistent with GRASP Information Expert: the Domain layer is the
 * expert on what moment maps *mean* mathematically. The Application layer
 * is the expert on *when* to compute them.
 *
 * WHAT THIS REPLACES
 * ──────────────────
 * In the before state, MomentMapRenderer.GetBounds() performed min/max
 * extraction by reading back pixels from a RenderTexture into a Texture2D
 * and iterating the raw data — tightly coupled to Unity GPU types.
 *
 * MomentMapCalculator performs the same bounds computation on plain float[]
 * arrays. In production, the Infrastructure adapter reads back from the GPU,
 * converts to float[], and passes to this class. In tests, pass any float[]
 * directly — no Unity runtime required.
 *
 * NOTE ON CPU MOMENT COMPUTATION
 * ──────────────────────────────
 * ComputeMoment0 and ComputeMoment1 provide CPU-side reference implementations
 * of the same algorithms run on the GPU in MomentMapRendererAdapter.
 * They serve two purposes:
 *   1. Unit-test ground truth — the GPU adapter's output can be compared
 *      against these results for correctness validation.
 *   2. Headless / CI execution — moment maps can be generated in CI without
 *      a GPU or Unity process (useful for integration tests).
 *
 * CK METRICS (target)
 * ───────────────────
 * WMC  = 3   (three static methods)
 * CBO  = 0   (no dependencies beyond BCL)
 * RFC  = 3
 * LCOM = 0   (static — all methods share the same data abstraction)
 */

using System;

namespace iDaVIE.Domain.Feature
{
    /// <summary>
    /// Pure-C# moment-map calculation helpers.
    /// No Unity or framework dependencies — fully unit-testable in headless CI.
    /// </summary>
    public static class MomentMapCalculator
    {
        // ── Bounds extraction ────────────────────────────────────────────────

        /// <summary>
        /// Returns the (min, max) finite value pair for the given pixel array,
        /// ignoring <see cref="float.NaN"/> entries.
        /// </summary>
        /// <param name="pixels">
        ///   Flat float array of moment-map pixel values. Must not be null or empty.
        /// </param>
        /// <returns>
        ///   (min, max) tuple. If all pixels are NaN, returns (0f, 0f).
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   Thrown when <paramref name="pixels"/> is null or empty.
        /// </exception>
        public static (float min, float max) ComputeMinMaxBounds(float[] pixels)
        {
            if (pixels == null || pixels.Length == 0)
                throw new ArgumentException("Pixel array must not be null or empty.", nameof(pixels));

            float min = float.MaxValue;
            float max = float.MinValue;

            foreach (float v in pixels)
            {
                if (float.IsNaN(v)) continue;
                if (v < min) min = v;
                if (v > max) max = v;
            }

            // If every pixel is NaN (e.g. masked-out region) return safe defaults.
            if (min > max) return (0f, 0f);

            return (min, max);
        }

        // ── CPU reference implementations (used for test ground truth) ───────

        /// <summary>
        /// CPU-side moment-0 (integrated intensity) computation.
        /// Each output pixel is the sum of voxel values along the spectral (Z) axis
        /// that exceed <paramref name="threshold"/>.
        /// </summary>
        /// <param name="dataVoxels">Flat voxel array ordered [z, y, x].</param>
        /// <param name="width">Cube width in pixels.</param>
        /// <param name="height">Cube height in pixels.</param>
        /// <param name="depth">Number of spectral channels.</param>
        /// <param name="threshold">Voxels at or below this value are excluded.</param>
        /// <returns>
        ///   Flat float array of length <c>width * height</c>, ordered [y, x].
        /// </returns>
        public static float[] ComputeMoment0(
            float[] dataVoxels,
            int width, int height, int depth,
            float threshold)
        {
            if (dataVoxels == null) throw new ArgumentNullException(nameof(dataVoxels));

            float[] moment0 = new float[width * height];

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width;  x++)
            {
                float sum = 0f;
                for (int z = 0; z < depth; z++)
                {
                    float v = dataVoxels[z * width * height + y * width + x];
                    if (!float.IsNaN(v) && v > threshold)
                        sum += v;
                }
                moment0[y * width + x] = sum;
            }

            return moment0;
        }

        /// <summary>
        /// CPU-side moment-1 (flux-weighted mean velocity) computation.
        /// Each output pixel is the flux-weighted mean of the <paramref name="spectrumZ"/>
        /// values along the Z axis, restricted to voxels above <paramref name="threshold"/>.
        /// Pixels with zero integrated flux are set to <see cref="float.NaN"/>.
        /// </summary>
        /// <param name="dataVoxels">Flat voxel array ordered [z, y, x].</param>
        /// <param name="spectrumZ">
        ///   Physical Z-axis values (velocity / frequency) per channel.
        ///   Length must equal <paramref name="depth"/>.
        /// </param>
        /// <param name="width">Cube width in pixels.</param>
        /// <param name="height">Cube height in pixels.</param>
        /// <param name="depth">Number of spectral channels.</param>
        /// <param name="threshold">Voxels at or below this value are excluded.</param>
        /// <returns>
        ///   Flat float array of length <c>width * height</c>, ordered [y, x].
        ///   NaN where integrated flux is zero.
        /// </returns>
        public static float[] ComputeMoment1(
            float[] dataVoxels,
            float[] spectrumZ,
            int width, int height, int depth,
            float threshold)
        {
            if (dataVoxels == null) throw new ArgumentNullException(nameof(dataVoxels));
            if (spectrumZ  == null) throw new ArgumentNullException(nameof(spectrumZ));
            if (spectrumZ.Length != depth)
                throw new ArgumentException(
                    $"spectrumZ.Length ({spectrumZ.Length}) must equal depth ({depth}).",
                    nameof(spectrumZ));

            float[] moment1 = new float[width * height];

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width;  x++)
            {
                float weightedSum = 0f;
                float totalFlux   = 0f;

                for (int z = 0; z < depth; z++)
                {
                    float v = dataVoxels[z * width * height + y * width + x];
                    if (!float.IsNaN(v) && v > threshold)
                    {
                        weightedSum += v * spectrumZ[z];
                        totalFlux   += v;
                    }
                }

                moment1[y * width + x] = totalFlux > 0f
                    ? weightedSum / totalFlux
                    : float.NaN;
            }

            return moment1;
        }
    }
}
