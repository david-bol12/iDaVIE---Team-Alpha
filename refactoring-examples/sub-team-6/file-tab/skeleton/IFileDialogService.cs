// brief §6.6 | File tab AFTER skeleton — IFileDialogService (ACL boundary)
// Abstracts over StandaloneFileBrowser (SFB). The adapter wraps the SFB async
// callback and lives in the Unity assembly. No UnityEngine dependency here.
// Satisfies ADR-003 (DI) and ADR-002 (ACL).
namespace iDaVIE.Desktop.FileTab
{
    // The anti-corruption boundary for OS file-picker dialogs. The VM depends on this, not on StandaloneFileBrowser — so the Unity dependency stays in the adapter and the VM stays testable with a fake.
    // Replaces the direct StandaloneFileBrowser.OpenFilePanelAsync calls in CanvassDesktop.BrowseImageFile/BrowseMaskFile/BrowseSourcesFile.
    public interface IFileDialogService
    {
        // Opens a modal file picker and returns the chosen absolute path, or null if the user cancelled.
        // title: dialog window title.
        // initialDirectory: where to start; pass "" to reuse the last-remembered directory (the adapter persists that via PlayerPrefs).
        // extensions: allowed extensions without the dot, e.g. ["fits", "fit"].
        Task<string?> PickFileAsync(string title, string initialDirectory, string[] extensions);
    }
}
