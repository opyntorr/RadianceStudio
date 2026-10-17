namespace RadianceStudio.Core.Models;

using RadianceStudio.Core.Enums;

/// <summary>
/// Represents a placed gem instance transformed in 3D space.
/// </summary>
public record GemPlacement
{
    public int Index { get; init; }
    public GemCutType CutType { get; init; }
    public double GemSizeMm { get; init; }
    public double ArcPositionMm { get; init; }
    public double ParameterT { get; init; }

    // 4x4 Affine Transformation Matrix components (Row-Major)
    public double[] Matrix4x4 { get; init; } = new double[16]
    {
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1
    };
}
