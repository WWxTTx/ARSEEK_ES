using UnityFramework.Runtime;
using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// 加入单位界面
/// </summary>
public class QRPanel : UIPanelBase
{
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
    /// 打开Panel传入数据
    /// </summary>
    private PanelData panelData;
    /// <summary>
    /// 显示相机画面
    /// </summary>
    private RawImage webCamTexture;
    /// <summary>
    /// 是否有相机
    /// </summary>
    private bool isHasCam = false;
    /// <summary>
    /// 是否暂停扫描
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

    /// <summary>
    /// 初始化，只有创建(实例化)时才调用
    /// </summary>
    public override void Open(UIData uiData = null)
    {
        base.Open();
        webCamTexture = ComponentExtend.GetComponentByChildName<RawImage>(transform, "RawImage");
        //ComponentExtend.GetComponentByChildName<Button>(transform, "InputBtn").onClick.AddListener(InputBtn);
        scanLine = ComponentExtend.GetComponentByChildName<RectTransform>(transform, "ScanLine");
        startPos = scanLine.anchoredPosition;
        scanLineImage = scanLine.GetComponent<Image>();
    }

    public override void Show(UIData uiData = null)
    {
        base.Show();
        if (uiData != null)
        {
            panelData = uiData as PanelData;
            this.GetComponentByChildName<Button>("Exit").onClick.AddListener(GoBack);
        }

#if UNITY_ANDROID 
        this.GetPermission(PermissionManager.Request.相机, arg =>
        {
            switch (arg)
            {
                case PermissionManager.Result.已授权:
                    GetWebCam();
                    break;
                case PermissionManager.Result.未授权:
                case PermissionManager.Result.未授权且不再询问:
                    Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
                    popupDic.Add("确认", new PopupButtonData(null, true));
                    UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("错误", "未经授权，无法打开摄像头！", popupDic));
                    break;
            }
        });
#else
        GetWebCam();
#endif
    }
    /// <summary>
    /// 返回上一级
    /// </summary>
    private void GoBack()
    {
        panelData.callBack.Invoke(false, string.Empty);
        UIManager.Instance.CloseUI<QRPanel>();
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        CloseScan();
        QRBase.Stop();
        base.Close();
    }

    /// <summary>
    /// 获取摄像头
    /// </summary>
    private void GetWebCam()
    {
        if (QRBase.Start(webCamTexture))
        {
            isHasCam = true;
            OpenScan();
        }
        else
        {
            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("确认", new PopupButtonData(null, true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("错误", "未找到摄像头，请检查摄像头是否可用", popupDic));
        }
    }

    /// <summary>
    /// 开始扫描
    /// </summary>
    private void OpenScan()
    {
        if (!isHasCam) return;

        QRBase.Replay();
        onScan = true;
        Color color = scanLineImage.color;
        color.a = 0f;
        scanLineImage.color = color;
        scanLine.anchoredPosition = startPos;
        var endPoint = (scanLine.parent as RectTransform).rect.height - Mathf.Abs(scanLine.anchoredPosition.y) - scanLine.rect.height;
        Sequence sequence = DOTween.Sequence();
        sequence.Append(scanLineImage.DOFade(1, playTime_Show));
        sequence.Append(scanLine.DOAnchorPosY(-endPoint, playTime_Move));
        sequence.Append(scanLineImage.DOFade(0, playTime_Hide));
        sequence.SetLoops(-1);
        sequence.SetId("ScanLine");
    }
    /// <summary>
    /// 暂停扫描
    /// </summary>
    private void CloseScan()
    {
        if (!isHasCam) return;

        onScan = false;
        DOTween.Kill("ScanLine");
        QRBase.Pause();
    }
    /// <summary>
    /// 每隔2秒识别一次二维码
    /// </summary>
    private void Update()
    {
        if (!isHasCam || !onScan) return;

        timer += Time.deltaTime;
        if (timer > interval)
        {
            timer = 0;
            // 扫描二维码
            qrCode = QRBase.DecodeQRCode();
            if (!string.IsNullOrEmpty(qrCode))
            {
                CloseScan();
                panelData.callBack.Invoke(true, qrCode);
                UIManager.Instance.CloseUI<QRPanel>();
                //RequestManager.Instance.VerifyCompany(qrCode, VerifyCompanySuccess, VerifyCompanyFailure);
            }
        }
    }

    public class PanelData : UIData
    {
        public UnityAction<bool, string> callBack;
    }

    #region 旧代码
    ///// <summary>
    ///// 手动输入
    ///// </summary>
    ///// <param name="go"></param>
    //private void InputBtn()
    //{
    //    CloseScan();
    //    List<InputFieldData> popupInputFields = new List<InputFieldData>()
    //    {
    //        new InputFieldData(string.Empty, "输入激活信息",string.Empty, false)
    //    };
    //    Dictionary<string, InputPopupButtonData> popupDic2 = new Dictionary<string, InputPopupButtonData>();
    //    popupDic2.Add("取消", new InputPopupButtonData((inputFields) =>
    //    {
    //        OpenScan();
    //        return true;
    //    }));
    //    popupDic2.Add("提交", new InputPopupButtonData((inputFields) =>
    //    {
    //        if (string.IsNullOrEmpty(inputFields[0].text))
    //        {
    //            inputFields[0].ShowTip("密码不能为空");
    //            inputFields[0].Select();
    //            return false;
    //        }
    //        //RequestManager.Instance.VerifyCompany(inputFields[0].text, VerifyCompanySuccess, VerifyCompanyFailure);
    //        return true;
    //    }, true));

    //    UIManager.Instance.OpenUI<InputPopupPanel>(UILevel.Normal, new UIInputPopupData("手动输入", popupInputFields, popupDic2));
    //}
    //private void VerifyCompanySuccess(VerifyCompanyInfo data)
    //{
    //    string popupInfo = string.Format("<b>是否加入{0}</b>", data.join_school);
    //    if (!string.IsNullOrEmpty(data.exit_school))
    //        popupInfo += string.Format("\r\n<size=25>加入后将自动退出{0}</size>", data.exit_school);

    //    //Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
    //    //popupDic.Add("取消", new PopupButtonData(() => OpenScan()));
    //    //popupDic.Add("确认", new PopupButtonData(() => RequestManager.Instance.JoinCompany(JoinCompanySuccess, JoinCompanyFailure), true));
    //    //UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", popupInfo, popupDic));
    //}
    //private void VerifyCompanyFailure(string failureMessage)
    //{
    //    Log.Error($"加入单位失败！原因为：{failureMessage}");

    //    Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
    //    popupDic.Add("确认", new PopupButtonData(() => OpenScan(), true));

    //    if (failureMessage == "无效参数")
    //        UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "激活码错误，请检查激活码是否正确", popupDic));
    //    else if (failureMessage == "HTTP/1.1 500 Internal Server Error")
    //        UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "加入单位失败，请检查网络或激活码是否正确", popupDic));
    //    else
    //        UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "加入单位失败，您已加入该单位", popupDic));
    //}
    //private void JoinCompanySuccess()
    //{
    //    Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
    //    popupDic.Add("确定", new PopupButtonData(() =>
    //    {
    //        //后台重新登录
    //        string account = PlayerPrefs.GetString(GlobalInfo.accountCacheKey);
    //        string password = PlayerPrefs.GetString(GlobalInfo.passwordCacheKey);
    //        RequestManager.Instance.Login(account, password, LoginSuccess, LoginFailure);
    //    }, true));
    //    UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "加入成功", popupDic));
    //}
    //private void JoinCompanyFailure(string failureMessage)
    //{
    //    Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
    //    popupDic.Add("确认", new PopupButtonData(() => OpenScan(), true));
    //    UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "加入失败", popupDic));
    //    Log.Error($"删除模板票失败！原因为：{failureMessage}");
    //}
    //private void LoginSuccess(Account data, string message)
    //{
    //    ApiData.AccessToken = data.token.access_token;

    //    //记录服务器返回数据到本地
    //    RequestBase temp = new RequestBase();
    //    var tempSave = temp.GetNewUrl(ApiData.LoginQuery);
    //    string value = ConfigXML.GetData(DtataType.LocalSever, tempSave);
    //    if (string.IsNullOrEmpty(value))
    //        ConfigXML.AddData(DtataType.LocalSever, tempSave, message);
    //    else
    //        ConfigXML.UpdateData(DtataType.LocalSever, tempSave, message);

    //    GlobalInfo.account = data;

    //    int t = GlobalInfo.account.role_id / 10;
    //    if (t == 7)
    //        GlobalInfo.account.role_type = 1;
    //    else if (t == 8)
    //        GlobalInfo.account.role_type = 0;

    //    GoBack();
    //}
    //private void LoginFailure(int code, string failureMessage)
    //{
    //    Log.Error($"刷新数据失败！原因为：{failureMessage}");
    //}
    ///// <summary>
    ///// 登录回调
    ///// </summary>
    ///// <param name="success"></param>
    ///// <param name="loginInfo"></param>.
    //private void LoginCallback(bool success, string info)
    //{
    //    if (success)
    //    {
    //        LoginRequestResult result = JsonTool.DeSerializable<LoginRequestResult>(info);
    //        GlobalInfo.account = result.data;

    //        int t = GlobalInfo.account.role_id / 10;
    //        if (t == 7)
    //            GlobalInfo.account.role_type = 1;
    //        else if (t == 8)
    //            GlobalInfo.account.role_type = 0;

    //        GoBack();
    //    }
    //    else
    //    {
    //        Log.Debug("刷新数据失败：" + info);
    //    }
    //}
    #endregion
}