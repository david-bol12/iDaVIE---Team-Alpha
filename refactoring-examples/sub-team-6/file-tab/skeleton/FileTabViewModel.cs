// brief §6.6 | File tab AFTER skeleton — FileTabViewModel
// Replaces the File-tab responsibilities of CanvassDesktop 
// These were scattered across lines 306–1133 (BrowseImageFile→LoadCubeCoroutine) plus ChangeHduSelection (1435).
// No UnityEngine dependency. Satisfies NFR-MOD-2, ADR-009 (MVVM), ADR-002 (ACL).


// gives us INotifyPropertyChanged so the View can bind without us touching Unity
using System.ComponentModel;
// for [CallerMemberName] so Notify() figures out which property changed on its own
using System.Runtime.CompilerServices;

// equivalent to a package in java
namespace iDaVIE.Desktop.FileTab
{
    // This is the "brain" of the File tab. It holds all the state (which files are picked, which HDU/axis is selected, the subset bounds) and the actions the user can trigger.
    // It's plain C# — it never touches Unity directly, so we can test it on its own. The View just reflects whatever this class says.
    //
    // 'sealed': nothing is allowed to inherit from this class. It's a leaf — you change its behaviour by passing in different services, not by subclassing.
    //
    // The ': IFileTabViewModel, IDisposable' part lists the contracts it fulfils. There's no base class here on purpose).
    // IFileTabViewModel is what the View binds against, so the View never sees this concrete type.
    // IDisposable means it owns something that must be cleaned up (open FITS file handles) — hence the Dispose() method.
    public sealed class FileTabViewModel : IFileTabViewModel, IDisposable
    {
        // The 4 things this class needs to do its job. They're  handed in through the constructor ("dependency injection").
        // These interfaces lets us pass real adapters at runtime and fakes in tests
        // and keeps Unity/server code out of here entirely (ADR-002).
        // 'readonly' = set once.
        private readonly IFitsService       _fitsService;
        private readonly IFileDialogService _dialogService;
        private readonly IVolumeService     _volumeService;
        private readonly IMemoryProbe      _memoryProbe;

        // Private memory
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

        // Human Readable Lookup Table.
        private static readonly string[] RatioLabels = { "X=Y=Z", "X=Y" };

        private FitsFileInfo? _currentImageInfo;
        private FitsFileInfo? _currentMaskInfo;

