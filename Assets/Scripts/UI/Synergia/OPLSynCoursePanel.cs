using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;
using static UnityFramework.Runtime.ServiceRequestData;

/// <summary>
/// 房间
/// </summary>
public class OPLSynCoursePanel : OPLCoursePanel
{
    private GameObject ModelCtrl;

    private CanvasGroup TopBtnsCanvas;
    private CanvasGroup MidBtnsCanvas;

    /// <summary>
    /// 直播答题开关
    /// </summary>
    private Toggle JudgeTog;

    /// <summary>
    /// 编辑房间信息开关
    /// </summary>
    private Toggle RoomInfoTog;
    /// <summary>
    /// 成员列表开关
    /// </summary>
    private Toggle MemberTog;

    /// <summary>
    /// 成员列表模块
    /// </summary>
    private LiveRoomMemberModule LiveRoomMemberModule;

    private RectTransform MainScreenView;
    private RawImage MainViewRawImage;
    /// <summary>
    /// 视频流解码组件,接收显示主屏数据
    /// </summary>
    private GameViewDecoder GameViewDecoder;

    /// <summary>
    /// 画笔模块父物体
    /// </summary>
    private Transform PaintMenuPoint;
    /// <summary>
    /// 房间信息模块父物体
    /// </summary>
    private Transform LiveSettingMenuPoint;
    /// <summary>
    /// 直播答题模块父物体
    /// </summary>
    private Transform JudgeOnlineMenuPoint;
    /// <summary>
    /// 绘图模块
    /// </summary>
    private SynPaintModule SynPaintModule;

    /// <summary>
    /// 是否退出登录
    /// </summary>
    private bool logout;

    private int baikeIndex = -1;
    private string videoPacketUrl = string.Empty;

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        AddMsg(new ushort[]
        {
            (ushort)ResourcesPanelEvent.SelectCourse,
            (ushort)CoursePanelEvent.OpenMask,
            (ushort)CoursePanelEvent.CloseMask,
            (ushort)CoursePanelEvent.SwitchResource,
            (ushort)PaintEvent.SyncPaint,
            (ushort)MediaChannelEvent.MicError,
            (ushort)MediaChannelEvent.AddView,
            (ushort)MediaChannelEvent.RemoveView,
            (ushort)NetworkChannelEvent.HeartMiss,
            (ushort)RoomChannelEvent.UpdateMemberList,
            (ushort)RoomChannelEvent.LeaveRoom,
            (ushort)RoomChannelEvent.OtherLeave,
            (ushort)RoomChannelEvent.UpdateControl,
            (ushort)RoomChannelEvent.LiveRoomSettingModuleClose,
            (ushort)RoomChannelEvent.RoomClose,
            (ushort)StateEvent.PreSyncVersion,
            (ushort)JudgeOnlineEvent.Start,
            (ushort)JudgeOnlineEvent.End
        });

