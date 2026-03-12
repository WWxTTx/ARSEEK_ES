using UnityFramework.Runtime;
using DG.Tweening;
using UnityEngine;
using Vuplex.WebView;
using System.Collections.Generic;

public enum ApiType
{
    本地,
    测试外网,
    外网,
    华能测试外网
}
public enum VersionType
{
    测试版,
    正式版,
}

/// <summary>
/// 程序启动入口
/// </summary>
public class StartSetup : StartSetupBase
{
    /// <summary>
    /// 0:本地，1测试外网，2外网
    /// </summary>
    public ApiType apiUrl = ApiType.测试外网;
    /// <summary>
    /// 0测试版，1正式版
    /// </summary>
    public VersionType state = VersionType.测试版;

    public bool ShowLog = true;

    public bool UploadLog = true;

    protected override void InstanceAwake()
    {
        base.InstanceAwake();

        //3d Webview 采用PC样式显示网页
        // Use a desktop User-Agent to request the desktop versions of websites.
        // https://developer.vuplex.com/webview/Web#SetUserAgent
        // Call this from Awake() to ensure it's called before the webview initializes.
        Web.SetUserAgent(false);

#if UNITY_ANDROID
        Screen.sleepTimeout = SleepTimeout.NeverSleep;//设置移动端不休眠
        SetPermission();//设置移动端权限请求失败提示
#endif
    }

    void Start()
    {
        Log.Debug($"当前版本号:<color=red>{Application.version}</color>");

        Option_GeneralModule.InitQuality();// 设置画质为历史设置
        UniversalRenderPipelineUtils.SetRendererFeatureActive("ScreenSpaceAmbientOcclusion", true);//默认开启ao

        DOTween.defaultEaseType = Ease.InOutCubic;//设置DoTween默认动画曲线

        if (apiUrl != ApiType.外网 || state != VersionType.正式版)//设置是否可打开调试信息面板
            LogManager.Instance.show = ShowLog;
        else
            LogManager.Instance.show = false;
        DebuggerSave.UploadEnabled = UploadLog;

        ApiData.Init((int)apiUrl, (int)state);// 设置服务器接口地址
        GlobalInfo.InitServerTime();

        InitResolution();
        DontDestroyOnLoad(gameObject);

        PlayerPrefs.SetString(GlobalInfo.appLoginLogoCacheKey, string.Empty);
        PlayerPrefs.SetString(GlobalInfo.appLoginBgCacheKey, string.Empty);

        RequestManager.Instance.GetOrgTheme((theme) =>
        {
            PlayerPrefs.SetString(GlobalInfo.appLoginLogoCacheKey, theme?.appLoginLogo);
            PlayerPrefs.SetString(GlobalInfo.appLoginBgCacheKey, theme?.appLoginBg);
            LoadLoginPanel();

        }, (error) =>
        {
            Log.Error($"获取主题失败! \n原因为：{error}");
            LoadLoginPanel();
        });
    }

    private void LoadLoginPanel()
    {
        UIManager.Instance.OpenUI<AnimMaskPanel>(UILevel.Top);
        UIManager.Instance.OpenUI<LoginPanel>(UILevel.Normal, new LoginPanel.PanelData()
        {
            startModule = LoginEvent.CheckVersion
        });
    }

    /// <summary>
    /// 初始化分辨率
    /// </summary>
    private void InitResolution()
    {
        /**
         * 由于Android端打包取消勾选Render Outside Safe Area, 导致Screen.width/height和通过Screen.resolutions获取到的设备支持的全屏分辨率存在差异, 从而导致UI出现拉伸
         * Screen.SetResolution((int)Screen.SafeArea.width, (int)Screen.SafeArea.height, true);则不会导致拉伸
         */
        //        //获取设置当前屏幕分辩率
        //        Resolution[] resolutions = Screen.resolutions;

        //#if UNITY_ANDROID
        //        if (resolutions != null && resolutions.Length > 0)
        //        {
        //            int width = resolutions[resolutions.Length - 1].width;
        //            int height = resolutions[resolutions.Length - 1].height;
        //            if (width > height)
        //                Screen.SetResolution(width, height, true);
        //            else
        //                Screen.SetResolution(height, width, true);
        //        }
        //#else
        //        if (resolutions != null && resolutions.Length > 0)
        //            Screen.SetResolution(resolutions[resolutions.Length - 1].width, resolutions[resolutions.Length - 1].height, true);
        //#endif
        //        //设置成全屏
        //        Screen.fullScreen = true;
    }


#if UNITY_ANDROID
    private void SetPermission()
    {
        //权限请求失败且未勾选不再询问时提示
        PermissionManager.denied = (arg) =>
        {
            Dictionary<string, PopupButtonData> bt = new Dictionary<string, PopupButtonData>();
            bt.Add("确认", new PopupButtonData(null, true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("权限错误", $"未授予{arg}权限,无法使用该功能!", bt));
        };

        //权限请求失败且勾选不再询问时提示
        PermissionManager.deniedAndDontAskAgain = (arg) =>
        {
            Dictionary<string, PopupButtonData> bt = new Dictionary<string, PopupButtonData>();
            bt.Add("确认", new PopupButtonData(null, true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("权限错误", $"未授予{arg}权限，且勾选了不再询问，请自行修改相关权限否则无法使用该功能", bt));
        };
    }
#endif
    protected override void InstanceDestroy()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
#endif
    }
}
