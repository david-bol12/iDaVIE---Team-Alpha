<#
    Cross-platform automated build script for iDaVIE.

    Usage:
        pwsh ./build.ps1 <path/to/vcpkg_root> <path/to/unity_executable>

    Examples:
        pwsh ./build.ps1 C:\vcpkg "C:\Program Files\Unity\Hub\Editor\2022.3.0f1\Editor\Unity.exe"

        pwsh ./build.ps1 ~/vcpkg /opt/unity/Editor/Unity

    Params:
        VCPKGROOT  - Root directory of vcpkg
        UNITYPATH - Path to Unity executable
#>

param (
    [Parameter(Mandatory, Position=0)]
    [Alias("vcpkg", "v")]
    [string]$VCPKGROOT,

    [Parameter(Mandatory, Position=1)]
    [Alias("unity", "u")]
    [string]$UNITYPATH
)

# ------------------------------------------------------------
# Platform Detection
# ------------------------------------------------------------

$IsWindowsPlatform = $PSVersionTable.Platform -eq "Win32NT"

if ($IsWindowsPlatform) {
    $VCPKGEXE = Join-Path $VCPKGROOT "vcpkg.exe"
    $VCPKG_TRIPLET = "x64-windows"
}
else {
    $VCPKGEXE = Join-Path $VCPKGROOT "vcpkg"
    $VCPKG_TRIPLET = "x64-linux"
}

$VCPKGCMAKE = Join-Path $VCPKGROOT "scripts/buildsystems/vcpkg.cmake"

# ------------------------------------------------------------
# Validation
# ------------------------------------------------------------

function Assert-FileExists {
    param(
        [string]$Path,
        [string]$Description
    )

    if (-not (Test-Path $Path -PathType Leaf)) {
        Write-Host "$Description not found at: $Path" -ForegroundColor Red
        exit 1
    }
}

Assert-FileExists $VCPKGCMAKE "vcpkg toolchain file"
Assert-FileExists $VCPKGEXE "vcpkg executable"
Assert-FileExists $UNITYPATH "Unity executable"

# ------------------------------------------------------------
# Configure vcpkg triplets
# ------------------------------------------------------------

Write-Host "Configuring vcpkg release builds..."

Get-ChildItem (Join-Path $VCPKGROOT "triplets") -Filter "*.cmake" | ForEach-Object {

    $Content = Get-Content $_.FullName -Raw

    if ($Content -notmatch "VCPKG_BUILD_TYPE") {
        Add-Content $_.FullName "`nset(VCPKG_BUILD_TYPE release)"
    }
}

# ------------------------------------------------------------
# Install dependencies
# ------------------------------------------------------------

Write-Host "Installing starlink-ast and cfitsio..."

& $VCPKGEXE install `
    "starlink-ast:$VCPKG_TRIPLET" `
    "cfitsio:$VCPKG_TRIPLET"

if ($LASTEXITCODE -ne 0) {
    Write-Host "vcpkg install failed." -ForegroundColor Red
    exit 1
}

# ------------------------------------------------------------
# Build native plugins
# ------------------------------------------------------------

Write-Host "Building native plugins..."

$NativePluginDir = Join-Path $PSScriptRoot "native_plugins_cmake"
$BuildDir = Join-Path $NativePluginDir "build"

if (-not (Test-Path $BuildDir)) {
    New-Item -ItemType Directory -Path $BuildDir | Out-Null
}

Push-Location $BuildDir

# Delete CMakeCache.txt manually instead of using --fresh (requires CMake 3.24+)
$CMakeCache = Join-Path $BuildDir "CMakeCache.txt"
if (Test-Path $CMakeCache) {
    Remove-Item $CMakeCache -Force
}

