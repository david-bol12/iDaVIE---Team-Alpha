// brief §6.6 | File tab AFTER skeleton — minimal command interfaces
// Defined in-project to avoid a WindowsBase / WPF reference that Unity does not link.
// The View (FileTabView MonoBehaviour) calls CanExecute() before wiring button onClick.
namespace iDaVIE.Desktop.FileTab
{
    // Minimal synchronous command exposed by the VM. The View calls CanExecute() to drive button.interactable and Execute() on click — replaces wiring button-onClick straight to a method in the Unity scene. CanExecuteChanged lets the VM re-enable/disable the button when state changes.
    public interface ICommand
    {
        bool CanExecute();
        void Execute();
        event EventHandler CanExecuteChanged;
    }

    // Async variant of ICommand for I/O-bound actions (file-picking, FITS reads, cube loading). ExecuteAsync returns a Task the View fires-and-forgets; the VM flips CanExecute while it runs so the button disables itself.
    public interface IAsyncCommand
    {
        bool CanExecute();
        Task ExecuteAsync();
        event EventHandler CanExecuteChanged;
    }
}
