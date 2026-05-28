# Team Alpha — Sub-team 3 Standup Notes
**Project:** iDaVIE — Refactoring `VolumeDataSetRenderer` (God Class Split using SRP)
**Team Members:** Damien, Cathal, Chris, Ciallian
**Module:** Software Construction — Section 6.3
 
---
 
## Status Symbols (Quick Reference)
- ✅ = Completed
- 🔄 = In Progress
- ⏳ = Blocked / Waiting
- 📅 = Planned for Today
---
 
## Team Rotation Schedule
 
| Week | Scrum Master | Tech Lead | Quality Champion | PO Liaison |
|------|--------------|-----------|------------------|------------|
| 1 (May 18–22) | Cathal   | Damien   | Chris    | Ciallian |
| 2 (May 25–29) | Damien   | Cathal   | Ciallian | Chris    |
| 3 (Jun 1–5)   | Ciallian | Chris    | Cathal   | Damien   |
 
---
 
# WEEK 1 (May 18–22, 2026) 
 
### Roles
- **Scrum Master:** Cathal
- **Tech Lead:** Damien
- **Quality Champion:** Chris
- **PO Liaison:** Ciallian
 
---
 
### Monday, May 18 
**Present:** Cathal, Damien, Chris, Ciallian
 
**Cathal (Scrum Master)**
- ✅ Completed: Booked recurring standup slot
- 🔄 In Progress: Drafting sprint backlog skeleton in the team board
- 📅 Today: Run kickoff meeting, walk team through the assignment brief
  
**Damien (Tech Lead)**
- ✅ Completed: Cloned the iDaVIE repo
- 🔄 In Progress: Reading through `CONTEXT.md` and `README.md` to understand scope
- 📅 Today: Begin reading the existing `VolumeDataSetRenderer` source file
  
**Chris (Quality Champion)**
- ✅ Completed: Reviewed module brief for QA expectations
- 🔄 In Progress: Researching CK metrics tooling (Understand, NDepend)
  
**Ciallian (PO Liaison)**
- 🔄 In Progress: Drafting list of stakeholder questions for lecturer/PO
- 📅 Today: Email PO for clarification on grading criteria
   
---
 
### Tuesday, May 19 
**Present:** Cathal, Damien, Chris, Ciallian
 
**Cathal (Scrum Master)**
- ✅ Completed: Sprint board set up in GitHub Projects
- 🔄 In Progress: Scheduling ceremonies (planning, retro, review)
- 📅 Today: Define Definition of Done with the team
  
**Damien (Tech Lead)**
- ✅ Completed: First pass read of `VolumeDataSetRenderer.cs`
- 🔄 In Progress: Mapping the 4 responsibility clusters (material, texture, camera, foveation)
- 📅 Today: Sketch a draft "before" class diagram in PlantUML
  
**Chris (Quality Champion)**
- ✅ Completed: Installed Understand tool for CK metrics
- 🔄 In Progress: Drafting QA strategy document
- 📅 Today: Run baseline CK metrics on the God Class
  
**Ciallian (PO Liaison)**
- ✅ Completed: Got reply from PO with grading rubric clarifications
- 🔄 In Progress: Documenting acceptance criteria for the refactor
- 📅 Today: Share rubric breakdown with team and align on priorities
 
---
 
### Wednesday, May 20 
**Present:** Cathal, Damien, Chris, Ciallian
 
**Cathal (Scrum Master)**
- ✅ Completed: Definition of Done agreed and pinned to the board
- 🔄 In Progress: Capacity planning for remainder of sprint
- 📅 Today: Run a design workshop on the proposed class split
  
**Damien (Tech Lead)**
- ✅ Completed: Draft "before" PlantUML diagram (`diagrams/class-before.puml`)
- 🔄 In Progress: Identifying SOLID violations (SRP, OCP, DIP)
- 📅 Today: Lead the design workshop — propose splitting into 4 classes
  
