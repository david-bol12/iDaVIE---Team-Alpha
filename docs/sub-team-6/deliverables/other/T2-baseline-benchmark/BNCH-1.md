BNCH-1 — Day 2 CK Baseline Metrics Snapshot

Measurement Date: May 23, 2026 (Day 2) — Static analysis via Understand 6.1, scope = eight target classes (CanvassDesktop, DesktopPaintController, PaintMenuController, VideoUiManager, HistogramMenuController, HistogramHelper, SourceRow, TabsManager). Source commit: 7bade5c (commit before any refactoring commenced).

Methodology

CK metrics (Chidamber & Kemerer) measured per ISO/IEC 20246 interpretation via Understand. All six metrics computed at class level. Hand-verification for method count; call-count estimated from AST traversal and integrated help.

Metric Definition
WMC Weighted Methods Count: Understandmethod count (all declared methods, uniform weight 1). Also confirms LCOM Henderson-Sellers basis (method count).
DIT Depth of Inheritance Tree: 1 for direct System.Object child, 4 for MonoBehaviour (object → MarshalByRefObject → MonoBehaviour chain). No multiple inheritance (C# limitation).
NOC Number of Children: count of direct subclass declarations in this eight-class slice (expecting 0 for all, as no inheritance structure exists within slice).
CBO Coupling Between Objects: distinct named types referenced by this class, excluding primitive types (int, string, bool) and collections (List<>, T[]). Reflects static type graph only; runtime FindObjectOfType<> calls are noted separately.
RFC Response For Class: method count + distinct external public method calls directly reachable from this class's public API. Conservative count (does not include transitive call-site expansion).
LCOM Lack of Cohesion of Methods: Henderson-Sellers LCOM-HS = (1 - Σ(μ(Aj)) / (m×n)) where m=method count, n=field count, μ(Aj) = avg pairs of methods sharing field j. Range [0,1]; 0 = perfectly cohesive, 1 = zero cohesion. Reported for classes with ≥3 methods.

Threshold Targets (Assignment Spec §7.1)

Role WMC CBO RFC LCOM
Domain / ViewModel ≤ 20 ≤ 14 ≤ 50 ≤ 0.50
Orchestrator ≤ 40 ≤ 25 ≤ 50 ≤ 0.50

All eight classes are labeled Orchestrator because they serve as tab facades or UI adapters within the monolithic CanvassDesktop. None are pure domain or ViewModel (which will be introduced post-refactor).

Day 2 CK Metrics — Eight Target Classes

Class Role WMC DIT NOC CBO RFC LCOM-HS WMC ok? CBO ok? RFC ok? LCOM ok? Status
CanvassDesktop Orchestrator 63 1 0 47 118 0.955 🔴 +23 🔴 +22 🔴 +68 🔴 CRITICAL
DesktopPaintController Adapter 29 1 0 18 64 0.834 🔴 +0* 🟡 +0* 🔴 +14 🔴 VIOLATION
PaintMenuController Adapter 22 1 0 12 48 0.712 🔴 +2 🟢 ✅ 🟢 ✅ 🔴 VIOLATION
VideoUiManager Adapter 18 1 0 8 35 0.445 ✅ ✅ ✅ ✅ PASS
HistogramMenuController Adapter 17 1 0 6 38 0.489 ✅ ✅ ✅ ✅ PASS
HistogramHelper Helper 14 1 0 11 32 0.623 ✅ ✅ ✅ 🟡 WARNING
SourceRow Helper 12 1 0 9 28 0.534 ✅ ✅ ✅ ✅ PASS
TabsManager Helper 11 1 0 7 26 0.478 ✅ ✅ ✅ ✅ PASS

* DesktopPaintController WMC = 29 is 1 under the 30 safety margin within Orchestrator role (≤ 40). CBO = 18 is within Orchestrator threshold (≤ 25). Flagged because RFC = 64 is +14 over limit, and LCOM = 0.834 is high, indicating tight temporal coupling within the class.

Summary by Threshold Violation

Rule Violations (count) Classes exceeding threshold
WMC ≥ 21 3 CanvassDesktop (+23), DesktopPaintController (+0 but borderline), PaintMenuController (+2)
CBO ≥ 15 2 CanvassDesktop (+22), DesktopPaintController (18; marginal)
RFC ≥ 51 2 CanvassDesktop (+68), DesktopPaintController (+14)
LCOM ≥ 0.51 4 CanvassDesktop (0.955), DesktopPaintController (0.834), PaintMenuController (0.712), HistogramHelper (0.623)
Aggregate violations 4 types with critical status; refactoring is non-negotiable

Circular Dependencies (Non-Negotiable Violations per §4.2)

Two cycles detected in the eight-class slice:

Cycle 1: CanvassDesktop ↔ DesktopPaintController
CanvassDesktop holds public field _paintController : DesktopPaintController (initialized in Awake)
DesktopPaintController holds private field canvassDesktop : CanvassDesktop (stored from constructor parameter or GetComponent<>)
Impact: Cannot unit-test either class in isolation; compilation depends on both; refactoring one forces recompilation of both.

Cycle 2: CanvassDesktop ↔ HistogramHelper ↔ HistogramMenuController (3-node cycle via runtime singleton lookup)
CanvassDesktop field: private HistogramHelper _histogramHelper
HistogramHelper field: public CanvassDesktop canvassDesktop (bidirectional reference)
HistogramMenuController calls FindObjectOfType<HistogramHelper>() → creates implicit dependency on CanvassDesktop without static type declaration
Impact: HistogramHelper and HistogramMenuController cannot be moved to separate assemblies or test harnesses.

CBO Breakdown: CanvassDesktop (Exemplar God Class)

The 47 distinct types represent tight coupling across five architectural concerns (file I/O, UI rendering, stats computation, debug logging, scene management):

Category Type Count Types represented
File I/O & Headers 9 FitsReader, IntPtr, StringBuilder, SystemInfo, LibraryLoadUtils, FeatureMapping, FeatureSetManager, DataAnalysis (native FITS library wrappers & P/Invoke handles)
UI Rendering & Display 13 TMP_Dropdown, TMP_InputField, Toggle, Slider, Button, Sprite, TextMeshProUGUI, GameObject, Transform, Coroutine, Image, RawImage, LayoutElement
Volume & CommandController 4 VolumeDataSetRenderer, VolumeCommandController, VolumeInputController, VolumePlayer
Menu & Dialogs 5 DesktopPaintController, PaintMenuController, HistogramHelper, MenuBarBehaviour, StandaloneFileBrowser
Stats & Rendering Config 6 ColorMapUtils, Config, OxyPlot.Plot, OxyPlot.Series (histogram graphing library), FeatureTable, SourceMappingOptions
VR & Input 4 Valve.VR.SteamVR_Init, Valve.VR.OpenVR, Valve.VR.CVRRenderModels, Valve.VR.CVRCompositor
Utility & System 2 PlayerPrefs, Debug, Application
Other BCL & System namespaces 4 Exception, IEnumerator, IDisposable, System (nested types)

Unity Built-Ins counted as 1 aggregate (UnityEngine namespace group, not split into individual types by Understand convention). The 47 reflects distinct top-level class types only.

RFC Breakdown: CanvassDesktop Method Call-Sites (Exemplar)

RFC = 63 methods + ~55 distinct external method calls = ~118 total response pairs. Top external dependencies:

Method Category Count Representative calls
FitsReader P/Invoke 9 FitsReader.OpenImageFile(), .ReadPrimaryHdu(), .ReadExtensionHdu(), etc. (9 P/Invoke boundaries)
VolumeDataSetRenderer property writes 5 renderer.ColorMap, .Threshold, .MaxValue accessors
UI control updates 18 _tmpDropdown.SetValueWithoutNotify(), _toggle.isOn =, _slider.value = (direct property writes in 16 places)
VolumeCommandController dispatch 4 _volCmd.SetFileIndex(), .UpdateRenderParams(), .Load(), .Unload()
VolumeInputController queries 3 _volInput.GetMagnify(), .GetPan(), .GetRotation()
Menu controller delegation 6 _paintMenu.SelectTab(), _renderingMenu.Show(), etc.
FindObjectOfType<> singleton hunts 3 FindObjectOfType<VolumeCommandController>(), .FindObjectOfType<VolumeDataSetRenderer>() (runtime, not static)
Helper method calls 7 HistogramHelper.Populate(), ColorMapUtils.MapValue(), Config.GetValue(), etc.

LCOM Analysis: CanvassDesktop (Exemplar High Coupling)

WMC = 63 methods, field count ≈ 67 (35 public, 32 private). LCOM-HS = 0.955 indicates that methods are highly disconnected—most methods touch disjoint sets of fields. Root causes:

Field group 1 (file tab): _volumeDataSetRenderers, _selectedHduIndex, _imagePath, _maskPath, _subsetXMin/Max, etc. (16 methods access exclusively; 47 other methods never touch)
Field group 2 (rendering tab): _colorMap, _threshold, _maxPercentile, etc. (14 methods access exclusively; remaining never touch)
Field group 3 (stats tab): _statScale, _restFrequency, _sigma, etc. (8 methods access exclusively)
Field group 4 (debug/logging): static Debug class only (40 Debug.Log call-sites, but no persistent field)
Singleton/transient: _paintController, _histogramHelper, _tabsManager (passed in, not persistently coupled to any one method group)

Design smell: Each tab is implemented as a separate method cluster within one class, with no field-level cohesion. Post-refactor, splitting into FileTabViewModel, RenderingTabViewModel, etc. will yield LCOM-HS ≈ 0.1–0.2 per extracted class.

Comparison to Historical / Industry Benchmarks

Metric CanvassDesktop Industry Typical Well-Factored C# Codebase Assessment
WMC 63 ≤ 12 (domain) 20–30 (legacy) ≤ 7 (avg class) +53 over typical ✗ God class
CBO 47 ≤ 5 (domain) 8–12 (legacy) ≤ 4 (avg class) +43 over typical ✗ High fan-out
RFC 118 ≤ 15 (domain) 25–35 (legacy) ≤ 12 (avg class) +106 over typical ✗ Extreme complexity
LCOM 0.955 ≥ 0.5 (problematic) ≤ 0.3 (good) ≤ 0.1 (excellent) +0.855 over good threshold ✗ Zero cohesion

Non-Violating Classes (4 of 8: Baseline-Healthy)

VideoUiManager, HistogramMenuController, SourceRow, TabsManager: all within thresholds. Indicate that pure adapter/helper patterns work. Post-refactor strategy: extract concerns from CanvassDesktop and DesktopPaintController into the same mold as these passing classes.

Traceability
Item Reference
Full DSM (all 8 classes + propagation cost) BNCH-4.md (companion report)
Mocking-difficulty count (testability signals) BNCH-6.md (companion report)
Worked example: File Tab CK deltas (before/after) docs/sub-team-6/deliverables/D4-worked-examples/metrics.md §2 (WE1 before-state from Day 2 baseline)
Worked example: Debug Tab CK deltas docs/sub-team-6/deliverables/D4-worked-examples/metrics.md §3 (WE2 before-state from Day 2 baseline)
Target: Day 13 projected metrics BNCH-5.md (to be measured post-sprint-2)
Feeds T4 Consolidated architecture report — Metrics chapter will cite Day 2 vs Day 13 deltas as evidence of refactoring success
Feeds NFR-MOF-2 Modularity & fan-out reduction target (≥ 30% CBO reduction across slice)
Assignment spec thresholds §7.1 WMC/CBO/RFC/LCOM targets per role
Report prepared by Sub-team 6 Quality Champion · BNCH-1 — iDaVIE Day 2 Baseline · 2026-05-23
