using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;
using UnityFramework.Runtime;

/// <summary>
/// 检查软件版本
/// </summary>
public class CheckVersionModule : UIModuleBase
{
    public CanvasGroup canvasGroup;

    private int fontSize;

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);
        canvasGroup = GetComponent<CanvasGroup>();
#if UNITY_ANDROID || UNITY_IOS
        fontSize = 42;
#else
        fontSize = 24;
#endif
        this.GetComponentByChildName<Button>("Enter").onClick.AddListener(GotoLogin);

        //CheckVersion();
        UpdateVersionIfNeeded();
    }

    public override void Show(UIData uiData = null)
    {
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
    }

    /// <summary>
    /// 打开登录界面
    /// </summary>
    private void GotoLogin()
    {
        UIManager.Instance.CloseModuleUI<CheckVersionModule>(ParentPanel, null, () =>
        {
            SendMsg(new MsgBase((ushort)LoginEvent.Login));
        });
    }

    /// <summary>
    /// 是否需要更新版本
    /// </summary>
    /// <param name="latestVersion"></param>
    /// <returns></returns>
    private bool NeedUpdateVersion(RequestData.Version latestVersion)
    {
        if (latestVersion == null || string.IsNullOrEmpty(latestVersion.downloadUrl))
            return false;

        return !Application.version.Equals(latestVersion.version) || !CompareVersionCache(latestVersion.version, latestVersion.downloadUrl);
    }

    /// <summary>
    /// 检查版本并更新
    /// </summary>
    private void UpdateVersionIfNeeded()
    {
        RequestManager.Instance.GetLatestVersion(ApiData.state, (data) =>
        {
            //无新版本或应用程序版本号与最新版本一致
            if (!NeedUpdateVersion(data))
            {
                RequestManager.Instance.GetVersionInfo(Application.version, (data) =>
                {
                    if (data == null)
                    {
                        Log.Error("新版本获取失败，数据为空！");
                        GotoLogin();
                    }
                    else
                    {
                        if (CompareVersionCache(data.version, data.downloadUrl))
                        {
                            GotoLogin();
                        }
                        else//更新新版本后第一次进入，显示更新日志
                        {
                            this.GetComponentByChildName<Text>("Info").text = $"<size=24>ARSeek新版本V{data.version}上线</size>\n【更新时间】\n{data.addTime}\n【更新内容】\n{data.content}\n";
                            canvasGroup.alpha = 1;
                            canvasGroup.interactable = true;
                            UpdateVersionCache(data.version, data.downloadUrl);
                        }
                    }
                }, (code, msg) =>
                {
                    Log.Warning("新版本获取失败！", msg);
                    GotoLogin();
                });
            }
            //应用程序版本号与最新版本不一致
            else
            {
                this.GetComponentByChildName<Text>("Info").text = $"<size={fontSize}>ARSeek新版本V{data.version}上线</size>\n【更新时间】\n{data.addTime}\n【更新内容】\n{data.content}\n";
                canvasGroup.alpha = 1;
                canvasGroup.interactable = true;
                var Enter = this.GetComponentByChildName<Button>("Enter");
                {
                    Enter.GetComponentInChildren<Text>().text = "更新";
                    Enter.onClick.RemoveAllListeners();
                    Enter.onClick.AddListener(() =>
                    {
#if UNITY_EDITOR
                        GotoLogin();
#elif UNITY_ANDROID
                        GotoDownloadPage($"{ResManager.Instance.OSSDownLoadPath}{data.downloadUrl}");
#elif UNITY_STANDALONE_WIN
                        GotoDownloadPage($"{ResManager.Instance.OSSDownLoadPath}{data.downloadUrl}");
#endif
                    });
                }
            }
        }, (code, msg) =>
        {
            Log.Error("获取版本号失败！", msg);
            UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("获取版本号失败！"));
            GotoLogin();
        });
    }

    /// <summary>
    /// 检查当前版本是不是最新版本
    /// </summary>
    private void CheckVersion()
    {
        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
        {
            RequestManager.Instance.GetLatestVersion(ApiData.state, (data) =>
            {
                if (data == null || Application.version.Equals(data.version))//当前版本为最新版本
                {
                    GotoLogin();
                }
                else//当前版本不是最新版本
                {
                    this.GetComponentByChildName<Text>("Info").text = $"<size=42>ARSeek新版本V{data.version}上线</size>\n【更新时间】\n{data.addTime}\n【更新内容】\n{data.content}\n";
                    canvasGroup.alpha = 1;
                    canvasGroup.interactable = true;
                    var Enter = this.GetComponentByChildName<Button>("Enter");
                    {
                        Enter.onClick.RemoveAllListeners();
                        Enter.onClick.AddListener(() =>
                        {
                            GotoDownloadPage(data.downloadUrl);
                        });

                        Enter.GetComponentInChildren<Text>().text = "更新";
                    }
                }

            }, (code, msg) =>
            {
                Log.Error("获取版本号失败！", msg);
                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("获取版本号失败！"));
                GotoLogin();
            });
        }
        else if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
        {
            RequestManager.Instance.GetVersionInfo(Application.version, (data) =>
            {
                if (data == null)
                {
                    Log.Warning("新版本获取失败，数据为空！");
                    GotoLogin();
                }
                else
                {
                    if (CompareVersionCache(data.version, data.downloadUrl))//无新版本，不显示日志
                    {
                        GotoLogin();
                    }
                    else//更新新版本后第一次进入，显示更新日志
                    {
                        this.GetComponentByChildName<Text>("Info").text = $"<size=24>ARSeek新版本V{data.version}上线</size>\n【更新时间】\n{data.addTime}\n【更新内容】\n{data.content}\n";
                        canvasGroup.alpha = 1;
                        canvasGroup.interactable = true;
                        UpdateVersionCache(data.version, data.downloadUrl);
                    }
                }
            }, (code, msg) =>
            {
                Log.Error("新版本获取失败！", msg);
                GotoLogin();
            });
        }
    }

    /// <summary>
    /// 获取移动端APK
    /// </summary>
    /// <param name="url"></param>
    private void GotoDownloadPage(string url)
    {
        UIManager.Instance.OpenUI<LoadingPanel2>(UILevel.PopUp, new LoadingPanel2.PanelData()
        {
            tip = "下载最新版本中...",
            slider = slider =>
            {
                ResManager.Instance.DownLoadAPK(url, value =>
                {
                    if (value >= 1)
                    {
                        Log.Debug("新版本下载完成");

                        Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
                        {
                            popupDic.Add("确定", new PopupButtonData(() =>
                            {
                                string fileName = url.Split('/')[url.Split('/').Length - 1];
                                string filePath = ResManager.Instance.resourcesCacheRootPath + "/" + fileName;
#if UNITY_ANDROID
                                AndroidInstallAPK(filePath);
#elif UNITY_STANDALONE_WIN
                                StartWindowsUpdate(filePath);
#endif
                                UIManager.Instance.CloseUI<LoadingPanel2>();
                            }, true));

                            popupDic.Add("取消", new PopupButtonData(() => Application.Quit()));

                            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "下载完毕，开始安装！", popupDic));
                        }
                    }
                    else if (value < -0.99f)
                    {
                        Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
                        {
                            popupDic.Add("确定", new PopupButtonData(() => Application.Quit(), true));

                            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "新版本下载失败！", popupDic));
                        }
                    }

                    slider.value = value;
                });
            }
        });
    }

    /// <summary>
    /// 安卓8以上不支持直接在unity中创建res\xml\fiel_path.xml,只能以aar的形式获得FileProvider
    /// 替换了原来的安装方法
    /// </summary>
    /// <param name="path"></param>
    public void AndroidInstallAPK(string path)
    {
        AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
        AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent",
            intentClass.GetStatic<string>("ACTION_VIEW"));

        string authority = Application.identifier + ".fileprovider";

        AndroidJavaClass fileProvider = new AndroidJavaClass("androidx.core.content.FileProvider");
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject fileObject = new AndroidJavaObject("java.io.File", path);

        AndroidJavaObject uri = fileProvider.CallStatic<AndroidJavaObject>(
            "getUriForFile", currentActivity, authority, fileObject);

        intentObject.Call<AndroidJavaObject>("setDataAndType", uri, "application/vnd.android.package-archive");
        intentObject.Call<AndroidJavaObject>("addFlags", intentClass.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION"));
        intentObject.Call<AndroidJavaObject>("addFlags", intentClass.GetStatic<int>("FLAG_ACTIVITY_NEW_TASK"));

        currentActivity.Call("startActivity", intentObject);
    }

    private bool CompareVersionCache(string version, string downloadUrl)
    {
        try
        {
            var versionHistory = JsonTool.DeSerializable<Dictionary<string, string>>(PlayerPrefs.GetString(GlobalInfo.commonVersion));
            return versionHistory != null && versionHistory.ContainsKey(version) && versionHistory[version].Equals(downloadUrl);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"CompareVersionCache error {PlayerPrefs.GetString(GlobalInfo.commonVersion)}");
            return false;
        }
    }

    private void UpdateVersionCache(string version, string downloadUrl)
    {
        try
        {
            var versionHistory = JsonTool.DeSerializable<Dictionary<string, string>>(PlayerPrefs.GetString(GlobalInfo.commonVersion));
            {
                if (versionHistory == null)
                    versionHistory = new Dictionary<string, string>();

                if (!versionHistory.ContainsKey(version))
                    versionHistory.Add(version, downloadUrl);
                else
                    versionHistory[version] = downloadUrl;

                PlayerPrefs.SetString(GlobalInfo.commonVersion, JsonTool.Serializable(versionHistory));
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"CompareVersionCache error {PlayerPrefs.GetString(GlobalInfo.commonVersion)}");
        }
    }

    /// <summary>
    /// PC端自更新：解压zip到临时目录，生成替换脚本，启动脚本后退出
    /// </summary>
    private void StartWindowsUpdate(string zipPath)
    {
        try
        {
            string installDir = Path.GetDirectoryName(Application.dataPath);
            string stagingDir = Path.Combine(Path.GetTempPath(), Application.productName + "_Update");

            if (Directory.Exists(stagingDir))
                FileTool.FolderDelete(stagingDir);
            Directory.CreateDirectory(stagingDir);
            ExtractZip(zipPath, stagingDir);

            // 若zip内包了一层顶层目录，则取该目录作为实际源目录
            string sourceDir = stagingDir;
            string[] entries = Directory.GetFileSystemEntries(stagingDir);
            if (entries.Length == 1 && Directory.Exists(entries[0]))
                sourceDir = entries[0];

            string batPath = Path.Combine(Path.GetTempPath(), Application.productName + "_update.bat");
            GenerateUpdateScript(batPath, installDir, stagingDir, sourceDir, Application.productName + ".exe");

            Process.Start(new ProcessStartInfo(batPath) { UseShellExecute = true });
            Application.Quit();
        }
        catch (Exception e)
        {
            Log.Error("启动更新失败: " + e);
            UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("更新失败，请重试！"));
        }
    }

    /// <summary>
    /// 解压zip到指定目录
    /// </summary>
    private void ExtractZip(string zipPath, string destDir)
    {
        string destRoot = Path.GetFullPath(destDir);
        using (ZipArchive archive = new ZipArchive(File.OpenRead(zipPath), ZipArchiveMode.Read))
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string targetPath = Path.GetFullPath(Path.Combine(destDir, entry.FullName));
                // 防止路径穿越
                if (!targetPath.StartsWith(destRoot, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(targetPath);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                using (Stream src = entry.Open())
                using (FileStream dst = File.Create(targetPath))
                {
                    src.CopyTo(dst);
                }
            }
        }
    }

    /// <summary>
    /// 生成等待游戏退出后覆盖文件并重启的批处理脚本
    /// </summary>
    private void GenerateUpdateScript(string batPath, string installDir, string stagingDir, string sourceDir, string exeName)
    {
        string exePath = Path.Combine(installDir, exeName);
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("@echo off");
        sb.AppendLine("chcp 65001 >nul");
        sb.AppendLine(":wait");
        sb.AppendLine("timeout /t 1 /nobreak >nul");
        sb.AppendLine("tasklist | find /I \"" + exeName + "\" >nul");
        sb.AppendLine("if not errorlevel 1 goto wait");
        sb.AppendLine("xcopy \"" + sourceDir + "\\*\" \"" + installDir + "\\\" /E /Y /H /R /C /I >nul");
        sb.AppendLine("if errorlevel 4 echo update failed, relaunching current version...");
        sb.AppendLine("rd /S /Q \"" + stagingDir + "\"");
        sb.AppendLine("start \"\" \"" + exePath + "\"");
        sb.AppendLine("del \"%~f0\"");
        File.WriteAllText(batPath, sb.ToString(), new UTF8Encoding(false));
    }
}