/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * Refactoring proposal — Sub-team 5: Feature System and Domain Model
 *
 * FeatureSetService.cs
 * Application-layer service: orchestrates use-cases that cross FeatureSet
 * and Feature boundaries.
 *
 * BEFORE (these lived scattered across FeatureSetManager):
 *   • ImportFeatureSetFromTable — mixed catalogue loading with rendering setup
 *   • CreateSelectionFeature   — created Feature + manipulated Unity GameObject
 *   • SelectFeature / DeselectFeature — modified domain state + spawned anchors
 *   • AddFeatureToNewSet       — use-case logic + GUI refresh calls
 *   • AppendFeatureToFile      — directly called StreamWriter
 *
 * AFTER (FeatureSetService responsibilities):
 *   • Orchestrates all use-cases purely in terms of domain objects
 *   • Delegates persistence to FeatureCatalog (which delegates to WP7)
 *   • Delegates import/loading to IFeatureTableLoader (WP2 owns the impl.)
 *   • Raises domain events; FeatureVisualiser and GUI react via those events
 *   • Zero Unity types — pure C#, fully unit-testable with mocks
 *
 * Dependencies (constructor-injected — testable, invertible):
 *   FeatureCatalog          — registry + persistence gateway
 *   IFeatureTableLoader     — data I/O boundary (WP2)
 *   IFeatureStatisticsProvider — statistics boundary (PluginInterface/DataAnalysis)
 *
 * CK metric targets (post-refactor projection):
 *   WMC  ≤ 18
 *   CBO  ≤ 10
 *   LCOM ≤ 0.4
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using DataFeatures;
using iDaVIE.Domain.Feature;

namespace iDaVIE.Application.Feature
{
    // ── Boundary interfaces (defined here; implemented by other WPs) ──────────

    /// <summary>
    /// Loads a FeatureTable from a file path.
    /// Implemented by WP2 (Data I/O). Injected into FeatureSetService.
    /// </summary>
    public interface IFeatureTableLoader
    {
        /// <summary>Returns null and populates <paramref name="error"/> on failure.</summary>
        FeatureTable Load(string filePath, out string error);
    }

    /// <summary>
    /// Provides real-time statistics for a feature derived from the volume mask.
    /// Implemented against DataAnalysis.SourceStats (PluginInterface layer).
    /// </summary>
    public interface IFeatureStatisticsProvider
    {
        FeatureStatistics GetStatistics(Feature feature);
    }

    /// <summary>
    /// Value object carrying the statistics for one Feature.
    /// Kept separate from Feature so the domain object stays a plain aggregate.
    /// </summary>
    public sealed class FeatureStatistics
    {
        public long   VoxelCount            { get; init; }
        public double TotalFlux             { get; init; }
        public double PeakFlux              { get; init; }
        public double CentroidX             { get; init; }
        public double CentroidY             { get; init; }
        public double CentroidZ             { get; init; }
        public double W20                   { get; init; }
        public double W50                   { get; init; }

        /// <summary>Invariant check: centroid must lie inside the bounding box.</summary>
        public bool CentroidInsideBounds(Feature f) =>
            CentroidX >= f.CornerMin.X && CentroidX <= f.CornerMax.X &&
            CentroidY >= f.CornerMin.Y && CentroidY <= f.CornerMax.Y &&
            CentroidZ >= f.CornerMin.Z && CentroidZ <= f.CornerMax.Z;
    }

    // ── FeatureSetService ─────────────────────────────────────────────────────

    /// <summary>
    /// Orchestrates feature domain use-cases.
    /// Pure C# — no Unity dependency.
    /// </summary>
    public sealed class FeatureSetService
    {
        private readonly FeatureCatalog              _catalog;
        private readonly IFeatureTableLoader         _loader;
        private readonly IFeatureStatisticsProvider  _statistics;

        // ── Selection state ───────────────────────────────────────────────────
        private Feature _selectedFeature;

        /// <summary>The currently selected feature, or null.</summary>
        public Feature SelectedFeature
        {
            get => _selectedFeature;
            private set
            {
                var previous = _selectedFeature;
                _selectedFeature = value;
                FeatureSelectionChanged?.Invoke(previous, value);
            }
        }

        // ── Domain events ─────────────────────────────────────────────────────

