namespace RadianceStudio.Rhino.Commands;

using System;
using System.Runtime.InteropServices;
using global::Rhino;
using global::Rhino.Commands;
using RadianceStudio.Core.Enums;
using RadianceStudio.UI.ViewModels;

/// <summary>
/// Creates a Comfort-Fit swept shank solid and activates the lateral dynamic Inspector.
/// </summary>
[Guid("6F19A2E4-E5BE-48A1-88EB-6E04CE6B9999")]
public class RadShankCommand : Command
{
    public static readonly string[] CommandParameters = { "TargetRailId", "ProfileType", "TaperRatio", "HollowAzurador" };

    public override string EnglishName => "RadShank";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        return ExecuteShank(doc, mode, ShankProfileType.ComfortFit);
    }

    internal static Result ExecuteShank(RhinoDoc doc, RunMode mode, ShankProfileType profileType)
    {
        RadianceStudioPlugIn.OpenStudioWorkspace();
        var inspector = RadianceStudioPlugIn.GetOrCreateInspector();

        var vm = new ShankViewModel
        {
            ProfileType = profileType,
            BottomWidthMm = 2.2,
            BottomThicknessMm = 1.6,
            TopWidthMm = 2.8,
            TopThicknessMm = 2.0
        };

        if (mode == RunMode.Scripted)
        {
            vm.Apply(doc);
            RhinoApp.WriteLine($"[RadianceStudio] Shank created (Scripted): Profile {profileType}.");
            return Result.Success;
        }

        inspector.SetViewModel(vm);
        RhinoApp.WriteLine($"[RadianceStudio] Shank active in Inspector: Profile {profileType}. Adjust in sidebar and click Apply.");
        return Result.Success;
    }
}

/// <summary>
/// Creates a Flat profile swept shank and activates the lateral dynamic Inspector.
/// </summary>
[Guid("7F20B3E4-E5BE-48A1-88EB-6E04CE6BA001")]
public class RadFlatShankCommand : Command
{
    public static readonly string[] CommandParameters = { "Param" };
    public override string EnglishName => "RadFlatShank";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        return RadShankCommand.ExecuteShank(doc, mode, ShankProfileType.Flat);
    }
}

/// <summary>
/// Creates a D-Shape half-round swept shank and activates the lateral dynamic Inspector.
/// </summary>
[Guid("8F31C4E4-E5BE-48A1-88EB-6E04CE6BA002")]
public class RadDShapeShankCommand : Command
{
    public static readonly string[] CommandParameters = { "Param" };
    public override string EnglishName => "RadDShapeShank";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        return RadShankCommand.ExecuteShank(doc, mode, ShankProfileType.DShape);
    }
}

/// <summary>
/// Creates a Knife-Edge swept shank and activates the lateral dynamic Inspector.
/// </summary>
[Guid("9F42D5E4-E5BE-48A1-88EB-6E04CE6BA003")]
public class RadKnifeEdgeShankCommand : Command
{
    public static readonly string[] CommandParameters = { "Param" };
    public override string EnglishName => "RadKnifeEdgeShank";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        return RadShankCommand.ExecuteShank(doc, mode, ShankProfileType.KnifeEdge);
    }
}

/// <summary>
/// Creates a Cathedral arch swept shank and activates the lateral dynamic Inspector.
/// </summary>
[Guid("AF53E6E4-E5BE-48A1-88EB-6E04CE6BA004")]
public class RadCathedralShankCommand : Command
{
    public static readonly string[] CommandParameters = { "Param" };
    public override string EnglishName => "RadCathedralShank";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        return RadShankCommand.ExecuteShank(doc, mode, ShankProfileType.Cathedral);
    }
}
