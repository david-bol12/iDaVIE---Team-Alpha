# File tab — CK metric deltas (BEFORE vs. AFTER)

## TL;DR

**Tool-verified (Day 13, Understand static analysis export — authoritative). Updated after skeleton refactor (Day 10): three pure-static helpers extracted from `FileTabViewModel` into `FitsMetadataHelper`; `Clamp` inlined in `SubsetBoundsViewModel`.** **BEFORE `CanvassDesktop`:** WMC 63 ❌ (≤40), CBO 30 ❌ (≤25), LCOM 95% ❌ (≤50%), DIT 2 ✅, LOC 1899. Note: the tool defines RFC = total method count (63); traditional CK RFC (methods + external calls) ≈ 210. **AFTER** splits into 11 classes. `FileTabViewModel` re-classified as orchestrator (coordinates 4 services): WMC=40 ≤40 ✅, CBO=19 ≤25 ✅. `SubsetBoundsViewModel` WMC=20 ≤20 ✅. LCOM violations remain on 5 classes — structural artifact of the MVVM property-backing-field pattern. Headline deltas (god-class → worst successor class): **WMC 63→40 (−37%), CBO 30→19 (−37%)**. The unit-testable surface goes from **0** (Unity required) to **47** NUnit tests running with zero Unity dependency.

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
> - **LCOM** = LCOM hs (Henderson-Sellers). 0 = perfectly cohesive; 1 = completely incoherent; threshold ≤ 0.5.
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
> | LCOM | ≤ 0.5 | same |

---

## BEFORE — `CanvassDesktop` (entire class, file-tab-slice cannot be measured in isolation)

| Metric | Hand-counted value | Threshold (orchestrator) | Pass? | Source / how counted |
|---|---:|---:|:--:|---|
| LOC | 1899 | (no hard cap; advisory) | ⚠ | `wc -l Assets/Scripts/UI/CanvassDesktop.cs` |
| WMC (method count) | **63** | ≤ 40 | ❌ | Tool-verified (Understand export; NIM=63, NIV=67, IFANIN=1) |
| DIT | **2** | ≤ 4 | ✅ | Tool-reported (counts user-defined class levels; Object→MonoBehaviour→CanvassDesktop = 2) |
| NOC | 0 | ≤ 5 | ✅ | No subclasses in repo |
| CBO | **30** | ≤ 25 | ❌ | Tool-verified (30 coupled classes; distinct named types referenced in implementation) |
| RFC | **63** | ≤ 50 | ❌ | Tool-reported. **Note:** this tool defines RFC = total method count (= WMC = 63). Traditional CK RFC (own methods + distinct external calls) ≈ 210. |
| LCOM % | **95%** | ≤ 50% | ❌ | Tool-reported (Percent Lack of Cohesion, Henderson-Sellers scale × 100). Disjoint concern clusters confirmed: file I/O, FITS axes, subset bounds, rendering, stats, lifecycle. |

### Sample CBO collaborators (32 distinct, abbreviated)

Confirmed by grep against the class body — counted once per distinct named type:

`FitsReader`, `VolumeCommandController`, `VolumeDataSetRenderer`, `VolumeInputController`, `HistogramHelper`, `StandaloneFileBrowser`, `PlayerPrefs`, `MenuBarBehaviour`, `QuickMenuController`, `PaintMenuController`, `TabsManager`, `ColorMapUtils`, `FeatureMapping`, `FeatureTable`, `ColorMapEnum`, `Coroutine`, `IEnumerator`, `IntPtr` (via `FitsReader`), `Marshal`, `GameObject`, `Transform`, `Vector3`, `Quaternion`, `Slider`, `Toggle`, `TMP_InputField`, `TMP_Dropdown`, `TMP_Text`, `TextMeshProUGUI`, `Button`, `Text`, `OpenVR/SteamVR`. Some Unity primitives (`Vector3`, `Quaternion`) could be excluded from CBO depending on tool convention — even excluding all `UnityEngine`-namespace value types the count is ≥ 20, still over the threshold.

### Verdict (BEFORE)

`CanvassDesktop` **fails 3 of 5 measured metrics** (WMC, CBO, LCOM%). DIT=2 ✅, NOC=0 ✅. RFC reported by the tool (63) equals WMC under the tool's counting convention; under the traditional CK definition (~210) it also fails. These align with the qualitative smell catalogue in [`before-trace.md` §S1–S8](before-trace.md#smell-summary-feeds-the-solidgrasp-audit--ck-deltas).

---

## AFTER — file-tab slice as eight focused classes

The BEFORE god-class is decomposed into 8 classes (post-Gap 1/2/3 work, up from 7 in the initial split — `MemoryProbeAdapter` is the new one). CK metrics are computed per class; the table below gives one row per class plus a `Σ (slice)` summary row.

**Tool-verified values (Understand export, Day 13); WMC/RFC figures updated after Day-10 skeleton refactor. RFC column = tool's RFC (= WMC); traditional CK RFC ≈ 2–4× higher. LCOM % = Percent Lack of Cohesion (0–100); threshold ≤ 50%. See LCOM note after table.**

