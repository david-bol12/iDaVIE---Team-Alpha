# Sub-team 5 — AI Tool Usage Log

§10.5 + §9.1 T8. One row per substantive AI assist. Required at team level on Day 14.

| Date | Author | Tool / Model | Prompt class | Where it helped | Where it failed |
|---|---|---|---|---|---|
| 2026-05-25 | Harry Kennedy | Claude Code / Sonnet 4.6 | refactoring-proposal | Produced the initial three-way split of `FeatureSetManager` into `FeatureCatalog`, `FeatureSetService`, and `FeatureVisualiser` with correct layer assignments (`iDaVIE.Domain`, `iDaVIE.Application`, `iDaVIE.Infrastructure.Unity`); surfaced the dirty-event coupling issue between `Feature` and `FeatureSetRenderer` | Suggested a `FeatureFactory` helper class that wasn't needed — dismissed; constructor parameter names in the generated `Feature` skeleton didn't match the existing codebase (`cubeMin`/`cubeMax` vs `cornerMin`/`cornerMax`) and had to be fixed manually |
| 2026-05-25 | Harry Kennedy | Claude Code / Sonnet 4.6 | diagram-generation | Generated the before/after PlantUML class diagram for `FeatureData` (`FeatureData(Before).puml`, `FeatureData(Refactored).puml`) with correct associations, multiplicities, and namespace notes | |

**Prompt classes (use these):** requirements-drafting · ADR-drafting · code-skeleton · test-generation · refactoring-proposal · diagram-generation · metric-interpretation · prose-editing · backlog-drafting · review · tool-setup.

**Policy reminders:**
- AI output must be reviewed, understood, and defensible by a named human author.
- AI MAY NOT be used for: peer-rating, contribution log, individual reflection, live pitch/interview defence (§10.5.6).
- Verbatim AI prose must not be passed off as human-authored in the final report (§10.5.3).