        /// <summary>
        /// Raised when the selection changes.
        /// Args: (previousFeature, newFeature) — either may be null.
        /// FeatureVisualiser subscribes to spawn/hide anchor handles.
        /// </summary>
        public event Action<Feature, Feature> FeatureSelectionChanged;

        /// <summary>Raised when a mask-derived feature is selected (triggers stats panel update).</summary>
        public event Action MaskFeatureSelected;

        /// <summary>Raised after a FeatureSet is fully imported and populated.</summary>
        public event Action<FeatureSet> FeatureSetImported;

        // ── Constructor ───────────────────────────────────────────────────────
        public FeatureSetService(
            FeatureCatalog             catalog,
            IFeatureTableLoader        loader,
            IFeatureStatisticsProvider statistics)
        {
            _catalog    = catalog    ?? throw new ArgumentNullException(nameof(catalog));
            _loader     = loader     ?? throw new ArgumentNullException(nameof(loader));
            _statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
        }

        // ── Use-case: import from file ────────────────────────────────────────

        /// <summary>
        /// Loads a VOTable or FITS catalogue from <paramref name="filePath"/>,
        /// applies the column mapping, and registers the result as an imported set.
        /// </summary>
        /// <param name="filePath">Absolute path to the source file.</param>
        /// <param name="name">Display name for the new FeatureSet.</param>
        /// <param name="mapping">Column-to-axis mapping (from FeatureMapper).</param>
        /// <param name="columnsMask">Which data columns to include.</param>
        /// <param name="excludeExternal">If true, features outside the volume bounds are dropped.</param>
        /// <param name="volumeMin">Lower-left-back corner of the volume in voxel space. Required when <paramref name="excludeExternal"/> is true.</param>
        /// <param name="volumeMax">Upper-right-front corner of the volume in voxel space. Required when <paramref name="excludeExternal"/> is true.</param>
        /// <returns>The new FeatureSet, or null on load failure.</returns>
        public FeatureSet ImportFromFile(
            string filePath,
            string name,
            Dictionary<SourceMappingOptions, string> mapping,
            bool[] columnsMask,
            bool excludeExternal,
            Vec3 volumeMin = default,
            Vec3 volumeMax = default)
        {
            var table = _loader.Load(filePath, out string error);
            if (table == null)
            {
                // FeatureSetService does not know about Unity Debug.Log.
                // The caller (FeatureVisualiser or GUI) is responsible for
                // surfacing the error message to the user.
                LastImportError = error;
                return null;
            }

            var set = _catalog.CreateImportedSet(name);
            set.FileName = filePath;

            PopulateFromTable(set, mapping, table, columnsMask, excludeExternal, volumeMin, volumeMax);

            FeatureSetImported?.Invoke(set);
            return set;
        }

        /// <summary>Last error message from a failed import (for GUI display).</summary>
        public string LastImportError { get; private set; }

        // ── Use-case: create selection feature ───────────────────────────────

        /// <summary>
        /// Creates (or replaces) the transient selection feature.
        /// The selection feature is always temporary — it disappears on deselect.
        /// </summary>
        /// <param name="boundsMin">Lower-left-back corner in voxel space.</param>
        /// <param name="boundsMax">Upper-right-front corner in voxel space.</param>
        public Feature CreateSelectionFeature(Vec3 boundsMin, Vec3 boundsMax)
        {
            var selectionSet = _catalog.EnsureSelectionSet();
            selectionSet.Clear();

            Deselect();

            var selectionFeature = new Feature(
                cornerMin:  boundsMin,
                cornerMax:  boundsMax,
                color:      FeatureColor.White,
                name:       "selection",
                flag:       string.Empty,
                index:      -1,
                id:         -1,
                rawData:    new[] { string.Empty },
                visible:    true)
            {
                Temporary = true
            };

            selectionSet.Add(selectionFeature);
            Select(selectionFeature);
            return selectionFeature;
        }

        // ── Use-case: selection ───────────────────────────────────────────────

        /// <summary>
        /// Selects the smallest feature whose bounding box contains
        /// <paramref name="cursorPosition"/> (in volume/voxel space).
        /// Returns true if a feature was found.
        /// </summary>
        public bool SelectAtPosition(Vec3 cursorPosition)
        {
            Deselect();

            Feature best = null;
            float   bestVolume = float.MaxValue;

            foreach (var set in _catalog.AllSets())
            {
                foreach (var feature in set.Features)
                {
                    if (!feature.Visible) continue;
                    if (!feature.BoundsContains(cursorPosition)) continue;

                    float vol = feature.BoundsVolume();
                    if (vol < bestVolume ||
                        (Math.Abs(vol - bestVolume) < 1e-6f && set.SetType != FeatureSetType.Selection))
                    {
                        best        = feature;
                        bestVolume  = vol;
                    }
                }
            }

            if (best == null) return false;
            Select(best);
            return true;
        }

