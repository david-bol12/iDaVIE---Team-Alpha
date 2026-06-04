# iDaVIE Codebase Metrics Report

**Project:** iDaVIE (Immersive Data Visualisation in Astrophysics)
**Analysis Date:** 2026-06-04
**Platform:** Unity C# (Windows/VR)

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Directory Structure](#2-directory-structure)
3. [CK Metrics — Per-Class Summary](#3-ck-metrics--per-class-summary)
4. [Metric Definitions](#4-metric-definitions)
5. [Inheritance Hierarchies](#5-inheritance-hierarchies)
6. [Coupling Analysis (CBO)](#6-coupling-analysis-cbo)
7. [Largest Classes (WMC / LOC)](#7-largest-classes-wmc--loc)
8. [Cohesion Analysis (LCOM)](#8-cohesion-analysis-lcom)
9. [Architectural Layers](#9-architectural-layers)
10. [Quality Observations](#10-quality-observations)
11. [Summary Statistics](#11-summary-statistics)

---

## 1. Project Overview

| Property | Value |
|----------|-------|
| Total C# source files | 104 |
| Estimated total LOC | ~31,500 |
| Shader files | 8 |
| JSON config/data files | 23 |
| Meta files (Unity) | 355 |
| Total identified classes | 180+ |
| Average class size | ~175 LOC |
| Max class size | 1,920 LOC (`VolumeDataSet`) |

---

## 2. Directory Structure

```
Assets/Scripts/
├── CatalogData/        — Catalog data management and rendering
├── Debuggers/          — Debug/diagnostic utilities
├── Editor/             — Unity Editor-only scripts
├── FeatureData/        — Feature set management and rendering
├── LineRenderer/       — Custom world-space line rendering
├── Menu/               — Menu controllers and helpers
├── PluginInterface/    — P/Invoke wrappers for native C++ plugins
├── Shapes/             — Shape creation and management
├── Tools/              — General-purpose utilities
├── UI/
│   └── Menus/          — Individual menu panel controllers
├── VideoMaker/         — Video recording and playback
├── VoiceCommands/      — Voice command recognition
├── VolumeData/         — Core volume data structures and rendering
└── VRKeyboard/         — VR on-screen keyboard hierarchy

Assets/Editor/          — Main editor integration script
Assets/Unity-UI-Rounded-Corners/ — Third-party UI library
```

---

## 3. CK Metrics — Per-Class Summary

The six Chidamber & Kemerer (CK) metrics are: **WMC**, **DIT**, **NOC**, **CBO**, **RFC**, **LCOM**.
See [Section 4](#4-metric-definitions) for definitions.

> **Note:** Figures are derived from static analysis of source files and are approximations.
> Automated tool output should be preferred for precise values in production audits.

### Core Volume Data

| Class | File | LOC | WMC | DIT | NOC | CBO | RFC | LCOM |
|-------|------|-----|-----|-----|-----|-----|-----|------|
| `VolumeDataSet` | VolumeData/VolumeDataSet.cs | 1920 | 330 | 0 | 0 | 15 | 80 | High |
| `VolumeDataSetRenderer` | VolumeData/VolumeDataSetRenderer.cs | 1402 | 297 | 1 | 0 | 8 | 60 | High |
| `VolumeInputController` | VolumeData/VolumeInputController.cs | 1634 | 192 | 1 | 0 | 10 | 65 | High |
| `VolumeCommandController` | VolumeData/VolumeCommandController.cs | 685 | 108 | 1 | 0 | 7 | 45 | Moderate |
| `MaskDataSet` | VolumeData/MaskDataSet.cs | ~300 | 35 | 0 | 0 | 5 | 28 | Moderate |
| `MomentMapRenderer` | VolumeData/MomentMapRenderer.cs | ~350 | 40 | 1 | 0 | 6 | 32 | Moderate |

### Catalog Data

| Class | File | LOC | WMC | DIT | NOC | CBO | RFC | LCOM |
|-------|------|-----|-----|-----|-----|-----|-----|------|
| `CatalogDataSet` | CatalogData/CatalogDataSet.cs | ~400 | 55 | 0 | 0 | 6 | 40 | Moderate |
| `CatalogDataSetRenderer` | CatalogData/CatalogDataSetRenderer.cs | 694 | 145 | 1 | 0 | 8 | 55 | High |
| `CatalogInputController` | CatalogData/CatalogInputController.cs | ~450 | 60 | 1 | 0 | 7 | 42 | Moderate |
| `DataMapping` | CatalogData/DataMapping.cs | ~280 | 30 | 0 | 0 | 4 | 22 | Low |
| `SourceRow` | CatalogData/SourceRow.cs | ~200 | 22 | 0 | 0 | 3 | 18 | Low |
| `ColumnInfo` | CatalogData/ColumnInfo.cs | ~80 | 8 | 0 | 0 | 1 | 8 | Low |

### Feature Data

| Class | File | LOC | WMC | DIT | NOC | CBO | RFC | LCOM |
|-------|------|-----|-----|-----|-----|-----|-----|------|
| `Feature` | FeatureData/Feature.cs | ~150 | 18 | 0 | 0 | 2 | 15 | Low |
| `FeatureSetManager` | FeatureData/FeatureSetManager.cs | ~400 | 55 | 1 | 0 | 8 | 42 | Moderate |
| `FeatureSetRenderer` | FeatureData/FeatureSetRenderer.cs | 616 | 77 | 1 | 0 | 7 | 48 | Moderate |

### UI / Menu

| Class | File | LOC | WMC | DIT | NOC | CBO | RFC | LCOM |
|-------|------|-----|-----|-----|-----|-----|-----|------|
| `CanvassDesktop` | Menu/CanvassDesktop.cs | 1899 | 340 | 1 | 0 | 12 | 70 | High |
| `DesktopPaintController` | UI/DesktopPaintController.cs | 1558 | 305 | 1 | 0 | 9 | 55 | High |
| `QuickMenuController` | UI/Menus/QuickMenuController.cs | ~300 | 35 | 1 | 0 | 5 | 28 | Moderate |
| `HistogramMenuController` | UI/Menus/HistogramMenuController.cs | ~350 | 42 | 1 | 0 | 6 | 32 | Moderate |
| `FeatureMenuController` | UI/Menus/FeatureMenuController.cs | ~280 | 30 | 1 | 0 | 5 | 24 | Moderate |
| `ToastNotification` | UI/ToastNotification.cs | ~120 | 12 | 1 | 0 | 2 | 10 | Low |
| `FeatureMenuCell` | UI/Menus/FeatureMenuCell.cs | ~100 | 10 | 1 | 0 | 2 | 8 | Low |
| `FeatureMenuDataSource` | UI/Menus/FeatureMenuDataSource.cs | ~120 | 12 | 0 | 0 | 3 | 10 | Low |

### Plugin Interface

| Class | File | LOC | WMC | DIT | NOC | CBO | RFC | LCOM |
|-------|------|-----|-----|-----|-----|-----|-----|------|
| `FitsReader` | PluginInterface/FitsReader.cs | 730 | 147 | 0 | 0 | 3 | 50 | Moderate |
| `DataAnalysis` | PluginInterface/DataAnalysis.cs | ~500 | 80 | 0 | 0 | 4 | 45 | Moderate |
| `AstTool` | PluginInterface/AstTool.cs | ~300 | 40 | 0 | 0 | 2 | 28 | Low |
| `NativePluginLoader` | PluginInterface/NativePluginLoader.cs | ~200 | 20 | 0 | 0 | 2 | 15 | Low |
| `ColorMapUtils` | PluginInterface/ColorMapUtils.cs | ~180 | 22 | 0 | 0 | 2 | 18 | Low |

### Shapes

| Class | File | LOC | WMC | DIT | NOC | CBO | RFC | LCOM |
|-------|------|-----|-----|-----|-----|-----|-----|------|
| `ShapesManager` | Shapes/ShapesManager.cs | ~500 | 169 | 1 | 0 | 8 | 55 | High |
| `Shape` | Shapes/Shape.cs | ~200 | 25 | 0 | 0 | 3 | 18 | Low |
| `ShapeAction` | Shapes/ShapeAction.cs | ~150 | 18 | 0 | 0 | 2 | 14 | Low |

### Video Maker

| Class | File | LOC | WMC | DIT | NOC | CBO | RFC | LCOM |
|-------|------|-----|-----|-----|-----|-----|-----|------|
| `VideoCameraController` | VideoMaker/VideoCameraController.cs | 605 | 74 | 1 | 0 | 7 | 42 | Moderate |
| `VideoUiManager` | VideoMaker/VideoUiManager.cs | ~350 | 45 | 1 | 0 | 5 | 30 | Moderate |
| `VideoScriptReader` | VideoMaker/VideoScriptReader.cs | ~250 | 28 | 0 | 0 | 3 | 20 | Low |
| `Path` (abstract) | VideoMaker/Path.cs | ~200 | 10 | 0 | 5 | 1 | 10 | Low |
| `Action` (abstract) | VideoMaker/Action.cs | ~300 | 12 | 0 | 8 | 2 | 12 | Low |
| `Easing` (abstract) | VideoMaker/Easing.cs | ~150 | 8 | 0 | 4 | 1 | 8 | Low |

### VR Keyboard

| Class | File | LOC | WMC | DIT | NOC | CBO | RFC | LCOM |
|-------|------|-----|-----|-----|-----|-----|-----|------|
| `Abstract_VR_button` | VRKeyboard/Abstract_VR_button.cs | ~80 | 6 | 1 | 5 | 1 | 6 | Low |
| `Char_VR_button` | VRKeyboard/Char_VR_button.cs | ~60 | 5 | 2 | 0 | 1 | 5 | Low |
| `Backspace_VR_button` | VRKeyboard/Backspace_VR_button.cs | ~50 | 4 | 2 | 0 | 1 | 4 | Low |
| `Shift_VR_button` | VRKeyboard/Shift_VR_button.cs | ~50 | 4 | 2 | 0 | 1 | 4 | Low |
| `Symbol_VR_button` | VRKeyboard/Symbol_VR_button.cs | ~50 | 4 | 2 | 0 | 1 | 4 | Low |
| `Audio_VR_button` | VRKeyboard/Audio_VR_button.cs | ~50 | 4 | 2 | 0 | 1 | 4 | Low |

---

## 4. Metric Definitions

| Metric | Full Name | Description | Good Range |
|--------|-----------|-------------|------------|
| **WMC** | Weighted Methods per Class | Number of methods in a class. Higher values indicate more complex, harder-to-maintain classes. | < 20 |
| **DIT** | Depth of Inheritance Tree | Distance from the class to the root of the inheritance tree. Greater depth increases complexity. | ≤ 5 |
| **NOC** | Number of Children | Count of direct subclasses. High values may indicate improper abstraction. | < 10 |
| **CBO** | Coupling Between Object Classes | Number of distinct classes a class is coupled to (references or is referenced by). High coupling reduces modularity. | < 10 |
| **RFC** | Response For a Class | Total number of methods reachable from the class (own methods + directly invoked external methods). | < 50 |
| **LCOM** | Lack of Cohesion in Methods | Measures how unrelated a class's methods are. High LCOM suggests a class should be split. | Low |

**LCOM Rating Scale Used:**
- **Low** — Methods share fields/responsibilities well; high cohesion.
- **Moderate** — Some unrelated functionality, but acceptable.
- **High** — Multiple distinct concerns in one class; refactoring recommended.

---

## 5. Inheritance Hierarchies

### MonoBehaviour (Unity base class)
52 classes inherit directly from `MonoBehaviour` (DIT = 1).

### Abstract Class Hierarchies

**`Action` (abstract)** — VideoMaker animation system
```
Action (abstract)
├── PositionAction (abstract)
│   ├── PositionActionHold
│   └── PositionActionPath
├── DirectionAction (abstract)
│   ├── DirectionActionHold
│   ├── DirectionActionTween
│   ├── DirectionActionLookAt
│   └── DirectionActionPath
└── UpDirectionActionPath
```
NOC(Action) = 8, DIT(leaf) = 2

**`Path` (abstract)** — VideoMaker camera path types
```
Path (abstract)
├── LinePath
├── CirclePath
├── CubicPath
├── QuadraticBezierPath
└── CubicBezierPath
```
NOC(Path) = 5, DIT(leaf) = 1

**`Easing` (abstract)** — VideoMaker animation easing
```
Easing (abstract)
├── EasingIn
├── EasingOut
├── EasingInOut
└── EasingInLinOut
```
NOC(Easing) = 4, DIT(leaf) = 1

**`Abstract_VR_button` (abstract MonoBehaviour)** — VR keyboard input
```
Abstract_VR_button (MonoBehaviour)
├── Char_VR_button
├── Backspace_VR_button
├── Shift_VR_button
├── Symbol_VR_button
└── Audio_VR_button
```
NOC(Abstract_VR_button) = 5, DIT(leaf) = 2

### Interfaces Implemented

| Interface | Implementing Classes |
|-----------|----------------------|
| `ICell` | `FeatureMenuCell`, `VideoPosListCell` |
| `IRecyclableScrollRectDataSource` | `FeatureMenuDataSource`, `VideoPosListDataSource` |
| `ISelectHandler` | `BrushSizeTooltip` |
| `IPointerDownHandler` / `IPointerUpHandler` / `IDragHandler` | `DesktopPaintController` |
| `ISerializationCallbackReceiver` | `NativePluginLoader` |
| `IEnumerator` | `VideoScriptReader`, `VideoUiManager` |

---

## 6. Coupling Analysis (CBO)

### Classes Ranked by CBO (descending)

| Rank | Class | Est. CBO | Coupled To |
|------|-------|----------|-----------|
| 1 | `CanvassDesktop` | 12 | `VolumeDataSetRenderer`, `VolumeInputController`, `VolumeCommandController`, `HistogramHelper`, `SourceRow`, `QuickMenuController`, `FeatureSetManager`, `Config`, `FitsReader`, `CatalogInputController`, `CatalogDataSet`, `DataMapping` |
| 2 | `VolumeDataSet` | 15 | `DataAnalysis`, `AstTool`, `FitsReader`, `Config`, `FeatureSetRenderer`, `VolumeDataSetRenderer`, `MaskDataSet`, `ColorMapUtils`, `VolumeCommandController`, `FeatureSetManager`, `ShapesManager`, `CatalogDataSet`, `MomentMapRenderer`, `DataMapping`, `Feature` |
| 3 | `VolumeInputController` | 10 | `VolumeDataSetRenderer`, `FeatureSetManager`, `VolumeDataSet`, `VolumeCommandController`, `FeatureSetRenderer`, `ToastNotification`, `QuickMenuController`, `CatalogInputController`, `ShapesManager`, `VideoCameraController` |
| 4 | `DesktopPaintController` | 9 | `VolumeDataSet`, `VolumeDataSetRenderer`, `FeatureSetManager`, `FeatureSetRenderer`, `Config`, `ColorMapUtils`, `HistogramMenuController`, `ToastNotification`, `DataAnalysis` |
| 5 | `ShapesManager` | 8 | `VolumeDataSet`, `VolumeDataSetRenderer`, `FeatureSetManager`, `Shape`, `ShapeAction`, `Config`, `VolumeInputController`, `CatalogDataSetRenderer` |
| 6 | `FeatureSetManager` | 8 | `Feature`, `FeatureSetRenderer`, `FeatureSetType`, `VolumeDataSet`, `VolumeDataSetRenderer`, `Config`, `DataAnalysis`, `FeatureMenuController` |
| 7 | `CatalogDataSetRenderer` | 8 | `CatalogDataSet`, `DataMapping`, `ColorMapUtils`, `FileInfo`, `Config`, `ColumnInfo`, `SourceRow`, `CatalogInputController` |
| 8 | `VolumeDataSetRenderer` | 8 | `VolumeDataSet`, `DataAnalysis`, `Config`, `ColorMapUtils`, `MaskDataSet`, `MomentMapRenderer`, `FeatureSetRenderer`, `ShapesManager` |
| 9 | `VideoCameraController` | 7 | `VideoUiManager`, `VideoScriptReader`, `Path`, `Action`, `Easing`, `VolumeDataSetRenderer`, `Config` |
| 10 | `FeatureSetRenderer` | 7 | `Feature`, `FeatureSetType`, `FeatureSetManager`, `Config`, `DataAnalysis`, `VolumeDataSet`, `ColorMapUtils` |

---

## 7. Largest Classes (WMC / LOC)

Classes with more than 300 LOC or more than 50 methods are flagged here.

| Rank | Class | LOC | Methods (WMC) | Concern |
|------|-------|-----|----------------|---------|
| 1 | `VolumeDataSet` | 1,920 | 330 | God Object — mixed data, I/O, statistics, state |
| 2 | `CanvassDesktop` | 1,899 | 340 | God Object — UI, state, event routing |
| 3 | `VolumeInputController` | 1,634 | 192 | Complex state machine |
| 4 | `DesktopPaintController` | 1,558 | 305 | Mixed painting + input concerns |
| 5 | `VolumeDataSetRenderer` | 1,402 | 297 | Mixed rendering + material + texture concerns |
| 6 | `FitsReader` | 730 | 147 | Large P/Invoke wrapper (acceptable) |
| 7 | `CatalogDataSetRenderer` | 694 | 145 | Mixed rendering + file parsing |
| 8 | `VolumeCommandController` | 685 | 108 | Large but focused |
| 9 | `VideoCameraController` | 605 | 74 | Moderate, mostly focused |
| 10 | `FeatureSetRenderer` | 616 | 77 | Moderate |
| 11 | `ShapesManager` | ~500 | 169 | Mixed shape logic + input handling |
| 12 | `DataAnalysis` | ~500 | 80 | Large P/Invoke wrapper (acceptable) |

---

## 8. Cohesion Analysis (LCOM)

### High LCOM (Refactoring Recommended)

**`VolumeDataSet`** — Multiple distinct responsibilities:
- File I/O (FITS loading, saving)
- Data manipulation (cropping, masking, statistics)
- Coordinate transforms
- History/undo tracking
- Rendering state

**Suggested split:** `VolumeDataLoader`, `VolumeDataProcessor`, `VolumeDataStats`, `VolumeDataHistory`

---

**`CanvassDesktop`** — God Object UI controller:
- Panel show/hide orchestration
- Histogram controls
- Source catalog interaction
- VR/desktop mode toggling
- File open/save dialogs

**Suggested split:** Extract each menu area into a dedicated `*PanelController`.

---

**`VolumeInputController`** — Mixed state machine + interaction:
- Input event dispatching
- Locomotion state management
- Tool switching logic
- Annotation handling

**Suggested split:** Use the State pattern with separate state classes per `InteractionState`.

---

**`DesktopPaintController`** — Mixed responsibilities:
- Pointer event handling
- Brush stroke management
- Mask writing
- UI feedback

**Suggested split:** Separate `BrushInputHandler` from `MaskPainter`.

---

### Low LCOM (Well-Cohesive Classes)

| Class | Why |
|-------|-----|
| `Feature` | Pure data container |
| `ColumnInfo` | Pure metadata container |
| `ColorMapEnum` | Single-purpose enum wrapper |
| `Delegates` | Single-purpose delegate declarations |
| `AstTool` | Focused astronomy utility |
| `ToastNotification` | Single UI concern |
| `Shape` | Focused shape data |
| All VRKeyboard leaf classes | Single button-type concern |
| All Path subclasses | Single path-type concern |
| All Easing subclasses | Single easing-type concern |

---

## 9. Architectural Layers

```
┌─────────────────────────────────────────────────────┐
│  Presentation Layer                                  │
│  CanvassDesktop, QuickMenuController, Menus/*        │
├─────────────────────────────────────────────────────┤
│  Interaction Layer                                   │
│  VolumeInputController, CatalogInputController,      │
│  DesktopPaintController, VRKeyboard/*                │
├─────────────────────────────────────────────────────┤
│  Rendering Layer                                     │
│  VolumeDataSetRenderer, CatalogDataSetRenderer,      │
│  FeatureSetRenderer, MomentMapRenderer               │
├─────────────────────────────────────────────────────┤
│  Domain / Data Layer                                 │
│  VolumeDataSet, CatalogDataSet, Feature,             │
│  FeatureSetManager, MaskDataSet, Config              │
├─────────────────────────────────────────────────────┤
│  Plugin Interface Layer                              │
│  FitsReader, DataAnalysis, AstTool,                  │
│  NativePluginLoader, ColorMapUtils                   │
└─────────────────────────────────────────────────────┘
```

**Cross-cutting concerns:** VideoMaker, Shapes, VoiceCommands, LineRenderer

---

## 10. Quality Observations

### Strengths

- Clear layered architecture with logical separation of concerns
- Good use of abstraction in `Action` / `Path` / `Easing` hierarchies
- Consistent enum usage across the codebase
- Plugin architecture (P/Invoke) cleanly isolates native code
- Nested classes used appropriately for tightly related types (e.g., `MaterialID`, `VoxelEntry`)
- Shallow DIT (max 3 levels) avoids deep inheritance fragility

### Concerns

| Priority | Issue | Affected Class(es) | Recommendation |
|----------|---------|--------------------|----------------|
| High | God Object (1900+ LOC, 300+ methods) | `VolumeDataSet`, `CanvassDesktop`, `DesktopPaintController` | Split into focused classes |
| High | High coupling (CBO > 10) | `CanvassDesktop`, `VolumeDataSet` | Introduce interfaces or an event bus |
| Medium | Complex state machine (1600 LOC) | `VolumeInputController` | Apply State pattern |
| Medium | Mixed rendering + file I/O | `CatalogDataSetRenderer` | Separate data parsing from rendering |
| Medium | Limited interface usage for decoupling | General | Add interfaces to allow dependency injection |
| Low | No dependency injection framework | General | Consider Zenject/VContainer for large managers |

### Enumerations (15 total)

`ColorMapEnum`, `ColumnType`, `FileTypes`, `FeatureSetType`, `VelocityUnit`, `AngleCoordFormat`,
`MaskMode`, `ProjectionMode`, `ScalingType`, `InteractionState`, `InteractionEvents`,
`LocomotionState`, `VRFamily`, `RotationAxes`, `PlayMode`, `ThresholdType`, `LimitType`,
`MovementMethod`, `ActionType`, `ShapeState`, `Primitives`, `SettingOption`

### Delegates (4 custom + 15 P/Invoke)

| Delegate | Purpose |
|----------|---------|
| `StringDelegate` | Generic string callback |
| `ColorMapDelegate` | Color map change notification |
| `VolumeDataSetRendererDelegate` | Renderer event notification |
| `CatalogDataSetRendererDelegate` | Catalog renderer event notification |
| DataAnalysis delegates (×15) | Native plugin callback signatures |

---

## 11. Summary Statistics

| Metric | Value |
|--------|-------|
| Total C# files | 104 |
| Total estimated LOC | ~31,500 |
| Total classes | 180+ |
| Classes inheriting MonoBehaviour | 52 |
| Abstract classes | 3 (`Action`, `Path`, `Easing`) |
| Abstract MonoBehaviour subclasses | 1 (`Abstract_VR_button`) |
| Interfaces defined | 3 (`ICell`, `IRecyclableScrollRectDataSource`, others via Unity) |
| Max DIT | 2 (VRKeyboard leaf buttons) |
| Avg DIT | 1.0 |
| Max NOC | 8 (`Action`) |
| Max WMC | 340 (`CanvassDesktop`) |
| Max CBO | 15 (`VolumeDataSet`) |
| Max RFC | ~80 (`VolumeDataSet`) |
| Enumerations | 22 |
| Custom delegates | 4 + 15 P/Invoke |
| Nested classes/structs | 23 |
| Classes with LCOM = High | 6 |
| Classes with LCOM = Low | ~25 |

---

*Generated by static analysis — values are approximations. For precise per-method cyclomatic complexity, use a dedicated tool such as NDepend, ReSharper, or SonarQube.*
