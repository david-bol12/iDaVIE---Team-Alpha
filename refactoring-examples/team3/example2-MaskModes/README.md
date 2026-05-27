# Refactoring Example 2 — Mask Mode Strategy Pattern
**Team Alpha | Sub-team 3 — Rendering Engine ("Cache Me If You Can")**  
*Brief reference: §9.2 Sub-team deliverable 3 — worked refactoring example; §6.3 OCP for mask modes*

---

## What This Example Shows

The mask-mode switch statement in `VolumeDataSetRenderer` is a textbook Open-Closed Principle violation: adding a new mask mode requires editing existing production code. This example applies the Strategy pattern to replace the switch with a polymorphic `IMaskMode` interface. Each mode becomes a sealed, stateless class of ~15 lines. Adding a future mode (e.g. `IsoSurfaceMaskMode`) creates one new file and changes nothing else.

---

## Before: Switch Statement Approach

**File:** `before/VolumeDataSetRendererMaskMode.cs` (annotated)  
**Source:** `VolumeDataSetRendererMaskMode.cs` + `VolumeDataSetRenderer.Update()` lines 1072–1094

### The OCP Violation

Every time a new mask mode is needed, a developer must:
1. Add a new value to the `MaskMode` enum in `VolumeDataSetRenderer.cs`
2. Add a new `case` or `else if` branch to the switch in `VolumeDataSetRendererMaskMode.cs`
3. Risk breaking existing modes (no isolation between branches)
4. Retest the entire `VolumeDataSetRenderer` class — it has no mask-only test entry point

This is **V-04** in the SOLID/GRASP violation audit (`docs/design-document.md §7.1`).

### Before Code

```csharp
// VolumeDataSetRenderer.Update() — lines 1072–1094 (simplified)
// Each branch writes shader keywords directly; adding a mode means editing here.

switch (_maskMode) {
    case MaskMode.Enabled:
        _material.EnableKeyword("_MASK_APPLY");
        _material.DisableKeyword("_MASK_INVERSE");
        _material.DisableKeyword("_MASK_ISOLATE");
        _material.SetFloat("_MaskAlpha", 1.0f);
        _material.SetTexture("_MaskTex", _maskTexture);
        break;

    case MaskMode.Inverted:
        _material.DisableKeyword("_MASK_APPLY");
        _material.EnableKeyword("_MASK_INVERSE");
        _material.DisableKeyword("_MASK_ISOLATE");
        _material.SetFloat("_MaskAlpha", 1.0f);
        _material.SetTexture("_MaskTex", _maskTexture);
        break;

    case MaskMode.Isolated:
        _material.DisableKeyword("_MASK_APPLY");
        _material.DisableKeyword("_MASK_INVERSE");
        _material.EnableKeyword("_MASK_ISOLATE");
        _material.SetFloat("_MaskAlpha", 0.15f);  // partial opacity outside mask
        _material.SetTexture("_MaskTex", _maskTexture);
        break;
}
// Adding MaskMode.IsoSurface here requires editing this file + the enum.
```

### Before CK Impact (mask-related code within VolumeDataSetRenderer)

The switch block is not a standalone class — it lives inside `VolumeDataSetRenderer`, which as a whole measured:

| Metric | VolumeDataSetRenderer (Day 2) | Brief Target | Status |
|--------|------------------------------|-------------|--------|
| WMC (Σ cyclomatic) | ~192 | ≤ 20 (domain) | ❌ |
| CBO | 45 | ≤ 14 (domain) | ❌ |
| LCOM | ~0.81 | ≤ 0.5 | ❌ |

The mask-mode switch contributed:
- **WMC:** 3 branches × ~CC2 = ~6 extra complexity units in Update()
- **LCOM:** Mask methods access `_maskTexture` and `_material` — disjoint from camera and foveation fields, raising LCOM
- **OCP:** Every new mode requires editing a 1,403-line file

---

## After: Strategy Pattern with IMaskMode

