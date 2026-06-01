// brief §6.6 | File tab AFTER — MemoryProbeAdapter (Unity-assembly adapter)
// Wraps UnityEngine.SystemInfo.systemMemorySize so the ViewModel can ask "does this cube fit?" without depending on Unity.
// Satisfies ADR-002 (ACL).
// Replaces the direct SystemInfo call in CanvassDesktop.CheckMemSpaceForCubes

using iDaVIE.Desktop.FileTab;
using UnityEngine;

namespace iDaVIE.Desktop.Adapters.FileTab
{
    // Concrete adapter for IMemoryProbe — the Unity side of the ACL. Returns total system RAM as Unity reports it (in megabytes), converted to bytes, so the VM can run its "does this cube fit?" check without touching UnityEngine. Constructed by FileTabCompositionRoot;
    // It is a plain class and not a MonoBehaviour.
    public sealed class MemoryProbeAdapter : IMemoryProbe
    {
        // Converts Megabyte to Bytes
        public long TotalSystemBytes => (long)SystemInfo.systemMemorySize * 1024L * 1024L;
    }
}
