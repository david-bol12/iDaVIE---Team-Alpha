/*
 * REFACTORING EXAMPLE 2 — VOTable Export
 * Sub-team 5: Feature System and Domain Model
 *
 * VoTableExportService.cs — AFTER STATE (new file, design-level example)
 * =======================================================================
 * Design role: concrete implementation of IVoTableExporter.
 *
 * KEY CONSTRAINT
 * ──────────────
 * This class contains ZERO UnityEngine imports.
 * It operates entirely on the plain DataFeatures domain model (FeatureSet,
 * Feature, ICoordinateTransformer) and System.Xml.Linq.
 * This makes it unit-testable without a running Unity instance.
 *
 * WHAT CHANGED FROM VoTableSaver
 * ────────────────────────────────
 * Before                              After
 * ──────────────────────────────────  ───────────────────────────────────
 * static class, not injectable        implements IVoTableExporter
 * takes FeatureSetRenderer (Unity)    takes FeatureSet (plain domain obj)
 * calls AstTool.* directly (DLL)      calls ICoordinateTransformer (injected)
 * reads VolumeRenderer.SourceStats    reads FeatureSet.Features (domain list)
 * writes to file (doc.Save)           returns XML string (caller writes file)
 * lives in VoTableReader namespace    lives in DataFeatures namespace
 *
 * CK METRICS (target)
 * ───────────────────
 * WMC  ≤  6   (Export + 3–4 private helpers)
 * CBO  ≤  3   (FeatureSet, Feature, ICoordinateTransformer)
 * RFC  ≤  8
 * LCOM =  0   (single method drives all helpers)
 */

using System;
using System.Collections.Generic;
using System.Xml.Linq;

// NOTE: No "using UnityEngine" — this class is intentionally Unity-free.

namespace DataFeatures
{
    /// <summary>
    /// Pure-C# VOTable 1.3 XML serialiser.
    /// <para>
    /// Implements <see cref="IVoTableExporter"/> and depends only on
    /// <see cref="ICoordinateTransformer"/> for WCS conversion.
    /// No Unity types are referenced — the class is fully unit-testable.
    /// </para>
    /// <para>
    /// Injected into <see cref="IFeaturePersistenceService"/> at the
    /// composition root. The persistence service calls
    /// <see cref="Export"/> and writes the returned string to disk.
    /// </para>
    /// </summary>
    public sealed class VoTableExportService : IVoTableExporter
    {
        // ── IVoTableExporter ─────────────────────────────────────────────────

        /// <inheritdoc/>
        public string Export(FeatureSet featureSet, ICoordinateTransformer transformer)
        {
            if (featureSet    == null) throw new ArgumentNullException(nameof(featureSet));
            if (transformer   == null) throw new ArgumentNullException(nameof(transformer));

            // ── 1. Build column headers ──────────────────────────────────────
            var headers = BuildHeaders(featureSet);

            // ── 2. Build VOTABLE skeleton ────────────────────────────────────
            XDocument doc = BuildDocument(headers, featureSet);

            // ── 3. Populate data rows ────────────────────────────────────────
            XElement tableData = doc.Root
                                    .Element("RESOURCE")
                                    .Element("TABLE")
                                    .Element("DATA")
                                    .Element("TABLEDATA");

            foreach (Feature feature in featureSet.Features)
            {
                // Delegate coordinate conversion — no AstTool call here.
                transformer.Transform(
                    featureSet.AstFrame,
                    feature.Center.X, feature.Center.Y, feature.Center.Z,
                    out double ra, out double dec, out double zPhys);

                transformer.Normalise(
                    featureSet.AstFrame,
                    ra, dec, zPhys,
                    out double normRa, out double normDec, out double normZ);

                tableData.Add(BuildRow(feature, normRa, normDec, normZ));
            }

            // ── 4. Return XML string — the caller writes it to disk ──────────
            return doc.ToString();
        }

