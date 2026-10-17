namespace RadianceStudio.Launcher;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

/// <summary>
/// Provides cross-platform discovery of the Rhino 8 executable.
/// Complies with Clean-Room Chinese Wall requirements (no registry, no Win32, no P/Invoke).
/// </summary>
public static class RhinoLocator
{
    private const string EnvVarRhinoPath = "RHINO8_PATH";
    private const string WindowsRelativeSubPath = @"Rhino 8\System\Rhino.exe";
    private const string MacStandardPath = "/Applications/Rhino 8.app/Contents/MacOS/Rhinoceros";

    /// <summary>
    /// Searches standard system paths and environment variables for the Rhino 8 executable.
    /// </summary>
    /// <returns>Full path to Rhino 8 executable if located; otherwise null.</returns>
    public static string? FindRhinoExecutable()
    {
        // 1. Environment variable override (RHINO8_PATH)
        string? envPath = Environment.GetEnvironmentVariable(EnvVarRhinoPath);
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            string trimmed = envPath.Trim('\"', ' ', '\t');
            if (File.Exists(trimmed))
            {
                return Path.GetFullPath(trimmed);
            }
        }

        // 2. Windows Standard Installations
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrWhiteSpace(programFiles))
            {
                string candidate = Path.Combine(programFiles, WindowsRelativeSubPath);
                if (File.Exists(candidate))
                {
                    return Path.GetFullPath(candidate);
                }
            }

            // Fallback to 64-bit ProgramW6432 if present
            string? progW64 = Environment.GetEnvironmentVariable("ProgramW6432");
            if (!string.IsNullOrWhiteSpace(progW64))
            {
                string candidate = Path.Combine(progW64, WindowsRelativeSubPath);
                if (File.Exists(candidate))
                {
                    return Path.GetFullPath(candidate);
                }
            }

            // Fallback to default system drive Program Files
            string root = Path.GetPathRoot(programFiles) ?? @"C:\";
            string fallbackCandidate = Path.Combine(root, "Program Files", WindowsRelativeSubPath);
            if (File.Exists(fallbackCandidate))
            {
                return Path.GetFullPath(fallbackCandidate);
            }
        }

        // 3. macOS Standard Application Bundle
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            if (File.Exists(MacStandardPath))
            {
                return MacStandardPath;
            }

            // User application bundle fallback
            string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(userHome))
            {
                string userMacPath = Path.Combine(userHome, "Applications", "Rhino 8.app", "Contents", "MacOS", "Rhinoceros");
                if (File.Exists(userMacPath))
                {
                    return userMacPath;
                }
            }
        }

        // 4. Linux / Containerized Environments
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string[] linuxCandidates = ["/usr/bin/rhino8", "/usr/local/bin/rhino8", "/opt/rhino8/rhino"];
            foreach (string candidate in linuxCandidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Attempts to find the Rhino 8 executable and returns a boolean status.
    /// </summary>
    public static bool TryFindRhinoExecutable([NotNullWhen(true)] out string? executablePath)
    {
        executablePath = FindRhinoExecutable();
        return executablePath != null;
    }

    /// <summary>
    /// Returns the default expected path for diagnostic messages on the current OS.
    /// </summary>
    public static string GetDefaultExpectedPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            return Path.Combine(string.IsNullOrWhiteSpace(programFiles) ? @"C:\Program Files" : programFiles, WindowsRelativeSubPath);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return MacStandardPath;
        }

        return "/usr/bin/rhino8";
    }

    /// <summary>
    /// Enumerates all probe candidate paths searched on the current system.
    /// </summary>
    public static IEnumerable<string> GetProbePaths()
    {
        string? envPath = Environment.GetEnvironmentVariable(EnvVarRhinoPath);
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            yield return envPath.Trim('\"', ' ', '\t');
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrWhiteSpace(programFiles))
            {
                yield return Path.Combine(programFiles, WindowsRelativeSubPath);
            }

            string? progW64 = Environment.GetEnvironmentVariable("ProgramW6432");
            if (!string.IsNullOrWhiteSpace(progW64))
            {
                yield return Path.Combine(progW64, WindowsRelativeSubPath);
            }

            string root = Path.GetPathRoot(programFiles) ?? @"C:\";
            yield return Path.Combine(root, "Program Files", WindowsRelativeSubPath);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            yield return MacStandardPath;
            string userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(userHome))
            {
                yield return Path.Combine(userHome, "Applications", "Rhino 8.app", "Contents", "MacOS", "Rhinoceros");
            }
        }
        else
        {
            yield return "/usr/bin/rhino8";
            yield return "/usr/local/bin/rhino8";
        }
    }
}
