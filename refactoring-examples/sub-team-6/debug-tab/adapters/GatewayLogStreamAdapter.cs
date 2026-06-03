// Gateway notifications fire on the read-loop thread; UI-bound observers must
// marshal to the main thread themselves before touching Unity state.
using System;
using System.Text.Json;
using iDaVIE.Client.Gateway;
using iDaVIE.Desktop.DebugTab;

namespace iDaVIE.Desktop.Adapters.DebugTab
{
    // ILogStream that receives entries via log.emit JSON-RPC notifications and fans them out to local observers.
    public sealed class GatewayLogStreamAdapter : ILogStream, IDisposable
    {
        // Single source of truth — tests reference this constant too.
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
            // Fall back to client time on a missing/bad ts; prefer keeping the entry.
            var ts = DateTime.TryParse(payload.Ts, null,
                                       System.Globalization.DateTimeStyles.RoundtripKind,
                                       out var parsed)
                ? parsed.ToUniversalTime()
                : DateTime.UtcNow;

            _inner.Publish(level, payload.Msg ?? string.Empty, ts);
        }

        private static LogLevel ParseLevel(string? wire)
        {
            // Accept both short ("WARN") and long ("WARNING") forms case-insensitively.
            if (string.IsNullOrEmpty(wire)) return LogLevel.Info;
            return wire.Trim().ToUpperInvariant() switch
            {
                "WARN" or "WARNING"             => LogLevel.Warning,
                "ERROR" or "ERR" or "FATAL"     => LogLevel.Error,
                _                               => LogLevel.Info, // INFO, DEBUG, TRACE → Info
            };
        }

        // Publish overloads let tests and client-side emitters inject entries directly;
        // log.emit is server→client only so these never round-trip through the gateway.

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

        // Wire-shape DTO for log.emit params (Gateway Contract v1 "Message shape").
        private sealed record LogEmitParams(string? Level, string? Msg, string? Ts);
    }
}
