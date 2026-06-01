namespace iDaVIE.Desktop.DebugTab
{
    public interface ILogObserver
    {
        /// <summary>Called by the stream on whatever thread Publish was invoked from.</summary>
        void OnNext(LogEntry entry);
    }
}
