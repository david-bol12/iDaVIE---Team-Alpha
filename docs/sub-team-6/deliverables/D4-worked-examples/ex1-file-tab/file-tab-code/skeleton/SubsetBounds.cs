// WE1-3 | File tab AFTER skeleton — SubsetBounds DTO
// Plain data transfer object; owned and validated by SubsetBoundsViewModel.
// Passed into LoadCubeRequest when the user has enabled the subset selector.
// No UnityEngine dependency.
namespace iDaVIE.Desktop.FileTab
{
    /// <summary>
    /// Six-axis subset selection. Mutable DTO; created via
    /// <see cref="SubsetBoundsViewModel.ToDto"/> to snapshot validated state.
    /// </summary>
    public sealed class SubsetBounds
    {
        public int XMin { get; set; }
        public int XMax { get; set; }
        public int YMin { get; set; }
        public int YMax { get; set; }
        public int ZMin { get; set; }
        public int ZMax { get; set; }
    }
}
