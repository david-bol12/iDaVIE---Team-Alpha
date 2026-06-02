// brief §6.6 | File tab AFTER skeleton — FitsMetadataHelper
// Pure-static computations extracted from FileTabViewModel to keep its WMC
// within the ≤ 40 orchestrator threshold. No state, no Unity dependency.
namespace iDaVIE.Desktop.FileTab
{
    // A bag of stateless FITS-metadata sums that FileTabViewModel would otherwise carry itself. Pulling them out keeps the VM's method count under the orchestrator WMC threshold (≤ 40). No state, no Unity — just inputs in, numbers out, so each is trivially unit-testable on its own.
    internal static class FitsMetadataHelper
    {
        // Pulls the pixel sizes of axes 1/2/3 (X/Y/Z) out of a FitsFileInfo, defaulting any missing axis to 1.
        internal static (int maxX, int maxY, int maxZ) GetAxisMaxima(FitsFileInfo info)
        {
            long Get(int axis) => info.AxisSizes.TryGetValue(axis, out var v) ? v : 1;
            return ((int)Get(1), (int)Get(2), (int)Get(3));
        }

        // Works out how much to stretch the Z axis so the cube renders with the chosen aspect ratio. Replaces the inline zScale arithmetic at CanvassDesktop.cs:1028-1039.
        // Isotropic (or no file) returns 1 — no stretch; ProportionalZ scales Z by axisZ / max(axisX, axisY).
        internal static float ComputeZScale(RatioMode mode, FitsFileInfo? info)
        {
            if (mode == RatioMode.Isotropic || info is null) return 1f;

            long ax(int axis) => info.AxisSizes.TryGetValue(axis, out var v) ? v : 1;
            long xy = Math.Max(ax(1), ax(2));
            if (xy <= 0) return 1f;
            return (float)ax(3) / xy;
        }

        // True when a mask's first three axes are the same size as the image's — i.e. the mask actually fits the cube. Replaces the axis-comparison logic inside CanvassDesktop._browseMaskFile.
        // There are no Unity types.
        internal static bool MaskAxesMatchImage(FitsFileInfo image, FitsFileInfo mask) =>
            image.AxisSizes.TryGetValue(1, out var ix) && mask.AxisSizes.TryGetValue(1, out var mx) && ix == mx &&
            image.AxisSizes.TryGetValue(2, out var iy) && mask.AxisSizes.TryGetValue(2, out var my) && iy == my &&
            image.AxisSizes.TryGetValue(3, out var iz) && mask.AxisSizes.TryGetValue(3, out var mz) && iz == mz;
    }
}
