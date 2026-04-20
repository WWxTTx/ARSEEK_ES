using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityFramework.Runtime;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using static UnityFramework.Runtime.ServiceRequestData;

/// <summary>
/// 房间成员列表模块
/// </summary>
public class LiveRoomMemberModule : UIModuleBase
{
    private Button Collapse;

    /// <summary>
    /// 成员列表
    /// </summary>
    private ScrollRect MemberScrollView;
    /// <summary>
    /// 人数
    /// </summary>
    private Text MemberCount;

    private GameObject MainText;
    private GameObject OpText;
    private GameObject KickText;

    /// <summary>
    /// 全员禁言按钮
    /// </summary>
    private Button AllVoiceOffBtn;
    private GameObject VoiceOnIcon;
    private GameObject VoiceOffIcon;

    /// <summary>
    /// 搜索成员
    /// </summary>
    private SearchModule SearchModule;
    private GameObject Empty;

    private CanvasGroup canvasGroup;
    private RectTransform Background;

    /// <summary>
    /// 房间内所有用户的数据字典
    /// </summary>
    public Dictionary<int, GameObject> allMemberItem;
    /// <summary>
    /// 用户麦克风状态
    /// </summary>
    public Dictionary<int, Image> allMemberMicState;

    private OPLSynCoursePanel SynCoursePanel;

    private bool isOpen = false;

    private Color defaultIconColor = new Color(0.8509804f, 0.854902f, 0.8784314f);

