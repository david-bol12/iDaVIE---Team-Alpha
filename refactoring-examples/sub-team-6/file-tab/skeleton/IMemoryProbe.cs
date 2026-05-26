// WE1-3 | File tab AFTER skeleton — IMemoryProbe (ACL boundary)
// Abstracts over UnityEngine.SystemInfo so the ViewModel can reason about
// RAM availability without referencing Unity. Replaces the direct
// SystemInfo.systemMemorySize / FileInfo.Length calls in
// CanvassDesktop.CheckMemSpaceForCubes (lines 995-1013, Responsibility Group 6).
// No UnityEngine dependency. Satisfies ADR-0001 (DIP) and ADR-0003 (ACL).
namespace iDaVIE.Desktop.FileTab
{
    /// <summary>
    /// Domain interface for host-memory queries used by load-feasibility checks.
    /// Implemented in the Unity assembly by <c>MemoryProbeAdapter</c>.
    /// </summary>
    public interface IMemoryProbe
    {
        /// <summary>Total system RAM in bytes (not "available" — matches BEFORE behaviour).</summary>
        long TotalSystemBytes { get; }
    }
}
