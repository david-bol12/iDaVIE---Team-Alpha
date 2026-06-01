// brief §6.6 | File tab AFTER skeleton — SubsetBounds DTO
// Plain data transfer object; owned and validated by SubsetBoundsViewModel.
// Passed into LoadCubeRequest when the user has enabled the subset selector.
// No UnityEngine dependency.
namespace iDaVIE.Desktop.FileTab
{
    // The six X/Y/Z min-max bounds of a crop region — a flat snapshot of the values, with no validation of its own.
    // SubsetBoundsViewModel does the clamping/validating and produces one of these via ToDto() to hand to LoadCubeRequest once the user confirms a load.
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
