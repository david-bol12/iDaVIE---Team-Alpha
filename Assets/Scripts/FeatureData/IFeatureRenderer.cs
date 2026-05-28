/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * Refactoring proposal — Sub-team 5: Feature System and Domain Model
 *
 * IFeatureRenderer.cs
 * Rendering boundary between the feature domain layer (sub-team 5) and the
 * GPU rendering layer (WP3 — rendering engine sub-team).
 *
 * The domain defines this interface; WP3 provides the concrete implementation
 * (FeatureSetRenderer). This enforces the Dependency Inversion Principle:
 * FeatureVisualiser depends on an abstraction it defines, not on the concrete
 * MonoBehaviour that drives the ComputeBuffer.
 *
 * Same pattern as IFeaturePersistenceService (WP7 boundary) and
 * IFeatureTableLoader (WP2 boundary) — the domain owns the contract,
 * the implementing sub-team owns the body.
 */

using DataFeatures;

namespace iDaVIE.Domain.Feature
{
    /// <summary>
    /// Rendering boundary for a single FeatureSet.
    /// Implemented by FeatureSetRenderer (WP3 — GPU rendering layer).
    /// Called only by FeatureVisualiser — never directly by domain objects.
    /// </summary>
    public interface IFeatureRenderer
    {
        /// <summary>Adds a feature so it is included in the next draw call.</summary>
        void AddFeature(Feature feature);

        /// <summary>Removes a feature from the render list.</summary>
        void RemoveFeature(Feature feature);

        /// <summary>Clears all features from the render list.</summary>
        void ClearFeatures();

        /// <summary>
        /// Marks a feature as needing a vertex-buffer update on the next frame.
        /// Pass -1 to mark all features dirty.
        /// Intended to be subscribed directly to <see cref="FeatureSet.FeatureDirty"/>.
        /// </summary>
        void SetFeatureAsDirty(int index);

        /// <summary>
        /// Display colour for all features in this set.
        /// Uses the domain colour type so this interface has no Unity dependency.
        /// The implementing class is responsible for converting to its internal colour type.
        /// </summary>
        FeatureColor FeatureColor { get; set; }
    }
}
