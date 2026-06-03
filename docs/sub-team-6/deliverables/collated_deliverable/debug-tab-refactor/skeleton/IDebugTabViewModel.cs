using System.Collections.Generic;
namespace iDaVIE.Desktop.DebugTab
{
    public interface IDebugTabViewModel
    {
        //Get all the current set of debug logs
        IReadOnlyList<LogEntry> LogEntries { get; }

        //add entry to logs
        void AppendEntry(LogEntry entry);

        //clear entries from logs
        void ClearEntries();

        //The view refreshes when the log entries changes
        event System.Action? EntriesChanged;

        // When true the View scrolls to the bottom on each new entry.
        bool AutoScrollEnabled { get; set; }
    }
}
