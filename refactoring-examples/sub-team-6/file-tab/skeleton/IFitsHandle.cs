// WE1-3 | File tab AFTER skeleton — IFitsHandle (server-resident FITS file lifetime)
// Eliminates the per-HDU-change file-reopen defect (CanvassDesktop.ChangeHduSelection
// line 1435 reopens the file on every dropdown selection). The handle is held open
// by the adapter; the ViewModel disposes it when replacing the image or on shutdown.
// No UnityEngine, no IntPtr — concrete IntPtr is sealed inside the adapter implementation.
namespace iDaVIE.Desktop.FileTab
{
    /// <summary>
    /// Opaque handle to an open FITS file held by <see cref="IFitsService"/>.
    /// Carried by <see cref="FitsFileInfo"/> and passed back to the service when
    /// reading subsequent HDU headers — replaces the per-call reopen pattern.
    ///
    /// Disposing the handle closes the underlying file pointer. The ViewModel
    /// owns the lifetime: dispose old before assigning a new one, and dispose
    /// all live handles in <c>FileTabViewModel.Dispose()</c>.
    /// </summary>
    public interface IFitsHandle : IDisposable
    {
        /// <summary>Absolute path the handle was opened against (informational only).</summary>
        string FilePath { get; }
    }
}
