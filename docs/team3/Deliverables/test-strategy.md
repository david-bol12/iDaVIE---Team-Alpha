# Sub-team 3: Rendering Engine — Test Strategy
**Team Alpha | Cache Me If You Can**
*Brief reference: Section 9.2 Deliverable 4 + Section 6.3 Software Testing*
*Version: 1.1 | Updated: 2 June 2026*

---

## 1. Purpose and Scope

This document defines the test strategy for Sub-team 3's volume rendering refactoring. It covers how we validate that refactored code is correct, that existing behaviour is preserved, and that the proposed architecture is demonstrably more testable than the monolith it replaces.

The central architectural guarantee is that rendering logic becomes **independently testable** without a running Unity scene, GPU, or VR hardware. The two worked examples produce 60+ concrete NUnit tests that prove this is achievable.

Test files:
- `refactoring-examples/team3/tests/Example1_RendererSplitTests.cs` — renderer four-class split
- `refactoring-examples/team3/tests/Example2_MaskModeTests.cs` — mask mode Strategy pattern
- `refactoring-examples/team3/tests/GoldenImageRegressionTests.cs` — golden-image regression suite

---

## 2. Objectives

1. **Validate correctness** — confirm all refactored classes produce identical outputs to pre-refactoring behaviour under equivalent inputs.
2. **Prevent regression** — ensure changes to the rendering layer do not silently break adjacent or dependent functionality.
3. **Reduce defect risk in hotspots** — CodeScene identifies `VolumeDataSetRenderer` as a critical hotspot (Code Health 4.8, declining from 5.5 a year ago). Tests directly target this.
4. **Measure testability improvement** — the before/ code had zero unit-testable surface; the after/ architecture targets ≥70% branch/line coverage on domain classes (brief §7.2).

---

## 3. Baseline (Before Refactoring)

| Metric | Baseline (May 2026) |
|---|---|
| Hotspot Code Health (`VolumeDataSetRenderer`) | 4.8 — Problematic |
| Average Codebase Code Health | 6.8 — Problematic |
| Worst Performer | 1.9 — Unhealthy |
| Unit-testable methods in `VolumeDataSetRenderer` | 0 (all Unity/SteamVR bound) |
| Files with declining health | 14 |

---

## 4. Test Tiers

### Tier 1 — Pure NUnit (no Unity runtime)

All logic tests that do not touch `Material`, `Renderer`, or Unity player-loop APIs. Runs in headless CI — no Unity Editor licence required.

Covers:
- `FoveatedSamplingPolicy` — `ComputeParameters`, `ComputeMipBias`, `IsGazeAvailable`
- `FoveationParameters.Uniform()` — fallback struct factory
- `FoveatedSamplingConfig.Default` — Inspector-constant regression guard
- `VolumeCoordinateService` — `WorldToObjectSpace`, `ObjectToNormalisedVolume`, `ExtractFrustumPlanes`
- `StubCameraDriver` — `ComputeFrame`, `SetProjectionMode` round-trip
- `ShaderKeyword` properties on all `IMaskMode` implementations

### Tier 2 — Unity Edit Mode (Material required, no player loop)

Tests that call `Material.EnableKeyword` / `Material.GetFloat`. Run in Unity Test Runner Edit Mode; no GPU or running game required.

Covers:
- `Apply()` mutual-exclusion invariant for `ApplyMaskMode`, `InverseMaskMode`, `IsolateMaskMode`
- `_MaskAlpha` uniform values (1.0 for Apply/Inverse, 0.15 for Isolate)
- `DisabledMaskMode` — Null Object safety (`Apply(mat, null)` never throws, all keywords off, idempotent)
- `StrategySubstitutabilityTests` — LSP round-robin over six mode switches

### Tier 3 — Play Mode / Integration

Tests requiring a running Unity player, GPU, or physical HMD. Nightly on reference machine.

- 90 fps invariant (INV-01): per-frame CPU time ≤ 2 ms on rendering layer
- Golden-image regression suite across all mask modes and colour maps (`GoldenImageRegressionTests.cs`)
- `CoordinatorWiringTest` — end-to-end coordinator wiring with injected stubs

---

## 5. What the Tests Prove

### Example 1 — VolumeDataSetRenderer Split

The monolithic `VolumeDataSetRenderer.Update()` block (before/ lines 1139–1165) could only be executed by booting Unity with a scene and a connected HMD. After the split:

| Class | Can now be tested because |
|---|---|
| `FoveatedSamplingPolicy` | Depends on `IGaze` interface; `StubGazeProvider` injects any gaze state |
| `VolumeCoordinateService` | Pure `static` math; no `Transform` dependency, no `MonoBehaviour` |
| `StubCameraDriver` | Implements `IVolumeCameraDriver`; returns controllable `CameraFrameState` |

Critical test: `ComputeParameters_GazeUnavailable_BothStepCountsEqualMaxSteps` — directly mirrors the `else`-branch of before/ lines 1163–1165 that previously had zero test coverage.

### Example 2 — Mask Mode Strategy Pattern

The before/ switch statement was a single block exercisable only through a live render. After the Strategy pattern:

- Each `IMaskMode` class is tested in isolation.
- The mutual-exclusion invariant (exactly one keyword active) is verified by an exhaustive six-step transition sequence.
- `IsolateMaskMode._MaskAlpha = 0.15` has a dedicated regression test — the only active mode that differs from 1.0.
- `DisabledMaskMode` (Null Object) makes the "no mask loaded" state explicit and testable; previously this was an implicit null check inside the God Class.

---

## 6. Stub and Mock Strategy

