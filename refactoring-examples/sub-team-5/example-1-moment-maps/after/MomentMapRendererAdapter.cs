/*
 * REFACTORING EXAMPLE 1 — Moment-Map Generation
 * Sub-team 5: Feature System and Domain Model
 *
 * MomentMapRendererAdapter.cs — AFTER STATE (new file, design-level example)
 * ===========================================================================
 * Design role: thin MonoBehaviour adapter — the ONLY class in Example 1
 *              that imports UnityEngine.
 *
 * NAMESPACE
 * ─────────
 * iDaVIE.Infrastructure.Unity  (ADR-008)
 *
 * ADR-006 COMPLIANCE — Separation of MonoBehaviour from Domain Logic
 * ──────────────────────────────────────────────────────────────────
 * Before: MomentMapRenderer : MonoBehaviour contained:
 *   • ComputeShader dispatch (GPU) — Infrastructure concern
 *   • RenderTexture management    — Infrastructure concern
 *   • Threshold comparison        — Domain/Application concern
 *   • Bounds extraction           — Domain concern
 *   • Config.Instance access      — Singleton violation (ADR-003)
 *   • UI sprite and colour-bar update — Presentation concern
 *   All mixed in ~380 lines of MonoBehaviour.
 *
 * After: Responsibilities are split across layers:
 *   ┌─ iDaVIE.Infrastructure.Unity ─────────────────────────────────────┐
 *   │ MomentMapRendererAdapter : MonoBehaviour, IMomentMapAdapter        │
 *   │  • Unity lifecycle (Awake, Start)                                  │
 *   │  • Holds ComputeShader + RenderTexture GPU resources               │
 *   │  • Implements IMomentMapAdapter.Compute() — GPU dispatch only      │
 *   │  • Calls service to get result, then uploads to GPU / UI           │
 *   │  • The ONLY class with "using UnityEngine" in Example 1            │
 *   └────────────────────────────────────────────────────────────────────┘
 *   ┌─ iDaVIE.Application.Feature ──────────────────────────────────────┐
 *   │ MomentMapService           — orchestrates adapter + calculator     │
 *   │ IMomentMapAdapter          — GPU abstraction seam                  │
 *   │ MomentMapRequest / Result  — plain C# value objects               │
 *   └────────────────────────────────────────────────────────────────────┘
 *   ┌─ iDaVIE.Domain.Feature ───────────────────────────────────────────┐
 *   │ MomentMapCalculator        — pure bounds math                      │
 *   └────────────────────────────────────────────────────────────────────┘
 *
 * ADR-003 DEPENDENCY INJECTION
 * ────────────────────────────
 * In production, MomentMapService is resolved via the composition root and
 * injected. The adapter (this) is passed as the IMomentMapAdapter to the
 * service — forming a deliberate circular delegation:
 *   Adapter → Service → Adapter (via IMomentMapAdapter interface)
 * This is the standard Separated Interface / Dependency Inversion pattern:
 * the service depends on the interface, not the concrete adapter.
 *
 * No singletons, no Config.Instance, no static access (ADR-003 ✓).
 *
 * ADR-006 RULE — MonoBehaviour responsibilities:
 *   ✓ Subscribe to Unity lifecycle events
 *   ✓ Delegate to domain/application services via injected interfaces
 *   ✓ Bind data to Unity GameObjects / UI
 *   ✗ NO conditional business logic beyond trivial null-guards
 */

using System;
using UnityEngine;
using UnityEngine.UI;
using iDaVIE.Application.Feature;

namespace iDaVIE.Infrastructure.Unity
{
    /// <summary>
    /// Thin MonoBehaviour adapter for moment-map generation and display.
    /// <para>
    /// Implements <see cref="IMomentMapAdapter"/> to provide the GPU compute
    /// back-end. Delegates all orchestration to the injected
    /// <see cref="IMomentMapService"/>. Handles Unity GPU and UI concerns after
    /// receiving the plain <see cref="MomentMapResult"/> from the service.
    /// </para>
    /// <para>
    /// This is the <em>only</em> class in Example 1 that imports
    /// <c>UnityEngine</c> — satisfying ADR-002 and ADR-006.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(VolumeDataSetRenderer))]
    public sealed class MomentMapRendererAdapter : MonoBehaviour, IMomentMapAdapter
    {
        // ── Service dependency (injected at composition root) ─────────────────
        private IMomentMapService _momentMapService;

        // ── Unity GPU resources ───────────────────────────────────────────────
        private ComputeShader _computeShader;
        private int   _kernelIndex;
        private int   _kernelIndexMasked;
        private uint  _threadGroupX;
        private uint  _threadGroupY;

        // ── Scene references (set via Inspector or DI) ────────────────────────
        [SerializeField] private MomentMapMenuController _momentMapMenuController;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            _computeShader     = (ComputeShader)Resources.Load("MomentMapGenerator");
            _kernelIndex       = _computeShader.FindKernel("MomentsGenerator");
            _kernelIndexMasked = _computeShader.FindKernel("MaskedMomentsGenerator");
            _computeShader.GetKernelThreadGroupSizes(
                _kernelIndex, out _threadGroupX, out _threadGroupY, out _);

            // Resolve service from composition root (ADR-003).
            // In production this would use a DI container; here we wire manually
            // to keep the example self-contained.
            _momentMapService = new MomentMapService(this);
        }

        // ── Public trigger (called by owning VolumeDataSetRenderer adapter) ───

