namespace iDaVIE.Desktop.DebugTab
{
    //File references the errors/warnings being added/removed from the logs
    public interface ILogStream
    {
        //Publish with UtcNow as the timestamp
        void Publish(LogLevel level, string message);

        //Publish a log from json, with warning/error level, message, and timestamp
        void Publish(LogLevel level, string message, System.DateTime timestamp);

        //registering the same observer twice has no effect.
        void Subscribe(ILogObserver observer);
        void Unsubscribe(ILogObserver observer);
    }

    public enum LogLevel { Info, Warning, Error }

    public sealed record LogEntry(LogLevel Level, string Message, System.DateTime Timestamp);
}
