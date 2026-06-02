# Sub-team 3: Rendering Engine — Test Strategy
**Team Alpha | Cache Me If You Can**  
*Brief reference: Section 6.3 Software Testing + Section 9.2 Deliverable 4*

---

## 1. Overview

The central goal of the refactoring is to make rendering logic **independently testable** without a running Unity scene, GPU, or VR hardware. The two worked examples produce 60+ concrete NUnit tests that demonstrate this is achievable today.

Test files:
- `refactoring-examples/team3/tests/Example1_RendererSplitTests.cs` — renderer split
- `refactoring-examples/team3/tests/Example2_MaskModeTests.cs` — mask mode strategy

---

## 2. Test Tiers

### Tier 1 — Pure NUnit (no Unity runtime)

All logic tests that do not touch `Material`, `Renderer`, or player-loop APIs. Safe in headless CI (no Unity Editor licence required).

Covers:
- `FoveatedSamplingPolicy` — `ComputeParameters`, `ComputeMipBias`, `IsGazeAvailable`
- `FoveationParameters.Uniform()` — fallback struct factory
- `FoveatedSamplingConfig.Default` — Inspector-constant regression guard
- `VolumeCoordinateService` — `WorldToObjectSpace`, `ObjectToNormalisedVolume`, `ExtractFrustumPlanes`
- `StubCameraDriver` — `ComputeFrame`, `SetProjectionMode` round-trip
- `ShaderKeyword` properties on all `IMaskMode` implementations

### Tier 2 — Unity Edit Mode (Material required, no player loop)

Tests that call `Material.EnableKeyword` / `Material.GetFloat`. These run in the Unity Test Runner's Edit Mode; they do **not** need a running game or GPU.

Covers:
- `Apply()` mutual-exclusion invariant for `ApplyMaskMode`, `InverseMaskMode`, `IsolateMaskMode`
- `_MaskAlpha` uniform values (1.0 for Apply/Inverse, 0.15 for Isolate)
- `DisabledMaskMode` — Null Object safety (`Apply(mat, null)` never throws, all keywords off, idempotent)
- `StrategySubstitutabilityTests` — LSP round-robin over six mode switches

---

## 3. What the Tests Prove

### Example 1 — VolumeDataSetRenderer Split

The monolithic `VolumeDataSetRenderer.Update()` block (before/ lines 1139–1165) could only be executed by booting Unity with a scene and a connected HMD. After the split:

| Class | Can now be tested because |
|---|---|
| `FoveatedSamplingPolicy` | Depends on `IGazeProvider` interface; `StubGazeProvider` injects any gaze state |
| `VolumeCoordinateService` | Pure `static` math; no `Transform` dependency, no `MonoBehaviour` |
| `StubCameraDriver` | Implements `IVolumeCameraDriver`; test double returns controllable `CameraFrameState` |

Critical test: `ComputeParameters_GazeUnavailable_BothStepCountsEqualMaxSteps` — this directly mirrors the `else`-branch of the before/ code (lines 1163–1165) that previously had no test coverage.

### Example 2 — Mask Mode Strategy

The before/ switch statement was a single block that could only be exercised through a live render. After the Strategy pattern:

- Each `IMaskMode` class is tested in isolation.
- The mutual-exclusion invariant (exactly one keyword active) is enforced per-class and verified by an exhaustive six-step transition sequence.
- `IsolateMaskMode._MaskAlpha = 0.15` has a dedicated regression test — the only active mode that differs from 1.0.
- `DisabledMaskMode` (Null Object) makes the "no mask loaded" state explicit and testable; previously there was an implicit null check inside the God Class.

---

## 4. Stub and Mock Strategy

| Dependency | Before (untestable) | After (stub used in tests) |
|---|---|---|
| Eye-tracking SDK | Direct `SteamVR_Action_Vector2` call | `StubGazeProvider` implements `IGazeProvider` |
| Unity `Camera` | Direct `Camera.main` access | `StubCameraDriver` implements `IVolumeCameraDriver` |
| URP/HDRP adapter | Direct `UniversalRenderPipeline` import | `NullRenderPipeline` / `StubRenderPipeline` implements `IRenderPipeline` |
| Sub-team 2 data | Not injectable | Synthetic `RawVolumeData` passed directly to `VolumeTextureManager` |

