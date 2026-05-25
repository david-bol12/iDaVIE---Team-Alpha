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

        public Task<string> GetHeaderTextAsync(string path, int hduIndex, CancellationToken ct = default)
            => Task.FromResult(NextHeaderText);
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

        public Task LoadCubeAsync(
            LoadCubeRequest request,
            IProgress<float>? progress = null,
            CancellationToken ct = default)
        {
            if (ThrowOnLoad != null) throw ThrowOnLoad;
            LastRequest = request;
            progress?.Report(1f);
            return Task.CompletedTask;
        }
    }

    // ── Shared fixture helpers ─────────────────────────────────────────────────

    internal static class FitsFactory
    {
        public static FitsFileInfo Cube3D(int x = 512, int y = 512, int z = 100,
            string path = "/data/test.fits") => new FitsFileInfo
        {
            FilePath   = path,
            HduList    = new[] { new HduInfo(1, "PRIMARY", "IMAGE") },
            NAxis      = 3,
            AxisSizes  = new Dictionary<int, long> { [1] = x, [2] = y, [3] = z },
            HeaderText = $"NAXIS  = 3\nNAXIS1 = {x}\nNAXIS2 = {y}\nNAXIS3 = {z}\n",
        };

        public static FitsFileInfo TwoDimImage(string path = "/data/flat.fits") => new FitsFileInfo
        {
            FilePath   = path,
            HduList    = new[] { new HduInfo(1, "PRIMARY", "IMAGE") },
            NAxis      = 2,
            AxisSizes  = new Dictionary<int, long> { [1] = 512, [2] = 512 },
            HeaderText = "NAXIS  = 2\n",
        };

        public static FitsFileInfo FourAxisCube(string path = "/data/4d.fits") => new FitsFileInfo
        {
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
                         StubFileDialogService dialog, StubVolumeService volume)
            BuildVm(string? initialDialogPath = "/data/test.fits")
        {
            var fits   = new StubFitsService();
            var dialog = new StubFileDialogService { PathToReturn = initialDialogPath };
            var vol    = new StubVolumeService();
            return (new FileTabViewModel(fits, dialog, vol), fits, dialog, vol);
        }

        // ── Browse image ──────────────────────────────────────────────────────

        [Test]
        public async Task BrowseImage_ValidCube_SetsImagePathAndIsLoadable()
        {
            var (vm, fits, _, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D();

            await vm.BrowseImageCommand.ExecuteAsync();

            Assert.AreEqual("/data/test.fits", vm.ImagePath);
            Assert.IsTrue(vm.IsLoadable);
            Assert.IsNull(vm.ValidationMessage);
        }

        [Test]
        public async Task BrowseImage_UserCancels_LeavesStateUnchanged()
        {
            var (vm, fits, dialog, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D();
            dialog.PathToReturn  = null;   // simulate cancel

            await vm.BrowseImageCommand.ExecuteAsync();

            Assert.IsNull(vm.ImagePath);
            Assert.IsFalse(vm.IsLoadable);
        }

        [Test]
        public async Task BrowseImage_TwoDimensionalFile_IsNotLoadable()
        {
            var (vm, fits, _, _) = BuildVm();
            fits.NextImageResult = FitsFactory.TwoDimImage();

            await vm.BrowseImageCommand.ExecuteAsync();

            Assert.IsFalse(vm.IsLoadable);
            Assert.IsNotNull(vm.ValidationMessage);
        }

        [Test]
        public async Task BrowseImage_FitsOpenThrows_SetsValidationMessage()
        {
            var (vm, fits, _, _) = BuildVm();
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
            var (vm, fits, dialog, _) = BuildVm();
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
            var (vm, fits, _, _) = BuildVm();
            var info = new FitsFileInfo
            {
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
            var (vm, fits, _, _) = BuildVm();
            fits.NextImageResult = FitsFactory.FourAxisCube();

            await vm.BrowseImageCommand.ExecuteAsync();

            // 4 axes, all > 1 → z-axis dropdown should have options
            Assert.Greater(vm.ZAxisOptions.Count, 0);
        }

        [Test]
        public async Task BrowseImage_ResetSubsetToFullCubeExtents()
        {
            var (vm, fits, _, _) = BuildVm();
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
            var (vm, fits, dialog, _) = BuildVm();
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
            var (vm, fits, dialog, _) = BuildVm();
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
            var (vm, _, _, _) = BuildVm();

            // BrowseMaskCommand should not be executable without a loaded image
            Assert.IsFalse(vm.BrowseMaskCommand.CanExecute());
        }

        // ── Load ───────────────────────────────────────────────────────────────

        [Test]
        public async Task Load_ValidFile_PassesCorrectPathAndHduIndex()
        {
            var (vm, fits, dialog, vol) = BuildVm();
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
            var (vm, fits, _, vol) = BuildVm();
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
            var (vm, fits, _, vol) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D();

            await vm.BrowseImageCommand.ExecuteAsync();
            vm.SubsetEnabled = false;
            await vm.LoadCommand.ExecuteAsync();

            Assert.IsNull(vol.LastRequest!.Subset);
        }

        [Test]
        public async Task Load_WithMask_PassesMaskPathInRequest()
        {
            var (vm, fits, dialog, vol) = BuildVm();
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
            var (vm, fits, _, vol) = BuildVm();
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
            var (vm, _, _, _) = BuildVm();
            Assert.IsFalse(vm.LoadCommand.CanExecute());
        }

        // ── Clear mask ─────────────────────────────────────────────────────────

        [Test]
        public async Task ClearMask_RemovesMaskPath()
        {
            var (vm, fits, dialog, _) = BuildVm();
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
            var (vm, fits, _, _) = BuildVm();
            fits.NextImageResult = FitsFactory.Cube3D();
            var raised = new HashSet<string?>();
            vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

            await vm.BrowseImageCommand.ExecuteAsync();

            Assert.Contains(nameof(IFileTabViewModel.ImagePath), raised);
        }

        [Test]
        public async Task PropertyChanged_IsLoading_ToggledDuringBrowse()
        {
            var (vm, fits, _, _) = BuildVm();
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
