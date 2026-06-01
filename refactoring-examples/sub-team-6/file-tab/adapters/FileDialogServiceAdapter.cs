// brief §6.6 | File tab AFTER — FileDialogServiceAdapter (Unity-assembly adapter)
// Wraps StandaloneFileBrowser's async callback pattern into Task<string?>.
// Also owns PlayerPrefs persistence of the last-used directory.
// Satisfies ADR-002 (ACL): the ViewModel never touches SFB or PlayerPrefs.
using System.IO;
using System.Threading.Tasks;
using iDaVIE.Desktop.FileTab;
using SFB;
using UnityEngine;

namespace iDaVIE.Desktop.Adapters.FileTab
{
    /// <summary>
    /// Adapter for <see cref="IFileDialogService"/>.
    /// Converts StandaloneFileBrowser's callback model into a Task the ViewModel awaits.
    ///
    /// Replaces the direct SFB calls in CanvassDesktop.BrowseImageFile (line 317)
    /// and BrowseMaskFile, while keeping PlayerPrefs persistence out of the domain.
    /// </summary>
    public sealed class FileDialogServiceAdapter : IFileDialogService
    {
        private const string LastPathKey = "LastPath";

        public Task<string?> PickFileAsync(string title, string initialDirectory, string[] extensions)
        {
            var tcs = new TaskCompletionSource<string?>();

            string startDir = string.IsNullOrEmpty(initialDirectory)
                ? PlayerPrefs.GetString(LastPathKey, "")
                : initialDirectory;

            if (!Directory.Exists(startDir))
                startDir = "";

            var filters = new[]
            {
                new ExtensionFilter("FITS Files", extensions),
                new ExtensionFilter("All Files", "*"),
            };

            StandaloneFileBrowser.OpenFilePanelAsync(title, startDir, filters, false, paths =>
            {
                if (paths.Length == 1 && !string.IsNullOrEmpty(paths[0]))
                {
                    PlayerPrefs.SetString(LastPathKey, Path.GetDirectoryName(paths[0]));
                    PlayerPrefs.Save();
                    tcs.TrySetResult(paths[0]);
                }
                else
                {
                    tcs.TrySetResult(null);
                }
            });

            return tcs.Task;
        }
    }
}
