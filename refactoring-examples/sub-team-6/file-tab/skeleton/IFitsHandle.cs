// brief §6.6 | File tab AFTER skeleton — IFitsHandle (server-resident FITS file lifetime)
// Eliminates the per-HDU-change file-reopen defect (CanvassDesktop.ChangeHduSelection
// line 1435 reopens the file on every dropdown selection). The handle is held open
// by the adapter; the ViewModel disposes it when replacing the image or on shutdown.
// No UnityEngine, no IntPtr — concrete IntPtr is sealed inside the adapter implementation.
namespace iDaVIE.Desktop.FileTab
{
    // Opaque handle to a FITS file that IFitsService keeps open. Carried on FitsFileInfo and handed back to the service to read further HDU headers — so the file is opened once, not reopened on every dropdown change.
    // The handle is IDisposable and the ViewModel owns its lifetime: dispose the old one before assigning a new image, and dispose every live handle in FileTabViewModel.Dispose(). Disposing closes the underlying file pointer.
    public interface IFitsHandle : IDisposable
    {
        // Absolute path the handle was opened against (informational only).
        string FilePath { get; }
    }
}