cmake `
    -DCMAKE_TOOLCHAIN_FILE="$VCPKGCMAKE" `
    -DCMAKE_BUILD_TYPE=Release `
    ..

if ($LASTEXITCODE -ne 0) {
    Write-Host "CMake configure failed." -ForegroundColor Red
    exit 1
}

cmake --build . --config Release --target install

if ($LASTEXITCODE -ne 0) {
    Write-Host "Native plugin build failed." -ForegroundColor Red
    exit 1
}

Pop-Location

Write-Host "Native plugins built successfully."

# ------------------------------------------------------------
# Plugin build directory
# ------------------------------------------------------------

$PluginBuildDir = Join-Path $PSScriptRoot "plugin_build"

if (-not (Test-Path $PluginBuildDir)) {
    New-Item -ItemType Directory -Path $PluginBuildDir | Out-Null
}

Push-Location $PluginBuildDir

# ------------------------------------------------------------
# Download helper
# ------------------------------------------------------------

function Download-IfMissing {
    param(
        [string]$FileName,
        [string]$Url
    )

    if (Test-Path $FileName) {
        Write-Host "$FileName already exists."
    }
    else {
        Write-Host "Downloading $FileName..."
        Invoke-WebRequest $Url -OutFile $FileName
    }
}

Write-Host "Downloading Unity packages..."

# TextMeshPro must be downloaded before import; it is not bundled with the repo
Download-IfMissing `
    "textMeshPro-3.0.6.unitypackage" `
    "https://download.packages.unity.com/com.unity.textmeshpro/-/com.unity.textmeshpro-3.0.6.tgz"

Download-IfMissing `
    "steamvr.unitypackage" `
    "https://github.com/ValveSoftware/steamvr_unity_plugin/releases/download/2.7.3/steamvr_2_7_3.unitypackage"

Download-IfMissing `
    "nuget.unitypackage" `
    "https://github.com/GlitchEnzo/NuGetForUnity/releases/download/v3.0.5/NugetForUnity.3.0.5.unitypackage"

Download-IfMissing `
    "scroll_rect.unitypackage" `
    "https://github.com/idia-astro/Recyclable-Scroll-Rect/releases/download/v1.0/recyclable-scroll-rect.unitypackage"

Download-IfMissing `
    "file_browser.unitypackage" `
    "https://github.com/idia-astro/UnityStandaloneFileBrowser/releases/download/1.2/StandaloneFileBrowser.unitypackage"

Pop-Location

# ------------------------------------------------------------
# Unity package import helper
# ------------------------------------------------------------

function Import-UnityPackage {
    param(
        [string]$PackagePath,
        [string]$LogName
    )

    Write-Host "Importing $(Split-Path $PackagePath -Leaf)..."

    # -ignorecompilererrors is intentional: packages have cross-dependencies and
    # will not all compile cleanly until the full set has been imported.
    & $UNITYPATH `
        -projectPath $PSScriptRoot `
        -batchmode `
        -nographics `
        -ignorecompilererrors `
        -logfile (Join-Path $PSScriptRoot $LogName) `
        -importPackage $PackagePath `
        -quit

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed importing $PackagePath" -ForegroundColor Red
        exit 1
    }
}

# ------------------------------------------------------------
# Import Unity packages
# ------------------------------------------------------------

Write-Host "Importing Unity packages..."

Import-UnityPackage `
    (Join-Path $PluginBuildDir "textMeshPro-3.0.6.unitypackage") `
    "import0.log"

Import-UnityPackage `
    (Join-Path $PluginBuildDir "steamvr.unitypackage") `
    "import1.log"

Import-UnityPackage `
    (Join-Path $PluginBuildDir "nuget.unitypackage") `
    "import2.log"

Import-UnityPackage `
    (Join-Path $PluginBuildDir "file_browser.unitypackage") `
    "import3.log"

Import-UnityPackage `
    (Join-Path $PluginBuildDir "scroll_rect.unitypackage") `
    "import4.log"

Write-Host "Packages imported."

# ------------------------------------------------------------
# Update Unity manifest
# ------------------------------------------------------------

Write-Host "Updating Unity manifest..."

$PackagesDir = Join-Path $PSScriptRoot "Packages"
$ManifestPath = Join-Path $PackagesDir "manifest.json"

if (-not (Test-Path $PackagesDir)) {
    New-Item -ItemType Directory -Path $PackagesDir | Out-Null
}

if (Test-Path $ManifestPath) {
    $Manifest = Get-Content $ManifestPath -Raw | ConvertFrom-Json
}
else {
    $Manifest = @{
        dependencies = @{}
    }
}

# Ensure dependencies object exists
if (-not $Manifest.dependencies) {
    $Manifest | Add-Member -MemberType NoteProperty -Name "dependencies" -Value ([PSCustomObject]@{})
}

# Add/update packages safely
$Manifest.dependencies | Add-Member -MemberType NoteProperty `
    -Name "com.unity.mathematics" `
    -Value "1.3.2" `
    -Force

$Manifest.dependencies | Add-Member -MemberType NoteProperty `
    -Name "com.unity.ugui" `
    -Value "1.0.0" `
    -Force

$Manifest.dependencies | Add-Member -MemberType NoteProperty `
    -Name "com.unity.xr.management" `
    -Value "4.4.0" `
    -Force

$Manifest.dependencies | Add-Member -MemberType NoteProperty `
    -Name "com.unity.xr.openxr" `
    -Value "1.8.2" `
    -Force

# Save manifest
$Manifest | ConvertTo-Json -Depth 10 | Out-File $ManifestPath -Encoding utf8

Write-Host ""
Write-Host "Configuration complete!" -ForegroundColor Green