// WE1-3 | File tab AFTER — FileTabCompositionRoot (Unity-assembly)
// The only class permitted to reference both the domain assembly and the Unity
// assembly simultaneously. Constructs and wires the object graph once on Awake.
// Satisfies the Pure DI / composition root pattern from ADR-0001.
using iDaVIE.Desktop.FileTab;
using UnityEngine;

namespace iDaVIE.Desktop.Adapters.FileTab
{
    /// <summary>
    /// Composition root for the File tab.
    ///
    /// Scene setup:
    ///   - Attach this MonoBehaviour to the root of the File tab panel GameObject.
    ///   - Assign <see cref="_view"/> and <see cref="_volumeAdapter"/> in the Inspector.
    ///   - FitsServiceAdapter and FileDialogServiceAdapter are pure-C# objects with no
    ///     MonoBehaviour lifecycle, so they are new()-ed here; VolumeServiceAdapter
    ///     needs the coroutine host so it is a MonoBehaviour assigned via Inspector.
    ///
    /// This pattern eliminates the FindObjectOfType calls that CanvassDesktop.Start()
    /// used to locate VolumeCommandController (line 158), VolumeInputController (line 157),
    /// and HistogramHelper (line 159).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FileTabCompositionRoot : MonoBehaviour
    {
        [SerializeField] private FileTabView          _view          = null!;
        [SerializeField] private VolumeServiceAdapter _volumeAdapter = null!;

        // The composition root owns the VM lifetime. Holding the reference here
        // ensures the VM is not GC'd if the view is ever rebound, and makes the
        // ownership chain explicit: CompositionRoot → ViewModel → View.
        private FileTabViewModel? _vm;

        private void Awake()
        {
            IFitsService       fitsService   = new FitsServiceAdapter();
            IFileDialogService dialogService = new FileDialogServiceAdapter();
            IVolumeService     volumeService = _volumeAdapter;
            IMemoryProbe       memoryProbe   = new MemoryProbeAdapter();

            _vm = new FileTabViewModel(fitsService, dialogService, volumeService, memoryProbe);
            _view.BindTo(_vm);
        }

        private void OnDestroy()
        {
            // Dispose the VM first so any open FITS handles are released, then
            // null out the owning reference so the object graph becomes eligible
            // for GC once the view also releases its reference.
            _vm?.Dispose();
            _vm = null;
        }
    }
}
