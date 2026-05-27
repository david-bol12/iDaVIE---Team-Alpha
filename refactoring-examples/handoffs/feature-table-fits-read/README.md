# Handoff: `FeatureTable.LoadFromFitsTable` — column-major FITS read + leak fix

**Status:** captured for handoff. **Not Sub-team 6 scope.** Reverted from `main` working tree on 2026-05-27.

## Origin

- Introduced in commit `ad62b9` ("updating to where team 6 is at", 2026-05-19, ConKirby) alongside an unrelated bulk update.
- Touches `Assets/Scripts/FeatureData/FeatureTable.cs` (lines 140–195 of the original file).
- Out of Sub-team 6's behavioural slice (Desktop GUI & Client Shell). Belongs to whichever sub-team owns `Assets/Scripts/FeatureData/`.
- Reverted from our branch because per `CLAUDE.md` the assignment is design-only and we must not modify production `Assets/` code.

## Why someone wrote it (defensible reasons)

The original loop in `LoadFromFitsTable` had three real problems:

1. **Native-memory leak.** Each call to `FitsReadColString` / `FitsReadColFloat` returns a pointer that must be released with `FreeFitsPtrMemory`. The original code read one cell per call (`numRows * numCols` calls) and never freed any of them — one leak per cell, two per string cell.
2. **P/Invoke overhead.** A table with R rows and C columns triggered R × C native transitions. Reading column-major collapses this to C calls.
3. **`int` row counter against a `long numRows`.** The original `for (int i = 0; i < numRows; i++)` silently truncates on tables with > 2³¹ rows.

The proposed change also adds `Debug.LogError` on non-zero `status` returns, which the original silently ignored.

## Why we reverted it from our branch

- Sub-team 6 owns Desktop GUI + Client Shell (`Assets/Scripts/UI/`, `Assets/Scripts/Menu/`). `FeatureData/` is not ours.
- The assignment is a **design proposal** — production code under `Assets/` must not be edited. Worked examples belong under `refactoring-examples/` or `docs/`.
- Bundled into an unrelated catch-up commit, with no review, no test coverage, no metric delta captured.

## Recommended next step

Hand this artefact to whichever sub-team owns FeatureData. They can either:

- adopt the fix as one of their own worked refactoring examples (it's a clean before/after with a measurable defect → fix story — leak count, P/Invoke count, max-rows correctness), or
- raise it as a defect against upstream iDaVIE if they don't want to claim it.

## Files in this folder

- `before.cs` — `LoadFromFitsTable` body as it existed pre-`ad62b9`.
- `after.cs` — `LoadFromFitsTable` body as introduced by `ad62b9`.
- `change.patch` — unified diff between the two (apply with `git apply`).
