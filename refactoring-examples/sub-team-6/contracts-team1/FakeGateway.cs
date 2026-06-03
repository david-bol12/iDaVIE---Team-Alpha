// Sub-team 6 — FakeGateway (in-process IServiceGateway test double).
//
// Ships in the GatewayContracts assembly (not the tests/ folder) so every
// downstream adapter suite — WE1 file-tab (FitsServiceAdapterTests) and WE2
// debug-tab (GatewayLogStreamAdapterTests) — can exercise the transport seam
// without a real named pipe or a running server. Its behavioural contract is
// pinned by FakeGatewayTests in the sibling tests/ project.
//
// Faithful-enough mimic of JsonRpcPipeGateway:
//   - SendAsync before ConnectAsync is a programmer error (InvalidOperationException).
//   - Every call is recorded in Sent (method + camelCase params element) so tests
//     can assert the exact JSON-RPC method/param shape from Gateway Contract v1.
//   - SetResponse / SetError stub the server side; an unstubbed method throws an
//     InvalidOperationException naming the method (the "forgot to stub" trap).
//   - Params/response are round-tripped through System.Text.Json with the same
//     CamelCase policy the real pipe uses, so DTO (de)serialisation is exercised.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace iDaVIE.Client.Gateway
{
    /// <summary>
    /// In-memory <see cref="IServiceGateway"/> for unit tests. Stub responses with
    /// <see cref="SetResponse{T}"/> / <see cref="SetError"/>, push server
    /// notifications with <see cref="EmitNotification"/>, and inspect outbound
    /// calls via <see cref="Sent"/>.
    /// </summary>
    public sealed class FakeGateway : IServiceGateway
    {
        // Mirrors JsonRpcPipeGateway.JsonOptions: CamelCase on the wire, tolerant
        // casing on read so DTO round-trips match the real transport.
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

        /// <summary>A single outbound request recorded by the fake.</summary>
        public sealed record SentCall(string Method, JsonElement? Params);

        /// <summary>Outbound calls in dispatch order (snapshot — safe to enumerate).</summary>
        public IReadOnlyList<SentCall> Sent
        {
            get { lock (_lock) return _sent.ToArray(); }
        }

        // ── Stubbing API ───────────────────────────────────────────────────────

        /// <summary>Stub the result returned for <paramref name="method"/>.</summary>
        public void SetResponse<T>(string method, T response)
        {
            lock (_lock)
            {
                _responses[method] = JsonSerializer.SerializeToElement(response, JsonOpts);
                _errors.Remove(method);
            }
        }

        /// <summary>Stub a JSON-RPC error for <paramref name="method"/>.</summary>
        public void SetError(string method, int code, string message)
        {
            lock (_lock)
            {
                _errors[method] = (code, message);
                _responses.Remove(method);
            }
        }

        /// <summary>Fire a server-pushed notification to all <see cref="OnNotification"/> subscribers.</summary>
        public void EmitNotification(string method, object? payload)
        {
            JsonElement? p = payload is null
                ? null
                : JsonSerializer.SerializeToElement(payload, JsonOpts);
            OnNotification?.Invoke(new JsonRpcNotification(method, p));
        }

        // ── IServiceGateway ────────────────────────────────────────────────────

        public Task ConnectAsync(CancellationToken ct = default)
        {
            _connected = true;
            return Task.CompletedTask;
        }

        public Task<TResult> SendAsync<TResult>(string method, object? @params, CancellationToken ct = default)
        {
            if (!_connected)
                throw new InvalidOperationException(
                    $"FakeGateway is not connected — call ConnectAsync() before SendAsync (method '{method}').");

            // Record the call FIRST so an errored call still shows up in Sent
            // (the GetAxesFails path asserts file.open + dataset.getAxes + file.close).
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
