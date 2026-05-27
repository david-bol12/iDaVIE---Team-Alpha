# Refactoring Example 2: Mask Mode Strategy Pattern
**Team Alpha | Cache Me If You Can — Sub-team 3**  
*Brief reference: Section 6.3 Software Construction — Open-Closed for mask modes*

---

## What This Example Shows

The Open-Closed Principle violation: adding a new mask mode requires editing existing code.
We apply the Strategy pattern behind `IMaskMode` to fix this.

---

## Before: Switch Statement Approach

### The Problem

Every time a new mask mode is needed (e.g. an iso-surface mask for a future feature),
a developer must:
1. Open `VolumeDataSetRenderer`
2. Add a new `case` to the switch statement
3. Risk breaking the other two existing modes
4. The class has no single reason to change — it must change for every new mask mode

### Before Code

```csharp
public class VolumeDataSetRenderer : MonoBehaviour {

    public void SetMaskMode(string mode) {
        // Violates OCP — new mode = edit this class
        switch (mode) {
            case "apply":
                _material.EnableKeyword("_MASK_APPLY");
                _material.DisableKeyword("_MASK_INVERSE");
                _material.DisableKeyword("_MASK_ISOLATE");
                _material.SetFloat("_MaskAlpha", 1.0f);
                _material.SetTexture("_MaskTex", _maskTexture);
                break;
            
            case "inverse":
                _material.DisableKeyword("_MASK_APPLY");
                _material.EnableKeyword("_MASK_INVERSE");
                _material.DisableKeyword("_MASK_ISOLATE");
                _material.SetFloat("_MaskAlpha", 1.0f);
                _material.SetTexture("_MaskTex", _maskTexture);
                break;
            
            case "isolate":
                _material.DisableKeyword("_MASK_APPLY");
                _material.DisableKeyword("_MASK_INVERSE");
                _material.EnableKeyword("_MASK_ISOLATE");
                _material.SetFloat("_MaskAlpha", 0.15f);
                _material.SetTexture("_MaskTex", _maskTexture);
                break;
            
            default:
                Debug.LogWarning($"Unknown mask mode: {mode}");
                break;
        }
    }
}
```

### Before CK Metrics (for mask-related methods in VolumeDataSetRenderer)

*(Fill in actual measured — this switch block contributes directly to WMC and LCOM)*

| Metric | Value | Note |
|--------|-------|------|
| WMC contribution from mask methods | TBC | Each case counted separately |
| LCOM | TBC | Mask methods share no fields with camera methods |

---

## After: Strategy Pattern with IMaskMode

### The Solution

Each mask mode becomes its own class. `VolumeMaterialBinder` holds a reference to
the active `IMaskMode` and calls `Apply()` — it doesn't know which mode it is.

Adding a new mode (e.g. `IsoSurfaceMaskMode`) = create a new class, nothing existing changes.

### After Interface and Implementations