        /// <summary>Selects a specific feature directly (e.g. via UI list tap).</summary>
        public void Select(Feature feature)
        {
            if (feature == null) throw new ArgumentNullException(nameof(feature));
            Deselect();
            feature.Selected = true;
            SelectedFeature  = feature;

            if (feature.FeatureSetParent?.SetType == FeatureSetType.Mask)
                MaskFeatureSelected?.Invoke();
        }

        /// <summary>Deselects the current feature. If it was temporary, hides it.</summary>
        public void Deselect()
        {
            if (SelectedFeature == null) return;
            SelectedFeature.Selected = false;
            if (SelectedFeature.Temporary)
                SelectedFeature.Visible = false;
            SelectedFeature = null;
        }

        // ── Use-case: add selected feature to user set ────────────────────────

        /// <summary>
        /// Duplicates the currently selected feature into the first user-created set
        /// (creating that set if none exists yet) and persists it to the session file.
        /// Returns false if nothing is selected.
        /// </summary>
        public bool AddSelectedFeatureToUserSet()
        {
            if (SelectedFeature == null) return false;

            var userSets = _catalog.UserSets;
            if (userSets.Count == 0) _catalog.CreateUserSet();

            var targetSet = _catalog.UserSets[0];

            // Duplicate — features are value-like in the domain model
            var duplicate = new Feature(
                cornerMin: SelectedFeature.CornerMin,
                cornerMax: SelectedFeature.CornerMax,
                color:     targetSet.Color,
                name:      SelectedFeature.Name,
                flag:      SelectedFeature.Flag,
                index:     targetSet.Features.Count,
                id:        SelectedFeature.Id,
                rawData:   new[] { string.Empty },
                visible:   SelectedFeature.Visible)
            {
                Temporary = false,
                Comment   = SelectedFeature.Comment,
                Metric    = SelectedFeature.Metric
            };

            targetSet.Add(duplicate);
            _catalog.AppendFeatureToSessionFile(duplicate);
            return true;
        }

        // ── Use-case: export ──────────────────────────────────────────────────

        /// <summary>
        /// Exports a FeatureSet to VOTable XML.
        /// Returns the written file path for display in the GUI.
        /// </summary>
        public string ExportToVoTable(FeatureSet set)
        {
            return _catalog.ExportToVoTable(set);
        }

        // ── Use-case: real-time statistics ────────────────────────────────────

