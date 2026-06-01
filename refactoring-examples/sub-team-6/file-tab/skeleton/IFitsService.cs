// brief §6.6 | File tab AFTER skeleton — IFitsService (ACL boundary)
// Abstracts over FitsReader P/Invoke calls. The adapter (FitsServiceAdapter)
// lives in the Unity assembly and may use [DllImport]; nothing inside this
// interface or the ViewModel may. Satisfies ADR-003 (DI) and ADR-002 (ACL).
// No UnityEngine dependency.
namespace iDaVIE.Desktop.FileTab
{
    // The anti-corruption boundary for FITS file operations. The VM depends on this, not on FitsReader's P/Invoke — so the [DllImport] calls stay in the adapter (FitsServiceAdapter) and the VM stays testable with a fake.
    // Replaces the direct FitsReader.FitsOpenFile / FitsGetHduCount / FitsReadKey / FitsGetImageSize calls scattered through CanvassDesktop._browseImageFile, UpdateHeaderFromFits, IsLoadable, and ChangeHduSelection.
    // HDU index convention: every hduIndex here is 1-based (FITS native), not a zero-based dropdown index. The ViewModel converts at the boundary.
    public interface IFitsService
    {
        // Opens an image file, reads all HDU metadata + the primary header, and returns a FitsFileInfo carrying an IFitsHandle to the still-open file. The handle stays open until disposed; GetHeaderTextAsync reuses it (no reopen).
        Task<FitsFileInfo> OpenImageAsync(string path, CancellationToken ct = default);

        // Opens a mask file and returns its axis metadata + open handle. The VM compares this against the loaded image to check axis compatibility (replaces the _browseMaskFile axis checks).
        Task<FitsFileInfo> OpenMaskAsync(string path, CancellationToken ct = default);

        // Returns the formatted header text for a 1-based HDU index, reusing the open handle. Replaces CanvassDesktop.ChangeHduSelection (line 1435), which reopened the file from disk on every dropdown selection.
        Task<string> GetHeaderTextAsync(IFitsHandle handle, int hduIndex, CancellationToken ct = default);
    }
}
