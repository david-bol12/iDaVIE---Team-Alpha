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
 * NAMESPACE
 * ─────────
 * iDaVIE.Domain.Feature  (ADR-008)
 * FeatureCatalog is a first-class domain aggregate (ADR-011).
 * It must live in the Domain namespace, not in DataFeatures (the old,
 * non-conformant name that predated the ADR-008 namespace decision).
 *
 * DELEGATION CHAIN (Example 2)
 * ────────────────────────────
 *
 *   [Unity/Client layer]       FeatureMenuController
 *                                      │ calls
 *   [Application layer]        FeatureSetService.ExportToVoTable(set)
 *          iDaVIE.Application.Feature  │ delegates to
 *   [Domain layer]             FeatureCatalog.ExportToVoTable(set)    ← this file
 *          iDaVIE.Domain.Feature       │ delegates to
 *   [Persistence boundary]     IFeaturePersistenceService.ExportToVoTable(set)
 *          iDaVIE.Domain.Feature       │ (interface — no concrete ref)
 *   [Infrastructure]           FeaturePersistenceService
 *          iDaVIE.Infrastructure.      │ calls
 *          Persistence                 │
 *   [Format plug-in]           IVoTableExporter.Export(set, transformer)
 *          iDaVIE.Domain.Feature       │ implemented by
 *                              VoTableExportService (iDaVIE.Infrastructure.Persistence)
 *
 * Each arrow crosses exactly one layer boundary.
 * Dependencies point inward only — FeatureCatalog does not reference
 * IVoTableExporter or VoTableExportService; it only knows
 * IFeaturePersistenceService. This satisfies the ADR-001 layered
 * architecture constraint and the ADR-008 namespace rules.
 *
 * KEY CHANGES FROM BEFORE STATE
 * ──────────────────────────────
 * Before: FeatureSetManager called VoTableSaver.SaveFeatureSetAsVoTable(renderer, path)
 *         directly — a static call with a Unity MonoBehaviour argument.
 *         Namespace: DataFeatures (ADR-008 non-compliant).
 *
 * After:  FeatureCatalog.ExportToVoTable(set) takes a plain FeatureSet domain
 *         object and delegates to IFeaturePersistenceService. FeatureCatalog
 *         has no knowledge of XML, AstTool, file paths, or IntPtr.
 *         Namespace: iDaVIE.Domain.Feature (ADR-008 compliant).
 */

using System;

namespace iDaVIE.Domain.Feature
{
    /// <summary>
    /// DOCUMENTATION EXCERPT — NOT COMPILED AS PART OF THE UNITY PROJECT.
    /// <para>
    /// This file is an annotated copy of selected members from
    /// <c>Assets/Scripts/FeatureData/FeatureCatalog.cs</c>. It is NOT a real
    /// <c>partial</c> extension of that class; it is placed outside <c>Assets/</c>
    /// so Unity's compiler never sees it.
    /// </para>
    /// <para>
    /// Do NOT add this file to the Assets folder or any .asmdef. Doing so will
    /// produce duplicate-member compile errors because <c>_persistence</c>,
    /// the constructor, and <c>ExportToVoTable</c> are already declared in the
    /// production class, which is <b>not</b> <c>partial</c>.
    /// </para>
    /// <para>
    /// Purpose: illustrate the ExportToVoTable delegation chain for
    /// Refactoring Example 2 without reproducing the full 224-line class.
    /// See the full class in Assets/Scripts/FeatureData/FeatureCatalog.cs.
    /// </para>
    /// </summary>
    public sealed partial class FeatureCatalog
    {
        // ── Persistence boundary ─────────────────────────────────────────────
        //
        // IFeaturePersistenceService is injected at construction (ADR-003).
        // FeatureCatalog never creates the concrete persistence object itself —
        // that would violate the Dependency Inversion Principle.
        //
        // In production: FeaturePersistenceService (iDaVIE.Infrastructure.Persistence)
        //   is passed in, which in turn holds an injected IVoTableExporter.
        //
        // In unit tests: a mock or NullFeaturePersistenceService is passed in,
        //   keeping FeatureCatalog tests free of file I/O and DLL calls.
        private readonly IFeaturePersistenceService _persistence;

        /// <param name="persistence">
        ///   Persistence service implementation.
        ///   Pass a <c>NullFeaturePersistenceService</c> or mock in unit tests.
        /// </param>
        public FeatureCatalog(IFeaturePersistenceService persistence)
        {
            _persistence = persistence
                ?? throw new ArgumentNullException(nameof(persistence));
        }

        // ── Use-case: Export a FeatureSet to VOTable ─────────────────────────

        /// <summary>
        /// Exports <paramref name="set"/> to a VOTable XML file.
        /// <para>
        /// FeatureCatalog owns the decision of <em>when</em> to export.
        /// <see cref="IFeaturePersistenceService"/> owns <em>how</em> to
        /// serialise and <em>where</em> to write the file.
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
            // XML, AstTool, IAstFrame, IVoTableExporter, or file paths.
            // Those are Infrastructure concerns behind IFeaturePersistenceService.
            return _persistence.ExportToVoTable(set);
        }

        // ── Note on IFeaturePersistenceService.ExportToVoTable ───────────────
        //
        // The concrete persistence service (iDaVIE.Infrastructure.Persistence)
        // receives IVoTableExporter and ICoordinateTransformer at construction:
        //
        //   public sealed class FeaturePersistenceService : IFeaturePersistenceService
        //   {
        //       private readonly IVoTableExporter       _exporter;
        //       private readonly ICoordinateTransformer  _transformer;
        //
        //       public FeaturePersistenceService(
        //           IVoTableExporter exporter,
        //           ICoordinateTransformer transformer)
        //       {
        //           _exporter    = exporter    ?? throw new ArgumentNullException(...);
        //           _transformer = transformer ?? throw new ArgumentNullException(...);
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
        // File I/O stays out of VoTableExportService and out of the domain layer.
        // IAstFrame is passed through FeatureSet.AstFrame; the concrete
        // AstFrameHandle (iDaVIE.Infrastructure.NativePlugins) holds the real IntPtr.
    }
}
