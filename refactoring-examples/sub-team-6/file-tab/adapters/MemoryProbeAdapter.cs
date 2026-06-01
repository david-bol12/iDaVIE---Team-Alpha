// brief §6.6 | File tab AFTER — MemoryProbeAdapter (Unity-assembly adapter)
// Wraps UnityEngine.SystemInfo.systemMemorySize so the ViewModel can ask
// "does this cube fit?" without depending on Unity. Satisfies ADR-002 (ACL).
// Replaces the direct SystemInfo call in CanvassDesktop.CheckMemSpaceForCubes
// (line 997, Responsibility Group 6).
using iDaVIE.Desktop.FileTab;
using UnityEngine;

namespace iDaVIE.Desktop.Adapters.FileTab
{
    /// <summary>
    /// Concrete adapter for <see cref="IMemoryProbe"/>.
    /// Returns total system RAM as reported by Unity (in megabytes), converted to bytes.
    /// Constructed by <see cref="FileTabCompositionRoot"/>; pure-C# object, no MonoBehaviour.
    /// </summary>
    public sealed class MemoryProbeAdapter : IMemoryProbe
    {
        public long TotalSystemBytes => (long)SystemInfo.systemMemorySize * 1024L * 1024L;
    }
}
