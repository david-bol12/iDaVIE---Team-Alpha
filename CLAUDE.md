# CLAUDE.md

iDaVIE (immersive Data Visualisation Interactive Explorer) is a Unity VR app for rendering 3D astronomical FITS data cubes via ray-marching shaders. Runs on Windows via SteamVR. Native C++ plugin also builds on Linux (no Unity/VR there).

Unity version: 2021.3.x LTS. Scene entry point: `Assets/Scenes/ui.unity`.

## Build

### First-time setup (Windows)

Run once from the repo root — installs vcpkg deps, builds native plugin, imports Unity packages:

```powershell
.\Configure.ps1 "C:\vcpkg" "C:\Program Files\Unity\2021.3.47f1\Editor\Unity.exe"
```

If blocked by execution policy: `Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope CurrentUser`

### Rebuild native plugin only

Windows:

```powershell
cmake --fresh -DCMAKE_TOOLCHAIN_FILE="C:\vcpkg\scripts\buildsystems\vcpkg.cmake" -DCMAKE_BUILD_TYPE=Release native_plugins_cmake/
cmake --build native_plugins_cmake/build --config Release --target install
```

Linux:

```bash
sudo apt install build-essential cmake pkg-config libcfitsio-dev libcminpack-dev libstarlink-ast-dev libomp-dev
cmake -S native_plugins_cmake -B native_plugins_cmake/build -DCMAKE_BUILD_TYPE=Release
cmake --build native_plugins_cmake/build -j && cmake --install native_plugins_cmake/build
```

Output: `idavie_native.dll` / `libidavie_native.so` copied to `Assets/Plugins/`.

### Unity build (after native plugin)

1. Open project in Unity Hub (add from disk, repo root)
2. Open `Assets/Scenes/ui.unity`
3. Window → SteamVR Input → Save and generate (required once; regenerates action bindings)
4. File → Build Settings → Build (confirm OpenVR Loader selected under XR Plug-in Management)

## Testing

Manual checklist required before merging to `main`: https://forms.gle/ezLXLHeWR4ZeLmfz7

Native plugin test harness (Linux, no Unity required):

```bash
cmake -S tests -B tests/build && cmake --build tests/build -j
./tests/build/test_fits_reader Data/SampleData/test_volume.fits
# Expected: six [ok] lines, exit code 0
```

Windows:

```powershell
cmake -S tests -B tests/build -DCMAKE_TOOLCHAIN_FILE=C:/vcpkg/scripts/buildsystems/vcpkg.cmake -A x64
cmake --build tests/build --config Release --parallel
tests\build\Release\test_fits_reader.exe Data\SampleData\test_volume.fits
```

CI runs these on every push (`.github/workflows/ci.yaml`).

## Architecture

Two-layer design: Unity C# (`Assets/Scripts/`) handles VR interaction, UI, and rendering. It calls into `idavie_native` (.dll/.so) via P/Invoke for FITS I/O, WCS coordinate transforms, and analysis. The native layer depends on CFITSIO, Starlink AST, cminpack, and OpenMP.

Key native files: `fits_reader`, `ast_tool`, `data_analysis_tool`, `cdl_zscale`.

Key C# classes: `VolumeDataSet` (owns voxels/mask), `VolumeDataSetRenderer` (drives ray-march shader), `VolumeInputController` (VR input), `Config` (singleton loaded from `config.json` beside the executable).

P/Invoke bridge: `Assets/Scripts/PluginInterface/` — `FitsReader.cs`, `AstTool.cs`, `DataAnalysis.cs`.

## Code Conventions

- All `Assets/Scripts/*.cs` files must include the LGPL v3 licence header. CI checks for the string `"iDaVIE is free software"` and fails if it's missing.
- New public APIs should have XML doc comments (`/// <summary>`).
- PRs must be tested as a compiled executable, not just in the Unity Editor.

## Scene File Merging

`.unity` files are YAML and conflict-prone. Use UnityYamlMerge — see `DEVELOP.md` for the `.git/config` setup. When merging branches that touch scenes:

```bash
git switch <target-branch>
git merge --no-commit <source-branch>
# Resolve .cs conflicts first, then:
git mergetool   # invokes UnityYamlMerge for .unity files
```

## Runtime Config

`config.json` lives next to the executable and is auto-created with defaults if absent. Schema: https://idavie.readthedocs.io/en/latest/_static/idavie_config_2.json

Key fields: `gpuMemoryLimitMb`, `maxRaymarchingSteps`, `defaultColorMap`, `voiceCommandConfidenceLevel`.
