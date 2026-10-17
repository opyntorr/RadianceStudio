namespace RadianceStudio.Core.Tests.Geometry;

using System;
using RadianceStudio.Core.Geometry;
using Xunit;

public class OrthonormalFrameTests
{
    [Fact]
    public void StandardBasis_CreatesCanonicalFrame()
    {
        var frame = OrthonormalFrame.Create(Vector3D.UnitY, Vector3D.UnitZ);

        Assert.True(frame.IsOrthonormal());
        Assert.Equal(1.0, frame.Determinant, 6);
        Assert.Equal(Vector3D.UnitY, frame.Tangent);
        Assert.Equal(Vector3D.UnitZ, frame.Normal);
        Assert.Equal(Vector3D.UnitX, frame.Binormal);
    }

    [Theory]
    [InlineData(1, 0, 0, 0, 0, 1)]
    [InlineData(0, 1, 0, 0, 0, 1)]
    [InlineData(1, 1, 0, 0, 0, 1)]
    [InlineData(1, 2, 3, 4, 5, 6)]
    [InlineData(-0.7071, 0.7071, 0, 0, 1, 1)]
    [InlineData(0.123, -0.456, 0.789, 0.987, 0.654, -0.321)]
    public void ArbitraryVectors_AlwaysProduceStrictlyOrthonormalRightHandedBasis(
        double tx, double ty, double tz,
        double nx, double ny, double nz)
    {
        var tangent = new Vector3D(tx, ty, tz);
        var normal = new Vector3D(nx, ny, nz);

        var frame = OrthonormalFrame.Create(tangent, normal);

        // Strict unit lengths
        Assert.Equal(1.0, frame.Tangent.Length, 6);
        Assert.Equal(1.0, frame.Normal.Length, 6);
        Assert.Equal(1.0, frame.Binormal.Length, 6);

        // Strict mutual orthogonality (< 10^-6)
        Assert.True(Math.Abs(Vector3D.Dot(frame.Tangent, frame.Normal)) < 1e-6);
        Assert.True(Math.Abs(Vector3D.Dot(frame.Tangent, frame.Binormal)) < 1e-6);
        Assert.True(Math.Abs(Vector3D.Dot(frame.Normal, frame.Binormal)) < 1e-6);

        // Determinant of [B T* N] must be strictly +1.0
        Assert.Equal(1.0, frame.Determinant, 6);
    }

    [Fact]
    public void FlipNormal_InvertsNormalWhilePreservingRightHandedness()
    {
        var tangent = new Vector3D(1, 1, 0);
        var normal = new Vector3D(0, 0, 1);

        var frameStandard = OrthonormalFrame.Create(tangent, normal, flipNormal: false);
        var frameFlipped = OrthonormalFrame.Create(tangent, normal, flipNormal: true);

        // Normal must be exactly negated
        Assert.Equal(-frameStandard.Normal.X, frameFlipped.Normal.X, 6);
        Assert.Equal(-frameStandard.Normal.Y, frameFlipped.Normal.Y, 6);
        Assert.Equal(-frameStandard.Normal.Z, frameFlipped.Normal.Z, 6);

        // Right-handed determinant remains strictly +1.0
        Assert.Equal(1.0, frameFlipped.Determinant, 6);
        Assert.True(frameFlipped.IsOrthonormal());
    }

    [Fact]
    public void CollinearVectors_ResolveRobustlyViaDeterministicFallback()
    {
        // Tangent and candidate normal parallel
        var tangent = new Vector3D(0, 0, 1);
        var candidateNormal = new Vector3D(0, 0, 1);

        var frame = OrthonormalFrame.Create(tangent, candidateNormal);

        Assert.True(frame.IsOrthonormal());
        Assert.Equal(1.0, frame.Determinant, 6);
        Assert.True(Math.Abs(Vector3D.Dot(frame.Tangent, frame.Normal)) < 1e-6);
    }

    [Fact]
    public void ZeroCandidateNormal_ResolvesRobustly()
    {
        var tangent = new Vector3D(1, 0, 0);
        var zeroNormal = Vector3D.Zero;

        var frame = OrthonormalFrame.Create(tangent, zeroNormal);

        Assert.True(frame.IsOrthonormal());
        Assert.Equal(1.0, frame.Determinant, 6);
    }

    [Fact]
    public void ZeroTangent_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            OrthonormalFrame.Create(Vector3D.Zero, Vector3D.UnitZ));
    }
}
