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
    /// UpdateHeaderFromFits, and IsLoadable.
    /// </summary>
    public interface IFitsService
    {
        /// <summary>
        /// Opens a FITS image file, reads all HDU metadata and the primary header,
        /// and returns a plain <see cref="FitsFileInfo"/> DTO.
        /// The adapter closes the native file pointer before returning — no IntPtr leaks
        /// across the ACL boundary.
        /// </summary>
        Task<FitsFileInfo> OpenImageAsync(string path, CancellationToken ct = default);

        /// <summary>
        /// Opens a FITS mask file and returns its axis metadata.
        /// FileTabViewModel compares the result against the loaded image to validate
        /// axis compatibility (replaces CanvassDesktop._browseMaskFile axis checks).
        /// </summary>
        Task<FitsFileInfo> OpenMaskAsync(string path, CancellationToken ct = default);

        /// <summary>
        /// Returns the formatted header text for a specific HDU.
        /// Called by FileTabViewModel when the user changes the HDU dropdown selection
        /// (replaces CanvassDesktop.ChangeHduSelection + UpdateHeaderFromFits).
        /// </summary>
        Task<string> GetHeaderTextAsync(string path, int hduIndex, CancellationToken ct = default);
    }
}
