// WE1-3 | File tab AFTER skeleton — FitsMetadataHelper
// Pure-static computations extracted from FileTabViewModel to keep its WMC
// within the ≤ 40 orchestrator threshold. No state, no Unity dependency.
namespace iDaVIE.Desktop.FileTab
{
    /// <summary>
    /// Pure-static FITS metadata calculations used by <see cref="FileTabViewModel"/>.
    /// Extracted so FileTabViewModel's method count stays within the orchestrator
    /// WMC threshold (≤ 40). All methods are stateless and have no Unity dependency.
    /// </summary>
    internal static class FitsMetadataHelper
    {
        /// <summary>Returns the pixel maxima for axes 1, 2, and 3 from a FitsFileInfo.</summary>
        internal static (int maxX, int maxY, int maxZ) GetAxisMaxima(FitsFileInfo info)
        {
            long Get(int axis) => info.AxisSizes.TryGetValue(axis, out var v) ? v : 1;
            return ((int)Get(1), (int)Get(2), (int)Get(3));
        }

        /// <summary>
        /// Replaces the inline zScale arithmetic at CanvassDesktop.cs:1028-1039.
        /// Isotropic returns 1; ProportionalZ scales Z by axisZ / max(axisX, axisY).
        /// </summary>
        internal static float ComputeZScale(RatioMode mode, FitsFileInfo? info)
        {
            if (mode == RatioMode.Isotropic || info is null) return 1f;

            long ax(int axis) => info.AxisSizes.TryGetValue(axis, out var v) ? v : 1;
            long xy = Math.Max(ax(1), ax(2));
            if (xy <= 0) return 1f;
            return (float)ax(3) / xy;
        }

        /// <summary>
        /// Pure validation — replaces the axis-comparison logic inside
        /// CanvassDesktop._browseMaskFile. No Unity types.
        /// </summary>
        internal static bool MaskAxesMatchImage(FitsFileInfo image, FitsFileInfo mask) =>
            image.AxisSizes.TryGetValue(1, out var ix) && mask.AxisSizes.TryGetValue(1, out var mx) && ix == mx &&
            image.AxisSizes.TryGetValue(2, out var iy) && mask.AxisSizes.TryGetValue(2, out var my) && iy == my &&
            image.AxisSizes.TryGetValue(3, out var iz) && mask.AxisSizes.TryGetValue(3, out var mz) && iz == mz;
    }
}