        GlobalInfo.waitExam = true;
        GlobalInfo.canEditUserInfo = false;
    }

    protected override void InitVariables()
    {
        base.InitVariables();

        ModelCtrl = transform.FindChildByName("ModelCtrl").gameObject;
        TopBtnsCanvas = transform.GetComponentByChildName<CanvasGroup>("TopBtns");
        MidBtnsCanvas = transform.GetComponentByChildName<CanvasGroup>("MidBtns");
        RoomInfoTog = transform.GetComponentByChildName<Toggle>("RoomInfoTog");
        MemberTog = transform.GetComponentByChildName<Toggle>("MemberTog");

        AddHoverHint(RoomInfoTog, "房间信息");
        AddHoverHint(MemberTog, "成员列表");

#if UNITY_STANDALONE
        JudgeTog = transform.GetComponentByChildName<Toggle>("JudgeTog");
        JudgeTog.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                int choiceCount = 0;
                bool multipleChoice = false;
                List<int> correctIndex = new List<int>();

                Exercise exercise = (GlobalInfo.currentWiki as EncyclopediaExercise).data.exercise;
                switch (exercise.type)
                {
                    //选择题
                    case 1:
                        ExerciseContent ec = JsonTool.DeSerializable<ExerciseContent>(exercise.content);
                        choiceCount = ec.answers.Count;
                        multipleChoice = ec.answers.FindAll(a => a.right == true).Count > 1;
                        for (int i = 0; i < ec.answers.Count; i++)
                        {
                            if (ec.answers[i].right)
                            {
                                correctIndex.Add(i);
                            }
                        }
                        break;
                    //判断题
                    case 2:
                        JudgementExerciseContent jec = JsonTool.DeSerializable<JudgementExerciseContent>(exercise.content);
                        choiceCount = 2;
                        correctIndex.Add(jec.answers ? 0 : 1);
                        break;
                }

                UIManager.Instance.OpenModuleUI<JudgeOnlineResultModule>(this, JudgeOnlineMenuPoint, new JudgeOnlineData(choiceCount, multipleChoice, correctIndex));
                SendMsg(new MsgBase((ushort)JudgeOnlineEvent.Start));
                NetworkManager.Instance.SendFrameMsg(new MsgJudgeOnline((ushort)JudgeOnlineEvent.Start, GlobalInfo.currentWiki.id, choiceCount, multipleChoice));
            }
            else
            {
                UIManager.Instance.CloseModuleUI<JudgeOnlineResultModule>(this);
                SendMsg(new MsgBase((ushort)JudgeOnlineEvent.End));
                NetworkManager.Instance.SendFrameMsg(new MsgBase((ushort)JudgeOnlineEvent.End));
            }
        });
