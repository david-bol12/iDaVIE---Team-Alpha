// WE1-3 | File tab AFTER skeleton — FileTabViewModel
// Replaces the File-tab responsibilities of CanvassDesktop (lines ~200–700).
// No UnityEngine dependency. Satisfies NFR-MOD-2, ADR-0001 (MVVM), ADR-0003 (ACL).
using System.ComponentModel;
using System.Runtime.CompilerServices;
namespace iDaVIE.Desktop.FileTab
{
    /// <summary>
    /// Concrete ViewModel for the File tab panel.
    /// Owns file-path state, HDU selection, subset configuration, validation, and
    /// the commands that drive file browsing and cube loading.
    ///
    /// All external dependencies are constructor-injected — no FindObjectOfType,
    /// no MonoBehaviour lifecycle coupling, no transform.Find chains.
    /// The composition root (a thin Unity MonoBehaviour) builds this and calls
    /// FileTabView.BindTo(vm) once.
    /// </summary>
    public sealed class FileTabViewModel : IFileTabViewModel, IDisposable
    {
        // ── Injected services (all behind interfaces) ─────────────────────────
        private readonly IFitsService       _fitsService;
        private readonly IFileDialogService _dialogService;
        private readonly IVolumeService     _volumeService;
        private readonly IMemoryProbe      _memoryProbe;

        // ── Private backing state ──────────────────────────────────────────────
        private string? _imagePath;
        private string? _maskPath;
        private readonly List<HduInfo> _hduOptions    = new();
        private readonly List<string>  _zAxisOptions  = new();
        private int      _selectedHduIndex;
        private int      _selectedZAxisIndex;
        private bool     _subsetEnabled;
        private bool     _isLoading;
        private string?  _headerText;
        private string?  _validationMessage;
        private RatioMode _ratioMode = RatioMode.Isotropic;

        // Fixed display labels for the Ratio_Dropdown — index-aligned with RatioMode.
        // Mirrors the two options on the original Ratio_Dropdown UI element
        // (scope §1 / §10 Anomaly #5).
        private static readonly string[] RatioLabels = { "X=Y=Z", "X=Y" };

        private FitsFileInfo? _currentImageInfo;
        private FitsFileInfo? _currentMaskInfo;

