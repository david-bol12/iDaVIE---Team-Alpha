/*
 * idavie_abi.h — Public C ABI for iDaVIE kernel plug-ins.
 *
 * Part of the Sub-team 1 (Architecture and Micro-kernel Core) refactoring
 * proposal. This header is a design artefact: it is not yet built into
 * libidavie_native. Plug-in authors targeting the revised architecture
 * #include only this header — no CFITSIO, AST, or other third-party
 * headers leak through the ABI.
 *
 * Status: DRAFT v0.1.0. Subject to change until the Sprint 1 review on
 * Fri 22 May 2026; ABI-stable from v1.0.0 onward per the compatibility
 * policy in ABI_SPEC.md.
 *
 * Licensed under LGPL-3.0-or-later, matching the iDaVIE project.
 */

#ifndef IDAVIE_ABI_H
#define IDAVIE_ABI_H

#include <stdint.h>
#include <stddef.h>

/* --------------------------------------------------------------------------
 * Calling convention and visibility
 *
 * IDAVIE_API is the umbrella macro every exported function must carry.
 * It encodes (a) symbol visibility and (b) calling convention.
 *
 * Plug-ins are expected to compile with -fvisibility=hidden on Linux/macOS;
 * only IDAVIE_API-tagged symbols escape the .so. On Windows, plug-ins
 * compiling the DLL must #define IDAVIE_BUILDING_DLL.
 * -------------------------------------------------------------------------- */
#if defined(_WIN32)
#  if defined(IDAVIE_BUILDING_DLL)
#    define IDAVIE_API __declspec(dllexport) __cdecl
#  else
#    define IDAVIE_API __declspec(dllimport) __cdecl
#  endif
#else
#  define IDAVIE_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

/* --------------------------------------------------------------------------
 * ABI versioning
 *
 * MAJOR : breaking changes (struct layout, function signature, enum values).
 * MINOR : backwards-compatible additions (new functions, new status codes
 *         at the end of the enum).
 * PATCH : documentation or non-behavioural fixes.
 *
 * Hosts MUST call idavie_abi_version() immediately after loading a plug-in
 * and refuse to bind any other symbol if the MAJOR field does not match
 * the host's compile-time IDAVIE_ABI_VERSION_MAJOR.
 * -------------------------------------------------------------------------- */
#define IDAVIE_ABI_VERSION_MAJOR 0
#define IDAVIE_ABI_VERSION_MINOR 1
#define IDAVIE_ABI_VERSION_PATCH 0

#define IDAVIE_ABI_VERSION_PACK(major, minor, patch) \
    ((((uint32_t)(major)) << 16) | (((uint32_t)(minor)) << 8) | ((uint32_t)(patch)))

#define IDAVIE_ABI_VERSION \
    IDAVIE_ABI_VERSION_PACK(IDAVIE_ABI_VERSION_MAJOR, \
                            IDAVIE_ABI_VERSION_MINOR, \
                            IDAVIE_ABI_VERSION_PATCH)

/* Returns the ABI version the plug-in was built against. */
IDAVIE_API uint32_t idavie_abi_version(void);

/* --------------------------------------------------------------------------
 * Status / error model
 *
 * Every function in this ABI returns idavie_status_t. There are no
 * side-channel int* status outputs. For human-readable context, callers
 * may consult idavie_last_error_message() on the same thread immediately
 * after a non-OK return.
 *
 * Status codes are stable within a major version. New codes may be added
 * at the end of the enum in a minor version.
 * -------------------------------------------------------------------------- */
typedef int32_t idavie_status_t;

enum {
    IDAVIE_OK                       = 0,
    IDAVIE_ERR_INVALID_ARGUMENT     = 1,
    IDAVIE_ERR_NULL_HANDLE          = 2,
    IDAVIE_ERR_OUT_OF_MEMORY        = 3,
    IDAVIE_ERR_IO                   = 4,
    IDAVIE_ERR_NOT_FOUND            = 5,
    IDAVIE_ERR_UNSUPPORTED          = 6,
    IDAVIE_ERR_FORMAT               = 7,
    IDAVIE_ERR_OUT_OF_RANGE         = 8,
    IDAVIE_ERR_CANCELLED            = 9,
    IDAVIE_ERR_VERSION_MISMATCH     = 10,
    IDAVIE_ERR_INTERNAL             = 99
};

