using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;
using static UnityFramework.Runtime.ServiceRequestData;

/// <summary>
/// 资源下载工具
/// </summary>
public class ResourcesDownloader : MonoBehaviour
{
    #region UI Event
    /// <summary>
    /// 总下载数变化时调用
    /// </summary>
    public UnityEvent<int> OnTotalDownloadCountChanged = new UnityEvent<int>();
    /// <summary>
    /// 总下载进度变化时调用
    /// </summary>
    public UnityEvent<float> OnDownloadProgressChanged = new UnityEvent<float>();
    /// <summary>
    /// 下载完成重置状态时调用
    /// </summary>
    public UnityEvent OnReset = new UnityEvent();
    #endregion

    /// <summary>
    /// ab包的下载限制
    /// </summary>
    public const int abPackDownloadMax = 3;
    /// <summary>
    /// 图片的下载限制
    /// </summary>
    public const int imageDownloadMax = 6;

    /// <summary>
    /// 是否正在下载
    /// </summary>
    private bool allDownload = false;

    /// <summary>
    /// 正在下载资源计数
    /// </summary>
    private static object countLock = new object();
    private static int _downloadingCount;
    public static int DownloadingCount
    {
        get { return _downloadingCount; }
        set
        {
            lock (countLock)
            {
                _downloadingCount = value;
            }
        }
    }

    /// <summary>
    /// 总下载数
    /// </summary>
    private object progressLock = new object();
    private int _totalCount;
    public int TotalCount
    {
        get { return _totalCount; }
        set
        {
            lock (progressLock)
            {
                _totalCount = value;
                if (_totalCount > 0)
                {
                    OnTotalDownloadCountChanged?.Invoke(_totalCount);
                }
            }
        }
    }

    /// <summary>
    /// 总下载进度
    /// </summary>
    private float _totalProgress;
    public float TotalProgress
    {
        get { return _totalProgress; }
        set
        {
            _totalProgress = value;
            OnDownloadProgressChanged?.Invoke(value);
        }
    }


    /// <summary>
    /// 各课程下载进度
    /// </summary>
    private Dictionary<int, float> courseProgress = new Dictionary<int, float>();

    /// <summary>
    /// 待更新课程id列表
    /// </summary>
    private List<int> coursesNeedUpdate = new List<int>();
    public List<int> CourseNeedUpdate => coursesNeedUpdate;

    /// <summary>
    /// 当前AB包下载数
    /// </summary>
    private int currentABPackDownload = 0;
    /// <summary>
    /// 待下载AB包任务序列
    /// </summary>
    private List<DownloadABPackData> abPackList = new List<DownloadABPackData>();

    /// <summary>
    /// 当前图片下载数
    /// </summary>
    private int currentImageDownload = 0;
    /// <summary>
    /// 待下载封面图片任务序列
    /// </summary>
    private List<DownloadImageData> imageList = new List<DownloadImageData>();

    /// <summary>
    /// 下载失败的ab包
    /// </summary>
    private List<string> abDownloadFailed = new List<string>();

    /// <summary>
    /// 一键下载
    /// </summary>
    /// <param name="onDownload">开始下载回调</param>
    /// <param name="onDownloading">正在下载回调</param>
    /// <param name="noNeedUpdate">无可下载资源回调</param>
    public void DownloadAllResources(UnityAction onDownload, UnityAction onDownloading = null, UnityAction noNeedUpdate = null)
    {
        if (GlobalInfo.isOffLine)
        {
            ToolManager.PleaseOnline();
            return;
        }

        if (allDownload)
        {
            onDownloading?.Invoke();
            return;
        }

        if (coursesNeedUpdate.Count == 0)
        {
            noNeedUpdate?.Invoke();
            return;
        }

        allDownload = true;
        TotalCount += coursesNeedUpdate.Count;

        lock (coursesNeedUpdate)
        {
            onDownload?.Invoke();
        }
    }

    /// <summary>
    /// 更新课程状态
    /// </summary>

