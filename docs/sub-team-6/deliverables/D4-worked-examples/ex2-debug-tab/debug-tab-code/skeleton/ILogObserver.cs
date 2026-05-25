// WE2-4 | Debug tab AFTER skeleton
// ILogObserver — consumer-side contract for the Observer pattern.
// No UnityEngine dependency. Primary test-double target for T4 (NFR-TST-1).
namespace iDaVIE.Desktop.DebugTab
{
    /// <summary>
    /// Receives log entries published by <see cref="ILogStream"/>.
    /// Implement this interface to react to new log entries
    /// (e.g. DebugTabViewModel, test doubles for T4).
    /// </summary>
    public interface ILogObserver
    {
        /// <summary>Called by ILogStream when a new log entry is published.</summary>
        void OnNext(LogEntry entry);
    }
}
