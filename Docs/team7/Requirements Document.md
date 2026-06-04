Sub-team 14 \- Persistence and Workspace State: Requirements

FR1 \- Define the Workspace Domain Model

The system shall define a single domain aggregate that represents the complete, recoverable  
state of an iDaVIE session. This aggregate must capture the following state groups:

  Loaded data  
    File paths, HDU index, subset bounds

  Render settings  
    Colour map, scaling type, thresholds, projection mode, scaling parameters

  View and camera  
    World transform, slice bounds, depth axis

  Mask edit history  
    Ordered list of brush stroke records

  Shape history  
    Shape type, transform, add/delete order

  Feature sets  
    Feature set type, source catalog paths, user-defined feature data

  Rest frequency  
    Selected frequency and override flag

  App config  
    Voice settings, moment map defaults, histogram step config

The aggregate must have documented invariants. For example, a mask file path may only be set  
when a primary file path is also set, and threshold min must not exceed threshold max.

FR2 \- State Capture

The system shall provide a mechanism to read all session state from live runtime objects into  
the domain aggregate. Currently no such mechanism exists \- the only save behaviour on exit is  
writing the FITS mask file.

The capture mechanism must read from all source classes listed in FR1, convert any  
framework-specific types (such as 3D vectors, quaternions, and colours) into plain serialisable  
equivalents, and require each domain class to expose a read accessor for any state that is  
currently private.

FR3 \- State Store

The system shall serialise the domain aggregate to a versioned JSON file on disk.

The file format must include a schema version field using semantic versioning, a UTC timestamp,  
and an integrity checksum. Writes must be atomic \- a crash during a write must not corrupt the  
previously saved snapshot.

FR4 \- State Restore

The system shall deserialise a snapshot file and apply each field back to the correct live  
runtime object. The restore path must be the symmetric reverse of FR2.

The restore mechanism must apply state to each sub-system in dependency order (data must be  
loaded before rendering state is applied, rendering before features, and so on). It must handle  
missing or invalid values gracefully per the recovery rules defined in each state contract, and  
must never crash or enter undefined behaviour due to invalid saved state.

FR5 \- State Contracts with All Sub-teams

The system shall define a formal interface between the persistence layer and each of the  
following sub-teams, specifying what state must be readable for capture and writable for  
restore. All contracts must use pure C\# types with no Unity or SteamVR dependencies.

  Data I/O  
    Reads: file path, mask path, HDU index, subset bounds, spectral system and unit,  
           standard of rest, transformed spectral value  
    Writes: trigger file load with saved parameters

  Rendering  
    Reads: colour map, scaling, thresholds, projection mode, spatial transform,  
           rest frequency override, foveation settings, mask render settings,  
           moment map settings  
    Writes: apply all rendering parameters to the renderer

  Interaction  
    Reads: active interaction mode, locomotion state, active tool  
    Writes: reset to restored interaction mode and tool

  Features  
    Reads: all feature sets (masked, user-created, imported) with source catalog paths  
    Writes: reconstruct feature sets; recompute statistics from data

  Desktop GUI  
    Reads: file path, HDU, depth axis, subset bounds, full cube bounds  
    Writes: repopulate file-loading UI fields

  Architecture  
    Reads: plugin versions, kernel config state  
    Writes: restore kernel configuration

These contracts must be signed off by the Architecture Guild before Day 9 (Thu 28 May).

FR6 \- Autosave

The system shall periodically save a workspace snapshot without user intervention.

The save interval must be configurable with a default of 20 seconds. A save must be triggered  
by any change to mask, shapes, features, thresholds, or camera state using a dirty flag. A save  
in progress must not be interrupted by the next timer tick, and every autosave write must be  
atomic as defined in FR3.

FR7 \- Crash Recovery