    public void UpdateResourcesState(Transform content, UnityAction<Transform, int, int, List<CourseABPackage>> onItemUpdate, UnityAction<Transform> onItemActive)
    {
        coursesNeedUpdate.Clear();
        foreach (Transform child in content)
        {
            if (int.TryParse(child.name, out int courseId))
            {
                if (GlobalInfo.courseDicExists.TryGetValue(courseId, out Course course))
                {
                    GlobalInfo.courseABDic.TryGetValue(courseId, out List<CourseABPackage> data);

                    ResManager.Instance.CheckUpdate(data, result =>
                    {
                        if (child == null || Object.ReferenceEquals(child, null))
                            return;

                        onItemUpdate?.Invoke(child, result, courseId, data);

                        if (result != 0)
                            coursesNeedUpdate.Add(courseId);
                    });
                    
                    if (child == null || Object.ReferenceEquals(child, null))
                        return;
                    onItemActive?.Invoke(child);
                }
            }
        }
    }

    /// <summary>
    /// 更新考核房间资源状态
    /// </summary>
    /// <param name="content"></param>
    /// <param name="onItemUpdate"></param>
    /// <param name="onItemActive"></param>

    public void UpdateExamResourcesState(Dictionary<Transform, RoomInfoModel> content, UnityAction<Transform, int, RoomInfoModel, List<CourseABPackage>> onItemUpdate, UnityAction<Transform> onItemActive)
    {
        foreach(var item in content)
        {
            if (GlobalInfo.examABDic.TryGetValue(item.Value.CourseId, out List<CourseABPackage> data))
            {
                ResManager.Instance.CheckUpdate(data, result =>
                {
                    if (item.Key == null || Object.ReferenceEquals(item.Key, null))
                        return;

                    onItemUpdate?.Invoke(item.Key, result, item.Value, data);

                    if (result != 0)
                        coursesNeedUpdate.Add(item.Value.CourseId);
                });

                if (item.Key == null || Object.ReferenceEquals(item.Key, null))
                    return;
                onItemActive?.Invoke(item.Key);
            }
        }
    }

    /// <summary>
    /// 更新协同房间课程资源状态
    /// </summary>
    public void UpdateCourseResourcesState(Dictionary<Transform, RoomInfoModel> content, UnityAction<Transform, int, RoomInfoModel, List<CourseABPackage>> onItemUpdate, UnityAction<Transform> onItemActive)
    {
        foreach (var item in content)
        {
            if (GlobalInfo.courseABDic.TryGetValue(item.Value.CourseId, out List<CourseABPackage> data))
            {
                ResManager.Instance.CheckUpdate(data, result =>
                {
                    if (item.Key == null || Object.ReferenceEquals(item.Key, null))
                        return;

                    onItemUpdate?.Invoke(item.Key, result, item.Value, data);

                    if (result != 0)
                        coursesNeedUpdate.Add(item.Value.CourseId);
                });

                if (item.Key == null || Object.ReferenceEquals(item.Key, null))
                    return;
                onItemActive?.Invoke(item.Key);
            }
        }
    }

    /// <summary>
    /// 更新下载数量
    /// </summary>
    /// <param name="courseId"></param>

    public void UpdateDownloadingCount(int courseId)
    {
        if (!allDownload)
        {
            lock (coursesNeedUpdate)
                coursesNeedUpdate.Remove(courseId);
            TotalCount++;
        }

        DownloadingCount++;
    }

    /// <summary>
    /// 添加封面图加载任务
    /// </summary>
    public void AddImageTask(string courseId, string imgUrl, Component ResourcesBtn)
    {
        lock (imageList)
        {
            imageList.Add(new DownloadImageData()
            {
                courseId = courseId,
                downLoadPath = imgUrl,
                isShowLoading = false,
                call = texture =>
                {
                    if (ResourcesBtn == null || Object.ReferenceEquals(ResourcesBtn, null))
                        return;

                    if (texture)
                    {
                        ResourcesBtn.GetComponentInChildren<RawImage>().texture = texture;
                        ResourcesBtn.GetComponentInChildren<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;
                    }
                }
            });
        }
    }

