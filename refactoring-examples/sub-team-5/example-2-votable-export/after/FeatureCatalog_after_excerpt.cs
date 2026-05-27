/*
 * REFACTORING EXAMPLE 2 — VOTable Export
 * Sub-team 5: Feature System and Domain Model
 *
 * FeatureCatalog_after_excerpt.cs — AFTER STATE (annotated excerpt)
 * =================================================================
 * This file is NOT the full FeatureCatalog class.
 * It is an annotated excerpt showing the ExportToVoTable method and the
 * constructor injection of IFeaturePersistenceService to illustrate the
 * complete delegation chain for this example.
 *
 * The full production class is at:
 *   Assets/Scripts/FeatureData/FeatureCatalog.cs
 *
 * DELEGATION CHAIN (Example 2)
 * ────────────────────────────
 *
 *   [Unity layer]          FeatureMenuController
 *                                  │ calls
 *   [Application layer]    FeatureSetService.ExportToVoTable(set)
 *                                  │ delegates to
 *   [Domain layer]         FeatureCatalog.ExportToVoTable(set)    ← this file
 *                                  │ delegates to
 *   [Persistence boundary] IFeaturePersistenceService.ExportToVoTable(set)
 *                                  │ delegates to
 *   [Format plug-in]       IVoTableExporter.Export(set, transformer)
 *                                  │ implemented by
 *                          VoTableExportService  (pure C#, no Unity)
 *
 * Each arrow crosses exactly one layer boundary.
 * Dependencies point inward only — FeatureCatalog does not reference
 * IVoTableExporter directly; it only knows IFeaturePersistenceService.
 * This satisfies the §4.1 layered architecture constraint.
 *
 * KEY CHANGES FROM BEFORE STATE
 * ──────────────────────────────
 * Before: FeatureSetManager called VoTableSaver.SaveFeatureSetAsVoTable(renderer, path)
 *         directly — a static call with a Unity MonoBehaviour argument.
 *
 * After:  FeatureCatalog.ExportToVoTable(set) takes a plain FeatureSet domain
 *         object and delegates to IFeaturePersistenceService. FeatureCatalog
 *         has no knowledge of XML, AstTool, or file paths.
 */

using System;

namespace DataFeatures
{
    /// <summary>
    /// Excerpt: FeatureCatalog showing constructor injection and
    /// ExportToVoTable delegation.
    /// See the full class in Assets/Scripts/FeatureData/FeatureCatalog.cs.
    /// </summary>
    public sealed partial class FeatureCatalog
    {
        // ── Persistence boundary ─────────────────────────────────────────────
        //
        // IFeaturePersistenceService is injected at construction.
        // FeatureCatalog never creates the concrete persistence object itself —
        // that would violate the Dependency Inversion Principle.
        //
        // In production: FeaturePersistenceService (WP7 implementation) is
        //   passed in, which in turn holds an injected IVoTableExporter.
        //
        // In unit tests: NullFeaturePersistenceService (or a mock) is passed in,
        //   keeping FeatureCatalog tests free of file I/O and DLL calls.
        private readonly IFeaturePersistenceService _persistence;

        /// <param name="persistence">
        ///   Persistence service implementation.
        ///   Pass <c>NullFeaturePersistenceService</c> in unit tests.
        /// </param>
        public FeatureCatalog(IFeaturePersistenceService persistence)
        {
            // Guard clause — a null persistence service would produce a
            // NullReferenceException deep inside a VOTable export, which is
            // harder to diagnose than an early ArgumentNullException here.
            _persistence = persistence
                ?? throw new ArgumentNullException(nameof(persistence));
        }

        // ── Use-case: Export a FeatureSet to VOTable ─────────────────────────

        /// <summary>
        /// Exports <paramref name="set"/> to a VOTable XML file.
        /// <para>
        /// FeatureCatalog owns the decision of *when* to export.
        /// <see cref="IFeaturePersistenceService"/> owns *how* to serialise
        /// and *where* to write the file (path resolution, timestamp naming).
        /// </para>
        /// </summary>
        /// <returns>
        ///   The absolute path of the file that was written,
        ///   forwarded from the persistence service for display in the GUI.
        /// </returns>
        public string ExportToVoTable(FeatureSet set)
        {
            if (set == null) throw new ArgumentNullException(nameof(set));

            // Single delegation call — FeatureCatalog knows nothing about
            // XML, AstTool, or IVoTableExporter. Those are persistence-layer
            // concerns that live behind IFeaturePersistenceService.
            return _persistence.ExportToVoTable(set);
        }

        // ── Note on IFeaturePersistenceService.ExportToVoTable ───────────────
        //
        // The concrete persistence service (WP7 team) will receive an
        // IVoTableExporter and ICoordinateTransformer at its own construction:
        //
        //   public class FeaturePersistenceService : IFeaturePersistenceService
        //   {
        //       private readonly IVoTableExporter      _exporter;
        //       private readonly ICoordinateTransformer _transformer;
        //
        //       public FeaturePersistenceService(
        //           IVoTableExporter exporter,
        //           ICoordinateTransformer transformer)
        //       {
        //           _exporter    = exporter;
        //           _transformer = transformer;
        //       }
        //
        //       public string ExportToVoTable(FeatureSet featureSet)
        //       {
        //           string xml      = _exporter.Export(featureSet, _transformer);
        //           string filePath = ResolveOutputPath(featureSet);
        //           File.WriteAllText(filePath, xml);
        //           return filePath;
        //       }
        //   }
        //
        // This keeps file I/O out of VoTableExportService and out of the
        // domain layer, satisfying the single-responsibility principle at
        // every layer.
    }
}
