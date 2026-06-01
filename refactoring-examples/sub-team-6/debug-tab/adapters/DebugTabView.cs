using System.Text;
using iDaVIE.Desktop.DebugTab;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace iDaVIE.Desktop.Adapters.DebugTab
{
    /// Thin MonoBehaviour view for the Debug tab panel.
    /// Rebuilds the log text on each entries change event.
    public sealed class DebugTabView : MonoBehaviour
    {
        // ── Inspector-assigned references ─────────────────────────────────────
        [SerializeField] private TMP_Text  _logText     = null!;
        [SerializeField] private Scrollbar _scrollbar   = null!;
        [SerializeField] private Button    _clearButton = null!;

        private IDebugTabViewModel? _vm;

        //Number cap of how many messages to rebuilt so not rebuilding pointless data
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

        private void OnDestroy()
        {
            if (_vm == null) return;
            _vm.EntriesChanged -= OnEntriesChanged;
            _clearButton.onClick.RemoveListener(_vm.ClearEntries);
            _vm = null;
        }

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
