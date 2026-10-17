namespace RadianceStudio.Launcher;

using System.Diagnostics;

/// <summary>
/// Bootstraps Rhino 8 under the isolated RadianceStudio scheme.
/// Handles CLI parameter configuration and readiness signal synchronization.
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
    /// Launches the Rhino 8 process configured for RadianceStudio.
    /// </summary>
    /// <param name="executablePath">Optional path to rhino executable; if null, RhinoLocator is used.</param>
    /// <param name="additionalArguments">Optional additional command line arguments to append.</param>
    /// <returns>The spawned Rhino Process.</returns>
    /// <exception cref="FileNotFoundException">Thrown when Rhino 8 executable is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when process failed to start.</exception>
    public static Process Launch(string? executablePath = null, string? additionalArguments = null)
    {
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
