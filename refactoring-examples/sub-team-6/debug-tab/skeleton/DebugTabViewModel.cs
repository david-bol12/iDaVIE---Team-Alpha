// WE2-4 | Debug tab AFTER
// DebugTabViewModel — concrete ViewModel for the Debug tab.
// Implements IDebugTabViewModel, ILogObserver, and IDisposable.
// No UnityEngine dependency. Satisfies NFR-MOD-2 and ADR-0001.
using System;
using System.Collections.Generic;
namespace iDaVIE.Desktop.DebugTab
{
    /// <summary>
    /// Concrete ViewModel for the Debug tab panel.
    /// Subscribes to <see cref="ILogStream"/> via <see cref="ILogObserver"/>
    /// and exposes <see cref="LogEntries"/> for the View to bind.
    ///
    /// Dispose() unsubscribes from the log stream. The composition root owns
    /// the lifetime and must call Dispose() from OnDestroy.
    /// </summary>
    public sealed class DebugTabViewModel : IDebugTabViewModel, ILogObserver, IDisposable
    {
        // Caps the backing list so a long session cannot exhaust memory.
        // The View already limits display to 500 lines; we keep 4× that in
        // the ViewModel so scroll-back history survives a view rebind.
        private const int MaxEntries = 2000;

        private readonly List<LogEntry> _entries = new(capacity: 256);
        private readonly ILogStream _logStream;
        private bool _disposed;

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
            // Trim oldest entries once the cap is reached, preserving the most recent.
            if (_entries.Count > MaxEntries)
                _entries.RemoveAt(0);
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

        /// <summary>
        /// Unsubscribes from the log stream. Safe to call more than once.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _logStream.Unsubscribe(this);
            _disposed = true;
        }
    }
}
