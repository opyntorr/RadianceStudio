namespace RadianceStudio.Core.Geometry;

/// <summary>
/// Defines a 3D parametric space curve parameterized by arc length.
/// </summary>
public interface ICurve3D
{
    /// <summary>
    /// Gets the total arc length of the curve in millimeters.
    /// </summary>
    double Length { get; }

    /// <summary>
    /// Gets whether the curve forms a closed loop.
    /// </summary>
    bool IsClosed { get; }

    /// <summary>
    /// Evaluates the 3D position at arc distance s from the start of the curve.
    /// </summary>
    Point3D PointAtArcLength(double s);

    /// <summary>
    /// Evaluates the unit tangent vector at arc distance s.
    /// </summary>
    Vector3D TangentAtArcLength(double s);

    /// <summary>
    /// Evaluates the principal normal vector (Frenet normal) at arc distance s.
    /// Returns Vector3D.Zero or reference normal for zero-curvature curves.
    /// </summary>
    Vector3D NormalAtArcLength(double s);

    /// <summary>
    /// Evaluates the native curve parameter t corresponding to arc distance s.
    /// </summary>
    double ParameterAtArcLength(double s);
}
