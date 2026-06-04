# Team Alpha — AI Tool Usage Reflection (T8, Part 2)

## How we used AI

AI was used across the lifecycle, not as a novelty: requirements drafting, ADR drafting, code skeletons, test generation, diagram generation, metric interpretation, and prose editing — all within the §10.5.5 permitted-use list. Primary tools were Claude Code and Claude chat, with Cursor (Composer) for in-editor refactoring and Perplexity/Gemini for diagrams and prose. Every sub-team kept a per-assist log naming a human reviewer.

## Where it helped

The biggest wins were analytical and structural: SOLID/CK audits of the god classes (`VolumeDataSetRenderer`, `CanvassDesktop`, `VolumeInputController`), skeleton scaffolding for the refactored layers, and fast PlantUML/Mermaid diagram generation. AI turned ~800-line source files into navigable responsibility maps in minutes and produced usable before/after worked examples grounded in our git history.

## Where it failed — and how we caught it

This is where the discipline mattered, and our logs record it honestly:

- **Hallucinated artefacts.** AI invented interfaces and classes that never existed — `IFitsPlugin`/`IAstPlugin`/`PluginRegistry` (Native Plug-ins) and a fabricated `TryTransform(Vector3)` helper. Whole documents were discarded once checked against the actual code.
- **Unverifiable numbers.** AI could not independently compute CK metrics — it worked from values we fed it, miscounted DIT (0 vs NDepend's 1), and confused LCOM4 with the brief's normalised LCOM, putting several metric files on the wrong scale.
- **Cross-team confusion.** AI repeatedly mislabelled sub-team numbers, including the Persistence/Native-Plug-ins (2↔7) mix-up we had to correct by hand.
- **Tooling dead-ends.** Confident but wrong setup advice cost real time — NDepend on a Unity project with no `Assembly-CSharp.dll`, and npm/PATH failures that blocked Claude Code until fixed manually.

In every case a named human cross-checked AI output against source code, tool exports (SciTools Understand, NDepend), or the brief before it shipped.

## Responsible use

We treated AI metric and architecture output as a hypothesis to verify, never as a source of truth. Per §10.5.1/§10.5.3, all AI-assisted artefacts were reviewed and are defensible by their human authors, and no verbatim AI prose is passed off as human-authored in the final report. Per §10.5.6, AI was **not** used for peer-rating, contribution logs, individual reflections, or live pitch/interview defence.

## Lessons learned

1. AI is strongest at structure and explanation, weakest at facts it cannot measure — verify every number against the tool that produced it.
2. Ground generation in real source and git history; un-grounded prompts hallucinate plausible APIs.
3. Log the misses as carefully as the hits — the failures are the evidence that the human, not the tool, is the author of record.

---

**Sign-off (one line per sub-team — each owns its own contribution):**

| Sub-team | Representative | Signed |
|---|---|---|
| 1 — Architecture / Micro-kernel | David | |
| 2 — Native Plug-ins | Conor Healy | |
| 3 — Rendering & Compute | Damien O Brien | |
| 4 — Interaction System | Colin Forde | |
| 5 — Networking / Server | Mark Mannion | |
| 6 — Desktop GUI & Client Shell | Con | |
| 7 — Persistence & Data | Sean Corrigan | |