**Chris (Quality Champion)**
- ✅ Completed: Baseline CK metrics captured (WMC, CBO, RFC, LCOM all over threshold)
- 🔄 In Progress: Building the "before" metrics table for the report
- 📅 Today: Define what "improved" means quantitatively — set target metrics
  
**Ciallian (PO Liaison)**
- ✅ Completed: Acceptance criteria document drafted and shared
- 🔄 In Progress: Validating priorities with PO — which class split matters most?
- 📅 Today: Sit in on design workshop, capture PO concerns
 
---
 
### Thursday, May 21 
**Present:** Cathal, Damien, Chris, Ciallian
 
**Cathal (Scrum Master)**
- ✅ Completed: Sprint tickets broken down and assigned
- 🔄 In Progress: Tracking velocity now that implementation is starting
- 📅 Today: Daily check-ins on first code commits
  
**Damien (Tech Lead)**
- ✅ Completed: Created feature branch `refactor/split-volumedataset`
- 🔄 In Progress: Extracting `MaterialBinder` class (first cut)
- 📅 Today: Get `MaterialBinder` compiling with no behaviour change
  
**Chris (Quality Champion)**
- ✅ Completed: "Before" metrics report finalised
- 🔄 In Progress: Writing characterisation tests against the existing God Class so we can verify behaviour parity
- 📅 Today: Get characterisation tests green on the baseline
  
**Ciallian (PO Liaison)**
- ✅ Completed: Acceptance criteria signed off by PO
- 🔄 In Progress: Drafting the "before" half of the report writeup
- 📅 Today: Capture screenshots of baseline diagrams for the report
 
---
 
### Friday, May 22 
**Present:** Cathal, Damien, Chris, Ciallian
 
**Cathal (Scrum Master)**
- ✅ Completed: Ran first retrospective — captured "more planning, less rushing" as a Week 1 win
- 🔄 In Progress: Sprint review prep
- 📅 Today: Sprint review at 3 PM, hand over SM role to Damien
  
**Damien (Tech Lead)**
- ✅ Completed: `MaterialBinder` extracted, compiles, characterisation tests still green
- 🔄 In Progress: Starting `TextureManager` extraction
- 📅 Today: Code-review session with Cathal before handover
  
**Chris (Quality Champion)**
- ✅ Completed: Characterisation test suite (12 tests, all green)
- ✅ Completed: "Before" CK metrics table finalised
- 📅 Today: Sprint review demo of the test harness
  
**Ciallian (PO Liaison)**
- ✅ Completed: "Before" writeup section drafted
- ✅ Completed: PO walked through current state, happy with progress
- 📅 Today: Sprint review with stakeholders
 
---
 
# WEEK 2 (May 25–29, 2026) 
 
### Roles
- **Scrum Master:** Damien
- **Tech Lead:** Cathal
- **Quality Champion:** Ciallian
- **PO Liaison:** Chris
 
---
 
### Monday, May 25 
**Present:** Damien, Cathal, Ciallian, Chris
 
**Cathal (Tech Lead)**
- ✅ Completed: Reviewed Damien's `MaterialBinder` extraction from last sprint
- 🔄 In Progress: Extracting `TextureManager` — moving texture upload + eviction logic
- 📅 Today: Get `TextureManager` compiling, leave wiring for tomorrow
  
**Damien (Scrum Master)**
- ✅ Completed: Took handover from Cathal, reviewed sprint board state
- 🔄 In Progress: Sprint 2 planning — broke remaining work into tickets per class
- 📅 Today: Run sprint planning at 11, set up daily standup time
  
**Ciallian (Quality Champion)**
- ✅ Completed: Ran full characterisation suite on Cathal's WIP branch — all green
- 🔄 In Progress: Adding unit tests specifically for the extracted `MaterialBinder`
- 📅 Today: Get `MaterialBinder` unit-test coverage above 80%
  
**Chris (PO Liaison)**
- ✅ Completed: Collected Week 1 feedback from PO and lecturer
- 🔄 In Progress: Drafting the "during refactor" section of the report
- 📅 Today: Confirm whether we need a video demo or just the written report
 
---
 
