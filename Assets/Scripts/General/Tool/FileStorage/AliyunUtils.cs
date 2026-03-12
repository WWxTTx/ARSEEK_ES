using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Aliyun.OSS;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;

/// <summary>
/// 阿里云工具类
/// </summary>
public class AliyunUtils
{

    /// <summary>
    /// 下载阿里云资源
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="savePath"></param>
    /// <param name="progressCallback"></param>
    /// <param name="successCallback"></param>
    /// <param name="failCallback"></param>
    public static void DownloadOSSFile(string filePath, string savePath, UnityAction<float> progressCallback, UnityAction successCallback, UnityAction failCallback)
    {
        RequestManager.Instance.GetOSS((config) =>
        {
            OssClient ossClient = new OssClient(config.endpoint, config.accessKeyId, config.accessKeySecret, config.securityToken);

            try
            {
                Log.Debug($"开始下载文件：{filePath}");
                var getObjectRequest = new GetObjectRequest(config.bucket, filePath);
                getObjectRequest.StreamTransferProgress += (sender, args) =>
                {
                    progressCallback?.Invoke((float)args.TransferredBytes / args.TotalBytes);
                };

                // 下载文件到流
                var obj = ossClient.GetObject(getObjectRequest);
                using (var requestStream = obj.Content)
                {
                    byte[] buf = new byte[1024];
                    var fs = File.Open(savePath, FileMode.OpenOrCreate);
                    var len = 0;

                    while ((len = requestStream.Read(buf, 0, 1024)) != 0)
                    {
                        fs.Write(buf, 0, len);
                    }
                    fs.Close();
                }
                successCallback?.Invoke();
            }
            catch (Exception ex)
            {
                failCallback?.Invoke();
            }
        }, (message) =>
        {
            failCallback?.Invoke();
            Log.Error($"获取阿里云认证失败：{message}");
        });
    }

    private static int PARTSIZE = 50 * 1024 * 1024;

    public struct PartData
    {
        public string uploadId;
        public OssClient ossClient;
    }

    private static Dictionary<string, PartData> keyClient = new Dictionary<string, PartData>();

    //断点续传
    private static string checkpointDir = Application.persistentDataPath + "/Cache/Checkpoint";

    /// <summary>
    /// 简单上传文件
    /// </summary>
    /// <param name="config"></param>
    /// <param name="filePath"></param>
    /// <param name="savePath"></param>
    /// <returns></returns>
    //public static bool PutObject(OSSConfig config, string filePath, string savePath, UnityAction<float> progress)
    public static bool PutObject(AliyunOSSStsInfo config, string filePath, string savePath, UnityAction<float> progress)
    {
        Log.Debug("上传阿里云: filePath = " + filePath + "; savePath = " + savePath);
        OssClient ossClient = new OssClient(config.endpoint, config.accessKeyId, config.accessKeySecret, config.securityToken);
        try
        {
            using (var fs = File.Open(filePath, FileMode.Open))
            {
                //简单上传
                PutObjectRequest putObjectRequest = new PutObjectRequest(config.bucket, savePath, fs);
                putObjectRequest.StreamTransferProgress += (sender, args) =>
                {
                    progress?.Invoke(args.TransferredBytes / args.TotalBytes);
                };
                ossClient.PutObject(putObjectRequest);

                ////断点续传
                //UploadObjectRequest request = new UploadObjectRequest(config.bucket, savePath, filePath)
                //{
                //    // 指定上传的分片大小。
                //    PartSize = 8 * 1024 * 1024,
                //    // 指定并发线程数。
                //    ParallelThreadCount = 3,
                //    // checkpointDir保存断点续传的中间状态，用于失败后继续上传。
                //    // 如果checkpointDir为null，断点续传功能不会生效，每次失败后都会重新上传。
                //    CheckpointDir = checkpointDir,
                //};
                //request.StreamTransferProgress += (sender, args) =>
                //{
                //    progress?.Invoke(args.TransferredBytes * 100 / args.TotalBytes, args.TotalBytes / 1024);
                //};
                //ossClient.ResumableUploadObject(request);         
            }

            Log.Debug("上传阿里云成功: " + filePath);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(string.Format("上传阿里云失败: {0}", ex.Message));
            return false;
        }
    }

