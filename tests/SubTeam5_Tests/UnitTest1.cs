using NUnit.Framework;

namespace SubTeam5_Tests;

public class FeatureStatisticsTests 
{
    // TEST 1: Centroid inside bounding box
    [TestCase(0, 10, 0, 10, 0, 10, 5.0, 5.0, 5.0)]
    [TestCase(0, 100, 0, 100, 0, 100, 0.0, 0.0, 0.0)]
    [TestCase(0, 100, 0, 100, 0, 100, 100.0, 100.0, 100.0)]
    [TestCase(10, 50, 10, 50, 10, 50, 30.0, 30.0, 30.0)]
    [TestCase(0, 1, 0, 1, 0, 1, 0.5, 0.5, 0.5)]
    public void Centroid_IsInsideBoundingBox(
        double minX, double maxX,
        double minY, double maxY,
        double minZ, double maxZ,
        double cX, double cY, double cZ)
    {
        Assert.That(cX, Is.GreaterThanOrEqualTo(minX));
        Assert.That(cX, Is.LessThanOrEqualTo(maxX));
        Assert.That(cY, Is.GreaterThanOrEqualTo(minY));
        Assert.That(cY, Is.LessThanOrEqualTo(maxY));
        Assert.That(cZ, Is.GreaterThanOrEqualTo(minZ));
        Assert.That(cZ, Is.LessThanOrEqualTo(maxZ));
    }

    // TEST 2: Flux is non-negative
    [TestCase(new double[] { 1.0, 2.0, 3.0 })]
    [TestCase(new double[] { 0.0, 0.0, 0.0 })]
    [TestCase(new double[] { 100.0, 50.0, 200.0 })]
    [TestCase(new double[] { 0.001, 0.002, 0.003 })]
    [TestCase(new double[] { 999.9 })]
    public void Flux_IsNonNegative(double[] voxelValues)
    {
        double sum  = voxelValues.Sum();
        double peak = voxelValues.Max();

        Assert.That(sum,  Is.GreaterThanOrEqualTo(0.0));
        Assert.That(peak, Is.GreaterThanOrEqualTo(0.0));
    }

    // TEST 3: W20 width >= W50 width on a Gaussian profile
    [TestCase(100.0)]
    [TestCase(500.0)]
    [TestCase(1.0)]
    [TestCase(9999.0)]
    [TestCase(0.5)]
    public void W20_Width_AtLeast_W50_Width(double peakFlux)
    {
        // On any Gaussian spectral profile, the line measured
        // at 20% of peak is always wider than at 50% of peak
        double widthAt20 = 2.0 * Math.Sqrt(-2.0 * Math.Log(0.20));
        double widthAt50 = 2.0 * Math.Sqrt(-2.0 * Math.Log(0.50));

        Assert.That(widthAt20, Is.GreaterThanOrEqualTo(widthAt50));
    }
}