| Dependency | Before (untestable) | After (stub used in tests) |
|---|---|---|
| Eye-tracking SDK | Direct `SteamVR_Action_Vector2` call | `StubGazeProvider` implements `IGaze` |
| Unity `Camera` | Direct `Camera.main` access | `StubCameraDriver` implements `IVolumeCameraDriver` |
| URP/HDRP adapter | Direct `UniversalRenderPipeline` import | `NullRenderPipeline` implements `IRenderPipeline` |
| Sub-team 2 data | Not injectable | Synthetic `RawVolumeData` (`byte[] Voxels`, `long` dims) passed directly to `VolumeTextureManager` |

Naming convention: `Stub*` for test doubles with configurable state; `Null*` for no-op implementations.

---

## 7. Coverage Targets (Brief §7.2)

| Scope | Line/Branch coverage target |
|---|---|
| Domain classes (all Tier 1) | ≥ 70% |
| Overall (all tiers combined) | ≥ 50% |
| Unity-bound code (`UrpRenderPipeline`, `VolumeMaterialBinder.Tick`) | Tracked but not in strict target |

Coverage gate blocks merges from Day 10 (§10.3).

---

## 8. Risk-Based Prioritisation

**Critical — test first:** `VolumeDataSetRenderer` and its split classes. Hotspot + declining health = highest defect probability.

**High — test before merge:** Any file in the unhealthy range (code health < 4.0) touched during extraction. The Worst Performer (1.9) requires 90% coverage and paired review.

**Standard:** Remaining modified files. Normal unit + regression coverage applies.

Knowledge gap risk: CodeScene identifies 13 possible ex-developers with code in this area. Before touching any unfamiliar file, assign a code owner and run a brief knowledge-transfer session.

---

## 9. What Is Not Tested Here (and Why)

| Area | Reason |
|---|---|
| GPU shader compilation | Hardware-bound; covered by integration test on reference machine |
| `VolumeMaterialBinder.Tick()` full pipeline | Requires live `Material` on a real shader; play-mode scope |
| Frame rate (90 fps invariant) | Play-mode `[UnityTest]`, requires player loop |
| Sub-team 4 gaze SDK | Out of scope; `StubGazeProvider` substitutes |
| FITS loading | Sub-team 2's responsibility |
| HDRP migration | Deferred; `HdrpRenderPipeline` design complete, testing out of sprint scope |

---

## 10. Unity 6 / URP Migration Testing

*Full 15-step migration sequence: `docs/team3/exploration/migration_plan.md`*

Because `IRenderPipeline` is in place before migration begins, **all render-loop changes are confined to two files** — `UrpRenderPipeline.cs` and `VolumeRenderFeature.cs`. The five domain classes are untouched during migration. This is a testable invariant: the domain assembly must not import `UnityEngine.Rendering.Universal` at any point.

### Migration Categories and Test Coverage

| Category | Changes | Test approach | Tier |
|---|---|---|---|
| 1 — Render Loop | `OnRenderObject` → `ScriptableRenderPass`; `DrawProceduralNow` → `cmd.DrawProcedural` | Visual verification in Unity 6 player; `NullRenderPipeline` confirms domain unchanged | Play-mode |
| 2 — Shader Keywords | `Shader.EnableKeyword` → `material.SetKeyword(LocalKeyword)` | Existing `IMaskMode` Tier 2 tests; new `LocalKeyword` round-trip test | Tier 1 + 2 |
| 3 — C# API Deprecations | `FindObjectOfType` → `FindAnyObjectByType`; `Config.Instance` → `IAppConfig` | Tier 1 unit tests on injected `IAppConfig`; compile-time verification | Tier 1 |
| 4 — Shader Language | CGPROGRAM → HLSL macros, sampler declarations | Shader compilation gate in CI; INV-04 nearest-neighbour visual check | Shader compiler + visual |

### Per-Phase Exit Conditions (Strangler Fig, 7 phases)

| Phase | Exit condition |
|---|---|
| 1 — `IRenderPipeline` seam | All Tier 1 domain tests pass; no `UnityEngine.Rendering.Universal` import in domain assembly |
| 2 — `IMaskMode` strategies | All `IMaskMode` Tier 1 + Tier 2 tests pass; `LocalKeyword` variant verified |
| 3 — `FoveatedSamplingPolicy` | Tier 1 tests pass; `IsGazeAvailable = false` fallback path verified |
| 4 — `VolumeMaterialBinder` | Tier 2 tests pass; `DepthTextureAvailable` check verified |
| 5 — `VolumeCameraDriver` | `VolumeCoordinateService` Tier 1 tests pass; `StubCameraDriver` round-trip verified |
| 6 — `VolumeTextureManager` shadow mode | Frame comparison matches between old and new path; no 90 fps regression (CI: per-frame CPU ≤ 2 ms) |
| 7 — `VolumeRenderCoordinator` takes over | `CoordinatorWiringTest` passes; 90 fps INV-01 passes on reference machine |

---

## 11. CI Integration

- **Tier 1:** Every PR via GitHub Actions (headless, no Unity licence)
- **Tier 2 (Edit Mode):** Every PR where a Unity Editor runner is available
- **Tier 3 (Play Mode):** Nightly on reference machine
- **Coverage gate:** ≥ 70% domain branch/line blocks merge from Day 10

---

## 12. Coverage Summary

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

---

## 13. Success Metrics

| Metric | Baseline (May 2026) | Target |
|---|---|---|
| Hotspot Code Health | 4.8 | ≥ 6.0 |
| Average Code Health | 6.8 | ≥ 7.5 |
| Worst Performer | 1.9 | ≥ 3.0 |
| Unhealthy code (%) | 21% | ≤ 10% |
| Unit test coverage — domain classes | 0% | ≥ 70% |
| Regression failures on merge | Not tracked | 0 |
