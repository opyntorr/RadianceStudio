namespace RadianceStudio.Core.Tests.Adversarial;

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using RadianceStudio.Launcher;
using Xunit;

/// <summary>
/// Adversarial stress testing for RhinoLocator.
/// Tests missing environment variables, arbitrary paths, quoted paths, non-existent files,
/// probe enumeration, and concurrent discovery requests.
/// </summary>
public class AdversarialRhinoLocatorTests
{
    private static readonly object EnvLock = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n\r")]
    public void RhinoLocator_EmptyOrWhitespaceEnvVar_DoesNotThrow(string emptyVal)
    {
        lock (EnvLock)
        {
            string? orig = Environment.GetEnvironmentVariable("RHINO8_PATH");
            try
            {
                Environment.SetEnvironmentVariable("RHINO8_PATH", emptyVal);
                string? found = RhinoLocator.FindRhinoExecutable();
                // If it returned a path, it must be an existing file from standard directories
                if (found != null)
                {
                    Assert.True(File.Exists(found));
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("RHINO8_PATH", orig);
            }
        }
    }

    [Theory]
    [InlineData("C:\\NonExistent_Dir_98765\\FakeRhino.exe")]
    [InlineData("/invalid/unix/path/to/fake_rhino")]
    [InlineData("relative/path/rhino.bin")]
    [InlineData("C:\\Invalid::Path??**||.exe")]
    [InlineData("   C:\\AnotherBogusPath\\Rhino.exe   ")]
    public void RhinoLocator_BogusEnvVar_NeverReturnsBogusPath(string bogusPath)
    {
        lock (EnvLock)
        {
            string? orig = Environment.GetEnvironmentVariable("RHINO8_PATH");
            try
            {
                Environment.SetEnvironmentVariable("RHINO8_PATH", bogusPath);
                string? found = RhinoLocator.FindRhinoExecutable();

                if (found != null)
                {
                    // If a path was returned, it must NOT be the bogus path and must exist
                    Assert.NotEqual(bogusPath.Trim(), found);
                    Assert.True(File.Exists(found));
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("RHINO8_PATH", orig);
            }
        }
    }

    [Fact]
    public void RhinoLocator_QuotedValidEnvVar_ResolvesCorrectly()
    {
        lock (EnvLock)
        {
            string? orig = Environment.GetEnvironmentVariable("RHINO8_PATH");
            string tempFile = Path.Combine(Path.GetTempPath(), $"MockRhino_{Guid.NewGuid():N}.exe");

            try
            {
                File.WriteAllText(tempFile, "MOCK_RHINO_PAYLOAD");
                string quotedPath = $"\"{tempFile}\"";

                Environment.SetEnvironmentVariable("RHINO8_PATH", quotedPath);

                string? found = RhinoLocator.FindRhinoExecutable();
                Assert.NotNull(found);
                Assert.Equal(Path.GetFullPath(tempFile), found);

                bool success = RhinoLocator.TryFindRhinoExecutable(out string? tryFound);
                Assert.True(success);
                Assert.Equal(Path.GetFullPath(tempFile), tryFound);
            }
            finally
            {
                Environment.SetEnvironmentVariable("RHINO8_PATH", orig);
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
    }

    [Fact]
    public void RhinoLocator_GetDefaultExpectedPath_ReturnsNonEmptyPlatformPath()
    {
        string defaultPath = RhinoLocator.GetDefaultExpectedPath();
        Assert.False(string.IsNullOrWhiteSpace(defaultPath));

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Contains("Rhino.exe", defaultPath, StringComparison.OrdinalIgnoreCase);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Assert.Contains("Rhinoceros", defaultPath);
        }
    }

    [Fact]
    public void RhinoLocator_GetProbePaths_EnumeratesNonNullPaths()
    {
        var probePaths = RhinoLocator.GetProbePaths().ToList();
        Assert.NotEmpty(probePaths);

        foreach (var path in probePaths)
        {
            Assert.False(string.IsNullOrWhiteSpace(path));
        }
    }

    [Fact]
    public async Task RhinoLocator_ConcurrentInvocations_ThreadSafe()
    {
        const int concurrentTasks = 50;
        var tasks = Enumerable.Range(0, concurrentTasks).Select(_ =>
            Task.Run(() =>
            {
                string defaultPath = RhinoLocator.GetDefaultExpectedPath();
                Assert.False(string.IsNullOrWhiteSpace(defaultPath));

                var probes = RhinoLocator.GetProbePaths().ToList();
                Assert.NotEmpty(probes);

                // FindRhinoExecutable should execute cleanly
                string? result = RhinoLocator.FindRhinoExecutable();
                if (result != null)
                {
                    Assert.True(File.Exists(result));
                }
            })
        );

        await Task.WhenAll(tasks);
    }
}
