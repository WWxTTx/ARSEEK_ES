using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Events;
using Aliyun.OSS;
using static UnityFramework.Runtime.RequestData;
using UnityFramework.Runtime;

/// <summary>
/// 阿里云Task工具类
/// </summary>
public class AliyunTaskUtils
{
    /// <summary>
    /// 异步简单上传文件
    /// </summary>
    /// <param name="config"></param>
    /// <param name="filePath">本地文件路径</param>
    /// <param name="savePath">上传object name</param>
    /// <param name="progress">上传进度回调</param>
    /// <param name="error">上传失败回调</param>
    /// <param name="finish">上传完成回调</param>
    /// <returns></returns>
    public async Task<bool> PutObjectAsync(StsBase config, string filePath, string savePath, 
        UnityAction<float> progress = null, 
        UnityAction error = null, 
        UnityAction finish = null)
    {
        AliyunOSSStsInfo aliyunOSSStsInfo = config as AliyunOSSStsInfo;

        Log.Debug("[AliyunOSS] PutObjectAsync Start");
        OssClient ossClient = new OssClient(aliyunOSSStsInfo.endpoint, aliyunOSSStsInfo.accessKeyId, aliyunOSSStsInfo.accessKeySecret, aliyunOSSStsInfo.securityToken);
        try
        {
            //初始化ManualResetEvent类的新实例，布尔值表示是否将初始状态设置为已发出信号
            StorageManager.Instance.TaskSemaphores.Add(filePath, new Semaphore(new ManualResetEvent(true), new CancellationTokenSource()));

            using (var fs = File.Open(filePath, FileMode.Open))
            {
                //简单上传
                PutObjectRequest putObjectRequest = new PutObjectRequest(aliyunOSSStsInfo.bucket, savePath, fs);
                putObjectRequest.StreamTransferProgress += (sender, args) =>
                {
                    progress?.Invoke((float)args.TransferredBytes / args.TotalBytes * 100f);
                    //当mre状态为无信号时，阻塞当前线程
                    StorageManager.Instance.TaskSemaphores[filePath].mre.WaitOne();
                };

                //将Begin/End异步编程模式(APM)封装为async/await基于任务的异步模式(TAP)
                //https://github.com/aliyun/aliyun-oss-csharp-sdk/issues/55
                //https://dzone.com/articles/wrapping-beginend-asynchronous
                var result = await Task.Factory.FromAsync((cb, o) => ossClient.BeginPutObject(putObjectRequest, cb, o), ossClient.EndPutObject, null)
                    .HandleCancellation(StorageManager.Instance.TaskSemaphores[filePath].cts.Token);

                //完成上传后释放资源
                if (StorageManager.Instance.TaskSemaphores.TryGetValue(filePath, out Semaphore semaphore))
                {
                    semaphore.mre.Dispose();
                    semaphore.cts.Dispose();
                    StorageManager.Instance.TaskSemaphores.Remove(filePath);
                }

                Log.Debug($"[AliyunOSS] PutObjectAsync Finish with status code:{result.HttpStatusCode}");
                finish?.Invoke();
                return result.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[AliyunOSS] PutObjectAsync Exception: {ex.Message}");
            error?.Invoke();
            return false;
        }
    }
}