/*
 * REFACTORING EXAMPLE 2 — VOTable Export
 * Sub-team 5: Feature System and Domain Model
 *
 * IVoTableExporter.cs — AFTER STATE (new file, design-level example)
 * ===================================================================
 * Design role: explicit export plug-in seam.
 *
 * WHY THIS INTERFACE EXISTS
 * ─────────────────────────
 * In the before state, VoTableSaver was a static class in the VoTableReader
 * namespace. There was no interface, so callers were hard-coupled to one
 * output format. Adding FITS or JSON-LD output would require modifying
 * VoTableSaver — an Open/Closed violation.
 *
 * IVoTableExporter defines the *only* method FeatureCatalog needs to know
 * about. Any concrete exporter (VOTable, FITS, JSON-LD) can be injected
 * without modifying FeatureCatalog — satisfying the Open/Closed Principle
 * and §4.2 constraint 4 (every public API boundary expressed as an interface).
 *
 * NAMESPACE
 * ─────────
 * iDaVIE.Domain.Feature  (ADR-008)
 * This interface is referenced by FeatureCatalog (domain), which means it
 * must live in the Domain namespace so the dependency arrow points inward.
 * The concrete implementation (VoTableExportService) lives in
 * iDaVIE.Infrastructure.Persistence and depends on this interface — correct
 * outward-to-inward direction.
 *
 * DELEGATION CHAIN
 * ────────────────
 * FeatureSetService.ExportToVoTable          [iDaVIE.Application.Feature]
 *   → FeatureCatalog.ExportToVoTable         [iDaVIE.Domain.Feature]
 *     → IFeaturePersistenceService           [iDaVIE.Domain.Feature]
 *       → IVoTableExporter.Export            [iDaVIE.Domain.Feature]  ← here
 *         implemented by VoTableExportService [iDaVIE.Infrastructure.Persistence]
 *
 * CK METRICS (target)
 * ───────────────────
 * WMC  = 1   (single method)
 * CBO  = 0   (no concrete dependencies — both params are domain types)
 * RFC  = 1
 * LCOM = 0
 */

namespace iDaVIE.Domain.Feature
{
    /// <summary>
    /// Plug-in seam for VOTable serialisation.
    /// <para>
    /// Implement this interface to provide a concrete exporter.
    /// Inject the implementation into <see cref="IFeaturePersistenceService"/>
    /// at the composition root — <see cref="FeatureCatalog"/> is never aware
    /// of the concrete type.
    /// </para>
    /// <para>
    /// A future FITS or JSON-LD exporter replaces this implementation
    /// without touching any domain class — Open/Closed Principle.
    /// </para>
    /// </summary>
    public interface IVoTableExporter
    {
        /// <summary>
        /// Serialises <paramref name="featureSet"/> to a VOTable 1.3 XML string.
        /// </summary>
        /// <param name="featureSet">
        ///   The domain object containing features to export.
        ///   Must not be <c>null</c>.
        /// </param>
        /// <param name="transformer">
        ///   Coordinate transformer used to convert pixel (x,y,z) to (RA, Dec, Z).
        ///   Must not be <c>null</c>.
        ///   Inject a <c>StubCoordinateTransformer</c> in unit tests for
        ///   deterministic output.
        /// </param>
        /// <returns>
        ///   A well-formed VOTable 1.3 XML string ready for writing to disk
        ///   or transmission. The caller (<see cref="IFeaturePersistenceService"/>)
        ///   owns the decision of where to write it.
        /// </returns>
        string Export(FeatureSet featureSet, ICoordinateTransformer transformer);
    }
}
