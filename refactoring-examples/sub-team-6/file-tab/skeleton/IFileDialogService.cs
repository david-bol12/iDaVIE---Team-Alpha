// WE1-3 | File tab AFTER skeleton — IFileDialogService (ACL boundary)
// Abstracts over StandaloneFileBrowser (SFB). The adapter wraps the SFB async
// callback and lives in the Unity assembly. No UnityEngine dependency here.
// Satisfies ADR-003 (DI) and ADR-002 (ACL).
namespace iDaVIE.Desktop.FileTab
{
    /// <summary>
    /// Domain interface for OS file-picker dialogs.
    /// Replaces the direct StandaloneFileBrowser.OpenFilePanelAsync calls in
    /// CanvassDesktop.BrowseImageFile, BrowseMaskFile, and BrowseSourcesFile.
    /// </summary>
    public interface IFileDialogService
    {
        /// <summary>
        /// Opens a modal file-picker dialog.
        /// </summary>
        /// <param name="title">Dialog window title.</param>
        /// <param name="initialDirectory">
        ///     Starting directory path. Pass <see cref="string.Empty"/> to use the
        ///     last-remembered directory (the adapter persists this via PlayerPrefs).
        /// </param>
        /// <param name="extensions">
        ///     Allowed file extensions without a leading dot, e.g. ["fits", "fit"].
        /// </param>
        /// <returns>
        ///     The selected absolute file path, or <c>null</c> if the user cancelled.
        /// </returns>
        Task<string?> PickFileAsync(string title, string initialDirectory, string[] extensions);
    }
}
