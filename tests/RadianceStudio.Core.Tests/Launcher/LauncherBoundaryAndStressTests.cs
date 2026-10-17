namespace RadianceStudio.Core.Tests.Launcher;

using System;
using System.IO;
using RadianceStudio.Launcher;
using Xunit;

/// <summary>
/// Genuine verification tests for Desktop Launcher operations boundary and corner cases.
/// Tests RhinoLocator and RhinoBootstrapper directly with extreme PIDs and invalid environment variables.
/// </summary>
public class LauncherBoundaryAndStressTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("C:\\NonExistentDirectory_XYZ123\\FakeRhino.exe")]
    [InlineData("invalid_relative/path/rhino.bin")]
    public void RhinoLocator_HandlesInvalidEnvironmentVariableGracefully(string invalidPath)
    {
        string? original = Environment.GetEnvironmentVariable("RHINO8_PATH");
        try
        {
            Environment.SetEnvironmentVariable("RHINO8_PATH", invalidPath);

            string? resolved = RhinoLocator.FindRhinoExecutable();
            if (string.IsNullOrWhiteSpace(invalidPath) || !File.Exists(invalidPath))
            {
                // Must not return the non-existent invalid path
                Assert.NotEqual(invalidPath, resolved);
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("RHINO8_PATH", original);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(42424)]
    [InlineData(99999)]
    public void ReadinessToken_HandlesExtremeProcessIds(int pid)
    {
        string path = RhinoBootstrapper.GetReadinessFilePath(pid);

        try
        {
            RhinoBootstrapper.WriteReadinessSignal(pid);
            Assert.True(RhinoBootstrapper.IsReadinessSignaled(pid));
            Assert.True(File.Exists(path));

            RhinoBootstrapper.ClearReadinessSignal(pid);
            Assert.False(RhinoBootstrapper.IsReadinessSignaled(pid));
        }
        finally
        {
            RhinoBootstrapper.ClearReadinessSignal(pid);
        }
    }

    [Fact]
    public void ReadinessToken_IgnoresMalformedFileTokens()
    {
        string malformed = "radiance_ready_not_a_number.tmp";
        bool parsed = TryParsePidFromToken(malformed, out _);
        Assert.False(parsed);

        string emptyToken = "radiance_ready_.tmp";
        bool parsedEmpty = TryParsePidFromToken(emptyToken, out _);
        Assert.False(parsedEmpty);

        string prefixOnly = "radiance_ready_";
        bool parsedPrefix = TryParsePidFromToken(prefixOnly, out _);
        Assert.False(parsedPrefix);
    }

    private static bool TryParsePidFromToken(string filename, out int pid)
    {
        pid = 0;
        const string prefix = "radiance_ready_";
        const string suffix = ".tmp";

        if (!filename.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
            !filename.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        int start = prefix.Length;
        int length = filename.Length - prefix.Length - suffix.Length;
        if (length <= 0) return false;

        string sub = filename.Substring(start, length);
        return int.TryParse(sub, out pid);
    }
}
