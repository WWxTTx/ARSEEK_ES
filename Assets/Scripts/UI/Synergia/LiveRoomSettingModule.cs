using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using UnityFramework.Runtime;
using System.Collections;

/// <summary>
/// 直播间设置模块
/// </summary>
public class LiveRoomSettingModule : UIModuleBase
{
    private RectTransform Background;
    private CanvasGroup CanvasGroup;

    private Text Title;
    private Text RoomTypeTitle;
    private Text RoomNameTitle;
    private Text RoomPwdTitle;

    /// <summary>
    /// 关闭模块
    /// </summary>
    private Button CloseBtn;

    /// <summary>
    /// 房间类型
    /// </summary>
    private InputField_LinkMode RoomType;
    /// <summary>
    /// 房间名称
    /// </summary>
    private InputField_LinkMode RoomName;
    /// <summary>
    /// 房间密码
    /// </summary>
    private InputField_LinkMode RoomPassword;
    /// <summary>
    /// 房主昵称
    /// </summary>
    private InputField_LinkMode HostName;

    /// <summary>
    /// 保存按钮
    /// </summary>
    private Button SaveBtn;

    private string prevRoomName;
    private string prevPassword;


    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        Background = transform.GetComponentByChildName<RectTransform>("Background");
        CanvasGroup = Background.GetComponent<CanvasGroup>();

        CloseBtn = transform.GetComponentByChildName<Button>("CloseBtn");
        RoomType = transform.GetComponentByChildName<InputField_LinkMode>("RoomType");
        RoomName = transform.GetComponentByChildName<InputField_LinkMode>("RoomName");
        RoomPassword = transform.GetComponentByChildName<InputField_LinkMode>("RoomPassword");
        HostName = transform.GetComponentByChildName<InputField_LinkMode>("HostName");
        SaveBtn = transform.GetComponentByChildName<Button>("Save");

        Title = transform.GetComponentByChildName<Text>("Title");
        RoomTypeTitle = RoomType.transform.parent.GetComponentByChildName<Text>("Title");
        RoomNameTitle = RoomName.transform.parent.GetComponentByChildName<Text>("Title");
        RoomPwdTitle = RoomPassword.transform.parent.GetComponentByChildName<Text>("Title");
        if (GlobalInfo.IsExamMode())
        {
            Title.text = "考核房间信息";
            RoomTypeTitle.text = "考核类型";
            RoomNameTitle.text = "考核房间名称";
            RoomPwdTitle.text = "考核房间密码";
        }

        CloseBtn.onClick.AddListener(() =>
        {
            SendMsg(new MsgBase((ushort)RoomChannelEvent.LiveRoomSettingModuleClose));
        });

        RoomName.onValueChanged.AddListener((value) =>
        {
            RoomName.text = value.RemoveSpecialSymbols();
            SaveBtn.interactable = !string.IsNullOrEmpty(value) && (string.IsNullOrEmpty(RoomPassword.text) || RoomPassword.text.Length == 6);
        });
        RoomPassword.onValueChanged.AddListener((value) => SaveBtn.interactable = !string.IsNullOrEmpty(RoomName.text) && (string.IsNullOrEmpty(value) || value.Length == 6));
        
