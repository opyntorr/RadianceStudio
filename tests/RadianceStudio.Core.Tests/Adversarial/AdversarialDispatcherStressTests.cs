namespace RadianceStudio.Core.Tests.Adversarial;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RadianceStudio.MCP.Dispatcher;
using Xunit;

/// <summary>
/// Adversarial stress testing for RhinoUiDispatcher.
/// Tests concurrent invocations, execution ordering, exception isolation across threads,
/// and null argument guards under high throughput.
/// </summary>
public class AdversarialDispatcherStressTests
{
    [Fact]
    public async Task Dispatcher_NullActions_ThrowArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => RhinoUiDispatcher.RunOnMainThread<int>(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => RhinoUiDispatcher.RunOnMainThreadAsync<int>(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => RhinoUiDispatcher.RunOnMainThread((Action)null!));
    }

    [Fact]
    public async Task Dispatcher_HighConcurrencyStress_200ParallelCalls_AllSucceed()
    {
        const int taskCount = 200;
        var tasks = new Task<int>[taskCount];

        for (int i = 0; i < taskCount; i++)
        {
            int captured = i;
            tasks[i] = Task.Run(() => RhinoUiDispatcher.RunOnMainThread(() => captured * 3));
        }

        int[] results = await Task.WhenAll(tasks);

        Assert.Equal(taskCount, results.Length);
        for (int i = 0; i < taskCount; i++)
        {
            Assert.Equal(i * 3, results[i]);
        }
    }

    [Fact]
    public async Task Dispatcher_MixedSuccessAndExceptions_IsolatesErrorsSafely()
    {
        const int totalCount = 100;
        var tasks = new Task<int>[totalCount];

        for (int i = 0; i < totalCount; i++)
        {
            int index = i;
            if (index % 2 == 0)
            {
                // Succeeding task
                tasks[i] = Task.Run(() => RhinoUiDispatcher.RunOnMainThread(() => index * 10));
            }
            else
            {
                // Failing task
                tasks[i] = Task.Run(async () =>
                {
                    try
                    {
                        await RhinoUiDispatcher.RunOnMainThread<int>(() =>
                        {
                            throw new InvalidOperationException($"Failure at index {index}");
                        });
                        return -1; // Should not reach
                    }
                    catch (InvalidOperationException ex)
                    {
                        Assert.Equal($"Failure at index {index}", ex.Message);
                        return -999; // Caught expected failure
                    }
                });
            }
        }

        int[] results = await Task.WhenAll(tasks);

        for (int i = 0; i < totalCount; i++)
        {
            if (i % 2 == 0)
            {
                Assert.Equal(i * 10, results[i]);
            }
            else
            {
                Assert.Equal(-999, results[i]);
            }
        }
    }

    [Fact]
    public async Task Dispatcher_AsyncActionDispatcher_ExecutesAndPropagatesExceptions()
    {
        // Async success
        var result = await RhinoUiDispatcher.RunOnMainThreadAsync(async () =>
        {
            await Task.Yield();
            return "ASYNC_SUCCESS";
        });
        Assert.Equal("ASYNC_SUCCESS", result);

        // Async failure
        var ex = await Assert.ThrowsAsync<ApplicationException>(async () =>
        {
            await RhinoUiDispatcher.RunOnMainThreadAsync<string>(async () =>
            {
                await Task.Yield();
                throw new ApplicationException("Async dispatch failure simulation");
            });
        });
        Assert.Equal("Async dispatch failure simulation", ex.Message);
    }

    [Fact]
    public async Task Dispatcher_ThroughputStress_1000SequentialAndParallelDispatches()
    {
        const int totalIterations = 1000;
        var bag = new ConcurrentBag<int>();

        var parallelTasks = Enumerable.Range(0, totalIterations).Select(i =>
            Task.Run(async () =>
            {
                int val = await RhinoUiDispatcher.RunOnMainThread(() => i + 1);
                bag.Add(val);
            })
        );

        await Task.WhenAll(parallelTasks);

        Assert.Equal(totalIterations, bag.Count);
        Assert.Equal(totalIterations * (totalIterations + 1) / 2, bag.Sum(x => (long)x));
    }
}