        /// <summary>
        /// Builds a <see cref="MomentMapRequest"/> from the current scene state,
        /// delegates to the service, and applies the result to the Unity UI.
        /// All business logic (threshold application, bounds) happens in the
        /// service and calculator — not here.
        /// </summary>
        public void RefreshMomentMaps(
            float[] dataVoxels, float[] spectrumZ,
            int width, int height, int depth,
            float threshold, bool useMask, float[] maskVoxels = null)
        {
            var request = new MomentMapRequest(
                dataVoxels, spectrumZ, width, height, depth, threshold, useMask, maskVoxels);

            MomentMapResult result = _momentMapService.GenerateMomentMaps(request);

            // Pure Infrastructure concern: upload pixel arrays to GPU textures
            // and push bounds to the Unity UI colour-bar components.
            ApplyResultToUI(result);
        }

        // ── IMomentMapAdapter — GPU dispatch ──────────────────────────────────

        /// <inheritdoc/>
        /// <remarks>
        /// Called by <see cref="MomentMapService"/> via the <see cref="IMomentMapAdapter"/>
        /// interface. This method is the sole GPU entry point in Example 1.
        /// </remarks>
        (float[] moment0, float[] moment1) IMomentMapAdapter.Compute(MomentMapRequest request)
        {
            bool useMask = request.UseMask && request.MaskVoxels != null;
            int  kernel  = useMask ? _kernelIndexMasked : _kernelIndex;

            // Transient render textures — created per dispatch, released after readback.
            var m0Tex = CreateRenderTexture(request.Width, request.Height);
            var m1Tex = CreateRenderTexture(request.Width, request.Height);

            _computeShader.SetTexture(kernel, "Moment0Result", m0Tex);
            _computeShader.SetTexture(kernel, "Moment1Result", m1Tex);
            _computeShader.SetInt("Depth", request.Depth);
            _computeShader.SetFloat("Threshold", request.Threshold);

            var spectrumBuf = new ComputeBuffer(request.SpectrumZ.Length, sizeof(float));
            spectrumBuf.SetData(request.SpectrumZ);
            _computeShader.SetBuffer(kernel, "Spectrum", spectrumBuf);

            int groupsX = Mathf.CeilToInt(request.Width  / (float)_threadGroupX);
            int groupsY = Mathf.CeilToInt(request.Height / (float)_threadGroupY);
            _computeShader.Dispatch(kernel, groupsX, groupsY, 1);

            float[] m0 = ReadbackTexture(m0Tex, request.Width, request.Height);
            float[] m1 = ReadbackTexture(m1Tex, request.Width, request.Height);

            spectrumBuf.Release();
            m0Tex.Release();
            m1Tex.Release();

            return (m0, m1);
        }

        // ── Private Unity helpers ─────────────────────────────────────────────

        private static RenderTexture CreateRenderTexture(int width, int height)
        {
            var rt = new RenderTexture(width, height, 0,
                                       RenderTextureFormat.RFloat,
                                       RenderTextureReadWrite.Linear)
            {
                enableRandomWrite = true,
                filterMode        = FilterMode.Point
            };
            rt.Create();
            return rt;
        }

        private static float[] ReadbackTexture(RenderTexture rt, int width, int height)
        {
            var tex  = new Texture2D(width, height, TextureFormat.RFloat, false);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            // NativeArray<float> → managed float[] (Unity Collections)
            return tex.GetRawTextureData<float>().ToArray();
        }

        /// <summary>
        /// Uploads the computed result to Unity GPU textures and UI colour-bars.
        /// All pure Infrastructure / Unity presentation work — no domain logic.
        /// </summary>
        private void ApplyResultToUI(MomentMapResult result)
        {
            if (_momentMapMenuController == null) return;

            var mapContainer = _momentMapMenuController.transform.Find("Map_container");
            if (mapContainer == null) return;

            // Push moment-0 sprite to UI image component.
            var tex0 = PixelsToTexture2D(result.Moment0Pixels, result.Width, result.Height);
            var img0 = mapContainer.Find("MomentMap0")?.GetComponent<Image>();
            if (img0 != null)
            {
                img0.sprite = Sprite.Create(
                    tex0,
                    new Rect(0, 0, result.Width, result.Height),
                    new Vector2(result.Width, result.Height));
                img0.preserveAspect = true;
            }

            // Push moment-0 bounds to colour-bar widget.
            var bar0 = mapContainer.Find("ColorbarM0")?.GetComponent<Colorbar>();
            if (bar0 != null)
            {
                bar0.ScaleMin = result.Moment0Min;
                bar0.ScaleMax = result.Moment0Max;
            }

            // Push moment-1 sprite and bounds.
            var tex1 = PixelsToTexture2D(result.Moment1Pixels, result.Width, result.Height);
            var img1 = mapContainer.Find("MomentMap1")?.GetComponent<Image>();
            if (img1 != null)
            {
                img1.sprite = Sprite.Create(
                    tex1,
                    new Rect(0, 0, result.Width, result.Height),
                    new Vector2(result.Width, result.Height));
                img1.preserveAspect = true;
            }

            var bar1 = mapContainer.Find("ColorbarM1")?.GetComponent<Colorbar>();
            if (bar1 != null)
            {
                bar1.ScaleMin = result.Moment1Min;
                bar1.ScaleMax = result.Moment1Max;
            }
        }

        private static Texture2D PixelsToTexture2D(float[] pixels, int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RFloat, false);
            tex.filterMode = FilterMode.Point;
            tex.LoadRawTextureData(ConvertFloatArrayToBytes(pixels));
            tex.Apply();
            return tex;
        }

        private static byte[] ConvertFloatArrayToBytes(float[] values)
        {
            var bytes = new byte[values.Length * sizeof(float)];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            return bytes;
        }
    }
}
