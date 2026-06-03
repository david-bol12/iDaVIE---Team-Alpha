// JsonRpcException: wraps the server's structured error object (see the "Error
// model" section of the Gateway Contract v1).
//
// The point is that callers can tell "FITS header invalid" (-32011) apart from
// "Dataset id unknown" (-32012) by looking at a code instead of parsing the
// message string. It also carries whatever structured "data" payload the server
// attached to the error.

using System;
using System.Text.Json;

namespace iDaVIE.Client.Gateway
{
    /// <summary>
    /// Thrown by <see cref="IServiceGateway.SendAsync{TResult}"/> when the server
    /// sends back a JSON-RPC error. <see cref="Code"/> matches the table in the
    /// Gateway Contract's "Error model" section, and <see cref="ErrorData"/> holds
    /// the optional structured payload (e.g. <c>{ "axis": 2 }</c> on a FITS-header
    /// error).
    /// </summary>
    /// <remarks>
    /// It's called <c>ErrorData</c> rather than <c>Data</c> so it doesn't shadow
    /// <see cref="Exception.Data"/>, which is the base type's unrelated arbitrary
    /// key/value dictionary.
    /// </remarks>
    public sealed class JsonRpcException : Exception
    {
        /// <summary>Server-side error code (see the contract's Error model section).</summary>
        public int Code { get; }

        /// <summary>Optional structured payload from the server. May be null.</summary>
        public JsonElement? ErrorData { get; }

        public JsonRpcException(int code, string message, JsonElement? errorData)
            : base(message)
        {
            Code = code;
            ErrorData = errorData;
        }
    }
}
