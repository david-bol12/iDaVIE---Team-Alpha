// WE1-3 | File tab AFTER — FileTabCompositionRoot (Unity-assembly)
// The only class permitted to reference both the domain assembly and the Unity
// assembly simultaneously. Constructs and wires the object graph once on Awake.
// Satisfies the Pure DI / composition root pattern from ADR-003.
using System;
using iDaVIE.Client.Gateway;
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
    ///   - The <see cref="IServiceGateway"/> instance is supplied by an outer
    ///     scene-level composition root (one gateway per session) via
    ///     <see cref="Configure"/> before <see cref="Awake"/> runs. The File tab
    ///     does not own the transport lifecycle — it shares the session gateway
    ///     with the Debug tab and the other panels (mirrors DebugTabCompositionRoot).
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

        // The session-scoped transport seam. FitsServiceAdapter forwards FITS
        // reads over this gateway as JSON-RPC (file.open / dataset.getAxes /
        // dataset.getHeader) — no [DllImport] or IntPtr ever exists client-side.
        private IServiceGateway? _gateway;

        // The composition root owns the VM lifetime. Holding the reference here
        // ensures the VM is not GC'd if the view is ever rebound, and makes the
        // ownership chain explicit: CompositionRoot → ViewModel → View.
        private FileTabViewModel? _vm;

        /// <summary>
        /// Inject the session-scoped <see cref="IServiceGateway"/>. Must be called
        /// before <see cref="Awake"/> — the outer scene composition root is
        /// responsible for ordering. Mirrors DebugTabCompositionRoot.Configure.
        /// </summary>
        public void Configure(IServiceGateway gateway)
            => _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));

        private void Awake()
        {
            if (_gateway is null)
                throw new InvalidOperationException(
                    "FileTabCompositionRoot.Configure(gateway) must be called before Awake.");

            IFitsService       fitsService   = new FitsServiceAdapter(_gateway);
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
