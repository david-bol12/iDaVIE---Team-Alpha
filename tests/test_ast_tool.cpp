// Smoke test for ast_tool.cpp entry points (Starlink AST wrapper).
// Scope: see LAYOUT.md, "test_ast_tool" section.
//
// Builds a synthetic FITS header describing a 4x4x4 RA/Dec/Freq cube,
// initialises an AST frame-set from it, and transforms the reference pixel
// to world coordinates. The synthetic header avoids depending on the
// bundled sample cube, which has no WCS keywords.

#include <cstdio>
#include <cmath>
#include <string>
#include "ast.h"

extern "C" {
    int  InitAstFrameSet(AstFrameSet **, const char *, double);
    int  Transform3D(AstFrameSet *, double, double, double, const int,
                     double *, double *, double *);
    void DeleteObject(AstFrameSet *);
    void AstEnd();
}

static int fail(const char *what) {
    std::fprintf(stderr, "[FAIL] %s\n", what);
    return 1;
}

// 4x4x4 cube at RA=10 deg, Dec=-20 deg, Freq=1.420405752 GHz (HI line),
// with CRPIX1=CRPIX2=CRPIX3=1 so pixel (1,1,1) is the reference point.
// astPutCards requires strict FITS format: each card exactly 80 chars,
// concatenated with no separators.
static std::string buildWcsHeader() {
    static const char *cards[] = {
        "SIMPLE  =                    T",
        "BITPIX  =                  -32",
        "NAXIS   =                    3",
        "NAXIS1  =                    4",
        "NAXIS2  =                    4",
        "NAXIS3  =                    4",
        "CTYPE1  = 'RA---SIN'",
        "CTYPE2  = 'DEC--SIN'",
        "CTYPE3  = 'FREQ    '",
        "CUNIT1  = 'deg     '",
        "CUNIT2  = 'deg     '",
        "CUNIT3  = 'Hz      '",
        "CRVAL1  =                 10.0",
        "CRVAL2  =                -20.0",
        "CRVAL3  =        1.420405752E9",
        "CRPIX1  =                  1.0",
        "CRPIX2  =                  1.0",
        "CRPIX3  =                  1.0",
        "CDELT1  =                -0.01",
        "CDELT2  =                 0.01",
        "CDELT3  =               1000.0",
        "END",
    };
    std::string buf;
    for (const char *c : cards) {
        std::string card(c);
        if (card.size() > 80) card.resize(80);
        else                  card.resize(80, ' ');
        buf += card;
    }
    return buf;
}

int main(int argc, char **argv) {
    (void)argc;
    (void)argv;

    std::string header = buildWcsHeader();

    AstFrameSet *fs = nullptr;
    if (InitAstFrameSet(&fs, header.c_str(), 0.0) != 0)
        return fail("InitAstFrameSet on synthetic RA/Dec/Freq header");
    std::printf("[ok] InitAstFrameSet built a frame-set\n");

    // Pixel (1,1,1) is the reference pixel; should map to CRVAL1/2/3.
    // AST returns angles in radians by default.
    double wx = 0, wy = 0, wz = 0;
    if (Transform3D(fs, 1.0, 1.0, 1.0, 1, &wx, &wy, &wz) != 0)
        return fail("Transform3D pixel->world");
    if (!std::isfinite(wx) || !std::isfinite(wy) || !std::isfinite(wz))
        return fail("Transform3D returned non-finite output");
    std::printf("[ok] pixel (1,1,1) -> (RA=%g rad, Dec=%g rad, Freq=%g Hz)\n",
                wx, wy, wz);

    if (wx < -3.15 || wx > 3.15) return fail("RA outside [-pi, pi]");
    if (wy < -1.58 || wy > 1.58) return fail("Dec outside [-pi/2, pi/2]");
    if (wz <= 0)                 return fail("Freq not positive");
    std::printf("[ok] WCS values within sensible ranges\n");

    DeleteObject(fs);
    AstEnd();
    std::printf("[ok] cleanup complete\n");
    return 0;
}
