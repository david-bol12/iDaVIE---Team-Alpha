// WE1-3 | File tab AFTER skeleton — DTOs
// No UnityEngine dependency. Satisfies NFR-MOD-2 and ADR-0001.
namespace iDaVIE.Desktop.FileTab
{
    /// <summary>Immutable descriptor for a single HDU in a FITS file.</summary>
    public sealed record HduInfo(int Index, string Name, string HduType);

    /// <summary>
    /// Plain DTO returned by <see cref="IFitsService.OpenImageAsync"/>.
    /// Carries all FITS metadata the ViewModel needs plus a <see cref="IFitsHandle"/>
    /// to the still-open file pointer so subsequent HDU header reads do not reopen
    /// the file. Replaces the raw IntPtr + scattered FitsReader calls in
    /// CanvassDesktop._browseImageFile / UpdateHeaderFromFits / IsLoadable, and
    /// closes the ChangeHduSelection (line 1435) reopen-per-switch defect.
    ///
    /// Dispose to release the underlying file pointer. The ViewModel disposes
    /// the previous instance before assigning a new one, and on its own Dispose.
    /// </summary>
    public sealed class FitsFileInfo : IDisposable
    {
        public required IFitsHandle Handle { get; init; }
        public required string FilePath { get; init; }
        public required IReadOnlyList<HduInfo> HduList { get; init; }
        public required int NAxis { get; init; }

        /// <summary>Axis number (1-based FITS convention) → size in pixels.</summary>
        public required IReadOnlyDictionary<int, long> AxisSizes { get; init; }

        /// <summary>Pre-formatted FITS header dump for the primary HDU (Information panel).</summary>
        public required string HeaderText { get; init; }

        /// <summary>
        /// Estimated in-memory cube size in bytes (product of NAXIS sizes × sizeof(float)).
        /// Populated by the adapter so the ViewModel can run the RAM-feasibility check
        /// that previously lived in CanvassDesktop.CheckMemSpaceForCubes (lines 995-1013,
        /// Responsibility Group 6).
        /// </summary>
        public long EstimatedBytes { get; init; }

        public void Dispose() => Handle.Dispose();
    }
}
