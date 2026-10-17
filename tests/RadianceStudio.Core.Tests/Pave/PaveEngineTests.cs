namespace RadianceStudio.Core.Tests.Pave;

using System;
using System.Collections.Generic;
using System.Linq;
using RadianceStudio.Core.Engines;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;
using Xunit;

public class PaveEngineTests
{
    private const double Tolerance = 1e-4;

    #region 1. Hexagonal Packing Density Verification

    [Fact]
    public void HexagonalPackingDensity_YieldsApproximately15PercentHigherStoneCountThanGrid()
    {
        var surface = new PlaneSurface3D(Vector3D.UnitZ);
        double uMin = 0.0, uMax = 20.0;
        double vMin = 0.0, vMax = 20.0;

        var hexParams = new PaveParameters
        {
            PatternType = PavePatternType.Hexagonal,
            StoneDiameterMm = 1.3,
            StoneSpacingMm = 0.15,
            BorderOffsetMm = 0.20,
            RelaxationIterations = 0 // Pure geometric packing density
        };

        var gridParams = new PaveParameters
        {
            PatternType = PavePatternType.Grid,
            StoneDiameterMm = 1.3,
            StoneSpacingMm = 0.15,
            BorderOffsetMm = 0.20,
            RelaxationIterations = 0
        };

        var hexStones = PaveEngine.CalculatePavePlacements(surface, uMin, uMax, vMin, vMax, hexParams);
        var gridStones = PaveEngine.CalculatePavePlacements(surface, uMin, uMax, vMin, vMax, gridParams);

        Assert.NotEmpty(hexStones);
        Assert.NotEmpty(gridStones);

        int hexCount = hexStones.Count;
        int gridCount = gridStones.Count;

        Assert.True(hexCount > gridCount, $"Hexagonal packing ({hexCount}) must yield more stones than Grid ({gridCount}).");

        double densityRatio = (double)hexCount / gridCount;
        // Theoretical ratio 2 / sqrt(3) ~ 1.1547 (15.47% higher).
        Assert.InRange(densityRatio, 1.10, 1.25);
    }

    #endregion

    #region 2. Girdle-to-Girdle Non-Collision Guarantee

    [Theory]
    [InlineData(PavePatternType.Hexagonal, 1.3, 0.15)]
    [InlineData(PavePatternType.Grid, 1.5, 0.20)]
    [InlineData(PavePatternType.OffsetLinear, 1.2, 0.10)]
    [InlineData(PavePatternType.Fishtail, 1.3, 0.15)]
    public void GirdleToGirdleNonCollisionGuarantee_MinimumCenterDistanceMaintained(
        PavePatternType pattern,
        double stoneDiameterMm,
        double stoneSpacingMm)
    {
        var surface = new PlaneSurface3D(Vector3D.UnitZ);
        double uMin = 0.0, uMax = 15.0;
        double vMin = 0.0, vMax = 15.0;

        var parameters = new PaveParameters
        {
            PatternType = pattern,
            StoneDiameterMm = stoneDiameterMm,
            StoneSpacingMm = stoneSpacingMm,
            BorderOffsetMm = 0.20,
            RelaxationIterations = 5
        };

        var stones = PaveEngine.CalculatePavePlacements(surface, uMin, uMax, vMin, vMax, parameters);
        Assert.True(stones.Count >= 4, "Must place at least 4 stones for collision test.");

        double minAllowedDistance = stoneDiameterMm + stoneSpacingMm - Tolerance;

        for (int i = 0; i < stones.Count; i++)
        {
            for (int j = i + 1; j < stones.Count; j++)
            {
                double dist = stones[i].Center.DistanceTo(stones[j].Center);
                Assert.True(dist >= minAllowedDistance,
                    $"Collision detected between stone {i} and {j}: distance {dist:F5} mm < minimum {minAllowedDistance:F5} mm.");
            }
        }
    }

    #endregion

    #region 3. Border Offset Invariance

