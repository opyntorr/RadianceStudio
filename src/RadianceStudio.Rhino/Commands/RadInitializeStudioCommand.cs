namespace RadianceStudio.Rhino.Commands;

using System;
using System.IO;
using System.Runtime.InteropServices;
using global::Rhino;
using global::Rhino.Commands;
using global::Rhino.Input.Custom;

/// <summary>
/// Boot entry point executed by launcher switch: /runscript="_-RadInitializeStudio".
/// Configures millimetric jewelry template units/tolerances, sets custom window title,
/// docks the jewelry workspace, and writes the readiness token file to synchronize with splash screen.
/// </summary>
[Guid("9B308D64-325D-4F11-8CD1-2DDEB4815E70")]
[CommandStyle(Style.ScriptRunner)]
public class RadInitializeStudioCommand : Command
{
    public static readonly string[] CommandParameters = { "Silent" };

    public override string EnglishName => "RadInitializeStudio";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        bool silent = false;

        // Parse optional -Silent flag
        if (mode == RunMode.Scripted)
        {
            var go = new GetOption();
            go.SetCommandPrompt("RadianceStudio Initialize");
            var optSilent = new OptionToggle(false, "Off", "On");
            go.AddOptionToggle("Silent", ref optSilent);
            while (go.Get() == global::Rhino.Input.GetResult.Option)
            {
                silent = optSilent.CurrentValue;
            }
        }

        // 1. Configure jewelry document tolerances and viewport grid
        RadianceStudioPlugIn.ConfigureJewelryDocument(doc);

        // 2. Set top-level window title
        RadianceStudioPlugIn.SetStudioWindowTitle();

        // 3. Open and dock workspace panels (Ribbon, Inspector, History)
        RadianceStudioPlugIn.OpenStudioWorkspace();

        // 4. Write readiness token file to %TEMP%\radiance_ready_<pid>.tmp
        WriteReadinessToken(silent);

        if (!silent)
        {
            RhinoApp.WriteLine("[RadianceStudio] Jewelry CAD Suite initialized successfully.");
        }

        return Result.Success;
    }

    private static void WriteReadinessToken(bool silent)
    {
        try
        {
            int pid = Environment.ProcessId;
            string tempDir = Path.GetTempPath();
            string tokenPath = Path.Combine(tempDir, $"radiance_ready_{pid}.tmp");

            string content = $"status=ready\ntimestamp={DateTime.UtcNow:O}\npid={pid}\n";
            File.WriteAllText(tokenPath, content);

            if (!silent)
            {
                RhinoApp.WriteLine($"[RadianceStudio] Readiness token written: {tokenPath}");
            }
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"[RadianceStudio] Warning writing readiness token: {ex.Message}");
        }
    }
}
