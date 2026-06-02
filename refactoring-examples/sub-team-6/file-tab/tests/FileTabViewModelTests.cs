// brief §6.6 | File tab AFTER — unit tests for FileTabViewModel and SubsetBoundsViewModel
// Plain NUnit 3 with no Unity anywhere, so this just runs under `dotnet test` — no
// editor, no scene, no play mode. That's the whole point of the MVVM split, and it's
// our testability evidence for the File tab (NFR-TST-1, Section 9.2).
// Run with: dotnet test file-tab/tests/FileTabTests.csproj
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using iDaVIE.Desktop.FileTab;
using NUnit.Framework;

namespace iDaVIE.Desktop.FileTab.Tests
{
    // Test doubles
    // A test double is a stand-in object that replaces a real dependency during a test — a controllable fake whose behaviour the test sets up and inspects, so the VM can be exercised without the real FITS plugin, file dialog, or volume server.
    // Hand-written stubs standing in for the four services the VM depends on.
    // Each one lets a test say "next call returns this" or "next call throws that" and then check what the VM did with it. No mocking framework — the interfaces are small enough that fakes are clearer than Moq setup.

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
    // Builders for the FitsFileInfo shapes the tests reach for again and again:
    // a normal 3-D cube, a flat 2-D image (should be rejected), and a 4-axis cube
    // (exercises the extra Z-axis picker). Defaults give a valid cube; pass args
    // when a test needs odd dimensions.

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

    // FileTabViewModel tests

    [TestFixture]
    public sealed class FileTabViewModelTests
    {
        // Spin up a VM wired to fresh stubs and hand them all back so a test can
        // poke the ones it cares about (and ignore the rest with `_`). The dialog
        // is pre-loaded with a path so most tests can just call BrowseImage and go.
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

        // Browse image

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

        // Browse mask

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

        // Load

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

        // Aspect ratio (RatioMode → ZScale)

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

        // Memory-feasibility warning (CheckMemSpaceForCubes equivalent)

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

        // CubeLoaded event (peer-tab subscription point)

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

        // Clear mask

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

        // PropertyChanged

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
        // Filling the coverage gap (audit F14 follow-up, 2026-05-27).
        //
        // Everything above is happy-path plus one error case each, which is what
        // you write first — but ReportGenerator showed it left FileTabViewModel
        // sitting in the high 60s for branch coverage, under the 70% we committed
        // to in D5. So this second block deliberately chases the branches that
        // were still showing red, rather than adding more of the same:
        //
        //   - the four constructor null-guards
        //   - the "set to the value it already holds, so do nothing" early-outs
        //     on the two index setters
        //   - calling those setters before any image is open: they have to quietly
        //     no-op, not throw
        //   - Dispose() on a VM that never opened a file (both handles still null)
        //   - the NAXIS >= 3 but fewer than three real axes case
        //   - BrowseMask when the user cancels the dialog
        //   - BrowseMask replacing an existing mask, where the old handle must
        //     get disposed
        //   - the X and Y mask-mismatch branches — the earlier test only tripped
        //     the Z one, so two of the three checks were never run
        //
        // With these in, the File tab is over the 70% branch gate.
        // ═══════════════════════════════════════════════════════════════════════

        // ── Constructor null guards (one per injected service) ────────────────

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

        // ── Setters that no-op when the value hasn't changed ──────────────────

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

        // ── Setters poked before any image is open ────────────────────────────

        [Test]
        public void SelectedHduIndex_SetBeforeImage_NoOpAndDoesNotThrow()
        {
            // Nothing is open yet, so setting the HDU index kicks off a header
            // refresh and a Z-axis update against a null image. Both have to see
            // the null and bail out quietly instead of throwing.
            var (vm, _, _, _, _) = BuildVm();

            Assert.DoesNotThrow(() => vm.SelectedHduIndex = 1);
            Assert.IsNull(vm.HeaderText);
            Assert.IsFalse(vm.IsLoadable);
        }

        // ── Dispose on a VM that never opened anything ────────────────────────

        [Test]
        public void Dispose_VmWithNoOpenFiles_DoesNotThrow()
        {
            // Every other test opens an image first, so the ?.Dispose() calls in
            // Dispose() always had a handle to close. Here both are still null —
            // tearing the panel down without ever loading a file shouldn't blow up.
            var (vm, _, _, _, _) = BuildVm();

            Assert.DoesNotThrow(() => vm.Dispose());
        }

        // ── IsLoadable: needs at least three real axes ────────────────────────

        [Test]
        public async Task IsLoadable_ThreeAxesButOnlyTwoNonTrivial_False()
        {
            // NAXIS says 3, but the third axis is only 1 pixel deep — that's a flat
            // image dressed up as a cube, not something we can render. IsLoadable
            // counts the axes bigger than 1 and should reject this one.
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

        // ── BrowseMask edge cases (cancel, replace, X/Y mismatch) ─────────────

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

            // Pick a second mask. The first one's file handle is still open, so
            // swapping it in has to dispose the old handle or we leak it.
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
            // BrowseMask_MismatchedDimensions_RejectsMask only ever tripped the
            // Z check. Here the X dimension is the one that's off, so the mask
            // still has to be turned away.
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
            // Same idea, but this time it's the Y dimension that doesn't line up.
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

    // SubsetBoundsViewModel tests

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
