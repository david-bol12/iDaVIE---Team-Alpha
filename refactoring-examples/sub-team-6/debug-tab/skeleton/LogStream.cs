using System;
using System.Collections.Generic;
namespace iDaVIE.Desktop.DebugTab
{
    // Snapshot-under-lock so Subscribe/Unsubscribe during dispatch is safe.
    public sealed class LogStream : ILogStream
    {
        private readonly List<ILogObserver> _observers = new();
        private readonly object _lock = new();

        /// <inheritdoc/>
        public void Subscribe(ILogObserver observer)
        {
            if (observer is null) throw new ArgumentNullException(nameof(observer));
            lock (_lock) { if (!_observers.Contains(observer)) _observers.Add(observer); }
        }

        /// <inheritdoc/>
        public void Unsubscribe(ILogObserver observer)
        {
            if (observer is null) throw new ArgumentNullException(nameof(observer));
            lock (_lock) { _observers.Remove(observer); }
        }

        /// <inheritdoc/>
        public void Publish(LogLevel level, string message)
            => Publish(level, message, DateTime.UtcNow);

        /// <inheritdoc/>
        public void Publish(LogLevel level, string message, DateTime timestamp)
        {
            var entry = new LogEntry(level, message, timestamp);
            ILogObserver[] snapshot;
            lock (_lock) { snapshot = _observers.ToArray(); }
            foreach (var observer in snapshot)
                observer.OnNext(entry);
        }
    }
}
