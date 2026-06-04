// JsonRpcNotification DTO (see the "Message shape" section of the Gateway
// Contract v1).
//
// A message the server starts on its own: no id, and no response expected. We
// keep the params as a raw element so the subscriber can deserialise it lazily
// into whatever DTO it wants. The gateway can't know every notification payload
// type up front, so it doesn't try.

using System.Text.Json;

namespace iDaVIE.Client.Gateway
{
    /// <summary>
    /// A JSON-RPC 2.0 notification. The gateway raises one of these for each
    /// message the server pushes.
    /// </summary>
    /// <param name="Method">Dotted method name, e.g. <c>"log.emit"</c>.</param>
    /// <param name="Params">Raw <c>params</c> element, or null if the server sent none. Call <see cref="Deserialize{T}"/> to convert it.</param>
    public sealed record JsonRpcNotification(string Method, JsonElement? Params)
    {
        /// <summary>
        /// Deserialise <see cref="Params"/> into whatever DTO the subscriber owns.
        /// Returns <c>default</c> if the server didn't send any params.
        /// </summary>
        public T? Deserialize<T>(JsonSerializerOptions? options = null)
            => Params.HasValue ? Params.Value.Deserialize<T>(options) : default;
    }
}
