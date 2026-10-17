namespace RadianceStudio.Core.Enums;

using System;

/// <summary>
/// Resolution profiles for CAM manufacturing preparation, additive 3D wax printing, and voxelization.
/// </summary>
public enum CastingResolutionProfile
{
    /// <summary>
    /// 0.03 mm voxel resolution: Ultra-fine precision for delicate micro-pavé, filigree, and prong tips.
    /// </summary>
    UltraPrecision,

    /// <summary>
    /// 0.05 mm voxel resolution: Balanced industry-standard resolution for general direct-investment casting.
    /// </summary>
    StandardCasting,

    /// <summary>
    /// 0.08 mm voxel resolution: Coarser resolution optimized for rapid prototype visualization and fit verification.
    /// </summary>
    RapidPrototyping
}

/// <summary>
/// Extensions for casting resolution profile parameter mapping.
/// </summary>
public static class CastingResolutionProfileExtensions
{
    /// <summary>
    /// Gets the voxel / target edge length in millimeters for the given resolution profile.
    /// </summary>
    public static double GetVoxelSizeMm(this CastingResolutionProfile profile)
    {
        return profile switch
        {
            CastingResolutionProfile.UltraPrecision => 0.03,
            CastingResolutionProfile.StandardCasting => 0.05,
            CastingResolutionProfile.RapidPrototyping => 0.08,
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unsupported resolution profile.")
        };
    }
}
