namespace iDaVIE.Desktop.DebugTab
{
    public interface ILogStream
    {
        /// <summary>Publish with UtcNow as the timestamp.</summary>
        void Publish(LogLevel level, string message);

        /// <summary>Publish with an explicit timestamp — preserves the server-emitted ts end-to-end.</summary>
        void Publish(LogLevel level, string message, System.DateTime timestamp);

        /// <summary>Idempotent — registering the same observer twice has no effect.</summary>
        void Subscribe(ILogObserver observer);
        void Unsubscribe(ILogObserver observer);
    }

    public enum LogLevel { Info, Warning, Error }

    public sealed record LogEntry(LogLevel Level, string Message, System.DateTime Timestamp);
}
