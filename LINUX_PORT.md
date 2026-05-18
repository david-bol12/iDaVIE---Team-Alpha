# Linux Port: `idavie_native` Plugin

This document describes the changes made to port the `idavie_native` plugin
from Windows-only to a cross-platform (Windows + Linux) build, and provides
instructions for building a standalone C++ test harness against the resulting
shared library.

The plugin lives in `native_plugins_cmake/` and produces a shared library
loaded by the Unity client (`idavie_native.dll` on Windows,
`libidavie_native.so` on Linux).

---

## 1. Changes to `CMakeLists.txt`

The original `CMakeLists.txt` assumed a Windows/vcpkg toolchain:
`cminpack` was looked up via vcpkg's `CONFIG` package, the AST library was
expected to be a set of `libast*.lib` files, and the install step copied
`.dll` runtime files into `Assets/Plugins`. None of that works on Linux,
where AST ships as `libstarlink_ast.so`, `cminpack` is installed via
`apt` / `pkg-config`, and the produced artefact is a `.so` rather than a
DLL with separate runtime files.

The file is now split into `IF (WIN32) ... ELSE () ... ENDIF ()` blocks:

### `cminpack` lookup

- **Windows:** unchanged — `find_package(cminpack CONFIG REQUIRED)` from vcpkg.
- **Linux:** `find_package(CMinpack QUIET)` first, then fall back to
  `pkg-config` (`pkg_search_module(CMINPACK REQUIRED cminpack)`), which is how
  the Debian/Ubuntu `libcminpack-dev` package exposes itself.

A new variable `CMINPACK_LINK_TARGET` is set to either `cminpack::cminpack`
(Windows) or `${CMINPACK_LIBRARIES}` (Linux) and used in
`target_link_libraries`.

### AST library lookup

- **Windows:** unchanged — `find_path(AST_INCLUDE_DIR ast.h)` and
  `find_library(AST_LIB_PATH libast)`, then link against the full
  `libast / libast_err / libast_pal / libast_grf_*` set.
- **Linux:** Starlink AST installs as `libstarlink_ast.so`,
  `libstarlink_ast_err.so`, `libstarlink_ast_grf3d.so` (the `grf_*` graphics
  back-ends and PAL are bundled into the main library on most distros).
  Only the include directory is searched; linker names are hard-coded into
  a new `AST_LINK_LIBS` variable.

### `target_link_libraries`

Reduced to a single call that uses the platform-specific variables:

```cmake
target_link_libraries(idavie_native cfitsio ${CMINPACK_LINK_TARGET} ${AST_LINK_LIBS} OpenMP::OpenMP_CXX)
```

### Install step

- **Windows:** unchanged — installs the DLL plus the `cfitsio`, `zlib1`, and
  `cminpack` runtime DLLs into `Assets/Plugins`.
- **Linux:** installs only the `LIBRARY` artefact (`libidavie_native.so`)
  into `Assets/Plugins`. System libraries (cfitsio, cminpack, AST) are
  expected to be resolved by the dynamic loader at runtime via the standard
  library search path.

---

## 2. Changes to the `.cpp` / `.h` files

The original sources used Windows-only features in three places. Each was
guarded with `#ifdef _WIN32` so the Windows build is byte-identical to before.

### a. `DllExport` macro

`fits_reader.h`, `ast_tool.h`, `data_analysis_tool.h` all defined:

```cpp
#define DllExport __declspec(dllexport)
```

Replaced in all three headers with:

```cpp
#ifdef _WIN32
#define DllExport __declspec(dllexport)
#else
#define DllExport __attribute__((visibility("default")))
#endif
```

GCC/Clang on Linux export every non-static symbol by default, but using the
visibility attribute keeps the intent explicit and matches the behaviour
required if the build is later compiled with `-fvisibility=hidden`.

### b. `strcpy_s` / `strncpy_s`

Microsoft's "safe" string functions are not part of the C standard and are
absent from glibc. They are used in `fits_reader.cpp` and `ast_tool.cpp`.
Each file now adds, near the top:

```cpp
#include <cstring>

#ifndef _WIN32
#define strcpy_s(dest, destsz, src) strncpy((dest), (src), (destsz) - 1)
#define strncpy_s(dest, destsz, src, count) strncpy((dest), (src), (count) < (destsz) ? (count) : (destsz) - 1)
#endif
```

This preserves the call-site signatures used throughout the codebase
(`strcpy_s(dest, size, src)`) without rewriting them.

### c. `M_PI` redefinition

`ast_tool.cpp` had:

```cpp
const double M_PI = 3.141592653589793238463;
```

On Linux this clashes with the `M_PI` macro from `<cmath>` /
`<math.h>`. It is now guarded:

```cpp
#include <cmath>

#ifndef M_PI
const double M_PI = 3.141592653589793238463;
#endif
```

### d. `LONGLONG` vs `int64_t` for CFITSIO calls

`fits_read_pixll` and `fits_get_img_sizell` take `LONGLONG *`. On Windows
CFITSIO `typedef`s `LONGLONG` to `__int64`, which is layout-compatible with
`int64_t`, and the Windows compiler accepted the implicit conversion. On
Linux, `LONGLONG` is `long long`, and even though that has the same width
as `int64_t` (which on 64-bit Linux is `long`), GCC refuses the implicit
pointer conversion (`long*` → `long long*` is a different type).

Three call sites in `fits_reader.cpp` now use an explicit
`reinterpret_cast<LONGLONG*>(...)`:

- `FitsGetImageSize` — the `imageSize` buffer.
- `FitsReadImageFloat` — the `startPix` argument.
- `FitsReadImageInt16` — the `startPix` argument.

This is sound because `int64_t` and `LONGLONG` are the same width on every
platform we target; only the C++ type system disagrees.

---

## 3. Building on Linux

### Dependencies (Debian / Ubuntu)

```bash
sudo apt install build-essential cmake pkg-config \
                 libcfitsio-dev libcminpack-dev libstarlink-ast-dev \
                 libomp-dev
```

### Build

```bash
cd native_plugins_cmake
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build -j
```

The resulting `libidavie_native.so` is in `build/`. To install it into
`Assets/Plugins` so Unity picks it up:

```bash
cmake --install build
```

---

## 4. Test harness for the plugin

Below is a minimal standalone C++ test harness that links against
`libidavie_native.so` and exercises the most-used FITS reader entry points.
It opens a FITS cube, queries its dimensions, reads the data into memory,
and closes the file — exactly the path the Unity loader takes for a normal
cube load.

### 4.1 Layout

Create a `tests/` directory next to `native_plugins_cmake/`:

```
iDaVIE---Team-Alpha/
├── native_plugins_cmake/
│   └── build/libidavie_native.so
└── tests/
    ├── CMakeLists.txt
    └── test_fits_reader.cpp
```

### 4.2 `tests/test_fits_reader.cpp`

```cpp
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <cstdint>
#include <fitsio.h>

extern "C" {
    int FitsOpenFileReadOnly(fitsfile **fptr, char *filename, int *status);
    int FitsCloseFile(fitsfile *fptr, int *status);
    int FitsGetImageDims(fitsfile *fptr, int *dims, int *status);
    int FitsGetImageSize(fitsfile *fptr, int dims, int64_t **naxes, int *status);
    int FitsReadImageFloat(fitsfile *fptr, int dims, int64_t nelem,
                           float **array, int *status);
    int FreeFitsPtrMemory(void *ptr);
}

static int fail(const char *what, int status) {
    std::fprintf(stderr, "[FAIL] %s (cfitsio status=%d)\n", what, status);
    return 1;
}

int main(int argc, char **argv) {
    if (argc != 2) {
        std::fprintf(stderr, "usage: %s <cube.fits>\n", argv[0]);
        return 2;
    }

    char path[4096];
    std::strncpy(path, argv[1], sizeof(path) - 1);
    path[sizeof(path) - 1] = '\0';

    fitsfile *fptr = nullptr;
    int status = 0;

    if (FitsOpenFileReadOnly(&fptr, path, &status) != 0)
        return fail("FitsOpenFileReadOnly", status);
    std::printf("[ok] opened %s\n", path);

    int dims = 0;
    if (FitsGetImageDims(fptr, &dims, &status) != 0)
        return fail("FitsGetImageDims", status);
    std::printf("[ok] dims = %d\n", dims);

    int64_t *naxes = nullptr;
    if (FitsGetImageSize(fptr, dims, &naxes, &status) != 0)
        return fail("FitsGetImageSize", status);
    int64_t nelem = 1;
    for (int i = 0; i < dims; ++i) {
        std::printf("       NAXIS%d = %lld\n", i + 1, (long long)naxes[i]);
        nelem *= naxes[i];
    }
    std::printf("[ok] total voxels = %lld\n", (long long)nelem);

    float *data = nullptr;
    if (FitsReadImageFloat(fptr, dims, nelem, &data, &status) != 0)
        return fail("FitsReadImageFloat", status);
    std::printf("[ok] read %lld floats, first voxel = %f\n",
                (long long)nelem, data[0]);

    FreeFitsPtrMemory(naxes);
    FreeFitsPtrMemory(data);

    if (FitsCloseFile(fptr, &status) != 0)
        return fail("FitsCloseFile", status);
    std::printf("[ok] closed cleanly\n");

    return 0;
}
```