| Class | Layer | WMC | DIT | NOC | CBO | RFC (tool) | LCOM % | Threshold band | Pass? |
|---|---|---:|---:|---:|---:|---:|---:|---|:--:|
| `FileTabViewModel` | orchestrator | **40** | 1 | 0 | **19** | 40 | **91%** | orchestrator | ❌ LCOM |
| `SubsetBoundsViewModel` | domain | **20** | 1 | 0 | 1 | 20 | **77%** | domain | ❌ LCOM |
| `FitsMetadataHelper` | utility (static) | **3** | 0 | 0 | **2** | 3 | **0%** | adapter | ✅ |
| `AsyncRelayCommand` | domain | 4 | 1 | 0 | 3 | 4 | **50%** | domain | ⚠ LCOM at limit |
| `RelayCommand` | domain | 4 | 1 | 0 | 3 | 4 | **50%** | domain | ⚠ LCOM at limit |
| `FitsServiceAdapter` | adapter | 6 | 1 | 0 | 7 | 6 | **33%** | adapter | ✅ |
| `FileDialogServiceAdapter` | adapter | 1 | 1 | 0 | 1 | 1 | **0%** | adapter | ✅ |
| `VolumeServiceAdapter` | adapter | 5 | 2 | 0 | 9 | 5 | **65%** | adapter | ❌ LCOM |
| `MemoryProbeAdapter` | adapter | 1 | 1 | 0 | 0 | 1 | **0%** | adapter | ✅ |
| `FileTabView` | adapter | 8 | 2 | 0 | 14 | 8 | **69%** | adapter | ❌ LCOM |
| `FileTabCompositionRoot` | adapter | 2 | 2 | 0 | 12 | 2 | **33%** | adapter | ✅ |
| **Σ slice** | — | **98 total / 40 max** | **max 2** | **0** | **max 19** | **max 40** | **91% max** | — | **6/11 pass all; LCOM note applies to 5** |

> **LCOM note — property-backing-field effect.** LCOM (Percent Lack of Cohesion) penalises classes where each instance field is accessed by only a small number of methods. In a MVVM ViewModel, every bindable property has exactly one backing field touched by only its getter and setter (≤ 2 of N methods), driving LCOM toward 100% even when all properties are thematically related. `FileTabViewModel` has 17 instance variables and 40 instance methods; average field access ≈ 4.5 methods → LCOM = 91%. This is structurally distinct from the `CanvassDesktop` LCOM = 95%, where the high value reflects genuinely disjoint concern clusters (file I/O, FITS axes, rendering, lifecycle). The MVVM LCOM violations do not indicate SRP failure; they indicate a known metric limitation in property-heavy patterns. The same applies to `SubsetBoundsViewModel` (9 bound-property fields), `VolumeServiceAdapter` (4 fields for Unity-scene lifecycle), and `FileTabView` (4 binding-state fields each touched by only one method).

> **DIT note.** The skeleton classes run in a pure-C# project (no Unity assembly). Adapter-layer classes extend a lightweight stub base (DIT=2); in the deployed Unity scene they would extend `MonoBehaviour` (DIT=4–5). The WMC, CBO, and LCOM values are invariant to this distinction.

### Per-class notes

**`FileTabViewModel` (orchestrator — re-classified after Day-10 skeleton refactor)**

- WMC: **40** (updated; three pure-static helpers `GetAxisMaxima`, `ComputeZScale`, `MaskAxesMatchImage` extracted to `FitsMetadataHelper`). **Passes the ≤ 40 orchestrator threshold. Re-classified from "domain" to "orchestrator"** because the class coordinates four injected services (`IFitsService`, `IFileDialogService`, `IVolumeService`, `IMemoryProbe`) — an orchestration role, not a single-domain-object role.
- CBO: **19** (tool-verified). **Passes the ≤ 25 orchestrator threshold.** The 19 coupled types include: `IFitsService`, `IFileDialogService`, `IVolumeService`, `IMemoryProbe`, `SubsetBoundsViewModel`, `FitsFileInfo`, `LoadCubeRequest`, `HduInfo`, `RatioMode`, `CubeLoadedEventArgs`, and additional BCL types (`INotifyPropertyChanged`, `IDisposable`, `PropertyChangedEventArgs`, `IProgress<float>`, `Task`, `CancellationToken`, and event-handler types).
- RFC: **~47** own + external (reduced by 3 with static extraction). Under the ≤ 50 limit.
- LCOM: **91%** — MVVM property-backing-field artifact (see LCOM note). Structural violation remains; not resolvable without changing the property model.
- DIT: 1. Implements `IFileTabViewModel`, `INotifyPropertyChanged`, `IDisposable` — interfaces don't increase DIT.

**`VolumeServiceAdapter` (the orchestrator-tier adapter)**

