// Smoke test for data_analysis_tool.cpp entry points.
// Scope: see LAYOUT.md, "test_data_analysis_tool" section.
//
// Feeds {1, 2, 3, 4, 5} into FindMaxMin and FindStats and asserts the
// known answers. The point of this binary is to prove the third native TU's
// symbols are exported on Linux — one real call per function is enough.

#include <cstdio>
#include <cstdint>
#include <cmath>

extern "C" {
    int FindMaxMin(const float *, int64_t, float *, float *);
    int FindStats(const float *, int64_t, float *, float *, float *, float *);
}

static int fail(const char *what) {
    std::fprintf(stderr, "[FAIL] %s\n", what);
    return 1;
}

static bool nearly(float a, float b, float eps) {
    return std::fabs(a - b) < eps;
}

int main(int argc, char **argv) {
    (void)argc;
    (void)argv;

    // {1,2,3,4,5}: max=5, min=1, mean=3.
    // FindStats uses sample stddev (N-1): sqrt(((N*sumSq) - sum^2) / (N*(N-1)))
    //   = sqrt((5*55 - 225) / 20) = sqrt(50/20) = sqrt(2.5) ~= 1.58114.
    const float data[] = {1.0f, 2.0f, 3.0f, 4.0f, 5.0f};
    const int64_t n = 5;

    float mx = 0, mn = 0;
    if (FindMaxMin(data, n, &mx, &mn) != 0) return fail("FindMaxMin nonzero return");
    if (!nearly(mx, 5.0f, 1e-5f))           return fail("FindMaxMin max != 5");
    if (!nearly(mn, 1.0f, 1e-5f))           return fail("FindMaxMin min != 1");
    std::printf("[ok] FindMaxMin: max=%g min=%g\n", mx, mn);

    float sMax = 0, sMin = 0, mean = 0, stddev = 0;
    if (FindStats(data, n, &sMax, &sMin, &mean, &stddev) != 0)
        return fail("FindStats nonzero return");
    if (!nearly(sMax, 5.0f, 1e-5f))                return fail("FindStats max != 5");
    if (!nearly(sMin, 1.0f, 1e-5f))                return fail("FindStats min != 1");
    if (!nearly(mean, 3.0f, 1e-5f))                return fail("FindStats mean != 3");
    if (!nearly(stddev, std::sqrt(2.5f), 1e-4f))   return fail("FindStats stddev != sqrt(2.5)");
    std::printf("[ok] FindStats: max=%g min=%g mean=%g stddev=%g\n",
                sMax, sMin, mean, stddev);

    return 0;
}
