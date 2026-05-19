# `idavie_native` plugin test harness

A minimal standalone C++ harness that links against `libidavie_native.so` and
exercises the FITS reader entry points the Unity client uses for a normal cube
load. It is the quickest way to verify that the Linux build of the native
plugin works in isolation, without needing Unity or a VR headset.

The harness covers, in one run, all three of the Linux-port source fixes:

- The `extern "C"` symbol exports — confirms the `DllExport` visibility change
  is correct.
- `FitsGetImageSize` and `FitsReadImageFloat` — the two functions whose
  signatures required `reinterpret_cast<LONGLONG*>`. A wrong-width cast would
  crash or return garbage in the first few voxels.
- `FreeFitsPtrMemory` — confirms the harness and plugin agree on the allocator
  (both use the plugin's own `new[]` / `delete[]` via the exported free
  function, the correct cross-DLL pattern).

## Layout

```
iDaVIE---Team-Alpha/
├── native_plugins_cmake/
│   └── build/libidavie_native.so       <- the plugin
└── tests/
    ├── CMakeLists.txt
    ├── LAYOUT.md                       <- contributor docs (what tests go where)
    ├── test_fits_reader.cpp            <- cfitsio coverage
    ├── test_ast_tool.cpp               <- AST smoke (WCS roundtrip)
    ├── test_data_analysis_tool.cpp     <- analysis smoke (FindMaxMin/FindStats)
    └── build/                          <- the three harness binaries
```

There is one binary per native translation unit. The `SKIP_RETURN_CODE 77`
hook in `CMakeLists.txt` is kept available so future stub binaries can
return 77 to appear as Skipped rather than Passed. See `LAYOUT.md` for
the rationale and the scope of each binary.

Each binary's `BUILD_RPATH` points at `native_plugins_cmake/build/`, so it
locates the `.so` at run time without `LD_LIBRARY_PATH`.

## Prerequisites

On Debian / Ubuntu:

```bash
sudo apt install build-essential cmake pkg-config \
                 libcfitsio-dev libcminpack-dev libstarlink-ast-dev \
                 libomp-dev
```

These are the same packages required to build the plugin itself.

## Build

From the repo root:

```bash
# 1. Build the plugin first.
cd native_plugins_cmake
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build -j

# 2. Build the harness.
cd ../tests
cmake -S . -B build
cmake --build build -j
```

The harness is a single executable at `tests/build/test_fits_reader`.

### Note on the linker flag

`CMakeLists.txt` passes
`-Wl,--unresolved-symbols=ignore-in-shared-libs` to the harness link. The
Debian `libstarlink-ast-dev` package leaves the AST graphics callbacks
(`astGLine`, `astGMark`, `astGQch`, `astGAttr`, `astGBBuf`, `astGEBuf`,
`astGCap`, `astGScales`, `astGText`, `astGTxExt`) undefined — they are
expected to be supplied by the host application's plotting layer (PGPLOT or
similar). The plugin never calls those symbols, so the runtime loader is
happy with lazy binding, but binutils `ld` will refuse to link the executable
unless we tell it to ignore symbols that are transitively undefined through
shared libraries. This flag does exactly that and nothing else.

## Run

Run everything via CTest from `tests/build/`:

```bash
ctest --output-on-failure
```

This builds nothing; it just runs each harness against the bundled sample
cube (`Data/SampleData/test_volume.fits`). Pending stub binaries report as
`Skipped`, real failures as `Failed`. To run only one, use the `-R` regex:

```bash
ctest -R fits_reader --output-on-failure
```

To run a single harness directly (e.g. for a different fixture, or to read
its stdout in full):

```bash
./test_fits_reader ../../Data/SampleData/test_volume.fits
```

If the rpath ever breaks (e.g. the plugin `.so` moves), fall back to:

```bash
LD_LIBRARY_PATH=../../native_plugins_cmake/build \
    ./test_fits_reader <cube.fits>
```

Each harness takes exactly one argument (the FITS path). Exit codes:

| code | meaning                                      |
|------|----------------------------------------------|
| 0    | every step succeeded                         |
| 1    | a plugin call failed (cfitsio status logged) |
| 2    | wrong argument count                         |
| 77   | pending stub — not yet implemented (CTest treats this as Skipped) |

## Expected output

Running against the bundled sample cube (`Data/SampleData/test_volume.fits`, a
4×4×4 float cube):

```
[ok] opened ../Data/SampleData/test_volume.fits
[ok] dims = 3
       NAXIS1 = 4
       NAXIS2 = 4
       NAXIS3 = 4
[ok] total voxels = 64
[ok] read 64 floats, first voxel = 0.000000
[ok] closed cleanly
```

A good run has:

- Six `[ok]` lines, one per call, in this order:
  `opened` → `dims` → NAXIS lines + `total voxels` → `read … floats` → `closed cleanly`.
- `dims` matching the cube's NAXIS count (3 for a typical spectral cube).
- One `NAXIS<i>` line per axis, with sensible positive sizes.
- `total voxels` equal to the product of the NAXIS values.
- A `first voxel = …` line with a finite float. Zero is fine for an empty/blank
  cube like the bundled sample; a real cube will print real data.
- Exit status `0` (`echo $?` after running).

## Failure modes and what they mean

| `[FAIL] …` line                  | likely cause                                                 |
|----------------------------------|--------------------------------------------------------------|
| `FitsOpenFileReadOnly`           | path wrong, file unreadable, or not a valid FITS file        |
| `FitsGetImageDims`               | primary HDU is not an image (e.g. binary table cube)         |
| `FitsGetImageSize`               | usually pairs with a `LONGLONG`-width mismatch — check the `reinterpret_cast<LONGLONG*>` in `fits_reader.cpp` |
| `FitsReadImageFloat`             | same width-mismatch suspect, or genuine cfitsio I/O error    |
| `FitsCloseFile`                  | rare; usually a corrupted file handle                        |

A `cfitsio status=` number is printed next to every failure — look it up in
the [CFITSIO error code list](https://heasarc.gsfc.nasa.gov/docs/software/fitsio/c/c_user/node26.html)
for the precise cause.

If the executable crashes with a SIGSEGV inside the first voxel print, that is
almost always the `LONGLONG` / `int64_t` pointer-type fix not being applied —
recheck the three `reinterpret_cast<LONGLONG*>` call sites in
`native_plugins_cmake/fits_reader.cpp`.

If the binary fails to start with
`error while loading shared libraries: libidavie_native.so`, the rpath did not
take — rebuild the harness, or use the `LD_LIBRARY_PATH` fallback above.

## Suggested extensions

- Add a second test that calls `FitsReadSubImageFloat` with a non-trivial
  `startPix` / `finalPix` to exercise the chunked-read path.
- Add a test that opens a mask file and calls `FitsReadImageInt16` — this
  hits the third `reinterpret_cast<LONGLONG*>` call site.
- Wire the tests into CTest by adding `enable_testing()` and
  `add_test(NAME fits_open COMMAND test_fits_reader ${CMAKE_SOURCE_DIR}/fixtures/small.fits)`.
