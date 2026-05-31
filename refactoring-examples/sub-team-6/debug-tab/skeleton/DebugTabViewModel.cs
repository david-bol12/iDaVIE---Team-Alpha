using System;
using System.Collections.Generic;
namespace iDaVIE.Desktop.DebugTab
{
    /// <summary>
    /// Observes an <see cref="ILogStream"/> and exposes entries for the View.
    /// Call Dispose() from OnDestroy to unsubscribe.
    /// </summary>
    public sealed class DebugTabViewModel : IDebugTabViewModel, ILogObserver, IDisposable
    {
        // 4× the View's display cap so scroll-back history survives a view rebind.
        private const int MaxEntries = 2000;

        private readonly List<LogEntry> _entries = new(capacity: 256);
        private readonly ILogStream _logStream;
        private bool _disposed;

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

        // Bridge from ILogObserver into the public AppendEntry contract.
        void ILogObserver.OnNext(LogEntry entry) => AppendEntry(entry);

        // Safe to call multiple times; subsequent calls are no-ops.
        public void Dispose()
        {
            if (_disposed) return;
            _logStream.Unsubscribe(this);
            _disposed = true;
        }
    }
}