    [Theory]
    [InlineData(0.10)]
    [InlineData(0.20)]
    [InlineData(0.35)]
    [InlineData(0.50)]
    public void BorderOffsetInvariance_AllStoneCentersAtLeastBorderOffsetFromBoundaries(double borderOffsetMm)
    {
        var surface = new PlaneSurface3D(Vector3D.UnitZ);
        double uMin = 2.0, uMax = 18.0;
        double vMin = 3.0, vMax = 17.0;

        var parameters = new PaveParameters
        {
            StoneDiameterMm = 1.3,
            StoneSpacingMm = 0.15,
            BorderOffsetMm = borderOffsetMm,
            RelaxationIterations = 8
        };

        var stones = PaveEngine.CalculatePavePlacements(surface, uMin, uMax, vMin, vMax, parameters);
        Assert.NotEmpty(stones);

        double requiredClearance = (parameters.StoneDiameterMm * 0.5) + borderOffsetMm;

        foreach (var s in stones)
        {
            double distLeft = s.U - uMin;
            double distRight = uMax - s.U;
            double distBottom = s.V - vMin;
            double distTop = vMax - s.V;

            Assert.True(distLeft >= requiredClearance - Tolerance,
                $"Stone at U={s.U:F4} violates left border clearance: {distLeft:F4} < {requiredClearance:F4}");
            Assert.True(distRight >= requiredClearance - Tolerance,
                $"Stone at U={s.U:F4} violates right border clearance: {distRight:F4} < {requiredClearance:F4}");
            Assert.True(distBottom >= requiredClearance - Tolerance,
                $"Stone at V={s.V:F4} violates bottom border clearance: {distBottom:F4} < {requiredClearance:F4}");
            Assert.True(distTop >= requiredClearance - Tolerance,
                $"Stone at V={s.V:F4} violates top border clearance: {distTop:F4} < {requiredClearance:F4}");
        }
    }

    #endregion

    #region 4. Bead Placement & Non-Collision With Girdles

    [Fact]
    public void BeadPlacement_PlacedAtInterstitialCentroids_WithoutCollidingWithGirdles()
    {
        var surface = new PlaneSurface3D(Vector3D.UnitZ);
        var parameters = new PaveParameters
        {
            PatternType = PavePatternType.Hexagonal,
            StoneDiameterMm = 1.3,
            StoneSpacingMm = 0.15,
            BorderOffsetMm = 0.20,
            BeadDiameterMm = 0.50,
            BeadHeightMm = 0.40
        };

        var stones = PaveEngine.CalculatePavePlacements(surface, 0.0, 10.0, 0.0, 10.0, parameters);
        Assert.True(stones.Count >= 3, "Requires at least 3 stones to verify interstitial beads.");

        var beads = PaveEngine.CalculateBeadPlacements(stones, parameters);
        Assert.NotEmpty(beads);

        double stoneRadius = parameters.StoneDiameterMm * 0.5;

        // Bead centers must be strictly outside all stone cylinders
        foreach (var bead in beads)
        {
            foreach (var stone in stones)
            {
                // In-plane horizontal distance (XY distance on flat surface)
                double dx = bead.Center.X - stone.Center.X;
                double dy = bead.Center.Y - stone.Center.Y;
                double inPlaneDist = Math.Sqrt(dx * dx + dy * dy);

                Assert.True(inPlaneDist >= stoneRadius - Tolerance,
                    $"Bead center ({bead.Center.X:F3}, {bead.Center.Y:F3}) collides with stone at ({stone.Center.X:F3}, {stone.Center.Y:F3}): in-plane dist {inPlaneDist:F4} < stone radius {stoneRadius:F4}");
            }

            Assert.NotEmpty(bead.AssociatedStoneIndices);
        }
    }

    [Fact]
    public void BeadPlacement_SingleStone_PlacesQuadrantGrains()
    {
        var surface = new PlaneSurface3D(Vector3D.UnitZ);
        var parameters = new PaveParameters
        {
            StoneDiameterMm = 1.3,
            StoneSpacingMm = 0.15,
            BeadDiameterMm = 0.50
        };

        var stones = PaveEngine.CalculatePavePlacements(surface, 0.0, 3.0, 0.0, 3.0, parameters);
        if (stones.Count == 0 || stones.Count > 1)
        {
            // Construct a single stone directly
            var singleStone = new PaveStonePlacement
            {
                Index = 0,
                Center = new Point3D(1.5, 1.5, 0.0),
                Normal = Vector3D.UnitZ,
                Tangent = Vector3D.UnitX,
                DiameterMm = 1.3,
                U = 1.5,
                V = 1.5
            };
            stones = new List<PaveStonePlacement> { singleStone };
        }

        var beads = PaveEngine.CalculateBeadPlacements(new[] { stones[0] }, parameters);
        Assert.Equal(4, beads.Count);

        double stoneRadius = stones[0].DiameterMm * 0.5;
        foreach (var b in beads)
        {
            double dist = stones[0].Center.DistanceTo(b.Center);
            Assert.True(dist >= stoneRadius, "Bead center must lie outside stone girdle.");
            Assert.Single(b.AssociatedStoneIndices);
            Assert.Equal(0, b.AssociatedStoneIndices[0]);
        }
    }

    #endregion

    #region 5. Curved Surface Relaxation

