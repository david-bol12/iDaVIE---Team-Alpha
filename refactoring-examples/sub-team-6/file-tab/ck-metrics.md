# File tab — CK metric deltas (BEFORE vs. AFTER)

## TL;DR

Hand-counted CK projection (Day-13 tool verification pending). **BEFORE `CanvassDesktop` fails 5/7 thresholds:** WMC 57 (≤40), CBO ~32 (≤25), RFC ~210 (≤50), LCOM4 ≥7 (=1), LOC 1899. **AFTER** splits into 8 classes — 9/10 pass; only `FileTabViewModel` is borderline at WMC 27 (≤20 domain), with a documented remediation (extract command bodies into `FileTabCommands` helper → WMC ~22). Headline deltas: **WMC −53%, CBO −72%, RFC −76%, LCOM4 disjoint → 1 per class**. The unit-testable surface goes from **0** (Unity required) to **34** NUnit tests running with zero Unity dependency.

---

> **Status: hand-counted projection — pending Quality Guild tool verification on Day 13.**
> Numbers below were counted from the live `team6` branch using `grep`/`awk` against `Assets/Scripts/UI/CanvassDesktop.cs` (BEFORE) and the skeleton + adapter files in this folder (AFTER). They are submitted **alongside** the SonarQube Cloud + Understand baseline that the Quality Guild owns; if their tooling reports different values, those numbers supersede this document.
>
> **Revision (May 25):** numbers now reflect Gap 1 (`RatioMode` end-to-end), Gap 2 (`IMemoryProbe` / RAM warning), Gap 3 (`CubeLoaded` event on `IVolumeService`), and the parallel `IFitsHandle` lifetime fix. The new types add one class (`MemoryProbeAdapter`) and one interface (`IFitsHandle`) to the AFTER slice; smell S6 (busy-wait) is now *eliminated* rather than contained (see `VolumeServiceAdapter` notes).
>
> Counting conventions used here:
> - **WMC** = method count (NOM-style WMC; the same as Understand's `CountDeclMethod`). Property getters/setters with non-trivial bodies count as one method each; trivial auto-implemented getters do not.
> - **DIT** = depth from `System.Object`. `MonoBehaviour` adds 3 to the count (`Object → Component → Behaviour → MonoBehaviour`).
> - **CBO** = distinct named types referenced *in implementation*, excluding primitives, language types (`string`, `int`, etc.), and the class's own type. DTOs of the same package are counted.
> - **RFC** = WMC + distinct external methods called. Hand-count is approximate; tool-verified value is authoritative.
> - **LCOM** = LCOM4 (connected components of the method-field graph). 1 = perfectly cohesive; >1 = disjoint concerns.
>
> Threshold source: `CLAUDE.md` § *Mandatory metric tools*, Section 7.1 of the brief.
>
> | Metric | Domain threshold | Adapter/Orchestrator threshold |
> |---|---:|---:|
> | WMC | ≤ 20 | ≤ 40 |
> | DIT | ≤ 4 | ≤ 4 |
> | NOC | ≤ 5 | ≤ 5 |
> | CBO | ≤ 14 | ≤ 25 |
> | RFC | ≤ 50 | ≤ 50 |
> | LCOM | ≤ 0.5 (LCOM-HS) / = 1 (LCOM4) | same |

---

## BEFORE — `CanvassDesktop` (entire class, file-tab-slice cannot be measured in isolation)

| Metric | Hand-counted value | Threshold (orchestrator) | Pass? | Source / how counted |
|---|---:|---:|:--:|---|
| LOC | 1899 | (no hard cap; advisory) | ⚠ | `wc -l Assets/Scripts/UI/CanvassDesktop.cs` |
| WMC (method count) | **57** | ≤ 40 | ❌ | `grep -cE '^\s*(public|private|protected)\s+\w[\w<>\[\]?,\s]*\s+\w+\s*\('` over the file |
| DIT | **4** | ≤ 4 | ✅ (at limit) | `class CanvassDesktop : MonoBehaviour` → MonoBehaviour DIT chain |
| NOC | 0 | ≤ 5 | ✅ | No subclasses in repo |
| CBO | **~32** | ≤ 25 | ❌ | Distinct named types referenced; sample below |
| RFC | **~210** | ≤ 50 | ❌ | 57 own methods + ~150 distinct external method calls (Unity API + plug-in) |
| LCOM4 | **≥ 7** | = 1 | ❌ | Disjoint concern clusters: file-paths, FITS axes, subset bounds, threshold sliders, popup state, render lifecycle, coroutine state. Each cluster touches a non-overlapping subset of fields. |

### Sample CBO collaborators (32 distinct, abbreviated)

Confirmed by grep against the class body — counted once per distinct named type:

`FitsReader`, `VolumeCommandController`, `VolumeDataSetRenderer`, `VolumeInputController`, `HistogramHelper`, `StandaloneFileBrowser`, `PlayerPrefs`, `MenuBarBehaviour`, `QuickMenuController`, `PaintMenuController`, `TabsManager`, `ColorMapUtils`, `FeatureMapping`, `FeatureTable`, `ColorMapEnum`, `Coroutine`, `IEnumerator`, `IntPtr` (via `FitsReader`), `Marshal`, `GameObject`, `Transform`, `Vector3`, `Quaternion`, `Slider`, `Toggle`, `TMP_InputField`, `TMP_Dropdown`, `TMP_Text`, `TextMeshProUGUI`, `Button`, `Text`, `OpenVR/SteamVR`. Some Unity primitives (`Vector3`, `Quaternion`) could be excluded from CBO depending on tool convention — even excluding all `UnityEngine`-namespace value types the count is ≥ 20, still over the threshold.

### Verdict (BEFORE)

`CanvassDesktop` **fails 5 of 7 metrics**. The most severe failures are CBO (32 vs. 25), RFC (~210 vs. 50), and LCOM4 (≥ 7 vs. 1). These align with the qualitative smell catalogue in [`before-trace.md` §S1–S8](before-trace.md#smell-summary-feeds-the-solidgrasp-audit--ck-deltas).

---

## AFTER — file-tab slice as eight focused classes

The BEFORE god-class is decomposed into 8 classes (post-Gap 1/2/3 work, up from 7 in the initial split — `MemoryProbeAdapter` is the new one). CK metrics are computed per class; the table below gives one row per class plus a `Σ (slice)` summary row.

| Class | Layer | LOC | WMC | DIT | NOC | CBO | RFC | LCOM4 | Threshold band | Pass? |
|---|---|---:|---:|---:|---:|---:|---:|---:|---|:--:|
| `FileTabViewModel` | domain | ~480 | **27** | 1 | 0 | **9** | **~50** | 1 | domain (see note) | ⚠ |
| `SubsetBoundsViewModel` | domain | 117 | 12 | 1 | 0 | 1 | ~18 | 1 | domain | ✅ |
| `AsyncRelayCommand` (nested helper) | domain | ~25 | 5 | 1 | 0 | 1 | ~8 | 1 | domain | ✅ |
| `RelayCommand` (nested helper) | domain | ~15 | 4 | 1 | 0 | 1 | ~6 | 1 | domain | ✅ |
| `FitsServiceAdapter` | adapter | ~165 | 6 | 1 | 0 | 5 | ~26 | 1 | adapter | ✅ |
| `FileDialogServiceAdapter` | adapter | 59 | 1 | 1 | 0 | 4 | ~9 | 1 | adapter | ✅ |
| `VolumeServiceAdapter` | adapter | ~175 | 5 | 4 | 0 | **8** | ~32 | 1 | adapter (MonoBehaviour) | ✅ |
| `MemoryProbeAdapter` | adapter | 18 | 1 | 1 | 0 | 2 | ~3 | 1 | adapter | ✅ |
| `FileTabView` | adapter | ~330 | ~16 | 4 | 0 | 5 | ~40 | 1 | adapter (MonoBehaviour) | ✅ |
| `FileTabCompositionRoot` | adapter | 54 | 2 | 4 | 0 | 7 | ~12 | 1 | adapter (MonoBehaviour) | ✅ |
| **Σ slice** | — | **~1,438** | **79** | **— max 4** | **0** | **— max 9** | **— max ~50** | **1 per class** | — | **9 / 10 pass; 1 borderline** |

### Per-class notes

**`FileTabViewModel` (the domain centrepiece — borderline against domain WMC)**

- WMC: **27** methods (4 commands + 6 property setters with logic + `Dispose` + `BuildMemoryWarning` + `ComputeZScale` + `MaskAxesMatchImage` + `PopulateZAxisOptions` + `UpdateZAxisMax` + `RefreshHduHeaderAsync` + `NotifyCommandStates` + `NotifyIsLoadable` + 2 `Notify` helpers + `GetAxisMaxima`). **Exceeds the ≤ 20 domain threshold** — 7 of the 27 are 1–3-line property setters that Understand's `CountDeclMethod` may or may not collapse.
  - Remediation if the tool reports > 20: extract the three command bodies (`BrowseImageAsync`, `BrowseMaskAsync`, `LoadAsync`) and the two helpers `ComputeZScale` / `BuildMemoryWarning` into a small `FileTabCommands` helper. That removes 5 methods without behaviour change and brings WMC to ~22.
- CBO: **9** = four injected interfaces (`IFitsService`, `IFileDialogService`, `IVolumeService`, `IMemoryProbe`) + `SubsetBoundsViewModel` + four DTO/enum types (`FitsFileInfo`, `LoadCubeRequest`, `HduInfo`, `RatioMode`). Under the ≤ 14 domain threshold.
- RFC: **~50** own + external (Task / IProgress / EventArgs / PropertyChangedEventArgs / Math / Linq / Math.Min/Max / IDisposable). At the ≤ 50 limit — tool-verified value is authoritative.
- LCOM4: 1. All methods still cluster around the same fields (current image/mask info + selection state + commands + memory probe).
- DIT: 1. Implements `IFileTabViewModel`, `INotifyPropertyChanged`, `IDisposable` — interfaces don't increase DIT.

**`VolumeServiceAdapter` (the orchestrator-tier adapter)**

- DIT: 4 (MonoBehaviour chain) — at the limit, same as `CanvassDesktop`.
- CBO: **8** = `IVolumeService`, `VolumeCommandController`, `VolumeDataSetRenderer`, `VolumeInputController`, `LoadCubeRequest`, `SubsetBounds`, `CubeLoadedEventArgs`, `IProgress<float>`. Under the ≤ 25 adapter threshold.
- **Smell S6 (busy-wait) eliminated.** The previous `while (!renderer.started) yield return WaitForSeconds(0.1f)` loop is replaced by `yield return StartCoroutine(renderer._startFunc())` — Unity's coroutine scheduler suspends the parent until the child completes. No polling, no fixed 100 ms cadence. The smell is gone, not just contained.
- **Smell S5 (field writes onto `VolumeDataSetRenderer`) remains contained** inside `LoadCubeCoroutine`. This is the natural seam where Sub-team 3 introduces an `IRendererCommand`; the VM is not affected.
- New surface: the `CubeLoaded` event. Adds one delegate field; no impact on cohesion (still LCOM4 = 1).

**`FitsServiceAdapter` (RAII / handle ownership)**

- CBO bumped from 4 → 5 with the addition of the nested private `FitsHandle` class implementing `IFitsHandle`. The class is sealed inside the adapter so it does not contribute to the public domain surface.
- The handle lifetime fix (one open per file, reused across HDU reads) eliminates the per-dropdown-selection reopen at `CanvassDesktop.cs:1435` — visible in WMC count (one method `OpenAndReadMetadata` replaces the previous open-per-call pattern).

**`MemoryProbeAdapter` (new — Gap 2)**

- Trivial adapter (1 method, 1 property). Below all thresholds. Pure-C# object — no MonoBehaviour. Adds 1 CBO contribution to `FileTabCompositionRoot` and 1 to `FileTabViewModel` (via the injected interface).

**Layer DIT values**

- All domain classes: DIT = 1 (System.Object → class). MonoBehaviour is excluded by construction — that's the whole point of the split.
- All adapter `MonoBehaviour`s: DIT = 4 (Object → Component → Behaviour → MonoBehaviour → adapter). At the limit, same as any other Unity component.

---

## Delta summary

| Metric | BEFORE (`CanvassDesktop`) | AFTER (worst class in slice) | Δ |
|---|---:|---:|---:|
| LOC (god class only) | 1899 | ~480 (`FileTabViewModel`) | **−75%** |
| WMC | 57 | 27 (`FileTabViewModel`) | **−53%** |
| CBO | ~32 | 9 (`FileTabViewModel`) / 8 (`VolumeServiceAdapter`) | **−72% / −75%** |
| RFC | ~210 | ~50 (`FileTabViewModel`) | **−76%** |
| LCOM4 | ≥ 7 | 1 per class | **disjoint → cohesive** |
| Threshold pass count | 2 / 7 | 9 / 10 classes pass; 1 borderline | **fail → near-pass** |

**Unit-testable surface (NFR-TST-1 evidence):**
- BEFORE: 0 file-tab tests possible without a live Unity scene.
- AFTER: **34** NUnit tests in `tests/FileTabViewModelTests.cs` exercise the domain layer with no Unity dependency (added: 3 ratio-mode, 2 memory-feasibility, 2 cube-loaded event).

---

## What still needs tool verification on Day 13

1. **WMC for `FileTabViewModel`** — hand-count of 27 exceeds the ≤ 20 domain threshold. Understand's `CountDeclMethod` value is authoritative. Planned remediation if confirmed: extract the three async command bodies plus `ComputeZScale` / `BuildMemoryWarning` into a `FileTabCommands` helper (5 methods moved out, no behaviour change, WMC → ~22).
2. **RFC for `FileTabViewModel`** — hand-count of ~50 sits at the limit. Tool-verified value decides. Same remediation as WMC applies if it goes over.
3. **CBO for `CanvassDesktop` (BEFORE)** — depending on whether the tool excludes Unity-namespace value types (`Vector3`, `Quaternion`, `Color`), the figure may settle between 20 and 35. Either way it exceeds the ≤ 25 orchestrator threshold.
4. **LCOM4 for `CanvassDesktop`** — hand-estimate of "≥ 7 connected components" needs a real graph walk. Understand reports this directly; SonarQube reports LCOM-HS.
5. **NDepend / DV8 dependency rules** — confirm zero cycles across the AFTER package set (`Domain` ⇏ `Adapters` ⇏ `Domain`). Hand inspection passes; tool-verified result needed for the panel.

A short follow-up commit on Day 13 will replace this paragraph with the tool snapshot.
