// WE2-4 | Debug tab AFTER — DebugTabCompositionRoot (Unity-assembly)
// Wires the gateway-backed log stream adapter, the ViewModel, and the View.
// The only class for this tab permitted to reference both domain assemblies
// (DebugTabSkeleton + GatewayContracts) and the Unity runtime.
using System;
using iDaVIE.Client.Gateway;
using iDaVIE.Desktop.DebugTab;
using UnityEngine;

namespace iDaVIE.Desktop.Adapters.DebugTab
{
    /// <summary>
    /// Composition root for the Debug tab.
    ///
    /// Scene setup:
    ///   - Attach this MonoBehaviour to the Debug tab panel root GameObject.
    ///   - Assign <see cref="_view"/> in the Inspector.
    ///   - The <see cref="IServiceGateway"/> instance is supplied by an outer
    ///     scene-level composition root (one gateway per session). This panel
    ///     receives it via <see cref="Configure"/> before <see cref="Awake"/>
    ///     runs, or constructs a stand-in for editor/play-mode bring-up.
    ///   - <see cref="DebugTabViewModel"/> is a pure-C# class with no
    ///     MonoBehaviour lifecycle — it is new()-ed here and kept alive by
    ///     the <see cref="_vm"/> field.
    ///
    /// The ViewModel subscribes to <see cref="GatewayLogStreamAdapter"/> in its
    /// constructor and will receive every server-pushed log.emit notification
    /// the gateway forwards.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DebugTabCompositionRoot : MonoBehaviour
    {
        [SerializeField] private DebugTabView _view = null!;

        private IServiceGateway? _gateway;
        private GatewayLogStreamAdapter? _logStreamAdapter;
        private DebugTabViewModel? _vm;

        /// <summary>
        /// Inject the session-scoped <see cref="IServiceGateway"/>. Must be
        /// called before <see cref="Awake"/> — the outer scene composition root
        /// is responsible for ordering.
        /// </summary>
        public void Configure(IServiceGateway gateway)
            => _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));

        private void Awake()
        {
            if (_gateway is null)
                throw new InvalidOperationException(
                    "DebugTabCompositionRoot.Configure(gateway) must be called before Awake.");

            _logStreamAdapter = new GatewayLogStreamAdapter(_gateway);
            _vm = new DebugTabViewModel(_logStreamAdapter);
            _view.BindTo(_vm);
        }

        private void OnDestroy()
        {
            // Dispose order matters: VM first (unsubscribes its observer from
            // the stream), then the adapter (detaches from the gateway).
            _vm?.Dispose();
            _logStreamAdapter?.Dispose();
        }
    }
}
