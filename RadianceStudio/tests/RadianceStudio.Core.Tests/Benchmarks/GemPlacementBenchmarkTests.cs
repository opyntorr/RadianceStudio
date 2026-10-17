namespace RadianceStudio.Core.Tests.Benchmarks;

using System;
using System.Diagnostics;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Services;
using Xunit;

public class GemPlacementBenchmarkTests
{
    private readonly IGemEngine _engine = new GemEngine();

    [Fact]
    public void Benchmark_CalculatePlacements_500Stones_ExecutesInUnder10Milliseconds()
    {
        // 500 stones with 2.0mm gem size and 0.5mm spacing requires 1250mm usable length
        // Create Helix with ample length
        var helix = new HelixCurve3D(Point3D.Origin, 25.0, 8.0, 20.0);
        double gemSize = 2.0;
        double spacing = 0.5;

        // JIT warm-up
        for (int i = 0; i < 3; i++)
        {
            _ = _engine.CalculatePlacements(helix, null, GemCutType.RoundBrilliant, gemSize, spacing);
        }

        var sw = Stopwatch.StartNew();
        var placements = _engine.CalculatePlacements(helix, null, GemCutType.RoundBrilliant, gemSize, spacing);
        sw.Stop();

        Assert.True(placements.Count >= 500, $"Expected >= 500 stones, got {placements.Count}");
        Assert.True(sw.ElapsedMilliseconds < 10,
            $"Expected 500-stone placement calculation to execute in < 10 ms, but took {sw.ElapsedMilliseconds} ms (ticks: {sw.ElapsedTicks})");
    }
}
