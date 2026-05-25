# Sprint 1 Retrospective — Sub-team 6 (Die Boks / Team Alpha)

- **Sprint window:** Mon 18 May – Fri 22 May 2026 (Days 1–5)
- **Retro date:** Fri 22 May 2026 (Day 5) — 1 h sub-team session, §10.2
- **Attendees:** _(SM ticks who was present)_  Con · Mark · Jimmy · Rory
- **Facilitator (Sprint-1 SM):** Con
- **Companion docs:** [`week1-sprint-review.md`](week1-sprint-review.md) · [`standups.md`](standups.md) · [`../../ai-log.md`](../../ai-log.md)

> Inward-facing. How did the sprint feel, and what do we change for Sprint 2? Review answered "what did we ship?" — this answers "how did we work, and was it sustainable?"

---

## 1. Went well

_(3–6 specific items. Cite an artefact / date / commit when you can.)_

- _(e.g. "The Debug-tab skeleton compiled first-try on Day 3 because the interfaces — `ILogStream`, `ILogObserver`, `IDebugTabViewModel` — were defined before the implementation. Repeat this for File-tab in Sprint 2.")_
- _(…)_
- _(…)_

## 2. Didn't go well / hurt

_(3–6 specific items. Honest, blame-free. Cite the gap.)_

- _(e.g. "BNCH-3 (CodeScene) PDF was imported on Day 3 but the body was never written — the markdown is 4 empty pages. Tail item now on Sprint 2 critical path.")_
- _(e.g. "Day-5 standup has Jimmy + Rory with no 'Today' entry — we missed the cadence on the busiest day.")_
- _(…)_

## 3. What we learned

_(2–4 insights the panel can probe at interview. Tie to LO7 / LO8 where you can.)_

- _(e.g. "AI code-gen is one-shot when interfaces are pre-defined; it ping-pongs when we ask it to both design and implement in one prompt.")_
- _(…)_

## 4. Action items for Sprint 2

Each item: **owner + by-when**. No owner = no commitment.

| # | Action | Owner | By |
|---|---|---|---|
| A1 | Produce CodeScene hotspot/churn body (BNCH-3) | Rory | Day 7 |
| A2 | Produce DV8 + NDepend DSM + propagation cost (BNCH-4) | Rory | Day 7 |
| A3 | Fill ADR-0002 author line ("Sub-team 6 TL — fill in") | Mark | Day 6 |
| A4 | Add text-source companion for `concern-map.png` (§10.4 compliance) | _(?)_ | Day 7 |
| A5 | Resolve the 6 `_TODO_` markers in MVVM binding policy | Con (incoming TL) | Day 8 |
| A6 | Formal interface hand-off to Sub-team 1 at Day-8 integration review | Con (incoming TL) | Day 8 |
| A7 | _(team add)_ | | |

## 5. Role-rotation handover (Sprint 1 → Sprint 2)

§10.1 standard rotation. Each outgoing role-holder hands over open work + context to the incoming holder.

| Role | Out → In | Handover notes |
|---|---|---|
| **Scrum Master** | Con → Rory | _(open Kanban columns, blocker register, standup attendance pattern)_ |
| **Tech Lead** | Mark → Con | _(in-flight ADR-0002 author line; MVVM policy TODOs; Sub-team 1 conversation thread; Architecture Guild seat)_ |
| **PO Liaison** | Jimmy → Mark | _(backlog priorities for Sprint 2; outstanding REQ items; Team PO conversations)_ |
| **Quality Champion** | Rory → Jimmy | _(NDepend rules in progress; CodeScene + DV8 outstanding; CI dashboard access; Quality Guild seat + 10:30 huddle)_ |

## 6. Capacity check

Sprint 1: 21 cards at ~75 % planned capacity → **18 done, 1 partial, 2 carried**. Sprint 2: 31 cards.

_(One sentence: does Sprint 2 fit, given the Sprint-2 work already pulled forward — File-tab AFTER artefacts, both D5 testing docs, MVVM policy scaffold, both skeletons? Adjust if not.)_

## 7. AI usage — retrospective view

Continuous log is in [`ai-log.md`](../../ai-log.md) (6 entries). Retrospective summary for T8:

- **Where AI helped:** _(name the 2–3 highest-leverage assists. e.g. backlog drafting, Debug-tab skeleton scaffolding, ADR Nygard-format drafting.)_
- **Where AI failed:** _(name 1–2 places it produced wrong/misleading output. e.g. placeholder CK values in ADRs that needed manual correction; team-identity confusion early on.)_
- **What the human did instead:** _(the recovery, not the failure. Defensible at interview per §10.5.4.)_

## 8. One line each — what we'd tell ourselves on Day 1

_(Each member: one short lesson, signed. AI-free per §10.5.6 spirit — these feed individual reflections later.)_

- **Con:** _(…)_
- **Mark:** _(…)_
- **Jimmy:** _(…)_
- **Rory:** _(…)_

---

**Definition of done for this retro:**

- [ ] All sections filled by end of Day 5 (Fri 22 May 17:00)
- [ ] Action items entered on the Sprint-2 Kanban with owners + due dates
- [ ] Role-rotation handover §5 completed before Day-6 (Mon 25 May) Sprint 2 planning
- [ ] AI summary §7 reconciled with `ai-log.md`
