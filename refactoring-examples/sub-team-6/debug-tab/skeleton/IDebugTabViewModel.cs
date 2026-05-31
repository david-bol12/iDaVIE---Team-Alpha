using System.Collections.Generic;
namespace iDaVIE.Desktop.DebugTab
{
    public interface IDebugTabViewModel
    {
        /// <summary>Snapshot of current entries; rebind after each <see cref="EntriesChanged"/>.</summary>
        IReadOnlyList<LogEntry> LogEntries { get; }

        void AppendEntry(LogEntry entry);
        void ClearEntries();

        /// <summary>Raised when <see cref="LogEntries"/> changes; the View subscribes to refresh its display.</summary>
        event System.Action? EntriesChanged;
    }
}