        // ── Private helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Returns the ordered list of column names for the FIELD elements.
        /// Fixed positional columns are followed by the set's RawData keys.
        /// </summary>
        private static List<string> BuildHeaders(FeatureSet featureSet)
        {
            var headers = new List<string>
            {
                "id", "x", "y", "z",
                "x_min", "x_max", "y_min", "y_max", "z_min", "z_max",
                "ra", "dec", featureSet.ZAxisLabel,
                "Flag (" + DateTime.Now.ToString("dd/MM/yy HH:mm") + ")"
            };

            if (featureSet.RawDataKeys != null)
                headers.AddRange(featureSet.RawDataKeys);

            return headers;
        }

        /// <summary>
        /// Builds the VOTABLE / RESOURCE / TABLE structure with FIELD declarations
        /// but an empty TABLEDATA element ready for row insertion.
        /// </summary>
        private static XDocument BuildDocument(List<string> headers, FeatureSet featureSet)
        {
            // Number of positional columns before the RawData extras.
            int fixedColumnCount = headers.Count
                                   - (featureSet.RawDataKeys?.Length ?? 0);

            var fieldElements = new XElement[headers.Count];
            for (int i = 0; i < headers.Count; i++)
            {
                fieldElements[i] = i < fixedColumnCount
                    ? new XElement("FIELD",
                        new XAttribute("datatype", "float"),
                        new XAttribute("name", headers[i]))
                    : new XElement("FIELD",
                        new XAttribute("arraysize", "30"),
                        new XAttribute("datatype", "char"),
                        new XAttribute("name", headers[i]));
            }

            var tableElement = new XElement("TABLE",
                new XAttribute("ID",   "idavie_cat"),
                new XAttribute("name", "idavie_cat"),
                new XElement("DATA", new XElement("TABLEDATA")));

            // FIELD elements precede DATA — insert at front.
            tableElement.AddFirst(fieldElements);

            return new XDocument(
                new XElement("VOTABLE",
                    new XElement("RESOURCE", new XAttribute("name", "iDaVIE catalogue"),
                        new XElement("DESCRIPTION", "Source data exported from iDaVIE"),
                        new XElement("COOSYS", new XAttribute("ID", "J2000")),
                        tableElement)));
        }

        /// <summary>
        /// Builds a single TR element from the domain <see cref="Feature"/>
        /// and pre-computed normalised coordinates.
        /// </summary>
        private static XElement BuildRow(
            Feature feature,
            double normRa, double normDec, double normZ)
        {
            // Convert radians to degrees for RA and Dec (standard VOTable convention).
            double raDeg  = 180.0 * normRa  / Math.PI;
            double decDeg = 180.0 * normDec / Math.PI;
            // Convert normalised Z to km/s (multiply by 1000, as in original code).
            double zKms   = 1000.0 * normZ;

            var row = new XElement("TR",
                new XElement("TD", (feature.Id + 1).ToString()),
                new XElement("TD", feature.Center.X.ToString()),
                new XElement("TD", feature.Center.Y.ToString()),
                new XElement("TD", feature.Center.Z.ToString()),
                new XElement("TD", feature.CornerMin.X.ToString()),
                new XElement("TD", feature.CornerMax.X.ToString()),
                new XElement("TD", feature.CornerMin.Y.ToString()),
                new XElement("TD", feature.CornerMax.Y.ToString()),
                new XElement("TD", feature.CornerMin.Z.ToString()),
                new XElement("TD", feature.CornerMax.Z.ToString()),
                new XElement("TD", raDeg.ToString()),
                new XElement("TD", decDeg.ToString()),
                new XElement("TD", zKms.ToString()),
                new XElement("TD", feature.Flag));

            if (feature.RawData != null)
            {
                foreach (string value in feature.RawData)
                    row.Add(new XElement("TD", value));
            }

            return row;
        }
    }
}