    /// <summary>
    /// 添加AB包下载任务
    /// </summary>
    /// <param name="courseId"></param>
    /// <param name="abDatas"></param>
    /// <param name="downloadText"></param>
    /// <param name="downloadTrans"></param>
    /// <param name="updateBtn"></param>
    /// <param name="onDownloadFinished"></param>
    public void AddABPackTask(int courseId, List<CourseABPackage> abDatas, Text downloadText, Transform downloadTrans, Button updateBtn, UnityAction<int> onDownloadFinished = null)
    {
        lock (abPackList)
        {
            abPackList.Add(new DownloadABPackData()
            {
                courseId = courseId,
                datas = abDatas,
                text = downloadText,
                callBack = (isSuccess, valid) =>
                {
                    DownloadingCount--;

                    onDownloadFinished?.Invoke(Mathf.Max(TotalCount - DownloadingCount, 0));

                    if (DownloadingCount == 0)
                    {                     
                        ResetDownloading();
                    }

                    downloadTrans.gameObject.SetActive(!isSuccess || !valid);
                    updateBtn.gameObject.SetActive(!isSuccess || !valid);

                    if (isSuccess && valid)//下载成功
                    {
                        SoundManager.Instance.PlayEffect("FinishNotice");
                    }
                    else
                    {
                        downloadText.text = valid ? "下载失败" : "下载失败\n未上传当前平台资源";

                        //测试 后台复制资源用
                        if (!allDownload)
                        {
                            string failedList = string.Empty;
                            foreach (string ab in abDownloadFailed)
                                failedList += $"{ab}\n";

                            var popupDic = new Dictionary<string, PopupButtonData>();
                            popupDic.Add("复制到剪贴板", new PopupButtonData(() => GUIUtility.systemCopyBuffer = failedList /*ClipboardControl.SetText(failedList)*/, true));
                            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", $"下列AB包加载失败:\n {failedList}", popupDic, () => ClipboardControl.SetText(failedList), false));
                        }
                    }
                }
            });
        }
    }


    /// <summary>
    /// 下载完成 重置状态
    /// </summary>
    public void ResetDownloading()
    {
        TotalCount = 0;
        TotalProgress = 0;
        courseProgress.Clear();
        allDownload = false;
        OnReset?.Invoke();
    }

