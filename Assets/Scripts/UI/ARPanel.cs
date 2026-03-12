using UnityFramework.Runtime;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static UnityFramework.Runtime.RequestData;
using System.Linq;
using UnityEngine.Networking;

/// <summary>
/// 多图识别
/// </summary>
public class ARPanel : UIPanelBase
{
    /// <summary>
    /// 是否重新请求课程列表
    /// </summary>
    public static bool request = true;

    #region 扫描动效相关参数
    public float moveDis = 420f;
    public float playTime_Show = 0.2f;
    public float playTime_Move = 1.26f;
    public float playTime_Hide = 0.8f;
    private RectTransform scanLine;
    private Vector3 startPos;
    private Image scanLineImage;
    #endregion

    /// <summary>
    /// 扫描动效父节点
    /// </summary>
    private GameObject Background;
    /// <summary>
    /// 返回按钮
    /// </summary>
    private Button backBtn;

    /// <summary>
    /// 摄像头textrue
    /// </summary>
    private RawImage cameraTextrue;

    /// <summary>
    /// 单位课程列表
    /// </summary>
    private List<Course> courseList;

    /// <summary>
    /// 是否扫描二维码
    /// </summary>
    private bool onScan = false;
    /// <summary>
    /// 识别计时器
    /// </summary>
    private float timer = 0;
    /// <summary>
    /// 识别间隔时间
    /// </summary>
    private float interval = 1f;
    /// <summary>
    /// 识别出的数据
    /// </summary>
    private string qrCode;

    public override void Open(UIData uiData = null)
    {
        base.Open();
        //？
        //RequestManager.Instance.GetCourseList(GetCourseSuccess, GetCourseFailure);
        Init();
    }

    private void Init()
    {
        ARManager.Instance.ControlBackgroundCanvas(false);

        cameraTextrue = this.GetComponentByChildName<RawImage>("CameraTextrue");
        Background = this.FindChildByName("Background").gameObject;
        scanLine = this.GetComponentByChildName<RectTransform>("ScanLine");
        startPos = scanLine.anchoredPosition;
        scanLineImage = scanLine.GetComponent<Image>();
        backBtn = this.GetComponentByChildName<Button>("Exit");
        backBtn.onClick.AddListener(() => GoBack());
    }

    public override void Show(UIData uiData = null)
    {
        base.Show();
        Background.SetActive(true);
#if UNITY_ANDROID
        this.GetPermission(PermissionManager.Request.相机, arg =>
        {
            switch (arg)
            {
                case PermissionManager.Result.已授权:
                    GetCourse();
                    break;
                case PermissionManager.Result.未授权:
                case PermissionManager.Result.未授权且不再询问:
                    GoBack();
                    break;
            }
        });
#else
        GetCourse();
#endif
    }

    /// <summary>
    /// 返回上级
    /// </summary>
    private void GoBack()
    {
        UIManager.Instance.CloseUI<ARPanel>();
        UIManager.Instance.OpenUI<HomePagePanel>();
    }

    public override void Hide(UIData uiData = null, UnityAction callback = null)
    {
        CloseScan();
        base.Hide(uiData, callback);
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        CloseScan();
        QRBase.Stop();
        ARManager.Instance.ControlBackgroundCanvas(true);
        base.Close(uiData, callback);
    }

    //public override void JoinAnim(UnityAction callback)
    //{
    //    var exit = this.FindChildByName("Exit").GetComponent<RectTransform>();
    //    float offset = 112f;
    //    var posX = exit.rect.width + offset;
    //    exit.gameObject.SetActive(true);
    //    exit.anchoredPosition = new Vector2(-posX, exit.anchoredPosition.y);
    //    JoinSequence.Append(exit.DOAnchorPosX(offset, JoinAnimePlayTime));
    //    base.JoinAnim(callback);
    //}

    /// <summary>
    /// 获取课程列表
    /// </summary>
    private void GetCourse()
    {
        if (!request)
        {
            GetWebCam();
        }
        else
        {
            UIManager.Instance.OpenUI<LoadingPanel>(UILevel.Loading);
            RequestManager.Instance.GetCourseList(courseData =>
            {
                GlobalInfo.SaveCourseInfo(courseData);
                RequestManager.Instance.GetCourseABPackageList((courseABData) =>
                {
                    request = false;
                    GlobalInfo.SaveCourseABInfo(courseABData);
                    UIManager.Instance.CloseUI<LoadingPanel>();
                    //courseList = courseData;
                    GetWebCam();
                }, (msg) => GetCourseFailure(msg));
            }, failureMessage => GetCourseFailure(failureMessage));
        }
    }

    private void GetCourseFailure(string failureMessage)
    {
        string hit = "获取课程列表失败";
        if (GlobalInfo.isOffLine)
            hit += ",请先正常登录后下载资源";

        Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
        popupDic.Add("确定", new PopupButtonData(() => { GoBack(); }, true));
        UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("错误", hit, popupDic));

