// brief §6.6 | File tab AFTER skeleton — SubsetBoundsViewModel
// Extracted from three methods in CanvassDesktop:
//   checkSubsetBounds()  → per-property setters with clamping
//   setSubsetBounds()    → ResetToAxisMaxima()
//   updateSubsetZMax()   → UpdateZAxisMax()
// Pure C# — no UnityEngine dependency. Satisfies NFR-MOD-2 and ADR-009 (MVVM).
using System.ComponentModel;
using System.Runtime.CompilerServices;
namespace iDaVIE.Desktop.FileTab
{
    /// <summary>
    /// ViewModel for the six-axis subset selector panel.
    /// Each bound property self-clamps on set and raises
    /// <see cref="INotifyPropertyChanged.PropertyChanged"/> so the View can
    /// update its input fields without any manual transform.Find() wiring.
    /// </summary>
    public sealed class SubsetBoundsViewModel : INotifyPropertyChanged
    {
        private int _xMin, _xMax, _yMin, _yMax, _zMin, _zMax;
        private int _maxX = 1, _maxY = 1, _maxZ = 1;
        private const int AbsoluteMin = 1;

        public event PropertyChangedEventHandler? PropertyChanged;

        // ── Axis maxima ───────────────────────────────────────────────────────
        public int MaxX => _maxX;
        public int MaxY => _maxY;
        public int MaxZ => _maxZ;

        // ── Bound properties ──────────────────────────────────────────────────
        // Each setter clamps to valid range and raises PropertyChanged.
        // The View input fields bind two-way; clamping replaces the manual
        // error-correction logic in CanvassDesktop.checkSubsetBounds.

        public int XMin
        {
            get => _xMin;
            set { _xMin = Math.Max(AbsoluteMin, Math.Min(_xMax, value)); Notify(); }
        }
        public int XMax
        {
            get => _xMax;
            set { _xMax = Math.Max(_xMin, Math.Min(_maxX, value)); Notify(); }
        }
        public int YMin
        {
            get => _yMin;
            set { _yMin = Math.Max(AbsoluteMin, Math.Min(_yMax, value)); Notify(); }
        }
        public int YMax
        {
            get => _yMax;
            set { _yMax = Math.Max(_yMin, Math.Min(_maxY, value)); Notify(); }
        }
        public int ZMin
        {
            get => _zMin;
            set { _zMin = Math.Max(AbsoluteMin, Math.Min(_zMax, value)); Notify(); }
        }
        public int ZMax
        {
            get => _zMax;
            set { _zMax = Math.Max(_zMin, Math.Min(_maxZ, value)); Notify(); }
        }

        // ── Mutating operations ───────────────────────────────────────────────

        /// <summary>
        /// Resets all bounds to [1 … axisMax] for each axis.
        /// Called by FileTabViewModel after a new FITS image is successfully opened.
        /// Replaces CanvassDesktop.setSubsetBounds().
        /// </summary>
        public void ResetToAxisMaxima(int maxX, int maxY, int maxZ)
        {
            _maxX = maxX; _maxY = maxY; _maxZ = maxZ;
            _xMin = _yMin = _zMin = AbsoluteMin;
            _xMax = maxX; _yMax = maxY; _zMax = maxZ;
            NotifyAll();
        }

        /// <summary>
        /// Updates ZMax when the Z-axis dropdown selection changes for 4+ axis cubes.
        /// Clamps the current ZMax and ZMin to stay in range.
        /// Replaces CanvassDesktop.updateSubsetZMax().
        /// </summary>
        public void UpdateZAxisMax(int newMaxZ)
        {
            _maxZ = newMaxZ;
            _zMax = Math.Min(_zMax, newMaxZ);
            _zMin = Math.Min(_zMin, _zMax);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ZMin)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ZMax)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxZ)));
        }

        /// <summary>Snapshots the current validated state as a plain DTO for LoadCubeRequest.</summary>
        public SubsetBounds ToDto() =>
            new() { XMin = _xMin, XMax = _xMax, YMin = _yMin, YMax = _yMax, ZMin = _zMin, ZMax = _zMax };

        // ── Helpers ───────────────────────────────────────────────────────────
        private void Notify([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void NotifyAll()
        {
            foreach (var n in new[]
            {
                nameof(XMin), nameof(XMax), nameof(YMin), nameof(YMax),
                nameof(ZMin), nameof(ZMax), nameof(MaxX), nameof(MaxY), nameof(MaxZ)
            })
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        }
    }
}