    [Fact]
    public void CurvedSurfaceRelaxation_CylinderSurface_MaintainsPositiveSpacing()
    {
        // Metric cylinder with R = 10 mm
        var cylinder = new CylinderSurface3D(radius: 10.0);
        double uMin = 0.0, uMax = 15.0; // Circumferential arc length
        double vMin = 0.0, vMax = 15.0; // Axial length

        var parameters = new PaveParameters
        {
            PatternType = PavePatternType.Hexagonal,
            StoneDiameterMm = 1.3,
            StoneSpacingMm = 0.15,
            BorderOffsetMm = 0.20,
            RelaxationIterations = 10
        };

        var stones = PaveEngine.CalculatePavePlacements(cylinder, uMin, uMax, vMin, vMax, parameters);
        Assert.NotEmpty(stones);

        double minAllowedDistance = parameters.StoneDiameterMm + parameters.StoneSpacingMm - Tolerance;

        for (int i = 0; i < stones.Count; i++)
        {
            for (int j = i + 1; j < stones.Count; j++)
            {
                double dist3D = stones[i].Center.DistanceTo(stones[j].Center);
                Assert.True(dist3D >= minAllowedDistance,
                    $"Cylinder stone collision between {i} and {j}: 3D distance {dist3D:F4} < {minAllowedDistance:F4}");
            }
        }
    }

    [Fact]
    public void CurvedSurfaceRelaxation_SpherePatch_MaintainsPositiveSpacing()
    {
        // Metric sphere with R = 15 mm (Gaussian curvature K = 1/225 > 0)
        var sphere = new SphereSurface3D(radius: 15.0);
        double uMin = -5.0, uMax = 5.0;
        double vMin = -5.0, vMax = 5.0;

        var parameters = new PaveParameters
        {
            PatternType = PavePatternType.Hexagonal,
            StoneDiameterMm = 1.3,
            StoneSpacingMm = 0.15,
            BorderOffsetMm = 0.20,
            RelaxationIterations = 15
        };

        var stones = PaveEngine.CalculatePavePlacements(sphere, uMin, uMax, vMin, vMax, parameters);
        Assert.NotEmpty(stones);

        double minAllowedDistance = parameters.StoneDiameterMm + parameters.StoneSpacingMm - Tolerance;

        for (int i = 0; i < stones.Count; i++)
        {
            for (int j = i + 1; j < stones.Count; j++)
            {
                double dist3D = stones[i].Center.DistanceTo(stones[j].Center);
                Assert.True(dist3D >= minAllowedDistance,
                    $"Sphere stone collision between {i} and {j}: 3D distance {dist3D:F4} < {minAllowedDistance:F4}");
            }
        }
    }

    #endregion

    #region 6. Watertight Bead Mesh Validation

    [Fact]
    public void WatertightBeadMeshValidation_EulerCharacteristicEqualsTwoPerBead()
    {
        var bead = new PaveBeadPlacement
        {
            Center = new Point3D(2.5, 3.5, 0.4),
            Normal = Vector3D.UnitZ,
            DiameterMm = 0.50,
            AssociatedStoneIndices = new List<int> { 0, 1, 2 }
        };

        var mesh = PaveEngine.CreateBeadMesh(bead);

        Assert.NotNull(mesh);
        Assert.NotEmpty(mesh.Vertices);
        Assert.NotEmpty(mesh.Faces);

        AssertWatertight2Manifold(mesh);
    }

    [Fact]
    public void WatertightBeadMeshValidation_MultipleBeads_EachBeadSatisfiesEulerCharacteristic()
    {
        var beads = new List<PaveBeadPlacement>
        {
            new() { Center = new Point3D(1.0, 1.0, 0.4), Normal = Vector3D.UnitZ, DiameterMm = 0.5 },
            new() { Center = new Point3D(3.0, 1.0, 0.4), Normal = Vector3D.UnitZ, DiameterMm = 0.6 },
            new() { Center = new Point3D(2.0, 3.0, 0.4), Normal = Vector3D.UnitZ, DiameterMm = 0.4 }
        };

        var individualMeshes = PaveEngine.CreateBeadMeshes(beads);
        Assert.Equal(3, individualMeshes.Count);

        foreach (var mesh in individualMeshes)
        {
            AssertWatertight2Manifold(mesh);
        }
    }