        Log.Error($"获取课程列表失败！原因为：{failureMessage}");
    }

    /// <summary>
    /// 获取摄像头
    /// </summary>
    private void GetWebCam()
    {
        if (QRBase.Start(cameraTextrue))
        {
            OpenScan();
        }
        else
        {
            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("确认", new PopupButtonData(() => GoBack(), true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "发生意料之外的错误，请检查相机是否可用", popupDic));
        }
    }

    /// <summary>
    /// 开始扫描
    /// </summary>
    private void OpenScan()
    {
        Background.SetActive(true);
        QRBase.Replay();

        timer = 0;
        onScan = true;

        //动效
        Color color = scanLineImage.color;
        color.a = 0f;
        scanLineImage.color = color;
        scanLine.anchoredPosition = startPos;
        Sequence sequence = DOTween.Sequence();
        sequence.Append(scanLineImage.DOFade(1, playTime_Show));
        sequence.Append(scanLine.DOAnchorPosY(startPos.y - moveDis, playTime_Move));
        sequence.Append(scanLineImage.DOFade(0, playTime_Hide));
        sequence.SetLoops(-1);
        sequence.SetId("ScanLine");
    }

    /// <summary>
    /// 暂停扫描
    /// </summary>
    private void CloseScan()
    {
        Background.SetActive(false);
        onScan = false;
        DOTween.Kill("ScanLine");
        QRBase.Pause();
    }

    /// <summary>
    /// 每隔2秒识别一次二维码
    /// </summary>
    private void Update()
    {
        if (!onScan) return;

        timer += Time.deltaTime;
        if (timer > interval)
        {
            timer = 0;
            // 扫描二维码
            qrCode = QRBase.DecodeQRCode();
            if (!string.IsNullOrEmpty(qrCode))
            {
                CloseScan();
                QRCallBack(qrCode);
            }
        }
    }

    /// <summary>
    /// 二维码扫描回调
    /// </summary>
    /// <param name="data">扫描出的string</param>
    private void QRCallBack(string data)
    {
        Log.Debug("识别成功:" + data);

        if (int.TryParse(data, out int courseID))
        {
            GlobalInfo.courseDicExists.TryGetValue(courseID, out GlobalInfo.currentCourseInfo);

            if (GlobalInfo.currentCourseInfo == null)
            {
                Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
                popupDic.Add("确定", new PopupButtonData(() => OpenScan(), true));
                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("错误", "当前账户未拥有该课程！", popupDic));
            }
            else
            {
                ResManager.Instance.LoadCoverImage(courseID.ToString(), ResManager.Instance.OSSDownLoadPath + GlobalInfo.currentCourseInfo.iconPath, false, texture =>
                {
                    Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
                    popupDic.Add("取消", new PopupButtonData(() =>
                    {
                        Resources.UnloadUnusedAssets();
                        OpenScan();
                    }, false));
                    popupDic.Add("进入课程", new PopupButtonData(() =>
                    {
                        if (GlobalInfo.isOffLine)
                        {
                            GlobalInfo.courseABDic.TryGetValue(courseID, out List<CourseABPackage> data);
                            ResManager.Instance.CheckUpdate(data, result =>
                             {
                                 if (result == 0)
                                 {
                                     GlobalInfo.isAR = true;
                                     UIManager.Instance.CloseUI<ARPanel>();
                                     ABPanelInfo data = new ABPanelInfo(courseID, typeof(ARPanel).ToString());
                                     UIManager.Instance.OpenUI<OPLCoursePanel>(UILevel.Normal, data);
                                 }
                                 else
                                 {
                                     Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
                                     popupDic.Add("确定", new PopupButtonData(() => OpenScan(), true));
                                     UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("错误", "该资源未下载，请先正常登录", popupDic, () => OpenScan()));
                                 }
                             });
                        }
                        else
                        {
                            GlobalInfo.isAR = true;
                            UIManager.Instance.CloseUI<ARPanel>();
                            ABPanelInfo data = new ABPanelInfo(courseID, typeof(ARPanel).ToString());
                            UIManager.Instance.OpenUI<OPLCoursePanel>(UILevel.Normal, data);
                        }
                    }, true));
                    string courseName = GlobalInfo.currentCourseInfo.name;
                    if (courseName.Length > 16)
                        courseName = $"{courseName.Substring(0, 13)}...{courseName.Substring(courseName.Length - 2)}";

                    UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", $"进入 <color=#FB5955>{courseName}</color> 课程？", texture, popupDic, null, false));
                });
            }
        }
        else
        {
            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("确定", new PopupButtonData(() => OpenScan(), true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("错误", "请使用正确二维码！", popupDic));
        }
    }
}