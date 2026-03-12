using Aliyun.OSS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace UnityFramework.Runtime
{
    /// <summary>
    /// 下载任务
    /// </summary>
    public class DownloadTask
    {
        /// <summary>
        /// 下载路径
        /// </summary>
        public string downloadPath;
        /// <summary>
        /// 保存路径
        /// </summary>
        public string savePath;
        /// <summary>
        /// 下载回调
        /// </summary>
        public UnityAction<float> call;

        public DownloadTask(string downloadPath, string savePath, UnityAction<float> call)
        {
            this.downloadPath = downloadPath;
            this.savePath = savePath;
            this.call = call;
        }
    }

    /// <summary>
    /// 跳过证书验证
    /// </summary>
    public class CertHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }

    public class WaitDownloadTask
    {
        public string savePath;
        public UnityAction<float> callback;
    }

    /// <summary>
    /// 资源下载
    /// </summary>
    public class DownLoad : Singleton<DownLoad>
    {
        /// <summary>
        /// 最大同时下载数
        /// </summary>
        int maxConcurrentMask = 5;
        /// <summary>
        /// 下载协程字典
        /// </summary>
        Dictionary<string, Coroutine> coroutineDic = new Dictionary<string, Coroutine>();
        /// <summary>
        /// 正在下载链接字典
        /// </summary>
        Dictionary<string, UnityWebRequest> downReqMap = new Dictionary<string, UnityWebRequest>();
        /// <summary>
        /// 等待下载链接列表
        /// </summary>
        List<DownloadTask> downloadTaskWaitList = new List<DownloadTask>();
        /// <summary>
        /// 用于单一资源多次重复下载时避免BUG
        /// </summary>
        Dictionary<string, List<WaitDownloadTask>> waitForDownLoad = new Dictionary<string, List<WaitDownloadTask>>();

        /// <summary>
        /// 下载资源
        /// </summary>
        /// <param name="downLoadPath">下载路径</param>
        /// <param name="savePath">保存路径</param>
        /// <param name="call">回调函数，返回下载进度(-1:下载失败  0-1:下载中  1:下载完成)</param>
        /// <param name="priority">是否优先下载</param>
        public void DownLoadFile(string downLoadPath, string savePath, UnityAction<float> call, bool priority = false)
        {
            Log.Debug("下载路径:" + downLoadPath + "--保存路径:" + savePath);
            if (string.IsNullOrEmpty(downLoadPath) || string.IsNullOrEmpty(savePath))
            {
                Log.Error("下载路径或保存目录为null");
                return;
            }

            if (!downReqMap.ContainsKey(downLoadPath))//判断是否正在下载此资源
            {
                if(downReqMap.Count < maxConcurrentMask)
                {
                    Log.Debug($"开始下载: {downLoadPath}");
                  
                    coroutineDic.Add(downLoadPath, StartCoroutine(DownLoadRes(downLoadPath, savePath, (p) =>
                    {
                        call?.Invoke(p);

                        if (p == 1 || p == -1)
                        {
                            coroutineDic.Remove(downLoadPath);

                            lock (waitForDownLoad)
                            {
                                if (waitForDownLoad.ContainsKey(downLoadPath))
                                {
                                    foreach (var task in waitForDownLoad[downLoadPath])
                                    {
                                        if (p == 1 && !task.savePath.Equals(savePath))
                                        {
                                            FileTool.FileCopy(savePath, task.savePath);
                                        }
                                        task.callback.Invoke(p);
                                    }
                                    waitForDownLoad.Remove(downLoadPath);
                                }
                            }

                            lock (downloadTaskWaitList)
                            {
                                if(downloadTaskWaitList.Count > 0)
                                {
                                    DownloadTask downloadTask = downloadTaskWaitList[0];
                                    downloadTaskWaitList.RemoveAt(0);
                                    DownLoadFile(downloadTask.downloadPath, downloadTask.savePath, downloadTask.call);
                                }
                            }
                        }
                    })));
                }
                else
                {
                    lock (downloadTaskWaitList)
                    {
                        if(priority)
                            downloadTaskWaitList.Insert(0, new DownloadTask(downLoadPath, savePath, call));
                        else
                            downloadTaskWaitList.Add(new DownloadTask(downLoadPath, savePath, call));
                        Log.Warning("加入下载队列:" + downLoadPath);
                    }
                }
            }
            else
            {
                if (!waitForDownLoad.ContainsKey(downLoadPath))
                    waitForDownLoad.Add(downLoadPath, new List<WaitDownloadTask>());
                waitForDownLoad[downLoadPath].Add(new WaitDownloadTask() 
                { 
                    savePath = savePath,
                    callback = call
                });

                Log.Warning("正在下载当前资源");
            }
        }

        /// <summary>
        ///  下载资源携程
        /// </summary>
        /// <param name="downPath">下载地址</param>
        /// <param name="savePath">保存路径（不包含文件名）</param>
        /// <param name="callback">下载进度回调 返回 0-1之间的小数</param>
        /// <returns></returns>
        IEnumerator DownLoadRes(string downPath, string savePath, UnityAction<float> callback)
        {
            bool downloadDone = false;
            //float lastTime;
            //float lastLength;
            //float downloadSpeed;
            //int count = 0;

            WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();

            //创建网络请求
            using (UnityWebRequest unityWeb = UnityWebRequest.Get(downPath))
            {
                //新建下载文件句柄
                DownloadFileHandler downloadFile = new DownloadFileHandler(savePath);
                unityWeb.certificateHandler = new CertHandler();
                unityWeb.downloadHandler = downloadFile;
                unityWeb.SetRequestHeader("Range", "bytes=" + downloadFile.nowLength + "-");//设置开始下载文件从什么位置开始
                unityWeb.SendWebRequest();
                downReqMap.Add(downPath, unityWeb);

                while (true)
                {
                    //yield return new WaitForEndOfFrame();
                    yield return waitForEndOfFrame;

                    if (callback != null)
                    {
                        if (downloadFile.DownloadProgress < 1)
                            callback.Invoke(downloadFile.DownloadProgress);
                    }

                    if (unityWeb.result == UnityWebRequest.Result.ProtocolError || unityWeb.result == UnityWebRequest.Result.ConnectionError)
                    {
                        //错误处理代码 
                        Log.Error("<color=red>请求出错: " + unityWeb.error + "</color>" + " downPath:" + downPath);
                        downloadFile.ClearTemp();
                        break;
                    }
                    else if (downloadFile.isDone)
                    {
                        downloadDone = true;
                        break;
                    }
                }
                downReqMap.Remove(downPath);
            }

            if (downloadDone)//下载成功
                callback.Invoke(1f);
            else            //下载失败
                callback.Invoke(-1f);
        }

        protected override void InstanceDestroy()
        {
            StopAllDownLoad();
        }

        /// <summary>
        /// 停止所有下载
        /// </summary>
        public void StopAllDownLoad()
        {
            foreach (Coroutine coroutine in coroutineDic.Values)
            {
                if(coroutine != null)
                    StopCoroutine(coroutine);
            }
            coroutineDic.Clear();

            downloadTaskWaitList.Clear();

            foreach (var item in downReqMap.Values)
            {
                try
                {
                    item.Abort();//中止下载  
                    item.Dispose();  //释放
                }
                catch(Exception e)
                {
                    Debug.LogError($"{e.Message}");
                }
            }
            downReqMap.Clear();
        }

        /// <summary>
        /// 停止下载
        /// </summary>
        /// <param name="downloadPathList"></param>
        public void StopDownLoad(List<string> downloadPathList)
        {
            for (int i = 0; i < downloadPathList.Count; i++)
                StopDownLoad(downloadPathList[i]);
        }

        /// <summary>
        /// 停止下载
        /// </summary>
        /// <param name="downloadPath"></param>
        public void StopDownLoad(string downloadPath)
        {
            if(coroutineDic.TryGetValue(downloadPath, out Coroutine coroutine))
            {
                StopCoroutine(coroutine);
                coroutineDic.Remove(downloadPath);
            }

            lock (downloadTaskWaitList)
            {
                int index = downloadTaskWaitList.FindIndex(task => task.downloadPath.Equals(downloadPath));
                if (index > -1)
                    downloadTaskWaitList.RemoveAt(index);
            }

            if (downReqMap.TryGetValue(downloadPath, out UnityWebRequest request))
            {
                try
                {
                    request.Abort();//中止下载  
                    request.Dispose();  //释放
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"{e.Message}");
                }
                finally
                {
                    downReqMap.Remove(downloadPath);
                }
            }
        }
    }
}