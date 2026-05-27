/*
 * REFACTORING EXAMPLE 1 — Moment-Map Generation
 * Sub-team 5: Feature System and Domain Model
 *
 * IMomentMapService.cs — AFTER STATE (new file, design-level example)
 * ====================================================================
 * Design role: Application-layer use-case interface for moment-map generation.
 *
 * NAMESPACE
 * ─────────
 * iDaVIE.Application.Feature  (ADR-008)
 *
 * ADR-011 states: "Moment-map generation … [is an] Application-layer use case
 * that calls into the Feature aggregate via FeatureSetService — no direct
 * Unity dependency."
 * Therefore this service interface belongs in iDaVIE.Application.Feature,
 * NOT in iDaVIE.Domain.Feature and NOT in the old DataFeatures namespace.
 *
 * ADR-006 RELATIONSHIP
 * ────────────────────
 * The before state (MomentMapRenderer) mixed Unity lifecycle, GPU dispatch,
 * colour-mapping, and UI update all in one MonoBehaviour.
 * This interface is the seam that lets the thin MonoBehaviour adapter
 * (MomentMapRendererAdapter, iDaVIE.Infrastructure.Unity) delegate
 * all business orchestration outward to MomentMapService — which in turn
 * delegates the raw GPU computation to the injected IMomentMapAdapter.
 *
 * DELEGATION CHAIN
 * ────────────────
 * MomentMapRendererAdapter : MonoBehaviour     [iDaVIE.Infrastructure.Unity]
 *   → IMomentMapService.GenerateMomentMaps()  [iDaVIE.Application.Feature] ← here
 *     → MomentMapService                      [iDaVIE.Application.Feature]
 *       → IMomentMapAdapter.Compute()         [iDaVIE.Application.Feature]
 *         → MomentMapRendererAdapter (GPU)    [iDaVIE.Infrastructure.Unity]
 *       → MomentMapCalculator.ComputeBounds() [iDaVIE.Domain.Feature]
 *     returns MomentMapResult                 [iDaVIE.Application.Feature]
 *
 * CK METRICS (target)
 * ───────────────────
 * WMC  = 1   (single method)
 * CBO  = 2   (MomentMapRequest, MomentMapResult — same namespace)
 * RFC  = 1
 * LCOM = 0
 */

namespace iDaVIE.Application.Feature
{
    /// <summary>
    /// Application-layer use-case interface for moment-map generation.
    /// <para>
    /// Inject the concrete <see cref="MomentMapService"/> in production.
    /// Inject a stub that returns fixed pixel arrays in unit tests — no
    /// Unity runtime or GPU compute shader required.
    /// </para>
    /// </summary>
    public interface IMomentMapService
    {
        /// <summary>
        /// Generates moment-0 (integrated intensity) and moment-1
        /// (flux-weighted mean velocity) maps for the given request.
        /// </summary>
        /// <param name="request">
        ///   Describes the data cube, mask, spectrum, and threshold settings.
        ///   Must not be <c>null</c>.
        /// </param>
        /// <returns>
        ///   A <see cref="MomentMapResult"/> containing the computed pixel
        ///   arrays and their min/max bounds — ready for colour-mapping in
        ///   the Infrastructure adapter.
        /// </returns>
        MomentMapResult GenerateMomentMaps(MomentMapRequest request);
    }
}