    /// <summary>
    /// 分片上传文件
    /// </summary>
    /// <param name="config"></param>
    /// <param name="filePath"></param>
    /// <param name="savePath"></param>
    /// <returns></returns>
    public static bool PutPartObject(OSSConfig config, string filePath, string savePath, UnityAction<float> progress)
    {
        Log.Debug("上传阿里云: filePath = " + filePath + "; savePath = " + savePath);
        OssClient ossClient = new OssClient(config.endpoint, config.accessKeyId, config.accessKeySecret, config.securityToken);
        //分片上传初始化
        string uploadId = string.Empty;
        try
        {
            InitiateMultipartUploadRequest multipartUploadRequest = new InitiateMultipartUploadRequest(config.bucket, savePath);
            InitiateMultipartUploadResult result = ossClient.InitiateMultipartUpload(multipartUploadRequest);
            uploadId = result.UploadId;
        }
        catch (Exception ex)
        {
            Log.Error($"初始化分片上传失败 {ex.Message}");
            return false;
        }

        //计算分片总数
        var fi = new FileInfo(filePath);
        var fileSize = fi.Length;
        var partCount = fileSize / PARTSIZE;
        if (fileSize % PARTSIZE != 0)
        {
            partCount++;
        }

        // PartETag的列表，OSS收到用户提交的分片列表后，会逐一验证每个分片数据的有效性。当所有的数据分片通过验证后，OSS会将这些分片组合成一个完整的文件
        List<PartETag> partETags = new List<PartETag>();
        using (var fs = File.Open(filePath, FileMode.Open))
        {         
            // 开始分片上传
            try
            {
                for (int i = 0; i < partCount; i++)
                {
                    var skipBytes = (long)PARTSIZE * i;
                    // 定位到本次上传的起始位置
                    fs.Seek(skipBytes, 0);
                    var size = (PARTSIZE < fileSize - skipBytes) ? PARTSIZE : (fileSize - skipBytes);
                    UploadPartRequest uploadPartRequest = new UploadPartRequest(config.bucket, savePath, uploadId)
                    {
                        PartSize = size,
                        PartNumber = i + 1,
                        InputStream = fs,
                        //TrafficLimit = 30 * 1024 * 8
                    };
                    uploadPartRequest.StreamTransferProgress += (sender, args) =>
                    {
                        progress?.Invoke(args.TransferredBytes / args.TotalBytes / partCount);
                    };
                    // 调用UploadPart接口执行上传功能，返回结果中包含了这个数据片的ETag值。
                    var result = ossClient.UploadPart(uploadPartRequest);
                    partETags.Add(result.PartETag);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"分片上传失败 { ex.Message}");
                return false;
            }

            // 完成分片上传。
            try
            {
                CompleteMultipartUploadRequest completeMultipartUploadRequest = new CompleteMultipartUploadRequest(config.bucket, savePath, uploadId);
                foreach (var partETag in partETags)
                {
                    completeMultipartUploadRequest.PartETags.Add(partETag);
                }
                var result = ossClient.CompleteMultipartUpload(completeMultipartUploadRequest);
                Log.Debug($"完成分片上传 {partETags.Count}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"完成分片上传失败 { ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// 异步分片上传文件
    /// </summary>
    /// <param name="config"></param>
    /// <param name="filePath"></param>
    /// <param name="savePath"></param>
    /// <returns></returns>
    public static void AsyncPutPartObject(OSSConfig config, string filePath, string savePath, UnityAction<float> progress, UnityAction<bool> finish)
    {
        Log.Debug("上传阿里云: filePath = " + filePath + "; savePath = " + savePath);
        OssClient ossClient = new OssClient(config.endpoint, config.accessKeyId, config.accessKeySecret, config.securityToken);
     
        //分片上传初始化
        string uploadId = string.Empty;
        try
        {
            InitiateMultipartUploadRequest multipartUploadRequest = new InitiateMultipartUploadRequest(config.bucket, savePath);
            InitiateMultipartUploadResult result = ossClient.InitiateMultipartUpload(multipartUploadRequest);
            uploadId = result.UploadId;
        }
        catch (Exception ex)
        {
            Log.Error($"初始化分片上传失败 {ex.Message}");
            finish?.Invoke(false);
        }

        //计算分片总数
        var fi = new FileInfo(filePath);
        var fileSize = fi.Length;
        var partCount = fileSize / PARTSIZE;
        if (fileSize % PARTSIZE != 0)
        {
            partCount++;
        }

        var ctx = new UploadPartContext()
        {
            BucketName = config.bucket,
            ObjectName = savePath,
            UploadId = uploadId,
            TotalParts = partCount,
            CompletedParts = 0,
            SyncLock = new object(),
            PartETags = new List<PartETag>(),
            WaitEvent = new ManualResetEvent(false)
        };

        for (var i = 0; i < partCount; i++)
        {
            var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var skipBytes = (long)PARTSIZE * i;
            fs.Seek(skipBytes, 0);
            var size = (PARTSIZE < fileSize - skipBytes) ? PARTSIZE : (fileSize - skipBytes);
            var uploadPartRequest = new UploadPartRequest(config.bucket, savePath, uploadId)
            {
                InputStream = fs,
                PartSize = size,
                PartNumber = i + 1,
            };
            //uploadPartRequest.StreamTransferProgress += (sender, args) =>
            //{
            //    progress?.Invoke(args.TransferredBytes / args.TotalBytes / partCount);
            //};
            ossClient.BeginUploadPart(uploadPartRequest, (ar) =>
            {
                var result = ossClient.EndUploadPart(ar);
                var wrappedContext = (UploadPartContextWrapper)ar.AsyncState;
                wrappedContext.PartStream.Close();

                var ctx = wrappedContext.Context;
                lock (ctx.SyncLock)
                {
                    var partETags = ctx.PartETags;
                    partETags.Add(new PartETag(wrappedContext.PartNumber, result.ETag));
                    ctx.CompletedParts++;

                    //Log.Error(string.Format("finish {0}/{1}", ctx.CompletedParts, ctx.TotalParts));
                    if (ctx.CompletedParts == ctx.TotalParts)
                    {
                        partETags.Sort((e1, e2) => (e1.PartNumber - e2.PartNumber));
                        var completeMultipartUploadRequest = new CompleteMultipartUploadRequest(ctx.BucketName, ctx.ObjectName, ctx.UploadId);
                        foreach (var partETag in partETags)
                        {
                            completeMultipartUploadRequest.PartETags.Add(partETag);
                        }

                        var completeMultipartUploadResult = ossClient.CompleteMultipartUpload(completeMultipartUploadRequest);
                        Log.Debug("Async upload multipart result : " + completeMultipartUploadResult.Location);

                        ctx.WaitEvent.Set();
                        finish?.Invoke(true);
                    }
                }
            }, new UploadPartContextWrapper(ctx, fs, i + 1));
        }

        ctx.WaitEvent.WaitOne();
    }

    /// <summary>
    /// 取消分片上传，已经上传的分片数据会被删除
    /// </summary>
    /// <param name="config"></param>
    /// <param name="savePath"></param>
    public static void AbortObject(OSSConfig config, string savePath, UnityAction callback)
    {
        if(keyClient.TryGetValue(savePath, out PartData partData))
        {
            if(partData.ossClient != null)
            {
                partData.ossClient.AbortMultipartUpload(new AbortMultipartUploadRequest(config.bucket, savePath, partData.uploadId));
                callback?.Invoke();
            }
        }
    }

    /// <summary>
    /// 列举对象
    /// </summary>
    /// <param name="config"></param>
    /// <param name="prefix"></param>
    public static void ListObjects(OSSConfig config, string prefix)
    {
        OssClient ossClient = new OssClient(config.endpoint, config.accessKeyId, config.accessKeySecret, config.securityToken);
        try
        {
            ObjectListing objectListing = ossClient.ListObjects(config.bucket, prefix);
            foreach (OssObjectSummary ossObjectSummary in objectListing.ObjectSummaries)
            {
                Debug.LogError(ossObjectSummary.Key);
            }
        }
        catch (System.Exception ex)
        {
        }
    }

    /// <summary>
    /// 删除对象
    /// </summary>
    /// <param name="config"></param>
    /// <param name="objectName"></param>
    public static void DeleteObject(OSSConfig config, string objectName)
    {
        OssClient ossClient = new OssClient(config.endpoint, config.accessKeyId, config.accessKeySecret, config.securityToken);
        try
        {
            var deleteResult = ossClient.DeleteObject(config.bucket, objectName);
        }
        catch (System.Exception ex)
        {

        }
    }

    public class UploadPartContext
    {
        public string BucketName { get; set; }
        public string ObjectName { get; set; }

        public List<PartETag> PartETags { get; set; }

        public string UploadId { get; set; }
        public long TotalParts { get; set; }
        public long CompletedParts { get; set; }
        public object SyncLock { get; set; }
        public ManualResetEvent WaitEvent { get; set; }
    }

    public class UploadPartContextWrapper
    {
        public UploadPartContext Context { get; set; }
        public int PartNumber { get; set; }
        public Stream PartStream { get; set; }

        public UploadPartContextWrapper(UploadPartContext context, Stream partStream, int partNumber)
        {
            Context = context;
            PartStream = partStream;
            PartNumber = partNumber;
        }
    }
}
