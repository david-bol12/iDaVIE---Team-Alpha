// Sub-team 6 — FakeGateway (in-process IServiceGateway for tier-1 tests).
//
// Lets tests pre-program responses by method name, assert on what was sent,
// and emit server-pushed notifications synchronously — without standing up
// a real named pipe. Used by:
//   - FileTabViewModelTests (once the adapter is rewired through the gateway)
//   - DebugTabTests (once the log adapter is rewired through the gateway)
//   - Future tier-2 transport tests that bypass IO entirely.
//
// Behaviour mirrors JsonRpcPipeGateway:
//   - SendAsync rejects calls made before ConnectAsync.
//   - Unhandled methods throw (catches the "test forgot to stub" mistake early).
//   - Errors are surfaced via JsonRpcException with the same shape as the real
//     transport.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace iDaVIE.Client.Gateway
{
    /// <summary>
    /// In-process <see cref="IServiceGateway"/> for unit tests.
    /// Not thread-safe with respect to handler registration — register all
    /// handlers before calling <see cref="ConnectAsync"/>. Concurrent
    /// <see cref="SendAsync{TResult}"/> calls are safe.
    /// </summary>
    public sealed class FakeGateway : IServiceGateway
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly ConcurrentDictionary<string, Queue<Func<JsonElement?, object?>>> _handlers = new();
        private readonly ConcurrentQueue<SentCall> _sent = new();

        public event Action<JsonRpcNotification>? OnNotification;

        public bool IsConnected { get; private set; }

        /// <summary>Read-only log of every <see cref="SendAsync{TResult}"/> call, in order.</summary>
        public IReadOnlyCollection<SentCall> Sent => _sent;

        public Task ConnectAsync(CancellationToken ct = default)
        {
            IsConnected = true;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Register a one-shot result for the next call to <paramref name="method"/>.
        /// Each call to <see cref="SendAsync{TResult}"/> dequeues one handler.
        /// </summary>
        public void SetResponse<TResult>(string method, TResult result)
        {
            EnqueueHandler(method, _ => result);
        }

        /// <summary>
        /// Register a one-shot result that depends on the inbound <c>params</c>.
        /// The handler receives the params element as a strongly-typed DTO.
        /// </summary>
        public void SetResponse<TParams, TResult>(string method, Func<TParams?, TResult> handler)
        {
            EnqueueHandler(method, prms =>
            {
                var typed = prms.HasValue ? prms.Value.Deserialize<TParams>(JsonOptions) : default;
                return handler(typed);
            });
        }

        /// <summary>
        /// Register a one-shot error response for the next call to <paramref name="method"/>.
        /// Codes match ADR-0002 §"Error model" — e.g. -32011 for invalid FITS header.
        /// </summary>
        public void SetError(string method, int code, string message, JsonElement? errorData = null)
        {
            EnqueueHandler(method, _ => throw new JsonRpcException(code, message, errorData));
        }

        public Task<TResult> SendAsync<TResult>(string method, object? @params, CancellationToken ct = default)
        {
            if (!IsConnected)
                throw new InvalidOperationException("FakeGateway not connected; call ConnectAsync first.");

            ct.ThrowIfCancellationRequested();

            JsonElement? prmsEl = @params is null
                ? null
                : JsonSerializer.SerializeToElement(@params, JsonOptions);
            _sent.Enqueue(new SentCall(method, prmsEl));

            if (!_handlers.TryGetValue(method, out var queue) || queue.Count == 0)
            {
                throw new InvalidOperationException(
                    $"FakeGateway has no handler registered for method '{method}'. " +
                    $"Use SetResponse / SetError before the call.");
            }

            var handler = queue.Dequeue();
            var raw = handler(prmsEl);

            // Direct hand-off when types match — avoids an avoidable JSON
            // round-trip when the test produces the same named type the
            // adapter expects.
            if (raw is TResult typed) return Task.FromResult(typed);

            // Otherwise round-trip through JSON. This mirrors the real
            // JsonRpcPipeGateway: the wire deserialises by property name
            // under the camelCase naming policy. Lets tests use anonymous
            // types ("new { nAxis = 3 }") that match the wire shape without
            // depending on the adapter's private DTO type.
            var json = JsonSerializer.SerializeToUtf8Bytes(raw, JsonOptions);
            var deserialised = JsonSerializer.Deserialize<TResult>(json, JsonOptions);
            return Task.FromResult(deserialised!);
        }

        /// <summary>
        /// Synthesise a server-pushed notification. Subscribers receive it on the
        /// calling thread (cf. the real transport, which dispatches on the read
        /// loop thread — tests that depend on threading should use the real
        /// gateway against an in-memory pipe).
        /// </summary>
        public void EmitNotification(string method, object? @params = null)
        {
            JsonElement? prmsEl = @params is null
                ? null
                : JsonSerializer.SerializeToElement(@params, JsonOptions);
            OnNotification?.Invoke(new JsonRpcNotification(method, prmsEl));
        }

        public ValueTask DisposeAsync()
        {
            IsConnected = false;
            return ValueTask.CompletedTask;
        }

        private void EnqueueHandler(string method, Func<JsonElement?, object?> handler)
        {
            var queue = _handlers.GetOrAdd(method, _ => new Queue<Func<JsonElement?, object?>>());
            lock (queue) queue.Enqueue(handler);
        }

        /// <summary>One recorded <see cref="SendAsync{TResult}"/> invocation.</summary>
        public sealed record SentCall(string Method, JsonElement? Params);
    }
}
