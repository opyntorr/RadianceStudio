namespace RadianceStudio.Core.Services.MeshGenerators;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;

/// <summary>
/// Canonical 58-facet Pear (teardrop) cut procedural mesh generator (aspect ratio 1.55).
/// Features authentic split-apex "French tip" bezel yielding 58 facets.
/// </summary>
public class PearMeshGenerator : BaseGemMeshGenerator
{
    public override GemCutType CutType => GemCutType.Pear;

    protected override (IReadOnlyList<Point3D> Vertices, IReadOnlyList<int[]> Faces) BuildTopology(double diameterMm)
    {
        var dim = GemDimensions.GetDefault(CutType, diameterMm);
        double a = diameterMm * 0.5; // semi-width along X (0.5 mm)
        double b = a * dim.AspectRatio; // semi-length along Y (0.775 mm)

        double hc = diameterMm * (dim.CrownHeightPercent / 100.0);
        double hp = diameterMm * (dim.PavilionDepthPercent / 100.0);
        double g = diameterMm * (dim.GirdleThicknessPercent / 100.0);

        double zTable = (g * 0.5) + hc;
        double zGirdle = 0.0;
        double zCulet = -((g * 0.5) + hp);
        double zStar = (g * 0.5) + 0.60 * hc;
        double zK = -((g * 0.5) + 0.75 * hp);

        Point3D EvalPear(double phi, double scale, double z)
        {
            double sinVal = Math.Sin(phi);
            double cosVal = Math.Cos(phi);
            double y = b * sinVal;
            double widthFactor = sinVal <= 0 ? 1.0 : (1.0 - 0.75 * sinVal);
            double x = a * cosVal * widthFactor;
            return new Point3D(x * scale, y * scale, z);
        }

        double tableScale = dim.TableWidthPercent / 100.0;
        double starScale = tableScale + 0.5 * (1.0 - tableScale);
        double kScale = 0.22;

        var vertices = new List<Point3D>(41);

        // V0: Culet
        vertices.Add(new Point3D(0, 0, zCulet));

        // V1..V16: Girdle vertices (16)
        for (int k = 0; k < 16; k++)
        {
            double phi = k * Math.PI / 8.0;
            vertices.Add(EvalPear(phi, 1.0, zGirdle));
        }

        // V17..V24: Table (8)
        for (int j = 0; j < 8; j++)
        {
            double phi = j * Math.PI / 4.0;
            vertices.Add(EvalPear(phi, tableScale, zTable));
        }

        // V25..V32: Star vertices (8)
        // Offset star vertices flanking equator by 0.027 * diameterMm along X to balance teardrop taper asymmetry
        double equatorShiftX = 0.027 * diameterMm;
        for (int j = 0; j < 8; j++)
        {
            double phi = (Math.PI / 8.0) + j * Math.PI / 4.0;
            var pt = EvalPear(phi, starScale, zStar);
            if (j == 0 || j == 7)
            {
                pt = new Point3D(pt.X + equatorShiftX, pt.Y, pt.Z);
            }
            else if (j == 3 || j == 4)
            {
                pt = new Point3D(pt.X - equatorShiftX, pt.Y, pt.Z);
            }
            vertices.Add(pt);
        }

        // V33..V40: Lower girdle ridge vertices (8)
        for (int j = 0; j < 8; j++)
        {
            double phi = (Math.PI / 8.0) + j * Math.PI / 4.0;
            vertices.Add(EvalPear(phi, kScale, zK));
        }

        var faces = new List<int[]>(58);

        // 1. Table Face (Face 0)
        faces.Add(new[] { 17, 18, 19, 20, 21, 22, 23, 24 });

        // 2. 8 Star Faces
        for (int j = 0; j < 8; j++)
        {
            faces.Add(new[] { 17 + j, 25 + j, 17 + ((j + 1) % 8) });
        }

        // 3. 9 Bezel Faces (apex bezel at j=2 is split into 2 triangles: French tip)
        for (int j = 0; j < 8; j++)
        {
            if (j == 2) // Apex at +Y
            {
                // Split quad [19, 26, 5, 27] into 2 triangles sharing apex ridge [19, 5]
                faces.Add(new[] { 19, 26, 5 });
                faces.Add(new[] { 19, 5, 27 });
            }
            else
            {
                faces.Add(new[] { 17 + j, 25 + ((j + 7) % 8), 1 + 2 * j, 25 + j });
            }
        }

        // 4. 16 Upper Girdle Faces
        for (int j = 0; j < 8; j++)
        {
            faces.Add(new[] { 25 + j, 1 + 2 * j, 1 + (2 * j + 1) });
            faces.Add(new[] { 25 + j, 1 + (2 * j + 1), 1 + ((2 * j + 2) % 16) });
        }

        // 5. 16 Lower Girdle Faces
        for (int j = 0; j < 8; j++)
        {
            faces.Add(new[] { 1 + 2 * j, 33 + j, 1 + (2 * j + 1) });
            faces.Add(new[] { 1 + (2 * j + 1), 33 + j, 1 + ((2 * j + 2) % 16) });
        }

        // 6. 8 Pavilion Main Faces
        for (int j = 0; j < 8; j++)
        {
            faces.Add(new[] { 1 + 2 * j, 33 + ((j + 7) % 8), 0, 33 + j });
        }

        return (vertices, faces);
    }
}
