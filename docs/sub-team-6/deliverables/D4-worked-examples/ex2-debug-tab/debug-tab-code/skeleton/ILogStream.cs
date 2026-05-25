// WE2-4 | Debug tab AFTER skeleton
// ILogStream — producer-side contract for the Observer pattern.
// No UnityEngine dependency. Satisfies NFR-MOD-2.
namespace iDaVIE.Desktop.DebugTab
{
    /// <summary>
    /// Allows any subsystem to publish log entries and
    /// allows consumers (e.g. DebugTabViewModel) to subscribe.
    /// </summary>
    public interface ILogStream
    {
        /// <summary>Publish a log entry to all registered observers.</summary>
        void Publish(LogLevel level, string message);

        /// <summary>Register an observer to receive future log entries.</summary>
        void Subscribe(ILogObserver observer);

        /// <summary>Unregister a previously registered observer.</summary>
        void Unsubscribe(ILogObserver observer);
    }

    /// <summary>Severity levels for log entries.</summary>
    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }

    /// <summary>Immutable value object representing a single log entry.</summary>
    public sealed record LogEntry(LogLevel Level, string Message, System.DateTime Timestamp);
}
