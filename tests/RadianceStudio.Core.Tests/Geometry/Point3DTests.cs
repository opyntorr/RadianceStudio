namespace RadianceStudio.Core.Tests.Geometry;

using System;
using RadianceStudio.Core.Geometry;
using Xunit;

public class Point3DTests
{
    [Fact]
    public void PointArithmetic_PointDisplacementAndDistance()
    {
        var p1 = new Point3D(1, 2, 3);
        var v = new Vector3D(4, 5, 6);

        var p2 = p1 + v;
        Assert.Equal(new Point3D(5, 7, 9), p2);

        var diff = p2 - p1;
        Assert.Equal(v, diff);

        var pSub = p2 - v;
        Assert.Equal(p1, pSub);

        double dist = p1.DistanceTo(new Point3D(4, 6, 3));
        Assert.Equal(5.0, dist, 6);
        Assert.Equal(25.0, p1.DistanceSquaredTo(new Point3D(4, 6, 3)), 6);
    }
}
