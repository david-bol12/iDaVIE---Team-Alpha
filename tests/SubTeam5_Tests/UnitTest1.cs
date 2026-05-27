using DataFeatures;

namespace SubTeam5_Tests;

public class FeatureStatisticsTests
{
    private static readonly FeatureColor DefaultColor = new FeatureColor(1f, 1f, 1f);

    // TEST 1: Feature.Center lies inside the feature's bounding box
    [TestCase(0f, 10f, 0f, 10f, 0f, 10f)]
    [TestCase(1f, 99f, 1f, 99f, 1f, 99f)]
    [TestCase(-5f, 5f, -5f, 5f, -5f, 5f)]
    [TestCase(10f, 50f, 10f, 50f, 10f, 50f)]
    [TestCase(0f, 1f, 0f, 1f, 0f, 1f)]
    public void Feature_Center_IsInsideBoundingBox(
        float minX, float maxX,
        float minY, float maxY,
        float minZ, float maxZ)
    {
        var feature = new Feature(
            new Vec3(minX, minY, minZ), new Vec3(maxX, maxY, maxZ),
            DefaultColor, "test", "", 0, 1, Array.Empty<string>(), true);

        Assert.That(feature.Center.X, Is.GreaterThanOrEqualTo(feature.CornerMin.X));
        Assert.That(feature.Center.X, Is.LessThanOrEqualTo(feature.CornerMax.X));
        Assert.That(feature.Center.Y, Is.GreaterThanOrEqualTo(feature.CornerMin.Y));
        Assert.That(feature.Center.Y, Is.LessThanOrEqualTo(feature.CornerMax.Y));
        Assert.That(feature.Center.Z, Is.GreaterThanOrEqualTo(feature.CornerMin.Z));
        Assert.That(feature.Center.Z, Is.LessThanOrEqualTo(feature.CornerMax.Z));
    }

    // TEST 2: Flux values stored in Feature.RawData are non-negative
    [TestCase(new double[] { 1.0, 2.0, 3.0 })]
    [TestCase(new double[] { 0.0, 0.0, 0.0 })]
    [TestCase(new double[] { 100.0, 50.0, 200.0 })]
    [TestCase(new double[] { 0.001, 0.002, 0.003 })]
    [TestCase(new double[] { 999.9 })]
    public void Feature_Flux_IsNonNegative(double[] fluxValues)
    {
        var rawData = fluxValues.Select(v => v.ToString("G")).ToArray();
        var feature = new Feature(
            new Vec3(0f, 0f, 0f), new Vec3(10f, 10f, 10f),
            DefaultColor, "test", "", 0, 1, rawData, true);

        var parsed = feature.RawData.Select(double.Parse).ToList();
        double sum = parsed.Sum();
        double peak = parsed.Max();

        Assert.That(sum, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(peak, Is.GreaterThanOrEqualTo(0.0));
    }

    // TEST 3: Feature Z-extent representing W20 is at least as wide as W50
    // On any Gaussian spectral profile, the line measured at 20% of peak is
    // always wider than at 50% of peak.
    [TestCase(100.0)]
    [TestCase(500.0)]
    [TestCase(1.0)]
    [TestCase(9999.0)]
    [TestCase(0.5)]
    public void Feature_W20_Width_AtLeast_W50_Width(double peakFlux)
    {
        float w20 = 2.0f * (float)Math.Sqrt(-2.0 * Math.Log(0.20));
        float w50 = 2.0f * (float)Math.Sqrt(-2.0 * Math.Log(0.50));

        var featureW20 = new Feature(
            new Vec3(0f, 0f, -w20 / 2f), new Vec3(0f, 0f, w20 / 2f),
            DefaultColor, "W20", "", 0, 1, new[] { peakFlux.ToString("G") }, true);

        var featureW50 = new Feature(
            new Vec3(0f, 0f, -w50 / 2f), new Vec3(0f, 0f, w50 / 2f),
            DefaultColor, "W50", "", 0, 2, new[] { peakFlux.ToString("G") }, true);

        Assert.That(featureW20.Size.Z, Is.GreaterThanOrEqualTo(featureW50.Size.Z));
    }
}