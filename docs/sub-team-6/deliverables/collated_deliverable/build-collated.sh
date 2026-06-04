#!/usr/bin/env bash
# Assembles the single integrated Section 9.2 deliverable for Sub-team 6 (Team Alpha / Die Boks).
# Concatenates the curated source artifacts with unifying front-matter, part dividers,
# and code fences. Re-runnable: overwrites collated-deliverable.md each time.
set -euo pipefail

BASE="docs/sub-team-6/deliverables/collated_deliverable"
FT="$BASE/file-tab-refactor"
DT="$BASE/debug-tab-refactor"
OUT="$BASE/collated-deliverable.md"

# --- helpers --------------------------------------------------------------
sep() { printf '\n\n---\n\n' >> "$OUT"; }

# include a markdown file verbatim
md() { cat "$1" >> "$OUT"; sep; }

# include a C# file inside a fenced block, with a heading + source-path note
cs() {
  local path="$1"; local title="$2"
  {
    printf '#### %s\n\n' "$title"
    printf '> Source: `%s`\n\n' "$path"
    printf '```csharp\n'
    cat "$path"
    printf '\n```\n'
  } >> "$OUT"
  sep
}

# part divider
part() { printf '\n\n<div style="page-break-before: always;"></div>\n\n# %s\n\n' "$1" >> "$OUT"; }

# --- cover + document control --------------------------------------------
cat > "$OUT" <<'EOF'
# iDaVIE Refactoring Proposal — Desktop GUI & Client Shell
## Integrated Sub-team Deliverable (Section 9.2)

**Team:** Team Alpha · Sub-team 5 "Die Boks" · Work package §6.6 (informally "Team 6")
**Work package:** Desktop GUI and Client Shell
**Module:** CS4443 — Computation and Architecture 2, 2025/6
**Assignment:** ISE EPIC — Refactoring the iDaVIE Codebase for Maintainability (Mon 18 May – Fri 5 June 2026)
**Status:** Design-only refactoring proposal — no upstream production code is changed.

---

### What this document is

This is the single document that integrates the five Section 9.2 sub-team outputs into one
artefact, as required by the brief. Each Section 9.2 item is reproduced in full below — the
requirements document, the design document, the two mandated worked refactoring examples
(File tab and Debug tab, both with before/after UML, dependency graphs, CK metric deltas,
skeleton code and unit tests inlined), the test strategy, and the end-of-sprint Kanban
snapshots.

### Section 9.2 traceability — every required item is present

