// WE2-4 | Debug tab AFTER skeleton
// LogStream — concrete implementation of ILogStream.
// Thread-safe observer registration and dispatch.
// No UnityEngine dependency.
using System;
using System.Collections.Generic;
namespace iDaVIE.Desktop.DebugTab
{
    /// <summary>
    /// Default implementation of <see cref="ILogStream"/>.
    /// Maintains a list of <see cref="ILogObserver"/> subscribers
    /// and dispatches each published <see cref="LogEntry"/> to all of them.
    /// </summary>
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
        {
            var entry = new LogEntry(level, message, DateTime.UtcNow);
            ILogObserver[] snapshot;
            lock (_lock) { snapshot = _observers.ToArray(); }
            foreach (var observer in snapshot)
                observer.OnNext(entry);
        }
    }
}
