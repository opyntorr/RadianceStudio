namespace RadianceStudio.Core.Tests.Cutters;

using System;
using System.Collections.Generic;
using System.Linq;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;
using Rhino.Geometry;
using Xunit;

public class CutterEngineTests
{
    #region Task 3.1 & 3.2: 13 Gem Cuts & 2-Manifold Validation

    [Theory]
    [InlineData(GemCutType.RoundBrilliant)]
    [InlineData(GemCutType.Princess)]
    [InlineData(GemCutType.Oval)]
    [InlineData(GemCutType.Pear)]
    [InlineData(GemCutType.Emerald)]
    [InlineData(GemCutType.Marquise)]
    [InlineData(GemCutType.Baguette)]
    [InlineData(GemCutType.Cushion)]
    [InlineData(GemCutType.Asscher)]
    [InlineData(GemCutType.Radiant)]
    [InlineData(GemCutType.Heart)]
    [InlineData(GemCutType.Trilliant)]
    [InlineData(GemCutType.TaperedBaguette)]
    public void CreateGemCutterMesh_SupportsAll13GemCuts_AndGeneratesWatertightManifold(GemCutType cutType)
    {
        var gem = GemDimensions.GetDefault(cutType, 5.0);
        var parameters = new CutterParameters
        {
            GirdleClearanceMm = 0.08,
            SeatAngle = CutterSeatStyle.Degree70,
            CrownHeightExtensionMm = 1.5,
            PavilionOffsetPercent = 5.0,
            LightHoleDiameterRatio = 0.50,
            LightHoleDepthExtensionMm = 2.0,
            TaperShaft = false
        };

        var mesh = CutterEngine.CreateGemCutterMesh(gem, parameters);

        Assert.NotNull(mesh);
        Assert.Equal(cutType, mesh.CutType);
        Assert.True(mesh.Vertices.Count > 0);
        Assert.True(mesh.Faces.Count > 0);
        AssertWatertight2Manifold(mesh);
    }

    #endregion

    #region Task 3.3: Invariance Checks (Girdle Clearance)

    [Theory]
    [InlineData(GemCutType.RoundBrilliant, 0.05)]
    [InlineData(GemCutType.RoundBrilliant, 0.08)]
    [InlineData(GemCutType.RoundBrilliant, 0.15)]
    [InlineData(GemCutType.Princess, 0.08)]
    [InlineData(GemCutType.Oval, 0.10)]
    [InlineData(GemCutType.Emerald, 0.12)]
    public void Cutter_OuterBoundsStrictlyEncompassGemGirdlePlusClearance(GemCutType cutType, double clearanceMm)
    {
        double diameter = 6.0;
        var gem = GemDimensions.GetDefault(cutType, diameter);
        var parameters = new CutterParameters
        {
            GirdleClearanceMm = clearanceMm
        };

        var cutterMesh = CutterEngine.CreateGemCutterMesh(gem, parameters);
        var (cMin, cMax) = cutterMesh.GetBounds();

        // Sample girdle outline to establish reference gem girdle bounds
        var girdlePoints = GemGirdleEvaluator.SampleGirdleOutline(cutType, gem, 128);
        double gemMinX = girdlePoints.Min(p => p.X);
        double gemMaxX = girdlePoints.Max(p => p.X);
        double gemMinY = girdlePoints.Min(p => p.Y);
        double gemMaxY = girdlePoints.Max(p => p.Y);

        const double tol = 1e-4;
        Assert.True(cMax.X >= gemMaxX + clearanceMm - tol,
            $"Cutter MaxX ({cMax.X:F4}) must encompass gem MaxX ({gemMaxX:F4}) + clearance ({clearanceMm:F4}).");
        Assert.True(cMin.X <= gemMinX - clearanceMm + tol,
            $"Cutter MinX ({cMin.X:F4}) must encompass gem MinX ({gemMinX:F4}) - clearance ({clearanceMm:F4}).");
        Assert.True(cMax.Y >= gemMaxY + clearanceMm - tol,
            $"Cutter MaxY ({cMax.Y:F4}) must encompass gem MaxY ({gemMaxY:F4}) + clearance ({clearanceMm:F4}).");
        Assert.True(cMin.Y <= gemMinY - clearanceMm + tol,
            $"Cutter MinY ({cMin.Y:F4}) must encompass gem MinY ({gemMinY:F4}) - clearance ({clearanceMm:F4}).");
    }

