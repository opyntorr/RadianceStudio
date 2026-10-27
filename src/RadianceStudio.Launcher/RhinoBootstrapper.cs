namespace RadianceStudio.Launcher;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Bootstraps Rhino 8 under the isolated RadianceStudio scheme.
/// Handles CLI parameter configuration, automatic package deployment, and readiness signal synchronization.
/// </summary>
public static class RhinoBootstrapper
{
    /// <summary>
    /// Scheme hive identifier for RadianceStudio isolation.
    /// </summary>
    public const string SchemeName = "RadianceStudio";

    /// <summary>
    /// Startup command executed by Rhino immediately upon viewport initialization.
    /// </summary>
    public const string StartupCommand = "_-RadInitializeStudio";

    /// <summary>
    /// Standard CLI arguments required to launch Rhino under the RadianceStudio scheme.
    /// </summary>
    public const string StandardArguments = "/scheme=\"RadianceStudio\" /nosplash /runscript=\"_-RadInitializeStudio\"";

    /// <summary>
    /// Ensures that RadianceStudio.Rhino.rhp and its dependencies are deployed to Rhino 8's packages directory
    /// (%APPDATA%\McNeel\Rhinoceros\packages\8.0\RadianceStudio\1.0.0\net8.0) so Rhino 8 automatically discovers
    /// and loads the plugin and all 12 jewelry commands across all schemes.
    /// Uses purely cross-platform System.IO APIs compliant with Clean-Room requirements.
    /// </summary>
    public static void EnsurePluginDeployed()
    {
        try
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrWhiteSpace(appData)) return;

            string packageBase = Path.Combine(appData, "McNeel", "Rhinoceros", "packages", "8.0", "RadianceStudio", "1.0.0");
            string packageNet8 = Path.Combine(packageBase, "net8.0");

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidateDirs =
            {
                baseDir,
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "RadianceStudio.Rhino", "bin", "Release", "net8.0")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "RadianceStudio.Rhino", "bin", "Debug", "net8.0")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "src", "RadianceStudio.Rhino", "bin", "Release", "net8.0")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "src", "RadianceStudio.Rhino", "bin", "Debug", "net8.0"))
            };

            string? sourceDir = candidateDirs.FirstOrDefault(d => Directory.Exists(d) && File.Exists(Path.Combine(d, "RadianceStudio.Rhino.rhp")));
            if (sourceDir == null) return;

            Directory.CreateDirectory(packageNet8);

            string manifestPath = Path.Combine(packageBase, "manifest.yml");
            if (!File.Exists(manifestPath))
            {
                string manifest = "---\nname: RadianceStudio\nversion: 1.0.0\nauthors:\n- RadianceStudio Team\ndescription: Commercial-Grade Parametric Jewelry CAD Suite for Rhino 8\n";
                File.WriteAllText(manifestPath, manifest);
            }

            foreach (string file in Directory.GetFiles(sourceDir, "*.*"))
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();
                if (ext is ".rhp" or ".dll" or ".pdb")
                {
                    string dest = Path.Combine(packageNet8, Path.GetFileName(file));
                    if (!File.Exists(dest) || File.GetLastWriteTimeUtc(file) > File.GetLastWriteTimeUtc(dest))
                    {
                        File.Copy(file, dest, overwrite: true);
                    }
                }
            }
        }
        catch
        {
            // Non-critical background deployment fallback
        }
    }

    /// <summary>
    /// Launches the Rhino 8 process configured for RadianceStudio.
    /// </summary>
    /// <param name="executablePath">Optional path to rhino executable; if null, RhinoLocator is used.</param>
    /// <param name="additionalArguments">Optional additional command line arguments to append.</param>
    /// <returns>The spawned Rhino Process.</returns>
    /// <exception cref="FileNotFoundException">Thrown when Rhino 8 executable is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when process failed to start.</exception>
    public static Process Launch(string? executablePath = null, string? additionalArguments = null)
    {
        EnsurePluginDeployed();

        string? targetPath = executablePath ?? RhinoLocator.FindRhinoExecutable();
        if (string.IsNullOrWhiteSpace(targetPath) || !File.Exists(targetPath))
        {
            throw new FileNotFoundException(
                $"Rhino 8 executable could not be located. Expected path: '{RhinoLocator.GetDefaultExpectedPath()}'. " +
                "Please install Rhino 8 or set the RHINO8_PATH environment variable.");
        }

        string arguments = string.IsNullOrWhiteSpace(additionalArguments)
            ? StandardArguments
            : $"{StandardArguments} {additionalArguments.Trim()}";

        var startInfo = new ProcessStartInfo
        {
            FileName = targetPath,
            Arguments = arguments,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(targetPath) ?? Environment.CurrentDirectory
        };

        Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start process '{targetPath}' with arguments '{arguments}'.");

        return process;
    }

    /// <summary>
    /// Gets the temporary signal file path used by RadInitializeStudioCommand to signal viewport readiness.
    /// </summary>
    /// <param name="processId">Target process ID.</param>
    /// <returns>Path in the system temporary directory: %TEMP%\radiance_ready_&lt;pid&gt;.tmp</returns>
    public static string GetReadinessFilePath(int processId)
    {
        string tempDir = Path.GetTempPath();
        return Path.Combine(tempDir, $"radiance_ready_{processId}.tmp");
    }

    /// <summary>
    /// Checks whether the readiness signal file exists for the specified process ID.
    /// </summary>
    public static bool IsReadinessSignaled(int processId)
    {
        try
        {
            string path = GetReadinessFilePath(processId);
            return File.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Deletes the readiness signal file for the specified process ID if it exists.
    /// </summary>
    public static void ClearReadinessSignal(int processId)
    {
        try
        {
            string path = GetReadinessFilePath(processId);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Ignore temporary file cleanup errors
        }
    }

    /// <summary>
    /// Creates the readiness signal file for the specified process ID (useful for testing and simulators).
    /// </summary>
    public static void WriteReadinessSignal(int processId)
    {
        try
        {
            string path = GetReadinessFilePath(processId);
            File.WriteAllText(path, DateTime.UtcNow.ToString("O"));
        }
        catch
        {
            // Ignore file write errors
        }
    }

    /// <summary>
    /// Asynchronously waits for Rhino readiness signal or process exit.
    /// </summary>
    public static async Task<bool> WaitForReadinessAsync(Process process, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        DateTime start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout && !cancellationToken.IsCancellationRequested)
        {
            if (process.HasExited)
            {
                return false;
            }

            if (IsReadinessSignaled(process.Id))
            {
                ClearReadinessSignal(process.Id);
                return true;
            }

            await Task.Delay(100, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }
}
