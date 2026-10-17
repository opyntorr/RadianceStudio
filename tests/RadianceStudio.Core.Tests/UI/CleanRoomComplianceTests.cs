namespace RadianceStudio.Core.Tests.UI;

using System;
using System.Diagnostics;
using System.IO;
using Xunit;

/// <summary>
/// Tier 1 &amp; Tier 2 tests for Features 20 &amp; 21:
/// Clean-Room Chinese Wall Compliance and Automated Test Suite Verification.
/// </summary>
public class CleanRoomComplianceTests
{
    [Fact]
    public void Feature20_CleanRoomScanner_ScriptExistsAndIsExecutable()
    {
        string rootDir = FindSolutionRoot();
        string scannerScript = Path.Combine(rootDir, "tools", "check_clean_room.ps1");

        Assert.True(File.Exists(scannerScript), $"Clean-room scanner missing at: {scannerScript}");
    }

    [Fact]
    public void Feature20_CleanRoomScanner_PassesWithZeroInfractions()
    {
        string rootDir = FindSolutionRoot();
        string scannerScript = Path.Combine(rootDir, "tools", "check_clean_room.ps1");

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scannerScript}\"",
            WorkingDirectory = rootDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        Assert.NotNull(process);

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit(30000);

        Assert.Equal(0, process.ExitCode);
        Assert.Contains("Zero proprietary contaminants found", output);
        Assert.Empty(error);
    }

    [Fact]
    public void Feature21_DirectoryBuildProps_EnforcesTreatWarningsAsErrors()
    {
        string rootDir = FindSolutionRoot();
        string propsPath = Path.Combine(rootDir, "Directory.Build.props");

        Assert.True(File.Exists(propsPath), "Directory.Build.props must exist at solution root");

        string content = File.ReadAllText(propsPath);
        Assert.Contains("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>", content);
    }

    [Fact]
    public void Feature21_RunTestsScript_ExistsAndTargetsBothStages()
    {
        string rootDir = FindSolutionRoot();
        string runTestsScript = Path.Combine(rootDir, "tools", "run_tests.ps1");

        Assert.True(File.Exists(runTestsScript), $"run_tests.ps1 missing at: {runTestsScript}");

        string content = File.ReadAllText(runTestsScript);
        Assert.Contains("check_clean_room.ps1", content);
        Assert.Contains("$DotnetCmd test", content);
        Assert.Contains("/warnaserror", content);
    }

    private static string FindSolutionRoot()
    {
        string dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "RadianceStudio.sln")))
            {
                return dir;
            }
            dir = Path.GetDirectoryName(dir) ?? string.Empty;
        }
        return @"C:\Users\omarp\matrix";
    }
}
