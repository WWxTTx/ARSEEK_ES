using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// https://stackoverflow.com/questions/24980427/task-factory-fromasync-with-cancellationtokensource
/// </summary>
public static class TaskExtensions
{
    public async static Task<TResult> HandleCancellation<TResult>(
        this Task<TResult> asyncTask,
        CancellationToken cancellationToken)
    {
        // Create another task that completes as soon as cancellation is requested.
        // http://stackoverflow.com/a/18672893/1149773
        var tcs = new TaskCompletionSource<TResult>();
        var cancellationTask = tcs.Task;

        // 注册CancellationTokenSource取消或消失后需要执行的动作
        // CancellationTokenSource被取消后尝试取消cancellationTask
        cancellationToken.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false);


        // Create a task that completes when either the async operation completes, or cancellation is requested.
        // 返回提供的任务中任何一个已完成的任务
        var readyTask = await Task.WhenAny(asyncTask, cancellationTask);

        // In case of cancellation, register a continuation to observe any unhandled 
        // exceptions from the asynchronous operation (once it completes).
        // In .NET 4.0, unobserved task exceptions would terminate the process.
        // ContinueWith在Task完成后继续异步执行传入的委托，可用于实现异常捕获
        if (readyTask == cancellationTask)
            asyncTask.ContinueWith(_ => asyncTask.Exception,
                TaskContinuationOptions.OnlyOnFaulted |
                TaskContinuationOptions.ExecuteSynchronously);

        asyncTask.ContinueWith((task) => task.Exception);

        return await readyTask;
    }
}

/// <summary>
/// 任务信号
/// </summary>
public struct Semaphore
{
    /// <summary>
    /// 通知线程等待或继续
    /// https://learn.microsoft.com/en-us/dotnet/api/system.threading.manualresetevent
    /// </summary>
    public ManualResetEvent mre;
    /// <summary>
    /// 通知线程取消
    /// https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtokensource
    /// </summary>
    public CancellationTokenSource cts;

    public Semaphore(ManualResetEvent mre, CancellationTokenSource cts)
    {
        this.mre = mre;
        this.cts = cts;
    }
}