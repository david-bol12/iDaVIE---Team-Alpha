// brief §6.6 | File tab AFTER skeleton — minimal command interfaces
// Defined in-project to avoid a WindowsBase / WPF reference that Unity does not link.
// The View (FileTabView MonoBehaviour) calls CanExecute() before wiring button onClick.
namespace iDaVIE.Desktop.FileTab
{
    /// <summary>
    /// Minimal synchronous command interface used by ViewModels.
    /// Replaces direct button-onClick method wiring in the Unity scene.
    /// </summary>
    public interface ICommand
    {
        bool CanExecute();
        void Execute();
        event EventHandler CanExecuteChanged;
    }

    /// <summary>
    /// Async variant of <see cref="ICommand"/> for I/O-bound operations
    /// (file-picking, FITS reads, cube loading).
    /// </summary>
    public interface IAsyncCommand
    {
        bool CanExecute();
        Task ExecuteAsync();
        event EventHandler CanExecuteChanged;
    }
}
