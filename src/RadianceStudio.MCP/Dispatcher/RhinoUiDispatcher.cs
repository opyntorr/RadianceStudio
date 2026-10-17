namespace RadianceStudio.MCP.Dispatcher;

using System;
using System.Threading.Tasks;
using Rhino;

/// <summary>
/// Thread-safe dispatcher executing RhinoCommon operations strictly on the Rhino UI thread.
/// Bridges background MCP socket/network threads to RhinoApp safely using TaskCompletionSource.
/// </summary>
public static class RhinoUiDispatcher
{
    /// <summary>
    /// Executes a function on the main Rhino UI thread and returns its result asynchronously.
    /// </summary>
    public static Task<T> RunOnMainThread<T>(Func<T> action)
    {
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
    /// Executes an action on the main Rhino UI thread.
    /// </summary>
    public static Task RunOnMainThread(Action action)
    {
        return RunOnMainThread<bool>(() =>
        {
            action();
            return true;
        });
    }
}