    /// <summary>
    /// 避免重复提示主画面离线
    /// </summary>
    private bool mainScreenOffline = false;

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);
        AddMsg(new ushort[]
        {
            (ushort)RoomChannelEvent.UpdateMemberList,
            (ushort)RoomChannelEvent.OtherJoin,
            (ushort)RoomChannelEvent.OtherLeave,
            (ushort)RoomChannelEvent.StartMainScreen,
            (ushort)RoomChannelEvent.UpdateMainScreen,
            (ushort)RoomChannelEvent.UpdateControl,
            (ushort)RoomChannelEvent.TalkState,
            (ushort)MediaChannelEvent.MicOnAir,
            (ushort)RoomChannelEvent.LiveRoomMemberModuleShow,
            (ushort)RoomChannelEvent.LiveRoomMemberModuleClose
        });

        Init();

        allMemberItem = new Dictionary<int, GameObject>();
        allMemberMicState = new Dictionary<int, Image>();
    }

    public override void Show(UIData uiData = null)
    {
        base.Show(uiData);
        SynCoursePanel = (OPLSynCoursePanel)ParentPanel;
    }

    private void Init()
    {
        Collapse = transform.GetComponentByChildName<Button>("Collapse");
        MemberCount = transform.GetComponentByChildName<Text>("MemberCount");
        MainText = transform.FindChildByName("MainText").gameObject;
        OpText = transform.FindChildByName("OpText").gameObject;
        KickText = transform.FindChildByName("KickText").gameObject;
        MemberScrollView = transform.GetComponentByChildName<ScrollRect>("MemberScrollView");
        AllVoiceOffBtn = transform.GetComponentByChildName<Button>("AllVoiceOffBtn");
        VoiceOnIcon = transform.FindChildByName("VoiceOn").gameObject;
        VoiceOffIcon = transform.FindChildByName("VoiceOff").gameObject;
        SearchModule = transform.GetComponentByChildName<SearchModule>("SearchModule");
        Empty = transform.FindChildByName("Empty")?.gameObject;
        canvasGroup = transform.GetComponent<CanvasGroup>();
        Background = transform.GetComponentByChildName<RectTransform>("BackGround");

        Collapse?.onClick.AddListener(() => SendMsg(new MsgBase((ushort)RoomChannelEvent.LiveRoomMemberModuleClose)));

        MemberCount.text = GlobalInfo.roomInfo.MemberCount.ToString();

        if (GlobalInfo.roomInfo.RoomType == (int)RoomType.Synergia)
        {
            MainText.SetActive(false);
            OpText.SetActive(false);
        }

        if (GlobalInfo.IsHomeowner())
        {
            KickText.gameObject.SetActive(true);
#if UNITY_STANDALONE
            MemberScrollView.GetComponent<RectTransform>().offsetMin = new Vector2(30, 81);
#endif
            AllVoiceOffBtn.transform.parent.gameObject.SetActive(true);
            AllVoiceOffBtn.onClick.AddListener(() =>
            {
                NetworkManager.Instance.SilentAllMember(!GlobalInfo.isAllTalk);
            });
        }

        SearchModule.OnSearch.AddListener((value) => UpdateMemberList(NetworkManager.Instance.GetRoomMemberList()));
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)RoomChannelEvent.UpdateMemberList:
                UpdateMemberList(NetworkManager.Instance.GetRoomMemberList());

                //主画面离线时,提示无操作权成员
                if (GlobalInfo.roomInfo.RoomType == (int)RoomType.Live && !GlobalInfo.IsOperator())
                {
                    if (!NetworkManager.Instance.IsUserOnline(GlobalInfo.mainScreenId))
                    {
                        if (!mainScreenOffline && !NetworkManager.Instance.IsLeavingRoom)
                        {
                            mainScreenOffline = true;
                            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
                            popupDic.Add("知道了", new PopupButtonData(null, true));
                            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "主画面已掉线，请耐心等待", popupDic));
                        }
                    }
                    else
                    {
                        mainScreenOffline = false;
                    }
                }
                break;
            case (ushort)RoomChannelEvent.OtherJoin:
                MsgIntString joinedMember = (MsgIntString)msg;
                OnOtherJoin(joinedMember.arg1, joinedMember.arg2);
                break;
            case (ushort)RoomChannelEvent.OtherLeave:
                MsgIntString leavedMember = (MsgIntString)msg;
                OnOtherLeave(leavedMember.arg1, leavedMember.arg2);
                break;
            case (ushort)RoomChannelEvent.StartMainScreen:
                Log.Debug("初始设置房主为主画面：" + GlobalInfo.mainScreenId + "--" + GlobalInfo.roomInfo.creatorId);
                NetworkManager.Instance.SetUserMainView(GlobalInfo.roomInfo.creatorId, true);
                break;
            case (ushort)RoomChannelEvent.UpdateMainScreen:
                MsgIntBool screenMsg = (MsgIntBool)msg;
                OnMainScreenUpdate(screenMsg.arg1, screenMsg.arg2);
                break;
            case (ushort)RoomChannelEvent.UpdateControl:
                MsgIntBool controlMsg = (MsgIntBool)msg;
                OnControllerUpdate(controlMsg.arg1, controlMsg.arg2);
                break;
            case (ushort)RoomChannelEvent.TalkState:
                UpdateAllVoiceOffBtn(((MsgBoolBool)msg).arg2);
                break;
            case (ushort)MediaChannelEvent.MicOnAir:
                int userId = ((MsgInt)msg).arg;
                if (!NetworkManager.Instance.IsUserChat(userId))
                    return;

                if (!allMemberMicState.ContainsKey(userId))
                {
                    Transform memberTrans = MemberScrollView.content.FindChildByName(userId.ToString());
                    if (memberTrans)
                    {
                        allMemberMicState.Add(userId, memberTrans.GetComponentByChildName<Image>("OnAir"));
                    }
                }

                if (allMemberMicState.ContainsKey(userId))
                {
                    allMemberMicState[userId].DOFade(1f, 0f);
                    allMemberMicState[userId].DOFade(0f, 1f);
                }
                break;
            case (ushort)RoomChannelEvent.LiveRoomMemberModuleShow:
                OpenModule();
                break;
            case (ushort)RoomChannelEvent.LiveRoomMemberModuleClose:
                UIManager.Instance.HideModuleUI<LiveRoomMemberModule>(ParentPanel);
                break;
        }
    }

    /// <summary>
    /// 新成员加入房间回调
    /// </summary>
    /// <param name="newJoinedId"></param>
    /// <param name="newJoinedName"></param>
    private void OnOtherJoin(int newJoinedId, string newJoinedName)
    {
        Log.Debug($"[协同调试] OnOtherJoin被调用 | newJoinedId:{newJoinedId} | newJoinedName:{newJoinedName} | 当前用户ID:{GlobalInfo.account?.id} | IsHomeowner:{GlobalInfo.IsHomeowner()} | RoomType:{GlobalInfo.roomInfo?.RoomType}");

        if (newJoinedId == GlobalInfo.roomInfo.creatorId)
        {
            if (!GlobalInfo.IsHomeowner())
                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("房主加入房间"));
        }
        else
        {
            if (GlobalInfo.account.id == newJoinedId)
            {
                Log.Debug($"[协同调试] 成员断线重连 | IsOperator:{GlobalInfo.IsOperator()}");
                // Operator 重连的同步由 RoomChannelAgent.UpdateRoomMembers 触发
                // 非 Operator 重连需打开遮罩
                if (!GlobalInfo.IsOperator())
                {
                    SendMsg(new MsgBase((ushort)CoursePanelEvent.OpenMask));
                    NetworkManager.Instance.EnableLocalVideo(false);
                }
            }
            else
            {
                if (GlobalInfo.IsHomeowner())
                {
                    //协同房间，房主赋予新加入房间的成员操作权限(分配颜色)
                    //todo 有成员加入房间时，房主异常离线的情况?
                    if (GlobalInfo.roomInfo.RoomType == (int)RoomType.Synergia)
                    {
                        Log.Debug($"[协同调试] 房主给新成员分配操作权限 | newJoinedId:{newJoinedId} | newJoinedName:{newJoinedName}");
                        NetworkManager.Instance.SetUserControl(newJoinedId, true);
                    }
                    else
                    {
                        Log.Debug($"[协同调试] 非协同房间，不分配权限 | RoomType:{GlobalInfo.roomInfo.RoomType}");
                    }
                    UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo(string.Format("{0}加入房间", newJoinedName)));
                }
            }
        }
    }

    /// <summary>
    /// 成员离开房间回调
    /// </summary>
    /// <param name="leavedUserId"></param>
    /// <param name="leavedUserName"></param>
    private void OnOtherLeave(int leavedUserId, string leavedUserName)
    {
        if (leavedUserId == GlobalInfo.roomInfo.creatorId)
        {
            //TODO 异常掉线
            UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("房主退出房间"));
        }
        else
        {
            if (GlobalInfo.IsHomeowner())
            {
                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo(string.Format("{0}退出房间", leavedUserName)));
            }
        }
        RemoveMember(leavedUserId);
    }

    /// <summary>
    /// 主画面变更回调
    /// </summary>
    /// <param name="id"></param>
    /// <param name="isControl"></param>
    private void OnMainScreenUpdate(int id, bool isMainScreen)
    {
        SendMsg(new MsgBase((ushort)MediaChannelEvent.RemoveView));

        if (GlobalInfo.account.id == id)
        {
            if (isMainScreen)
            {
                SendMsg(new MsgBase((ushort)CoursePanelEvent.CloseMask));

                // 分享主屏
                // 暂时处理方法：GameViewEncoder初始FastMode为true会导致Crash, Enable后等待一段时间再设置为true
                NetworkManager.Instance.EnableLocalVideo(true);
                if (!GlobalInfo.IsHomeowner())
                {
                    Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
                    popupDic.Add("知道了", new PopupButtonData(null, true));
                    UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "你已被设置为主画面", popupDic));
                }
            }
            else
            {
                SendMsg(new MsgBase((ushort)CoursePanelEvent.OpenMask));
                //停止分享主屏
                NetworkManager.Instance.EnableLocalVideo(false);

                if (!GlobalInfo.IsHomeowner() && GlobalInfo.IsOperator())
                {
                    Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
                    popupDic.Add("知道了", new PopupButtonData(null, true));
                    UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "主画面已解除", popupDic));
                }
            }
        }
        else
        {
            if (isMainScreen)
            {
                SendMsg(new MsgBase((ushort)CoursePanelEvent.OpenMask));
                if (!GlobalInfo.IsHomeowner() && !GlobalInfo.IsOperator())
                {
                    UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp,
                        new ToastPanelInfo($"{NetworkManager.Instance.GetUserName(id)}被设置为主画面"));
                }
            }
        }
    }

    /// <summary>
    /// 成员权限变更回调
    /// </summary>
    /// <param name="id"></param>
    /// <param name="isControl"></param>
    private void OnControllerUpdate(int id, bool isControl)
    {
        Log.Debug("成员权限变更回调:" + id + "--" + isControl);
        Log.Debug($"[协同调试] OnControllerUpdate | userId:{id} | isControl:{isControl} | 当前用户ID:{GlobalInfo.account?.id}");

        if (GlobalInfo.account.id == id)
        {
            if (isControl)
            {
                Log.Debug($"[协同调试] 当前用户获得操作权限 | RoomType:{GlobalInfo.roomInfo?.RoomType}");

                // YG: 改成手动加载百科
                //GlobalInfo.BaikeLoading = true;
                //ToolManager.SendBroadcastMsg(new MsgInt((ushort)BaikeSelectModuleEvent.BaikeSelect, GlobalInfo.currentWiki.id), true);

                // YG: 改成等待百科加载完成
                //StartCoroutine(WaitForBaikeComplete(() => {
                NetworkManager.Instance.TrySyncCachedVersion();
                SendMsg(new MsgBase((ushort)CoursePanelEvent.CloseMask));
                SendMsg(new MsgBase((ushort)MediaChannelEvent.RemoveView));

                if (GlobalInfo.roomInfo.RoomType == (int)RoomType.Synergia)
                    return;

                if (!GlobalInfo.IsHomeowner() && !GlobalInfo.IsMainScreen())//避免重复提示
                {
                    Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
                    popupDic.Add("知道了", new PopupButtonData(null, true));
                    UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "你已获得操作权限", popupDic));
                }
                //}));
            }
            else
            {
                SendMsg(new MsgBase((ushort)CoursePanelEvent.OpenMask));
                SendMsg(new MsgBase((ushort)StateEvent.PreSyncVersion));
                GlobalInfo.version = 0;

                if (!GlobalInfo.IsHomeowner())
                {
                    Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
                    popupDic.Add("知道了", new PopupButtonData(null, true));
                    UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "操作权限已被收回", popupDic));
                }
            }
        }
    }

    /// <summary>
    /// 更新全员闭麦按钮显示
    /// </summary>
    private void UpdateAllVoiceOffBtn(bool showToast)
    {
        VoiceOnIcon.SetActive(GlobalInfo.isAllTalk);
        VoiceOffIcon.SetActive(!GlobalInfo.isAllTalk);

        if (showToast)
        {
            if (GlobalInfo.IsHomeowner())
            {
                UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, Background.transform,
                    new LocalTipModule.ModuleData(GlobalInfo.isAllTalk ? "已解除全员禁言" : "已开启全员禁言"));
            }
            else
            {
                UIManager.Instance.OpenModuleUI<ToastPanel>(SynCoursePanel, UILevel.PopUp,
                    new ToastPanelInfo(GlobalInfo.isAllTalk ? "已解除全员禁言" : "已开启全员禁言"));
            }
        }
    }

    /// <summary>
    /// 用户列表刷新回调
    /// </summary>
    public void UpdateMemberList(List<Member> members)
    {
        MemberCount.text = $"({members.Count}人)";
        allMemberItem.Clear();

        MemberScrollView.content.UpdateItemsView(FilterMemberList(members), i => i.Id.ToString(), SetNewMember, SetOldMember);
    }

    private List<Member> FilterMemberList(List<Member> members)
    {
        string searchKeyword = SearchModule.Text.Replace(" ", "");

        if (!string.IsNullOrEmpty(searchKeyword))
        {
            members = members.Select(item => item).Where(member => member.Nickname.Contains(searchKeyword)).ToList();
        }

        Empty.SetActive(members.Count == 0);

        return members;
    }

    private void SetNewMember(Transform tf, Member info)
    {
        SetAccountInfo(tf, info, true);
        switch (GlobalInfo.roomInfo.RoomType)
        {
            //直播
            case (int)RoomType.Live:
                RegistUIEvent_Live(tf, info);
                UpdateUIState_Live(tf, info);
                break;
            //协同
            case (int)RoomType.Synergia:
                RegistUIEvent_Syn(tf, info);
                UpdateUIState_Syn(tf, info);
                break;
            default:
                Debug.LogError("未知房间类型");
                RegistUIEvent_Live(tf, info);
                UpdateUIState_Live(tf, info);
                break;
        }
    }

    private void SetOldMember(Transform tf, Member info)
    {
        SetAccountInfo(tf, info);
        switch (GlobalInfo.roomInfo.RoomType)
        {
            //直播
            case (int)RoomType.Live:
                UpdateUIState_Live(tf, info);
                break;
            //协同
            case (int)RoomType.Synergia:
                UpdateUIState_Syn(tf, info);
                break;
        }
    }

    /// <summary>
    /// 设置成员信息
    /// </summary>
    /// <param name="tf"></param>
    /// <param name="info"></param>
    /// <param name="isNewItem"></param>
    private void SetAccountInfo(Transform tf, Member info, bool isNew = false)
    {
        if (!allMemberItem.ContainsKey(info.Id))
            allMemberItem.Add(info.Id, tf.gameObject);
        else
            allMemberItem[info.Id] = tf.gameObject;

        if (info.Id == GlobalInfo.account.id)
            tf.SetSiblingIndex(1);
        else if (info.Id == GlobalInfo.roomInfo.creatorId)
            tf.SetSiblingIndex(2);

        string personName = info.Nickname;
        if (isNew)
        {
            tf.FindChildByName("db").gameObject.SetActive(info.Id == GlobalInfo.account.id);

            if (info.Id == GlobalInfo.account.id)
            {
                personName += string.Format("({0})", "我");
            }
            else
            {
                if (info.Id == GlobalInfo.roomInfo.creatorId)
                {
                    personName += string.Format("({0})", "创建人");
                }
            }
            tf.GetComponentByChildName<Text>("PersonName").text = personName;
        }

        string deviceType = NetworkManager.Instance.GetUserDevice(info.Id);
        Transform device = tf.FindChildByName("Device");
        foreach (Transform icon in device)
        {
            if (icon.name.Equals(deviceType))
            {
                if (GlobalInfo.roomInfo.RoomType == (int)RoomType.Synergia)
                {
                    icon.GetComponent<Image>().color = NetworkManager.Instance.GetPlayerColor(info.Id);
                }
                else
                {
                    if (string.IsNullOrEmpty(info.ColorNumber))
                    {
                        if (info.Id == GlobalInfo.roomInfo.creatorId && GlobalInfo.IsUserOperator(info.Id))
                            icon.GetComponent<Image>().color = NetworkManager.Instance.GetPlayerColor(info.Id);
                        else
                            icon.GetComponent<Image>().color = defaultIconColor;
                    }
                    else
                        icon.GetComponent<Image>().color = info.ColorNumber.HexToColor();
                }
                icon.gameObject.SetActive(true);
            }
            else
            {
                icon.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 直播房间注册UI事件
    /// </summary>
    /// <param name="tf"></param>
    /// <param name="info"></param>
    private void RegistUIEvent_Live(Transform tf, Member info)
    {
        Toggle mvToggle = tf.GetComponentByChildName<Toggle>("MainViewTog");
        //Toggle opConToggle = tf.GetComponentByChildName<Toggle>("OperationControlTog");
        Button voiceToggle = tf.GetComponentByChildName<Button>("VoiceControlTog");
        Button kickBtn = tf.GetComponentByChildName<Button>("KickBtn");

        //注册主画面开关按钮,仅房主显示
        if (GlobalInfo.IsHomeowner())
        {
            mvToggle.onValueChanged.RemoveAllListeners();
            mvToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    NetworkManager.Instance.SetUserMainView(GlobalInfo.mainScreenId, false);

                    //将除房主的用户操作权收回
                    int operatorId = -1;
                    if (GlobalInfo.controllerIds.Count > 1)
                        operatorId = GlobalInfo.controllerIds.Except(new List<int> { GlobalInfo.roomInfo.creatorId }).First();
                    if (operatorId != -1 && operatorId != info.Id)
                    {
                        NetworkManager.Instance.SetUserControl(operatorId, false);
                    }

                    ////记录用户获得主屏权限时是否已有操作权限，若无，则在收回主屏权限时将操作权一并收回 
                    //if (info.Id != GlobalInfo.roomInfo.hostId)
                    //    UpdateUserCtrlState(info.Id, GlobalInfo.IsUserOperator(info.Id));

                    //if (TakeMainWithControl(GlobalInfo.mainScreenId))
                    //    NetworkManager.Instance.SetUserControl(GlobalInfo.mainScreenId, false);
                    //else
                    //    NetworkManager.Instance.SetUserMainView(GlobalInfo.mainScreenId, false);
                    NetworkManager.Instance.SetUserMainView(info.Id, true);
                }
                else
                {
                    if (info.Id == GlobalInfo.mainScreenId)
                    {
                        //if (TakeMainWithControl(info.Id))
                        //{
                        //    NetworkManager.Instance.ReleasePlayerColor(info.Id);
                        //    NetworkManager.Instance.SetUserControl(info.Id, false);
                        //}
                        //else
                        //    NetworkManager.Instance.SetUserMainView(info.Id, false);

                        NetworkManager.Instance.SetUserMainView(info.Id, false);
                        //主画面回到房主
                        NetworkManager.Instance.SetUserMainView(GlobalInfo.roomInfo.creatorId, true);
                    }
                }
            });
        }

        //注册权限控制按钮,仅房主
        if (GlobalInfo.IsHomeowner())
        {
            //opConToggle.onValueChanged.RemoveAllListeners();
            //opConToggle.interactable = false;
            //TODO 2026.1.21 多人权限控制有问题，暂时去掉直播多人权限控制
            //opConToggle.onValueChanged.AddListener((isOn) =>
            //{
            //    if (isOn)
            //    {
            //        //将除房主以外的成员主画面和操作权收回
            //        if (GlobalInfo.controllerIds.Count > 1)
            //        {
            //            NetworkManager.Instance.SetUserControl(GlobalInfo.controllerIds.Except(new List<int> { GlobalInfo.roomInfo.creatorId }).First(), false);
            //        }
            //        NetworkManager.Instance.SetUserControl(info.Id, true);
            //    }
            //    else
            //    {
            //        NetworkManager.Instance.ReleasePlayerColor(info.Id);
            //        NetworkManager.Instance.SetUserControl(info.Id, false);
            //        //被收回权限的成员是当前主画面时，主画面回到房主
            //        if (info.Id == GlobalInfo.mainScreenId)
            //        {
            //            NetworkManager.Instance.SetUserMainView(GlobalInfo.roomInfo.creatorId, true);
            //        }
            //    }
            //});
        }

        //注册语音控制按钮
        voiceToggle.onClick.RemoveAllListeners();
        if (info.Id == GlobalInfo.account.id)
        {
            voiceToggle.onClick.AddListener(() =>
            {
                NetworkManager.Instance.SwitchUserChat(info.Id);
            });
        }
        else
        {
            if (GlobalInfo.IsHomeowner())
            {
                voiceToggle.onClick.AddListener(() => NetworkManager.Instance.SwitchUserTalk(info.Id));
            }
        }

        //注册踢人按钮,仅房主显示非本人的踢人按钮
        if (GlobalInfo.IsHomeowner())
        {
            kickBtn.onClick.RemoveAllListeners();
            kickBtn.onClick.AddListener(() =>
            {
                Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
                popupDic.Add("取消", new PopupButtonData(null));
                popupDic.Add("移出", new PopupButtonData(() =>
                {
                    NetworkManager.Instance.KickOutUser(info.Id);
                    //todo
                    //UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData($"您已将{info.Nickname}移出房间"));
                }, true));
                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", $"将<color=#F6533F>{info.Nickname}</color>移出房间?", popupDic));
            });
        }
    }

    /// <summary>
    /// 协同房间注册UI事件
    /// </summary>
    /// <param name="tf"></param>
    /// <param name="info"></param>
    private void RegistUIEvent_Syn(Transform tf, Member info)
    {
        Button voiceToggle = tf.GetComponentByChildName<Button>("VoiceControlTog");
        Button kickBtn = tf.GetComponentByChildName<Button>("KickBtn");

        //注册语音控制按钮
        voiceToggle.onClick.RemoveAllListeners();
        if (info.Id == GlobalInfo.account.id)
        {
            voiceToggle.onClick.AddListener(() =>
            {
                NetworkManager.Instance.SwitchUserChat(info.Id);
            });
        }
        else
        {
            if (GlobalInfo.IsHomeowner())
            {
                voiceToggle.onClick.AddListener(() => NetworkManager.Instance.SwitchUserTalk(info.Id));
            }
        }

        //注册踢人按钮,仅房主显示非本人的踢人按钮
        if (GlobalInfo.IsHomeowner())
        {
            kickBtn.onClick.RemoveAllListeners();
            kickBtn.onClick.AddListener(() =>
            {
                Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
                popupDic.Add("取消", new PopupButtonData(null));
                popupDic.Add("移出", new PopupButtonData(() =>
                {
                    NetworkManager.Instance.KickOutUser(info.Id);
                    //todo
                    //UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData($"您已将{info.Nickname}移出房间"));
                }, true));
                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", $"将<color=#F6533F>{info.Nickname}</color>移出房间?", popupDic));
            });
        }
    }

    #region 主画面、权限关联

    /// <summary>
    /// 是否需要同时收回当前主屏用户的操作权限
    /// </summary>
    private Dictionary<int, bool> UserCtrlState;

    /// <summary>
    /// 更新用户操作权状态
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="hasCtrl"></param>
    private void UpdateUserCtrlState(int userId, bool hasCtrl)
    {
        if (UserCtrlState == null)
            UserCtrlState = new Dictionary<int, bool>(3);

        if (UserCtrlState.ContainsKey(userId))
            UserCtrlState[userId] = hasCtrl;
        else
            UserCtrlState.Add(userId, hasCtrl);
    }

    /// <summary>
    /// 是否需要一并收回操作权
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>

    private bool TakeMainWithControl(int userId)
    {
        bool take = false;
        if (UserCtrlState == null)
            return take;

        if (UserCtrlState.ContainsKey(userId))
            take = !UserCtrlState[userId];
        UserCtrlState.Remove(userId);
        return take;
    }

    #endregion
    /// <summary>
    /// 直播房间更新UI状态
    /// </summary>
    /// <param name="tf"></param>
    /// <param name="info"></param>
    private void UpdateUIState_Live(Transform tf, Member info)
    {
        Toggle mvToggle = tf.GetComponentByChildName<Toggle>("MainViewTog");
        //Toggle opConToggle = tf.GetComponentByChildName<Toggle>("OperationControlTog");
        Button voiceToggle = tf.GetComponentByChildName<Button>("VoiceControlTog");
        Button kickBtn = tf.GetComponentByChildName<Button>("KickBtn");

        //主画面显示
        mvToggle.interactable = false;//GlobalInfo.IsHomeowner();临时修改，未来直播模式还是要能切
        if (info.Id == GlobalInfo.roomInfo.creatorId && info.Id == GlobalInfo.mainScreenId)
            mvToggle.interactable = false;
        mvToggle.SetIsOnWithoutNotify(info.Id == GlobalInfo.mainScreenId);//设置主画面toggle


        // LUO: 罗老师修复bug
        //opConToggle.interactable = false;
        ////权限显示
        //if (info.Id == GlobalInfo.roomInfo.creatorId)
        //{
        //    //房主移交主屏权限后，可选择取消自己的操作权限
        //    //opConToggle.interactable = GlobalInfo.IsHomeowner() && GlobalInfo.mainScreenId != info.Id;// TODO 2026.1.21 多人权限控制有问题，暂时去掉直播多人权限控制
        //    ////房主占一位操作权
        //    //opConToggle.interactable = false;
        //    opConToggle.SetIsOnWithoutNotify(GlobalInfo.controllerIds.Contains(info.Id));
        //}
        //else
        //{
        //    opConToggle.gameObject.SetActive(true);
        //    opConToggle.SetIsOnWithoutNotify(GlobalInfo.controllerIds.Contains(info.Id));
        //    //opConToggle.interactable = GlobalInfo.IsHomeowner();    //TODO 2026.1.21 多人权限控制有问题，暂时去掉直播多人权限控制
        //}

        //语音控制显示
        if (info.Id != GlobalInfo.roomInfo.creatorId)
            ButtonImageChange(voiceToggle, !info.IsTalk, info.IsChat);
        else
            ButtonImageChange(voiceToggle, false, info.IsChat);

        if (!GlobalInfo.IsHomeowner() && info.Id != GlobalInfo.account.id)
            voiceToggle.interactable = false;

        //踢人显示
        if (GlobalInfo.IsHomeowner())
        {
            CanvasGroup kickCanvas = kickBtn.GetComponent<CanvasGroup>();
            bool canKick = info.Id != GlobalInfo.account.id;
            kickCanvas.alpha = canKick ? 1 : 0;
            kickCanvas.interactable = canKick;
        }
        else
        {
            kickBtn.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 协同房间更新UI状态
    /// </summary>
    /// <param name="tf"></param>
    /// <param name="info"></param>
    private void UpdateUIState_Syn(Transform tf, Member info)
    {
        Toggle mvToggle = tf.GetComponentByChildName<Toggle>("MainViewTog");
        Toggle opConToggle = tf.GetComponentByChildName<Toggle>("OperationControlTog");
        Button voiceToggle = tf.GetComponentByChildName<Button>("VoiceControlTog");
        Button kickBtn = tf.GetComponentByChildName<Button>("KickBtn");

        mvToggle.gameObject.SetActive(false);
        opConToggle.gameObject.SetActive(false);

        //语音控制显示
        if (info.Id != GlobalInfo.roomInfo.creatorId)
            ButtonImageChange(voiceToggle, !info.IsTalk, info.IsChat);
        else
            ButtonImageChange(voiceToggle, false, info.IsChat);

        if (!GlobalInfo.IsHomeowner() && info.Id != GlobalInfo.account.id)
            voiceToggle.interactable = false;

        //踢人显示
        if (GlobalInfo.IsHomeowner())
        {
            CanvasGroup kickCanvas = kickBtn.GetComponent<CanvasGroup>();
            bool canKick = info.Id != GlobalInfo.account.id;
            kickCanvas.alpha = canKick ? 1 : 0;
            kickCanvas.interactable = canKick;
        }
        else
        {
            kickBtn.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 语音按钮替换状态图片
    /// </summary>
    /// <param name="button"></param>
    /// <param name="isShut">是否禁言</param>
    /// <param name="isChat">是否开启麦克风</param>
    private void ButtonImageChange(Button button, bool isShut, bool isChat)
    {
        string buttonState = "CloseToSpeak";
        if (isShut)
            buttonState = "BannedToPost";
        else if (isChat)
            buttonState = "OpenToSpeak";

        button.image.sprite = button.GetComponentByChildName<Image>(buttonState).sprite;
        switch (buttonState)
        {
            case "BannedToPost":
                button.interactable = GlobalInfo.IsHomeowner();
                break;
            case "OpenToSpeak":
            case "CloseToSpeak":
                button.interactable = true;
                //button.GetComponent<CanvasGroup>().alpha = 1;
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 移除成员
    /// </summary>
    /// <param name="id"></param>
    private void RemoveMember(int id)
    {
        if (allMemberItem.ContainsKey(id))
        {
            if (allMemberItem[id] != null)
                Destroy(allMemberItem[id]);
            allMemberItem.Remove(id);

            if (allMemberMicState.ContainsKey(id))
                allMemberMicState.Remove(id);
        }
    }

    #region 动效
    protected override float joinAnimePlayTime => 0.3f;

    protected override float exitAnimePlayTime => 0.2f;

    public void OpenModule()
    {
        SendMsg(new MsgBase((ushort)UIAnimEvent.ShowAnimMask));

        canvasGroup.alpha = 0;

#if UNITY_STANDALONE
        Background.localScale = Vector3.one * 0.2f;
        Background.DOScale(Vector3.one, JoinAnimePlayTime);
#else
        Background.anchoredPosition = new Vector2(Background.sizeDelta.x, Background.anchoredPosition.y);
        Background.DOAnchorPos3DX(0f, JoinAnimePlayTime);
#endif
        DOTween.To(() => canvasGroup.alpha, (value) => canvasGroup.alpha = value, 1f, JoinAnimePlayTime).OnComplete(() =>
        {
            SendMsg(new MsgBase((ushort)UIAnimEvent.HideAnimMask));
            canvasGroup.blocksRaycasts = true;
            isOpen = true;
        });
    }

    /// <summary>
    /// 退场动画
    /// </summary>
    /// <param name="callback">回调</param>
    public override void ExitAnim(UnityAction callback)
    {
        if (isOpen)
        {
            canvasGroup.blocksRaycasts = false;
#if UNITY_STANDALONE
            ExitSequence.Join(Background.DOScale(Vector3.one * 0.2f, ExitAnimePlayTime));
#else
            ExitSequence.Join(DOTween.To(() => new Vector2(0f, Background.anchoredPosition.y), (value) => Background.anchoredPosition = value, new Vector2(Background.sizeDelta.x, Background.anchoredPosition.y), ExitAnimePlayTime));
#endif
            ExitSequence.Join(DOTween.To(() => canvasGroup.alpha, (value) => canvasGroup.alpha = value, 0f, ExitAnimePlayTime).OnComplete(() =>
            {
                isOpen = false;
            }));
        }
        base.ExitAnim(callback);
    }
    #endregion
}