    [Fact]
    public void FishtailCutterMesh_GeneratesValidWatertight2ManifoldCutters()
    {
        var surface = new PlaneSurface3D(Vector3D.UnitZ);
        var parameters = new PaveParameters
        {
            PatternType = PavePatternType.Fishtail,
            ProngType = PaveProngType.FishtailV,
            StoneDiameterMm = 1.3,
            StoneSpacingMm = 0.15
        };

        var stones = PaveEngine.CalculatePavePlacements(surface, 0.0, 10.0, 0.0, 2.5, parameters);
        Assert.NotEmpty(stones);

        var cutterMesh = PaveEngine.CreateFishtailCutterMesh(stones, parameters);
        Assert.NotNull(cutterMesh);
        Assert.NotEmpty(cutterMesh.Vertices);
        Assert.NotEmpty(cutterMesh.Faces);

        // Multiple closed boxes combined: each box has V=8, F=6, E=12, chi = 2
        int numCutters = stones.Count * 2;
        int expectedV = numCutters * 8;
        int expectedF = numCutters * 6;
        Assert.Equal(expectedV, cutterMesh.Vertices.Count);
        Assert.Equal(expectedF, cutterMesh.Faces.Count);

        // Verify manifold structure across every face
        var edgeMap = new Dictionary<(int, int), List<(int faceIdx, int from, int to)>>();
        for (int f = 0; f < cutterMesh.Faces.Count; f++)
        {
            var face = cutterMesh.Faces[f];
            for (int i = 0; i < face.Length; i++)
            {
                int u = face[i];
                int v = face[(i + 1) % face.Length];
                var canonicalEdge = u < v ? (u, v) : (v, u);
                if (!edgeMap.TryGetValue(canonicalEdge, out var occ))
                {
                    occ = new List<(int, int, int)>();
                    edgeMap[canonicalEdge] = occ;
                }
                occ.Add((f, u, v));
            }
        }

        // Each edge must have exactly 2 occurrences with opposite winding
        foreach (var kvp in edgeMap)
        {
            Assert.Equal(2, kvp.Value.Count);
            var o1 = kvp.Value[0];
            var o2 = kvp.Value[1];
            Assert.True(o1.from == o2.to && o1.to == o2.from);
        }

        int totalE = edgeMap.Count;
        int totalChi = cutterMesh.Vertices.Count - totalE + cutterMesh.Faces.Count;
        Assert.Equal(2 * numCutters, totalChi);
    }

    #endregion

    #region 7. Parameter Validation & Edge Cases

    [Theory]
    [InlineData(0.5)]  // Below 0.8
    [InlineData(3.5)]  // Above 3.0
    public void PaveParameters_Validate_ThrowsForInvalidStoneDiameter(double diameterMm)
    {
        var parameters = new PaveParameters { StoneDiameterMm = diameterMm };
        Assert.Throws<ArgumentOutOfRangeException>(() => parameters.Validate());
    }

    [Theory]
    [InlineData(0.01)] // Below 0.05
    [InlineData(0.50)] // Above 0.40
    public void PaveParameters_Validate_ThrowsForInvalidStoneSpacing(double spacingMm)
    {
        var parameters = new PaveParameters { StoneSpacingMm = spacingMm };
        Assert.Throws<ArgumentOutOfRangeException>(() => parameters.Validate());
    }

    [Fact]
    public void PaveParameters_Validate_ThrowsForNegativeBorderOffset()
    {
        var parameters = new PaveParameters { BorderOffsetMm = -0.1 };
        Assert.Throws<ArgumentOutOfRangeException>(() => parameters.Validate());
    }

    [Theory]
    [InlineData(0.2)] // Below 0.3
    [InlineData(0.9)] // Above 0.8
    public void PaveParameters_Validate_ThrowsForInvalidBeadDiameter(double beadDiaMm)
    {
        var parameters = new PaveParameters { BeadDiameterMm = beadDiaMm };
        Assert.Throws<ArgumentOutOfRangeException>(() => parameters.Validate());
    }

    [Fact]
    public void CalculatePavePlacements_DomainTooSmall_ReturnsEmptyList()
    {
        var surface = new PlaneSurface3D(Vector3D.UnitZ);
        var parameters = new PaveParameters
        {
            StoneDiameterMm = 2.0,
            BorderOffsetMm = 0.5
        };

        // Margin required is 1.0 + 0.5 = 1.5 on each side, so min dimension must be > 3.0
        var stones = PaveEngine.CalculatePavePlacements(surface, 0.0, 2.0, 0.0, 2.0, parameters);
        Assert.Empty(stones);
    }

    [Fact]
    public void CalculateBeadPlacements_NullOrEmptyStones_ReturnsEmptyList()
    {
        var parameters = new PaveParameters();
        Assert.Empty(PaveEngine.CalculateBeadPlacements(null!, parameters));
        Assert.Empty(PaveEngine.CalculateBeadPlacements(Array.Empty<PaveStonePlacement>(), parameters));
    }

    #endregion

    #region Helper Assertions

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
