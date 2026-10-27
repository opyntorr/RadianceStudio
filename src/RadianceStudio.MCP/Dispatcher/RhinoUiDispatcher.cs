namespace RadianceStudio.MCP.Dispatcher;

using System;
using System.Threading.Tasks;
using RadianceStudio.Core.Engines;
using Rhino;

/// <summary>
/// Thread-safe dispatcher executing RhinoCommon operations strictly on the Rhino UI thread.
/// Bridges background MCP socket/network threads to RhinoApp safely using TaskCompletionSource.
/// Includes headless CI fallback guard using RailEngine.IsRhinoNativeAvailable so that
/// running outside Rhino.exe executes on the calling thread without throwing DllNotFoundException.
/// If already executing on the Rhino UI thread (RhinoApp.IsOnMainThread), executes synchronously
/// to prevent message pump deadlocks.
/// </summary>
public static class RhinoUiDispatcher
{
    /// <summary>
    /// Executes a function on the main Rhino UI thread and returns its result asynchronously.
    /// In headless CI environments where Rhino.exe is not hosting the process, falls back
    /// to executing directly on the calling thread.
    /// If called from Rhino's main UI thread, executes directly without re-dispatching.
    /// </summary>
    public static Task<T> RunOnMainThread<T>(Func<T> action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (!RailEngine.IsRhinoNativeAvailable || RhinoApp.IsOnMainThread)
        {
            try
            {
                var result = action();
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                return Task.FromException<T>(ex);
            }
        }

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            RhinoApp.InvokeOnUiThread((Action)(() =>
            {
                try
                {
                    var result = action();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }));
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
        }

        return tcs.Task;
    }

    /// <summary>
    /// Executes an asynchronous function on the main Rhino UI thread.
    /// If called from Rhino's main UI thread, executes directly without re-dispatching.
    /// </summary>
    public static async Task<T> RunOnMainThreadAsync<T>(Func<Task<T>> action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (!RailEngine.IsRhinoNativeAvailable || RhinoApp.IsOnMainThread)
        {
            return await action().ConfigureAwait(false);
        }

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            RhinoApp.InvokeOnUiThread((Action)(async () =>
            {
                try
                {
                    var result = await action().ConfigureAwait(false);
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }));
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
        }

        return await tcs.Task.ConfigureAwait(false);
    }

    /// <summary>
    /// Executes an action on the main Rhino UI thread.
    /// </summary>
    public static Task RunOnMainThread(Action action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        return RunOnMainThread<bool>(() =>
        {
            action();
            return true;
        });
    }
}
