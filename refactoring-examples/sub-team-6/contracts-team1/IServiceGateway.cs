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
    /// Talks to the iDaVIE server. Sends typed JSON-RPC requests and hands back any notifications the server pushes. The composition root decides which
    public interface IServiceGateway : System.IAsyncDisposable
    {
        /// Open the transport. The named-pipe implementation connects to the server's listening pipe; the fake just flips an in-memory connected flag.
        Task ConnectAsync(CancellationToken ct = default);

        /// Send a JSON-RPC request and wait for the typed result. The wire format and method names come from the "Method catalogue (v1)" section of the Gateway Contract; the namespaces are file.*, dataset.*,
        Task<TResult> SendAsync<TResult>(string method, object? @params, CancellationToken ct = default);

        /// Raised for every notification the server sends (a JSON-RPC message with no id). The Debug tab listens here for log.emit records, and the File tab listens for progress.update.
        event System.Action<JsonRpcNotification>? OnNotification;
    }
}
