// Sub-team 6 — IMenuRouter (ADR-009 Decision §2 · Shared Command Boundary v1).
//
// The single UI-agnostic seam between desktop/VR interaction surfaces and the
// client command layer. Every menu or toolbar action from the shell passes
// through this interface, regardless of whether the user clicked a desktop
// button, pressed a VR controller binding, or triggered a keyboard shortcut.
//
// Pure C#. No UnityEngine reference, no desktop/VR UI reference at the
// interface level — DesktopMenuRouter is one concrete implementation;
// FakeMenuRouter is another. ViewModels and tests depend on this interface only.

using System.Threading;
using System.Threading.Tasks;

namespace iDaVIE.Client.Gateway
{
    /// <summary>
    /// Client-side command seam shared by desktop and VR interaction layers.
    /// Dispatches named UI actions through a transport/UI-agnostic boundary so
    /// higher-level logic does not depend on concrete Unity UI types.
    /// Implementations are chosen at the composition root: a real router for
    /// desktop/VR shells, or a fake router for unit tests.
    /// </summary>
    public interface IMenuRouter
    {
        /// <summary>
        /// Execute a shared UI command such as <c>"file.open"</c>,
        /// <c>"view.togglePaintMode"</c>, or <c>"workspace.save"</c>.
        /// The command vocabulary is shared across desktop and VR so both shells
        /// can trigger the same actions without knowing about each other's UI
        /// widgets or layout structure.
        /// </summary>
        /// <param name="command">Dotted command name, e.g. <c>"file.open"</c>.</param>
        /// <param name="args">Plain object carrying optional command arguments; may be null.</param>
        /// <param name="ct">Cancellation token for the in-flight command only.</param>
        Task RouteAsync(string command, object? args = null, CancellationToken ct = default);

        /// <summary>
        /// Fires after a command has been routed. Useful for shells that want to
        /// observe command traffic for debug overlays, telemetry, or menu-state
        /// synchronisation without coupling to the concrete sender.
        /// </summary>
        event System.Action<MenuCommand>? OnCommandRouted;
    }

    /// <summary>
    /// Immutable record describing a routed shared UI command.
    /// </summary>
    /// <param name="Name">Dotted command name, e.g. <c>"file.open"</c>.</param>
    /// <param name="Args">Optional payload associated with the command.</param>
    public sealed record MenuCommand(string Name, object? Args);
}
