# Sub-team 7 — Persistence (Team 7 Alpha, "Sewe en sestig") — AI tool usage log

See [`README.md`](README.md) for schema. Newest entries on top. Author: Sean Corrigan.

| Date | Author | Tool / Model | Prompt class | Where it helped | Where it failed | What the human did instead |
|---|---|---|---|---|---|---|
| 2026-06-02 | Sean Corrigan | Claude Code (Sonnet 4.6) | Data formatting | Formatted the raw SciTools Understand CK-metrics dump into a Google-Docs-pasteable table for the persistence classes | — | Verified figures against the Understand export |
| 2026-06-01 | Sean Corrigan | Claude Code (Sonnet 4.6) | Artefact generation | Generated two sprint Kanban boards (one per working week) from the Day 1–10 daily stand-up notes | — | — |
| 2026-05-29 | Sean Corrigan | Claude Code (Sonnet 4.6) | Onboarding / concept | Explained C# record types in the context of the immutable persistence DTOs | — | — |
| 2026-05-28 | Sean Corrigan | Claude Code (Sonnet 4.6) | Code comprehension | Guided a fixed 9-step walkthrough of the persistence layer (DTOs → serialization → workspace metadata → remaining steps), building design-rationale understanding step by step (collapses the "continue to step N" navigation prompts) | — | Drove the walkthrough order and confirmed each step against the source |
| 2026-05-28 | Sean Corrigan | Claude Code (Sonnet 4.6) | Code comprehension / design rationale | Explained why strings are used instead of enums in the persistence DTOs | — | — |
| 2026-05-27 | Sean Corrigan | Claude Code (Sonnet 4.6) | Investigation / analysis | Used subagents to read `SPEC.md`, `SUBTEAM.md` and every contract in `state-contracts/` (data-io, feature, gui, …) and consolidate them into a single findings file | — | — |
| 2026-05-27 | Sean Corrigan | Claude Code (Sonnet 4.6) | Investigation / analysis | Identified what to look for when reviewing each team's state contracts, framed from the Team 7 Alpha persistence perspective | Claude Code launch failed — PowerShell "term not recognized" (PATH) | Fixed the PATH so Claude Code would launch |
| 2026-05-21 | Sean Corrigan | Claude Code (Sonnet 4.6) | Artefact generation | Built a Kanban board (all sub-teams, then scoped to Sewe en sestig); Day 3 step-by-step and Day 4 catch-up plans; requirements document (7 FRs + 7 NFRs traced to ISO/IEC 25010); explained aggregate invariants in plain terms; identified the relevant classes per state group; produced a scope/ownership diagram; drafted a ready-to-paste CK-baseline prompt | `npm install` failed — "npm not recognised" error blocked Claude Code setup | Resolved the npm PATH / install manually so Claude Code was usable |
| 2026-05-20 | Sean Corrigan | Claude Code (Sonnet 4.6) | Onboarding / code comprehension | Fork & branch strategy for the Team Alpha fork; how to read the codebase through a persistence lens; explained the astronomy domain (FITS, WCS, HDU selection, feature catalogues); analysed brief deadlines/constraints; introduced CK metrics as the measurement framework; checked whether a defaults/preferences framework already existed; walked through Worked Example 1 (extracting `RenderViewState` from `VolumeDataSetRenderer`) | — | — |
| 2026-05-18 | Sean Corrigan | Claude Code (Sonnet 4.6) | Onboarding / concept | Explained C#-unique keywords against Java/C analogues to build the language fundamentals needed before reading the persistence codebase | — | Checked explanations against actual repo code |

**Policy reminders:**
- AI output must be reviewed, understood, and defensible by a named human author.
- AI MAY NOT be used for: peer-rating, contribution log, individual reflection, live pitch/interview defence (§10.5.6).
- Verbatim AI prose must not be passed off as human-authored in the final report (§10.5.3).