#endif

        RoomInfoTog.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                UIManager.Instance.OpenModuleUI<LiveRoomSettingModule>(this, LiveSettingMenuPoint);
            }
            else
            {
                UIManager.Instance.CloseModuleUI<LiveRoomSettingModule>(this);
            }
        });

        MemberTog.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                var module = UIManager.Instance.OpenModuleUI<LiveRoomMemberModule>(this, SideToggleMenuPoint);
                module.closeDelegate = () => MemberTog.SetIsOnWithoutNotify(false);
                SendMsg(new MsgBase((ushort)RoomChannelEvent.LiveRoomMemberModuleShow));
            }
            else
            {
                SendMsg(new MsgBase((ushort)RoomChannelEvent.LiveRoomMemberModuleClose));
            }
        });

        Paint.onValueChanged.RemoveAllListeners();
        Paint.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                CollapseBtns();
                SynPaintModule = (SynPaintModule)UIManager.Instance.OpenModuleUI<SynPaintModule>(this, PaintMenuPoint);
                SynPaintModule.closeDelegate = () => Paint.SetIsOnWithoutNotify(false);
                UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo("当前为标注模式"));
            }
            else
            {
                //权限用户退出标注模式，清除本人操作
                ToolManager.SendBroadcastMsg(new MsgBase((ushort)PaintEvent.SyncReset), true);
                UIManager.Instance.HideModuleUI<SynPaintModule>(this);
                ExpandBtns();
            }
        });

        QuitBtn.onClick.RemoveAllListeners();
        QuitBtn.onClick.AddListener(() =>
        {
            string hint = "确定要离开房间吗?";
            if (GlobalInfo.IsHomeowner())
            {
                hint = "确定要离开并解散房间吗?";
            }
            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("取消", new PopupButtonData(null));
            popupDic.Add("离开", new PopupButtonData(ExitRoom, true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", hint, popupDic));
        });

        GameViewDecoder = transform.GetComponentByChildName<GameViewDecoder>("MainScreenView");
        GameViewDecoder.OnReceivedFrameOperation.AddListener((msg) =>
        {
            if (string.IsNullOrEmpty(msg))
                return;

            VideoPacket videoPacket = JsonTool.DeSerializable<VideoPacket>(msg);

            if (videoPacket.baikeIndex != baikeIndex)
            {
                ClearBaikeModules();
                ModelManager.Instance.DestroyModels(true);
                ModelManager.Instance.DestroyScripts(true);
#if UNITY_STANDALONE
                if (GlobalInfo.currentWikiList != null)
#endif
                {
                    baikeIndex = videoPacket.baikeIndex;
                    CourseSideBar.SetBaikeIndex(baikeIndex);
                }
            }

            if (string.IsNullOrEmpty(videoPacket.url) || (!string.IsNullOrEmpty(videoPacketUrl) && !videoPacketUrl.Equals(videoPacket.url)))
            {
                UIManager.Instance.CloseAllModuleUI<ShowAudioModule>(this);
                UIManager.Instance.CloseAllModuleUI<ShowVideoModule>(this);
            }

            if (!videoPacketUrl.Equals(videoPacket.url))
            {
                switch (videoPacket.type)
                {
                    case 0:
                        UIManager.Instance.OpenModuleUI<ShowAudioModule>(this, ShowModulePoint, new ShowLinkModuleData() { url = videoPacket.url, docType = FileExtension.MP3 });
                        break;
                    case 1:
                        UIManager.Instance.OpenModuleUI<ShowVideoModule>(this, ShowModulePoint, new ShowLinkModuleData() { url = videoPacket.url, docType = FileExtension.MP4 });
                        break;
                }
            }

            switch (videoPacket.type)
            {
                case 0:
                    MsgBool audioCtrl = new MsgBool((ushort)HyperLinkEvent.AudioCtrl, videoPacket.isPlay);
                    SendMsg(new MsgBrodcastOperate(audioCtrl.msgId, JsonTool.Serializable(audioCtrl)));

                    if (videoPacket.progressValue > 0)
                        SendMsg(new MsgFloat((ushort)HyperLinkEvent.AudioSync, videoPacket.progressValue));
                    break;
                case 1:
                    MsgBool videoCtrl = new MsgBool((ushort)HyperLinkEvent.VideoCtrl, videoPacket.isPlay);
                    SendMsg(new MsgBrodcastOperate(videoCtrl.msgId, JsonTool.Serializable(videoCtrl)));

                    if (videoPacket.progressValue > 0)
                        SendMsg(new MsgFloat((ushort)HyperLinkEvent.VideoSync, videoPacket.progressValue));
                    break;
            }
            videoPacketUrl = videoPacket.url;
        });

        MainScreenView = transform.GetComponentByChildName<RectTransform>("MainScreenView");
        MainViewRawImage = MainScreenView.GetComponentInChildren<RawImage>();
#if UNITY_ANDROID || UNITY_IOS
        Toggle MainViewTog = transform.GetComponentByChildName<Toggle>("Zoom");
        GameObject MinIcon = MainViewTog.transform.FindChildByName("Min").gameObject;
        GameObject MaxIcon = MainViewTog.transform.FindChildByName("Max").gameObject;
        MainViewTog.onValueChanged.AddListener(isOn =>
        {
            MinIcon.SetActive(!isOn);
            MaxIcon.SetActive(isOn);

            if (isOn)
                MainScreenView.offsetMax = Vector2.zero;
            else
                MainScreenView.offsetMax = new Vector2(0, -96f);
        });
#endif
        PaintMenuPoint = transform.FindChildByName("PaintMenuPoint");
        LiveSettingMenuPoint = transform.FindChildByName("LiveSettingMenuPoint");
        JudgeOnlineMenuPoint = transform.FindChildByName("JudgeOnlineMenuPoint");
    }


    protected override void OnPrepareShow(UIData uiData)
    {
        GlobalInfo.currentCourseID = GlobalInfo.roomInfo.CourseId;

        NetworkManager.Instance.IsIMSync = false;
        UIManager.Instance.OpenUI<LoadingPanel>(UILevel.Loading);
        InitData(() =>
        {
            SetTitle(GlobalInfo.currentCourseInfo);
            CourseSideBar.SetBaikePage();

            if (GlobalInfo.IsHomeowner())
            {
                Log.Debug("房主权限开始协同");
                RoomInfoTog.gameObject.SetActive(true);
                Mask(true, true);
                SendMsg(new MsgInt((ushort)ResourcesPanelEvent.SelectCourse, GlobalInfo.currentCourseID));
                //房主默认分享主屏
                //NetworkManager.Instance.EnableLocalVideo(true);
            }
            else
            {
                Log.Debug($"非房主权限开始协同 {GlobalInfo.IsOperator()}");
                Paint.interactable = GlobalInfo.IsOperator();
                switch (GlobalInfo.roomInfo.RoomType)
                {
                    case (int)RoomType.Synergia:
                        Mask(true, true);
                        break;
                    default:
                        break;
                }
            }
            UIManager.Instance.CloseUI<LoadingPanel>();
            GlobalInfo.waitExam = false;
            NetworkManager.Instance.IsIMSync = true;
        });
    }

    protected override void InitData(UnityAction callBack)
    {
        RequestManager.Instance.GetCourse(GlobalInfo.currentCourseID, (course) =>
        {
            GlobalInfo.SaveCourseInfo(course);
            GlobalInfo.currentWikiList = course.encyclopediaList;

            if (GlobalInfo.currentWikiList == null || GlobalInfo.currentWikiList.Count == 0)
            {
                var popupDic = new Dictionary<string, PopupButtonData>();
                {
                    popupDic.Add("确定", new PopupButtonData(null, true));
                    UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "该课程未添加百科", popupDic, null, false));
                }
            }
            callBack.Invoke();
        }, (failureMessage) =>
        {
            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("好的", new PopupButtonData(ExitRoom, true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("错误", "获取课程失败，请重新加入房间", popupDic));
            Log.Error($"获取课程失败！原因为：{failureMessage}");
        });
    }

    protected override void ClearBaikeModules(bool closeKnowledge = true)
    {
        base.ClearBaikeModules(closeKnowledge);
#if UNITY_STANDALONE
        JudgeTog.isOn = false;
#endif
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);

        switch (msg.msgId)
        {

            case (ushort)CoursePanelEvent.OpenMask:
                Mask(GlobalInfo.IsMainScreen(), GlobalInfo.IsOperator());
                break;
            case (ushort)CoursePanelEvent.CloseMask:
                Mask(GlobalInfo.IsMainScreen(), true);
                break;
            case (ushort)ResourcesPanelEvent.SelectCourse:
                OnSelectCourse(((MsgInt)msg).arg);
                break;
            //直播间切换课程
            case (ushort)CoursePanelEvent.SwitchResource:
                try
                {
                    GlobalInfo.currentCourseID = ((MsgBrodcastOperate)msg).GetData<MsgInt>().arg;
                    BaikeSelectModule.selectID = 0;
                    NetworkManager.Instance.IsIMSync = false;
                    InitData(() =>
                    {
                        UIManager.Instance.CloseUI<LoadingPanel>();
                        NetworkManager.Instance.IsIMSync = true;
                        if (GlobalInfo.IsHomeowner())
                        {
                            if (GlobalInfo.currentWikiList != null && GlobalInfo.currentWikiList.Count != 0)
                            {
                                CourseSideBar.SetBaikePage();
                                if (!NetworkManager.Instance.IsIMSyncCachedState && !NetworkManager.Instance.IsIMSyncState)
                                {
                                    Encyclopedia firstPedia = GlobalInfo.currentWikiList[0];
                                    ToolManager.SendBroadcastMsg(new MsgInt((ushort)BaikeSelectModuleEvent.BaikeSelect, firstPedia.id), true);
                                }
                            }
                        }
                    });                
                }
                catch(Exception e)
                {
                    Debug.LogError(e.Message);
                }
                break;
            case (ushort)PaintEvent.SyncPaint:
                //确保初次同步绘图操作的用户开启绘图模块
                int sender = ((MsgBrodcastOperate)msg).senderId;
                if (SynPaintModule != null)
                    return;
                SynPaintModule = (SynPaintModule)UIManager.Instance.OpenModuleUI<SynPaintModule>(this, PaintMenuPoint);
                UIManager.Instance.HideModuleUI<SynPaintModule>(this);
                SynPaintModule.gameObject.SetActive(false);
                SendMsg(msg);
                break;
            #region 通道消息
            case (ushort)NetworkChannelEvent.HeartMiss:
                UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo("网络状况差..."));
                break;
            case (ushort)MediaChannelEvent.MicError:
                UIManager.Instance.OpenModuleUI<ToastPanel>(null, UILevel.PopUp, new ToastPanelInfo("请检查麦克风"));
                break;
            case (ushort)MediaChannelEvent.AddView:
                MsgIntString msgIntString = (MsgIntString)msg;
                int userId = msgIntString.arg1;
                string label = msgIntString.arg2;
                if (userId == GlobalInfo.mainScreenId)
                {
                    GameViewDecoder.label = int.Parse(label);
                    NetworkManager.Instance.AddUserVideo(label, GameViewDecoder);
                    this.WaitTime(0.5f, () => MainViewRawImage.DOFade(1, 0.5f));//.color = Color.white);
                    GameViewDecoder.gameObject.SetActive(true);
                }
                break;
            case (ushort)MediaChannelEvent.RemoveView:
                GameViewDecoder.gameObject.SetActive(false);
                NetworkManager.Instance.ClearUserVideo(false);
                break;
            case (ushort)RoomChannelEvent.UpdateMemberList:
                if (LiveRoomMemberModule == null)
                {
                    LiveRoomMemberModule = (LiveRoomMemberModule)UIManager.Instance.OpenModuleUI<LiveRoomMemberModule>(this, SideToggleMenuPoint);
                    LiveRoomMemberModule.UpdateMemberList(NetworkManager.Instance.GetRoomMemberList());
                }
                break;
            case (ushort)RoomChannelEvent.LiveRoomSettingModuleClose:
                RoomInfoTog.isOn = false;
                break;
            case (ushort)RoomChannelEvent.UpdateControl:
                MsgIntBool msgIntBool = (MsgIntBool)msg;
                if (msgIntBool.arg1 == GlobalInfo.account.id)
                {
                    Paint.interactable = msgIntBool.arg2;
                    Paint.SetIsOnWithoutNotify(false);
                }
                break;
            case (ushort)RoomChannelEvent.LeaveRoom:
                Back();
                string message = ((MsgString)msg).arg;
                if (!string.IsNullOrEmpty(message))
                    UIManager.Instance.OpenModuleUI<ToastPanel>(null, UILevel.PopUp, new ToastPanelInfo(message));
                break;
            case (ushort)RoomChannelEvent.RoomClose:
                Dictionary<string, PopupButtonData> popupDic1 = new Dictionary<string, PopupButtonData>();
                popupDic1.Add("确定", new PopupButtonData(() => NetworkManager.Instance.EnsureLeaveRoom(string.Empty), true));
                UIManager.Instance.OpenUI<PopupPanel_AutoConfirm>(UILevel.PopUp, new UIAutoPopupData("提示", "房主已解散房间", popupDic1, 10, true, () =>
                {
                    NetworkManager.Instance.EnsureLeaveRoom(string.Empty);
                }));
                break;
            case (ushort)StateEvent.PreSyncVersion:
                ClearBaikeModules(true);
                ModelManager.Instance.DestroyModels(true);
                ModelManager.Instance.DestroyScripts(true);
                videoPacketUrl = string.Empty;
                GlobalInfo.CursorLockMode = CursorLockMode.None;
                break;
            #endregion
            #region 直播答题
            //暂时隐藏答题功能
            //#if UNITY_STANDALONE
            //case (ushort)BaikeSelectModuleEvent.BaikeSelect:
            //    int baikeId = ((MsgBrodcastOperate)msg).GetData<MsgInt>().arg;
            //    int pediaType = GlobalInfo.currentWikiList.Find(e => e.id == baikeId).typeId;
            //    JudgeTog.gameObject.SetActive(pediaType == (int)PediaType.Exercise && GlobalInfo.IsHomeowner());
            //    break;
            //#endif
            case (ushort)JudgeOnlineEvent.Start:
                if (GlobalInfo.IsHomeowner())
                    return;
                StartJudge(((MsgBrodcastOperate)msg).GetData<MsgJudgeOnline>());
                break;
            case (ushort)JudgeOnlineEvent.End:
