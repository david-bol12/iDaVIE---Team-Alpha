# Koffiewinkel — Refactoring Worked Examples

Team: **Koffiewinkel** (Team Alpha)  
Work package: **Interaction System** (VR input, voice commands, menus, VR keyboard)

## Scope (in)

- `VolumeInputController` and extracted interaction collaborators
- `VolumeCommandController` and extracted voice collaborators
- `Menu/`, `UI/`, `VRKeyboard/`, `VoiceCommands/` (as consumers of the above)

## Scope (out)

- **Rendering sub-team code** — e.g. `VolumeDataSetRenderer`, `VolumeDataSet`, cube generation, shaders
- Native FITS / catalog pipelines outside interaction

Worked examples document **interaction-layer** maintainability only. They do not propose rendering refactors.

## Worked examples in this repo

| # | Topic | Owner (week) | Code location | Documentation |
|---|--------|--------------|---------------|----------------|
| 1 | Voice command dispatch | Arnav Kothari | `Assets/Scripts/VolumeData/Voice/` | [voice/](voice/) |
| 2 | VR input / locomotion / interaction split | Colin Forde | `Assets/Scripts/Interaction/` | [volume-input/](volume-input/) |

## Implementation paths

```
refactoring-examples/koffiewinkel/     ← design evidence (this folder)
Assets/Scripts/VolumeData/Voice/       ← worked example 1
Assets/Scripts/Interaction/            ← worked example 2
Assets/Scripts/VolumeData/VolumeInputController.cs   ← thin SteamVR router
Assets/Scripts/VolumeData/VolumeCommandController.cs ← thin voice orchestrator
```

## Team evidence (CATME, May 2026)

Peer evaluations record delivery of these two worked examples and coordinated planning (Product Owner, Scrum Master, Kanban). See course submissions; technical detail lives in the per-example README and `.txt` files above.
