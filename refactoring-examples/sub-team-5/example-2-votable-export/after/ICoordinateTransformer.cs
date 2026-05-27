/*
 * REFACTORING EXAMPLE 2 — VOTable Export
 * Sub-team 5: Feature System and Domain Model
 *
 * ICoordinateTransformer.cs — AFTER STATE (new file, design-level example)
 * =========================================================================
 * Design role: abstraction boundary over the AstTool native DLL calls.
 *
 * WHY THIS INTERFACE EXISTS
 * ─────────────────────────
 * In the before state, VoTableSaver called AstTool.Transform3D and
 * AstTool.Norm as static methods directly inside the export loop.
 * AstTool is a P/Invoke wrapper around the DataAnalysis.dll native library.
 * This meant:
 *   • The exporter could not run in a test without the DLL present.
 *   • Coordinate values in tests were non-deterministic (real WCS maths).
 *   • The exporter's CBO included couplings to AstTool and AstFrame.
 *
 * ICoordinateTransformer removes both couplings. In production, the concrete
 * AstToolCoordinateTransformer (owned by Sub-team 2, WCS plug-in boundary)
 * wraps the real DLL calls. In tests, a StubCoordinateTransformer returns
 * fixed values so assertions are predictable.
 *
 * CROSS-TEAM BOUNDARY NOTE
 * ────────────────────────
 * This interface is defined here (Sub-team 5) as a stub.
 * The concrete implementation (AstToolCoordinateTransformer) is owned by
 * Sub-team 2 as part of the WCS transform plug-in boundary.
 * The interface name must be agreed between both sub-teams before Sprint 2.
 *
 * CK METRICS (target)
 * ───────────────────
 * WMC  = 2   (two methods)
 * CBO  = 0   (no concrete dependencies)
 * RFC  = 2
 * LCOM = 0
 */

using System;

namespace DataFeatures
{
    /// <summary>
    /// Abstracts the AstTool.Transform3D and AstTool.Norm DLL entry points.
    /// <para>
    /// Implement this interface to wrap the concrete WCS coordinate library.
    /// Inject a stub in unit tests to produce deterministic RA / Dec / Z values
    /// without requiring the DataAnalysis native DLL.
    /// </para>
    /// <para>
    /// The production implementation (<c>AstToolCoordinateTransformer</c>) is
    /// owned by Sub-team 2 (WCS transform plug-in). This interface is the
    /// agreed boundary between Sub-team 5 (feature domain) and Sub-team 2.
    /// </para>
    /// </summary>
    public interface ICoordinateTransformer
    {
        /// <summary>
        /// Converts pixel-space coordinates (x, y, z) to world coordinates
        /// (RA, Dec, spectral value) using the supplied AST frame.
        /// </summary>
        /// <param name="astFrame">
        ///   Opaque handle to the AST World Coordinate System frame.
        ///   In production this is <c>IntPtr</c> obtained from
        ///   <c>VolumeDataSetRenderer.AstFrame</c>.
        ///   Pass <c>IntPtr.Zero</c> with a stub implementation in tests.
        /// </param>
        /// <param name="x">Pixel X coordinate.</param>
        /// <param name="y">Pixel Y coordinate.</param>
        /// <param name="z">Pixel Z coordinate (channel or spectral axis).</param>
        /// <param name="ra">Output: Right Ascension in radians.</param>
        /// <param name="dec">Output: Declination in radians.</param>
        /// <param name="zPhys">Output: Physical spectral value (velocity, frequency, or redshift).</param>
        void Transform(
            IntPtr astFrame,
            double x, double y, double z,
            out double ra, out double dec, out double zPhys);

        /// <summary>
        /// Normalises world coordinates within the AST frame to a canonical range.
        /// Wraps <c>AstTool.Norm</c> in the production implementation.
        /// </summary>
        /// <param name="astFrame">Opaque AST frame handle (see <see cref="Transform"/>).</param>
        /// <param name="ra">Right Ascension in radians (from <see cref="Transform"/>).</param>
        /// <param name="dec">Declination in radians (from <see cref="Transform"/>).</param>
        /// <param name="zPhys">Physical spectral value (from <see cref="Transform"/>).</param>
        /// <param name="normRa">Output: normalised RA in radians.</param>
        /// <param name="normDec">Output: normalised Dec in radians.</param>
        /// <param name="normZ">Output: normalised spectral value.</param>
        void Normalise(
            IntPtr astFrame,
            double ra, double dec, double zPhys,
            out double normRa, out double normDec, out double normZ);
    }
}
