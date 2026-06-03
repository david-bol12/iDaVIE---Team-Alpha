namespace iDaVIE.Desktop.DebugTab
{
    public interface ILogObserver
    {
        //Called by the stream on whatever thread Publish was invoked from.
        void OnNext(LogEntry entry);
    }
}
