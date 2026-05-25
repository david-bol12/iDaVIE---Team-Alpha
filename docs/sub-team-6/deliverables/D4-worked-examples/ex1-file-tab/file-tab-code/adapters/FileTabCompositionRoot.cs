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

        private void Awake()
        {
            IFitsService       fitsService   = new FitsServiceAdapter();
            IFileDialogService dialogService = new FileDialogServiceAdapter();
            IVolumeService     volumeService = _volumeAdapter;

            var vm = new FileTabViewModel(fitsService, dialogService, volumeService);
            _view.BindTo(vm);
        }
    }
}
