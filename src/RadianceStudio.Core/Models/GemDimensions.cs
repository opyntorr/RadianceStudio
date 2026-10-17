namespace RadianceStudio.Core.Models;

using RadianceStudio.Core.Enums;

/// <summary>
/// Proportional dimensional parameters of a canonical gem cut.
/// All percentages are relative to the primary girdle diameter/width (1.0 mm).
/// </summary>
public record GemDimensions
{
    public GemCutType CutType { get; init; }
    public double DiameterMm { get; init; } = 1.0;
    public double AspectRatio { get; init; } = 1.0;
    public double TableWidthPercent { get; init; } = 56.0;
    public double CrownHeightPercent { get; init; } = 14.5;
    public double CrownAngleDeg { get; init; } = 34.5;
    public double PavilionDepthPercent { get; init; } = 43.0;
    public double PavilionAngleDeg { get; init; } = 40.75;
    public double GirdleThicknessPercent { get; init; } = 2.5;
    public double TotalDepthPercent => CrownHeightPercent + GirdleThicknessPercent + PavilionDepthPercent;

    /// <summary>
    /// Gets the absolute total depth in millimeters for the given diameter.
    /// </summary>
    public double GetTotalDepthMm() => DiameterMm * (TotalDepthPercent / 100.0);

    /// <summary>
    /// Gets the absolute length in millimeters along the major axis.
    /// </summary>
    public double GetLengthMm() => DiameterMm * AspectRatio;

    /// <summary>
    /// Gets canonical default dimensions for a given gem cut type.
    /// </summary>
    public static GemDimensions GetDefault(GemCutType cutType, double diameterMm = 1.0)
    {
        return cutType switch
        {
            GemCutType.RoundBrilliant => new GemDimensions
            {
                CutType = cutType,
                DiameterMm = diameterMm,
                AspectRatio = 1.0,
                TableWidthPercent = 56.0,
                CrownHeightPercent = 14.5,
                CrownAngleDeg = 34.5,
                PavilionDepthPercent = 43.0,
                PavilionAngleDeg = 40.75,
                GirdleThicknessPercent = 2.5
            },
            GemCutType.Princess => new GemDimensions
            {
                CutType = cutType,
                DiameterMm = diameterMm,
                AspectRatio = 1.0,
                TableWidthPercent = 68.0,
                CrownHeightPercent = 12.0,
                CrownAngleDeg = 36.0,
                PavilionDepthPercent = 58.0,
                PavilionAngleDeg = 44.0,
                GirdleThicknessPercent = 3.0
            },
            GemCutType.Oval => new GemDimensions
            {
                CutType = cutType,
                DiameterMm = diameterMm,
                AspectRatio = 1.45,
                TableWidthPercent = 58.0,
                CrownHeightPercent = 13.5,
                CrownAngleDeg = 34.0,
                PavilionDepthPercent = 44.0,
                PavilionAngleDeg = 41.0,
                GirdleThicknessPercent = 2.8
            },
            GemCutType.Pear => new GemDimensions
            {
                CutType = cutType,
                DiameterMm = diameterMm,
                AspectRatio = 1.55,
                TableWidthPercent = 57.0,
                CrownHeightPercent = 14.0,
                CrownAngleDeg = 34.5,
                PavilionDepthPercent = 44.5,
                PavilionAngleDeg = 41.0,
                GirdleThicknessPercent = 2.5
            },
            GemCutType.Emerald => new GemDimensions
            {
                CutType = cutType,
                DiameterMm = diameterMm,
                AspectRatio = 1.40,
                TableWidthPercent = 64.0,
                CrownHeightPercent = 12.5,
                CrownAngleDeg = 32.0,
                PavilionDepthPercent = 50.0,
                PavilionAngleDeg = 43.0,
                GirdleThicknessPercent = 3.5
            },
            GemCutType.Marquise => new GemDimensions
            {
                CutType = cutType,
                DiameterMm = diameterMm,
                AspectRatio = 1.95,
                TableWidthPercent = 58.0,
                CrownHeightPercent = 13.0,
                CrownAngleDeg = 33.5,
                PavilionDepthPercent = 44.0,
                PavilionAngleDeg = 41.0,
                GirdleThicknessPercent = 3.0
            },
            GemCutType.Baguette => new GemDimensions
            {
                CutType = cutType,
                DiameterMm = diameterMm,
                AspectRatio = 1.50,
                TableWidthPercent = 72.0,
                CrownHeightPercent = 8.0,
                CrownAngleDeg = 28.0,
                PavilionDepthPercent = 42.0,
                PavilionAngleDeg = 38.0,
                GirdleThicknessPercent = 2.5
            },
            GemCutType.Cushion => new GemDimensions
            {
                CutType = cutType,
                DiameterMm = diameterMm,
                AspectRatio = 1.05,
                TableWidthPercent = 61.0,
                CrownHeightPercent = 13.5,
                CrownAngleDeg = 33.0,
                PavilionDepthPercent = 44.0,
                PavilionAngleDeg = 41.0,
                GirdleThicknessPercent = 3.0
            },
            GemCutType.Asscher => new GemDimensions
            {
                CutType = cutType,
                DiameterMm = diameterMm,
                AspectRatio = 1.0,
                TableWidthPercent = 60.0,
                CrownHeightPercent = 14.0,
                CrownAngleDeg = 34.0,
                PavilionDepthPercent = 52.0,
                PavilionAngleDeg = 44.0,
                GirdleThicknessPercent = 3.5
            },
            GemCutType.Radiant => new GemDimensions
            {
                CutType = cutType,
                DiameterMm = diameterMm,
                AspectRatio = 1.15,
                TableWidthPercent = 65.0,
                CrownHeightPercent = 12.0,
                CrownAngleDeg = 32.0,
                PavilionDepthPercent = 48.0,
                PavilionAngleDeg = 42.0,
                GirdleThicknessPercent = 3.0
            },
            GemCutType.Heart => new GemDimensions
            {
                CutType = cutType,
                DiameterMm = diameterMm,
                AspectRatio = 1.0,
                TableWidthPercent = 56.0,
                CrownHeightPercent = 14.0,
                CrownAngleDeg = 34.5,
                PavilionDepthPercent = 44.0,
                PavilionAngleDeg = 41.0,
                GirdleThicknessPercent = 3.0
            },
            GemCutType.Trilliant => new GemDimensions
            {
                CutType = cutType,
                DiameterMm = diameterMm,
                AspectRatio = 1.0,
                TableWidthPercent = 53.0,
                CrownHeightPercent = 12.5,
                CrownAngleDeg = 33.0,
                PavilionDepthPercent = 43.0,
                PavilionAngleDeg = 41.0,
                GirdleThicknessPercent = 3.0
            },
            GemCutType.TaperedBaguette => new GemDimensions
            {
                CutType = cutType,
                DiameterMm = diameterMm,
                AspectRatio = 1.60,
                TableWidthPercent = 70.0,
                CrownHeightPercent = 8.0,
                CrownAngleDeg = 28.0,
                PavilionDepthPercent = 42.0,
                PavilionAngleDeg = 38.0,
                GirdleThicknessPercent = 2.5
            },
            _ => new GemDimensions
            {
                CutType = cutType,
                DiameterMm = diameterMm,
                AspectRatio = 1.0
            }
        };
    }
}