    #endregion

    #region Task 3.4: Piercing Bounds

    [Theory]
    [InlineData(GemCutType.RoundBrilliant, 1.0, 1.5)]
    [InlineData(GemCutType.Princess, 1.5, 2.0)]
    [InlineData(GemCutType.Oval, 2.0, 2.5)]
    [InlineData(GemCutType.Heart, 1.2, 1.8)]
    public void Cutter_PiercingBounds_StrictlyExceedGemTableAndCulet(
        GemCutType cutType,
        double crownExtMm,
        double lightHoleExtMm)
    {
        var gem = GemDimensions.GetDefault(cutType, 5.0);
        var parameters = new CutterParameters
        {
            CrownHeightExtensionMm = crownExtMm,
            LightHoleDepthExtensionMm = lightHoleExtMm
        };

        var cutterMesh = CutterEngine.CreateGemCutterMesh(gem, parameters);
        var (cMin, cMax) = cutterMesh.GetBounds();

        double g = gem.DiameterMm * (gem.GirdleThicknessPercent / 100.0);
        double hc = gem.DiameterMm * (gem.CrownHeightPercent / 100.0);
        double hp = gem.DiameterMm * (gem.PavilionDepthPercent / 100.0);

        double expectedGemTableZ = (g * 0.5) + hc;
        double expectedGemCuletZ = -((g * 0.5) + hp);

        // Cutter top Z must be strictly above table Z
        Assert.True(cMax.Z > expectedGemTableZ,
            $"Cutter top Z ({cMax.Z:F3}) must exceed gem table Z ({expectedGemTableZ:F3}).");
        Assert.Equal(expectedGemTableZ + crownExtMm, cMax.Z, 3);

        // Cutter bottom Z must be strictly below culet Z
        Assert.True(cMin.Z < expectedGemCuletZ,
            $"Cutter bottom Z ({cMin.Z:F3}) must be lower than gem culet Z ({expectedGemCuletZ:F3}).");
        Assert.Equal(expectedGemCuletZ - lightHoleExtMm, cMin.Z, 3);
    }

    #endregion

    #region Task 3.5: Batch Cutter Generation

    [Fact]
    public void CreateBatchCutterMesh_MultipleGems_ConcatenatesCorrectlyWithTransforms()
    {
        var gem1 = GemDimensions.GetDefault(GemCutType.RoundBrilliant, 4.0);
        var gem2 = GemDimensions.GetDefault(GemCutType.Princess, 4.0);
        var gem3 = GemDimensions.GetDefault(GemCutType.Oval, 4.0);

        var parameters = new CutterParameters();

        var gems = new List<(GemDimensions, Matrix4x4D)>
        {
            (gem1, Matrix4x4D.Identity),
            (gem2, Matrix4x4D.CreateTranslation(10.0, 0.0, 0.0)),
            (gem3, Matrix4x4D.CreateTranslation(20.0, 0.0, 0.0))
        };

        var singleMesh = CutterEngine.CreateGemCutterMesh(gem1, parameters);
        int singleV = singleMesh.Vertices.Count;
        int singleF = singleMesh.Faces.Count;

        var batchMesh = CutterEngine.CreateBatchCutterMesh(gems, parameters);

        Assert.NotNull(batchMesh);
        Assert.Equal(3 * singleV, batchMesh.Vertices.Count);
        Assert.Equal(3 * singleF, batchMesh.Faces.Count);

        // Verify face indices for instance 1 are in range [singleV, 2*singleV - 1]
        for (int f = singleF; f < 2 * singleF; f++)
        {
            var face = batchMesh.Faces[f];
            foreach (int idx in face)
            {
                Assert.InRange(idx, singleV, (2 * singleV) - 1);
            }
        }

        // Verify translated X coordinates
        var (bMin, bMax) = batchMesh.GetBounds();
        Assert.True(bMin.X < 0.0, "Batch bounds should start near first stone around X=0");
        Assert.True(bMax.X > 20.0, "Batch bounds should reach third stone at X=20");
    }

