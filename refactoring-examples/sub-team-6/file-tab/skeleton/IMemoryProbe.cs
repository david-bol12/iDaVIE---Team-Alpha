// brief §6.6 | File tab AFTER skeleton — IMemoryProbe (ACL boundary)
// Abstracts over UnityEngine.SystemInfo so the ViewModel can reason about
// RAM availability without referencing Unity. Replaces the direct
// SystemInfo.systemMemorySize / FileInfo.Length calls in
// CanvassDesktop.CheckMemSpaceForCubes (lines 995-1013, Responsibility Group 6).
// No UnityEngine dependency. Satisfies ADR-003 (DI) and ADR-002 (ACL).
namespace iDaVIE.Desktop.FileTab
{
    // The anti-corruption boundary for host-memory queries used by load-feasibility checks. The VM depends on this, not on UnityEngine.SystemInfo — so the Unity dependency stays in the adapter (MemoryProbeAdapter) and the VM stays testable with a fake.
    public interface IMemoryProbe
    {
        // Total system RAM in bytes (not "available" — matches the BEFORE behaviour).
        long TotalSystemBytes { get; }
    }
}
