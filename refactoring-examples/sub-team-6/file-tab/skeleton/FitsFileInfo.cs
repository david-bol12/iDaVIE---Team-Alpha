// brief §6.6 | File tab AFTER skeleton — DTOs
// No UnityEngine dependency. Satisfies NFR-MOD-2 and ADR-009 (MVVM).
namespace iDaVIE.Desktop.FileTab
{
    // Immutable descriptor for a single HDU (Header Data Unit) in a FITS file: its 1-based index, name, and type.
    public sealed record HduInfo(int Index, string Name, string HduType);

    // Plain data carrier returned by IFitsService.OpenImageAsync/OpenMaskAsync — everything the VM needs to know about a file, gathered in one read.
    // It also holds an IFitsHandle to the still-open file, so later HDU-header reads reuse it instead of reopening from disk. Replaces the raw IntPtr + scattered FitsReader calls in CanvassDesktop._browseImageFile / UpdateHeaderFromFits / IsLoadable, and closes the ChangeHduSelection (line 1435) reopen-per-switch defect.
    // IDisposable because of that open handle: the VM disposes the previous instance before assigning a new one, and again on its own Dispose.
    public sealed class FitsFileInfo : IDisposable
    {
        // The open file pointer this info was read from; disposed to close the file.
        public required IFitsHandle Handle { get; init; }
        // Absolute path the file was opened from.
        public required string FilePath { get; init; }
        // Every HDU in the file (drives the HDU dropdown).
        public required IReadOnlyList<HduInfo> HduList { get; init; }
        // Number of axes in the primary HDU (a cube is 3+).
        public required int NAxis { get; init; }

        // Axis number (1-based FITS convention) → size in pixels.
        public required IReadOnlyDictionary<int, long> AxisSizes { get; init; }

        // Pre-formatted FITS header dump for the primary HDU (shown in the Information panel).
        public required string HeaderText { get; init; }

        // Estimated in-memory cube size in bytes (product of NAXIS sizes × sizeof(float)). Filled by the adapter so the VM can run the RAM-feasibility check that lived in CanvassDesktop.CheckMemSpaceForCubes (lines 995-1013, Responsibility Group 6).
        public long EstimatedBytes { get; init; }

        // Closing the info closes the underlying file.
        public void Dispose() => Handle.Dispose();
    }
}
