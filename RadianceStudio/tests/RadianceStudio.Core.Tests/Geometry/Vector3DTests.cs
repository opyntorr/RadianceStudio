namespace RadianceStudio.Core.Tests.Geometry;

using System;
using RadianceStudio.Core.Geometry;
using Xunit;

public class Vector3DTests
{
    [Fact]
    public void VectorArithmetic_OperationsAreCorrect()
    {
        var v1 = new Vector3D(1, 2, 3);
        var v2 = new Vector3D(4, 5, 6);

        var sum = v1 + v2;
        var diff = v2 - v1;
        var scaled = v1 * 2.0;
        var divided = v2 / 2.0;
        var negated = -v1;

        Assert.Equal(new Vector3D(5, 7, 9), sum);
        Assert.Equal(new Vector3D(3, 3, 3), diff);
        Assert.Equal(new Vector3D(2, 4, 6), scaled);
        Assert.Equal(new Vector3D(2, 2.5, 3), divided);
        Assert.Equal(new Vector3D(-1, -2, -3), negated);
    }

    [Fact]
    public void DotAndCrossProducts_FollowRightHandRule()
    {
        var x = Vector3D.UnitX;
        var y = Vector3D.UnitY;
        var z = Vector3D.UnitZ;

        Assert.Equal(1.0, x.Dot(x));
        Assert.Equal(0.0, x.Dot(y));
        Assert.Equal(0.0, x.Dot(z));

        Assert.Equal(z, x.Cross(y));
        Assert.Equal(x, y.Cross(z));
        Assert.Equal(y, z.Cross(x));
        Assert.Equal(-z, y.Cross(x));
    }

    [Fact]
    public void Normalize_HandlesZeroAndNonZero()
    {
        var v = new Vector3D(3, 4, 0);
        Assert.Equal(5.0, v.Length);
        Assert.Equal(25.0, v.LengthSquared);

        var norm = v.Normalize();
        Assert.Equal(1.0, norm.Length, 6);
        Assert.Equal(0.6, norm.X, 6);
        Assert.Equal(0.8, norm.Y, 6);
        Assert.True(norm.IsNormalized());

        var zero = Vector3D.Zero.Normalize();
        Assert.Equal(Vector3D.Zero, zero);
        Assert.True(zero.IsZero());
    }
}
