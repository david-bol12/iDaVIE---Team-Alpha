// Sub-team 6 — JsonRpcException (Gateway Contract v1 §"Error model").
//
// Wraps the server's structured error object so callers can distinguish
// "FITS header invalid" (-32011) from "Dataset id unknown" (-32012) without
// parsing strings. The carrying mechanism for the structured "data" payload
// the server attaches.

using System;
using System.Text.Json;

namespace iDaVIE.Client.Gateway
{
    /// <summary>
    /// Thrown by <see cref="IServiceGateway.SendAsync{TResult}"/> when the server
    /// returns a JSON-RPC error object. <see cref="Code"/> matches the table in
    /// Gateway Contract v1 §"Error model"; <see cref="ErrorData"/> carries the optional
    /// structured payload (e.g. <c>{ "axis": 2 }</c> on a FITS-header error).
    /// </summary>
    /// <remarks>
    /// The property is named <c>ErrorData</c> rather than <c>Data</c> to avoid
    /// shadowing <see cref="Exception.Data"/>, which is an unrelated arbitrary
    /// key/value dictionary on the base type.
    /// </remarks>
    public sealed class JsonRpcException : Exception
    {
        /// <summary>Server-side error code (see Gateway Contract v1 §Error model).</summary>
        public int Code { get; }

        /// <summary>Optional structured payload attached by the server. May be null.</summary>
        public JsonElement? ErrorData { get; }

        public JsonRpcException(int code, string message, JsonElement? errorData)
            : base(message)
        {
            Code = code;
            ErrorData = errorData;
        }
    }
}