### Tuesday, May 26 
**Present:** Damien, Cathal, Ciallian, Chris
 
**Cathal (Tech Lead)**
- ✅ Completed: `TextureManager` extracted and compiling
- 🔄 In Progress: Rewiring `VolumeDataSetRenderer` to delegate texture calls to the new manager
- 📅 Today: Get characterisation tests passing with the new wiring
  
**Damien (Scrum Master)**
- ✅ Completed: Sprint board updated with new tickets, capacity numbers logged
- 🔄 In Progress: Watching for blockers, tracking burndown
- 📅 Today: 1:1 check-ins with each team member
  
**Ciallian (Quality Champion)**
- ✅ Completed: `MaterialBinder` unit tests at 88% coverage
- 🔄 In Progress: Writing unit tests for `TextureManager` (memory budget, eviction order)
- 📅 Today: Get a first pass of texture eviction tests written
  
**Chris (PO Liaison)**
- ✅ Completed: Confirmed with PO: written report + 5-minute video walkthrough
- 🔄 In Progress: Outlining the video script
- 📅 Today: Share video script outline with team for input
 
---
 
### Wednesday, May 27 
**Present:** Damien, Cathal, Ciallian, Chris
 
**Cathal (Tech Lead)**
- ✅ Completed: `TextureManager` fully wired in, all characterisation tests green
- 🔄 In Progress: `CameraDriver` extraction — clip planes and projection matrix logic
- ⏳ Blocked: Found a hidden coupling — `CameraDriver` logic touches a private field on the renderer that we'll need to expose carefully
- 📅 Today: Pair with Damien to figure out clean way to expose the field
  
**Damien (Scrum Master)**
- ✅ Completed: 1:1s wrapped, no surprises
- 🔄 In Progress: Helping Cathal unblock the `CameraDriver` coupling issue
- 📅 Today: Pair session with Cathal at 2 PM, then update the board
  
**Ciallian (Quality Champion)**
- ✅ Completed: `TextureManager` unit tests at 84% coverage
- 🔄 In Progress: Performance regression check — does delegation cost us frame time?
- 📅 Today: Run profiler on before/after build, capture numbers for the report
  
**Chris (PO Liaison)**
- ✅ Completed: Video script outline approved by team
- 🔄 In Progress: Recording draft narration for the first two extractions
- 📅 Today: Sit in on the Damien/Cathal pair session so the demo reflects reality
 
---
 
### Thursday, May 28
**Present:** Damien, Cathal, Ciallian, Chris
 
**Cathal (Tech Lead)**
- ✅ Completed: `CameraDriver` extracted using injected `ICameraUniformsSink` interface — coupling resolved cleanly
- 🔄 In Progress: Starting `FoveatedSampler` extraction (last class)
- 📅 Today: Get `FoveatedSampler` extracted, leave wiring for Friday
  
**Damien (Scrum Master)**
- ✅ Completed: Blocker resolved, burndown back on track
- 🔄 In Progress: Prepping retrospective for tomorrow — gathering "what went well / what to improve"
- 📅 Today: Send out anonymous retro form, book the room for 3 PM Friday
  
**Ciallian (Quality Champion)**
- ✅ Completed: Performance regression check — no measurable frame-time loss after split
- ✅ Completed: `CameraDriver` unit tests at 82% coverage
- 🔄 In Progress: Building the "after" CK metrics table
- 📅 Today: Run Understand on the refactored branch, capture metrics
  
**Chris (PO Liaison)**
- ✅ Completed: Draft narration for first three classes recorded
- 🔄 In Progress: Filling in the "after" section of the writeup
- 📅 Today: PO walkthrough at 1 PM, capture any last requests
 
---
 
### Friday, May 29 
**Present:** Damien, Cathal, Ciallian, Chris
 
**Cathal (Tech Lead)**
- ✅ Completed: `FoveatedSampler` extracted and wired in — refactor structurally complete
- 🔄 In Progress: Final cleanup pass on the original `VolumeDataSetRenderer` (now a thin coordinator)
- 📅 Today: Tag the refactor branch, open PR for review
  