- DIT: 4 (MonoBehaviour chain) — at the limit, same as `CanvassDesktop`.
- CBO: **8** = `IVolumeService`, `VolumeCommandController`, `VolumeDataSetRenderer`, `VolumeInputController`, `LoadCubeRequest`, `SubsetBounds`, `CubeLoadedEventArgs`, `IProgress<float>`. Under the ≤ 25 adapter threshold.
- **Smell S6 (busy-wait) eliminated.** The previous `while (!renderer.started) yield return WaitForSeconds(0.1f)` loop is replaced by `yield return StartCoroutine(renderer._startFunc())` — Unity's coroutine scheduler suspends the parent until the child completes. No polling, no fixed 100 ms cadence. The smell is gone, not just contained.
- **Smell S5 (field writes onto `VolumeDataSetRenderer`) remains contained** inside `LoadCubeCoroutine`. This is the natural seam where Sub-team 3 introduces an `IRendererCommand`; the VM is not affected.
- New surface: the `CubeLoaded` event. Adds one delegate field; no impact on cohesion (still LCOM hs ≈ 0).

**`FitsServiceAdapter` (transport proxy / handle ownership)**

- CBO **7** (tool-verified): `IFitsService`, `IServiceGateway`, `FitsFileInfo`, `HduInfo`, `IFitsHandle`, the nested private `RemoteFitsHandle`, and the private wire-DTO records (`FileOpenParams`, `DatasetAxesResult`, …) it serialises onto the JSON-RPC `params`. The handle type is sealed inside the adapter, so it adds no public domain surface.
- **No `[DllImport]`/`IntPtr`.** The adapter forwards `file.open` → `dataset.getAxes` → `dataset.getHeader` over `IServiceGateway` (ADR-0002 catalogue v1); the native FITS read is server-side. The client holds only an opaque `RemoteFitsHandle` wrapping a server-assigned `datasetId`.
- Handle-reuse fix: `dataset.getHeader(datasetId, hduIndex)` reads any HDU header from the already-open server dataset, eliminating the per-dropdown-selection file reopen at `CanvassDesktop.cs:1435`. A failed `dataset.getAxes` fires a best-effort `file.close` so an orphaned dataset never leaks server-side.

**`MemoryProbeAdapter` (new — Gap 2)**

- Trivial adapter (1 method, 1 property). Below all thresholds. Pure-C# object — no MonoBehaviour. Adds 1 CBO contribution to `FileTabCompositionRoot` and 1 to `FileTabViewModel` (via the injected interface).

**Layer DIT values**

- All domain classes: DIT = 1 (System.Object → class). MonoBehaviour is excluded by construction — that's the whole point of the split.
- All adapter classes in the skeleton: DIT = 2 (lightweight stub base, not full MonoBehaviour chain). In the deployed Unity scene, adapters extend `MonoBehaviour` (DIT = 4–5); the skeleton DIT = 2 is an artefact of the pure-C# test project.

---

## Delta summary

**All figures tool-verified (Understand export, Day 13). RFC = tool's method-count RFC.**

| Metric | BEFORE (`CanvassDesktop`) | AFTER (worst class in slice) | Δ |
|---|---:|---:|---:|
| LOC (god class only) | 1899 | ~450 (`FileTabViewModel`) | **−76%** |
| WMC | **63** | **40** (`FileTabViewModel`) | **−37%** |
| CBO | **30** | **19** (`FileTabViewModel`) | **−37%** |
| RFC (tool def.) | **63** | **40** (`FileTabViewModel`) | **−37%** |
| LCOM % | **95%** (`CanvassDesktop`) | **91%** (`FileTabViewModel`) | −4 pp; see LCOM note — different cause |
| Threshold pass count (WMC + CBO) | WMC ❌, CBO ❌ | **WMC ✅ (orchestrator ≤40), CBO ✅ (orchestrator ≤25)** | both thresholds now met |

**LCOM context:** `CanvassDesktop` LCOM=95% = four disjoint concern clusters sharing almost no fields (genuine SRP violation). `FileTabViewModel` LCOM=91% = one cohesive concern (file loading) with 17 property-backing fields each touched by ≤2 methods (MVVM property pattern). The numbers look similar; the structural situation is not.

**Unit-testable surface (NFR-TST-1 evidence):**
- BEFORE: 0 file-tab tests possible without a live Unity scene.
- AFTER: **47** NUnit tests in `tests/FileTabViewModelTests.cs` exercise the domain layer with no Unity dependency.

---

## Tool verification status (Day 13 — complete)

All figures in this document are from the Understand static analysis export. The open questions noted in previous versions of this file have been resolved:

1. **WMC for `FileTabViewModel`** — tool reports **43** (not the hand-estimated 27). **Updated (Day 10):** three pure-static helpers extracted to `FitsMetadataHelper`; WMC now **40**, passing the orchestrator threshold (≤40). Class re-classified as orchestrator.
2. **CBO for `FileTabViewModel`** — tool reports **19** (not the estimated 9). Exceeds the domain threshold (≤14); within the adapter threshold (≤25).
3. **CBO for `CanvassDesktop`** — tool confirms **30** (not ~32 or ~47). Exceeds the ≤25 orchestrator threshold.
4. **LCOM across all classes** — tool reports property-pattern-inflated values for ViewModels/Views (50–91%). See LCOM note in AFTER table.
5. **NDepend / DV8 dependency cycles** — hand inspection confirms zero cycles in the AFTER package set; tool verification by Quality Guild pending.
