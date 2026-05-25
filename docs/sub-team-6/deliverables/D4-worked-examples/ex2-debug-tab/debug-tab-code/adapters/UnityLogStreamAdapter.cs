// WE2-4 | Debug tab AFTER — UnityLogStreamAdapter (Unity-assembly adapter)
// Bridges Application.logMessageReceived to ILogStream.
// This is the only class that crosses the Unity ↔ domain logging boundary.
// The ViewModel never depends on Application.logMessageReceived directly.
// Satisfies ADR-0001 (Observer pattern) and ADR-0003 (ACL).
using iDaVIE.Desktop.DebugTab;
using UnityEngine;

namespace iDaVIE.Desktop.Adapters.DebugTab
{
    /// <summary>
    /// Concrete adapter that hooks Unity's global <c>Application.logMessageReceived</c>
    /// callback and publishes each message to an inner <see cref="LogStream"/> instance.
    ///
    /// Contrast with the "before" state where log information was only available via
    /// <c>Debug.Log</c> calls scattered through CanvassDesktop (lines 351, 372, 521, …)
    /// with no mechanism for the UI to observe or filter the log stream.
    ///
    /// Attach to a persistent GameObject in the scene (e.g. the CanvassDesktop root).
    /// The <see cref="DebugTabCompositionRoot"/> injects this instance into the ViewModel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnityLogStreamAdapter : MonoBehaviour, ILogStream
    {
        // Delegates to the domain LogStream so thread-safety logic lives once
        private readonly LogStream _inner = new();

        private void OnEnable()
            => Application.logMessageReceived += OnUnityLog;

        private void OnDisable()
            => Application.logMessageReceived -= OnUnityLog;

        private void OnUnityLog(string message, string stackTrace, LogType type)
        {
            var level = type switch
            {
                LogType.Warning   => LogLevel.Warning,
                LogType.Error     => LogLevel.Error,
                LogType.Exception => LogLevel.Error,
                LogType.Assert    => LogLevel.Error,
                _                 => LogLevel.Info,
            };
            _inner.Publish(level, message);
        }

        // ── ILogStream delegation ─────────────────────────────────────────────

        public void Publish(LogLevel level, string message)   => _inner.Publish(level, message);
        public void Subscribe(ILogObserver observer)          => _inner.Subscribe(observer);
        public void Unsubscribe(ILogObserver observer)        => _inner.Unsubscribe(observer);
    }
}
