# Worked Example 2: VolumeInputController refactor

**Author:** Colin Forde (Koffiewinkel)  
**Baseline:** ~1635-line god class handling SteamVR input, locomotion, interaction FSM, painting, cursor text, vignette, and menus.

## Problem (before)

- Single class mixed hardware input, locomotion state machine, Stateless interaction FSM, brush logic, and presentation
- High WMC/RFC/CBO on `VolumeInputController` (baseline critical class)
- Two parallel state concepts: `LocomotionState` enum + `InteractionStateMachine`
- Hard to test without SteamVR and Play mode

## Target architecture (after)

`VolumeInputController` is a **SteamVR input router** only:

- Wires `OnGripChanged`, `OnPinchChanged`, menu buttons, push-to-talk
- Delegates to injected collaborators
- `Update()` calls `_locomotionController.Update()` and `_vignetteController.Update()` only

| Collaborator | Responsibility |
|--------------|----------------|
| `ILocomotionController` / `LocomotionController` | Move, scale, threshold/Z edit |
| `IInteractionController` / `InteractionController` | Stateless interaction FSM, selection, region edit |
| `IBrushController` / `BrushController` | Brush size, source ID, undo/redo |
| `IVignetteController` / `VignetteController` | Tunnel vignette (duplicate loop bug fixed) |
| `ICursorInfoFormatter` / `CursorInfoFormatter` | Pure cursor/selection strings |
| `IQuickMenuPositioner` / `QuickMenuPositioner` | Attach quick menu to hand |
| `IGazeProvider` / `IGaze` / `CameraGazeProvider` | Cross-team gaze contract (Team 4 → Team 3); `IGaze` adds world-space members for interaction |

## Patterns

- **Strategy** — locomotion modes behind `ILocomotionController`
- **State** — interaction transitions in `InteractionController` (Stateless)
- **Facade** — `VolumeInputController` keeps public API for menus and voice
- **Dependency injection** — collaborators built in `BuildCollaborators()`

## Out of scope

- No changes to `VolumeDataSetRenderer` or rendering pipeline
- No menu controller rewrites (Quick/Paint menus still call existing public methods)

## Files

- Interfaces: `Assets/Scripts/Interaction/Interfaces/`
- Implementations: `Assets/Scripts/Interaction/`
- Router: `Assets/Scripts/VolumeData/VolumeInputController.cs`

Full narrative: `Worked-Example-VolumeInput-Refactor.txt`
