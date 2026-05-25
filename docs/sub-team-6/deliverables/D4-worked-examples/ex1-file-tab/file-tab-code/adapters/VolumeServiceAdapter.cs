// WE1-3 | File tab AFTER — VolumeServiceAdapter (Unity-assembly adapter)
// Owns coroutine lifecycle, prefab instantiation, and native memory cleanup.
// The ViewModel never touches VolumeCommandController, VolumeDataSetRenderer,
// or any Unity coroutine directly. Satisfies ADR-0002 (transport contract) and
// ADR-0003 (ACL).
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using iDaVIE.Desktop.FileTab;
using UnityEngine;
using VolumeData;

namespace iDaVIE.Desktop.Adapters.FileTab
{
    /// <summary>
    /// Concrete adapter for <see cref="IVolumeService"/>.
    /// Translates a plain <see cref="LoadCubeRequest"/> DTO into the scene-graph
    /// operations that were previously scattered inside CanvassDesktop.LoadCubeCoroutine
    /// (lines 1015–1131): renderer teardown, prefab instantiation, field assignment,
    /// coroutine management, and polling on <c>started</c>.
    ///
    /// Attach to the same GameObject as the scene's VolumeDataSetManager.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VolumeServiceAdapter : MonoBehaviour, IVolumeService
    {
        [SerializeField] private GameObject _cubePrefab            = null!;
        [SerializeField] private GameObject _volumeDataSetManager  = null!;

        private VolumeCommandController? _commandController;
        private VolumeInputController?   _inputController;

        private void Awake()
        {
            // Replaces the FindObjectOfType calls that CanvassDesktop.Start() made
            _commandController = FindObjectOfType<VolumeCommandController>();
            _inputController   = FindObjectOfType<VolumeInputController>();
        }

        public bool IsCubeLoaded => FindFirstActiveRenderer() != null;

        public Task LoadCubeAsync(
            LoadCubeRequest request,
            IProgress<float>? progress = null,
            CancellationToken ct = default)
        {
            var tcs = new TaskCompletionSource<bool>();
            StartCoroutine(LoadCubeCoroutine(request, progress, tcs));
            return tcs.Task;
        }

        // ── Coroutine — owns all Unity scene operations ────────────────────────

        private IEnumerator LoadCubeCoroutine(
            LoadCubeRequest request,
            IProgress<float>? progress,
            TaskCompletionSource<bool> tcs)
        {
            progress?.Report(0f);

            Exception? failure = null;
            try
            {
                // Phase 1: tear down any existing cube
                // (replaces CanvassDesktop.LoadCubeCoroutine lines 1041–1070)
                var existing = FindFirstActiveRenderer();
                if (existing != null)
                {
                    existing.gameObject.SetActive(false);
                    _commandController!.RemoveDataSet(existing);

                    if (_inputController != null)
                    {
                        _inputController.gameObject.SetActive(false);
                        _inputController.gameObject.SetActive(true);
                    }

                    _commandController.DisablePaintMode();
                    _commandController.endThresholdEditing();
                    _commandController.endZAxisEditing();

                    existing.Data.CleanUp(existing.RandomVolume);
                    existing.Mask?.CleanUp(false);
                    Destroy(existing);
                }
            }
            catch (Exception ex)
            {
                failure = ex;
            }

            if (failure != null)
            {
                tcs.TrySetException(failure);
                yield break;
            }

            progress?.Report(0.2f);
            yield return null;

            // Phase 2: instantiate new cube prefab
            // (replaces CanvassDesktop.LoadCubeCoroutine lines 1073–1094)
            GameObject newCube;
            VolumeDataSetRenderer renderer;
            try
            {
                newCube = Instantiate(_cubePrefab, Vector3.zero, Quaternion.identity);
                newCube.transform.SetParent(_volumeDataSetManager.transform, false);
                newCube.SetActive(true);

                renderer = newCube.GetComponent<VolumeDataSetRenderer>();

                // Assign fields via plain DTO — no public mutable access from CanvassDesktop
                renderer.FileName       = request.ImagePath;
                renderer.MaskFileName   = request.MaskPath ?? string.Empty;
                renderer.SelectedHdu    = request.HduIndex;
                renderer.CubeDepthAxis  = request.ZAxisSelection;

                if (request.Subset is { } sb)
                {
                    renderer.subsetBounds = new[] { sb.XMin, sb.XMax, sb.YMin, sb.YMax, sb.ZMin, sb.ZMax };
                    renderer.trueBounds   = (int[])renderer.subsetBounds.Clone();
                }
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
                yield break;
            }

            progress?.Report(0.4f);

            // Phase 3: re-toggle input controller to refresh its dataset list
            if (_inputController != null)
            {
                _inputController.gameObject.SetActive(false);
                yield return null;
                _inputController.gameObject.SetActive(true);
            }

            _commandController!.AddDataSet(renderer);
            StartCoroutine(renderer._startFunc());

            // Phase 4: poll until renderer signals ready
            // (replaces CanvassDesktop.LoadCubeCoroutine lines 1116–1119 busy-wait)
            while (!renderer.started)
                yield return new WaitForSeconds(0.1f);

            progress?.Report(1f);
            tcs.TrySetResult(true);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private VolumeDataSetRenderer? FindFirstActiveRenderer()
        {
            if (_volumeDataSetManager == null) return null;
            foreach (var r in _volumeDataSetManager.GetComponentsInChildren<VolumeDataSetRenderer>(true))
                if (r.gameObject.activeSelf) return r;
            return null;
        }
    }
}