**Damien (Scrum Master)**
- ✅ Completed: Sprint metrics collected, retro form responses gathered
- 🔄 In Progress: Running sprint review at 11 and retrospective at 3
- 📅 Today: Hand over SM role to Ciallian, brief her on what's outstanding
  
**Ciallian (Quality Champion)**
- ✅ Completed: Full characterisation + unit test suite green across all four classes
- ✅ Completed: "After" CK metrics captured — WMC, CBO, RFC, LCOM all under threshold
- 📅 Today: Sign off on quality for sprint review
  
**Chris (PO Liaison)**
- ✅ Completed: Writeup draft v1 complete (before + during + after sections)
- ✅ Completed: PO signed off on the refactor scope
- 📅 Today: Sprint review presentation, collect feedback for the report

---
 
# WEEK 3 (June 1–5, 2026) 
 
### Roles
- **Scrum Master:** Ciallian
- **Tech Lead:** Chris
- **Quality Champion:** Cathal
- **PO Liaison:** Damien
 
---
 
### Monday, June 1 
**Present:** Ciallian, Chris, Cathal, Damien
 
**Chris (Tech Lead)**
- ✅ Completed:
- 🔄 In Progress:
- ⏳ Blocked:
- 📅 Today:
  
**Ciallian (Scrum Master)**
- ✅ Completed:
- 🔄 In Progress:
- 📅 Today:
  
**Cathal (Quality Champion)**
- ✅ Completed:
- 🔄 In Progress:
- 📅 Today:
  
**Damien (PO Liaison)**
- ✅ Completed:
- 🔄 In Progress:
- 📅 Today:
 
---
 
### Tuesday, June 2 
**Present:** Ciallian, Chris, Cathal, Damien
 
**Chris (Tech Lead)**
- ✅ Completed:
- 🔄 In Progress:
- 📅 Today:
  
**Ciallian (Scrum Master)**
- ✅ Completed:
- 🔄 In Progress:
- 📅 Today:
  
**Cathal (Quality Champion)**
- ✅ Completed:
- 🔄 In Progress:
- 📅 Today:
  
**Damien (PO Liaison)**
- ✅ Completed:
- 🔄 In Progress:
- 📅 Today:
 
---
 
### Wednesday, June 3 
**Present:** Ciallian, Chris, Cathal, Damien
 
**Chris (Tech Lead)**
- ✅ Completed:
- 🔄 In Progress:
- ⏳ Blocked:
- 📅 Today:
  
**Ciallian (Scrum Master)**
- ✅ Completed:
- 🔄 In Progress:
- 📅 Today:
  
**Cathal (Quality Champion)**
- ✅ Completed:
- 🔄 In Progress:
- 📅 Today:
  
**Damien (PO Liaison)**
- ✅ Completed:
- 🔄 In Progress:
- 📅 Today:
   
---
 
### Thursday, June 4 
**Present:** Ciallian, Chris, Cathal, Damien
 
**Chris (Tech Lead)**
- ✅ Completed:
- 🔄 In Progress:
- 📅 Today:
  
**Ciallian (Scrum Master)**
- ✅ Completed:
- 🔄 In Progress:
- 📅 Today:
  
**Cathal (Quality Champion)**
- ✅ Completed:
- 🔄 In Progress:
- 📅 Today:
  
**Damien (PO Liaison)**
- ✅ Completed:
- 🔄 In Progress:
- 📅 Today:
 
---
 
### Friday, June 5 
**Present:** Ciallian, Chris, Cathal, Damien
 
**Chris (Tech Lead)**
- ✅ Completed:
- 🔄 In Progress:
- 📅 Today:
  
**Ciallian (Scrum Master)**
- ✅ Completed:
- 🔄 In Progress:
- 📅 Today:
  
**Cathal (Quality Champion)**
- ✅ Completed:
- 🔄 In Progress:
- 📅 Today:
  
**Damien (PO Liaison)**
- ✅ Completed:
- 🔄 In Progress:
- 📅 Today:
 
---
