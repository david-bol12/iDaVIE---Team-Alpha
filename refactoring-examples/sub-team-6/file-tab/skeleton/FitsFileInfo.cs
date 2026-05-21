// WE1-3 | File tab AFTER skeleton — DTOs
// No UnityEngine dependency. Satisfies NFR-MOD-2 and ADR-0001.
namespace iDaVIE.Desktop.FileTab
{
    /// <summary>Immutable descriptor for a single HDU in a FITS file.</summary>
    public sealed record HduInfo(int Index, string Name, string HduType);

    /// <summary>
    /// Plain DTO returned by <see cref="IFitsService.OpenImageAsync"/>.
    /// Carries all FITS metadata the ViewModel needs — no native FitsReader pointers escape
    /// the adapter boundary. Replaces the raw IntPtr + scattered FitsReader calls in
    /// CanvassDesktop._browseImageFile / UpdateHeaderFromFits / IsLoadable.
    /// </summary>
    public sealed class FitsFileInfo
    {
        public required string FilePath { get; init; }
        public required IReadOnlyList<HduInfo> HduList { get; init; }
        public required int NAxis { get; init; }

        /// <summary>Axis number (1-based FITS convention) → size in pixels.</summary>
        public required IReadOnlyDictionary<int, long> AxisSizes { get; init; }

        /// <summary>Pre-formatted FITS header dump for the Information panel.</summary>
        public required string HeaderText { get; init; }
    }
}
