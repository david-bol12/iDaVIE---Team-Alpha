// WE2-4 | Debug tab AFTER skeleton
// IDebugTabViewModel — ViewModel contract bound by the View (CanvassDesktop).
// No UnityEngine dependency. Satisfies NFR-MOD-2 and ADR-0001 (MVVM split).
using System.Collections.Generic;
namespace iDaVIE.Desktop.DebugTab
{
    /// <summary>
    /// ViewModel interface for the Debug tab panel.
    /// The View (CanvassDesktop) depends ONLY on this interface,
    /// never on a concrete implementation.
    /// </summary>
    public interface IDebugTabViewModel
    {
        /// <summary>
        /// Read-only list of log entries to display in the scroll list.
        /// The View binds to this collection.
        /// </summary>
        IReadOnlyList<LogEntry> LogEntries { get; }

        /// <summary>Append a new entry to the displayed log list.</summary>
        void AppendEntry(LogEntry entry);

        /// <summary>Clear all entries from the log display.</summary>
        void ClearEntries();

        /// <summary>
        /// Raised when <see cref="LogEntries"/> changes so the
        /// View can refresh its binding.
        /// </summary>
        event System.Action? EntriesChanged;
    }
}