#if UNITY_STANDALONE
                JudgeTog.SetIsOnWithoutNotify(false);
#endif
                EndJudge();
                break;
            case (ushort)RoomChannelEvent.OtherLeave:
                //主画面离开房间，结束答题
                int leavedUser = ((MsgIntString)msg).arg1;
                if (leavedUser == GlobalInfo.roomInfo.creatorId)
                {
                    EndJudge();
                }
                break;
                #endregion
        }
    }

    protected override void ChangeSelectModel(Transform go, int userId)
    {
        base.ChangeSelectModel(go, userId);
        PlayerManager.Instance.ChangeUserSelectState(userId, go != null);
    }

    protected override void DeSelectModel(Transform go, int userId)
    {
        base.DeSelectModel(go, userId);
        PlayerManager.Instance.ChangeUserSelectState(userId, false);
    }

    /// <summary>
    /// 选择课程
    /// </summary>
    /// <param name="courseId"></param>
    private void OnSelectCourse(int courseId)
    {
        if (!GlobalInfo.IsHomeowner())
            return;
        if (NetworkManager.Instance.IsIMSyncState || NetworkManager.Instance.IsIMSyncCachedState)
            return;

        MsgInt msgInt = new MsgInt((ushort)CoursePanelEvent.SwitchResource, courseId);
        MsgBrodcastOperate msgBrodcastOperate = new MsgBrodcastOperate(msgInt.msgId, JsonTool.Serializable(msgInt));
        NetworkManager.Instance.SendIMMsg(msgBrodcastOperate);
    }

    protected override void SetTitle(Course course)
    {
        //Title.EllipsisText($"协同/{GlobalInfo.roomInfo.roomName}/{course?.name}", "...");
        Title.text = course?.name;
    }

    /// <summary>
    /// 退出课程
    /// </summary>
    public void Back()
    {
#if UNITY_ANDROID || UNITY_IOS
        SendMsg(new MsgBase((ushort)ARModuleEvent.ExitCourse));
#endif

        GlobalInfo.currentWiki = null;
        GlobalInfo.currentCourseID = 0;
        BaikeSelectModule.selectID = 0;
        GlobalInfo.roomInfo = null;
        GlobalInfo.controllerIds.Clear();
        GlobalInfo.version = 0;
        UIManager.Instance.CloseUI<OPLSynCoursePanel>();

        if (logout)
            ToolManager.GoToLogin();
        else
            UIManager.Instance.OpenUI<TrainingPanel>();
    }

    public override void Previous()
    {
        base.Previous();
        Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
        popupDic.Add("取消", new PopupButtonData(null));
        popupDic.Add("确定", new PopupButtonData(() =>
        {
#if UNITY_ANDROID || UNITY_IOS
            SendMsg(new MsgBase((ushort)ARModuleEvent.ExitCourse));
#endif
            ExitRoom();
        }, true));
        UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "确定退出该课程?", popupDic));
    }

    /// <summary>
    /// 退出课程
    /// </summary>
    protected override void ExitRoom()
    {
        ModelManager.Instance.DestroySyncComponent();
        NetworkManager.Instance.ReleaseMicrophone();
        NetworkManager.Instance.LeaveRoom();
    }

    public override void GotoLogout()
    {
        Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
        popupDic.Add("取消", new PopupButtonData(null));
        popupDic.Add("退出", new PopupButtonData(() =>
        {
            logout = true;
            ExitRoom();
        }, true));
        UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", GlobalInfo.IsHomeowner() ? "确定要退出登录并解散房间吗?" : "确定要退出登录并离开房间吗?", popupDic));
    }


    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        GlobalInfo.isLive = false;
        UIManager.Instance.CloseUI<PopupPanel>();
        UIManager.Instance.CloseUI<PopupPanel_AutoConfirm>();
        //ModelManager.Instance.ControlGlobalVolume();
        base.Close(uiData, callback);

        GlobalInfo.canEditUserInfo = true;
    }

    /// <summary>
    /// 权限操作遮罩
    /// </summary>
    /// <param name="main">是否有主屏权限</param>
    /// <param name="ctrl">是否有操作权限</param>
    private void Mask(bool main, bool ctrl)
    {
        if(GlobalInfo.roomInfo.RoomType == (int)RoomType.Synergia)
        {
            ModelCtrl.SetActive(false);
            MidBtnsCanvas.interactable = true;
            TopBtnsCanvas.interactable = true;
        }
        else
        {
            ModelCtrl.SetActive(!ctrl);
            if (!main)
                CourseSideBar.ShowBaikeSelectModule(false);
            MidBtnsCanvas.interactable = main;
            TopBtnsCanvas.interactable = ctrl;
        }
    }

    #region 直播答题
    private void StartJudge(MsgJudgeOnline msgJudgeOnline)
    {
        GlobalInfo.isJudgeOnline = true;
        if (GlobalInfo.IsOperator())
        {
            if (msgJudgeOnline.pediaId != GlobalInfo.currentWiki.id)
            {
                //todo
                Log.Error("百科未同步");
            }
            UIManager.Instance.OpenModuleUI<JudgeOnlineModule>(this, JudgeOnlineMenuPoint, new JudgeOnlineData(msgJudgeOnline.choiceCount, msgJudgeOnline.multipleChoice));
        }
        else
        {
            RequestManager.Instance.GetEncyclopedia(msgJudgeOnline.pediaId, (encyclopedia, answer) =>
            {
                GlobalInfo.currentWiki = encyclopedia;
                if (GlobalInfo.currentWiki == null)
                    return;

                switch (GlobalInfo.currentWiki.origin)
                {
                    case (int)PediaOrigin.Project:
                    case (int)PediaOrigin.ABPackage:
                    default:
                        switch (GlobalInfo.currentWiki.typeId)
                        {
                            case (int)PediaType.Exercise:
                                OPLExerciseModule exerciseModule = (OPLExerciseModule)UIManager.Instance.OpenModuleUI<OPLExerciseModule>(this, JudgeOnlineMenuPoint);
                                exerciseModule.GetComponent<CanvasGroup>().interactable = false;
                                MainScreenView.gameObject.SetActive(false);
                                UIManager.Instance.OpenModuleUI<JudgeOnlineModule>(this, JudgeOnlineMenuPoint, new JudgeOnlineData(msgJudgeOnline.choiceCount, msgJudgeOnline.multipleChoice));
                                break;
                            default:
                                break;
                        }
                        break;
                }
            }, (msg) =>
            {
                Log.Error($"获取百科失败！原因为：{msg}");
            });
        }
    }

    private void EndJudge()
    {
        if (!GlobalInfo.IsOperator())
        {
            UIManager.Instance.CloseModuleUI<OPLExerciseModule>(this);
            MainScreenView.gameObject.SetActive(true);
        }
        UIManager.Instance.CloseModuleUI<JudgeOnlineModule>(this);
        GlobalInfo.isJudgeOnline = false;
    }
    #endregion

    #region 动效
    protected override Sequence Join()
    {
        Sequence sequence = DOTween.Sequence();
        sequence.AppendInterval(0.5f);
        sequence.Append(TopNavigation.DOAnchorPos3DY(0, JoinAnimePlayTime));
#if UNITY_STANDALONE
        sequence.Join(SideBar.DOAnchorPos3DX(0, JoinAnimePlayTime));
        sequence.Join(BottomBtns.DOAnchorPos3DX(0, JoinAnimePlayTime));
        sequence.Join(RootCanvasGroup.DOFade(1f, JoinAnimePlayTime));
#else
        sequence.Join(SideBar.DOAnchorPos3DX(100f, JoinAnimePlayTime));
#endif
        return sequence;
    }
    #endregion
}