| §9.2 item | Required length | Where in this document | Status |
|---|---|---|---|
| 1. Sub-team requirements document | 1–2 pages | [Part 1 — Requirements](#part-1requirements-d1) | ✅ |
| 2. Sub-team design document | 5–10 pages (4-person team) | [Part 2 — Design (MVVM strategy)](#part-2design--mvvm-strategy-for-the-desktop-client-shell) | ✅ |
| 3. Worked refactoring examples (two, for a 4-person team) | — | [Part 3 — File tab](#part-3worked-example-1--file-tab) · [Part 4 — Debug tab](#part-4worked-example-2--debug-tab) | ✅ |
| 4. Sub-team test strategy | 2–4 pages | [Part 5 — Test Strategy](#part-5test-strategy) | ✅ |
| 5. Sub-team Kanban/Trello snapshot at end of each sprint | 3 sprints | [Part 6 — Kanban Snapshots](#part-6kanban-snapshots) | ✅ |

> Item 6 of the brief's per-sub-team list (daily stand-up notes, single shared file) is a
> separate living artefact and is not part of this collation.

### The two mandated worked examples (brief §6.6)

1. **File tab** — from a direct native-plugin call (`CanvassDesktop` → `FitsReader` →
   `[DllImport] idavie_native`) to a **ViewModel command dispatched through the service
   gateway** as JSON-RPC. Exercises the request/response transport path.
2. **Debug tab** — from a passive Unity-log readout to an **Observer of a structured logging
   stream** (`ILogStream` → `ILogObserver`). Exercises the server-pushed notification path.

Together they exercise the transport contract on both directions and prove the MVVM split
produces independently unit-testable classes (47 + 35 NUnit tests, zero Unity dependency).

### Architectural non-negotiables this proposal satisfies (brief §4.2)

1. No SOLID/GRASP violation without a documented trade-off.
2. Zero circular dependencies between top-level components.
3. Domain code must not transitively depend on `UnityEngine` / `SteamVR`.
4. Every public API boundary is an interface with at least one test double.

### Reading note

Each worked example (Parts 3 and 4) is presented fully inline: the code-anchored before/after
traces, the Mermaid before/after sequence diagrams, the before/after class diagrams, the
module-level dependency graphs, the CK metric delta tables, and then the complete skeleton
(pure-C#) source, the adapter source, and the NUnit test listings. Mermaid and `csharp` blocks
render in any Markdown viewer that supports GitHub-flavoured Markdown + Mermaid. Each embedded
section retains its own `Source:` path so any artefact can be traced back to its file in
`file-tab-refactor/` or `debug-tab-refactor/`.
EOF
sep

# --- PART 1 — Requirements -----------------------------------------------
part "Part 1 — Requirements (D1)"
md "$BASE/requirements.md"

# --- PART 2 — Design ------------------------------------------------------
part "Part 2 — Design — MVVM Strategy for the Desktop Client Shell"
md "$BASE/design.md"

# --- PART 3 — File tab worked example -------------------------------------
part "Part 3 — Worked Example 1 — File Tab"
cat >> "$OUT" <<'EOF'
**Mandated by brief §6.6:** *File tab — from direct native-plugin call → ViewModel command via service gateway.*

This worked example is presented in full: the before-state code trace and sequence diagram,
the after-state trace and sequence diagram, the before/after class diagrams, the module-level
dependency graph, the CK metric deltas, and then the complete skeleton (pure-C#) source, the
Unity/gateway adapter source, and the NUnit test listings.
EOF
sep

printf '## 3.1 Before-state code trace\n\n' >> "$OUT"; md "$FT/before-trace.md"
printf '## 3.2 Before-state sequence diagram\n\n' >> "$OUT"; md "$FT/before-sequence.md"
printf '## 3.3 After-state code trace\n\n' >> "$OUT"; md "$FT/after-trace.md"
printf '## 3.4 After-state sequence diagram\n\n' >> "$OUT"; md "$FT/after-sequence.md"
printf '## 3.5 Class diagram (before vs. after)\n\n' >> "$OUT"; md "$FT/class-diagram.md"
printf '## 3.6 Dependency graph (before vs. after)\n\n' >> "$OUT"; md "$FT/dependency-graph.md"
printf '## 3.7 CK metric deltas\n\n' >> "$OUT"; md "$FT/ck-metrics.md"

printf '## 3.8 Skeleton source (pure C#, no UnityEngine)\n\n' >> "$OUT"; md "$FT/skeleton/Skeleton-ReadME.md"
cs "$FT/skeleton/IFileTabViewModel.cs"   "IFileTabViewModel — View↔ViewModel contract"
cs "$FT/skeleton/IFitsService.cs"        "IFitsService — FITS metadata ACL boundary"
cs "$FT/skeleton/IFitsHandle.cs"         "IFitsHandle — server-resident FITS file lifetime"
cs "$FT/skeleton/IFileDialogService.cs"  "IFileDialogService — OS file-picker ACL boundary"
cs "$FT/skeleton/IVolumeService.cs"      "IVolumeService — Sub-team 1 gateway boundary"
cs "$FT/skeleton/IMemoryProbe.cs"        "IMemoryProbe — host-memory ACL boundary"
cs "$FT/skeleton/ICommand.cs"            "ICommand / IAsyncCommand — minimal command interfaces"
cs "$FT/skeleton/FileTabViewModel.cs"    "FileTabViewModel (+ AsyncRelayCommand / RelayCommand)"
cs "$FT/skeleton/SubsetBoundsViewModel.cs" "SubsetBoundsViewModel — clamping subset selector"
cs "$FT/skeleton/FitsMetadataHelper.cs"  "FitsMetadataHelper — pure-static FITS computations"
cs "$FT/skeleton/FitsFileInfo.cs"        "FitsFileInfo + HduInfo — FITS metadata DTOs"
cs "$FT/skeleton/LoadCubeRequest.cs"     "LoadCubeRequest + RatioMode — load request DTO"
cs "$FT/skeleton/SubsetBounds.cs"        "SubsetBounds — crop-region DTO"
cs "$FT/skeleton/CubeLoadedEventArgs.cs" "CubeLoadedEventArgs — cube-loaded event payload"

printf '## 3.9 Adapter source (Unity / gateway proxy)\n\n' >> "$OUT"; md "$FT/adapters/Adapters-ReadME.md"
cs "$FT/adapters/FitsServiceAdapter.cs"       "FitsServiceAdapter — gateway proxy (JSON-RPC, no Unity)"
cs "$FT/adapters/FileDialogServiceAdapter.cs" "FileDialogServiceAdapter — Unity adapter (SFB + PlayerPrefs)"
cs "$FT/adapters/VolumeServiceAdapter.cs"     "VolumeServiceAdapter — Unity adapter (coroutine + prefab + VCC)"
cs "$FT/adapters/MemoryProbeAdapter.cs"       "MemoryProbeAdapter — Unity adapter (SystemInfo)"
cs "$FT/adapters/FileTabView.cs"              "FileTabView — thin Unity MonoBehaviour view"
cs "$FT/adapters/FileTabCompositionRoot.cs"   "FileTabCompositionRoot — Pure-DI composition root"

printf '## 3.10 Unit tests (NUnit, zero Unity dependency)\n\n' >> "$OUT"
cs "$FT/tests/FileTabViewModelTests.cs"            "FileTabViewModelTests + SubsetBoundsViewModelTests (47 tests, Tier 1)"
cs "$FT/adapters/tests/FitsServiceAdapterTests.cs" "FitsServiceAdapterTests (4 wire-shape tests, Tier 2)"

# --- PART 4 — Debug tab worked example ------------------------------------
part "Part 4 — Worked Example 2 — Debug Tab"
cat >> "$OUT" <<'EOF'
**Mandated by brief §6.6:** *Debug tab — as Observer of a structured logging stream.*

This worked example is presented in full, in the same shape as the File tab: before/after
traces and sequence diagrams, before/after class diagrams, the dependency graph, the log-origin
trace cataloguing all 44 `Debug.Log*` call sites, the CK metric deltas, and then the complete
skeleton source, adapter source, and NUnit test listings.

Honest framing carried over from the CK analysis: the Debug tab is a **testability and structure**
refactor, not a metric refactor — `DebugLogging` already passes 5 of 6 CK thresholds; the case
rests on the untestable static log hook (S1) and four-concerns-in-one-class (S8).
EOF
sep

printf '## 4.1 Before-state code trace\n\n' >> "$OUT"; md "$DT/before-trace.md"
printf '## 4.2 Before-state sequence diagram\n\n' >> "$OUT"; md "$DT/before-sequence.md"
printf '## 4.3 After-state code trace\n\n' >> "$OUT"; md "$DT/after-trace.md"
printf '## 4.4 After-state sequence diagram\n\n' >> "$OUT"; md "$DT/after-sequence.md"
printf '## 4.5 Class diagram (before vs. after)\n\n' >> "$OUT"; md "$DT/class-diagram.md"
printf '## 4.6 Dependency graph (before vs. after)\n\n' >> "$OUT"; md "$DT/dependency-graph.md"
printf '## 4.7 Log-origin trace (all 44 Debug.Log* call sites)\n\n' >> "$OUT"; md "$DT/log-origin-trace.md"
printf '## 4.8 CK metric deltas\n\n' >> "$OUT"; md "$DT/ck-metrics.md"

printf '## 4.9 Skeleton source (pure C#, no UnityEngine)\n\n' >> "$OUT"
cs "$DT/skeleton/ILogStream.cs"        "ILogStream + LogLevel + LogEntry — producer contract"
cs "$DT/skeleton/ILogObserver.cs"      "ILogObserver — consumer contract"
cs "$DT/skeleton/IDebugTabViewModel.cs" "IDebugTabViewModel — View↔ViewModel contract"
cs "$DT/skeleton/LogStream.cs"         "LogStream — thread-safe Observer dispatch"
cs "$DT/skeleton/DebugTabViewModel.cs" "DebugTabViewModel — bounded log observer"

printf '## 4.10 Adapter source (Unity / gateway proxy)\n\n' >> "$OUT"
cs "$DT/adapters/GatewayLogStreamAdapter.cs"  "GatewayLogStreamAdapter — gateway proxy (log.emit, no Unity)"
cs "$DT/adapters/DebugTabView.cs"             "DebugTabView — thin Unity MonoBehaviour view"
cs "$DT/adapters/DebugTabCompositionRoot.cs"  "DebugTabCompositionRoot — Pure-DI composition root"

printf '## 4.11 Unit tests (NUnit, zero Unity dependency)\n\n' >> "$OUT"
cs "$DT/tests/DebugTabTests.cs"                       "LogStreamTests + DebugTabViewModelTests (31 tests, Tier 1)"
cs "$DT/adapters/tests/GatewayLogStreamAdapterTests.cs" "GatewayLogStreamAdapterTests (4 notification tests, Tier 2)"

# --- PART 5 — Test strategy -----------------------------------------------
part "Part 5 — Test Strategy"
md "$BASE/test-strategy.md"

# --- PART 6 — Kanban snapshots --------------------------------------------
part "Part 6 — Kanban Snapshots"
cat >> "$OUT" <<'EOF'
Brief §9.2 item 5 requires a Kanban/Trello snapshot at the end of each sprint. The three
end-of-sprint board snapshots are reproduced below (PNG, source-controlled under
`kanban-snapshots/`).

### Sprint 1 — end of week 1

![Sprint 1 Kanban snapshot](kanban-snapshots/week1-snapshot.png)

### Sprint 2 — end of week 2

![Sprint 2 Kanban snapshot](kanban-snapshots/week2-snapshot.png)

### Sprint 3 — end of week 3

![Sprint 3 Kanban snapshot](kanban-snapshots/week3-snapshot.png)

---

*End of integrated Section 9.2 deliverable — Sub-team 6 / Team Alpha / Die Boks (Desktop GUI & Client Shell).*
EOF

# Report size
wc -l "$OUT"