    [Fact]
    public void CreateBatchCutterMesh_EmptyList_ReturnsEmptyMesh()
    {
        var parameters = new CutterParameters();
        var batchMesh = CutterEngine.CreateBatchCutterMesh(Array.Empty<(GemDimensions, Matrix4x4D)>(), parameters);

        Assert.NotNull(batchMesh);
        Assert.Empty(batchMesh.Vertices);
        Assert.Empty(batchMesh.Faces);
    }

    #endregion

    #region Seat Angle Styles & Shaft Taper Variations

    [Theory]
    [InlineData(CutterSeatStyle.Degree70)]
    [InlineData(CutterSeatStyle.Degree90)]
    [InlineData(CutterSeatStyle.FlatShelf)]
    [InlineData(CutterSeatStyle.Conical)]
    public void CreateGemCutterMesh_SupportsAllSeatAngleStyles(CutterSeatStyle seatStyle)
    {
        var gem = GemDimensions.GetDefault(GemCutType.RoundBrilliant, 5.0);
        var parameters = new CutterParameters
        {
            SeatAngle = seatStyle
        };

        var mesh = CutterEngine.CreateGemCutterMesh(gem, parameters);
        Assert.NotNull(mesh);
        AssertWatertight2Manifold(mesh);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateGemCutterMesh_ShaftTaperOptions_MaintainWatertightManifold(bool taperShaft)
    {
        var gem = GemDimensions.GetDefault(GemCutType.RoundBrilliant, 5.0);
        var parameters = new CutterParameters
        {
            TaperShaft = taperShaft,
            LightHoleDiameterRatio = 0.50
        };

        var mesh = CutterEngine.CreateGemCutterMesh(gem, parameters);
        Assert.NotNull(mesh);
        AssertWatertight2Manifold(mesh);

        // Bottom perimeter vertices must strictly match the LightHoleDiameterRatio
        double zMin = mesh.Vertices.Min(v => v.Z);
        var bottomRingVertices = mesh.Vertices
            .Where(v => Math.Abs(v.Z - zMin) < 0.05 && (v.X != 0.0 || v.Y != 0.0))
            .Select(v => Math.Sqrt((v.X * v.X) + (v.Y * v.Y)))
            .ToList();

        Assert.NotEmpty(bottomRingVertices);
        double minRadius = bottomRingVertices.Min();
        double expectedRadius = (gem.DiameterMm * 0.5) * parameters.LightHoleDiameterRatio;
        Assert.Equal(expectedRadius, minRadius, 2);
    }

    #endregion

    #region Parameter Validation & Boundary Guardrails

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.26)]
    [InlineData(double.NaN)]
    public void CutterParameters_InvalidGirdleClearance_ThrowsValidationException(double clearance)
    {
        var p = new CutterParameters { GirdleClearanceMm = clearance };
        Assert.Throws<ArgumentOutOfRangeException>(() => p.Validate());
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.5)]
    [InlineData(double.NaN)]
    public void CutterParameters_InvalidCrownHeightExtension_ThrowsValidationException(double ext)
    {
        var p = new CutterParameters { CrownHeightExtensionMm = ext };
        Assert.Throws<ArgumentOutOfRangeException>(() => p.Validate());
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(105.0)]
    [InlineData(double.NaN)]
    public void CutterParameters_InvalidPavilionOffset_ThrowsValidationException(double offset)
    {
        var p = new CutterParameters { PavilionOffsetPercent = offset };
        Assert.Throws<ArgumentOutOfRangeException>(() => p.Validate());
    }

    [Theory]
    [InlineData(0.15)]
    [InlineData(0.85)]
    [InlineData(double.NaN)]
    public void CutterParameters_InvalidLightHoleDiameterRatio_ThrowsValidationException(double ratio)
    {
        var p = new CutterParameters { LightHoleDiameterRatio = ratio };
        Assert.Throws<ArgumentOutOfRangeException>(() => p.Validate());
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void CutterParameters_InvalidLightHoleDepthExtension_ThrowsValidationException(double ext)
    {
        var p = new CutterParameters { LightHoleDepthExtensionMm = ext };
        Assert.Throws<ArgumentOutOfRangeException>(() => p.Validate());
    }

    #endregion

    #region Headless Brep & Boolean Safety

    [Fact]
    public void CreateGemCutterBrep_Headless_ReturnsNullSafely()
    {
        var gem = GemDimensions.GetDefault(GemCutType.RoundBrilliant, 5.0);
        var parameters = new CutterParameters();

        var brep = CutterEngine.CreateGemCutterBrep(gem, parameters);
        if (!RailEngine.IsRhinoNativeAvailable)
        {
            Assert.Null(brep);
        }
        else
        {
            Assert.NotNull(brep);
            Assert.True(brep.IsValid);
        }
    }

    [Fact]
    public void ApplyCutterDifference_WhenNativeAvailable_PerformsSubtraction()
    {
        if (RailEngine.IsRhinoNativeAvailable)
        {
            var box = new Box(Plane.WorldXY, new Interval(-10, 10), new Interval(-10, 10), new Interval(-5, 5));
            var targetMetal = box.ToBrep();

            var gem = GemDimensions.GetDefault(GemCutType.RoundBrilliant, 4.0);
            var parameters = new CutterParameters();
            var cutter = CutterEngine.CreateGemCutterBrep(gem, parameters);

            if (cutter != null && targetMetal != null)
            {
                var result = CutterEngine.ApplyCutterDifference(targetMetal, cutter);
                Assert.NotNull(result);
            }
        }
    }

    #endregion

    #region Manifold Topology Assertion Helper

    /// <summary>
    /// Validates that a mesh is a strictly closed, watertight 2-manifold solid
    /// with Euler characteristic chi = V - E + F = 2, exactly 2 faces sharing each edge,
    /// and opposite winding across each shared edge.
    /// </summary>
    private static void AssertWatertight2Manifold(GemMesh mesh)
    {
        var vertices = mesh.Vertices;
        var faces = mesh.Faces;
        int V = vertices.Count;
        int F = faces.Count;

        Assert.True(V >= 4, "Mesh must have at least 4 vertices.");
        Assert.True(F >= 4, "Mesh must have at least 4 faces.");

        var edgeMap = new Dictionary<(int, int), List<(int faceIdx, int from, int to)>>();

        for (int f = 0; f < faces.Count; f++)
        {
            var face = faces[f];
            int n = face.Length;
            Assert.True(n >= 3, $"Face {f} has fewer than 3 vertices.");

            for (int i = 0; i < n; i++)
            {
                int u = face[i];
                int v = face[(i + 1) % n];

                Assert.True(u >= 0 && u < V, $"Vertex index {u} out of range in face {f}");
                Assert.True(v >= 0 && v < V, $"Vertex index {v} out of range in face {f}");
                Assert.NotEqual(u, v);

                var canonicalEdge = u < v ? (u, v) : (v, u);
                if (!edgeMap.TryGetValue(canonicalEdge, out var occurrences))
                {
                    occurrences = new List<(int, int, int)>();
                    edgeMap[canonicalEdge] = occurrences;
                }
                occurrences.Add((f, u, v));
            }
        }

        int E = edgeMap.Count;
        int eulerCharacteristic = V - E + F;
        Assert.Equal(2, eulerCharacteristic);

        foreach (var kvp in edgeMap)
        {
            var (edge, occurrences) = (kvp.Key, kvp.Value);
            Assert.True(occurrences.Count == 2,
                $"Edge ({edge.Item1}, {edge.Item2}) is shared by {occurrences.Count} faces (expected exactly 2). Face indices: {string.Join(", ", occurrences.Select(o => o.faceIdx))}");

            var occ1 = occurrences[0];
            var occ2 = occurrences[1];

            bool oppositeOrientation = (occ1.from == occ2.to) && (occ1.to == occ2.from);
            Assert.True(oppositeOrientation,
                $"Edge ({edge.Item1}, {edge.Item2}) has inconsistent orientation between face {occ1.faceIdx} ({occ1.from}->{occ1.to}) and face {occ2.faceIdx} ({occ2.from}->{occ2.to})");
        }
    }

    #endregion
}
