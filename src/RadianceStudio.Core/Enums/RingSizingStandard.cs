namespace RadianceStudio.Core.Enums;

/// <summary>
/// International standard ring sizing systems.
/// </summary>
public enum RingSizingStandard
{
    /// <summary>
    /// United States / North America (Sizes 0 to 16 in quarter and half sizes).
    /// Standard formula: D = 11.63 + 0.8128 * Size
    /// </summary>
    US = 0,

    /// <summary>Alias for US standard.</summary>
    UnitedStates = US,

    /// <summary>
    /// United Kingdom / Australia / South Africa (BS 6820 Wheatsheaf scale A to Z+6 with halves).
    /// Standard formula: C = 37.82 + 1.25 * k, D = C / PI
    /// </summary>
    UK = 1,

    /// <summary>Alias for UK standard.</summary>
    UnitedKingdom = UK,

    /// <summary>
    /// Europe / ISO 8653 (Ring size defined directly as inner circumference in mm, 40 to 76 mm).
    /// Standard formula: D = Size / PI
    /// </summary>
    EU = 2,

    /// <summary>Alias for European / ISO 8653 standard.</summary>
    Europe = EU,

    /// <summary>Alias for European / ISO 8653 standard.</summary>
    ISO8653 = EU,

    /// <summary>
    /// Japan / East Asia (JIS B 4700 sizes 1 to 30).
    /// Standard formula: D = 13.00 + (Size - 1) / 3 = (38 + Size) / 3
    /// </summary>
    JP = 3,

    /// <summary>Alias for Japan / East Asia standard.</summary>
    Japan = JP
}
