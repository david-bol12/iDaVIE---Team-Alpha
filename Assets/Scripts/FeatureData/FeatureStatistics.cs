/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * Refactoring proposal — Sub-team 5: Feature System and Domain Model
 *
 * FeatureStatistics.cs
 * Value object carrying per-feature statistics derived from the volume mask,
 * together with the provider interface that computes them.
 *
 * Extracted from FeatureSetService.cs so the statistics types can be tested
 * independently without pulling in FeatureSetService's full dependency tree.
 */

namespace iDaVIE.Application.Feature
{
    /// <summary>
    /// Provides real-time statistics for a feature derived from the volume mask.
    /// Implemented against DataAnalysis.SourceStats (PluginInterface layer).
    /// </summary>
    public interface IFeatureStatisticsProvider
    {
        FeatureStatistics GetStatistics(DataFeatures.Feature feature);
    }

    /// <summary>
    /// Immutable value object carrying the statistics for one feature.
    /// Kept separate from <see cref="DataFeatures.Feature"/> so the domain aggregate
    /// stays a plain bounding-box container with no dependency on the native DataAnalysis DLL.
    /// </summary>
    public sealed class FeatureStatistics
    {
        public long   VoxelCount { get; init; }
        public double TotalFlux  { get; init; }
        public double PeakFlux   { get; init; }
        public double CentroidX  { get; init; }
        public double CentroidY  { get; init; }
        public double CentroidZ  { get; init; }
        public double W20        { get; init; }
        public double W50        { get; init; }

        /// <summary>
        /// Invariant: the flux-weighted centroid must lie inside the feature's bounding box.
        /// A centroid outside the box indicates a statistics/bounds mismatch.
        /// </summary>
        public bool CentroidInsideBounds(DataFeatures.Feature f) =>
            CentroidX >= f.CornerMin.X && CentroidX <= f.CornerMax.X &&
            CentroidY >= f.CornerMin.Y && CentroidY <= f.CornerMax.Y &&
            CentroidZ >= f.CornerMin.Z && CentroidZ <= f.CornerMax.Z;

        /// <summary>
        /// Invariant: observed flux values are physically non-negative.
        /// Negative flux indicates a sign error in the DataAnalysis binding.
        /// </summary>
        public bool FluxIsNonNegative() => TotalFlux >= 0.0 && PeakFlux >= 0.0;

        /// <summary>
        /// Invariant: the line width at 20% of peak (W20) is at least as wide
        /// as the line width at 50% of peak (W50), because a broader threshold
        /// captures more of the spectral line.
        /// </summary>
        public bool W20GeqW50() => W20 >= W50;
    }
}
