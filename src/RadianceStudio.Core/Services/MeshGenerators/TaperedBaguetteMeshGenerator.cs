namespace RadianceStudio.Core.Services.MeshGenerators;

using System.Collections.Generic;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;

/// <summary>
/// Canonical 14-facet Tapered Baguette cut (trapezoidal step cut) procedural mesh generator (aspect ratio 1.60).
/// Generates 1 table quad, 4 crown trapezoids, 4 girdle trapezoids, 4 pavilion trapezoids, and 1 culet quad.
/// Narrows at +Y with taper ratio 0.70.
/// </summary>
public class TaperedBaguetteMeshGenerator : BaseGemMeshGenerator
{
    public override GemCutType CutType => GemCutType.TaperedBaguette;

    protected override (IReadOnlyList<Point3D> Vertices, IReadOnlyList<int[]> Faces) BuildTopology(double diameterMm)
    {
        var dim = GemDimensions.GetDefault(CutType, diameterMm);
        double aWide = diameterMm * 0.5; // semi-width at wide base (-Y)
        double aNarrow = aWide * 0.70; // semi-width at narrow top (+Y, taper ratio 0.7)
        double b = aWide * dim.AspectRatio; // semi-length along Y (0.80 mm)

        double hc = diameterMm * (dim.CrownHeightPercent / 100.0);
        double hp = diameterMm * (dim.PavilionDepthPercent / 100.0);
        double g = diameterMm * (dim.GirdleThicknessPercent / 100.0);

        double zTable = (g * 0.5) + hc;
        double zGirdleTop = g * 0.5;
        double zGirdleBottom = -g * 0.5;
        double zCulet = -((g * 0.5) + hp);

        Point3D[] CreateTrapezoidRing(double scale, double z)
        {
            double curAnarrow = aNarrow * scale;
            double curAwide = aWide * scale;
            double curB = b * scale;

            return new Point3D[4]
            {
                new Point3D( curAnarrow,  curB, z), // 0: top-right (narrow)
                new Point3D(-curAnarrow,  curB, z), // 1: top-left (narrow)
                new Point3D(-curAwide,   -curB, z), // 2: bottom-left (wide)
                new Point3D( curAwide,   -curB, z)  // 3: bottom-right (wide)
            };
        }

        double tableScale = dim.TableWidthPercent / 100.0;
        double culetScale = 0.25;

        var tableRing = CreateTrapezoidRing(tableScale, zTable);
        var girdleTopRing = CreateTrapezoidRing(1.0, zGirdleTop);
        var girdleBottomRing = CreateTrapezoidRing(1.0, zGirdleBottom);
        var culetRing = CreateTrapezoidRing(culetScale, zCulet);

        var vertices = new List<Point3D>(16);
        // Indices:
        // 0..3: TableRing
        // 4..7: GirdleTopRing
        // 8..11: GirdleBottomRing
        // 12..15: CuletRing
        vertices.AddRange(tableRing);
        vertices.AddRange(girdleTopRing);
        vertices.AddRange(girdleBottomRing);
        vertices.AddRange(culetRing);

        var faces = new List<int[]>(14);

        // 1. Table Face (Face 0, quad at zTable, CCW viewed from exterior)
        faces.Add(new[] { 0, 1, 2, 3 });

        // 2. Crown Trapezoids (4 trapezoids connecting table to upper girdle)
        for (int j = 0; j < 4; j++)
        {
            int next = (j + 1) % 4;
            faces.Add(new[] { j, 4 + j, 4 + next, next });
        }

        // 3. Girdle Trapezoids (4 quads connecting upper to lower girdle)
        for (int j = 0; j < 4; j++)
        {
            int next = (j + 1) % 4;
            faces.Add(new[] { 4 + j, 8 + j, 8 + next, 4 + next });
        }

        // 4. Pavilion Trapezoids (4 trapezoids connecting lower girdle to culet)
        for (int j = 0; j < 4; j++)
        {
            int next = (j + 1) % 4;
            faces.Add(new[] { 8 + j, 12 + j, 12 + next, 8 + next });
        }

        // 5. Culet Face (1 quad at zCulet, CCW viewed from exterior)
        faces.Add(new[] { 15, 14, 13, 12 });

        return (vertices, faces);
    }
}
