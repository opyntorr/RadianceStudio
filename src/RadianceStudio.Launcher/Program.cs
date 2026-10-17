namespace RadianceStudio.Launcher;

using System.Diagnostics;
using Eto.Forms;

/// <summary>
/// Entry point for the RadianceStudio Standalone Desktop Launcher.
/// </summary>
public static class Program
{
    /// <summary>
    /// Application main entry point.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Exit code (0 on success, non-zero on failure).</returns>
    [STAThread]
    public static int Main(string[] args)
    {
        // Handle command-line arguments
        if (args.Length > 0)
        {
            string firstArg = args[0].ToLowerInvariant();
            if (firstArg is "--version" or "-v")
            {
                Console.WriteLine("RadianceStudio v1.0.0");
                return 0;
            }

            if (firstArg is "--help" or "-h")
            {
                PrintHelp();
                return 0;
            }

            if (firstArg is "--check" or "--locate")
            {
                return CheckRhinoInstallation();
            }
        }

        bool noSplash = args.Any(a => string.Equals(a, "--no-splash", StringComparison.OrdinalIgnoreCase));

        // 1. Locate Rhino 8 executable
        if (!RhinoLocator.TryFindRhinoExecutable(out string? rhinoPath))
        {
            string error = $"[ERROR] Rhino 8 executable could not be found.\n" +
                           $"Expected path: {RhinoLocator.GetDefaultExpectedPath()}\n" +
                           $"Please install Rhino 8 or set the RHINO8_PATH environment variable.";
            Console.Error.WriteLine(error);

            TryShowErrorDialog("Rhino 8 Not Found", error);
            return 1;
        }

        // 2. Bootstrap Rhino 8 under RadianceStudio scheme
        Process rhinoProcess;
        try
        {
            rhinoProcess = RhinoBootstrapper.Launch(rhinoPath);
        }
        catch (Exception ex)
        {
            string error = $"[ERROR] Failed to bootstrap Rhino under RadianceStudio scheme:\n{ex.Message}";
            Console.Error.WriteLine(error);
            TryShowErrorDialog("Launch Error", error);
            return 1;
        }

        // 3. Display Splash Screen or Headless Synchronization
        if (noSplash)
        {
            Console.WriteLine($"[RadianceStudio] Rhino launched (PID: {rhinoProcess.Id}). Waiting for readiness...");
            bool isReady = RhinoBootstrapper.WaitForReadinessAsync(rhinoProcess, TimeSpan.FromSeconds(60)).GetAwaiter().GetResult();
            if (isReady)
            {
                Console.WriteLine("[RadianceStudio] Viewport initialized successfully.");
            }
            return 0;
        }

        try
        {
            // Initialize Eto Application and display splash screen
            var app = new Application();
            var splash = new SplashScreen(rhinoProcess);
            app.Run(splash);
            return 0;
        }
        catch (Exception ex)
        {
            // Fallback for headless CI or display-less environments
            Console.WriteLine($"[RadianceStudio] Splash screen unavailable ({ex.Message}). Awaiting readiness...");
            bool isReady = RhinoBootstrapper.WaitForReadinessAsync(rhinoProcess, TimeSpan.FromSeconds(60)).GetAwaiter().GetResult();
            if (isReady)
            {
                Console.WriteLine("[RadianceStudio] Readiness signal confirmed.");
            }
            return 0;
        }
    }

    private static int CheckRhinoInstallation()
    {
        Console.WriteLine("Probing for Rhino 8 installation...");
        foreach (string probe in RhinoLocator.GetProbePaths())
        {
            bool exists = File.Exists(probe);
            Console.WriteLine($"  [{ (exists ? "FOUND" : "MISSING") }] {probe}");
        }

        string? found = RhinoLocator.FindRhinoExecutable();
        if (found != null)
        {
            Console.WriteLine($"\nActive Rhino 8 Executable: {found}");
            return 0;
        }

        Console.Error.WriteLine("\n[FAIL] No Rhino 8 executable found.");
        return 1;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("RadianceStudio Desktop Launcher");
        Console.WriteLine("Usage: RadianceStudio [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --no-splash    Launch Rhino without displaying splash screen");
        Console.WriteLine("  --check        Probe system for Rhino 8 executable without launching");
        Console.WriteLine("  --version, -v  Display version information");
        Console.WriteLine("  --help, -h     Display this help documentation");
    }

    private static void TryShowErrorDialog(string title, string message)
    {
        try
        {
            var app = Application.Instance ?? new Application();
            MessageBox.Show(message, title, MessageBoxType.Error);
        }
        catch
        {
            // Console error already logged
        }
    }
}
