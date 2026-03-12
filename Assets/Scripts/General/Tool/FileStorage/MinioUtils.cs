using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.DataModel.Encryption;
using Minio.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Events;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;

/// <summary>
/// MinIO工具类
/// </summary>
public class MinioUtils
{
    private IMinioClient minioClient;

    private object taskLock = new object();
    private HashSet<string> uploadingTasks = new HashSet<string>();

    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="config"></param>
    /// <param name="filePath"></param>
    /// <param name="savePath"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    public async Task<bool> PutObjectAsync(StsBase config, string filePath, string savePath, 
        UnityAction<float> progress = null,
        UnityAction error = null,
        UnityAction finish = null)
    {
        MinioStsInfo minioStsInfo = config as MinioStsInfo;

        if (minioClient == null)
        {
            InitMinio(minioStsInfo);
        }

        var syncProgress = new SyncProgress<ProgressReport>(progressReport =>
        {
            progress?.Invoke(progressReport.Percentage);//0-100                                                      
            //当mre状态为无信号时，阻塞当前线程
            StorageManager.Instance.TaskSemaphores[filePath].mre.WaitOne();
        });

        //lock(taskLock)
        //    uploadingTasks.Add(filePath);

        //var result = await Task.Run(() =>
        //{
        //    return PutObject.Run(minioClient, config.bucket, savePath, filePath, syncProgress).Result;
        //    //return PutObjectStream.Run(minioClient, config.bucket, savePath, filePath, syncProgress).Result;
        //}).ConfigureAwait(false);

        //lock (taskLock)
        //    uploadingTasks.Remove(filePath);

        //return result;
        try
        {
            Log.Debug($"[MinIO] PutObjectAsync Start");

            //初始化ManualResetEvent类的新实例，布尔值表示是否将初始状态设置为已发出信号
            StorageManager.Instance.TaskSemaphores.Add(filePath, new Semaphore(new ManualResetEvent(true), new CancellationTokenSource()));

            var args = new PutObjectArgs()
              .WithBucket(minioStsInfo.bucket)
              .WithObject(savePath)
              .WithContentType("application/octet-stream")
              .WithFileName(filePath)
              .WithProgress(syncProgress);
            var response = await minioClient.PutObjectAsync(args, StorageManager.Instance.TaskSemaphores[filePath].cts.Token).ConfigureAwait(false);

            //完成上传后释放资源
            if (StorageManager.Instance.TaskSemaphores.TryGetValue(filePath, out Semaphore semaphore))
            {
                semaphore.mre.Dispose();
                semaphore.cts.Dispose();
                StorageManager.Instance.TaskSemaphores.Remove(filePath);
            }

            Log.Debug($"[MinIO] PutObjectAsync Finish with status code:{response.ResponseStatusCode}");
            finish?.Invoke();
            return response.ResponseStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Exception e)
        {
            Log.Error($"[MinIO] PutObjectAsync Exception: {e}");
            error?.Invoke();
            return false;
        }
    }

    private void InitMinio(MinioStsInfo config)
    {
        try
        {
            //if (string.IsNullOrEmpty(config.region)) config.region = "cn-chengdu-1";
            minioClient = new MinioClient()
                .WithEndpoint(config.host, config.port)
                .WithCredentials(config.accessKey, config.secretKey)
                .WithSessionToken(config.sessionToken)
                .WithRegion(config.region)
                .Build();
            Log.Debug($"[MinIO] Init");
        }
        catch (Exception ex)
        {
            Log.Error($"[MinIO] Init Exception: {ex.Message}");
        }
    }
}

/// <summary>
/// Upload object to bucket from file
/// </summary>
internal class PutObject
{
    public static async Task<bool> Run(IMinioClient minio, string bucketName, string objectName, string fileName,
        IProgress<ProgressReport> progress = null,
        IServerSideEncryption sse = null)
    {
        try
        {
            var args = new PutObjectArgs()
              .WithBucket(bucketName)
              .WithObject(objectName)
              .WithContentType("application/octet-stream")
              .WithFileName(fileName)
              .WithProgress(progress)
              .WithServerSideEncryption(sse);
            Log.Debug($"[MinIO] PutObjectAsync Start");
            var response = await minio.PutObjectAsync(args).ConfigureAwait(false);
            Log.Debug($"[MinIO] PutObjectAsync Finish with status code:{response.ResponseStatusCode}");
            return response.ResponseStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Exception e)
        {
            Log.Error($"[MinIO] PutObjectAsync Exception: {e}");
            return false;
        }
    }
}

/// <summary>
/// Put an object from a local stream into bucket
/// </summary>
internal class PutObjectStream
{
    public static async Task<bool> Run(IMinioClient minio, string bucketName, string objectName, string fileName,
        IProgress<ProgressReport> progress = null,
        IServerSideEncryption sse = null)
    {
        try
        {

            byte[] bs = await Task.Run(() => File.ReadAllBytes(fileName)).ConfigureAwait(false);
            using var filestream = new MemoryStream(bs);
            var fileInfo = new FileInfo(fileName);
            var metaData = new Dictionary<string, string>
                (StringComparer.Ordinal) { { "Test-Metadata", "Test  Test" } };
            var args = new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(filestream)
                .WithObjectSize(filestream.Length)
                .WithContentType("application/octet-stream")
                .WithHeaders(metaData)
                .WithProgress(progress)
                .WithServerSideEncryption(sse);

            Log.Debug($"[MinIO] PutObjectAsyncStream Start");
            var response = await minio.PutObjectAsync(args).ConfigureAwait(false);
            Log.Debug($"[MinIO] PutObjectAsyncStream Finish with status code:{response.ResponseStatusCode}");
            return response.ResponseStatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (Exception e)
        {
            Log.Error($"[MinIO]  Exception: {e}");
            return false;
        }
    }
}

/// <summary>
/// Download object from bucket into local file
/// </summary>
internal static class GetObject
{
    public static async Task<string> Run(IMinioClient minio, string bucketName, string objectName, string fileName,
        IServerSideEncryption sse = null)
    {
        try
        {
            File.Delete(fileName);
            var args = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithFile(fileName)
                .WithServerSideEncryption(sse);
            var response = await minio.GetObjectAsync(args).ConfigureAwait(false);

            Log.Debug($"[MinIO] Downloaded file {fileName} from bucket {bucketName}");
            return response.ContentType;
        }
        catch (Exception e)
        {
            Log.Error($"[MinIO] GetObjectAsync Exception: {e}");
            return string.Empty;
        }
    }
}