```csharp
// The strategy interface
public interface IMaskMode {
    void Apply(Material material, Texture3D maskTexture);
    string ShaderKeyword { get; }
}

// Strategy 1
public sealed class ApplyMaskMode : IMaskMode {
    public string ShaderKeyword => "_MASK_APPLY";
    
    public void Apply(Material material, Texture3D maskTexture) {
        material.EnableKeyword(ShaderKeyword);
        material.DisableKeyword("_MASK_INVERSE");
        material.DisableKeyword("_MASK_ISOLATE");
        material.SetFloat("_MaskAlpha", 1.0f);
        material.SetTexture("_MaskTex", maskTexture);
    }
}

// Strategy 2
public sealed class InverseMaskMode : IMaskMode {
    public string ShaderKeyword => "_MASK_INVERSE";
    
    public void Apply(Material material, Texture3D maskTexture) {
        material.DisableKeyword("_MASK_APPLY");
        material.EnableKeyword(ShaderKeyword);
        material.DisableKeyword("_MASK_ISOLATE");
        material.SetFloat("_MaskAlpha", 1.0f);
        material.SetTexture("_MaskTex", maskTexture);
    }
}

// Strategy 3
public sealed class IsolateMaskMode : IMaskMode {
    public string ShaderKeyword => "_MASK_ISOLATE";
    
    public void Apply(Material material, Texture3D maskTexture) {
        material.DisableKeyword("_MASK_APPLY");
        material.DisableKeyword("_MASK_INVERSE");
        material.EnableKeyword(ShaderKeyword);
        material.SetFloat("_MaskAlpha", 0.15f);  // partial opacity outside mask
        material.SetTexture("_MaskTex", maskTexture);
    }
}

// Future FUT-01 — adding iso-surface mask requires ZERO changes above
public sealed class IsoSurfaceMaskMode : IMaskMode {
    public string ShaderKeyword => "_MASK_ISOSURFACE";
    private readonly float _isoValue;
    
    public IsoSurfaceMaskMode(float isoValue) => _isoValue = isoValue;
    
    public void Apply(Material material, Texture3D maskTexture) {
        material.EnableKeyword(ShaderKeyword);
        material.SetFloat("_IsoValue", _isoValue);
        // ... etc
    }
}
```

### How the Consumer Uses It

```csharp
public class VolumeMaterialBinder {
    private IMaskMode _activeMaskMode = new ApplyMaskMode();  // default
    
    // Switch mode at runtime — zero knowledge of which mode it is
    public void SetMaskMode(IMaskMode mode) => _activeMaskMode = mode;
    
    public void BindFrame(/* params */) {
        _activeMaskMode.Apply(_material, _maskTexture);
        // ...
    }
}
```

### After CK Metrics (Projected)

| Class | WMC | DIT | NOC | CBO | RFC | LCOM | Meets target? |
|-------|-----|-----|-----|-----|-----|------|---------------|
| `IMaskMode` (interface) | 0 | 0 | 3 | 0 | 0 | 0 | ✅ |
| `ApplyMaskMode` | TBC | 0 | 0 | TBC | TBC | TBC | — |
| `InverseMaskMode` | TBC | 0 | 0 | TBC | TBC | TBC | — |
| `IsolateMaskMode` | TBC | 0 | 0 | TBC | TBC | TBC | — |

---

## CK Delta

| Metric | Before (mask code in VolumeDataSetRenderer) | After (per strategy class) | Delta |
|--------|---------------------------------------------|---------------------------|-------|
| WMC | TBC (all mask methods counted together) | TBC (per class, much lower) | TBC |
| CBO | TBC (class coupled to everything) | TBC (each mode coupled to Material only) | TBC |
| LCOM | TBC (high — mask methods unrelated to camera) | TBC (each class fully cohesive) | TBC |

---

## SOLID/GRASP Analysis

| Principle | Before | After |
|-----------|--------|-------|
| OCP | ❌ Must edit VolumeDataSetRenderer for new mode | ✅ New mode = new class |
| SRP | ❌ VolumeDataSetRenderer owns mask logic + everything else | ✅ Each mode class owns exactly one mode |
| LSP | N/A | ✅ All three modes substitutable via IMaskMode |
| DIP | ❌ Consumer knows about specific mode strings | ✅ Consumer depends on IMaskMode interface |
| GRASP Protected Variations | ❌ Variation is unprotected — changes propagate | ✅ IMaskMode is the variation point |

---

## Test Impact

Before: testing mask mode "inverse" required instantiating the entire `VolumeDataSetRenderer`
and all its dependencies (camera, texture, URP pipeline).

After: testing `InverseMaskMode` is one line:
```csharp
[Test]
public void InverseMaskMode_SetsCorrectKeyword() {
    var mode = new InverseMaskMode();
    Assert.AreEqual("_MASK_INVERSE", mode.ShaderKeyword);
}
```
