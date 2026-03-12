using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Events;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;

/// <summary>
/// 文件存储工具类 异步操作
/// </summary>
public class StorageManager : Singleton<StorageManager>
{
    public Dictionary<string, Semaphore> TaskSemaphores = new Dictionary<string, Semaphore>();

    public bool TaskCompleted => TaskSemaphores.Count == 0;

    //private Lazy<MinioUtils> minioUtils = new Lazy<MinioUtils>();
    private Lazy<AliyunTaskUtils> aliyunUtils = new Lazy<AliyunTaskUtils>();

    public async Task<bool> PutObjectAsync(StsBase config, string filePath, string savePath,
        UnityAction<float> progress = null,
        UnityAction error = null,
        UnityAction finish = null)
    {
        bool result = false;
        switch (config.StorageType)
        {
            //case StsBase.StoreType.Minio:
            //    result = await minioUtils.Value.PutObjectAsync(config, filePath, savePath, progress, error, finish);
            //    break;
            case StsBase.StoreType.AliyunOSS:
                result = await aliyunUtils.Value.PutObjectAsync(config, filePath, savePath, progress, error, finish);
                break;
            default:
                break;
        }
        return result;
    }

    /// <summary>
    /// 暂停任务
    /// </summary>
    /// <param name="path"></param>
    public void PauseTask(string path)
    {
        if (TaskSemaphores == null)
            return;
        if (TaskSemaphores.TryGetValue(path, out Semaphore semaphore))
        {
            Log.Debug("任务暂停 " + path);
            //将mre状态设置为无信号，导致线程阻塞
            semaphore.mre.Reset();
        }
        else
        {
            Log.Error("任务不存在 " + path);
        }
    }

    /// <summary>
    /// 继续任务
    /// </summary>
    /// <param name="path"></param>
    public void ResumeTask(string path)
    {
        if (TaskSemaphores == null)
            return;
        if (TaskSemaphores.TryGetValue(path, out Semaphore semaphore))
        {
            Log.Debug("任务继续 " + path);
            //将mre状态设置为已发出信号，允许等待线程继续处理
            semaphore.mre.Set();
        }
        else
        {
            Log.Error("任务不存在 " + path);
        }
    }

    /// <summary>
    /// 取消任务
    /// </summary>
    /// <param name="path"></param>
    public void CancelTask(string path)
    {
        if (TaskSemaphores == null)
            return;
        if (TaskSemaphores.TryGetValue(path, out Semaphore semaphore))
        {
            Log.Debug("任务取消 " + path);
            semaphore.mre.Set();
            semaphore.mre.Dispose();
            semaphore.cts.Cancel();
            semaphore.cts.Dispose();
            TaskSemaphores.Remove(path);
        }
        else
        {
            Log.Error("任务不存在 " + path);
        }
    }

    public void ReleaseAllTask()
    {
        if (TaskSemaphores == null)
            return;

        foreach (var task in TaskSemaphores)
        {
            //确保所有等待的线程被释放，避免在退出运行时卡死
            task.Value.mre.Set();
            task.Value.mre.Dispose();
            task.Value.cts.Cancel();
            task.Value.cts.Dispose();
        }
        TaskSemaphores.Clear();
        TaskSemaphores = null;
    }

    private void OnDestroy()
    {
        ReleaseAllTask();
    }
}