**Files:** `after/` directory

### The Solution

Each mask mode is extracted into its own sealed class. `VolumeMaterialBinder` holds a reference to the active `IMaskMode` and calls `Apply()` once per frame — no branching, no switch statement, no knowledge of which concrete mode is active.

Adding a new mode (e.g. `IsoSurfaceMaskMode` for FUT-01) creates one new file. Nothing existing changes.

### After Interface

```csharp
// after/IMaskMode.cs
public interface IMaskMode
{
    void Apply(Material material, Texture3D maskTexture);
    string ShaderKeyword { get; }
}
```

Two members only — ISP: fat interfaces raise CBO on every implementor. Each concrete class stays at WMC=2, CBO=1, LCOM=0.0.

### After Concrete Implementations

```csharp
// after/ApplyMaskMode.cs — maps to MaskMode.Enabled = 1
public sealed class ApplyMaskMode : IMaskMode
{
    public string ShaderKeyword => "_MASK_APPLY";

    public void Apply(Material material, Texture3D maskTexture)
    {
        material.DisableKeyword("_MASK_DISABLED");
        material.DisableKeyword("_MASK_INVERSE");
        material.DisableKeyword("_MASK_ISOLATE");
        material.EnableKeyword(ShaderKeyword);   // "_MASK_APPLY"
        material.SetFloat("_MaskAlpha", 1.0f);
        // Texture already bound by VolumeMaterialBinder.BindMaskTexture()
    }
}
```

```csharp
// InverseMaskMode and IsolateMaskMode are identical in structure —
// each enables its own keyword, disables the others.
// See after/InverseMaskMode.cs and after/IsolateMaskMode.cs.
```

### How the Consumer Uses It

```csharp
// VolumeMaterialBinder — zero knowledge of which mode is active
private IMaskMode _activeMaskMode = new DisabledMaskMode(); // default (no mask loaded)

public void SetActiveMaskMode(IMaskMode mode) => _activeMaskMode = mode;

public void Tick(in VolumeRenderState s)
{
    // ...
    if (s.HasMask)
        _activeMaskMode.Apply(_material, null); // no switch; no branching here
    // ...
}
```

### Adding a Future Mode (FUT-01 — IsoSurface mask)

```csharp
// New file only — zero edits to any existing file:
public sealed class IsoSurfaceMaskMode : IMaskMode
{
    private readonly float _isoValue;
    public IsoSurfaceMaskMode(float isoValue) => _isoValue = isoValue;

    public string ShaderKeyword => "_MASK_ISOSURFACE";

    public void Apply(Material material, Texture3D maskTexture)
    {
        material.DisableKeyword("_MASK_APPLY");
        material.DisableKeyword("_MASK_INVERSE");
        material.DisableKeyword("_MASK_ISOLATE");
        material.EnableKeyword(ShaderKeyword);
        material.SetFloat("_IsoValue", _isoValue);
    }
}
// OCP: open for extension, closed for modification. ✅
```

---

## After CK Metrics (Day 13 Projections)

| Class | WMC | DIT | NOC | CBO | RFC | LCOM | Meets target? |
|-------|-----|-----|-----|-----|-----|------|---------------|
| `IMaskMode` (interface) | 0 | 0 | 4 | 0 | 0 | 0 | ✅ |
| `DisabledMaskMode` | 2 | 1 | 0 | 1 | 3 | 0.00 | ✅ all |
| `ApplyMaskMode` | 2 | 1 | 0 | 1 | 3 | 0.00 | ✅ all |
| `InverseMaskMode` | 2 | 1 | 0 | 1 | 3 | 0.00 | ✅ all |
| `IsolateMaskMode` | 2 | 1 | 0 | 1 | 3 | 0.00 | ✅ all |
| `NullMaskMode` (test double) | 2 | 1 | 0 | 0 | 1 | 0.00 | ✅ all |

