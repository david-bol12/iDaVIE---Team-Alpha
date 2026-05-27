# CK Metrics — Example 1: Moment-Map Generation

Sub-team 5 | iDaVIE Refactoring Assignment | 27 May 2026

Acceptable thresholds from §7.1 of the assignment brief:  
WMC ≤ 20 (domain), CBO ≤ 14 (domain), RFC ≤ 50, LCOM ≤ 0.5

> **Note:** The Day 2 baseline and Day 13 final columns will be populated
> with actual Understand / SonarQube output during Sprint 2.
> Values marked **est.** are projections derived from reading the source.

---

## Namespace Mapping (ADR-008)

| Class | Before namespace | After namespace | Layer |
|---|---|---|---|
| `MomentMapRenderer` (MonoBehaviour) | `VolumeData` | _decomposed_ | — |
| `IMomentMapService` | `DataFeatures` (non-compliant) | `iDaVIE.Application.Feature` | Application |
| `MomentMapService` | `DataFeatures` (non-compliant) | `iDaVIE.Application.Feature` | Application |
| `IMomentMapAdapter` | `DataFeatures` (non-compliant) | `iDaVIE.Application.Feature` | Application (ACL seam) |
| `MomentMapRequest` | `DataFeatures` (non-compliant) | `iDaVIE.Application.Feature` | Application |
| `MomentMapResult` | `DataFeatures` (non-compliant) | `iDaVIE.Application.Feature` | Application |
| `MomentMapCalculator` | `DataFeatures` (non-compliant) | `iDaVIE.Domain.Feature` | Domain |
| `MomentMapRendererAdapter` | _absent_ | `iDaVIE.Infrastructure.Unity` | Infrastructure |

---

## Before State

| Class | Role | WMC | CBO | RFC | LCOM | Threshold met? |
|---|---|:---:|:---:|:---:|:---:|:---:|
| `MomentMapRenderer` (MonoBehaviour) | Everything | est. 18 | est. 14 | est. 32 | est. 0.55 | ⚠ CBO, LCOM breach |

**Problems driving the high metrics:**

| Problem | Violation |
|---|---|
| `MonoBehaviour` inheritance — only testable in Unity runtime | ADR-006 |
| `Config.Instance` singleton access in `Start()` | ADR-003 |
| `ComputeShader`, `RenderTexture`, `Texture2D`, `RenderTexture.active` | ADR-002 (Unity types in logic) |
| Direct `AstTool.Transform3D` calls inside `GetSpectrumBuffer` | ADR-002, ADR-004 |
| `VolumeDataSetRenderer` navigated via `_parentVolumeDataSetRenderer` | LoD / CBO |
| Namespace `VolumeData` | ADR-008 violation |
| All responsibilities (GPU dispatch + domain math + UI update) in one class | SRP / ADR-006 |

**Day 2 baseline (Understand / SonarQube):** _to be filled_

---

## After State

| Class | Namespace | Role | WMC target | CBO target | RFC target | LCOM target | Threshold met? |
|---|---|---|:---:|:---:|:---:|:---:|:---:|
| `MomentMapCalculator` | `iDaVIE.Domain.Feature` | Pure math (bounds, CPU moments) | 3 | 0 | 3 | 0 | ✓ |
| `IMomentMapAdapter` | `iDaVIE.Application.Feature` | GPU ACL seam | 1 | 2 | 1 | 0 | ✓ |
| `IMomentMapService` | `iDaVIE.Application.Feature` | Use-case interface | 1 | 2 | 1 | 0 | ✓ |
| `MomentMapRequest` | `iDaVIE.Application.Feature` | Input value object | 1 | 0 | 1 | 0 | ✓ |
| `MomentMapResult` | `iDaVIE.Application.Feature` | Output value object | 1 | 0 | 1 | 0 | ✓ |
| `MomentMapService` | `iDaVIE.Application.Feature` | Orchestrator | 2 | 4 | 4 | 0 | ✓ |
| `MomentMapRendererAdapter` | `iDaVIE.Infrastructure.Unity` | Thin adapter (only Unity import) | ≤ 8 | ≤ 6 | ≤ 14 | ≤ 0.30 | ✓ |

**Day 13 final (post full-team refactor):** _to be filled_

---

## Delta Summary — MomentMapRenderer → split classes

| Metric | Before (MomentMapRenderer) | After (largest class: MomentMapRendererAdapter) | Δ |
|---|:---:|:---:|:---:|
| WMC | est. 18 | ≤ 8 | ↓ ↓ |
| CBO | est. 14 | ≤ 6 | ↓ ↓ |
| RFC | est. 32 | ≤ 14 | ↓ ↓ |
| LCOM | est. 0.55 | ≤ 0.30 | ↓ |

The largest gains are in **RFC** and **LCOM**.
By splitting the monolithic MonoBehaviour into seven cohesive classes:
- `MomentMapCalculator` and the value objects have CBO = 0.
- `MomentMapService` is fully testable with a stub adapter — no UnityEngine.
- `MomentMapRendererAdapter` is the only class with UnityEngine, so its CBO
  captures Unity couplings in isolation rather than spreading them through every class.

---

## ADR-006 Responsibility Split

| Concern | Before (MomentMapRenderer) | After (which class) |
|---|---|---|
| GPU compute shader dispatch | `CalculateMomentMaps()` | `MomentMapRendererAdapter.Compute()` |
| Bounds extraction (min/max) | `GetBounds()` via RenderTexture | `MomentMapCalculator.ComputeMinMaxBounds()` |
| Orchestration / sequencing | `CalculateMomentMaps()` | `MomentMapService.GenerateMomentMaps()` |
| Colour-bar and sprite update | `UpdatePlotWindow()` | `MomentMapRendererAdapter.ApplyResultToUI()` |
| Config singleton access | `Start()` — `Config.Instance` | Removed; settings injected at composition root |
| Unity lifecycle subscription | `Start()`, `Update()` | `MomentMapRendererAdapter.Awake()` |
