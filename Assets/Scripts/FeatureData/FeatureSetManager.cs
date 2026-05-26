/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 *
 * Additional information and disclaimers regarding liability and third-party
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */

// ── Phase 6 Safety-Net Facade ─────────────────────────────────────────────────
// FeatureSetManager is now a thin delegation shell.  Every public member routes
// to FeatureVisualiser / FeatureSetService / FeatureCatalog.
// Kept alive only so FeatureMenuCell.cs (and any other missed callers) continue
// to compile during the Phase 7 final audit.
// DO NOT add logic here — fix callers and migrate them to FeatureVisualiser.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using VolumeData;

namespace DataFeatures
{
    public enum FeatureSetType
    {
        Unassigned,
        Mask,
        New,
        Imported,
        Selection
    }

    /// <summary>
    /// FACADE — delegates to <see cref="FeatureVisualiser"/>.
    /// See Phase 6 comment at the top of this file.
    /// </summary>
    public class FeatureSetManager : MonoBehaviour
    {
        // ── Local flags (FeatureMenuController no longer polls these from here;
        //    kept so FeatureMenuCell.cs still compiles) ─────────────────────────
        public bool NeedToRespawnMenuList { get; set; }
        public bool NeedToUpdateInfo { get; set; }

        private FeatureVisualiser _visualiser;

        private void Awake()
        {
            var vdr = GetComponentInParent<VolumeDataSetRenderer>();
            if (vdr != null)
                _visualiser = vdr.FeatureVisualiser;
            if (_visualiser == null)
                Debug.LogWarning("[FeatureSetManager facade] Could not locate FeatureVisualiser — " +
                                 "migrate remaining callers to FeatureVisualiser then delete this class.");
        }

        // ── Feature selection ─────────────────────────────────────────────────

        /// <summary>The currently selected feature. Delegates to <see cref="FeatureSetService"/>.</summary>
        public Feature SelectedFeature => _visualiser?.Service?.SelectedFeature;

        /// <summary>Selects <paramref name="feature"/>. Delegates to <see cref="FeatureVisualiser"/>.</summary>
        public bool SelectFeature(Feature feature)
        {
            _visualiser?.SelectFeature(feature);
            return feature != null;
        }

        /// <summary>Deselects the current feature. Delegates to <see cref="FeatureVisualiser"/>.</summary>
        public void DeselectFeature() => _visualiser?.DeselectFeature();

        /// <summary>Deselects and clears the selection. Delegates to <see cref="FeatureVisualiser"/>.</summary>
        public void SelectNullFeature() => _visualiser?.DeselectFeature();

        // ── User set management ───────────────────────────────────────────────

        /// <summary>
        /// Adds <paramref name="feature"/> to the first user (New) set renderer.
        /// Delegates through FeatureVisualiser's child renderers.
        /// </summary>
        public void AddFeatureToNewSet(Feature feature, bool needMenuRespawn)
        {
            if (_visualiser == null) return;

            // Find the first New-type renderer owned by FeatureVisualiser.
            FeatureSetRenderer newSet = null;
            foreach (var r in _visualiser.GetComponentsInChildren<FeatureSetRenderer>())
            {
                if (r.FeatureSetType == FeatureSetType.New) { newSet = r; break; }
            }

            if (newSet == null)
            {
                // No New set exists yet — fall back to the service which creates one.
                _visualiser.Service?.AddSelectedFeatureToUserSet();
                return;
            }

            var duplicate = new Feature(
                feature.CornerMin, feature.CornerMax,
                newSet.FeatureColor, feature.Name, feature.Flag,
                newSet.FeatureList.Count, feature.Id,
                new string[] { "" }, feature.Visible) { Temporary = false };

            newSet.AddFeature(duplicate);
            newSet.FeatureMenuScrollerDataSource?.InitData();
            NeedToRespawnMenuList = needMenuRespawn;
        }
    }
}