        // ── Constructor ────────────────────────────────────────────────────────
        public FileTabViewModel(
            IFitsService       fitsService,
            IFileDialogService dialogService,
            IVolumeService     volumeService,
            IMemoryProbe       memoryProbe)
        {
            _fitsService   = fitsService   ?? throw new ArgumentNullException(nameof(fitsService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _volumeService = volumeService ?? throw new ArgumentNullException(nameof(volumeService));
            _memoryProbe   = memoryProbe   ?? throw new ArgumentNullException(nameof(memoryProbe));

            Subset = new SubsetBoundsViewModel();

            BrowseImageCommand = new AsyncRelayCommand(BrowseImageAsync, () => !IsLoading);
            BrowseMaskCommand  = new AsyncRelayCommand(BrowseMaskAsync,  () => !IsLoading && _currentImageInfo != null);
            LoadCommand        = new AsyncRelayCommand(LoadAsync,        () => IsLoadable && !IsLoading);
            ClearMaskCommand   = new RelayCommand(ClearMask,             () => _maskPath != null);
        }

        // ── INotifyPropertyChanged ─────────────────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;

        private void Notify([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ── File path properties ───────────────────────────────────────────────
        public string? ImagePath { get => _imagePath; private set { _imagePath = value; Notify(); } }
        public string? MaskPath  { get => _maskPath;  private set { _maskPath  = value; Notify(); } }

        // ── HDU dropdown ───────────────────────────────────────────────────────
        public IReadOnlyList<HduInfo> HduOptions => _hduOptions;

        public int SelectedHduIndex
        {
            get => _selectedHduIndex;
            set
            {
                if (_selectedHduIndex == value) return;
                _selectedHduIndex = value;
                Notify();
                _ = RefreshHduHeaderAsync(value);   // fire-and-forget header refresh
            }
        }

        // ── Z-axis selection (4+ axis cubes) ──────────────────────────────────
        public IReadOnlyList<string> ZAxisOptions => _zAxisOptions;

        public int SelectedZAxisIndex
        {
            get => _selectedZAxisIndex;
            set
            {
                if (_selectedZAxisIndex == value) return;
                _selectedZAxisIndex = value;
                Notify();
                UpdateZAxisMax();
                NotifyIsLoadable();
            }
        }

        // ── Subset properties ──────────────────────────────────────────────────
        public bool SubsetEnabled
        {
            get => _subsetEnabled;
            set { _subsetEnabled = value; Notify(); }
        }
        public SubsetBoundsViewModel Subset { get; }

        // ── Aspect-ratio (Ratio_Dropdown on file-load modal) ──────────────────
        public IReadOnlyList<string> RatioModeOptions => RatioLabels;
        public RatioMode RatioMode
        {
            get => _ratioMode;
            set { if (_ratioMode == value) return; _ratioMode = value; Notify(); }
        }

        // ── Derived / computed state ───────────────────────────────────────────
        public bool IsLoading
        {
            get => _isLoading;
            private set { _isLoading = value; Notify(); NotifyCommandStates(); }
        }

        public string? HeaderText
        {
            get => _headerText;
            private set { _headerText = value; Notify(); }
        }

        public string? ValidationMessage
        {
            get => _validationMessage;
            private set { _validationMessage = value; Notify(); }
        }

        /// <summary>
        /// Pure computed property — replaces CanvassDesktop.IsLoadable().
        /// No Unity types; fully unit-testable in isolation.
        /// Rules:
        ///   1. An image file must be open.
        ///   2. NAXIS must be ≥ 3.
        ///   3. At least 3 axes must have size > 1.
        ///   4. For 4+ non-trivial axes the user must have a Z-axis selection available.
        ///   5. If a mask was selected its axes 1, 2, 3 must match the image.
        /// </summary>
        public bool IsLoadable
        {
            get
            {
                if (_currentImageInfo is null) return false;
                if (_currentImageInfo.NAxis < 3) return false;

                var nonTrivialCount = _currentImageInfo.AxisSizes.Values.Count(s => s > 1);
                if (nonTrivialCount < 3) return false;
                if (nonTrivialCount > 3 && _zAxisOptions.Count == 0) return false;

                if (_currentMaskInfo != null && !MaskAxesMatchImage(_currentImageInfo, _currentMaskInfo))
                    return false;

                return true;
            }
        }

        // ── Commands ───────────────────────────────────────────────────────────
        public IAsyncCommand BrowseImageCommand { get; }
        public IAsyncCommand BrowseMaskCommand  { get; }
        public IAsyncCommand LoadCommand        { get; }
        public ICommand      ClearMaskCommand   { get; }

        // ── Command implementations ────────────────────────────────────────────

        /// <summary>
        /// Replaces CanvassDesktop.BrowseImageFile() + _browseImageFile().
        /// No direct P/Invoke, no transform.Find — everything behind interfaces.
        /// </summary>
        private async Task BrowseImageAsync()
        {
            var path = await _dialogService.PickFileAsync(
                "Select FITS image", string.Empty, new[] { "fits", "fit" });
            if (path is null) return;

            IsLoading = true;
            ValidationMessage = null;
            try
            {
                var info = await _fitsService.OpenImageAsync(path);
                // Dispose the previous handles before replacing — the adapter holds the
                // native file pointer open across HDU reads, so we must close it on swap.
                _currentImageInfo?.Dispose();
                _currentMaskInfo?.Dispose();
                _currentMaskInfo = null;
                _currentImageInfo = info;
                ImagePath = path;

                // Update HDU dropdown
                _hduOptions.Clear();
                _hduOptions.AddRange(info.HduList);
                _selectedHduIndex = 0;
                Notify(nameof(HduOptions));
                Notify(nameof(SelectedHduIndex));

                HeaderText = info.HeaderText;

                // Populate Z-axis options for 4+ axis cubes (replaces IsLoadable Z-axis logic)
                PopulateZAxisOptions(info);

                // Reset subset to full cube extents (replaces setSubsetBounds)
                var (maxX, maxY, maxZ) = GetAxisMaxima(info);
                Subset.ResetToAxisMaxima(maxX, maxY, maxZ);
                SubsetEnabled = false;

                // Invalidate any previously selected mask when the image changes
                // (the prior mask handle was already disposed above with the image handle).
                MaskPath = null;

                NotifyIsLoadable();
                ValidationMessage = IsLoadable ? null : "File does not represent a loadable 3-D cube.";
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Failed to read FITS file: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Replaces CanvassDesktop.BrowseMaskFile() + _browseMaskFile().
        /// Axis validation is pure C# (MaskAxesMatchImage); no Unity calls.
        /// </summary>
        private async Task BrowseMaskAsync()
        {
            var path = await _dialogService.PickFileAsync(
                "Select FITS mask", string.Empty, new[] { "fits", "fit" });
            if (path is null) return;

            IsLoading = true;
            ValidationMessage = null;
            try
            {
                var info = await _fitsService.OpenMaskAsync(path);

                if (_currentImageInfo != null && !MaskAxesMatchImage(_currentImageInfo, info))
                {
                    info.Dispose();   // mismatched mask — release its handle immediately
                    ValidationMessage = "Mask dimensions do not match the image cube.";
                    return;
                }

                _currentMaskInfo?.Dispose();
                _currentMaskInfo = info;
                MaskPath = path;
                NotifyIsLoadable();
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Failed to read mask file: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Replaces CanvassDesktop.LoadFileFromFileSystem() + LoadCubeCoroutine().
        /// Builds a plain LoadCubeRequest DTO and delegates to IVolumeService —
        /// no coroutine management, no scene hierarchy writes.
        /// </summary>
        private async Task LoadAsync()
        {
            if (_imagePath is null) return;

            IsLoading = true;
            ValidationMessage = null;
            try
            {
                // Non-blocking RAM warning — replaces CanvassDesktop.CheckMemSpaceForCubes
                // (CanvassDesktop.cs:995-1013). Matches BEFORE behaviour: warn the user,
                // continue with the load.
                var warning = BuildMemoryWarning();
                if (warning != null) ValidationMessage = warning;

                var request = new LoadCubeRequest
                {
                    ImagePath      = _imagePath,
                    MaskPath       = _maskPath,
                    HduIndex       = _selectedHduIndex + 1,     // FITS HDU is 1-based
                    Subset         = _subsetEnabled ? Subset.ToDto() : null,
                    ZAxisSelection = _selectedZAxisIndex,
                    ZScale         = ComputeZScale(_ratioMode, _currentImageInfo),
                };
                await _volumeService.LoadCubeAsync(request, progress: new Progress<float>());
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Load failed: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ClearMask()
        {
            _currentMaskInfo?.Dispose();
            _currentMaskInfo = null;
            MaskPath = null;
            NotifyIsLoadable();
        }

        /// <summary>
        /// Releases any open FITS file handles. Called by the composition root on
        /// OnDestroy so native pointers do not leak when the panel is torn down.
        /// </summary>
        public void Dispose()
        {
            _currentImageInfo?.Dispose();
            _currentMaskInfo?.Dispose();
            _currentImageInfo = null;
            _currentMaskInfo  = null;
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private async Task RefreshHduHeaderAsync(int hduIndex)
        {
            if (_currentImageInfo is null) return;
            try
            {
                // Reuse the still-open FITS handle (FITS HDU index is 1-based).
                // Replaces CanvassDesktop.ChangeHduSelection (line 1435) which
                // reopened the file from disk on every dropdown selection.
                HeaderText = await _fitsService.GetHeaderTextAsync(
                    _currentImageInfo.Handle, hduIndex + 1);
            }
            catch { /* non-fatal — keep displaying the previous header */ }
            NotifyIsLoadable();
        }

        /// <summary>
        /// Computes a human-readable RAM-feasibility warning, or null if the
        /// projected cube fits in available system memory. Non-blocking — matches
        /// the CanvassDesktop.CheckMemSpaceForCubes contract (warn, do not gate).
        /// </summary>
        private string? BuildMemoryWarning()
        {
            if (_currentImageInfo is null) return null;

            long required = _currentImageInfo.EstimatedBytes
                          + (_currentMaskInfo?.EstimatedBytes ?? 0);
            long total = _memoryProbe.TotalSystemBytes;
            if (total <= 0 || required <= total) return null;

            double gibReq = required / (1024d * 1024d * 1024d);
            double gibTot = total    / (1024d * 1024d * 1024d);
            return $"Warning: estimated cube size ({gibReq:F2} GiB) exceeds system memory ({gibTot:F2} GiB). Load may fail.";
        }

        private void PopulateZAxisOptions(FitsFileInfo info)
        {
            _zAxisOptions.Clear();
            var nonTrivial = info.AxisSizes
                .Where(kv => kv.Value > 1)
                .Select(kv => $"Axis {kv.Key} ({kv.Value} px)")
                .ToList();

            if (nonTrivial.Count > 3)
                _zAxisOptions.AddRange(nonTrivial);

            _selectedZAxisIndex = 0;
            Notify(nameof(ZAxisOptions));
            Notify(nameof(SelectedZAxisIndex));
        }

        private void UpdateZAxisMax()
        {
            if (_currentImageInfo is null) return;
            var nonTrivialKeys = _currentImageInfo.AxisSizes
                .Where(kv => kv.Value > 1)
                .Select(kv => kv.Key)
                .OrderBy(k => k)
                .ToList();

            if (_selectedZAxisIndex < nonTrivialKeys.Count)
            {
                var axisKey = nonTrivialKeys[_selectedZAxisIndex];
                Subset.UpdateZAxisMax((int)_currentImageInfo.AxisSizes[axisKey]);
            }
        }

        private static (int maxX, int maxY, int maxZ) GetAxisMaxima(FitsFileInfo info)
        {
            long Get(int axis) => info.AxisSizes.TryGetValue(axis, out var v) ? v : 1;
            return ((int)Get(1), (int)Get(2), (int)Get(3));
        }

        /// <summary>
        /// Pure computation — replaces the inline zScale arithmetic at
        /// CanvassDesktop.cs:1028-1039 (driven there by _ratioDropdownIndex).
        /// Isotropic returns 1; ProportionalZ scales Z by axisZ / max(axisX, axisY).
        /// </summary>
        private static float ComputeZScale(RatioMode mode, FitsFileInfo? info)
        {
            if (mode == RatioMode.Isotropic || info is null) return 1f;

            long ax(int axis) => info.AxisSizes.TryGetValue(axis, out var v) ? v : 1;
            long xy = Math.Max(ax(1), ax(2));
            if (xy <= 0) return 1f;
            return (float)ax(3) / xy;
        }

        /// <summary>
        /// Pure validation — replaces the axis-comparison logic inside
        /// CanvassDesktop._browseMaskFile. No Unity types.
        /// </summary>
        private static bool MaskAxesMatchImage(FitsFileInfo image, FitsFileInfo mask) =>
            image.AxisSizes.TryGetValue(1, out var ix) && mask.AxisSizes.TryGetValue(1, out var mx) && ix == mx &&
            image.AxisSizes.TryGetValue(2, out var iy) && mask.AxisSizes.TryGetValue(2, out var my) && iy == my &&
            image.AxisSizes.TryGetValue(3, out var iz) && mask.AxisSizes.TryGetValue(3, out var mz) && iz == mz;

        private void NotifyIsLoadable()
        {
            Notify(nameof(IsLoadable));
            NotifyCommandStates();
        }

        private void NotifyCommandStates()
        {
            (BrowseImageCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (BrowseMaskCommand  as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (LoadCommand        as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (ClearMaskCommand   as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    // ── Minimal command helpers ────────────────────────────────────────────────
    // No WPF / WindowsBase reference — Unity does not link that assembly.

    internal sealed class AsyncRelayCommand : IAsyncCommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;
        private bool _isRunning;

        internal AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute)
        {
            _execute    = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;
        public bool CanExecute() => !_isRunning && _canExecute();

        public async Task ExecuteAsync()
        {
            if (!CanExecute()) return;
            _isRunning = true;
            RaiseCanExecuteChanged();
            try   { await _execute(); }
            finally { _isRunning = false; RaiseCanExecuteChanged(); }
        }

        internal void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    internal sealed class RelayCommand : ICommand
    {
        private readonly Action    _execute;
        private readonly Func<bool> _canExecute;

        internal RelayCommand(Action execute, Func<bool> canExecute)
        {
            _execute    = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;
        public bool CanExecute() => _canExecute();
        public void Execute()    => _execute();
        internal void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
