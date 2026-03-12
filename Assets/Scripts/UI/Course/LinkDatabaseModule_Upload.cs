using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;

/// <summary>
/// 资料库模块_课件上传
/// </summary>
public partial class LinkDatabaseModule : UIModuleBase
{
    /// <summary>
    /// 限制文件大小
    /// </summary>
    private const long MAX_UPLOAD_SIZE = 200 * 1024 * 1024;

    private const string ellipsisTextMask = "...";

    public Sprite[] UploadFileSprites;

    private RectTransform UploadingPanel;
    private RectTransform UploadingContent;
    private Text Total;
    private Button OpenBtn;
    private Button AddFileBtn;
    private Button CollapseBtn;

    private FormTool.FileType fileType;

    private System.Object countLock = new System.Object();
    /// <summary>
    /// 正在上传文件数
    /// </summary>
    private int uploadingCount;
    public int UploadingCount
    {
        get { return uploadingCount; }
        set
        {
            uploadingCount = value;
            Total.text = $"有{uploadingCount}个文件正在上传";
        }
    }

    /// <summary>
    /// 初始化上传相关UI
    /// </summary>
    private void InitUploadUIEvents()
    {
        AddMsg((ushort)CoursePanelEvent.Option);

        UploadingPanel = transform.GetComponentByChildName<RectTransform>("Uploading");
        UploadingContent = UploadingPanel.GetComponentByChildName<RectTransform>("Content");
        Total = transform.GetComponentByChildName<Text>("Total");
        OpenBtn = transform.GetComponentByChildName<Button>("Open");
        AddFileBtn = transform.GetComponentByChildName<Button>("AddFile");
        CollapseBtn = transform.GetComponentByChildName<Button>("Collapse");

        AddFileBtn.onClick.AddListener(ShowFileSelector);
        OpenBtn.onClick.AddListener(ShowUploadingPanel);
        CollapseBtn.onClick.AddListener(HideUploadingPanel);
    }

    /// <summary>
    /// 打开文件选择对话框
    /// </summary>
    private void ShowFileSelector()
    {
        lock (countLock)
        {
            if (uploadingCount >= 5)
            {
                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("一次只能上传五个文件"));
                return;
            }
        }

        FormTool.OpenFileDialog("选择文件", Application.persistentDataPath, (string[] paths) =>
        {
            lock (countLock)
            {
                if (paths.Length + uploadingCount > 5)
                {
                    UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("一次只能上传五个文件"));
                    return;
                }
            }

            List<string> pathsToUpload = paths.Select(p => p).Where(p => Path.GetFileNameWithoutExtension(p).Length < 20 && FileTool.GetFileLength(p) < MAX_UPLOAD_SIZE).ToList();
            if(pathsToUpload.Count < paths.Length)
            {
                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("文件名不能超过20字符；文件大小不能超过200M"));
            }
            if (pathsToUpload.Count == 0)
                return;

            RequestManager.Instance.GetSTS((stsInfo) =>
            {
                UploadingContent.AppendItemsView(pathsToUpload, async (item, path) =>
                {
                    lock (countLock)
                    {
                        UploadingCount++;
                    }
                    string fileName = Path.GetFileName(path);
                    string savePrefix = GetOSSObjectPrefix(item.GetComponentByChildName<Image>("Icon"), FileExtension.Convert(path));
                    string savePath = $"{savePrefix}{(long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds}{Path.GetExtension(path)}";

                    item.GetComponentByChildName<Text>("Name").EllipsisText(fileName, ellipsisTextMask);

                    #region 取消\暂停上传任务
                    item.GetComponentByChildName<Button>("Cancel").onClick.AddListener(() =>
                    {
                        StorageManager.Instance.CancelTask(path);
                    });

                    Toggle toggle = item.GetComponentByChildName<Toggle>("Pause");
                    Image pauseImage = toggle.GetComponentByChildName<Image>("PauseImg"); 
                    Image resumeImage = toggle.GetComponentByChildName<Image>("ResumeImg");
                    toggle.onValueChanged.AddListener((isOn) =>
                    {
                        pauseImage.gameObject.SetActive(!isOn);
                        resumeImage.gameObject.SetActive(isOn);
                        if (isOn)
                        {
                            StorageManager.Instance.PauseTask(path);
                        }
                        else
                        {
                            StorageManager.Instance.ResumeTask(path);
                        }
                    });
                    #endregion

                    Text progressTxt = item.GetComponentByChildName<Text>("Progress");
                    Slider progressBar = item.GetComponentByChildName<Slider>("ProgressBar");

                    bool result = await StorageManager.Instance.PutObjectAsync(stsInfo, path, savePath, (progress) =>
                    {
                        ThreadUtils.Instance.InvokeOnMainThread(() =>
                        {
                            progressTxt.text = $"{progress:F0}%";
                            progressBar.value = progress / 100f;
                        });
                    });
                    if (result)
                    {
                        OnFileUploadSuccess(item, fileName, savePath);
                    }
                    else
                    {
                        OnFileUploadFailed(item);
                    }       
                });
                //自动弹出上传进度面板
                ShowUploadingPanel();
            }, errorMessage =>
            {
                Log.Error("STS获取失败:" + errorMessage);
            });

        }, fileType);
    }

    private void OnFileUploadSuccess(Transform item, string fileName, string savePath)
    {
        lock (countLock)
        {
            UploadingCount--;
        }
        bool lastFile = item.parent.childCount <= 2;
        Destroy(item.gameObject);
        //todo 数据库有限制:fileName 50，filePath 200
        RequestManager.Instance.AddCoursewareResource(fileName, savePath, () =>
        {
            Debug.Log($"新增资源信息成功");
            //todo 上传成功，更新列表
            if (lastFile)
            {
                RefreshResources();
            }
        },
           (code, msg) =>
           {
               Log.Error($"新增课件资源信息失败 {code} {msg}");
           });
    }

    private void OnFileUploadFailed(Transform item)
    {
        lock (countLock)
        {
            UploadingCount--;
        }
        Destroy(item.gameObject);
        UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("文件上传失败"));
    }

    private void ShowUploadingPanel()
    {
        UploadingPanel.transform.parent.gameObject.SetActive(true);
        UploadingPanel.DOAnchorPosX(0, 0.5f);
    }

    private void HideUploadingPanel()
    {
        UploadingPanel.DOAnchorPosX(UploadingPanel.sizeDelta.x, 0.5f).OnComplete(() => UploadingPanel.transform.parent.gameObject.SetActive(false));
    }

    private string GetOSSObjectPrefix(Image icon, string fileExtension)
    {
        string objectPrefix = string.Empty;
        switch (fileExtension)
        {
            case FileExtension.IMG:
                icon.sprite = UploadFileSprites[6];
                objectPrefix = "image/";
                break;
            case FileExtension.MP4:
                icon.sprite = UploadFileSprites[5];
                objectPrefix = "video/";
                break;
            case FileExtension.MP3:
                icon.sprite = UploadFileSprites[4];
                objectPrefix = "audio/";
                break;
            case FileExtension.PDF:
                icon.sprite = UploadFileSprites[3];
                objectPrefix = "wps/";
                break;
            case FileExtension.XLS:
                icon.sprite = UploadFileSprites[2];
                objectPrefix = "wps/";
                break;
            case FileExtension.DOC:
                icon.sprite = UploadFileSprites[1];
                objectPrefix = "wps/";
                break;
            case FileExtension.PPT:
                icon.sprite = UploadFileSprites[0];
                objectPrefix = "wps/";
                break;
        }
        return objectPrefix;
    }
}