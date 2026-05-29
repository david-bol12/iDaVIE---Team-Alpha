// WE1-3 | File tab AFTER skeleton — IFitsService (ACL boundary)
// Abstracts over FitsReader P/Invoke calls. The adapter (FitsServiceAdapter)
// lives in the Unity assembly and may use [DllImport]; nothing inside this
// interface or the ViewModel may. Satisfies ADR-0001 (DIP) and ADR-0003 (ACL).
// No UnityEngine dependency.
namespace iDaVIE.Desktop.FileTab
{
    /// <summary>
    /// Domain interface for FITS file operations.
    /// Replaces direct FitsReader.FitsOpenFile / FitsGetHduCount / FitsReadKey /
    /// FitsGetImageSize calls that were scattered inside CanvassDesktop._browseImageFile,
    /// UpdateHeaderFromFits, IsLoadable, and ChangeHduSelection.
    ///
    /// HDU index convention: all hduIndex parameters are <b>1-based</b> (FITS native),
    /// not zero-based dropdown indices. The ViewModel converts at the boundary.
    /// </summary>
    public interface IFitsService
    {
        /// <summary>
        /// Opens a FITS image file, reads all HDU metadata and the primary header,
        /// and returns a <see cref="FitsFileInfo"/> DTO carrying an <see cref="IFitsHandle"/>
        /// to the still-open file pointer. The handle stays open until the caller
        /// disposes it — subsequent <see cref="GetHeaderTextAsync"/> calls reuse it
        /// (no reopen).
        /// </summary>
        Task<FitsFileInfo> OpenImageAsync(string path, CancellationToken ct = default);

        /// <summary>
        /// Opens a FITS mask file and returns its axis metadata + open handle.
        /// FileTabViewModel compares the result against the loaded image to validate
        /// axis compatibility (replaces CanvassDesktop._browseMaskFile axis checks).
        /// </summary>
        Task<FitsFileInfo> OpenMaskAsync(string path, CancellationToken ct = default);

        /// <summary>
        /// Returns the formatted header text for the given 1-based HDU index, reusing
        /// the open handle. Replaces CanvassDesktop.ChangeHduSelection (line 1435) which
        /// reopened the file from disk on every dropdown selection.
        /// </summary>
        Task<string> GetHeaderTextAsync(IFitsHandle handle, int hduIndex, CancellationToken ct = default);
    }
}
