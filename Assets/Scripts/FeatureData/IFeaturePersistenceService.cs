/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * Refactoring proposal — Sub-team 5: Feature System and Domain Model
 *
 * IFeaturePersistenceService.cs
 * Interface boundary between the feature domain (FeatureCatalog) and the
 * Persistence layer (WP7 — Sewe en sestig). The domain calls this interface;
 * WP7 owns the concrete implementation that touches StreamWriter, XDocument,
 * and Application.dataPath. This enforces the Dependency Inversion Principle:
 * the domain depends on an abstraction it defines, not on a concrete I/O class.
 */

using System.Collections.Generic;
using DataFeatures;

namespace iDaVIE.Domain.Feature
{
    /// <summary>
    /// Persistence boundary for the feature domain.
    /// Implemented by WP7 (Persistence and Workspace State).
    /// Called by FeatureCatalog — never directly by domain objects.
    /// </summary>
    public interface IFeaturePersistenceService
    {
        /// <summary>
        /// Appends a single user-created feature to the session ASCII output file.
        /// </summary>
        void AppendFeatureToAscii(Feature feature, string outputFileName);

        /// <summary>
        /// Exports a complete feature set to a VOTable XML file.
        /// Path resolution (output directory, timestamp naming) is the
        /// responsibility of the implementation, not the caller.
        /// </summary>
        /// <returns>The absolute path of the file that was written.</returns>
        string ExportToVoTable(FeatureSet featureSet);

        /// <summary>
        /// Returns true if the session ASCII output file already exists on disk.
        /// Used by FeatureCatalog to decide whether to write a header row.
        /// </summary>
        bool AsciiOutputExists(string outputFileName);
    }
}