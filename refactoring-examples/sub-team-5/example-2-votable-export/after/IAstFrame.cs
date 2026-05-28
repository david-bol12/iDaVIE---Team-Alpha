/*
 * REFACTORING EXAMPLE 2 — VOTable Export
 * Sub-team 5: Feature System and Domain Model
 *
 * IAstFrame.cs — AFTER STATE (new file, design-level example)
 * ===========================================================
 * Design role: domain-level handle abstraction over the native AST frame pointer.
 *
 * WHY THIS INTERFACE EXISTS (ADR-002 Anti-Corruption Layer)
 * ──────────────────────────────────────────────────────────
 * In the before state, ICoordinateTransformer.Transform() and .Normalise()
 * both accepted an IntPtr as their first parameter — the raw P/Invoke handle
 * to the native DataAnalysis.dll AST frame object.
 *
 * IntPtr is an unmanaged-memory pointer type. Exposing it in a domain-layer
 * interface means:
 *   • Domain code carries a dependency on the native-plug-in boundary type.
 *   • Unit tests must manufacture or fake an unsafe pointer — unsafe code in tests.
 *   • The ACL (ADR-002) rule "zero native types above Infrastructure" is violated.
 *
 * IAstFrame is the fix: an opaque domain marker interface that Domain and
 * Application code can hold a reference to without knowing anything about
 * unmanaged memory. The Infrastructure implementation wraps the real IntPtr.
 *
 * LAYER OWNERSHIP
 * ───────────────
 * Interface defined here  → iDaVIE.Domain.Feature
 * Concrete implementation → iDaVIE.Infrastructure.NativePlugins
 *                            (class AstFrameHandle : IAstFrame, holds IntPtr)
 *
 * In unit tests, pass a stub:
 *   public sealed class NullAstFrame : IAstFrame { }
 *
 * CK METRICS
 * ──────────
 * WMC  = 0   (marker interface, no methods)
 * CBO  = 0   (no dependencies)
 * RFC  = 0
 * LCOM = 0
 */

// DOCUMENTATION COPY — NOT COMPILED AS PART OF THE UNITY PROJECT.
// The authoritative production file is Assets/Scripts/FeatureData/IAstFrame.cs.
// This copy exists only to make Example 2 self-contained for readers.
// If the production IAstFrame changes, update this file to match.

namespace iDaVIE.Domain.Feature
{
    /// <summary>
    /// Opaque domain handle for an AST World Coordinate System frame.
    /// <para>
    /// Replaces the raw <c>IntPtr</c> that previously leaked the
    /// <c>DataAnalysis.dll</c> P/Invoke boundary into domain-layer interfaces
    /// (ADR-002 Anti-Corruption Layer violation).
    /// </para>
    /// <para>
    /// This is an intentionally empty marker interface. Domain and Application
    /// code holds an <see cref="IAstFrame"/> reference and passes it to
    /// <see cref="ICoordinateTransformer"/> — neither layer ever sees an
    /// <c>IntPtr</c> or unsafe code.
    /// </para>
    /// <para>
    /// The production implementation (<c>AstFrameHandle</c>) lives in
    /// <c>iDaVIE.Infrastructure.NativePlugins</c> and wraps the real
    /// <c>IntPtr</c> from <c>VolumeDataSetRenderer.AstFrame</c>.
    /// </para>
    /// </summary>
    public interface IAstFrame
    {
        // Intentionally empty — opaque handle pattern.
        // Concrete types own the IntPtr; the domain layer never touches it.
    }
}
