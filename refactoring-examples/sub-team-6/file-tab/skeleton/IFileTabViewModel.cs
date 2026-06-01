// WE1-3 | File tab AFTER skeleton — IFileTabViewModel
// ViewModel contract bound by the View (FileTabView / thin CanvassDesktop shell).
// No UnityEngine dependency. Satisfies NFR-MOD-2 and ADR-009 (MVVM split).
using System.ComponentModel;
namespace iDaVIE.Desktop.FileTab
{
    /// <summary>
    /// ViewModel interface for the File tab panel.
    /// The View depends only on this interface — never on the concrete class.
    /// Every property the View needs to display is exposed here;
    /// every user action the View can trigger is exposed as a command.
    /// </summary>
    public interface IFileTabViewModel : INotifyPropertyChanged
    {
        // ── File paths (display labels) ──────────────────────────────────────
        string? ImagePath { get; }
        string? MaskPath  { get; }

        // ── HDU selection dropdown ───────────────────────────────────────────
        IReadOnlyList<HduInfo> HduOptions { get; }
        int SelectedHduIndex { get; set; }

        // ── Z-axis selection (shown only for 4+ axis cubes) ──────────────────
        IReadOnlyList<string> ZAxisOptions { get; }
        int SelectedZAxisIndex { get; set; }

        // ── Subset selector ──────────────────────────────────────────────────
        bool SubsetEnabled { get; set; }
        SubsetBoundsViewModel Subset { get; }

        // ── Aspect-ratio (Ratio_Dropdown on file-load modal) ─────────────────
        /// <summary>
        /// Display labels for the aspect-ratio dropdown ("X=Y=Z", "X=Y") — index-aligned
        /// with <see cref="RatioMode"/>. Replaces the inspector-wired Ratio_Dropdown
        /// options described in scope §10 Anomaly #5.
        /// </summary>
        IReadOnlyList<string> RatioModeOptions { get; }
        RatioMode RatioMode { get; set; }

        // ── Computed / derived state ─────────────────────────────────────────
        /// <summary>
        /// True when the selected file is a loadable 3-D+ cube and (if a mask
        /// was selected) its axes match the image. Pure C# — no Unity calls.
        /// Replaces CanvassDesktop.IsLoadable().
        /// </summary>
        bool IsLoadable { get; }

        /// <summary>Pre-formatted FITS header text for the Information panel.</summary>
        string? HeaderText { get; }

        /// <summary>True while any async command is running (drives spinner / disabled buttons).</summary>
        bool IsLoading { get; }

        /// <summary>Human-readable validation or error message for the View to display.</summary>
        string? ValidationMessage { get; }

        // ── Commands ─────────────────────────────────────────────────────────
        IAsyncCommand BrowseImageCommand { get; }
        IAsyncCommand BrowseMaskCommand  { get; }
        IAsyncCommand LoadCommand        { get; }
        ICommand      ClearMaskCommand   { get; }
    }
}
