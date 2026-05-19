# Test harness layout

This document defines how the C++ test harness for `libidavie_native.so` is
organised, and where new tests belong. The user-facing build/run instructions
live in `README.md`; this file is for whoever is *adding* tests.

## The split: one binary per native translation unit

The plugin is built from three independent `.cpp` files in
`native_plugins_cmake/`:

| native TU              | what it wraps             | test binary             |
|------------------------|---------------------------|-------------------------|
| `fits_reader.cpp`      | cfitsio (file I/O)        | `test_fits_reader`      |
| `ast_tool.cpp`         | Starlink AST (WCS math)   | `test_ast_tool`         |
| `data_analysis_tool.cpp` | stats / analysis helpers | `test_data_analysis_tool` |

Each binary has its own `main()`, its own CTest entry, and links against
`libidavie_native.so` independently. A crash or link failure in one does not
mask failures in the others — that isolation is the whole reason for the
three-binary layout.

```
tests/
├── CMakeLists.txt
├── README.md                       # build/run docs (user-facing)
├── LAYOUT.md                       # this file (contributor-facing)
├── test_fits_reader.cpp            # cfitsio entry points
├── test_ast_tool.cpp               # AST entry points
└── test_data_analysis_tool.cpp     # analysis entry points
```

## What goes in each binary

### `test_fits_reader`

Everything that calls a `Fits*` entry point. Grouped by feature area inside
the file (one helper function per area, called from `main()` in order):

- **Read path** — `FitsOpenFileReadOnly`, `FitsGetImageDims`,
  `FitsGetImageSize`, `FitsReadImageFloat`, `FitsReadSubImageFloat`,
  `FitsReadImageInt16`, `FitsReadSubImageInt16`. Covers all three
  `reinterpret_cast<LONGLONG*>` sites that the Linux port had to fix.
- **Header keys** — `FitsReadKey`, `FitsReadKeyString`, `FitsWriteKey`,
  `FitsUpdateKey`, `FitsDeleteKey`. Roundtrip on a temp file.
- **HDU navigation** — `FitsGetHduCount`, `FitsMovabsHdu`, `FitsGetHduType`,
  `FitsGetCurrentHdu`. Needs a multi-HDU fixture.
- **Write path** — `FitsCreateFile`, `FitsCreateImg`,
  `FitsWriteSubImageInt16`, `FitsWriteNewCopySubImageInt16`, then
  reopen-and-read-back. Catches endian / row-major / off-by-one bugs.
- **Subcube save** — `FitsCopyImageSection`, `FitsCopyCubeSection`. Regression
  guard for commit `7ef544d` ("Fix issue with saving subcubes and submasks").
- **Error paths** — nonexistent file, non-FITS file, null/double-free into
  `FreeFitsPtrMemory`. Cheap, catches a lot.

### `test_ast_tool`

Smoke-level only, for now. Goal is "AST loads, links, and survives one real
call" — *not* exhaustive WCS coverage.

- Build an AST frame-set from a real cube header via
  `FitsCreateHdrPtrForAst` → AST init entry point in `ast_tool.h`.
- Do one pixel → world transform on a known voxel; assert the result is
  finite and within a sensible range.
- Free everything cleanly.

This binary is the canary for the AST graphics-callback `--unresolved-symbols`
linker flag — if that flag ever stops working on a new distro, this is the
binary that fails to link first.

### `test_data_analysis_tool`

Smoke-level only. Pick the simplest exported function (likely a stats
call — read `data_analysis_tool.h` to confirm), feed it a small array with a
known answer, assert the result. One assertion is enough; the point is to
prove the third TU's symbols are exported on Linux.

## Shared helpers

When two binaries need the same fixture (e.g. "build a 4×4×4 float cube with
known voxel values" or "make a temp path that cleans itself up"), factor it
into `helpers.h` + `helpers.cpp` and link both binaries against it.

Do not pre-emptively create `helpers.*`. Add it only when the second binary
actually needs the helper — duplicated code across two TUs is cheaper than a
shared header used by one.

## CMake pattern

The current `CMakeLists.txt` has the per-binary boilerplate inline for one
target. When adding the second and third binaries, factor the common config
into a function so the three blocks don't drift:

```cmake
function(add_plugin_test name)
    add_executable(${name} ${name}.cpp)
    target_include_directories(${name} PRIVATE
        ${PLUGIN_DIR}
        ${CFITSIO_INCLUDE_DIRS})
    target_link_directories(${name} PRIVATE ${PLUGIN_BUILD_DIR})
    target_link_libraries(${name} PRIVATE
        idavie_native
        ${CFITSIO_LIBRARIES})
    target_link_options(${name} PRIVATE
        -Wl,--unresolved-symbols=ignore-in-shared-libs)
    set_target_properties(${name} PROPERTIES
        BUILD_RPATH "${PLUGIN_BUILD_DIR}")
    add_test(NAME ${name}
             COMMAND ${name} ${SAMPLE_CUBE})
    set_tests_properties(${name} PROPERTIES SKIP_RETURN_CODE 77)
endfunction()

enable_testing()
add_plugin_test(test_fits_reader)
add_plugin_test(test_ast_tool)
add_plugin_test(test_data_analysis_tool)
```

`ctest --output-on-failure` from the build dir then runs all three.

A stub binary returns 77 to signal "not yet implemented" — CTest reports it
as `Skipped` rather than `Passed`, so pending tests can't silently look
green. Drop the 77 and return 0/nonzero from real assertions when
implementing the body.

The `--unresolved-symbols=ignore-in-shared-libs` flag stays on every binary —
even ones that never touch AST — because `libidavie_native.so` pulls AST in
transitively, and binutils `ld` checks the whole link graph.

## When to escalate to doctest (or Catch2)

Stay framework-free until one of these is true:

- Any single binary has more than ~5 distinct scenarios. Past that, ad-hoc
  `[ok]` / `[FAIL]` lines stop being useful and you want real assertion
  diagnostics.
- You want to run a subset of cases by name (`./test_fits_reader --filter
  subcube`). CTest's `-R` regex is a stopgap; per-case filtering needs a
  framework.
- A shared global in `main()` causes ordering bugs between scenarios in the
  same file.

Migration path when the trigger fires: vendor `doctest.h` (single header),
add one `main.cpp` per binary that defines `DOCTEST_CONFIG_IMPLEMENT_WITH_MAIN`,
convert each scenario helper into a `TEST_CASE`. The binary count and CMake
shape don't change — only the file contents do.

## Test data

The bundled `Data/SampleData/test_volume.fits` (4³ float cube, all zeros) is
fine for read-path smoke tests but useless for asserting specific values.
For anything that needs known voxel content, build the fixture in-test with
cfitsio's own `fits_create_file` and write it to a temp path — keeps the
test hermetic and lets you assert exact bytes on read-back.

Mask fixtures (`Int16` images) and multi-HDU fixtures will need the same
treatment; there are no bundled samples for those.
