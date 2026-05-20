// WE2-4 | Debug tab AFTER skeleton
// DebugTabViewModel — concrete ViewModel for the Debug tab.
// Implements both IDebugTabViewModel and ILogObserver.
// No UnityEngine dependency. Satisfies NFR-MOD-2 and ADR-0001.
using System;
using System.Collections.Generic;
namespace iDaVIE.Desktop.DebugTab
{
    /// <summary>
    /// Concrete ViewModel for the Debug tab panel.
    /// Subscribes to <see cref="ILogStream"/> via <see cref="ILogObserver"/>
    /// and exposes <see cref="LogEntries"/> for the View to bind.
    /// </summary>
    public sealed class DebugTabViewModel : IDebugTabViewModel, ILogObserver
    {
        private readonly List<LogEntry> _entries = new();
        private readonly ILogStream _logStream;

        /// <summary>
        /// Initialises the ViewModel and subscribes to the provided log stream.
        /// </summary>
        public DebugTabViewModel(ILogStream logStream)
        {
            _logStream = logStream ?? throw new ArgumentNullException(nameof(logStream));
            _logStream.Subscribe(this);
        }

        /// <inheritdoc/>
        public IReadOnlyList<LogEntry> LogEntries => _entries.AsReadOnly();

        /// <inheritdoc/>
        public event Action? EntriesChanged;

        /// <inheritdoc/>
        public void AppendEntry(LogEntry entry)
        {
            if (entry is null) throw new ArgumentNullException(nameof(entry));
            _entries.Add(entry);
            EntriesChanged?.Invoke();
        }

        /// <inheritdoc/>
        public void ClearEntries()
        {
            _entries.Clear();
            EntriesChanged?.Invoke();
        }

        /// <summary>
        /// Called by <see cref="ILogStream"/> when a new log entry is published.
        /// Delegates to <see cref="AppendEntry"/>.
        /// </summary>
        void ILogObserver.OnNext(LogEntry entry) => AppendEntry(entry);
    }
}
