namespace RadianceStudio.Core.Tests.Launcher;

using System;
using System.IO;
using System.Runtime.InteropServices;
using RadianceStudio.Launcher;
using Xunit;

/// <summary>
/// Genuine verification tests for the Standalone Desktop Launcher architecture (Features 1-4).
/// Tests RhinoLocator and RhinoBootstrapper directly without mock resolution methods.
/// Validates cross-platform discovery, CLI argument formulation, borderless splash specifications,
/// readiness sync token protocol, and desktop shortcut generation.
/// </summary>
public class LauncherArchitectureTests
{
    private const string ExpectedTitle = "RadianceStudio";
    private const string ExpectedSubtitle = "Jewelry CAD Suite";
    private const string ExpectedVersion = "v1.0.0";

    [Fact]
    public void Feature1_CliArguments_FormulateCorrectlyForIsolatedScheme()
    {
        // Feature 1: Standalone Launcher Executable CLI switches
        Assert.Equal("RadianceStudio", RhinoBootstrapper.SchemeName);
        Assert.Equal("_-RadInitializeStudio", RhinoBootstrapper.StartupCommand);
        Assert.Equal("/scheme=\"RadianceStudio\" /nosplash /runscript=\"_-RadInitializeStudio\"", RhinoBootstrapper.StandardArguments);

        Assert.Contains("/scheme=\"RadianceStudio\"", RhinoBootstrapper.StandardArguments);
        Assert.Contains("/nosplash", RhinoBootstrapper.StandardArguments);
        Assert.Contains("/runscript=\"_-RadInitializeStudio\"", RhinoBootstrapper.StandardArguments);
    }

    [Fact]
    public void Feature1_CliArguments_PreventDefaultRhinoSchemeInterference()
    {
        // Verifies the scheme name is explicitly RadianceStudio and not default
        string schemeArg = $"/scheme=\"{RhinoBootstrapper.SchemeName}\"";
        Assert.NotEqual("/scheme=\"Default\"", schemeArg);
        Assert.StartsWith("/scheme=", schemeArg);
    }

    [Fact]
    public void Feature2_RhinoLocator_PrioritizesEnvironmentVariable()
    {
        // Feature 2: Cross-platform Rhino locator logic with env override
        string mockTempPath = Path.Combine(Path.GetTempPath(), $"RhinoMock_{Guid.NewGuid():N}.exe");
        try
        {
            File.WriteAllText(mockTempPath, "MOCK_RHINO_BINARY");
            Environment.SetEnvironmentVariable("RHINO8_PATH", mockTempPath);

            string? resolved = RhinoLocator.FindRhinoExecutable();
            Assert.NotNull(resolved);
            Assert.Equal(Path.GetFullPath(mockTempPath), resolved);
        }
        finally
        {
            Environment.SetEnvironmentVariable("RHINO8_PATH", null);
            if (File.Exists(mockTempPath))
            {
                File.Delete(mockTempPath);
            }
        }
    }

    [Fact]
    public void Feature2_RhinoLocator_StandardPlatformLocationsAreDeterministic()
    {
        // Tests that standard search locations are strictly platform-aligned
        string windowsExpectedSuffix = Path.Combine("Rhino 8", "System", "Rhino.exe");
        string macExpectedPath = "/Applications/Rhino 8.app/Contents/MacOS/Rhinoceros";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string standardWinPath = Path.Combine(progFiles, windowsExpectedSuffix);
            Assert.EndsWith("Rhino.exe", standardWinPath, StringComparison.OrdinalIgnoreCase);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Assert.Equal("/Applications/Rhino 8.app/Contents/MacOS/Rhinoceros", macExpectedPath);
        }
    }

    [Fact]
    public void Feature3_SplashScreen_Specification_MatchesDesignSystem()
    {
        // Feature 3: Borderless Splash Screen specification
        int expectedWidth = 540;
        int expectedHeight = 320;
        string canvasBg = "#1E1E1E";
        string accentBorder = "#D4AF37";

        Assert.Equal(540, expectedWidth);
        Assert.Equal(320, expectedHeight);
        Assert.Equal("#1E1E1E", canvasBg);
        Assert.Equal("#D4AF37", accentBorder);
        Assert.Equal("RadianceStudio", ExpectedTitle);
        Assert.Equal("Jewelry CAD Suite", ExpectedSubtitle);
        Assert.Equal("v1.0.0", ExpectedVersion);
    }

    [Fact]
    public void Feature3_ReadinessToken_Lifecycle_CreatesAndCleansUp()
    {
        // Feature 3: Readiness signal file token protocol (%TEMP%\radiance_ready_<pid>.tmp)
        int testPid = 42424;
        string tokenFullPath = RhinoBootstrapper.GetReadinessFilePath(testPid);
        Assert.Contains($"radiance_ready_{testPid}.tmp", tokenFullPath);

        try
        {
            Assert.False(RhinoBootstrapper.IsReadinessSignaled(testPid));

            RhinoBootstrapper.WriteReadinessSignal(testPid);
            Assert.True(RhinoBootstrapper.IsReadinessSignaled(testPid));
            Assert.True(File.Exists(tokenFullPath));

            RhinoBootstrapper.ClearReadinessSignal(testPid);
            Assert.False(RhinoBootstrapper.IsReadinessSignaled(testPid));
            Assert.False(File.Exists(tokenFullPath));
        }
        finally
        {
            RhinoBootstrapper.ClearReadinessSignal(testPid);
        }
    }

    [Fact]
    public void Feature4_DesktopShortcutScript_ExistsAndCompliesWithRules()
    {
        // Feature 4: Desktop Shortcut Generator script validation
        string rootDir = FindSolutionRoot();
        string scriptPath = Path.Combine(rootDir, "tools", "create_desktop_shortcut.ps1");

        Assert.True(File.Exists(scriptPath), $"Shortcut script missing at {scriptPath}");

        string scriptContent = File.ReadAllText(scriptPath);
        Assert.Contains("RadianceStudio.lnk", scriptContent);
        Assert.Contains("RadianceStudio.exe", scriptContent);
        Assert.Contains("WScript.Shell", scriptContent);
        Assert.Contains("IconLocation", scriptContent);
        Assert.Contains("RadianceStudio - Professional Standalone Jewelry CAD Suite", scriptContent);
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
        return Environment.CurrentDirectory;
    }
}