        SaveBtn.onClick.AddListener(() =>
        {
            StartCoroutine(SetRoomInfo());
        });
    }

    /// <summary>
    /// 修改房间信息
    /// </summary>
    /// <returns></returns>
    private IEnumerator SetRoomInfo()
    {
        bool setNameFinish = false;
        bool setPasswordFinish = false;

        if (!RoomName.text.Equals(GlobalInfo.roomInfo.RoomName))
        {
            prevRoomName = RoomName.text;
            NetworkManager.Instance.SetRoomName(GlobalInfo.roomInfo.Uuid, RoomName.text, () =>
            {
                GlobalInfo.roomInfo.RoomName = RoomName.text;
                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("房间名称修改成功"));
                //同步房间信息
                MsgString msgStr = new MsgString((ushort)RoomChannelEvent.RoomInfo, GlobalInfo.roomInfo.RoomName);
                NetworkManager.Instance.SendIMMsg(new MsgBrodcastOperate(msgStr.msgId, JsonTool.Serializable(msgStr)));
                setNameFinish = true;
            }, (code, setRoomNameFailed) =>
            {
                Log.Error($"修改房间[{GlobalInfo.roomInfo.Uuid}]名称失败, 原因为：{setRoomNameFailed}");
                RoomName.text = prevRoomName;
                switch (code)
                {
                    case 0:
                        UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("网络异常，房间名称修改失败"));
                        break;
                    default:
                        UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("房间名称修改失败"));
                        break;
                }
                setNameFinish = true;
            });
        }
        else
        {
            setNameFinish = true;
        }

        if (!RoomPassword.text.Equals(GlobalInfo.roomInfo.Password))
        {
            prevPassword = RoomPassword.text;
            NetworkManager.Instance.SetRoomPassword(GlobalInfo.roomInfo.Uuid, RoomPassword.text, () =>
            {
                GlobalInfo.roomInfo.Password = RoomPassword.text;
                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("房间密码修改成功"));
                setPasswordFinish = true;
            },(code, setRoomPasswordFailed) =>
            {
                Log.Error($"修改房间[{GlobalInfo.roomInfo.Uuid}]密码失败, 原因为：{setRoomPasswordFailed}");
                RoomPassword.text = prevPassword;
                switch (code)
                {
                    case 0:
                        UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("网络异常，房间密码修改失败"));
                        break;
                    default:
                        UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("房间密码修改失败"));
                        break;
                }
                setPasswordFinish = true;
            });
        }
        else
        {
            setPasswordFinish = true;
        }

        yield return new WaitUntil(() => setNameFinish && setPasswordFinish);
        SendMsg(new MsgBase((ushort)RoomChannelEvent.LiveRoomSettingModuleClose));
    }

    public override void Show(UIData uiData = null)
    {
        base.Show(uiData);

        if (GlobalInfo.IsExamMode())
        {
            RoomType.text = GlobalInfo.roomInfo.ExamType == (int)ServiceRequestData.ExamRoomType.Person ? "个人考核" : "小组考核";
            RoomName.text = GlobalInfo.roomInfo.RoomName;
            RoomPassword.text = GlobalInfo.roomInfo.Password;
            HostName.text = GlobalInfo.roomInfo.CreatorName;
        }
        else
        {
            NetworkManager.Instance.GetRoomInfo(GlobalInfo.roomInfo.Uuid, (roomInfoModel) =>
            {
                if (roomInfoModel != null)
                {
                    GlobalInfo.roomInfo = roomInfoModel;

                    if (GlobalInfo.roomInfo.RoomType == 0)
                        RoomType.text = GlobalInfo.roomInfo.ExamType == (int)ServiceRequestData.ExamRoomType.Person ? "个人考核" : "小组考核";
                    else
                        RoomType.text = GlobalInfo.roomInfo.RoomType == (int)ServiceRequestData.RoomType.Live ? "直播房间" : "协同房间";

                    RoomName.text = GlobalInfo.roomInfo.RoomName;
                    RoomPassword.text = GlobalInfo.roomInfo.Password;
                    HostName.text = GlobalInfo.roomInfo.CreatorName;
                }
            }, (code, msg) =>
            {
                switch (code)
                {
                    case 0:
                        UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("网络异常，获取房间信息失败"));
                        break;
                }
            });
        }  
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        base.Close(uiData, callback);
        StopAllCoroutines();
    }

    #region 动效
    protected override float exitAnimePlayTime => 0.1f;

    public override void JoinAnim(UnityAction callback)
    {
        //SoundManager.Instance.PlayEffect("Popup");
#if UNITY_STANDALONE
        JoinSequence.Join(DOTween.To(() => 0.2f * Vector3.one, (value) => Background.transform.localScale = value, Vector3.one, JoinAnimePlayTime));
#else
        JoinSequence.Join(DOTween.To(() => new Vector2(Background.sizeDelta.x, Background.anchoredPosition.y), (value) => Background.anchoredPosition = value, new Vector2(0f, Background.anchoredPosition.y), JoinAnimePlayTime));
#endif
        JoinSequence.Join(DOTween.To(() => 0f, (value) => CanvasGroup.alpha = (value), 1f, JoinAnimePlayTime));
        base.JoinAnim(callback);
    }

    public override void ExitAnim(UnityAction callback)
    {
#if UNITY_STANDALONE
        ExitSequence.Join(DOTween.To(() => Vector3.one, (value) => Background.transform.localScale = value, 0.8f * Vector3.one, ExitAnimePlayTime));
#else
        ExitSequence.Join(DOTween.To(() => new Vector2(0f, Background.anchoredPosition.y), (value) => Background.anchoredPosition = value, new Vector2(Background.sizeDelta.x, Background.anchoredPosition.y), ExitAnimePlayTime));
#endif
        ExitSequence.Join(DOTween.To(() => 1f, (value) => CanvasGroup.alpha = (value), 0f, ExitAnimePlayTime));
        base.ExitAnim(callback);
    }
    #endregion
}