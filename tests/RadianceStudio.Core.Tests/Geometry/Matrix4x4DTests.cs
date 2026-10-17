namespace RadianceStudio.Core.Tests.Geometry;

using System;
using RadianceStudio.Core.Geometry;
using Xunit;

public class Matrix4x4DTests
{
    [Fact]
    public void IdentityMatrix_HasExpectedElementsAndDeterminant()
    {
        var identity = Matrix4x4D.Identity;

        Assert.Equal(1.0, identity.M00);
        Assert.Equal(0.0, identity.M01);
        Assert.Equal(1.0, identity.M11);
        Assert.Equal(1.0, identity.M22);
        Assert.Equal(1.0, identity.M33);
        Assert.Equal(1.0, identity.Determinant3x3(), 6);
    }

    [Fact]
    public void RowMajorLayout_MatchesIndexerAndArray()
    {
        double[] expected = new double[16];
        for (int i = 0; i < 16; i++) expected[i] = i + 1.0;

        var mat = new Matrix4x4D(expected);
        double[] array = mat.ToRowMajorArray();

        Assert.Equal(expected.Length, array.Length);
        for (int i = 0; i < 16; i++)
        {
            Assert.Equal(expected[i], array[i]);
            int row = i / 4;
            int col = i % 4;
            Assert.Equal(expected[i], mat[row, col]);
        }
    }

    [Fact]
    public void Determinant3x3_ComputesCorrectlyForScaleAndRotation()
    {
        var scale = Matrix4x4D.CreateScale(2.0, 3.0, 4.0);
        Assert.Equal(24.0, scale.Determinant3x3(), 6);

        var rot = Matrix4x4D.CreateRotationZDegrees(45.0);
        Assert.Equal(1.0, rot.Determinant3x3(), 6);
    }

    [Fact]
    public void Transform_PointAndVector_BehavesCorrectly()
    {
        var trans = Matrix4x4D.CreateTranslation(10, 20, 30);
        var pt = new Point3D(1, 2, 3);
        var vec = new Vector3D(1, 2, 3);

        var ptTransformed = trans.Transform(pt);
        var vecTransformed = trans.Transform(vec);

        // Point is translated
        Assert.Equal(11.0, ptTransformed.X, 6);
        Assert.Equal(22.0, ptTransformed.Y, 6);
        Assert.Equal(33.0, ptTransformed.Z, 6);

        // Vector is invariant to translation
        Assert.Equal(1.0, vecTransformed.X, 6);
        Assert.Equal(2.0, vecTransformed.Y, 6);
        Assert.Equal(3.0, vecTransformed.Z, 6);
    }

    [Fact]
    public void Multiply_CombinesTransformationsCorrectly()
    {
        var trans = Matrix4x4D.CreateTranslation(5, 0, 0);
        var scale = Matrix4x4D.CreateScale(2, 2, 2);

        // T * S: scales first, then translates
        var combined = trans * scale;
        var pt = new Point3D(1, 1, 1);
        var result = combined.Transform(pt);

        Assert.Equal(7.0, result.X, 6); // 1*2 + 5 = 7
        Assert.Equal(2.0, result.Y, 6); // 1*2 + 0 = 2
        Assert.Equal(2.0, result.Z, 6); // 1*2 + 0 = 2
    }

    [Fact]
    public void CreatePlacement_CorrectlyMapsBasisAndPosition()
    {
        var frame = OrthonormalFrame.Create(Vector3D.UnitY, Vector3D.UnitZ);
        var pos = new Point3D(10, 20, 30);
        double gemSize = 2.0;
        double aspectRatio = 1.5;
        double rotationAngle = 0.0;

        var placement = Matrix4x4D.CreatePlacement(frame, pos, gemSize, aspectRatio, rotationAngle);

        // Origin maps to position
        var originTransformed = placement.Transform(Point3D.Origin);
        Assert.Equal(pos.X, originTransformed.X, 6);
        Assert.Equal(pos.Y, originTransformed.Y, 6);
        Assert.Equal(pos.Z, originTransformed.Z, 6);

        // +Z (normal unit vector) maps to Normal * gemSize
        var zAxis = placement.Transform(new Point3D(0, 0, 1)) - originTransformed;
        Assert.Equal(gemSize, zAxis.Length, 6);
        Assert.Equal(1.0, Vector3D.Dot(zAxis.Normalize(), frame.Normal), 6);

        // +Y (major axis) maps to Tangent * (gemSize * aspectRatio)
        var yAxis = placement.Transform(new Point3D(0, 1, 0)) - originTransformed;
        Assert.Equal(gemSize * aspectRatio, yAxis.Length, 6);
        Assert.Equal(1.0, Vector3D.Dot(yAxis.Normalize(), frame.Tangent), 6);
    }
}