/* Thread-local human-readable message describing the most recent non-OK
 * return on the current thread. Pointer is valid until the next ABI call
 * on the same thread. Never NULL; returns "" if no error is recorded. */
IDAVIE_API const char* idavie_last_error_message(void);

/* --------------------------------------------------------------------------
 * Memory ownership
 *
 * Buffers returned by the ABI are described by idavie_buffer_t. They are
 * owned by the plug-in until the caller passes them to idavie_free().
 * There is exactly one free function in the ABI; plug-ins MUST route all
 * frees through it so heap mismatches across DLL boundaries are
 * impossible.
 *
 * For functions where the caller can allocate the destination buffer,
 * the ABI prefers that pattern: pass in (T* out, int64_t capacity,
 * int64_t* written). The buffer-descriptor form is used only when the
 * length is not known until the call returns.
 * -------------------------------------------------------------------------- */
typedef struct {
    void*   data;        /* allocated by the plug-in */
    int64_t length;      /* number of elements */
    uint32_t elem_size;  /* size in bytes of one element */
    uint32_t reserved;   /* must be zero */
} idavie_buffer_t;

IDAVIE_API void idavie_free(idavie_buffer_t buf);

/* --------------------------------------------------------------------------
 * Progress and cancellation
 *
 * Long-running operations accept an optional progress callback. Return
 * a non-zero value from the callback to request cancellation; the
 * operation will then return IDAVIE_ERR_CANCELLED at its next safe
 * checkpoint. NULL is permitted everywhere a callback is accepted.
 * -------------------------------------------------------------------------- */
typedef int32_t (*idavie_progress_fn)(double fraction_done, void* user_data);

/* --------------------------------------------------------------------------
 * Opaque handle types
 *
 * Each module exposes one or more opaque handle types. Callers may store,
 * compare, and pass these pointers but MUST NOT dereference them or
 * compute sizeof. Construction goes through a module-specific *_open or
 * *_create function; destruction through a single *_close per type.
 * -------------------------------------------------------------------------- */
typedef struct idavie_fits      idavie_fits_t;       /* FITS file session   */
typedef struct idavie_ast       idavie_ast_t;        /* AST WCS frame set   */
typedef struct idavie_analysis  idavie_analysis_t;   /* Data analysis ctx   */

/* --------------------------------------------------------------------------
 * Common data structures (blittable, fixed layout)
 *
 * All structs that cross the ABI are blittable and have static_asserted
 * sizes. The C# side mirrors these with [StructLayout(Sequential, Pack=8)]
 * plus a Marshal.SizeOf assertion at startup.
 * -------------------------------------------------------------------------- */
typedef struct {
    int64_t min_x, max_x;
    int64_t min_y, max_y;
    int64_t min_z, max_z;
    int16_t mask_value;
    uint8_t _padding[6];      /* explicit; sizeof must equal 56 */
} idavie_source_info_t;

typedef struct {
    int64_t min_x, max_x;
    int64_t min_y, max_y;
    int64_t min_z, max_z;
    int64_t num_voxels;
    double  centroid_x, centroid_y, centroid_z;
    double  flux_sum;
    double  flux_peak;
    double  channel_vsys;
    double  channel_w20;
    double  velocity_vsys;
    double  velocity_w20;
    idavie_buffer_t spectral_profile;   /* float, length == max_z - min_z + 1 */
} idavie_source_stats_t;

/* Layout assertions — both sides of the ABI must compile against the
 * same byte layout, or refuse to link. */
#if defined(__cplusplus) && __cplusplus >= 201103L
  static_assert(sizeof(idavie_source_info_t)  == 56,  "ABI: idavie_source_info_t size drift");
  static_assert(sizeof(idavie_source_stats_t) == 128, "ABI: idavie_source_stats_t size drift");
  static_assert(sizeof(idavie_buffer_t)       == 24,  "ABI: idavie_buffer_t size drift");
