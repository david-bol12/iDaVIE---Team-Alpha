// FakeGateway: an in-process IServiceGateway test double.
//
// This lives in the GatewayContracts assembly itself, not under tests/, so that
// the downstream adapter suites can use it too: the WE1 file-tab tests
// (FitsServiceAdapterTests) and the WE2 debug-tab tests
// (GatewayLogStreamAdapterTests). That way they can hit the transport seam
// without a real named pipe or a running server. Its own behaviour is pinned
// down by FakeGatewayTests over in the sibling tests/ project.
//
// It tries to behave like JsonRpcPipeGateway closely enough to be useful:
//   - Calling SendAsync before ConnectAsync is a bug, so it throws
//     InvalidOperationException.
//   - Every call gets recorded in Sent (method plus the camelCase params), so
//     tests can check the exact method/param shape from the Gateway Contract.
//   - SetResponse / SetError stub out the server side. Hit a method nobody
//     stubbed and you get an InvalidOperationException that names it - that's
//     the "you forgot to stub this" trap.
//   - Params and responses go through System.Text.Json with the same CamelCase
//     policy the real pipe uses, so DTO (de)serialisation actually gets exercised.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace iDaVIE.Client.Gateway
{
    /// <summary>
    /// In-memory <see cref="IServiceGateway"/> for unit tests. Stub responses
    /// with <see cref="SetResponse{T}"/> or <see cref="SetError"/>, push server
    /// notifications with <see cref="EmitNotification"/>, and check what went out
    /// via <see cref="Sent"/>.
    /// </summary>
    public sealed class FakeGateway : IServiceGateway
    {
        // Same settings as JsonRpcPipeGateway.JsonOptions: CamelCase on the wire,
        // case-insensitive on read, so DTO round-trips match the real transport.
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };

        private readonly object _lock = new();
        private readonly List<SentCall> _sent = new();
        private readonly Dictionary<string, JsonElement> _responses = new(StringComparer.Ordinal);
        private readonly Dictionary<string, (int Code, string Message)> _errors = new(StringComparer.Ordinal);

        private bool _connected;

        public event Action<JsonRpcNotification>? OnNotification;

        /// One outbound request the fake recorded.
        public sealed record SentCall(string Method, JsonElement? Params);

        /// Outbound calls in the order they were sent. This is a snapshot, so it's safe to enumerate.
        public IReadOnlyList<SentCall> Sent
        {
            get { lock (_lock) return _sent.ToArray(); }
        }

        // Stubbing API
        public void SetResponse<T>(string method, T response)
        {
            lock (_lock)
            {
                _responses[method] = JsonSerializer.SerializeToElement(response, JsonOpts);
                _errors.Remove(method);
            }
        }

        public void SetError(string method, int code, string message)
        {
            lock (_lock)
            {
                _errors[method] = (code, message);
                _responses.Remove(method);
            }
        }

        /// <summary>Fire a server-pushed notification at every <see cref="OnNotification"/> subscriber.</summary>
        public void EmitNotification(string method, object? payload)
        {
            JsonElement? p = payload is null
                ? null
                : JsonSerializer.SerializeToElement(payload, JsonOpts);
            OnNotification?.Invoke(new JsonRpcNotification(method, p));
        }

        // IServiceGateway implementation

        public Task ConnectAsync(CancellationToken ct = default)
        {
            _connected = true;
            return Task.CompletedTask;
        }

        public Task<TResult> SendAsync<TResult>(string method, object? @params, CancellationToken ct = default)
        {
            if (!_connected)
                throw new InvalidOperationException(
                    $"FakeGateway is not connected; call ConnectAsync() before SendAsync (method '{method}').");

            // Record the call before anything else, so a call that errors still
            // shows up in Sent. The GetAxesFails test relies on this - it checks
            // that file.open, dataset.getAxes and file.close were all sent.
            JsonElement? paramsElement = @params is null
                ? null
                : JsonSerializer.SerializeToElement(@params, JsonOpts);
            lock (_lock) _sent.Add(new SentCall(method, paramsElement));

            (int Code, string Message) error;
            JsonElement response;
            lock (_lock)
            {
                if (_errors.TryGetValue(method, out error))
                    throw new JsonRpcException(error.Code, error.Message, errorData: null);

                if (!_responses.TryGetValue(method, out response))
                    throw new InvalidOperationException(
                        $"FakeGateway has no stubbed response for method '{method}'. " +
                        $"Add gateway.SetResponse(\"{method}\", ...) or SetError(\"{method}\", ...) in the test setup.");
            }

            return Task.FromResult(response.Deserialize<TResult>(JsonOpts)!);
        }

        public ValueTask DisposeAsync()
        {
            OnNotification = null;
            _connected = false;
            return ValueTask.CompletedTask;
        }
    }
}