Naming convention: `Stub*` for test doubles with configurable state; `Null*` for no-op implementations.

---

## 5. What Is Not Tested Here (and Why)

| Area | Reason |
|---|---|
| GPU shader compilation | Hardware-bound; covered by integration test on reference machine |
| `VolumeMaterialBinder.Tick()` full pipeline | Requires live `Material` bound to a real shader; play-mode scope |
| Frame rate (90 fps invariant) | Play-mode `[UnityTest]`, requires player loop |
| Sub-team 4 gaze SDK | Out of scope; `StubGazeProvider` substitutes until interface is finalised |
| FITS loading | Sub-team 2's responsibility |

---

## 6. Short-Term Impact on the Codebase (Sprint 2–3)

1. **Every new class in the split must have a corresponding test fixture** before its design is considered final. The test file is part of the design deliverable.
2. `FoveatedSamplingConfig.Default` tests serve as a **regression lock** on the Inspector defaults extracted from the before/ code. Any accidental value change will fail immediately.
3. The `ShaderKeyword` tests are a **contract test** between the C# strategy classes and the HLSL `#pragma multi_compile` declarations. A typo in either place fails a test before a render is ever attempted.

---

## 7. Long-Term Impact on the Codebase

1. **New mask modes cost one class and one test fixture.** The before/ switch required modifying `VolumeDataSetRenderer`. The after/ design means adding `SilhouetteMaskMode` or `HeatmapMaskMode` is a zero-change-to-existing-code operation — the test structure mirrors this directly.
2. **IRenderPipeline boundary enables headless CI for all domain logic.** No Unity Editor licence is needed for Tier 1 tests, which covers the entire mathematical core of the renderer. This scales as the team grows.
3. **`VolumeCoordinateService` tests are a safety net for future shader-side coordinate changes.** If `ObjectToNormalisedVolume` or `ExtractFrustumPlanes` is modified during a URP migration, the pure-math tests catch regressions before any render is needed.
4. **`StubCameraDriver` enables future `VolumeCameraDriver` changes to be verified without a live camera.** Camera matrix logic (clip planes, VP matrix, AIP toggle) is tested independently of the Unity Camera component.

---

## 8. CI Integration

- Tier 1 tests: run on every PR via GitHub Actions (headless, no Unity licence required)
- Tier 2 (Edit Mode) tests: run on every PR where a Unity Editor is available on the runner
- Play-mode integration tests: nightly on reference machine
- Coverage gate: ≥ 70% branch/line coverage on domain classes blocks merge from Day 10 (brief §7.2)

---

## 9. Coverage Summary

| Class | Tier | Methods covered |
|---|---|---|
| `FoveatedSamplingPolicy` | 1 | `ComputeParameters`, `ComputeMipBias`, `IsGazeAvailable` |
| `FoveationParameters` | 1 | `Uniform()` |
| `FoveatedSamplingConfig` | 1 | Constructor, all 6 default constants |
| `VolumeCoordinateService` | 1 | `WorldToObjectSpace`, `ObjectToNormalisedVolume`, `ExtractFrustumPlanes` |
| `StubCameraDriver` | 1 | `ComputeFrame`, `SetProjectionMode` |
| `ApplyMaskMode` | 1 + 2 | `ShaderKeyword`, `Apply` |
| `InverseMaskMode` | 1 + 2 | `ShaderKeyword`, `Apply` |
| `IsolateMaskMode` | 1 + 2 | `ShaderKeyword`, `Apply` |
| `DisabledMaskMode` | 1 + 2 | `ShaderKeyword`, `Apply` (idempotence, null-safety) |
| `NullMaskMode` | 1 | `ShaderKeyword` (sentinel check) |
