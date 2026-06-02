# Sub-team 5 — AI Tool Usage Log

§10.5 + §9.1 T8. One row per substantive AI assist. Required at team level on Day 14.

| Date | Author | Tool / Model | Prompt class | Where it helped | Where it failed |
|---|---|---|---|---|---|
| 2026-05-25 | Harry Kennedy | Claude Code / Sonnet 4.6 | refactoring-proposal | Produced the initial three-way split of `FeatureSetManager` into `FeatureCatalog`, `FeatureSetService`, and `FeatureVisualiser` with correct layer assignments (`iDaVIE.Domain`, `iDaVIE.Application`, `iDaVIE.Infrastructure.Unity`); surfaced the dirty-event coupling issue between `Feature` and `FeatureSetRenderer` | Suggested a `FeatureFactory` helper class that wasn't needed — dismissed; constructor parameter names in the generated `Feature` skeleton didn't match the existing codebase (`cubeMin`/`cubeMax` vs `cornerMin`/`cornerMax`) and had to be fixed manually |
| 2026-05-25 | Harry Kennedy | Claude Code / Sonnet 4.6 | diagram-generation | Generated the before/after PlantUML class diagram for `FeatureData` (`FeatureData(Before).puml`, `FeatureData(Refactored).puml`) with correct associations, multiplicities, and namespace notes | |
| 2026-06-02 | Harry Kennedy | Claude Code / Opus 4.8 | metric-interpretation | Drafted `SubTeam5_CK_Metrics.md`: mapped SciTools Understand CSV columns onto the CK suite (WMC/DIT/NOC/CBO/LCOM) and explained the before/after deltas for the Moment Maps and VOTable Export examples | Understand's export has no native RFC column, so RFC had to be estimated/flagged manually rather than read off; mapping caveats (e.g. `SumCyclomatic` vs `CountDeclMethod` for WMC) needed human confirmation against the actual project |
| 2026-06-02 | Harry Kennedy | Claude Code / Opus 4.8 | prose-editing | Drafted `SubTeam5_Testing_Strategy.md` documenting the three test levels (example-based unit, FsCheck property-based, scenario) for `SubTeam5_Tests`, the tooling table, and coverage gaps | Tooling versions and the 31-tests/passing figure had to be verified against `SubTeam5_Tests.csproj` and an actual test run, not taken on trust |
| 2026-06-02 | Harry Kennedy | Claude Code / Opus 4.8 | prose-editing | Drafted the `SubTeam5_Pitch_Practice_QA.md` rehearsal Q&A bank (God-class rationale, Clean-Architecture split, CK evidence) as private practice prep grounded in the refactored code | Practice aid only — §10.5.6 forbids AI in the live pitch/interview defence; answers must be internalised and delivered by the human author |

**Prompt classes (use these):** requirements-drafting · ADR-drafting · code-skeleton · test-generation · refactoring-proposal · diagram-generation · metric-interpretation · prose-editing · backlog-drafting · review · tool-setup.

**Policy reminders:**
- AI output must be reviewed, understood, and defensible by a named human author.
- AI MAY NOT be used for: peer-rating, contribution log, individual reflection, live pitch/interview defence (§10.5.6).
- Verbatim AI prose must not be passed off as human-authored in the final report (§10.5.3).
