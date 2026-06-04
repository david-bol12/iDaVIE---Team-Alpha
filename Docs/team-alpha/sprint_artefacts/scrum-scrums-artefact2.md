# **Team Alpha — Scrum of Scrums (09:15 cross-sub-team stand-up)**

**What this is:** the cohort-level daily sync that runs straight after each sub-team's 09:00 stand-up. One representative per sub-team (usually the SM) brings up only the things that cross a team boundary — shared contracts, blockers on another team, tooling that affects everyone. Local in-team work stays in the 09:00 stand-up.

**Facilitator (Sprint 2 / Week 2):** Team SM (Sprint 2 rotation — facilitation passed on with the role from Con Kirby). **Cadence:** 09:15–09:30, Mon–Fri. Architecture Guild weekly review folds in on the Wednesday slot. **Perspective:** record kept by Sub-team 7 (Persistence and Workspace State — "us") this sprint.

---

## **Day 6 — Mon 25 May 2026 (Sprint 2 planning)**

* Sprint 2 planning ran in-team (2h) then 1h at team level. New SM picks up facilitation from the rotation.  
* **Gateway contract v0.1 frozen Friday** — clients (Sub-teams 3/4/5/6) now code against it from this sprint. DEPS-1 closes once Sub-team 1 ratifies Sub-team 6's gateway stub.  
* Component diagrams \+ SysML BDDs to be frozen today (Day 6 focus).  
* **Headline Sprint-2 commitment restated:** every sub-team owes its **state contract** (the shape of what it needs saved/restored) to Sub-team 7 (us) by Day 9\. Architecture Guild sign-off of all 6 contracts is the Sprint-2 exit criterion.  
* **R04 (VR↔Desktop menu vocabulary)** carried in from Sprint 1 — Sub-teams 4 & 6 to hold the sync this sprint.

**Decisions / actions:**

* Component/BDD freeze by EOD.  
* Sub-team 7 to publish a **state-contract template** for the other six to fill, so contracts arrive in a consistent shape ahead of the Day-9 sign-off.

---

## **Day 7 — Tue 26 May 2026**

**Round the room:**

* **Sub-team 1:** worked example 1/2 drafted; minor gateway-schema clarifications fielded from clients.  
* **Sub-team 7 (us):** Worked Example 1 (`RenderViewState` extraction) drafted — pulling a pure C\# value object out of `VolumeDataSetRenderer`. Depends on Sub-team 3 confirming the render-state shape, which feeds directly into Rendering's state contract to us.  
* **Others:** worked refactoring examples 1/2 being drafted with before/after CK metrics (WMC, DIT, NOC, CBO, RFC, LCOM).  
* State-contract template circulated; first drafts beginning to trickle in.

**Decisions / actions:**

* Chase the six state contracts — target all received by Day 9\.  
* R01: gateway v0.1 in active use; schema clarifications routed to Sub-team 1, no contract change.

---

## **Day 8 — Wed 27 May 2026 (Cross-sub-team integration review \+ Architecture Guild Wednesday review)**

**Round the room:**

* Scheduled **cross-sub-team integration review**. Reality check raised: most sub-teams are working in **individual folders / long-lived branches** with little actually merged. Branch divergence is growing — Sub-team 7's branch is now well ahead of main.  
* **Architecture Guild Wednesday review:** layer-violation CI rules being tightened so that by Day 10 a PR introducing an architecture violation, a circular dependency or a CK threshold breach is blocked from merge.  
* **Sub-teams 4 ↔ 6:** R04 menu-vocabulary sync — folding into the shared command-catalogue conversation.

**Decisions / actions:**

* **New risk raised to the register (Integration Lead): late integration / branch divergence (provisional R08).** Folder-per-person structure should prevent most conflicts, but the Assets folder is a likely overlap. Mitigation: a dedicated integration session early next week.  
* Quality Guild: harden CI metric gates by Day 10\.

---

## **Day 9 — Thu 28 May 2026 (Sprint 2 exit criterion)**

**Round the room:**