### 4.3 `tests/CMakeLists.txt`

```cmake
cmake_minimum_required(VERSION 3.16)
project(idavie_plugin_tests CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Path to the built shared library and its headers.
set(PLUGIN_DIR ${CMAKE_SOURCE_DIR}/../native_plugins_cmake)
set(PLUGIN_BUILD_DIR ${PLUGIN_DIR}/build)

find_package(PkgConfig REQUIRED)
pkg_check_modules(CFITSIO REQUIRED cfitsio)

add_executable(test_fits_reader test_fits_reader.cpp)
target_include_directories(test_fits_reader PRIVATE
    ${PLUGIN_DIR}
    ${CFITSIO_INCLUDE_DIRS})
target_link_directories(test_fits_reader PRIVATE
    ${PLUGIN_BUILD_DIR})
target_link_libraries(test_fits_reader PRIVATE
    idavie_native
    ${CFITSIO_LIBRARIES})

# Make sure the harness can find the .so at run time without `make install`.
set_target_properties(test_fits_reader PROPERTIES
    BUILD_RPATH "${PLUGIN_BUILD_DIR}")
```

### 4.4 Build and run

```bash
# 1. Build the plugin first.
cd native_plugins_cmake
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build -j

# 2. Build the harness.
cd ../tests
cmake -S . -B build
cmake --build build -j

# 3. Run against any FITS cube. A small one from Data/ works:
./build/test_fits_reader ../Data/some_cube.fits
```

Expected output (sizes will vary):

```
[ok] opened ../Data/some_cube.fits
[ok] dims = 3
       NAXIS1 = 256
       NAXIS2 = 256
       NAXIS3 = 128
[ok] total voxels = 8388608
[ok] read 8388608 floats, first voxel = 0.012345
[ok] closed cleanly
```

If the loader cannot find `libidavie_native.so` despite the `BUILD_RPATH`
setting, fall back to:

```bash
LD_LIBRARY_PATH=../native_plugins_cmake/build ./build/test_fits_reader <cube.fits>
```

### 4.5 What this exercises

- The `extern "C"` symbol exports — confirms the `DllExport` /
  visibility change works.
- `FitsGetImageSize` and `FitsReadImageFloat` — the two functions whose
  signatures required the `reinterpret_cast<LONGLONG*>` fix; a wrong-width
  cast would either crash or return garbage in the first few voxels.
- `FreeFitsPtrMemory` — confirms the plugin and harness agree on the
  allocator (both use `new[]` / `delete[]` via the plugin's own free
  function, which is the correct cross-DLL pattern).

### 4.6 Suggested extensions

- Add a second test that calls `FitsReadSubImageFloat` with a non-trivial
  `startPix` / `finalPix` to exercise the chunked-read path.
- Add a test that opens a mask file and calls `FitsReadImageInt16` —
  this hits the third `reinterpret_cast<LONGLONG*>` call site.
- Wire the tests into CTest by adding `enable_testing()` and
  `add_test(NAME fits_open COMMAND test_fits_reader ${CMAKE_SOURCE_DIR}/fixtures/small.fits)`.
