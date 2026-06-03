# Interaction contract — Desktop GUI ↔ VR Interaction System (Sub-team 4)

**Dependency:** Sub-team 6 (Desktop GUI) → **Sub-team 4 (Interaction System)**.
**Risk:** R04 (VR ↔ Desktop menu vocabulary diverges) · **Backlog:** DEPS-2 · **Related ADR:** ADR-010 (Sub-team 4), ADR-009 §C3 (ours).
**Assembly:** `iDaVIE.Client.Interaction` (`InteractionContract.csproj`) — net8.0, zero UnityEngine, no dependency on the service-gateway assembly.

**Status:** 🟡 **STRAWMAN** — proposed by Sub-team 6 on 2026-05-29, **awaiting Sub-team 4 ratification.** The desktop side can code against this interface now; the open points (O1–O5, below) are Sub-team 4's to settle and don't block us.

---

## Why this contract exists

Today the VR GUI and Desktop GUI share the *same* `MonoBehaviour` composition root and mutate the *same* `CanvassDesktop` fields directly (`docs/sub-team-6/.../CurrentGUIStateDoc.md` §9, §11). So one user intent has two incompatible shapes — e.g. "set colour map to Viridis" is a reified VR `ICommand` on one side and `ColormapDropdown.SelectedIndex = 5` on the other.

Under the target client–server split that shared field disappears. Both surfaces must instead drive **one canonical command per intent** to the server, and the server must **notify** the other surface when shared state changes. This contract is that vocabulary, expressed as a C# interface the desktop ViewModels depend on.

## Files

| File | What it is |
|---|---|
| `IInteractionGateway.cs` | The contract: 8 command methods (Client→Server) + 5 notification events (Server→Client), plus the DTOs/enums they use. Each member's doc comment names its JSON-RPC wire verb. |
| `FakeInteractionGateway.cs` | Test double. Records every command in `Sent`; exposes `Raise*` helpers to simulate VR-originated notifications. Satisfies the Section 4.2 #4 non-negotiable (every boundary has a test double). |
| `InteractionContract.csproj` | Builds the two files as the standalone `iDaVIE.Client.Interaction` library. |

## The interface at a glance

**Commands (issued by either surface, routed Desktop → server → VR):**

| Method | Wire verb |
|---|---|
| `SetThresholdAsync` | `render.setThreshold` |
| `SetColorMapAsync` | `render.setColorMap` |
| `SetScalingFunctionAsync` | `render.setScalingFunction` |
| `FlagSourceAsync` | `sources.flag` |
| `AddToNewListAsync` | `sources.addToNewList` |
| `CropCubeAsync` | `cube.crop` |
| `SetMaskApplyModeAsync` | `mask.setApplyMode` |
| `ExitAsync` | `app.exit` |

**Notifications (raised on VR-originated changes, server → Desktop):**

| Event | Wire verb | Fixes |
|---|---|---|
| `ThresholdChanged` | `render.thresholdChanged` | **B-03** — desktop sliders go stale after VR threshold changes |
| `ColorMapChanged` | `render.colorMapChanged` | colour-map drift between surfaces |
| `ScalingChanged` | `render.scalingChanged` | — |
| `SourceListChanged` | `sources.listChanged` | SOURCES tab stale until tab-switch |
| `MaskApplyModeChanged` | `mask.applyModeChanged` | — |

## How it relates to the service-gateway contract (`../contracts`)

This is a *domain* interface. Its real implementation is an adapter that translates these calls into JSON-RPC requests/notifications on `IServiceGateway` (the Sub-team 1 transport in `../contracts`). The interface deliberately has **no compile-time dependency** on the gateway assembly, so ViewModels and tests bind to it directly and the transport can change underneath.

## Open points — for Sub-team 4 to ratify

- **O1** Verb names / arg field names — Sub-team 4 may rename any of them.
- **O2** Does ADR-010's `ICommand` wrap a separate wire DTO, or is the reified command itself the payload? (Doesn't affect us — we depend on this interface, not their `ICommand`.)
- **O3** Will Sub-team 4 emit the five notifications on VR-originated changes? If not, **B-03 stays unfixable**.
- **O4** Ownership — mask painting + mask save are assumed **VR-only** (no member here). Confirm.
- **O5** Units — colour map as name string (assumed) vs index; threshold as raw float (assumed) vs normalised 0–1. (Defect B-05 shows units already drift.)

## Test double usage

```csharp
var interaction = new FakeInteractionGateway();
var vm = new RenderTabViewModel(interaction);

// assert a desktop control issued the right command
await vm.ApplyThresholdCommand.ExecuteAsync();
Assert.That(interaction.Sent[0].Verb, Is.EqualTo("render.setThreshold"));

// simulate VR changing the threshold and assert the VM refreshed (B-03)
interaction.RaiseThresholdChanged("ds-1", 0.1f, 0.9f);
Assert.That(vm.Min, Is.EqualTo(0.1f));
```
