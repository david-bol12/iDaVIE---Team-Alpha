// WE2-4 | Debug tab AFTER — DebugTabCompositionRoot (Unity-assembly)
// Wires the log stream adapter, ViewModel, and View together.
// The only class permitted to reference both domain and Unity assemblies for this tab.
using iDaVIE.Desktop.DebugTab;
using UnityEngine;

namespace iDaVIE.Desktop.Adapters.DebugTab
{
    /// <summary>
    /// Composition root for the Debug tab.
    ///
    /// Scene setup:
    ///   - Attach this MonoBehaviour to the Debug tab panel root GameObject.
    ///   - Assign <see cref="_view"/> and <see cref="_logStreamAdapter"/> in the Inspector.
    ///   - <see cref="DebugTabViewModel"/> is a pure-C# class with no MonoBehaviour
    ///     lifecycle — it is new()-ed here and kept alive by the <see cref="_vm"/> field.
    ///
    /// The ViewModel subscribes to the log stream in its constructor and will receive
    /// all entries published by <see cref="UnityLogStreamAdapter"/> from that point on.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DebugTabCompositionRoot : MonoBehaviour
    {
        [SerializeField] private DebugTabView          _view             = null!;
        [SerializeField] private UnityLogStreamAdapter _logStreamAdapter = null!;

        // Kept alive by this MonoBehaviour field — not eligible for GC
        private DebugTabViewModel? _vm;

        private void Awake()
        {
            _vm = new DebugTabViewModel(_logStreamAdapter);
            _view.BindTo(_vm);
        }

        private void OnDestroy()
        {
            // Unsubscribe to prevent dead-observer memory leak during hot-reload
            _logStreamAdapter.Unsubscribe(_vm!);
        }
    }
}