        // Constructor
        public FileTabViewModel(IFitsService fitsService, IFileDialogService dialogService, IVolumeService volumeService, IMemoryProbe memoryProbe)
        {
            // throw an argument if (fitsService) is null
            _fitsService   = fitsService   ?? throw new ArgumentNullException(nameof(fitsService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _volumeService = volumeService ?? throw new ArgumentNullException(nameof(volumeService));
            _memoryProbe   = memoryProbe   ?? throw new ArgumentNullException(nameof(memoryProbe));

            Subset = new SubsetBoundsViewModel();

            // The 4 button actions. Each command pairs a method to run with a rule ("() => ...") that decides whether the button is currently clickable
            // Async versions are for slow file I/O (and auto-disable while running so you can't double-fire);
            //   BrowseImage : only when not already busy
            //   BrowseMask  : not busy AND an image is open (can't mask before an image)
            //   Load        : the file is a valid cube AND not busy
            //   ClearMask   : only when a mask is actually selected
            BrowseImageCommand = new AsyncRelayCommand(BrowseImageAsync, () => !IsLoading);
            BrowseMaskCommand  = new AsyncRelayCommand(BrowseMaskAsync,  () => !IsLoading && _currentImageInfo != null);
            LoadCommand        = new AsyncRelayCommand(LoadAsync,        () => IsLoadable && !IsLoading);
            ClearMaskCommand   = new RelayCommand(ClearMask,             () => _maskPath != null);
        }

        // Fires whenever a bound property changes so the View can update itself.
        public event PropertyChangedEventHandler? PropertyChanged;

        private void Notify([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // Currently loaded image and mask files (null until one is opened).
        public string? ImagePath { get => _imagePath; private set { _imagePath = value; Notify(); } }
        public string? MaskPath  { get => _maskPath;  private set { _maskPath  = value; Notify(); } }

        // HDUs available in the open file, shown in the selection dropdown.
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

        // For cubes with 4+ axes, lets the user pick which axis is the depth (Z) axis.
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

        // "Load only part of the cube" toggle.
        public bool SubsetEnabled
        {
            get => _subsetEnabled;
            set { _subsetEnabled = value; Notify(); }
        }
        // Nested VM holding the 6 subset bound values (X/Y/Z min+max).
        public SubsetBoundsViewModel Subset { get; }

        // Read-only label list for the aspect-ratio dropdown.
        public IReadOnlyList<string> RatioModeOptions => RatioLabels;
        // The chosen ratio; guard skips notifying if it didn't actually change.
        public RatioMode RatioMode
        {
            get => _ratioMode;
            set { if (_ratioMode == value) return; _ratioMode = value; Notify(); }
        }

        // Derived States
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

        // Pure, Unity-free replacement for CanvassDesktop.IsLoadable()
        // true when an image is open with NAXIS ≥ 3, ≥ 3 non-trivial axes (a Z-axis selection is required beyond 3), and any selected mask's axes 1–3 match the image.
        public bool IsLoadable
        {
            get
            {
                if (_currentImageInfo is null) return false;
                if (_currentImageInfo.NAxis < 3) return false;

                var nonTrivialCount = _currentImageInfo.AxisSizes.Values.Count(s => s > 1);
                if (nonTrivialCount < 3) return false;
                if (nonTrivialCount > 3 && _zAxisOptions.Count == 0) return false;

                if (_currentMaskInfo != null && !FitsMetadataHelper.MaskAxesMatchImage(_currentImageInfo, _currentMaskInfo))
                    return false;

                return true;
            }
        }

        // User triggerable actions on File tab
        public IAsyncCommand BrowseImageCommand { get; }
        public IAsyncCommand BrowseMaskCommand  { get; }
        public IAsyncCommand LoadCommand        { get; }
        public ICommand      ClearMaskCommand   { get; }

        // Implementations

        // Replaces CanvassDesktop.BrowseImageFile() + _browseImageFile().
        // No direct P/Invoke, no transform.Find — everything behind interfaces.
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
                var (maxX, maxY, maxZ) = FitsMetadataHelper.GetAxisMaxima(info);
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

        // Replaces CanvassDesktop.BrowseMaskFile() + _browseMaskFile().
        // Axis validation is pure C# (MaskAxesMatchImage); no Unity calls.
        private async Task BrowseMaskAsync()
        {
            // Pop up a file picker (interface call, no Unity). Null = user hit Cancel, so bail.
            var path = await _dialogService.PickFileAsync(
                "Select FITS mask", string.Empty, new[] { "fits", "fit" });
            if (path is null) return;

            IsLoading = true; 
            ValidationMessage = null;  // clear any old error text
            try
            {
                // Open the mask via the gateway; returns metadata + an open handle.
                var info = await _fitsService.OpenMaskAsync(path);

                // A mask overlays the image cube, so its axes must line up. Pure-C# check.
                if (_currentImageInfo != null && !FitsMetadataHelper.MaskAxesMatchImage(_currentImageInfo, info))
                {
                    info.Dispose();   // mismatched mask — release its handle immediately
                    ValidationMessage = "Mask dimensions do not match the image cube.";
                    return;
                }

                _currentMaskInfo?.Dispose();   // close the previously loaded mask, if any
                _currentMaskInfo = info;
                MaskPath = path;               // notifies the View to show the new path
                NotifyIsLoadable();            // re-check whether Load should be enabled
            }
            catch (Exception ex)
            {
                // Any failure → friendly message instead of a crash.
                ValidationMessage = $"Failed to read mask file: {ex.Message}";
            }
            finally
            {
                IsLoading = false;   // always runs, so the UI never gets stuck loading
            }
        }

        // Replaces CanvassDesktop.LoadFileFromFileSystem() + LoadCubeCoroutine().
        // Builds a plain LoadCubeRequest DTO and delegates to IVolumeService
        // There is no coroutine management or no scene hierarchy writes now.
        private async Task LoadAsync()
        {
            if (_imagePath is null) return;

            IsLoading = true;
            ValidationMessage = null;
            try
            {
                // Non-blocking RAM warning — replaces CanvassDesktop.CheckMemSpaceForCubes (CanvassDesktop.cs:995-1013). Matches BEFORE behaviour: warn the user, continue with the load.
                var warning = BuildMemoryWarning();
                if (warning != null) ValidationMessage = warning;

                var request = new LoadCubeRequest
                {
                    ImagePath      = _imagePath,
                    MaskPath       = _maskPath,
                    HduIndex       = _selectedHduIndex + 1,     // FITS HDU is 1-based
                    Subset         = _subsetEnabled ? Subset.ToDto() : null,
                    ZAxisSelection = _selectedZAxisIndex,
                    ZScale         = FitsMetadataHelper.ComputeZScale(_ratioMode, _currentImageInfo),
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

        // Releases any open FITS file handles. Called by the composition root on
        // OnDestroy so native pointers do not leak when the panel is torn down.
        public void Dispose()
        {
            _currentImageInfo?.Dispose();
            _currentMaskInfo?.Dispose();
            _currentImageInfo = null;
            _currentMaskInfo  = null;
        }

        // Private helpers

        private async Task RefreshHduHeaderAsync(int hduIndex)
        {
            if (_currentImageInfo is null) return;
            try
            {
                // Reuse the still-open FITS handle (FITS HDU index is 1-based).
                // Replaces CanvassDesktop.ChangeHduSelection (line 1435) which
                // reopened the file from disk on every dropdown selection.
                HeaderText = await _fitsService.GetHeaderTextAsync(_currentImageInfo.Handle, hduIndex + 1);
            }
            catch { /* non-fatal — keep displaying the previous header */ }
            NotifyIsLoadable();
        }

        // Computes a human-readable RAM-feasibility warning, or null if the projected cube fits in available system memory.
        // Non-blocking — matches the CanvassDesktop.CheckMemSpaceForCubes contract (warn, do not gate).
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

    // Minimal command helpers
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
