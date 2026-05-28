// WE2-4 | Debug tab AFTER — GatewayLogStreamAdapter (client-side gateway proxy)
//
// Replaces the previous UnityLogStreamAdapter, which subscribed to Unity's
// static Application.logMessageReceived event. Per the brief §6.6 ("Debug tab
// as Observer of a structured logging stream") and ADR-0002 §"Method
// catalogue (v1)", log records arrive as server-pushed log.emit notifications
// on the JSON-RPC transport.
//
// This adapter:
//   1. Subscribes to IServiceGateway.OnNotification on construction.
//   2. Filters for method == "log.emit".
//   3. Deserialises the params into a (level, msg, ts) triple per ADR-0002.
//   4. Republishes through an inner LogStream so the existing ILogStream
//      subscriber contract (DebugTabViewModel + future observers) is preserved.
//
// No UnityEngine, no [DllImport]. Compiles in isolation against
// GatewayContracts + DebugTabSkeleton.
//
// Threading: gateway notifications fire on the gateway's read-loop thread.
// LogStream.Publish is thread-safe; observers that touch UI state must marshal
// to the UI thread themselves (see D3 §IUIDispatcher).
//
// Satisfies ADR-009 Decision §1 (ViewModel → server via transport), ADR-009
// "Debug tab as Observer of structured stream", and ADR-0003 (ACL).

using System;
using System.Text.Json;
using iDaVIE.Client.Gateway;
using iDaVIE.Desktop.DebugTab;

namespace iDaVIE.Desktop.Adapters.DebugTab
{
    /// <summary>
    /// Client-side <see cref="ILogStream"/> implementation that receives entries
    /// from the server via <c>log.emit</c> JSON-RPC notifications and fans them
    /// out to local observers (the Debug tab ViewModel and any future siblings).
    ///
    /// Constructor-injected with the gateway. <see cref="Dispose"/> unsubscribes
    /// from gateway notifications and releases all observers.
    /// </summary>
    public sealed class GatewayLogStreamAdapter : ILogStream, IDisposable
    {
        // ADR-0002 method name — single source of truth so a rename here matches
        // the spec text and the unit tests in step 4.
        internal const string MethodLogEmit = "log.emit";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly IServiceGateway _gateway;
        private readonly LogStream _inner = new();
        private readonly Action<JsonRpcNotification> _handler;
        private bool _disposed;

        public GatewayLogStreamAdapter(IServiceGateway gateway)
        {
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            _handler = OnGatewayNotification;
            _gateway.OnNotification += _handler;
        }

        private void OnGatewayNotification(JsonRpcNotification notification)
        {
            if (!string.Equals(notification.Method, MethodLogEmit, StringComparison.Ordinal))
                return;

            var payload = notification.Deserialize<LogEmitParams>(JsonOptions);
            if (payload is null) return;

            var level = ParseLevel(payload.Level);
            // ADR-0002 example uses ISO 8601: "2026-05-21T09:14:02Z". Tolerate
            // missing or unparseable timestamps — fall back to client time and
            // keep the message rather than dropping it.
            var ts = DateTime.TryParse(payload.Ts, null,
                                       System.Globalization.DateTimeStyles.RoundtripKind,
                                       out var parsed)
                ? parsed.ToUniversalTime()
                : DateTime.UtcNow;

            _inner.Publish(level, payload.Msg ?? string.Empty, ts);
        }

        private static LogLevel ParseLevel(string? wire)
        {
            // ADR-0002 example shows "WARN"; be lenient on case and accept the
            // common aliases so the server-side log producer can use either
            // short or long forms without breaking the client.
            if (string.IsNullOrEmpty(wire)) return LogLevel.Info;
            return wire.Trim().ToUpperInvariant() switch
            {
                "WARN" or "WARNING"             => LogLevel.Warning,
                "ERROR" or "ERR" or "FATAL"     => LogLevel.Error,
                _                               => LogLevel.Info, // INFO, DEBUG, TRACE → Info
            };
        }

        // ── ILogStream delegation ─────────────────────────────────────────────
        //
        // Publish overloads are useful for tests and for any client-side
        // emitter that wants to bypass the gateway (e.g. a fallback bridge for
        // UI Toolkit warnings). They never round-trip back through the gateway —
        // log.emit is server → client only.

        public void Publish(LogLevel level, string message)
            => _inner.Publish(level, message);

        public void Publish(LogLevel level, string message, DateTime timestamp)
            => _inner.Publish(level, message, timestamp);

        public void Subscribe(ILogObserver observer)
            => _inner.Subscribe(observer);

        public void Unsubscribe(ILogObserver observer)
            => _inner.Unsubscribe(observer);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _gateway.OnNotification -= _handler;
        }

        /// <summary>Wire-shape DTO for <c>log.emit</c> params (ADR-0002 §"Message shape").</summary>
        private sealed record LogEmitParams(string? Level, string? Msg, string? Ts);
    }
}
