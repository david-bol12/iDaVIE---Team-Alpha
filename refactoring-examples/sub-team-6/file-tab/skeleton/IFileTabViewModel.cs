// brief §6.6 | File tab AFTER skeleton — IFileTabViewModel
// ViewModel contract bound by the View (FileTabView / thin CanvassDesktop shell).
// No UnityEngine dependency. Satisfies NFR-MOD-2 and ADR-009 (MVVM split).
using System.ComponentModel;
namespace iDaVIE.Desktop.FileTab
{
    // The contract between View and ViewModel. The View binds against this interface only, never the concrete class — so the VM can be swapped or faked in tests.
    // Everything the View displays is a get property; everything the user can do is a command. INotifyPropertyChanged is how the VM pushes changes back out to the View.
    public interface IFileTabViewModel : INotifyPropertyChanged
    {
        // File paths (display labels)
        // Chosen image/mask paths. Get-only: the user sets them via BrowseImage/BrowseMask, not by typing into the View.
        string? ImagePath { get; }
        string? MaskPath  { get; }

        // HDU selection dropdown
        // The dropdown pattern used throughout: VM owns the option list, the View shows it; SelectedHduIndex is two-way so picking an entry feeds back into the VM.
        IReadOnlyList<HduInfo> HduOptions { get; }
        int SelectedHduIndex { get; set; }

        // Z-axis selection (shown only for 4+ axis cubes)
        // Same list/index pairing as HDU. The View hides this whole group when there's nothing to choose.
        IReadOnlyList<string> ZAxisOptions { get; }
        int SelectedZAxisIndex { get; set; }

        // Subset selector
        // Toggle for the optional crop region; Subset is the nested VM holding the six X/Y/Z bounds (it raises its own PropertyChanged).
        bool SubsetEnabled { get; set; }
        SubsetBoundsViewModel Subset { get; }

        // Aspect-ratio (Ratio_Dropdown on file-load modal)
        // Labels for the aspect-ratio dropdown ("X=Y=Z", "X=Y"), index-aligned with RatioMode. Replaces the inspector-wired Ratio_Dropdown from scope §10 Anomaly #5.
        IReadOnlyList<string> RatioModeOptions { get; }
        RatioMode RatioMode { get; set; }

        // Computed / derived state
        // Read-only state the VM works out for itself; the View just displays it. All pure C#, no Unity calls.

        // True when the file is a loadable 3-D+ cube and any chosen mask's axes match the image. Replaces CanvassDesktop.IsLoadable().
        bool IsLoadable { get; }

        // Pre-formatted FITS header text for the Information panel.
        string? HeaderText { get; }

        // True while an async command is running — drives the spinner / disabled buttons.
        bool IsLoading { get; }

        // Validation or error message for the View to show the user.
        string? ValidationMessage { get; }

        // Commands
        // User actions the View triggers. Async ones (browse/load) do I/O off the UI thread; ClearMask is instant so it's a plain ICommand.
        IAsyncCommand BrowseImageCommand { get; }
        IAsyncCommand BrowseMaskCommand  { get; }
        IAsyncCommand LoadCommand        { get; }
        ICommand      ClearMaskCommand   { get; }
    }
}
