/*
 * REFACTORING EXAMPLE 1 — Moment-Map Generation
 * Sub-team 5: Feature System and Domain Model
 *
 * MomentMapService.cs — AFTER STATE (new file, design-level example)
 * ===================================================================
 * Design role: Application-layer concrete service — orchestrates computation
 *              and bounds extraction for moment-map generation.
 *
 * NAMESPACE
 * ─────────
 * iDaVIE.Application.Feature  (ADR-008)
 *
 * WHAT THIS REPLACES
 * ──────────────────
 * Before: MomentMapRenderer.CalculateMomentMaps() did everything inside one
 *         MonoBehaviour method — GPU dispatch, bounds extraction (via
 *         RenderTexture readback), and UI update (colour-mapped sprite).
 *         It was untestable and violated SRP.
 *
 * After:  MomentMapService handles the orchestration only:
 *           1. Validate the request.
 *           2. Delegate raw pixel computation to IMomentMapAdapter (GPU in prod,
 *              stub in tests — ADR-002 Anti-Corruption Layer).
 *           3. Compute min/max bounds using MomentMapCalculator (pure math,
 *              iDaVIE.Domain.Feature — no Unity dep).
 *           4. Return an immutable MomentMapResult.
 *
 *         The Infrastructure adapter (MomentMapRendererAdapter) receives the
 *         result and handles all Unity GPU / UI concerns — colour-mapping,
 *         RenderTexture upload, colourbar update, sprite creation.
 *
 * ADR-003 DEPENDENCY INJECTION
 * ────────────────────────────
 * IMomentMapAdapter is injected at construction. MomentMapService never
 * calls 'new' on any concrete type — it has no coupling to UnityEngine.
 *
 * ZERO UnityEngine IMPORTS (ADR-002)
 * ───────────────────────────────────
 * No using UnityEngine — this class is fully unit-testable with a
 * StubMomentMapAdapter that returns fixed float arrays.
 *
 * CK METRICS (target)
 * ───────────────────
 * WMC  = 2   (constructor + GenerateMomentMaps)
 * CBO  = 4   (IMomentMapAdapter, MomentMapRequest, MomentMapResult,
 *             MomentMapCalculator — all internal dependencies)
 * RFC  = 4
 * LCOM = 0
 */

using System;
using iDaVIE.Domain.Feature;

// NOTE: No "using UnityEngine" — this class is intentionally Unity-free (ADR-002).

namespace iDaVIE.Application.Feature
{
    /// <summary>
    /// Application-layer orchestrator for moment-map generation.
    /// <para>
    /// Delegates raw pixel computation to the injected <see cref="IMomentMapAdapter"/>
    /// (GPU in production, a stub in unit tests), then uses the pure-C#
    /// <see cref="MomentMapCalculator"/> to compute display bounds before
    /// packaging results into an immutable <see cref="MomentMapResult"/>.
    /// </para>
    /// <para>
    /// This class contains no Unity types — it is fully testable in headless CI.
    /// </para>
    /// </summary>
    public sealed class MomentMapService : IMomentMapService
    {
        private readonly IMomentMapAdapter _adapter;

        /// <param name="adapter">
        ///   Computation back-end.
        ///   In production: <c>MomentMapRendererAdapter</c> (dispatches GPU kernels).
        ///   In tests: a <c>StubMomentMapAdapter</c> returning fixed float arrays.
        /// </param>
        public MomentMapService(IMomentMapAdapter adapter)
        {
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        }

        /// <inheritdoc/>
        public MomentMapResult GenerateMomentMaps(MomentMapRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // ── Step 1: Delegate raw pixel computation to the adapter ─────────
            // In production this dispatches a ComputeShader kernel on the GPU.
            // In tests this returns pre-set float arrays — no Unity required.
            var (moment0, moment1) = _adapter.Compute(request);

            // ── Step 2: Compute bounds using pure domain math ─────────────────
            // MomentMapCalculator lives in iDaVIE.Domain.Feature — no Unity dep.
            // The service depends inward on the domain layer (ADR-001 ✓).
            var (m0Min, m0Max) = MomentMapCalculator.ComputeMinMaxBounds(moment0);
            var (m1Min, m1Max) = MomentMapCalculator.ComputeMinMaxBounds(moment1);

            // ── Step 3: Return immutable result ──────────────────────────────
            return new MomentMapResult(
                moment0Pixels: moment0,
                moment1Pixels: moment1,
                width:         request.Width,
                height:        request.Height,
                moment0Min:    m0Min,
                moment0Max:    m0Max,
                moment1Min:    m1Min,
                moment1Max:    m1Max);
        }
    }
}
