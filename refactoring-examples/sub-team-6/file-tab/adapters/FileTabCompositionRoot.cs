// brief §6.6 | File tab AFTER — FileTabCompositionRoot (Unity-assembly)
// The only class permitted to reference both the domain assembly and the Unity
// assembly simultaneously. Constructs and wires the object graph once on Awake.
// Satisfies the Pure DI / composition root pattern from ADR-003.
using System;
using iDaVIE.Client.Gateway;
using iDaVIE.Desktop.FileTab;
using UnityEngine;

namespace iDaVIE.Desktop.Adapters.FileTab
{
    // The "main()" of the File tab — the one place the whole object graph is built and wired (Pure DI, ADR-003). It's the only class allowed to touch both the domain assembly and Unity at once, which is why all the messy wiring lives here at the edge.
    // Scene setup: attach this MonoBehaviour to the root of the File tab panel GameObject, and assign _view and _volumeAdapter in the Inspector. The IServiceGateway is supplied by an outer scene-level composition root (one gateway per session) via Configure() before Awake() runs — the File tab shares the session gateway with the Debug tab and other panels rather than owning the transport (mirrors DebugTabCompositionRoot).
    // FitsServiceAdapter and FileDialogServiceAdapter are plain classes (not MonoBehaviours), so they are new()-ed here. VolumeServiceAdapter is a MonoBehaviour — it needs the coroutine host — so Unity instantiates it and it is assigned via the Inspector.


    // Ensure that this game object is only attached to a GameObject once
    [DisallowMultipleComponent]
    public sealed class FileTabCompositionRoot : MonoBehaviour
    {
        [SerializeField] private FileTabView          _view          = null!;
        [SerializeField] private VolumeServiceAdapter _volumeAdapter = null!;

        // The session-scoped transport seam. FitsServiceAdapter forwards FITS reads over this gateway as JSON-RPC (file.open / dataset.getAxes / dataset.getHeader) — no [DllImport] or IntPtr ever exists client-side.
        private IServiceGateway? _gateway;

        // The composition root owns the VM lifetime. Holding the reference here ensures the VM is not Collected by Garbage if the view is ever rebound, and makes the ownership chain explicit: CompositionRoot → ViewModel → View.
        private FileTabViewModel? _vm;

        // Injects the session-scoped IServiceGateway. Must be called before Awake() — the outer scene composition root is responsible for ordering. Mirrors DebugTabCompositionRoot.Configure.
        public void Configure(IServiceGateway gateway) => _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));

        private void Awake()
        {
            if (_gateway is null)
                throw new InvalidOperationException("FileTabCompositionRoot.Configure(gateway) must be called before Awake.");

            IFitsService       fitsService   = new FitsServiceAdapter(_gateway);
            IFileDialogService dialogService = new FileDialogServiceAdapter();
            IVolumeService     volumeService = _volumeAdapter;
            IMemoryProbe       memoryProbe   = new MemoryProbeAdapter();

            _vm = new FileTabViewModel(fitsService, dialogService, volumeService, memoryProbe);
            _view.BindTo(_vm);
        }

        private void OnDestroy()
        {
            // Dispose the VM first so any open FITS handles are released,
            // then null out the owning reference so the object graph becomes eligible for Garbage Collection once the view also releases its reference.
            _vm?.Dispose();
            _vm = null;
        }
    }
}
