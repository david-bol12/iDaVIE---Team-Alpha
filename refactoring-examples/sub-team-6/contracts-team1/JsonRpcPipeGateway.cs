// Sub-team 6 — JsonRpcPipeGateway (Gateway Contract v1 §"Wire specification — local mode").
//
// The real-transport implementation of IServiceGateway. Connects to a Windows
// named pipe at \\.\pipe\idavie.<session-id> and speaks JSON-RPC 2.0 framed
// with LengthPrefixFraming.
//
// Skeleton: the read loop, request dispatch, error mapping, and shutdown are
// production-shaped. The server side does not yet exist — Sub-team 1 owns it —
// so end-to-end runs are deferred to integration sprints. Tier-1 tests use
// FakeGateway; tier-2 (transport) tests stub a server-side pipe in-memory.

using System;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace iDaVIE.Client.Gateway
{
    /// <summary>
    /// Named-pipe JSON-RPC 2.0 gateway. One instance per session.
    /// Thread-safe for concurrent <see cref="SendAsync{TResult}"/> calls;
    /// notifications are dispatched on the read-loop thread.
    /// </summary>
    public sealed class JsonRpcPipeGateway : IServiceGateway
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly string _pipeName;
        private readonly NamedPipeClientStream _pipe;
        private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonElement>> _pending = new();
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        private int _nextId;
        private CancellationTokenSource? _shutdown;
        private Task? _readLoopTask;

        public event Action<JsonRpcNotification>? OnNotification;

        /// <param name="pipeName">
        /// Pipe name without the <c>\\.\pipe\</c> prefix. The server writes the
        /// canonical name to <c>%LOCALAPPDATA%\iDaVIE\session.pipe</c> at startup
        /// (see Gateway Contract v1 §"Pipe naming"); the composition root reads it from
        /// there and constructs this gateway.
        /// </param>
        public JsonRpcPipeGateway(string pipeName)
        {
            _pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
            _pipe = new NamedPipeClientStream(
                serverName: ".",
                pipeName: _pipeName,
                direction: PipeDirection.InOut,
                options: PipeOptions.Asynchronous);
        }

        public async Task ConnectAsync(CancellationToken ct = default)
        {
            await _pipe.ConnectAsync(ct).ConfigureAwait(false);
            _shutdown = new CancellationTokenSource();
            _readLoopTask = Task.Run(() => ReadLoopAsync(_shutdown.Token), CancellationToken.None);
        }

        public async Task<TResult> SendAsync<TResult>(string method, object? @params, CancellationToken ct = default)
        {
            if (_shutdown is null)
                throw new InvalidOperationException("Gateway not connected; call ConnectAsync first.");

            var id = Interlocked.Increment(ref _nextId);
            var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[id] = tcs;

            try
            {
                var request = new JsonRpcRequest("2.0", id, method, @params);
                var payload = JsonSerializer.SerializeToUtf8Bytes(request, JsonOptions);
                var frame = LengthPrefixFraming.Encode(payload);

                await _writeLock.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    await _pipe.WriteAsync(frame, ct).ConfigureAwait(false);
                    await _pipe.FlushAsync(ct).ConfigureAwait(false);
                }
                finally
                {
                    _writeLock.Release();
                }

                using var registration = ct.Register(() => tcs.TrySetCanceled(ct));
                var resultEl = await tcs.Task.ConfigureAwait(false);
                var result = resultEl.Deserialize<TResult>(JsonOptions);
                return result!;
            }
            finally
            {
                _pending.TryRemove(id, out _);
            }
        }

        private async Task ReadLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var payload = await LengthPrefixFraming.ReadOneAsync(_pipe, ct).ConfigureAwait(false);
                    DispatchInbound(payload);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Normal shutdown.
            }
            catch (Exception ex)
            {
                // Propagate to any pending callers so they don't hang.
                foreach (var kv in _pending)
                    kv.Value.TrySetException(new InvalidOperationException(
                        "Gateway read loop terminated unexpectedly.", ex));
            }
        }

        private void DispatchInbound(byte[] payload)
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            // Notification: Gateway Contract v1 — "no id, no response expected."
            if (!root.TryGetProperty("id", out var idEl) || idEl.ValueKind == JsonValueKind.Null)
            {
                if (!root.TryGetProperty("method", out var methodEl)) return;
                var method = methodEl.GetString();
                if (method is null) return;
                var prms = root.TryGetProperty("params", out var prmsEl)
                    ? prmsEl.Clone()
                    : (JsonElement?)null;
                OnNotification?.Invoke(new JsonRpcNotification(method, prms));
                return;
            }

            if (idEl.ValueKind != JsonValueKind.Number) return;
            var id = idEl.GetInt32();
            if (!_pending.TryRemove(id, out var tcs)) return;

            if (root.TryGetProperty("error", out var errEl))
            {
                var code = errEl.GetProperty("code").GetInt32();
                var msg = errEl.GetProperty("message").GetString() ?? string.Empty;
                JsonElement? errorData = errEl.TryGetProperty("data", out var dataEl)
                    ? dataEl.Clone()
                    : null;
                tcs.TrySetException(new JsonRpcException(code, msg, errorData));
            }
            else if (root.TryGetProperty("result", out var resEl))
            {
                tcs.TrySetResult(resEl.Clone());
            }
            else
            {
                tcs.TrySetException(new JsonRpcException(
                    code: -32603, // JSON-RPC reserved "internal error"
                    message: $"Response for id {id} had neither result nor error.",
                    errorData: null));
            }
        }

        public async ValueTask DisposeAsync()
        {
            _shutdown?.Cancel();
            if (_readLoopTask is not null)
            {
                try { await _readLoopTask.ConfigureAwait(false); } catch { /* swallowed — shutdown */ }
            }
            await _pipe.DisposeAsync().ConfigureAwait(false);
            _shutdown?.Dispose();
            _writeLock.Dispose();
        }

        private sealed record JsonRpcRequest(
            [property: JsonPropertyName("jsonrpc")] string JsonRpc,
            [property: JsonPropertyName("id")] int Id,
            [property: JsonPropertyName("method")] string Method,
            [property: JsonPropertyName("params")] object? Params);
    }
}
