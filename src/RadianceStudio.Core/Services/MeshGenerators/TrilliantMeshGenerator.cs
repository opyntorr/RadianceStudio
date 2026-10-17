namespace RadianceStudio.Core.Services.MeshGenerators;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;

/// <summary>
/// Canonical 43-facet Trilliant cut (curved triangular brilliant) procedural mesh generator (aspect ratio 1.0).
/// Generates 12 girdle vertices on an equilateral curved triangle with 3 rounded corners and bulged sides.
/// Crown features 1 hexagonal table + 6 star facets + 6 bezel quads + 12 upper girdle triangles = 25 facets.
/// Pavilion features 12 lower girdle triangles + 6 pavilion main quads converging to culet = 18 facets.
/// Total: 43 facets.
/// </summary>
public class TrilliantMeshGenerator : BaseGemMeshGenerator
{
    public override GemCutType CutType => GemCutType.Trilliant;

    protected override (IReadOnlyList<Point3D> Vertices, IReadOnlyList<int[]> Faces) BuildTopology(double diameterMm)
    {
        var dim = GemDimensions.GetDefault(CutType, diameterMm);
        double hc = diameterMm * (dim.CrownHeightPercent / 100.0);
        double hp = diameterMm * (dim.PavilionDepthPercent / 100.0);
        double g = diameterMm * (dim.GirdleThicknessPercent / 100.0);

        double zTable = (g * 0.5) + hc;
        double zGirdle = 0.0;
        double zCulet = -((g * 0.5) + hp);
        double zStar = (g * 0.5) + 0.60 * hc;
        double zK = -((g * 0.5) + 0.75 * hp);

        const double beta = 0.22; // Outward bulge curvature factor
        static (double X, double Y) EvalRaw(double phi)
        {
            double angleDiff = 3.0 * (phi - (Math.PI * 0.5));
            double r = 1.0 / (1.0 - beta * Math.Cos(angleDiff));
            return (r * Math.Cos(phi), r * Math.Sin(phi));
        }

        // Determine discrete bounds of 12 girdle vertices
        double minX = double.MaxValue, maxX = double.MinValue;
        double minY = double.MaxValue, maxY = double.MinValue;
        for (int k = 0; k < 12; k++)
        {
            double phi = k * Math.PI / 6.0;
            var (rx, ry) = EvalRaw(phi);
            if (rx < minX) minX = rx;
            if (rx > maxX) maxX = rx;
            if (ry < minY) minY = ry;
            if (ry > maxY) maxY = ry;
        }

        double spanX = maxX - minX;
        double spanY = maxY - minY;
        double midY = (maxY + minY) * 0.5;

        Point3D EvalTrilliant(double phi, double scale, double z)
        {
            var (rx, ry) = EvalRaw(phi);
            double normX = (rx / spanX) * diameterMm;
            double normY = ((ry - midY) / spanY) * diameterMm;

            return new Point3D(normX * scale, normY * scale, z);
        }

        double tableScale = dim.TableWidthPercent / 100.0;
        double starScale = tableScale + 0.5 * (1.0 - tableScale);
        double kScale = 0.22;

        var vertices = new List<Point3D>(31);

        // V0: Culet
        vertices.Add(new Point3D(0, 0, zCulet));

        // V1..V12: 12 Girdle vertices (4 per side)
        for (int k = 0; k < 12; k++)
        {
            double phi = k * Math.PI / 6.0;
            vertices.Add(EvalTrilliant(phi, 1.0, zGirdle));
        }

        // V13..V18: Table vertices (hexagon, 6)
        for (int j = 0; j < 6; j++)
        {
            double phi = j * Math.PI / 3.0;
            vertices.Add(EvalTrilliant(phi, tableScale, zTable));
        }

        // V19..V24: Star vertices (6)
        for (int j = 0; j < 6; j++)
        {
            double phi = (Math.PI / 6.0) + j * Math.PI / 3.0;
            vertices.Add(EvalTrilliant(phi, starScale, zStar));
        }

        // V25..V30: Lower girdle ridge vertices (6)
        for (int j = 0; j < 6; j++)
        {
            double phi = (Math.PI / 6.0) + j * Math.PI / 3.0;
            vertices.Add(EvalTrilliant(phi, kScale, zK));
        }

        var faces = new List<int[]>(43);

        // 1. Table Face (Face 0: hexagon, 1 face)
        faces.Add(new[] { 13, 14, 15, 16, 17, 18 });

        // 2. 6 Star Faces (6 faces)
        for (int j = 0; j < 6; j++)
        {
            faces.Add(new[] { 13 + j, 19 + j, 13 + ((j + 1) % 6) });
        }

        // 3. 6 Bezel Faces (6 quads)
        for (int j = 0; j < 6; j++)
        {
            faces.Add(new[] { 13 + j, 19 + ((j + 5) % 6), 1 + 2 * j, 19 + j });
        }

        // 4. 12 Upper Girdle Faces (12 triangles)
        for (int j = 0; j < 6; j++)
        {
            faces.Add(new[] { 19 + j, 1 + 2 * j, 1 + (2 * j + 1) });
            faces.Add(new[] { 19 + j, 1 + (2 * j + 1), 1 + ((2 * j + 2) % 12) });
        }

        // 5. 12 Lower Girdle Faces (12 triangles)
        for (int j = 0; j < 6; j++)
        {
            faces.Add(new[] { 1 + 2 * j, 25 + j, 1 + (2 * j + 1) });
            faces.Add(new[] { 1 + (2 * j + 1), 25 + j, 1 + ((2 * j + 2) % 12) });
        }

        // 6. 6 Pavilion Main Faces (6 quads converging to culet V0)
        for (int j = 0; j < 6; j++)
        {
            faces.Add(new[] { 1 + 2 * j, 25 + ((j + 5) % 6), 0, 25 + j });
        }

        return (vertices, faces);
    }
}
