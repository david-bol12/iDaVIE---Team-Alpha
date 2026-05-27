# Sub-team 3: Rendering Engine — Test Strategy
**Team Alpha | Cache Me If You Can**  
*Brief reference: Section 6.3 Software Testing + Section 9.2 Deliverable 4*  
*Target length: 2–4 pages*

---

## 1. Overview

This document describes how the rendering layer is tested in the proposed architecture.
A key goal of the refactoring is to **improve testability** — currently most rendering
logic is untestable without a full Unity runtime due to direct URP/HDRP dependencies.

---

## 2. Test Types

### 2.1 Edit-Mode Unit Tests (Pure C# — no Unity required)

**What:** Tests for all classes that do not depend on Unity types.  
**Framework:** Unity Test Framework in EditMode, or plain NUnit.  
**Scope:**
- `IMaskMode` implementations (`ApplyMaskMode`, `InverseMaskMode`, `IsolateMaskMode`)
- `FoveatedSamplingPolicy` (with `MockGazeProvider` injected)
- Memory budget calculations in `VolumeTextureManager`
- Camera matrix calculations in `VolumeCameraDriver` (if extractable from Unity)

**Why possible after refactoring:** `IRenderPipeline` abstraction means domain classes
no longer import `UnityEngine.Rendering.Universal`. Mock implementations can be injected.

**Example:**
```csharp
[Test]
public void InverseMaskMode_SetsCorrectShaderKeyword() {
    var mode = new InverseMaskMode();
    Assert.AreEqual("_MASK_INVERSE", mode.ShaderKeyword);
}

[Test]
public void FoveatedPolicy_HighSampleRate_WhenGazeAtCentre() {
    var gaze = new MockGazeProvider { GazeFocusPoint = new Vector2(0.5f, 0.5f) };
    var policy = new FoveatedSamplingPolicy(gaze);
    float rate = policy.GetSampleRate();
    Assert.GreaterOrEqual(rate, 0.8f);
}
```

---

### 2.2 Play-Mode Tests (Unity runtime required)

**What:** Tests that require Unity to be running — rendering behaviour, frame timing, integration.  
**Framework:** Unity Test Framework in PlayMode.  
**Scope:**
- Frame rate test: renderer must sustain ≥ 90 fps on reference test machine
- Mask mode switching: changing `IMaskMode` must take effect within one frame
- Texture upload: `VolumeTextureManager` must upload and bind a test volume correctly
- Memory budget: loading over-budget volume must trigger eviction, not crash

**Example:**
```csharp
[UnityTest]
public IEnumerator Renderer_SustainsNinetyFps_WithDefaultVolume() {
    // Load test volume, warm up 30 frames, measure average over 60 frames
    yield return LoadTestVolume("synthetic_368mb.fits");
    yield return new WaitForSeconds(0.5f);  // warm-up
    
    float totalTime = 0f;
    for (int i = 0; i < 60; i++) {
        yield return null;
        totalTime += Time.deltaTime;
    }
    float avgFrameTime = totalTime / 60f;
    Assert.Less(avgFrameTime, 1f / 90f, "Frame time exceeded 90fps budget");
}
```

---

### 2.3 Golden-Image Regression Tests

**What:** Reference PNG renders for each mask mode × colour map combination.
Any future change that shifts pixel values beyond threshold fails the test.  
**Framework:** Custom Unity Test Framework extension using `Texture2D.LoadImage` comparison.  
**Scope:** 3 mask modes × 5 colour maps = 15 reference renders (minimum).

**How it works:**
1. On first run: render scene → save as `golden/apply_viridis.png`
2. On subsequent runs: render scene → pixel-diff against reference → fail if delta > threshold
3. Threshold: max 1-bit difference per channel (allows minor float precision variation)

**Value:** Catches silent visual regressions from shader changes, colour map tweaks, or
refactoring errors that don't break logic tests but do change what the astronomer sees.

---

## 3. What We Are NOT Testing (and Why)

| Area | Reason not tested |
|------|------------------|
| GPU shader compilation on all target hardware | CI hardware constraints; covered by integration test on reference machine |
| Sub-team 4's gaze tracking SDK | Out of our scope; we test with `MockGazeProvider` |
| FITS data loading | Sub-team 2's responsibility; we use a synthetic `RawVolumeData` in tests |
| Pitch rendering quality at full resolution | Manual review at sprint performance budget review |

---

## 4. Mocking Strategy

### The Problem Before Refactoring
`VolumeDataSetRenderer` directly calls `UnityEngine.Rendering.Universal` APIs.
You cannot mock these in edit-mode tests — they require a GPU context.

### The Solution After Refactoring

| Real dependency | Replaced with | Used in |
|----------------|---------------|---------|
| `URPAdapter` | `MockRenderPipeline` | All edit-mode tests |
| Eye-tracking SDK | `MockGazeProvider` | FoveatedSamplingPolicy tests |
| Sub-team 2 data loader | `MockVolumeDataSource` | VolumeTextureManager tests |

All mocks implement the same interface as the real class — this is enforced by
the architecture constraint that every API boundary must be expressed as an interface.

---

## 5. Coverage Targets

Per brief Section 7.2 (Testability metrics):

| Scope | Target | Note |
|-------|--------|------|
| Domain logic (mask modes, foveation, camera math) | ≥ 70% branch/line coverage | Edit-mode testable |
| Adapter classes (URPAdapter, HDRPAdapter) | ≥ 50% overall | Unity-bound; tracked but not in strict target |
| Play-mode integration tests | Scenario-based (no % target) | Key user flows covered |
| Golden-image suite | 15 renders minimum | All mask × colourmap combos |

CI gate: branch/line coverage below 70% on domain classes **blocks merge** from Day 10.

---

## 6. CI Integration

- All edit-mode tests run on every PR via GitHub Actions (no Unity licence required for edit-mode)
- Play-mode tests run nightly on the reference test machine
- Golden-image suite runs on every PR that touches shader files
- Coverage report posted as PR comment (Quality Guild owns the pipeline)

---

## 7. Dependency on Other Sub-teams

| Dependency | Impact on our tests | Mitigation |
|-----------|-------------------|------------|
| `IGazeProvider` from Sub-team 4 | `FoveatedSamplingPolicy` tests need it | `MockGazeProvider` stub used until real interface is finalised |
| `RawVolumeData` from Sub-team 2 | `VolumeTextureManager` tests need it | Synthetic in-memory struct used in tests |