#elif defined(__STDC_VERSION__) && __STDC_VERSION__ >= 201112L
  _Static_assert(sizeof(idavie_source_info_t)  == 56,  "ABI: idavie_source_info_t size drift");
  _Static_assert(sizeof(idavie_source_stats_t) == 128, "ABI: idavie_source_stats_t size drift");
  _Static_assert(sizeof(idavie_buffer_t)       == 24,  "ABI: idavie_buffer_t size drift");
#endif

/* --------------------------------------------------------------------------
 * FITS plug-in surface (subset shown — full surface in MIGRATION.md)
 * -------------------------------------------------------------------------- */
IDAVIE_API idavie_status_t idavie_fits_open(const char* path, idavie_fits_t** out);
IDAVIE_API idavie_status_t idavie_fits_close(idavie_fits_t* h);

IDAVIE_API idavie_status_t idavie_fits_get_hdu_count(idavie_fits_t* h, int32_t* out_count);
IDAVIE_API idavie_status_t idavie_fits_set_current_hdu(idavie_fits_t* h, int32_t hdu_index);
IDAVIE_API idavie_status_t idavie_fits_get_image_dims(idavie_fits_t* h, int32_t* out_dims);
IDAVIE_API idavie_status_t idavie_fits_get_image_size(idavie_fits_t* h, int64_t* sizes_out, int32_t capacity);

IDAVIE_API idavie_status_t idavie_fits_read_subimage_float(
    idavie_fits_t*  h,
    const int64_t*  start_pixel,    /* length == ndim */
    const int64_t*  end_pixel,      /* length == ndim */
    int32_t         ndim,
    idavie_buffer_t* out_data,
    idavie_progress_fn progress, void* progress_user);

IDAVIE_API idavie_status_t idavie_fits_read_subimage_int16(
    idavie_fits_t*  h,
    const int64_t*  start_pixel,
    const int64_t*  end_pixel,
    int32_t         ndim,
    idavie_buffer_t* out_data,
    idavie_progress_fn progress, void* progress_user);

/* --------------------------------------------------------------------------
 * AST / WCS plug-in surface (subset)
 * -------------------------------------------------------------------------- */
IDAVIE_API idavie_status_t idavie_ast_create_from_fits_header(
    const char* header, idavie_ast_t** out, double rest_freq_hz);

IDAVIE_API idavie_status_t idavie_ast_close(idavie_ast_t* h);

IDAVIE_API idavie_status_t idavie_ast_transform_3d(
    idavie_ast_t* h,
    double  x_in,  double  y_in,  double  z_in,
    int32_t forward,
    double* x_out, double* y_out, double* z_out);

IDAVIE_API idavie_status_t idavie_ast_format_axis(
    idavie_ast_t* h, int32_t axis, double value,
    char* out_buf, int32_t out_buf_capacity, int32_t* out_written);

/* --------------------------------------------------------------------------
 * Data analysis plug-in surface (subset)
 * -------------------------------------------------------------------------- */
IDAVIE_API idavie_status_t idavie_analysis_find_max_min(
    const float* data, int64_t n_elements,
    float* out_max, float* out_min);

IDAVIE_API idavie_status_t idavie_analysis_find_stats(
    const float* data, int64_t n_elements,
    float* out_max, float* out_min,
    float* out_mean, float* out_stddev);

IDAVIE_API idavie_status_t idavie_analysis_get_histogram(
    const float* data, int64_t n_elements,
    int32_t num_bins, float min_value, float max_value,
    idavie_buffer_t* out_histogram);

IDAVIE_API idavie_status_t idavie_analysis_get_masked_sources(
    const int16_t* mask, int64_t dim_x, int64_t dim_y, int64_t dim_z,
    idavie_buffer_t* out_sources);   /* element type: idavie_source_info_t */

IDAVIE_API idavie_status_t idavie_analysis_get_source_stats(
    const float* data, const int16_t* mask,
    int64_t dim_x, int64_t dim_y, int64_t dim_z,
    idavie_source_info_t source,
    idavie_ast_t* wcs,                /* nullable */
    idavie_source_stats_t* out_stats);

#ifdef __cplusplus
} /* extern "C" */
#endif

#endif /* IDAVIE_ABI_H */
