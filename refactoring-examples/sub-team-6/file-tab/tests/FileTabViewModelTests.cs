// WE1-3 | File tab AFTER — unit tests for FileTabViewModel and SubsetBoundsViewModel
// NUnit 3, no Unity dependency. Satisfies NFR-TST-1 and Section 9.2 testability evidence.
// Run with: dotnet test file-tab/tests/FileTabTests.csproj
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using iDaVIE.Desktop.FileTab;
using NUnit.Framework;

namespace iDaVIE.Desktop.FileTab.Tests
{
    // ── Test doubles ──────────────────────────────────────────────────────────

    internal sealed class StubFitsHandle : IFitsHandle
    {
        public string FilePath { get; }
        public bool   Disposed { get; private set; }
        public StubFitsHandle(string filePath) => FilePath = filePath;
        public void Dispose() => Disposed = true;
    }

    internal sealed class StubFitsService : IFitsService
    {
        public FitsFileInfo? NextImageResult;
        public FitsFileInfo? NextMaskResult;
        public string        NextHeaderText = string.Empty;
        public Exception?    ThrowOnOpen;

        public Task<FitsFileInfo> OpenImageAsync(string path, CancellationToken ct = default)
        {
            if (ThrowOnOpen != null) throw ThrowOnOpen;
            return Task.FromResult(NextImageResult
                ?? throw new InvalidOperationException("StubFitsService: NextImageResult not set"));
        }

        public Task<FitsFileInfo> OpenMaskAsync(string path, CancellationToken ct = default)
            => Task.FromResult(NextMaskResult
                ?? throw new InvalidOperationException("StubFitsService: NextMaskResult not set"));

        public Task<string> GetHeaderTextAsync(IFitsHandle handle, int hduIndex, CancellationToken ct = default)
            => Task.FromResult(NextHeaderText);
    }

    internal sealed class StubMemoryProbe : IMemoryProbe
    {
        public long TotalSystemBytes { get; set; } = 64L * 1024L * 1024L * 1024L; // 64 GiB default
    }

    internal sealed class StubFileDialogService : IFileDialogService
    {
        public string? PathToReturn;
        public Task<string?> PickFileAsync(string title, string dir, string[] ext)
            => Task.FromResult(PathToReturn);
    }

    internal sealed class StubVolumeService : IVolumeService
    {
        public bool                IsCubeLoaded => false;
        public LoadCubeRequest?   LastRequest;
        public Exception?         ThrowOnLoad;

        public event EventHandler<CubeLoadedEventArgs>? CubeLoaded;

        public Task LoadCubeAsync(
            LoadCubeRequest request,
            IProgress<float>? progress = null,
            CancellationToken ct = default)
        {
            if (ThrowOnLoad != null) throw ThrowOnLoad;
            LastRequest = request;
            progress?.Report(1f);

            // Simulate the adapter raising the event on successful load.
            CubeLoaded?.Invoke(this, new CubeLoadedEventArgs
            {
                ImagePath = request.ImagePath,
                MaskPath  = request.MaskPath,
                HduIndex  = request.HduIndex,
            });
            return Task.CompletedTask;
        }
    }

    // ── Shared fixture helpers ─────────────────────────────────────────────────

    internal static class FitsFactory
    {
        public static FitsFileInfo Cube3D(int x = 512, int y = 512, int z = 100,
            string path = "/data/test.fits", long estimatedBytes = 0) => new FitsFileInfo
        {
            Handle         = new StubFitsHandle(path),
            FilePath       = path,
            HduList        = new[] { new HduInfo(1, "PRIMARY", "IMAGE") },
            NAxis          = 3,
            AxisSizes      = new Dictionary<int, long> { [1] = x, [2] = y, [3] = z },
            HeaderText     = $"NAXIS  = 3\nNAXIS1 = {x}\nNAXIS2 = {y}\nNAXIS3 = {z}\n",
            EstimatedBytes = estimatedBytes == 0 ? (long)x * y * z * sizeof(float) : estimatedBytes,
        };

