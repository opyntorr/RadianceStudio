namespace RadianceStudio.Core.Models;

using RadianceStudio.Core.Geometry;

/// <summary>
/// Geometric diagnostic record reporting localized wall thickness measurements and casting constraint compliance.
/// </summary>
public record WallThicknessDiagnostic
{
    /// <summary>
    /// Inspection point on the outer surface of the mesh.
    /// </summary>
    public Point3D Location { get; init; }

    /// <summary>
    /// Outward unit surface normal at the inspection point.
    /// </summary>
    public Vector3D Normal { get; init; }

    /// <summary>
    /// Measured wall thickness in millimeters to the opposing interior or opposing surface face.
    /// </summary>
    public double MeasuredThicknessMm { get; init; }

    /// <summary>
    /// Required minimum structural wall thickness in millimeters for this geometric feature.
    /// </summary>
    public double RequiredMinimumMm { get; init; }

    /// <summary>
    /// Indicates whether the measured thickness violates the required minimum threshold (Measured &lt; Required).
    /// </summary>
    public bool IsViolation { get; init; }

    /// <summary>
    /// Descriptive classification of the inspected feature (e.g. "Prong Neck", "Under-Gallery Wall", "Shank Base", "General Wall").
    /// </summary>
    public string FeatureDescription { get; init; } = string.Empty;

    public WallThicknessDiagnostic()
    {
    }

    public WallThicknessDiagnostic(
        Point3D location,
        Vector3D normal,
        double measuredThicknessMm,
        double requiredMinimumMm,
        string featureDescription = "General Wall")
    {
        Location = location;
        Normal = normal;
        MeasuredThicknessMm = measuredThicknessMm;
        RequiredMinimumMm = requiredMinimumMm;
        IsViolation = measuredThicknessMm < requiredMinimumMm;
        FeatureDescription = featureDescription;
    }
}
