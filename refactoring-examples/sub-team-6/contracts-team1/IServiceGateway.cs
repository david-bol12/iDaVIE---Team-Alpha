// Sub-team 6 — IServiceGateway (ADR-009 Decision §1 · ADR-0002 wire spec).
//
// The single transport-agnostic seam between the ViewModel layer and the server
// kernel. Every cross-process call from the desktop client passes through this
// interface — both request/response (file.open, dataset.getAxes, ...) and
// server-pushed notifications (log.emit, progress.update).
//
// Pure C#. No UnityEngine reference, no transport library reference at the
// interface level — JsonRpcPipeGateway is one concrete implementation;
// FakeGateway is another. ViewModels and tests depend on this interface only.

using System.Threading;
using System.Threading.Tasks;

namespace iDaVIE.Client.Gateway
{
    /// <summary>
    /// Client-side transport seam to the iDaVIE server. Sends typed JSON-RPC
    /// requests and surfaces server-pushed notifications. Implementations are
    /// chosen at the composition root: <see cref="JsonRpcPipeGateway"/> for the
    /// real named-pipe transport, <see cref="FakeGateway"/> for unit tests.
    /// </summary>
    public interface IServiceGateway : System.IAsyncDisposable
    {
        /// <summary>
        /// Open the underlying transport. For the named-pipe implementation this
        /// connects to the server's listening pipe; for the fake implementation it
        /// flips an in-memory <c>IsConnected</c> flag. Must be called before
        /// <see cref="SendAsync{TResult}"/>.
        /// </summary>
        Task ConnectAsync(CancellationToken ct = default);

        /// <summary>
        /// Send a JSON-RPC request and await the strongly-typed result. The wire
        /// format and method-name discipline are defined by ADR-0002 §"Method
        /// catalogue (v1)" — namespaces are <c>file.*</c>, <c>dataset.*</c>,
        /// <c>log.*</c>, <c>progress.*</c>.
        /// Server errors surface as <see cref="JsonRpcException"/>.
        /// </summary>
        /// <typeparam name="TResult">DTO type deserialised from <c>result</c>.</typeparam>
        /// <param name="method">Dotted method name, e.g. <c>"file.open"</c>.</param>
        /// <param name="params">Plain object serialised as the <c>params</c> field; may be null.</param>
        /// <param name="ct">Cancellation token; aborts the in-flight request only.</param>
        Task<TResult> SendAsync<TResult>(string method, object? @params, CancellationToken ct = default);

        /// <summary>
        /// Fires for every server-initiated notification (a JSON-RPC message with
        /// no <c>id</c> field). The Debug tab subscribes to this to receive
        /// <c>log.emit</c> records; the File tab subscribes to it for
        /// <c>progress.update</c>.
        /// </summary>
        event System.Action<JsonRpcNotification>? OnNotification;
    }
}
