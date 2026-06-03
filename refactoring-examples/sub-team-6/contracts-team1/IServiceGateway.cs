// IServiceGateway: the one seam between the ViewModels and the server.
// (Background: ADR-009 decision 1, and the Gateway Contract v1 wire spec.)
//
// Everything the desktop client sends across the process boundary comes through
// here. That's both normal request/response calls (file.open, dataset.getAxes,
// and so on) and the notifications the server pushes back at us on its own
// (log.emit, progress.update).
//
// There's deliberately no UnityEngine or transport library referenced at this
// level. JsonRpcPipeGateway is the real implementation, FakeGateway is the one
// we use in tests, and ViewModels/tests only ever see this interface.

using System.Threading;
using System.Threading.Tasks;

namespace iDaVIE.Client.Gateway
{
    /// <summary>
    /// Talks to the iDaVIE server. Sends typed JSON-RPC requests and hands back
    /// any notifications the server pushes. The composition root decides which
    /// implementation to wire up: <see cref="JsonRpcPipeGateway"/> for the real
    /// named-pipe transport, or <see cref="FakeGateway"/> in unit tests.
    /// </summary>
    public interface IServiceGateway : System.IAsyncDisposable
    {
        /// <summary>
        /// Open the transport. The named-pipe implementation connects to the
        /// server's listening pipe; the fake just flips an in-memory connected
        /// flag. Call this before <see cref="SendAsync{TResult}"/>.
        /// </summary>
        Task ConnectAsync(CancellationToken ct = default);

        /// <summary>
        /// Send a JSON-RPC request and wait for the typed result. The wire format
        /// and method names come from the "Method catalogue (v1)" section of the
        /// Gateway Contract; the namespaces are <c>file.*</c>, <c>dataset.*</c>,
        /// <c>log.*</c> and <c>progress.*</c>. If the server returns an error you
        /// get a <see cref="JsonRpcException"/>.
        /// </summary>
        /// <typeparam name="TResult">DTO type to deserialise <c>result</c> into.</typeparam>
        /// <param name="method">Dotted method name, e.g. <c>"file.open"</c>.</param>
        /// <param name="params">Plain object that becomes the <c>params</c> field; can be null.</param>
        /// <param name="ct">Cancellation token; only cancels this one request.</param>
        Task<TResult> SendAsync<TResult>(string method, object? @params, CancellationToken ct = default);

        /// <summary>
        /// Raised for every notification the server sends (a JSON-RPC message with
        /// no <c>id</c>). The Debug tab listens here for <c>log.emit</c> records,
        /// and the File tab listens for <c>progress.update</c>.
        /// </summary>
        event System.Action<JsonRpcNotification>? OnNotification;
    }
}