> CBO = 1 for concrete classes = UnityEngine (Material, Texture3D are parameter types, not fields; counted as one dependency edge).

### CK Delta

| Metric | Before (mask code in VolumeDataSetRenderer) | After (per strategy class) | Delta |
|--------|---------------------------------------------|---------------------------|-------|
| WMC contribution | ~6 (3 switch branches in Update()) | 2 per class | ✅ from 6 shared to 2 isolated |
| CBO | 45 (whole VDSR) | 1 per class | ✅ -44 per class |
| LCOM | ~0.81 (mask methods unrelated to camera) | 0.00 (no instance fields) | ✅ fully cohesive |
| Files changed to add a mode | 2+ (enum + switch + test) | 1 (new class only) | ✅ OCP satisfied |

---

## SOLID / GRASP Analysis

| Principle | Before | After |
|-----------|--------|-------|
| **OCP** | ❌ Adding a mode requires editing `VolumeDataSetRenderer` (V-04 in audit) | ✅ New mode = new class; no existing file changes |
| **SRP** | ❌ `VolumeDataSetRenderer` owns mask logic + 8 other responsibilities | ✅ Each mode class owns exactly one mode's render behaviour |
| **LSP** | N/A | ✅ All four modes are fully substitutable through `IMaskMode` |
| **ISP** | ❌ 152-member public surface includes mask methods as internal detail | ✅ 2-member interface — every consumer has minimum coupling |
| **DIP** | ❌ `VolumeMaterialBinder` depends on concrete mode strings | ✅ `VolumeMaterialBinder` depends only on `IMaskMode` |
| **GRASP Protected Variations** | ❌ Mask-mode variation is unprotected — every new mode touches VDSR | ✅ `IMaskMode` is the variation point; changes are contained to new classes |
| **GRASP Indirection** | ❌ No indirection — direct keyword-string coupling in Update() | ✅ `IMaskMode` decouples `VolumeMaterialBinder` from HLSL keyword strings |
| **GRASP Low Coupling** | ❌ VDSR coupled to material, texture, enum, keyword strings — all in one method | ✅ Each class coupled to `Material` only (CBO = 1) |

---

## Test Impact

**Before:** testing `InverseMaskMode` required instantiating the full `VolumeDataSetRenderer` (1,403 lines, GPU, material assets, URP pipeline, all other dependencies).

**After:** each mode class is testable in one line:

```csharp
[Test]
public void InverseMaskMode_HasCorrectShaderKeyword()
{
    var mode = new InverseMaskMode();
    Assert.AreEqual("_MASK_INVERSE", mode.ShaderKeyword);
}

[Test]
public void DisabledMaskMode_DoesNotThrowOnNullTexture()
{
    // Disabled mode is valid when no mask is loaded (maskTexture == null).
    var mode = new DisabledMaskMode();
    var mat  = new Material(Shader.Find("Hidden/InternalErrorShader"));
    Assert.DoesNotThrow(() => mode.Apply(mat, null));
    Object.Destroy(mat);
}
```

The `NullMaskMode` test double enables unit tests of `VolumeMaterialBinder` with no Material asset or GPU at all:

```csharp
var binder = new VolumeMaterialBinder(new NullRenderPipeline(), new NullMaskMode());
// Tick() runs without any shader or GPU context.
```

---

## File Index

| File | Description |
|------|-------------|
| `before/VolumeDataSetRendererMaskMode.cs` | Annotated original switch-statement source |
| `after/IMaskMode.cs` | Strategy interface + `DisabledMaskMode` + `NullMaskMode` test double |
| `after/ApplyMaskMode.cs` | `MaskMode.Enabled = 1` → Strategy class |
| `after/InverseMaskMode.cs` | `MaskMode.Inverted = 2` → Strategy class |
| `after/IsolateMaskMode.cs` | `MaskMode.Isolated = 3` → Strategy class |
| `../stubs/IMaskMode.cs` | Earliest stub version (Sprint 1) — superseded by after/ |