The system shall detect on startup whether the previous session exited uncleanly and offer the  
user the option to restore from the most recent snapshot. Four states must be handled:

  Clean exit  
    Detection: no crash indicator present  
    Behaviour: start fresh

  Crash with valid snapshot  
    Detection: crash indicator present; latest snapshot passes integrity check  
    Behaviour: offer restore dialog

  Crash with corrupted snapshot  
    Detection: crash indicator present; snapshot fails integrity check or cannot be parsed  
    Behaviour: warn user, offer start fresh, log the error

  Incompatible snapshot version  
    Detection: snapshot major version exceeds current  
    Behaviour: attempt migration (FR8), otherwise warn and discard

On a clean exit the application must write a clean-exit marker. On startup the presence or  
absence of this marker determines which path is taken.

FR8 \- Schema Migration

The system shall be able to load a snapshot file saved by an older version of iDaVIE.

Each schema version must be uniquely identified. A migration step must exist for each version  
transition, and migrations must be applied in order to bring any older snapshot up to the  
current version. A snapshot whose major version exceeds the current tool version must be  
rejected with a clear error.

NFR1 \- No Unity Dependency in the Persistence Layer (Mandatory)

Source: Spec Section 4.2 constraint 3\.

All persistence domain and application classes must compile and run outside Unity, in a plain  
NUnit test project with no Unity assemblies referenced. Unity-specific types must be mapped to  
plain C\# equivalents at the adapter boundary.

NFR2 \- Interface-Based Public APIs (Mandatory)

Source: Spec Section 4.2 constraint 4\.

Every public boundary in the persistence layer must be defined as an interface. This applies to  
all sub-team adapters, the snapshot repository, the migrator, and the workspace collector. Each  
interface must have at least one documented test double.

NFR3 \- CK Metric Targets

Source: Spec Section 7.1.

  Workspace snapshot aggregate  
    WMC: 5 or under  |  CBO: 0  |  LCOM: 0.1 or under

  Snapshot serialiser / repository  
    WMC: 10 or under  |  CBO: 5 or under  |  LCOM: 0.3 or under

  State collection orchestrator  
    WMC: 15 or under  |  CBO: 8 or under  |  LCOM: 0.4 or under

  Autosave service  
    WMC: 8 or under  |  CBO: 4 or under  |  LCOM: 0.3 or under

  Config (post-split)  
    WMC: 3 or under  |  CBO: 0  |  LCOM: 0.1 or under

Baseline metrics for Config.cs and ExitController.cs must be recorded on Day 2 as the  
"before" figures.

Summary and Deliverable Mapping

  FR1 \- Domain model  
    Deliverable: Design doc section 1, Worked Example 2 before/after UML  
    Due: Day 7 (Tue 26 May)

  FR2 \- Capture  
    Deliverable: Worked Example 2 (capture half), state contract drafts  
    Due: Day 7

  FR3 \- Store  
    Deliverable: Worked Example 1 (Config split) \+ Example 2 (store half)  
    Due: Day 7-9

  FR4 \- Restore  
    Deliverable: Design doc section 3, sequence diagram  
    Due: Day 9 (Thu 28 May)

  FR5 \- State contracts  
    Deliverable: Six interface skeletons distributed to sub-teams  
    Due: Day 6 (Mon 25 May) \- critical path

  FR6 \- Autosave  
    Deliverable: Design doc section 4, autosave service design  
    Due: Day 9

  FR7 \- Crash recovery  
    Deliverable: Design doc section 5, state machine diagram  
    Due: Day 9

  FR8 \- Migration  
    Deliverable: Design doc section 6, migration pipeline design  
    Due: Day 11

  NFR1 \- No Unity dep  
    Deliverable: Both worked examples, test strategy  
    Due: Day 9

  NFR2 \- Interfaces  
    Deliverable: Both worked examples  
    Due: Day 9

  NFR3 \- CK metrics  
    Deliverable: Day 2 baseline \+ Day 13 projected delta  
    Due: Day 2 \+ Day 12  
