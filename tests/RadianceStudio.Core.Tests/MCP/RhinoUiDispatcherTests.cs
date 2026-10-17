namespace RadianceStudio.Core.Tests.MCP;

using System;
using System.Threading.Tasks;
using RadianceStudio.Core.Engines;
using RadianceStudio.MCP.Dispatcher;
using Xunit;

/// <summary>
/// Genuine verification tests for Feature 17: Thread-Safe Rhino UI Dispatcher.
/// Tests the production RadianceStudio.MCP.Dispatcher.RhinoUiDispatcher directly without mock facades.
/// Validates thread synchronization, TaskCompletionSource resolution, headless CI fallback guard,
/// and exception propagation when running outside live Rhino.exe.
/// </summary>
public class RhinoUiDispatcherTests
{
    [Fact]
    public async Task Dispatcher_HeadlessFallback_ExecutesActionSynchronouslyWithoutDllNotFound()
    {
        // When running in headless test runner (RailEngine.IsRhinoNativeAvailable == false),
        // dispatcher must execute action safely without throwing rhcommon_c DllNotFoundException.
        bool executed = false;
        var result = await RhinoUiDispatcher.RunOnMainThread(() =>
        {
            executed = true;
            return "SUCCESS_PAYLOAD";
        });

        Assert.True(executed);
        Assert.Equal("SUCCESS_PAYLOAD", result);
    }

    [Fact]
    public async Task Dispatcher_PropagatesExceptions_WhenActionThrows()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await RhinoUiDispatcher.RunOnMainThread<string>(() =>
            {
                throw new InvalidOperationException("Simulation of dispatch failure");
            });
        });

        Assert.Equal("Simulation of dispatch failure", ex.Message);
    }

    [Fact]
    public async Task Dispatcher_VoidAction_CompletesSuccessfully()
    {
        int sideEffect = 0;
        await RhinoUiDispatcher.RunOnMainThread(() =>
        {
            sideEffect = 42;
        });

        Assert.Equal(42, sideEffect);
    }

    [Fact]
    public async Task Dispatcher_HandlesConcurrentInvocations()
    {
        const int concurrentTasks = 50;
        var tasks = new Task<int>[concurrentTasks];

        for (int i = 0; i < concurrentTasks; i++)
        {
            int captured = i;
            tasks[i] = RhinoUiDispatcher.RunOnMainThread(() => captured * 2);
        }

        var results = await Task.WhenAll(tasks);

        for (int i = 0; i < concurrentTasks; i++)
        {
            Assert.Equal(i * 2, results[i]);
        }
    }

    [Fact]
    public async Task Dispatcher_NullAction_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
        {
            return RhinoUiDispatcher.RunOnMainThread<string>(null!);
        });
    }
}
