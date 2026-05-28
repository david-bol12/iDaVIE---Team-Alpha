namespace DataFeatures
{
    /// <summary>
    /// Opaque domain handle for an AST World Coordinate System frame.
    /// Replaces raw IntPtr at the domain boundary (ADR-002 Anti-Corruption Layer).
    /// Production implementation (AstFrameHandle) lives in Infrastructure.NativePlugins.
    /// Pass <see cref="NullAstFrame"/> in unit tests — no unsafe code required.
    /// </summary>
    public interface IAstFrame { }

    /// <summary>
    /// No-op IAstFrame for use in unit tests and as the default on a new FeatureSet.
    /// The production coordinate transformer ignores this and uses the real IntPtr instead.
    /// </summary>
    public sealed class NullAstFrame : IAstFrame { }
}
