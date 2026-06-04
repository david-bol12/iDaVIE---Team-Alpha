# Worked Example 1: Voice command refactor

**Author:** Arnav Kothari (Koffiewinkel)

Implementation lives in the Unity project:

- `Assets/Scripts/VolumeData/Voice/` — recogniser adapter, command registry, desktop UI sync
- `Assets/Scripts/VolumeData/VolumeCommandController.cs` — thin orchestrator (keywords, dataset lifecycle, public API unchanged)

## Before

- `ExecuteVoiceCommand`: long `if/else` chain (~190 lines)
- `KeywordRecognizer` owned directly by `VolumeCommandController`
- Desktop threshold/ratio updates via repeated `transform.Find` chains

## After

| Type | Role |
|------|------|
| `IVoiceRecogniser` / `WindowsVoiceRecogniser` | Platform speech behind an interface |
| `IVoiceCommand` / `DelegateVoiceCommand` | Single command execution unit |
| `VoiceCommandRegistry` | Phrase → command map (replaces if/else) |
| `IVoiceCommandContext` / `VoiceCommandContext` | Shared dependencies for commands |
| `VoiceDesktopUiSync` | Isolated desktop slider/dropdown sync |

Public API preserved for existing callers (`CanvassDesktop`, voice list UI, colour-map UI, custom editor).  
**Out of scope:** rendering pipeline / `VolumeDataSetRenderer` internals.

See also: [../volume-input/](../volume-input/) (worked example 2 — `VolumeInputController`).
