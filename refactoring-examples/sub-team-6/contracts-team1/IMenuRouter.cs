// IMenuRouter: the UI-agnostic seam between the desktop/VR surfaces and the
// client command layer. (Background: ADR-009 decision 2, Shared Command
// Boundary v1.)
//
// Any menu or toolbar action from the shell goes through here, and it doesn't
// matter whether the user clicked a desktop button, pressed a VR controller
// binding, or hit a keyboard shortcut.
//
// Same deal as the gateway: no UnityEngine or desktop/VR UI types referenced at
// this level. DesktopMenuRouter is the real implementation, FakeMenuRouter is
// the test one, and ViewModels/tests only see this interface.

using System.Threading;
using System.Threading.Tasks;

namespace iDaVIE.Client.Gateway
{
    /// <summary>
    /// The command seam shared by the desktop and VR layers. It dispatches named
    /// UI actions across a boundary that knows nothing about Unity UI types, so
    /// higher-level logic stays decoupled from the actual widgets. The
    /// composition root wires up a real router for the shells, or a fake one for
    /// unit tests.
    /// </summary>
    public interface IMenuRouter
    {
        /// <summary>
        /// Run a shared UI command, e.g. <c>"file.open"</c>,
        /// <c>"view.togglePaintMode"</c> or <c>"workspace.save"</c>. Desktop and
        /// VR share the same command vocabulary, so both shells can fire the same
        /// action without knowing anything about each other's widgets or layout.
        /// </summary>
        /// <param name="command">Dotted command name, e.g. <c>"file.open"</c>.</param>
        /// <param name="args">Plain object with any command arguments; can be null.</param>
        /// <param name="ct">Cancellation token; only cancels this one command.</param>
        Task RouteAsync(string command, object? args = null, CancellationToken ct = default);

        /// <summary>
        /// Raised after a command has been routed. Handy for shells that want to
        /// watch command traffic (debug overlays, telemetry, keeping menu state in
        /// sync) without being coupled to whoever sent it.
        /// </summary>
        event System.Action<MenuCommand>? OnCommandRouted;
    }

    /// <summary>
    /// Immutable record for a routed UI command.
    /// </summary>
    /// <param name="Name">Dotted command name, e.g. <c>"file.open"</c>.</param>
    /// <param name="Args">Optional payload associated with the command.</param>
    public sealed record MenuCommand(string Name, object? Args);
}