* **Sprint 2 exit criterion — Architecture Guild signs off all 6 state contracts delivered to Persistence.** Sub-teams 1–6 each confirm their state contract is delivered to Sub-team 7; we confirm receipt and reconciliation against the workspace domain model.  
* **State vs interface contract clarified** (Sub-team 4 raised it): a *state contract* is the list of variables a sub-team needs saved/restored (delivered to us); an *interface contract* is the actual integration interface between sub-teams, required under brief §5.4. The two are distinct and both are needed. Example contracts to be shared in the group chat for reference.  
* **Sub-team 4** noted it depends on all other teams and needs interface contracts from everyone.  
* Worked examples 2/2 complete across sub-teams; SOLID/GRASP audit per example.

**Decisions / actions:**

* 6 state contracts signed off — exit criterion met. R03 closing.  
* Interface contracts (§5.4) now the live integration concern; carry into the Day-12 integration session.

---

## **Day 10 — Fri 29 May 2026 (Sprint 2 review \+ retro; mid-assessment iDaVIE visit)**

* **Cross-sub-team Sprint 2 review** (15-min slot per sub-team).  
* **Mid-assessment iDaVIE-team visit** (30 min per team).  
* **Cross-sub-team retro** after the in-team retros.

**Round the room (review):**

* **Sub-team 1:** ADR log re-done; architecture diagrams refactored; CI pipeline complete (T6).  
* **Sub-team 2:** 2 worked examples done; testing documentation still outstanding.  
* **Sub-team 3:** all refactoring complete (god class split into components); docs complete; into interview prep.  
* **Sub-team 4:** 2 worked examples done (rendering \+ interaction control); advised against a 3rd (no extra marks — prioritise required requirements/testing/design docs).  
* **Sub-team 5:** refactoring nearly complete (CK metrics progressing); updating UML; docs need final updates.  
* **Sub-team 6:** effectively complete; 2 worked examples (File tab, Debug tab from the god class); into interview prep.  
* **Sub-team 7 (us):** complete persistence layer built; comprehensive tests run; crash-recovery scenarios being simulated in the Unity app; documentation needs consolidation.

**Cross-team discussion:**

* **Integration deferred:** the Day-8 integration review did not produce merged integration. Branch is now \~100 commits ahead → merge risk. Team commitment: dedicated **integration session Tue 2 June (Day 12), 06:00 start**. Folder-per-person should contain most conflicts; Assets folder is the overlap to watch. Suggestion to use an AI (Claude) agent to assist the merge.  
* **Pitch structure agreed** (per Appendix C, 40 min): 6 min pain points \+ metrics; 10 min architecture overview; 12 min worked examples (\~2 min per sub-team); 6 min testability \+ Unity 6 migration; 5 min trade-offs; 3 min summary.  
* **Interview prep:** Colin's sub-team interview-requirements doc incoming. No slides for sub-team interviews (slides only for the main pitch); 15 min per person on their component; focus on *how/why* and LLM usage over *what*. **Sub-team 6 interviews first (Wed 3 June, 10:00)** and will report feedback back to the rest.

**Sprint-2 cross-team status:**

* **R01 (gateway contract)** — v0.1 frozen; clients integrated against it; DEPS-1 closed on stub ratification. Effectively closed.  
* **R03 (state contracts)** — 6 contracts delivered to Persistence and signed off Day 9; state-vs-interface distinction clarified. Closing; interface contracts (§5.4) now the open thread.  
* **R04 (VR↔Desktop menus)** — folded into the shared command-catalogue \+ contract-sharing work; no blocker reported at review.  
* **R08 (NEW — late integration / branch divergence)** — High. \~100 commits ahead; mitigation is the Tue 2 June 06:00 integration session; owned by Integration Lead.

**Handover:** Sprint 3 begins Mon 1 June (compressed — no formal Sprint 3 planning; team enters substantially complete). Role elevations for Sprint 3: **Colin Forde → Team PO; Uday → Team SM** (facilitation passes to Uday). Risk register stays with Sub-team 1 (Integration Lead). Tuesday 2 June \= formal scrum \+ integration session; Thursday 4 June \= main-team pitch (Team Alpha 11:00). Artefact freeze Thu 4 June 11:00.

---

*Team Alpha — cross-sub-team coordination record. Week 2 (Sprint 2).*