        /// <summary>
        /// Returns up-to-date statistics for the given feature.
        /// Delegates to IFeatureStatisticsProvider, which wraps DataAnalysis.
        /// Pure pass-through here; the service is the correct place to add
        /// caching or throttling if statistics become expensive.
        /// </summary>
        public FeatureStatistics GetStatistics(Feature feature)
        {
            if (feature == null) throw new ArgumentNullException(nameof(feature));
            return _statistics.GetStatistics(feature);
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        private static void PopulateFromTable(
            FeatureSet                               set,
            Dictionary<SourceMappingOptions, string> mapping,
            FeatureTable                             table,
            bool[]                                   columnsMask,
            bool                                     excludeExternal,
            Vec3                                     volumeMin = default,
            Vec3                                     volumeMax = default)
        {
            if (table.Rows.Count == 0 || table.Column.Count == 0)
                return;

            // Build column name index and raw-data key list from the mask.
            var colNames         = new string[table.Column.Count];
            var rawDataKeysList  = new List<string>();
            for (int i = 0; i < table.Column.Count; i++)
            {
                colNames[i] = table.Column[i].Name;
                if (columnsMask[i])
                    rawDataKeysList.Add(colNames[i]);
            }
            set.RawDataKeys  = rawDataKeysList.ToArray();
            set.RawDataTypes = new string[rawDataKeysList.Count];
            for (int i = 0; i < set.RawDataTypes.Length; i++)
                set.RawDataTypes[i] = "string";

            var keys           = mapping.Keys;
            bool containsBoxes = keys.Contains(SourceMappingOptions.Xmin);
            bool containsXYZ   = keys.Contains(SourceMappingOptions.X);

            // Sky-coordinate transforms (Ra/Dec + Velo/Freq/Redshift) require AstTool,
            // which is a Unity/native dependency outside the domain layer. The IFeatureTableLoader
            // implementation (WP2) is responsible for converting sky coords to pixel coords
            // before returning the FeatureTable, so by the time we get here the mapping
            // should contain X/Y/Z or Xmin/Xmax columns.
            if (!containsBoxes && !containsXYZ)
                return;

            int nameIndex = keys.Contains(SourceMappingOptions.ID)
                ? Array.IndexOf(colNames, mapping[SourceMappingOptions.ID])
                : -1;
            int flagIndex = keys.Contains(SourceMappingOptions.Flag)
                ? Array.IndexOf(colNames, mapping[SourceMappingOptions.Flag])
                : -1;

            int[] posIndices = { -1, -1, -1 };
            if (containsXYZ)
            {
                posIndices[0] = Array.IndexOf(colNames, mapping[SourceMappingOptions.X]);
                posIndices[1] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Y]);
                posIndices[2] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Z]);
            }

            int[] boxIndices = { -1, -1, -1, -1, -1, -1 };
            if (containsBoxes)
            {
                boxIndices[0] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Xmin]);
                boxIndices[1] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Xmax]);
                boxIndices[2] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Ymin]);
                boxIndices[3] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Ymax]);
                boxIndices[4] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Zmin]);
                boxIndices[5] = Array.IndexOf(colNames, mapping[SourceMappingOptions.Zmax]);
            }

            for (int row = 0; row < table.Rows.Count; row++)
            {
                var rawData = new List<string>();
                for (int i = 0; i < table.Column.Count; i++)
                {
                    if (columnsMask[i])
                        rawData.Add(table.Rows[row].ColumnData[i]?.ToString() ?? string.Empty);
                }

                string name = nameIndex >= 0
                    ? (string)table.Rows[row].ColumnData[nameIndex]
                    : $"Source #{row + 1}";

                string flag = flagIndex >= 0
                    ? (string)table.Rows[row].ColumnData[flagIndex]
                    : string.Empty;

                Vec3 cornerMin, cornerMax;
                if (containsBoxes)
                {
                    float xMin = ParseColFloat(table.Rows[row].ColumnData[boxIndices[0]]);
                    float xMax = ParseColFloat(table.Rows[row].ColumnData[boxIndices[1]]);
                    float yMin = ParseColFloat(table.Rows[row].ColumnData[boxIndices[2]]);
                    float yMax = ParseColFloat(table.Rows[row].ColumnData[boxIndices[3]]);
                    float zMin = ParseColFloat(table.Rows[row].ColumnData[boxIndices[4]]);
                    float zMax = ParseColFloat(table.Rows[row].ColumnData[boxIndices[5]]);
                    cornerMin = new Vec3(xMin, yMin, zMin);
                    cornerMax = new Vec3(xMax, yMax, zMax);
                }
                else
                {
                    // Point coordinate — expand to a 1-voxel box (mirrors SpawnFeaturesFromTable).
                    float x = ParseColFloat(table.Rows[row].ColumnData[posIndices[0]]);
                    float y = ParseColFloat(table.Rows[row].ColumnData[posIndices[1]]);
                    float z = ParseColFloat(table.Rows[row].ColumnData[posIndices[2]]);
                    cornerMin = new Vec3(x - 1f, y - 1f, z - 1f);
                    cornerMax = new Vec3(x + 1f, y + 1f, z + 1f);
                }

                var feature = new Feature(
                    cornerMin: cornerMin,
                    cornerMax: cornerMax,
                    color:     set.Color,
                    name:      name,
                    flag:      flag,
                    index:     set.Features.Count,
                    id:        row,
                    rawData:   rawData.ToArray(),
                    visible:   false);

                if (excludeExternal && feature.IsOutsideVolume(volumeMin, volumeMax))
                    continue;

                set.Add(feature);
            }
        }

        private static float ParseColFloat(object value)
        {
            if (value == null) return 0f;
            return float.TryParse(value.ToString(), NumberStyles.Any,
                CultureInfo.InvariantCulture, out float result) ? result : 0f;
        }
    }

}