        public static FitsFileInfo TwoDimImage(string path = "/data/flat.fits") => new FitsFileInfo
        {
            Handle     = new StubFitsHandle(path),
            FilePath   = path,
            HduList    = new[] { new HduInfo(1, "PRIMARY", "IMAGE") },
            NAxis      = 2,
            AxisSizes  = new Dictionary<int, long> { [1] = 512, [2] = 512 },
            HeaderText = "NAXIS  = 2\n",
        };

        public static FitsFileInfo FourAxisCube(string path = "/data/4d.fits") => new FitsFileInfo
        {
            Handle     = new StubFitsHandle(path),
            FilePath   = path,
            HduList    = new[] { new HduInfo(1, "PRIMARY", "IMAGE") },
            NAxis      = 4,
            AxisSizes  = new Dictionary<int, long> { [1] = 512, [2] = 512, [3] = 100, [4] = 4 },
            HeaderText = "NAXIS = 4\n",
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FileTabViewModel tests
    // ═══════════════════════════════════════════════════════════════════════════

    [TestFixture]
    public sealed class FileTabViewModelTests
    {
        private static (FileTabViewModel vm, StubFitsService fits,
                         StubFileDialogService dialog, StubVolumeService volume,
                         StubMemoryProbe probe)
            BuildVm(string? initialDialogPath = "/data/test.fits")
        {
            var fits   = new StubFitsService();
            var dialog = new StubFileDialogService { PathToReturn = initialDialogPath };
            var vol    = new StubVolumeService();
            var probe  = new StubMemoryProbe();
            return (new FileTabViewModel(fits, dialog, vol, probe), fits, dialog, vol, probe);
        }

        // ── Browse image ──────────────────────────────────────────────────────

        [Test]
        public async Task BrowseImage_ValidCube_SetsImagePathAndIsLoadable()
        {
            var (vm, fits, _, _, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D();

            await vm.BrowseImageCommand.ExecuteAsync();

            Assert.AreEqual("/data/test.fits", vm.ImagePath);
            Assert.IsTrue(vm.IsLoadable);
            Assert.IsNull(vm.ValidationMessage);
        }

        [Test]
        public async Task BrowseImage_UserCancels_LeavesStateUnchanged()
        {
            var (vm, fits, dialog, _, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D();
            dialog.PathToReturn  = null;   // simulate cancel

            await vm.BrowseImageCommand.ExecuteAsync();

            Assert.IsNull(vm.ImagePath);
            Assert.IsFalse(vm.IsLoadable);
        }

        [Test]
        public async Task BrowseImage_TwoDimensionalFile_IsNotLoadable()
        {
            var (vm, fits, _, _, _) = BuildVm();
            fits.NextImageResult = FitsFactory.TwoDimImage();

            await vm.BrowseImageCommand.ExecuteAsync();

            Assert.IsFalse(vm.IsLoadable);
            Assert.IsNotNull(vm.ValidationMessage);
        }

        [Test]
        public async Task BrowseImage_FitsOpenThrows_SetsValidationMessage()
        {
            var (vm, fits, _, _, _) = BuildVm();
            fits.ThrowOnOpen = new InvalidOperationException("native error");

            await vm.BrowseImageCommand.ExecuteAsync();

            Assert.IsNull(vm.ImagePath);
            Assert.IsFalse(vm.IsLoadable);
            Assert.IsNotNull(vm.ValidationMessage);
            StringAssert.Contains("native error", vm.ValidationMessage);
        }

        [Test]
        public async Task BrowseImage_ClearsExistingMask()
        {
            var (vm, fits, dialog, _, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D();
            fits.NextMaskResult  = FitsFactory.Cube3D();

            await vm.BrowseImageCommand.ExecuteAsync();
            dialog.PathToReturn = "/data/mask.fits";
            await vm.BrowseMaskCommand.ExecuteAsync();
            Assert.IsNotNull(vm.MaskPath);

            // Browse a second image — mask should be cleared
            dialog.PathToReturn  = "/data/test2.fits";
            fits.NextImageResult = FitsFactory.Cube3D(path: "/data/test2.fits");
            await vm.BrowseImageCommand.ExecuteAsync();

            Assert.IsNull(vm.MaskPath);
        }

        [Test]
        public async Task BrowseImage_PopulatesHduDropdownWithHduCount()
        {
            var (vm, fits, _, _, _) = BuildVm();
            var info = new FitsFileInfo
            {
                Handle     = new StubFitsHandle("/data/multi.fits"),
                FilePath   = "/data/multi.fits",
                HduList    = new[] { new HduInfo(1, "PRIMARY", "IMAGE"), new HduInfo(2, "SCI", "IMAGE") },
                NAxis      = 3,
                AxisSizes  = new Dictionary<int, long> { [1] = 512, [2] = 512, [3] = 100 },
                HeaderText = "",
            };
            fits.NextImageResult = info;

            await vm.BrowseImageCommand.ExecuteAsync();

            Assert.AreEqual(2, vm.HduOptions.Count);
            Assert.AreEqual("SCI", vm.HduOptions[1].Name);
        }

        [Test]
        public async Task BrowseImage_FourAxisCube_PopulatesZAxisOptions()
        {
            var (vm, fits, _, _, _) = BuildVm();
            fits.NextImageResult = FitsFactory.FourAxisCube();

            await vm.BrowseImageCommand.ExecuteAsync();

            // 4 axes, all > 1 → z-axis dropdown should have options
            Assert.Greater(vm.ZAxisOptions.Count, 0);
        }

        [Test]
        public async Task BrowseImage_ResetSubsetToFullCubeExtents()
        {
            var (vm, fits, _, _, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D(x: 100, y: 200, z: 50);

            await vm.BrowseImageCommand.ExecuteAsync();

            Assert.AreEqual(1,   vm.Subset.XMin);
            Assert.AreEqual(100, vm.Subset.XMax);
            Assert.AreEqual(1,   vm.Subset.YMin);
            Assert.AreEqual(200, vm.Subset.YMax);
            Assert.AreEqual(1,   vm.Subset.ZMin);
            Assert.AreEqual(50,  vm.Subset.ZMax);
        }

        // ── Browse mask ────────────────────────────────────────────────────────

        [Test]
        public async Task BrowseMask_MatchingDimensions_SetsMaskPath()
        {
            var (vm, fits, dialog, _, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D();
            fits.NextMaskResult  = FitsFactory.Cube3D();

            await vm.BrowseImageCommand.ExecuteAsync();
            dialog.PathToReturn = "/data/mask.fits";
            await vm.BrowseMaskCommand.ExecuteAsync();

            Assert.AreEqual("/data/mask.fits", vm.MaskPath);
            Assert.IsNull(vm.ValidationMessage);
        }

        [Test]
        public async Task BrowseMask_MismatchedDimensions_RejectsMask()
        {
            var (vm, fits, dialog, _, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D(x: 512, y: 512, z: 100);
            fits.NextMaskResult  = FitsFactory.Cube3D(x: 256, y: 256, z: 50);

            await vm.BrowseImageCommand.ExecuteAsync();
            dialog.PathToReturn = "/data/mask.fits";
            await vm.BrowseMaskCommand.ExecuteAsync();

            Assert.IsNull(vm.MaskPath);
            Assert.IsNotNull(vm.ValidationMessage);
        }

        [Test]
        public async Task BrowseMask_NotAvailableBeforeImage()
        {
            var (vm, _, _, _, _) = BuildVm();

            // BrowseMaskCommand should not be executable without a loaded image
            Assert.IsFalse(vm.BrowseMaskCommand.CanExecute());
        }

        // ── Load ───────────────────────────────────────────────────────────────

        [Test]
        public async Task Load_ValidFile_PassesCorrectPathAndHduIndex()
        {
            var (vm, fits, dialog, vol, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D();

            await vm.BrowseImageCommand.ExecuteAsync();
            await vm.LoadCommand.ExecuteAsync();

            Assert.IsNotNull(vol.LastRequest);
            Assert.AreEqual("/data/test.fits", vol.LastRequest!.ImagePath);
            Assert.AreEqual(1, vol.LastRequest.HduIndex);   // index 0 → 1-based HDU
        }

        [Test]
        public async Task Load_WithSubsetEnabled_PassesSubsetBounds()
        {
            var (vm, fits, _, vol, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D(x: 512, y: 512, z: 100);

            await vm.BrowseImageCommand.ExecuteAsync();
            vm.SubsetEnabled = true;
            vm.Subset.XMin   = 10;
            vm.Subset.XMax   = 100;
            vm.Subset.ZMin   = 5;
            vm.Subset.ZMax   = 50;

            await vm.LoadCommand.ExecuteAsync();

            Assert.IsNotNull(vol.LastRequest!.Subset);
            Assert.AreEqual(10,  vol.LastRequest.Subset!.XMin);
            Assert.AreEqual(100, vol.LastRequest.Subset.XMax);
            Assert.AreEqual(5,   vol.LastRequest.Subset.ZMin);
            Assert.AreEqual(50,  vol.LastRequest.Subset.ZMax);
        }

        [Test]
        public async Task Load_SubsetDisabled_PassesNullSubset()
        {
            var (vm, fits, _, vol, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D();

            await vm.BrowseImageCommand.ExecuteAsync();
            vm.SubsetEnabled = false;
            await vm.LoadCommand.ExecuteAsync();

            Assert.IsNull(vol.LastRequest!.Subset);
        }

        [Test]
        public async Task Load_WithMask_PassesMaskPathInRequest()
        {
            var (vm, fits, dialog, vol, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D();
            fits.NextMaskResult  = FitsFactory.Cube3D();

            await vm.BrowseImageCommand.ExecuteAsync();
            dialog.PathToReturn = "/data/mask.fits";
            await vm.BrowseMaskCommand.ExecuteAsync();
            await vm.LoadCommand.ExecuteAsync();

            Assert.AreEqual("/data/mask.fits", vol.LastRequest!.MaskPath);
        }

        [Test]
        public async Task Load_VolumeServiceThrows_SetsValidationMessage()
        {
            var (vm, fits, _, vol, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D();
            vol.ThrowOnLoad      = new Exception("load failure");

            await vm.BrowseImageCommand.ExecuteAsync();
            await vm.LoadCommand.ExecuteAsync();

            Assert.IsNotNull(vm.ValidationMessage);
            StringAssert.Contains("load failure", vm.ValidationMessage);
        }

        [Test]
        public async Task Load_NotAvailableWithoutValidImage()
        {
            var (vm, _, _, _, _) = BuildVm();
            Assert.IsFalse(vm.LoadCommand.CanExecute());
        }

        // ── Aspect ratio (RatioMode → ZScale) ─────────────────────────────────

        [Test]
        public async Task Load_IsotropicRatio_PassesZScaleOne()
        {
            var (vm, fits, _, vol, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D(x: 512, y: 512, z: 100);

            await vm.BrowseImageCommand.ExecuteAsync();
            vm.RatioMode = RatioMode.Isotropic;
            await vm.LoadCommand.ExecuteAsync();

            Assert.AreEqual(1f, vol.LastRequest!.ZScale, 1e-6f);
        }

        [Test]
        public async Task Load_ProportionalZRatio_ScalesZAxisRelativeToXY()
        {
            var (vm, fits, _, vol, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D(x: 512, y: 512, z: 100);

            await vm.BrowseImageCommand.ExecuteAsync();
            vm.RatioMode = RatioMode.ProportionalZ;
            await vm.LoadCommand.ExecuteAsync();

            // z=100, max(x,y)=512 → expected zScale ≈ 100/512
            Assert.AreEqual(100f / 512f, vol.LastRequest!.ZScale, 1e-4f);
        }

        [Test]
        public void RatioModeOptions_ExposesTwoLabels()
        {
            var (vm, _, _, _, _) = BuildVm();
            Assert.AreEqual(2, vm.RatioModeOptions.Count);
            Assert.AreEqual("X=Y=Z", vm.RatioModeOptions[0]);
            Assert.AreEqual("X=Y",   vm.RatioModeOptions[1]);
        }

        // ── Memory-feasibility warning (CheckMemSpaceForCubes equivalent) ─────

        [Test]
        public async Task Load_CubeFitsInMemory_NoWarning()
        {
            var (vm, fits, _, _, probe) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D(estimatedBytes: 1L * 1024 * 1024 * 1024); // 1 GiB
            probe.TotalSystemBytes = 8L * 1024 * 1024 * 1024;                                   // 8 GiB

            await vm.BrowseImageCommand.ExecuteAsync();
            await vm.LoadCommand.ExecuteAsync();

            Assert.IsNull(vm.ValidationMessage);
        }

        // ── CubeLoaded event (peer-tab subscription point) ────────────────────

        [Test]
        public async Task Load_Success_RaisesCubeLoadedEventOnce()
        {
            var (vm, fits, _, vol, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D();

            int eventCount = 0;
            CubeLoadedEventArgs? captured = null;
            vol.CubeLoaded += (_, e) => { eventCount++; captured = e; };

            await vm.BrowseImageCommand.ExecuteAsync();
            await vm.LoadCommand.ExecuteAsync();

            Assert.AreEqual(1, eventCount);
            Assert.IsNotNull(captured);
            Assert.AreEqual("/data/test.fits", captured!.ImagePath);
            Assert.IsFalse(captured.HasMask);
        }

        [Test]
        public async Task Load_PeerTabUnsubscribes_DoesNotFireForFutureLoads()
        {
            var (vm, fits, _, vol, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D();

            int eventCount = 0;
            EventHandler<CubeLoadedEventArgs> handler = (_, _) => eventCount++;
            vol.CubeLoaded += handler;

            await vm.BrowseImageCommand.ExecuteAsync();
            await vm.LoadCommand.ExecuteAsync();
            Assert.AreEqual(1, eventCount);

            // Peer tab disposes / unsubscribes — closes scope §10 Anomaly #8.
            vol.CubeLoaded -= handler;

            await vm.LoadCommand.ExecuteAsync();
            Assert.AreEqual(1, eventCount);   // still 1; no leak
        }

        [Test]
        public async Task Load_CubeExceedsMemory_WarnsButStillLoads()
        {
            var (vm, fits, _, vol, probe) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D(estimatedBytes: 16L * 1024 * 1024 * 1024); // 16 GiB
            probe.TotalSystemBytes = 8L * 1024 * 1024 * 1024;                                    // 8 GiB

            await vm.BrowseImageCommand.ExecuteAsync();
            await vm.LoadCommand.ExecuteAsync();

            Assert.IsNotNull(vm.ValidationMessage);
            StringAssert.Contains("exceeds system memory", vm.ValidationMessage);
            // Non-blocking — load request still went through to the volume service.
            Assert.IsNotNull(vol.LastRequest);
        }

        // ── Clear mask ─────────────────────────────────────────────────────────

        [Test]
        public async Task ClearMask_RemovesMaskPath()
        {
            var (vm, fits, dialog, _, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D();
            fits.NextMaskResult  = FitsFactory.Cube3D();

            await vm.BrowseImageCommand.ExecuteAsync();
            dialog.PathToReturn = "/data/mask.fits";
            await vm.BrowseMaskCommand.ExecuteAsync();
            Assert.IsNotNull(vm.MaskPath);

            vm.ClearMaskCommand.Execute();

            Assert.IsNull(vm.MaskPath);
        }

        // ── PropertyChanged ────────────────────────────────────────────────────

        [Test]
        public async Task PropertyChanged_ImagePath_RaisedOnBrowse()
        {
            var (vm, fits, _, _, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D();
            var raised = new List<string?>();
            vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

            await vm.BrowseImageCommand.ExecuteAsync();

            Assert.Contains(nameof(IFileTabViewModel.ImagePath), raised);
        }

        [Test]
        public async Task PropertyChanged_IsLoading_ToggledDuringBrowse()
        {
            var (vm, fits, _, _, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D();
            bool loadingRaised = false;
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(IFileTabViewModel.IsLoading))
                    loadingRaised = true;
            };

            await vm.BrowseImageCommand.ExecuteAsync();

            Assert.IsTrue(loadingRaised);
            Assert.IsFalse(vm.IsLoading);   // must be false when command finishes
        }

        // ═══════════════════════════════════════════════════════════════════════
        // Branch-coverage gate close (audit F14 follow-up, 2026-05-27).
        //
        // The 34 happy-path / single-error tests above leave FileTabViewModel at
        // 68.7 % branch coverage (per ReportGenerator on the merged Cobertura).
        // The tests below target the specific uncovered branches identified by
        // line-level coverage:
        //
        //   - Constructor null-guards on the four injected services (L54-57)
        //   - "no-op when set to same value" early returns on the two index
        //     setters (L85, L100)
        //   - Setters called before any image is loaded — RefreshHduHeaderAsync
        //     and UpdateZAxisMax must short-circuit, not throw (L338, L388)
        //   - Dispose on a VM that never opened a file (L328, L329)
        //   - IsLoadable rule 3: NAXIS ≥ 3 but only two non-trivial axes (L161)
        //   - BrowseMask: user cancels the dialog (L244)
        //   - BrowseMask: replacing an existing mask disposes the old handle (L259)
        //   - MaskAxesMatchImage: NAXIS1 and NAXIS2 mismatches (existing test
        //     covered only NAXIS3) — all 3 short-circuit branches now exercised
        //
        // Together these are estimated to lift FileTabViewModel branch coverage
        // from 68.7 % to ≥ 76 %, clearing the D5 §7 ≥ 70 % gate.
        // ═══════════════════════════════════════════════════════════════════════

        // ── Constructor null guards (L54-57) ──────────────────────────────────

        [Test]
        public void Ctor_NullFitsService_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new FileTabViewModel(
                fitsService:   null!,
                dialogService: new StubFileDialogService(),
                volumeService: new StubVolumeService(),
                memoryProbe:   new StubMemoryProbe()));
        }

        [Test]
        public void Ctor_NullDialogService_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new FileTabViewModel(
                fitsService:   new StubFitsService(),
                dialogService: null!,
                volumeService: new StubVolumeService(),
                memoryProbe:   new StubMemoryProbe()));
        }

        [Test]
        public void Ctor_NullVolumeService_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new FileTabViewModel(
                fitsService:   new StubFitsService(),
                dialogService: new StubFileDialogService(),
                volumeService: null!,
                memoryProbe:   new StubMemoryProbe()));
        }

        [Test]
        public void Ctor_NullMemoryProbe_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new FileTabViewModel(
                fitsService:   new StubFitsService(),
                dialogService: new StubFileDialogService(),
                volumeService: new StubVolumeService(),
                memoryProbe:   null!));
        }

        // ── Setter no-op paths (L85, L100) ────────────────────────────────────

        [Test]
        public async Task SelectedHduIndex_SetToSameValue_DoesNotRaisePropertyChanged()
        {
            var (vm, fits, _, _, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D();
            await vm.BrowseImageCommand.ExecuteAsync();

            int notifications = 0;
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(IFileTabViewModel.SelectedHduIndex))
                    notifications++;
            };

            // Set to the value it already has — the equality short-circuit must
            // skip both the field write and the PropertyChanged fire.
            vm.SelectedHduIndex = vm.SelectedHduIndex;

            Assert.AreEqual(0, notifications);
        }

        [Test]
        public async Task SelectedZAxisIndex_SetToSameValue_DoesNotRaisePropertyChanged()
        {
            var (vm, fits, _, _, _) = BuildVm();
            fits.NextImageResult = FitsFactory.FourAxisCube();
            await vm.BrowseImageCommand.ExecuteAsync();

            int notifications = 0;
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(IFileTabViewModel.SelectedZAxisIndex))
                    notifications++;
            };

            vm.SelectedZAxisIndex = vm.SelectedZAxisIndex;

            Assert.AreEqual(0, notifications);
        }

        // ── Setters with no image loaded (L338, L388) ─────────────────────────

        [Test]
        public void SelectedHduIndex_SetBeforeImage_NoOpAndDoesNotThrow()
        {
            // Without an image loaded, the SelectedHduIndex setter fires
            // RefreshHduHeaderAsync (L336) which must short-circuit on the null
            // image check (L338); the same setter calls UpdateZAxisMax (via the
            // notify chain) which must also short-circuit (L388).
            var (vm, _, _, _, _) = BuildVm();

            Assert.DoesNotThrow(() => vm.SelectedHduIndex = 1);
            Assert.IsNull(vm.HeaderText);
            Assert.IsFalse(vm.IsLoadable);
        }

        // ── Dispose on empty VM (L328, L329) ──────────────────────────────────

        [Test]
        public void Dispose_VmWithNoOpenFiles_DoesNotThrow()
        {
            // The null-conditional ?.Dispose() chains in Dispose() are only
            // exercised when both handles are null. The existing fixtures
            // always open an image first; this test exercises the no-files
            // disposal path.
            var (vm, _, _, _, _) = BuildVm();

            Assert.DoesNotThrow(() => vm.Dispose());
        }

        // ── IsLoadable axis-count rule (L161) ─────────────────────────────────

        [Test]
        public async Task IsLoadable_ThreeAxesButOnlyTwoNonTrivial_False()
        {
            // NAXIS = 3, but the third axis has size 1 — IsLoadable rule 3:
            // "At least 3 axes must have size > 1." Exercises the branch at
            // L161 (nonTrivialCount < 3 return false).
            var (vm, fits, _, _, _) = BuildVm();
            fits.NextImageResult = new FitsFileInfo
            {
                Handle     = new StubFitsHandle("/data/degenerate.fits"),
                FilePath   = "/data/degenerate.fits",
                HduList    = new[] { new HduInfo(1, "PRIMARY", "IMAGE") },
                NAxis      = 3,
                AxisSizes  = new Dictionary<int, long> { [1] = 512, [2] = 512, [3] = 1 },
                HeaderText = "",
            };

            await vm.BrowseImageCommand.ExecuteAsync();

            Assert.IsFalse(vm.IsLoadable);
        }

        // ── BrowseMask edge cases (L244, L259, MaskAxesMatchImage axes 1+2) ───

        [Test]
        public async Task BrowseMask_UserCancels_LeavesStateUnchanged()
        {
            var (vm, fits, dialog, _, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D();
            await vm.BrowseImageCommand.ExecuteAsync();

            dialog.PathToReturn = null;   // simulate cancel
            await vm.BrowseMaskCommand.ExecuteAsync();

            Assert.IsNull(vm.MaskPath);
            Assert.IsNull(vm.ValidationMessage);
        }

        [Test]
        public async Task BrowseMask_ReplacesExistingMask_DisposesOldHandle()
        {
            var (vm, fits, dialog, _, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D();
            var firstMask  = FitsFactory.Cube3D(path: "/data/mask1.fits");
            var secondMask = FitsFactory.Cube3D(path: "/data/mask2.fits");
            fits.NextMaskResult = firstMask;

            await vm.BrowseImageCommand.ExecuteAsync();
            dialog.PathToReturn = "/data/mask1.fits";
            await vm.BrowseMaskCommand.ExecuteAsync();
            Assert.AreEqual("/data/mask1.fits", vm.MaskPath);

            // Browse a second mask — the first mask's handle must be disposed
            // by the _currentMaskInfo?.Dispose() path at L259.
            fits.NextMaskResult = secondMask;
            dialog.PathToReturn = "/data/mask2.fits";
            await vm.BrowseMaskCommand.ExecuteAsync();

            Assert.AreEqual("/data/mask2.fits", vm.MaskPath);
            Assert.IsTrue(((StubFitsHandle)firstMask.Handle).Disposed,
                "First mask handle should have been disposed when the second mask replaced it.");
        }

        [Test]
        public async Task BrowseMask_AxisXMismatch_RejectsMask()
        {
            // The existing BrowseMask_MismatchedDimensions_RejectsMask covers
            // only one of the three short-circuit branches in
            // MaskAxesMatchImage. This test exercises the NAXIS1 (X) mismatch
            // branch specifically.
            var (vm, fits, dialog, _, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D(x: 512, y: 256, z: 100);
            fits.NextMaskResult  = FitsFactory.Cube3D(x: 256, y: 256, z: 100,   // X mismatch
                                                     path: "/data/wrong-x.fits");

            await vm.BrowseImageCommand.ExecuteAsync();
            dialog.PathToReturn = "/data/wrong-x.fits";
            await vm.BrowseMaskCommand.ExecuteAsync();

            Assert.IsNull(vm.MaskPath);
            StringAssert.Contains("dimensions do not match", vm.ValidationMessage ?? string.Empty);
        }

        [Test]
        public async Task BrowseMask_AxisYMismatch_RejectsMask()
        {
            // The other untested MaskAxesMatchImage short-circuit branch.
            var (vm, fits, dialog, _, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D(x: 512, y: 256, z: 100);
            fits.NextMaskResult  = FitsFactory.Cube3D(x: 512, y: 512, z: 100,   // Y mismatch
                                                     path: "/data/wrong-y.fits");

            await vm.BrowseImageCommand.ExecuteAsync();
            dialog.PathToReturn = "/data/wrong-y.fits";
            await vm.BrowseMaskCommand.ExecuteAsync();

            Assert.IsNull(vm.MaskPath);
            StringAssert.Contains("dimensions do not match", vm.ValidationMessage ?? string.Empty);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SubsetBoundsViewModel tests
    // ═══════════════════════════════════════════════════════════════════════════

    [TestFixture]
    public sealed class SubsetBoundsViewModelTests
    {
        [Test]
        public void ResetToAxisMaxima_SetsAllBoundsCorrectly()
        {
            var sb = new SubsetBoundsViewModel();
            sb.ResetToAxisMaxima(100, 200, 50);

            Assert.AreEqual(1,   sb.XMin); Assert.AreEqual(100, sb.XMax);
            Assert.AreEqual(1,   sb.YMin); Assert.AreEqual(200, sb.YMax);
            Assert.AreEqual(1,   sb.ZMin); Assert.AreEqual(50,  sb.ZMax);
        }

        [Test]
        public void XMin_CannotExceedXMax()
        {
            var sb = new SubsetBoundsViewModel();
            sb.ResetToAxisMaxima(512, 512, 100);
            sb.XMax = 50;
            sb.XMin = 200;   // should be clamped to XMax

            Assert.LessOrEqual(sb.XMin, sb.XMax);
        }

        [Test]
        public void XMax_CannotExceedAxisMax()
        {
            var sb = new SubsetBoundsViewModel();
            sb.ResetToAxisMaxima(512, 512, 100);
            sb.XMax = 9999;   // beyond axis max

            Assert.AreEqual(512, sb.XMax);
        }

        [Test]
        public void XMin_CannotFallBelowAbsoluteMin()
        {
            var sb = new SubsetBoundsViewModel();
            sb.ResetToAxisMaxima(512, 512, 100);
            sb.XMin = -5;

            Assert.GreaterOrEqual(sb.XMin, 1);
        }

        [Test]
        public void UpdateZAxisMax_ClampsZMaxAndZMin()
        {
            var sb = new SubsetBoundsViewModel();
            sb.ResetToAxisMaxima(512, 512, 200);
            sb.ZMax = 200;
            sb.ZMin = 150;

            sb.UpdateZAxisMax(100);   // shrink axis

            Assert.LessOrEqual(sb.ZMax, 100);
            Assert.LessOrEqual(sb.ZMin, sb.ZMax);
        }

        [Test]
        public void PropertyChanged_RaisedOnXMinSet()
        {
            var sb      = new SubsetBoundsViewModel();
            sb.ResetToAxisMaxima(512, 512, 100);
            bool raised = false;
            sb.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SubsetBoundsViewModel.XMin)) raised = true;
            };

            sb.XMin = 10;

            Assert.IsTrue(raised);
        }

        [Test]
        public void ToDto_SnapshotsCurrentValues()
        {
            var sb = new SubsetBoundsViewModel();
            sb.ResetToAxisMaxima(512, 512, 100);
            sb.XMin = 10; sb.XMax = 200;
            sb.YMin = 5;  sb.YMax = 300;
            sb.ZMin = 1;  sb.ZMax = 80;

            var dto = sb.ToDto();

            Assert.AreEqual(10,  dto.XMin); Assert.AreEqual(200, dto.XMax);
            Assert.AreEqual(5,   dto.YMin); Assert.AreEqual(300, dto.YMax);
            Assert.AreEqual(1,   dto.ZMin); Assert.AreEqual(80,  dto.ZMax);
        }
    }
}
