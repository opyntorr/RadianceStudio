namespace RadianceStudio.Core.Models;

using RadianceStudio.Core.Enums;

/// <summary>
/// Immutable record capturing international ring size data, dimensions, and cross-standard conversions.
/// </summary>
public record RingSizeInfo
{
    /// <summary>
    /// The primary standard used to query or define this ring size.
    /// </summary>
    public RingSizingStandard Standard { get; init; }

    /// <summary>
    /// Numeric size value in the queried standard.
    /// </summary>
    public double SizeValue { get; init; }

    /// <summary>
    /// Formatted display label in the primary standard (e.g. "7", "N", "N 1/2", "Z+2", "54", "14").
    /// </summary>
    public string SizeLabel { get; init; } = string.Empty;

    /// <summary>
    /// Alias for SizeLabel for API convenience.
    /// </summary>
    public string SizeString => SizeLabel;

    /// <summary>
    /// Inner diameter of the ring rail in millimeters.
    /// </summary>
    public double InnerDiameterMm { get; init; }

    /// <summary>
    /// Inner circumference of the ring rail in millimeters (finger circumference).
    /// </summary>
    public double InnerCircumferenceMm { get; init; }

    /// <summary>
    /// Inner radius of the ring rail in millimeters.
    /// </summary>
    public double RadiusMm => InnerDiameterMm / 2.0;

    /// <summary>
    /// Equivalent US / North America size value.
    /// </summary>
    public double UsSize { get; init; }

    /// <summary>
    /// Equivalent UK / Australia Wheatsheaf size string.
    /// </summary>
    public string UkSize { get; init; } = string.Empty;

    /// <summary>
    /// Equivalent Europe / ISO 8653 size value (circumference in millimeters).
    /// </summary>
    public double EuSize { get; init; }

    /// <summary>
    /// Equivalent Japan / East Asia size value.
    /// </summary>
    public double JpSize { get; init; }

    /// <summary>
    /// Formatted summary string across international standards.
    /// </summary>
    public override string ToString()
    {
        return $"RingSize [{Standard}: {SizeLabel}] - D: {InnerDiameterMm:F2} mm, C: {InnerCircumferenceMm:F2} mm (US: {UsSize:0.##}, UK: {UkSize}, EU: {EuSize:0.#}, JP: {JpSize:0.#})";
    }
}
