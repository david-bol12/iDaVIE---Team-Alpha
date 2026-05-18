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