    private void Update()
    {
        //if (imageList.Count > 0 && ((abPackList.Count == 0 && currentImageDownload + currentABPackDownload < imageDownloadMax + abPackDownloadMax) || currentImageDownload < imageDownloadMax))
        if (imageList.Count > 0 && currentImageDownload < imageDownloadMax)
        {
            currentImageDownload++;
            lock (imageList)
            {
                var tempData = imageList[0];
                tempData.Add(() =>
                {
                    currentImageDownload--;
                });
                ResManager.Instance.LoadCoverImage(tempData.courseId, ResManager.Instance.OSSDownLoadPath + tempData.downLoadPath, tempData.isShowLoading, tempData.call);
                imageList.RemoveAt(0);
            }
        }

        //if (abPackList.Count > 0 && ((imageList.Count == 0 && currentImageDownload + currentABPackDownload < imageDownloadMax + abPackDownloadMax) || currentABPackDownload < abPackDownloadMax))
        if (abPackList.Count > 0 && currentABPackDownload < abPackDownloadMax)
        {
            currentABPackDownload++;
            lock (abPackList)
            {
                var tempData = abPackList[0];
                tempData.Add(() =>
                {
                    currentABPackDownload--;
                });
                StartCoroutine(DownloadResource(tempData));
                abPackList.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// 下载课程AB包
    /// </summary>
    /// <param name="downloadInfo">下载任务</param>
    /// <returns></returns>
    private IEnumerator DownloadResource(DownloadABPackData downloadInfo)
    {
        bool isSuccess = true;
        int successCount = 0;
        abDownloadFailed.Clear();

        List<float> index = new List<float>();

        int requestNum = downloadInfo.datas.Count;

        for (int i = 0; i < downloadInfo.datas.Count; i++)
        {
            int currentIndex = 0;

            lock (index)
            {
                currentIndex = index.Count;
                index.Add(0);
            }

            requestNum--;

            if (downloadInfo.datas[currentIndex] == null)
            {
                index[currentIndex] = 1;
                continue;
            }

            string path = downloadInfo.datas[currentIndex].filePath;
            if (path == string.Empty)
            {
                index[currentIndex] = 1;
                Log.Error($"ID为{downloadInfo.datas[i].encyclopediaId}的资源AB包地址获取失败!");
                continue;
            }

            path = string.Format("{0}{1}", ResManager.Instance.OSSDownLoadPath, downloadInfo.datas[currentIndex].filePath);

            int k = i;
            //ResManager.Instance.UnparsedData(downloadInfo.datas[i].encyclopediaId.ToString(), path, DtataType.abs, out string savePath, out string name, out string version);
            ResManager.Instance.UnparsedData(downloadInfo.datas[i].encyclopediaId.ToString(), path, DtataType.abs, out string savePath, out string name, out string version);
            ResManager.Instance.DownLoadFile(name, version, path, savePath, DtataType.abs, false, value =>
            {
                index[currentIndex] = value;

                if (value >= 1)//成功
                {
                    index[currentIndex] = 1;
                    successCount++;
                }
                else if (value <= -1)//失败
                {
                    index[currentIndex] = 1;
                    isSuccess = false;
                    abDownloadFailed.Add(downloadInfo.datas[currentIndex].filePath);

                    Log.Error($"ID为{downloadInfo.datas[k].encyclopediaId}名字为{downloadInfo.datas[k].fileName}的百科ab包下载失败！");
                }
            });
        }

        //等待下载并同步
        int max = index.Count;
        float current = downloadInfo.datas.Count;

        while (isSuccess)
        {
            current = 0;
            foreach (var value in index)
                current += value;

            UpdateDownloadProgress(downloadInfo.courseId, current / max);

            if (current >= max)
                break;

            if (downloadInfo.text)
            {
                downloadInfo.text.text = $"下载中{((current / max) * 100).ToString("f0")}%";
            }

            yield return new WaitForSeconds(0.2f);
        }

        downloadInfo.callBack.Invoke(isSuccess, successCount > 0);
    }

    /// <summary>
    /// 更新下载进度
    /// </summary>
    /// <param name="courseId"></param>
    /// <param name="current"></param>
    private void UpdateDownloadProgress(int courseId, float current)
    {
        lock (progressLock)
        {
            if (courseProgress.ContainsKey(courseId))
                courseProgress[courseId] = current;
            else
            {
                //TotalCount++;
                courseProgress.Add(courseId, current);
            }
            TotalProgress = courseProgress.Values.Sum() / TotalCount;
        }
    }

    internal void UpdateExamResourcesState(Dictionary<Transform, RoomInfoModel> currentItems, System.Action<int, Transform, int, int, List<CourseABPackage>> onItemStateUpdate, System.Action<Transform> onItemActive)
    {
        throw new System.NotImplementedException();
    }

    #region Struct
    /// <summary>
    /// 下载AB包数据
    /// </summary>
    private struct DownloadABPackData
    {
        public int courseId;
        public List<CourseABPackage> datas;
        public Text text;
        public UnityAction<bool, bool> callBack;
        public void Add(UnityAction newAction)
        {
            callBack += (result, check) =>
            {
                newAction.Invoke();
            };
        }
    }
    
    /// <summary>
    /// 下载封面图数据
    /// </summary>
    private struct DownloadImageData
    {
        public string courseId;
        public string downLoadPath;
        public bool isShowLoading;
        public UnityAction<Texture2D> call;
        public void Add(UnityAction newAction)
        {
            call += result =>
            {
                newAction.Invoke();
            };
        }
    }
    #endregion
}