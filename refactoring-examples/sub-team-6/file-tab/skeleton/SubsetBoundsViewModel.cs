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
    // The ViewModel behind the six-axis subset (crop) selector. It owns the bounds and the rule that keeps them valid; the View just binds its input fields to these properties.
    // The whole point: every bound self-corrects on set and raises PropertyChanged, so a bad number entered in the UI is clamped here and the corrected value flows straight back — no manual transform.Find()/error-checking in the View (replaces CanvassDesktop.checkSubsetBounds).
    public sealed class SubsetBoundsViewModel : INotifyPropertyChanged
    {
        // Current bounds (backing fields) and the per-axis maxima they clamp against. Maxima default to 1 until a file sets them via ResetToAxisMaxima.
        private int _xMin, _xMax, _yMin, _yMax, _zMin, _zMax;
        private int _maxX = 1, _maxY = 1, _maxZ = 1;
        private const int AbsoluteMin = 1;   // FITS pixel indices are 1-based, so no bound goes below 1.

        public event PropertyChangedEventHandler? PropertyChanged;

        // Per-axis upper limits
        // These are read-only to the View (set only by ResetToAxisMaxima / UpdateZAxisMax).
        public int MaxX => _maxX;
        public int MaxY => _maxY;
        public int MaxZ => _maxZ;

        // The six bounds, bound two-way to the input fields. Each setter clamps into a valid range before storing:
        //   a Min is held within [AbsoluteMin .. its own Max]; a Max within [its own Min .. the axis maximum].
        // So Min and Max can never cross, and neither can leave the cube. Notify() then pushes the (possibly corrected) value back to the field.
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

        // Sets the axis maxima for a freshly opened cube and resets every bound to the full extent [1 .. axisMax]. Called by FileTabViewModel once a new FITS image opens; replaces CanvassDesktop.setSubsetBounds().
        public void ResetToAxisMaxima(int maxX, int maxY, int maxZ)
        {
            _maxX = maxX; _maxY = maxY; _maxZ = maxZ;
            _xMin = _yMin = _zMin = AbsoluteMin;
            _xMax = maxX; _yMax = maxY; _zMax = maxZ;
            NotifyAll();
        }

        // Re-clamps the Z bounds when the user repicks the Z axis on a 4+ axis cube (the new axis may be shorter). Only the Z properties changed, so only those are notified. Replaces CanvassDesktop.updateSubsetZMax().
        public void UpdateZAxisMax(int newMaxZ)
        {
            _maxZ = newMaxZ;
            _zMax = Math.Min(_zMax, newMaxZ);
            _zMin = Math.Min(_zMin, _zMax);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ZMin)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ZMax)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxZ)));
        }

        // Snapshots the current (already-validated) bounds into a plain SubsetBounds for LoadCubeRequest.
        public SubsetBounds ToDto() =>
            new() { XMin = _xMin, XMax = _xMax, YMin = _yMin, YMax = _yMax, ZMin = _zMin, ZMax = _zMax };

        // Fires PropertyChanged for the caller's own property. [CallerMemberName] makes the compiler fill in the property name, so each setter just calls Notify() with no magic string.
        private void Notify([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // Fires PropertyChanged for every property at once — used after a bulk reset where all of them may have moved.
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
