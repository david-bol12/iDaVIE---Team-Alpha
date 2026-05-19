# ADR-0001: MVVM split for the desktop client shell

- **Status:** proposed
- **Date:** 2026-05-19
- **Authors:** _(Sub-team 6 TL — fill in)_
- **Backlog:** ARCH-1
- **Supersedes:** —

## Context

`Assets/Scripts/UI/CanvassDesktop.cs` is a ~1900-line `MonoBehaviour` that mixes menu structure, panel state, file dialogs, configuration, threshold maths, and direct native-plugin calls. The assignment requires a client–server + micro-kernel target style (§4.1) with an anti-corruption layer around Unity 6 APIs and domain code that does not transitively depend on UnityEngine/SteamVR (§4.2.3). Section 6.6 explicitly prescribes an MVVM split for our sub-team.

## Decision

Adopt a three-layer split inside the desktop client:

1. **View** — Unity 6 UI Toolkit `UIDocument` + USS + UXML. Knows about Unity. No business rules.
2. **ViewModel** — Pure C# class library. Knows nothing about Unity. Exposes observable properties + commands bound by the View.
3. **Service Gateway** — Thin client of the server kernel. Hides transport details.

All three live in separate assemblies so the dependency rule is enforced by the build.

## Consequences

- ViewModel is unit-testable with NUnit + Moq, no Unity required (LO6).
- The Unity 5 → Unity 6 migration is contained to the View layer (LO5).
- Adds ceremony: extra interfaces, extra projects. Mitigation: code skeletons in `refactoring-examples/sub-team-6/`.

## Alternatives considered

- **MVP** — Presenter holds View reference; harder to test, more wiring.
- **MVU / Elm-style** — Elegant but unfamiliar to the team and to UI Toolkit's binding model.
- **Status quo (MonoBehaviour does everything)** — Fails §4.2.3 and §4.2.4.

## References

- §4.1 architectural style.
- §4.2 mandatory architectural constraints.
- §6.6 sub-team work package brief.
