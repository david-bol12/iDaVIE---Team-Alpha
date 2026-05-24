# GUI Direct File I/O — Refactoring Analysis

This document identifies places where GUI/menu controllers perform direct file I/O that should instead be delegated to a service or data layer. No prior analysis or tracking of this issue exists in the codebase.

## Summary

Every export/save action in iDaVIE (moment maps, spectral profiles, catalogs, screenshots, video frames, video scripts) is triggered directly from a menu or controller class. These classes construct output paths, create directories, and write files themselves rather than delegating to a dedicated service.

---

## GUI and Menu Scripts

These are the clearest candidates for refactoring — UI layer classes that own file system concerns directly.

| File | Line(s) | Operation |
|------|---------|-----------|
| `Assets/Scripts/Menu/MomentMapMenuController.cs` | 211–234 | Creates `Outputs/MomentMaps/` directory; writes PNG via `File.WriteAllBytes` and FITS files |
| `Assets/Scripts/Menu/SpectralProfileMenuController.cs` | 64–74 | Creates `Outputs/SpectralProfiles/` directory; writes CSV/text data via `StreamWriter` |
| `Assets/Scripts/Menu/VideoRecordMenuController.cs` | 105–117 | Creates `Outputs/VideoScripts/` directory; writes video script files |
| `Assets/Scripts/FeatureData/FeatureMenuController.cs` | 402–415 | Creates `Outputs/Catalogs/` directory via `Directory.CreateDirectory` |
| `Assets/Scripts/Tools/CameraControllerTool.cs` | 112 | Writes screenshot bytes via `File.WriteAllBytes` |

## VideoMaker Scripts

These classes are not strictly GUI but are tightly coupled to the video recording UI workflow and also handle files directly.

| File | Line(s) | Operation |
|------|---------|-----------|
| `Assets/Scripts/VideoMaker/VideoCameraController.cs` | 561 | Writes video frame bytes from a queue via `File.WriteAllBytes` |
| `Assets/Scripts/VideoMaker/VideoPosRecorder.cs` | 176 | Writes camera position recording via `StreamWriter` |
| `Assets/Scripts/VideoMaker/VideoUiManager.cs` | 349 | Reads a video script file via `StreamReader` |

## Data Layer Scripts (Borderline)

These are not GUI classes, but they are closely coupled to the above controllers and perform their own file I/O rather than going through a shared abstraction.

| File | Line(s) | Operation |
|------|---------|-----------|
| `Assets/Scripts/FeatureData/FeatureMapper.cs` | 66, 74 | `File.ReadAllText` / `File.WriteAllText` for JSON column-mapping config |
| `Assets/Scripts/FeatureData/FeatureSetManager.cs` | 435–438 | Opens a `StreamWriter` for feature output logging |
| `Assets/Scripts/FeatureData/VoTable.cs` | 142 | `File.Copy` to copy a VOTable file to an output location |
| `Assets/Scripts/CatalogData/DataMapping.cs` | 61 | `File.ReadAllText` for column-mapping JSON |
| `Assets/Scripts/CatalogData/CatalogDataSet.cs` | 74, 414, 423 | `File.ReadAllLines` for catalog ingestion; `File.Open` for `.cache` read and write |

---

## Recurring Pattern

All of the GUI-layer offenders follow the same pattern:

```csharp
var directoryPath = Path.Combine(directory.Parent.FullName, "Outputs/<Type>");
if (!Directory.Exists(directoryPath))
    Directory.CreateDirectory(directoryPath);
var path = Path.Combine(directoryPath, filename);
// ... write to path directly
```

This logic is duplicated across at least five menu controllers and could be consolidated into a single output-path service.

---

## Suggested Approach

1. Introduce an `IOutputFileService` (or similar) responsible for resolving output paths and creating directories.
2. Move all `File.WriteAllBytes`, `StreamWriter`, and `File.Copy` calls out of menu/controller classes and into the service or into dedicated exporter classes per output type (e.g. `MomentMapExporter`, `SpectralProfileExporter`).
3. GUI controllers should pass data to the exporter and receive a result (success/failure, final path), with no knowledge of the filesystem.
