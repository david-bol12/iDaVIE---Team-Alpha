// Only class in this tab allowed to touch both the domain assemblies and UnityEngine.
using System;
using iDaVIE.Client.Gateway;
using iDaVIE.Desktop.DebugTab;
using UnityEngine;

namespace iDaVIE.Desktop.Adapters.DebugTab
{
    /// Wires <see cref="GatewayLogStreamAdapter"/>, <see cref="DebugTabViewModel"/>,
    /// and <see cref="DebugTabView"/> for the Debug tab panel.
    /// Attach to the panel root; assign <see cref="_view"/> in the Inspector.
    [DisallowMultipleComponent]
    public sealed class DebugTabCompositionRoot : MonoBehaviour
    {
        [SerializeField] private DebugTabView _view = null!;

        private IServiceGateway? _gateway;
        private GatewayLogStreamAdapter? _logStreamAdapter;
        private DebugTabViewModel? _vm;

        //Must be called before Awake — the outer scene composition root owns ordering
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
