// WE2-4 | Debug tab AFTER — DebugTabView (Unity-assembly, thin View)
// Binds IDebugTabViewModel to Unity UI controls.
// No business logic. No transform.Find chains.
// Satisfies ADR-0001 (MVVM split) and NFR-MOD-2.
using System.Text;
using iDaVIE.Desktop.DebugTab;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace iDaVIE.Desktop.Adapters.DebugTab
{
    /// <summary>
    /// Thin MonoBehaviour view for the Debug tab panel.
    ///
    /// Subscribes to <see cref="IDebugTabViewModel.EntriesChanged"/> and rebuilds
    /// the log display text on each change. The Clear button delegates directly to
    /// <see cref="IDebugTabViewModel.ClearEntries"/>.
    ///
    /// Contrast with the "before" state: CanvassDesktop had no debug console panel —
    /// log output existed only in Unity's built-in Console window and was unreachable
    /// at runtime by the desktop operator. This view makes the live log observable
    /// inside the application UI.
    /// </summary>
    public sealed class DebugTabView : MonoBehaviour
    {
        // ── Inspector-assigned references ─────────────────────────────────────
        [SerializeField] private TMP_Text  _logText     = null!;
        [SerializeField] private Scrollbar _scrollbar   = null!;
        [SerializeField] private Button    _clearButton = null!;

        private IDebugTabViewModel? _vm;

        // Capped to avoid unbounded TMP_Text growth inside long sessions
        private const int MaxDisplayLines = 500;

        // ── Public binding point ──────────────────────────────────────────────

        public void BindTo(IDebugTabViewModel vm)
        {
            if (_vm != null)
                _vm.EntriesChanged -= OnEntriesChanged;

            _vm = vm;
            _vm.EntriesChanged += OnEntriesChanged;

            _clearButton.onClick.AddListener(_vm.ClearEntries);

            OnEntriesChanged();
        }

        // ── ViewModel → View ──────────────────────────────────────────────────

        private void OnEntriesChanged()
        {
            if (_vm == null) return;

            var entries = _vm.LogEntries;
            int start   = entries.Count > MaxDisplayLines ? entries.Count - MaxDisplayLines : 0;

            var sb = new StringBuilder(entries.Count * 80);
            for (int i = start; i < entries.Count; i++)
            {
                var e     = entries[i];
                string hex = e.Level switch
                {
                    LogLevel.Warning => "#FFFF00",
                    LogLevel.Error   => "#FF4444",
                    _                => "#FFFFFF",
                };
                sb.Append($"<color={hex}>[{e.Timestamp:HH:mm:ss}] {e.Message}</color>\n");
            }

            _logText.text    = sb.ToString();
            _scrollbar.value = 0f;   // scroll to bottom (0 = bottom for inverted scroll)
        }
    }
}
