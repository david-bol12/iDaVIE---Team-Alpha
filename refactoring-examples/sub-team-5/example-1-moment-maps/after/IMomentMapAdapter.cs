/*
 * REFACTORING EXAMPLE 1 — Moment-Map Generation
 * Sub-team 5: Feature System and Domain Model
 *
 * IMomentMapAdapter.cs — AFTER STATE (new file, design-level example)
 * ====================================================================
 * Design role: Anti-Corruption Layer interface for GPU compute-shader dispatch.
 *
 * NAMESPACE
 * ─────────
 * iDaVIE.Application.Feature  (ADR-008)
 *
 * ADR-002 ANTI-CORRUPTION LAYER
 * ──────────────────────────────
 * The before state (MomentMapRenderer) dispatched ComputeShader kernels directly
 * inside the same class that held domain state (threshold, useMask, colour maps).
 * This made the computation path impossible to test without a Unity GPU context.
 *
 * IMomentMapAdapter is the ACL seam:
 *   • Application/Domain code depends on this interface (inward dependency ✓)
 *   • The production implementation (MomentMapRendererAdapter) lives in
 *     iDaVIE.Infrastructure.Unity and is the ONLY class that imports UnityEngine
 *     to do GPU work.
 *   • In unit tests, inject a StubMomentMapAdapter that returns pre-computed
 *     float arrays — no ComputeShader, no RenderTexture, no Unity runtime.
 *
 * RESPONSIBILITY SPLIT
 * ────────────────────
 * IMomentMapAdapter.Compute()  — raw pixel extraction (GPU in prod, float[] in tests)
 * MomentMapCalculator          — bounds computation (pure math, always CPU)
 * MomentMapService             — orchestrates both, returns MomentMapResult
 *
 * This respects SRP: the adapter knows how to dispatch GPU work; it does not
 * know what to do with the results (that is the service's job).
 *
 * CK METRICS (target)
 * ───────────────────
 * WMC  = 1   (single method)
 * CBO  = 2   (MomentMapRequest, tuple return type — same namespace)
 * RFC  = 1
 * LCOM = 0
 */

namespace iDaVIE.Application.Feature
{
    /// <summary>
    /// Anti-Corruption Layer interface for the moment-map GPU computation back-end.
    /// <para>
    /// Abstracts Unity <c>ComputeShader</c> dispatch behind a testable boundary.
    /// The production implementation (<c>MomentMapRendererAdapter</c>) lives in
    /// <c>iDaVIE.Infrastructure.Unity</c> and dispatches GPU kernels.
    /// A stub can be injected in unit tests without a Unity runtime.
    /// </para>
    /// </summary>
    public interface IMomentMapAdapter
    {
        /// <summary>
        /// Computes the raw moment-0 and moment-1 pixel arrays from the data
        /// described by <paramref name="request"/>.
        /// </summary>
        /// <param name="request">
        ///   Input description: voxel data, optional mask, spectrum, dimensions,
        ///   threshold, and mask flag. Must not be <c>null</c>.
        /// </param>
        /// <returns>
        ///   A tuple of (<c>moment0</c>, <c>moment1</c>), each a flat float array
        ///   of length <c>request.Width * request.Height</c>, ordered [y, x].
        ///   Pixel values are raw sums/means before colour-mapping or display.
        ///   <c>moment1</c> pixels with zero integrated flux are
        ///   <see cref="float.NaN"/>.
        /// </returns>
        (float[] moment0, float[] moment1) Compute(MomentMapRequest request);
    }
}
