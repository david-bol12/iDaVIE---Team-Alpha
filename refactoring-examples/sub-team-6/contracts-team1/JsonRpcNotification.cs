// Sub-team 6 — JsonRpcNotification DTO (ADR-0002 §"Message shape").
//
// A server-initiated JSON-RPC message — no id, no response expected.
// Carries the raw params element so the subscriber can deserialise lazily
// into its own DTO type (the gateway has no compile-time knowledge of every
// notification payload type).

using System.Text.Json;

namespace iDaVIE.Client.Gateway
{
    /// <summary>
    /// A JSON-RPC 2.0 notification. The gateway raises one per inbound
    /// server-pushed message.
    /// </summary>
    /// <param name="Method">Dotted method name, e.g. <c>"log.emit"</c>.</param>
    /// <param name="Params">Raw <c>params</c> element, or null if the server sent none. Use <see cref="Deserialize{T}"/> to convert.</param>
    public sealed record JsonRpcNotification(string Method, JsonElement? Params)
    {
        /// <summary>
        /// Deserialise <see cref="Params"/> into a subscriber-owned DTO type.
        /// Returns <c>default</c> if the server sent no params.
        /// </summary>
        public T? Deserialize<T>(JsonSerializerOptions? options = null)
            => Params.HasValue ? Params.Value.Deserialize<T>(options) : default;
    }
}
