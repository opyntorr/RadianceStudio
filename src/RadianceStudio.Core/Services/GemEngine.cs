namespace RadianceStudio.Core.Services;

using System;
using System.Collections.Generic;
using RadianceStudio.Core.Enums;
using RadianceStudio.Core.Geometry;
using RadianceStudio.Core.Models;

/// <summary>
/// Core implementation of the procedural gem placement engine.
/// Provides microsecond-scale closed-form affine matrix evaluation and curve distribution.
/// </summary>
public class GemEngine : IGemEngine
{
    public int CalculateStoneCount(double usableLengthMm, double gemSizeMm, double spacingMm)
    {
        if (gemSizeMm <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(gemSizeMm), "Gem size must be positive.");
        }

        double step = gemSizeMm + spacingMm;
        if (step <= 1e-6)
        {
            throw new ArgumentException("Sum of gem size and spacing must be strictly positive.", nameof(spacingMm));
        }

        if (usableLengthMm < gemSizeMm - 1e-9)
        {
            return 0;
        }

        // Spec §3.1: N = floor((L_usable - d) / S_step) + 1
        // 1e-9 epsilon guards against floating-point under-representation of exact quotients
        double ratio = (usableLengthMm - gemSizeMm + 1e-9) / step;
        return (int)Math.Floor(ratio) + 1;
    }

    public IReadOnlyList<double> CalculateArcDistances(double usableLengthMm, double gemSizeMm, double spacingMm, double startOffsetMm)
    {
        int count = CalculateStoneCount(usableLengthMm, gemSizeMm, spacingMm);
        if (count <= 0)
        {
            return Array.Empty<double>();
        }

        double step = gemSizeMm + spacingMm;
        double firstCenter = startOffsetMm + (gemSizeMm * 0.5);
        var distances = new double[count];

        for (int i = 0; i < count; i++)
        {
            distances[i] = firstCenter + (i * step);
        }

        return distances;
    }

    public IReadOnlyList<GemPlacement> CalculatePlacements(
        ICurve3D curve,
        ISurface3D? surface,
        GemCutType cutType,
        double gemSizeMm,
        double spacingMm,
        double startOffsetMm = 0,
        double endOffsetMm = 0,
        bool flipNormal = false,
        double rotationAngleDeg = 0)
    {
        ArgumentNullException.ThrowIfNull(curve);

        double usableLength = curve.Length - (startOffsetMm + endOffsetMm);
        int count = CalculateStoneCount(usableLength, gemSizeMm, spacingMm);
        if (count <= 0)
        {
            return Array.Empty<GemPlacement>();
        }

        double step = gemSizeMm + spacingMm;
        double firstCenter = startOffsetMm + (gemSizeMm * 0.5);
        // Canonical gem meshes already embody authentic cut aspect ratios (width = 1.0, length = AspectRatio).
        // Placement transforms scale uniformly by gemSizeMm (aspectRatio multiplier = 1.0).
        var placements = new List<GemPlacement>(count);

        for (int i = 0; i < count; i++)
        {
            double s = firstCenter + (i * step);
            Point3D pt = curve.PointAtArcLength(s);
            double paramT = curve.ParameterAtArcLength(s);
            Vector3D tangent = curve.TangentAtArcLength(s);

            // Evaluate normal from surface or Frenet curve frame
            Vector3D normalCand = surface != null ? surface.NormalAtPoint(pt) : curve.NormalAtArcLength(s);

            // Construct strictly orthonormal right-handed frame (handles flipNormal & degeneracies)
            var frame = OrthonormalFrame.Create(tangent, normalCand, flipNormal);

            // Direct closed-form affine 4x4 matrix composition (Spec §3.3)
            var matrix = Matrix4x4D.CreatePlacement(frame, pt, gemSizeMm, aspectRatio: 1.0, rotationAngleDeg);

            placements.Add(new GemPlacement
            {
                Index = i,
                CutType = cutType,
                GemSizeMm = gemSizeMm,
                ArcPositionMm = s,
                ParameterT = paramT,
                Matrix4x4 = matrix.ToRowMajorArray()
            });
        }

        return placements;
    }
}
