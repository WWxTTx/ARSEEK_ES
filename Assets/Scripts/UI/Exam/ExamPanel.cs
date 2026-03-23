using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.ServiceRequestData;


/// <summary>
/// 考核房间 房主
/// </summary>
public class ExamPanel : HoverHintPanel
{
    protected override bool CanLogout { get { return true; } }

    public override bool canOpenOption => true;

    protected CanvasGroup RootCanvasGroup;
    /// <summary>
    /// 顶部导航栏
    /// </summary>
    protected RectTransform TopNavigation;
    protected Text Title;
    /// <summary>
    /// 左侧边栏
    /// </summary>
    protected RectTransform SideBar;
    private Toggle RoomInfoTog;
    /// <summary>
    /// 开始考核/结束考核
    /// </summary>
    private Button ExamBtn;
    private GameObject WaitHint;
    /// <summary>
    /// 是否在考核中
    /// </summary>
    private bool inExam;
    /// <summary>
    /// 是否已经结束考核
    /// </summary>
    private bool inStop = false;

    /// <summary>
    /// 当前考核Id
    /// </summary>
    private int activeExamId = -1;

    #region 个人考核显示数量
    private int columnCount = 3;//4
    private int rowCount = 3;
    #endregion

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        GlobalInfo.canEditUserInfo = false;
        GlobalInfo.waitExam = true;
        NetworkManager.Instance.IsIMSync = false;

        AddMsg(
            (ushort)RoomChannelEvent.LiveRoomSettingModuleClose,
            (ushort)ExamPanelEvent.Submit,
            (ushort)ExamPanelEvent.Quit,
            (ushort)ExamPanelEvent.Resume,
            (ushort)RoomChannelEvent.RoomInfo
        );

        RootCanvasGroup = transform.GetComponent<CanvasGroup>();
        RootCanvasGroup.blocksRaycasts = false;
        TopNavigation = transform.GetComponentByChildName<RectTransform>("TopNavigation");
        Title = TopNavigation.transform.GetComponentByChildName<Text>("Title");
        SideBar = transform.GetComponentByChildName<RectTransform>("SideBar");
        WaitHint = transform.FindChildByName("WaitHint").gameObject;

        InitFullScene();
        InitPage(!GlobalInfo.IsGroupMode());
        InitMember();

        RoomInfoTog = transform.GetComponentByChildName<Toggle>("Info");
        RoomInfoTog.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
                UIManager.Instance.OpenModuleUI<LiveRoomSettingModule>(this, transform);
            else
                UIManager.Instance.CloseModuleUI<LiveRoomSettingModule>(this);
        });
        this.GetComponentByChildName<Button>("Set").onClick.AddListener(() => UIManager.Instance.OpenUI<OptionPanel>(UILevel.Fixed));
        this.GetComponentByChildName<Button>("Quit").onClick.AddListener(() =>
        {
            var popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("是", new PopupButtonData(() => EndExamBeforeExitRoom(() => ExitRoom(true)), false));
            popupDic.Add("否", new PopupButtonData(() => ExitRoom(false), true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", inExam ? "考核时间还未结束，退出房间时是否结束考核并解散房间？" : "退出房间时是否解散房间？", popupDic));
        });

        Title.text = GlobalInfo.roomInfo.RoomName;

        ExamBtn = this.GetComponentByChildName<Button>("ExamBtn");
        {
            ExamBtn.onClick.AddListener(() =>
            {
                if (!inExam)
                {
                    if (CanStart())
                    {
                        //创建考核
                        RequestManager.Instance.CreateExamRecord(GlobalInfo.currentCourseID, GlobalInfo.roomInfo.RoomName, GlobalInfo.roomInfo.ExamType == (int)ExamRoomType.Group, examId =>
                        {
                            //获取试卷
                            RequestManager.Instance.GetExamination(examId, examination =>
                            {
                                GlobalInfo.SaveExaminationInfo(examination);
                                GlobalInfo.currentWikiList = examination.encyclopediaList;

                                if (GlobalInfo.currentWikiList == null || GlobalInfo.currentWikiList.Count == 0)
                                {
                                    var popupDic = new Dictionary<string, PopupButtonData>();
                                    {
                                        popupDic.Add("确定", new PopupButtonData(() => ExitRoom(), true));
                                        UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "该考核未添加习题", popupDic, null, false));
                                    }
                                    return;
                                }

                                //按考生人数初始化考生成绩
                                RequestManager.Instance.InitExamRecord(new RequestData.StartExamRecordRequest()
                                {
                                    examineId = examId,
                                    examinee = NetworkManager.Instance.GetRoomMemberList().Where(value => value.Id != GlobalInfo.account.id).Select(value => new RequestData.ExamRecordMember
                                    {
                                        examineeId = value.Id,
                                        examineeNo = value.UserNo,
                                        examineeName = value.Nickname
                                    }).ToList()
                                }, () =>
                                {
                                    //取得考核成绩列表，记录提交情况
                                    ExamUtility.Instance.InitSubmitCache(examId, () =>
                                    {
                                        //修改考核房间状态
                                        NetworkManager.Instance.RoomWorking(GlobalInfo.roomInfo.Uuid, () =>
                                        {
                                            ExamUtility.Instance.SetHostExamCache(GlobalInfo.roomInfo.Uuid, examId);
                                            StartExam(examId);
                                        }, (error) =>
                                        {
                                            Log.Error($"修改考核房间[{GlobalInfo.roomInfo.Uuid}]状态失败：{error}");
                                            ExamUtility.Instance.SetHostExamCache(GlobalInfo.roomInfo.Uuid, examId);
                                            StartExam(examId);
                                        });
                                    }, (error) => OnStartExamFailed());
                                }, (msg) => OnStartExamFailed());
                            }, (error) => OnStartExamFailed());
                        }, (error) => OnStartExamFailed());
                    }
                }
                else
                {
                    var popupDic = new Dictionary<string, PopupButtonData>();
                    popupDic.Add("取消", new PopupButtonData(null, false));
                    popupDic.Add("结束考核", new PopupButtonData(() => StopExam(), true));
                    UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "考核还未结束，确定结束考核？", popupDic));
                }
            });
            ExamBtn.gameObject.SetActive(true);
        }

        CheckLastExam();
        UpdateMemberList(NetworkManager.Instance.GetRoomMemberList());
    }

    /// <summary>
    /// 检查是否存在异常退出的考核
    /// </summary>

    private void CheckLastExam()
    {
        activeExamId = ExamUtility.Instance.GetHostExamCache(GlobalInfo.roomInfo.Uuid);
        if (activeExamId != -1)
        {
            ExamUtility.Instance.InitSubmitCache(activeExamId, () =>
            {
                if (!ExamUtility.Instance.AllSubmit())
                {
                    OnExamStart();
                    RootCanvasGroup.blocksRaycasts = true;
                    GlobalInfo.waitExam = false;
                    NetworkManager.Instance.IsIMSync = true;
                }
                else
                {
                    ResetRoom(() =>
                    {
                        ////清空上轮考核的状态信息
                        //ToolManager.SendBroadcastMsg(new MsgBase((ushort)ExamPanelEvent.Flush));
  
                        //上轮考核已全部结束答题 清除缓存
                        OnExamStop();
                        ExamUtility.Instance.DeleteHostExamCache(GlobalInfo.roomInfo.Uuid);
                        activeExamId = -1;

                        RootCanvasGroup.blocksRaycasts = true;
                        GlobalInfo.waitExam = false;
                        NetworkManager.Instance.IsIMSync = true;
                    });                
                }
            }, (error) =>
            {
                Log.Error($"获取考核[{activeExamId}]成绩列表失败：{error}");
                Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
                popupDic.Add("好的", new PopupButtonData(() => ExitRoom(false), true));
                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("错误", "获取考核失败，请重新加入房间", popupDic));
            });
        }
        else
        {
            RootCanvasGroup.blocksRaycasts = true;
            GlobalInfo.waitExam = false;
            NetworkManager.Instance.IsIMSync = true;
        }
    }

    /// <summary>
    /// 是否可以开启考核
    /// </summary>
    /// <param name="members"></param>
    private bool CanStart()
    {
        if (NetworkManager.Instance.GetRoomMemberList().Count < 1)
        {
            UIManager.Instance.OpenModuleUI<ToastPanel>(null, UILevel.PopUp, new ToastPanelInfo("房间人数不足，无法开始考核!"));
            return false;
        }

        foreach (Transform item in Content)
        {
            if (item.gameObject.activeSelf && item.GetComponentByChildName<Image>("StateColor").color == stateColor[2].BGColor)
            {
                UIManager.Instance.OpenModuleUI<ToastPanel>(null, UILevel.PopUp, new ToastPanelInfo("有已交卷人员未退出，无法开始考核!"));
                return false;
            }
        }

        return true;
    }
    private void OnStartExamFailed()
    {
        var popupDic = new Dictionary<string, PopupButtonData>();
        popupDic.Add("确定", new PopupButtonData(null, true));
        UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "未正常开始考核，请重试", popupDic));
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);

        switch (msg.msgId)
        {
            case (ushort)ExamPanelEvent.Submit:
                MsgBrodcastOperate submitData = msg as MsgBrodcastOperate;
                {
                    if (inExam && submitData.GetData<MsgInt>().arg != activeExamId)
                        return;
                    SetMemberItemState(Content.FindChildByName(submitData.senderId.ToString()), (int)State.Submit);
                    ExamUtility.Instance.UpdateSubmitCache(submitData.senderId);
                    if (ExamUtility.Instance.AllSubmit())
                    {
                        StopExam();
                    }
                }
                break;
            case (ushort)ExamPanelEvent.Quit://todo
                MsgBrodcastOperate quitData = msg as MsgBrodcastOperate;
                {
                    if (inExam && quitData.GetData<MsgInt>().arg != activeExamId)
                        return;
                    SetMemberItemState(Content.FindChildByName(quitData.senderId.ToString()), (int)State.Wait);
                }
                break;
            case (ushort)ExamPanelEvent.Resume:
                if (countdownCoroutine != null)
                {
                    StopCoroutine(countdownCoroutine);
                    countdownCoroutine = null;
                }
                countdownCoroutine = StartCoroutine(Timing(GlobalInfo.ServerTime.AddSeconds(((MsgBrodcastOperate)msg).GetData<MsgInt>().arg)));
                break;
            case (ushort)RoomChannelEvent.LiveRoomSettingModuleClose:
                RoomInfoTog.isOn = false;
                break;
            case (ushort)RoomChannelEvent.RoomInfo:
                Title.text = (msg as MsgBrodcastOperate).GetData<MsgString>().arg;
                break;
        }

        MemberMsg(msg);
    }

    /// <summary>
    /// 开始考核
    /// </summary>
    /// <param name="id"></param>
    private void StartExam(int id)
    {
        activeExamId = id;

        //清空上轮考核的状态信息
        ToolManager.SendBroadcastMsg(new MsgBase((ushort)ExamPanelEvent.Flush));

        OnExamStart();

        DateTime startTime = GlobalInfo.ServerTime;
        DateTime endTime = startTime.AddMinutes(GlobalInfo.currentCourseInfo.duration).AddSeconds(3);
        ToolManager.SendBroadcastMsg(new MsgExamStart((ushort)ExamPanelEvent.Start, id, startTime, endTime, ExamUtility.Instance.ExamineeRecords));
        ToolManager.SendBroadcastMsg(new MsgInt()
        {
            msgId = (ushort)BaikeSelectModuleEvent.BaikeSelect,
            arg = GlobalInfo.currentWikiList?[0]?.id ?? 0
        });

        #region 开始3秒无法操作
        RootCanvasGroup.blocksRaycasts = false;
        this.WaitTime(3f, () =>
        {
            //考核准备时间结束后再开始考核倒计时
            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
                countdownCoroutine = null;
            }
            countdownCoroutine = StartCoroutine(Timing(endTime));
            RootCanvasGroup.blocksRaycasts = true;
        });
        #endregion
    }

    /// <summary>
    /// 结束考核,更新房间及UI状态
    /// </summary>
    /// <param name="msgId"></param>
    private void StopExam(ushort msgId = (ushort)ExamPanelEvent.Stop)
    {
        //避免重复发送终止考核消息
        if (!inExam || inStop)
            return;

        inStop = true;

        ToolManager.SendBroadcastMsg(new MsgInt(msgId, activeExamId));
        ToolManager.SendBroadcastMsg(new MsgBase((ushort)ExamPanelEvent.Flush));

        RequestManager.Instance.EndExam(activeExamId, () =>
        {
            ResetRoom(() =>
            {
                OnExamStop();
                ExamUtility.Instance.DeleteHostExamCache(GlobalInfo.roomInfo.Uuid);
                activeExamId = -1;
                inStop = false;
            });
        }, (error) =>
        {
            Log.Error($"考核[{activeExamId}]结束答题失败：{error}");
            ResetRoom(() =>
            {
                OnExamStop();
                ExamUtility.Instance.DeleteHostExamCache(GlobalInfo.roomInfo.Uuid);
                activeExamId = -1;
                inStop = false;
            });
        });
    }

    /// <summary>
    /// 考核结束 重置房间状态
    /// </summary>
    /// <param name="callback"></param>
    private void ResetRoom(UnityAction callback)
    {
        NetworkManager.Instance.RoomReset(GlobalInfo.roomInfo.Uuid, (roomInfo) =>
        {
            callback?.Invoke();
        }, (error) =>
        {
            Log.Error($"重置房间[{GlobalInfo.roomInfo.Uuid}]状态失败：{error}");
            callback?.Invoke();
        });
    }

    /// <summary>
    /// 考核开始更新UI状态
    /// </summary>
    private void OnExamStart()
    {
        inExam = true;
        this.FindChildByName("StartExam").gameObject.SetActive(false);
        this.FindChildByName("InExam").gameObject.SetActive(true);
        foreach (Transform item in Content)
        {
            SetMemberItemState(item, (int)State.InExam);
        }
    }

    /// <summary>
    /// 考核结束更新UI状态
    /// </summary>
    private void OnExamStop()
    {
        inExam = false;
        this.FindChildByName("StartExam").gameObject.SetActive(true);
        this.FindChildByName("InExam").gameObject.SetActive(false);
    }

    /// <summary>
    /// 退出房间
    /// 默认删除房间
    /// </summary>
    protected void ExitRoom(bool deleteRoom = true)
    {
        NetworkManager.Instance.ReleaseMicrophone();
        NetworkManager.Instance.LeaveRoom(deleteRoom);
    }

    #region 成员控制部分
    /// <summary>
    /// 房间内所有用户的数据字典
    /// </summary>
    public Dictionary<int, GameObject> allMemberItem = new Dictionary<int, GameObject>();
    /// <summary>
    /// 用户麦克风状态
    /// </summary>
    public Dictionary<int, Image> allMemberMicState = new Dictionary<int, Image>();
    private Image TeacherOnAir;
    /// <summary>
    /// 成员数量
    /// </summary>
    private Text MemberCount;
    /// <summary>
    /// 全员禁言
    /// </summary>
    private Button AllVoiceOffBtn;
    /// <summary>
    /// 禁言按钮
    /// </summary>
    private GameObject VoiceOnIcon;
    /// <summary>
    /// 取消禁言按钮
    /// </summary>
    private GameObject VoiceOffIcon;
    /// <summary>
    /// 全屏
    /// </summary>
    private GameViewDecoder FullScene;
    /// <summary>
    /// 下部栏
    /// </summary>
    private RectTransform Bottom;
    /// <summary>
    /// 状态颜色
    /// </summary>
    private StateColor[] stateColor;

    /// <summary>
    /// 全屏用户Id
    /// </summary>
    private int FullScreenUserId = -1;

    private struct StateColor
    {
        public Color BGColor;
        public Color TextColor;
        public string Content;

        public StateColor(Color bgColor, Color textColor, string content)
        {
            BGColor = bgColor;
            TextColor = textColor;
            Content = content;
        }
    }

    public enum State
    {
        Wait,
        InExam,
        Submit
    }

    /// <summary>
    /// 初始化成员列表部分
    /// </summary>
    private void InitMember()
    {
        stateColor = new StateColor[]
        {
            new StateColor("#D9D9D9".HexToColor(),"#5D5D5D".HexToColor(),"等待考核"),
            new StateColor("#ACE8C9".HexToColor(),"#248D57".HexToColor(),"考核中"),
            new StateColor("#F4CE9F".HexToColor(),"#E77000".HexToColor(),"已交卷")
        };

        AddMsg(new ushort[]
        {
            (ushort)RoomChannelEvent.UpdateMemberList,
            (ushort)RoomChannelEvent.OtherJoin,
            (ushort)RoomChannelEvent.OtherLeave,
            (ushort)RoomChannelEvent.TalkState,
            (ushort)MediaChannelEvent.MicOnAir,
            (ushort)MediaChannelEvent.AddView,
            (ushort)BaikeSelectModuleEvent.Hide,
            (ushort)RoomChannelEvent.LeaveRoom,
            (ushort)BaikeSelectModuleEvent.BaikeSelect,
            (ushort)ExamPanelEvent.Start
        });

        VoiceOnIcon = transform.FindChildByName("VoiceOn").gameObject;
        VoiceOffIcon = transform.FindChildByName("VoiceOff").gameObject;

        AllVoiceOffBtn = transform.GetComponentByChildName<Button>("AllVoiceOffBtn");
        AllVoiceOffBtn.onClick.AddListener(() =>
        {
            NetworkManager.Instance.SilentAllMember(!GlobalInfo.isAllTalk);
        });

        TeacherOnAir = this.GetComponentByChildName<Image>("TeacherOnAir");

        MemberCount = this.GetComponentByChildName<Text>("MemberCount");
        //MemberCount.text = GlobalInfo.roomInfo.memberCount.ToString();

        FullScene = this.GetComponentByChildName<GameViewDecoder>("FullScene");
        this.GetComponentByChildName<Button>("FullSceneClose").onClick.AddListener(() =>
        {
            CloseFullScene();
            FullScreenUserId = -1;
        });

        Bottom = this.GetComponentByChildName<RectTransform>("Bottom");
    }
    /// <summary>
    /// 成员信息接收
    /// </summary>
    /// <param name="msg"></param>
    private void MemberMsg(MsgBase msg)
    {
        switch (msg.msgId)
        {
            case (ushort)RoomChannelEvent.UpdateMemberList:
                UpdateMemberList(NetworkManager.Instance.GetRoomMemberList());
                break;
            case (ushort)RoomChannelEvent.OtherJoin:
                MsgIntString joinedMember = (MsgIntString)msg;
                OnOtherJoin(joinedMember.arg1, joinedMember.arg2);
                break;
            case (ushort)RoomChannelEvent.OtherLeave:
                MsgIntString leavedMember = (MsgIntString)msg;
                OnOtherLeave(leavedMember.arg1, leavedMember.arg2);
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
                    if (userId == GlobalInfo.account.id)
                    {
                        allMemberMicState.Add(userId, TeacherOnAir);
                    }
                    else
                    {
                        Transform memberTrans = Content.FindChildByName(userId.ToString());
                        if (memberTrans)
                        {
                            allMemberMicState.Add(userId, memberTrans.GetComponentByChildName<Image>("OnAir"));
                        }
                    }
                }

                if (allMemberMicState.ContainsKey(userId))
                {
                    allMemberMicState[userId].DOFade(1f, 0f);
                    allMemberMicState[userId].DOFade(0f, 1f);

                    if (userId == FullScreenUserId)
                    {
                        FullScreenOnAirImage.DOFade(1f, 0f);
                        FullScreenOnAirImage.DOFade(0f, 1f);
                    }
                }
                break;
            case (ushort)MediaChannelEvent.AddView:
                AddView(msg);
                break;
            case (ushort)RoomChannelEvent.LeaveRoom:
                GlobalInfo.currentWiki = null;
                GlobalInfo.currentCourseID = 0;
                BaikeSelectModule.selectID = 0;
                GlobalInfo.roomInfo = null;
                GlobalInfo.controllerIds.Clear();
                GlobalInfo.version = 0;
                GlobalInfo.isAllTalk = false;

                UIManager.Instance.CloseUI<ExamPanel>();
                if (logout)
                    ToolManager.GoToLogin();
                else
                    UIManager.Instance.OpenUI<ExamTrainingPanel>();

                string message = ((MsgString)msg).arg;
                if (!string.IsNullOrEmpty(message))
                    UIManager.Instance.OpenModuleUI<ToastPanel>(null, UILevel.PopUp, new ToastPanelInfo(message));
                break;
            case (ushort)BaikeSelectModuleEvent.Hide:
                UIManager.Instance.HideModuleUI<BaikeSelectModule>(this);
                SendMsg(new MsgBool((ushort)SmallFlowModuleEvent.LeftFlex, false));
                break;
            case (ushort)BaikeSelectModuleEvent.BaikeSelect:
            case (ushort)ExamPanelEvent.Start:
                NetworkManager.Instance.IsIMSync = true;
                break;
        }
    }
    /// <summary>
    /// 用户列表刷新回调
    /// </summary>
    public void UpdateMemberList(List<Member> members)
    {
        if (members == null || members.Count == 0)
            return;

        MemberCount.text = $"({members.Count - 1}人)";
        allMemberItem.Clear();

        Content.UpdateItemsView(members.Where(member => member.Id != GlobalInfo.account.id).ToList(), i => i.Id.ToString(), SetNewMember, SetOldMember);
        LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
        RefreshUI();

        SetTeacher(Bottom, members.Find(member => member.Id == GlobalInfo.account.id));
        WaitHint.SetActive(allMemberItem.Count == 0);
    }
    private void SetTeacher(Transform tf, Member info)
    {
        var voiceToggle = tf.GetComponentByChildName<Button>("VoiceControlTog");
        {
            voiceToggle.onClick.RemoveAllListeners();
            voiceToggle.onClick.AddListener(() => NetworkManager.Instance.SwitchUserChat(info.Id));
        }
        UpdateUIState(tf, info);
    }

    private void SetNewMember(Transform tf, Member info)
    {
        //防止重复利用旧资源的时候没清除状态
        if (inExam)
            SetMemberItemState(tf, (int)State.InExam);
        else
            SetMemberItemState(tf, (int)State.Wait);

        SetAccountInfo(tf, info);
        RegistMemberUIEvent(tf, info);
        UpdateUIState(tf, info);
    }

    private void SetMemberItemState(Transform item, int state)
    {
        if (item == null)
            return;
        item.GetComponentByChildName<Image>("StateColor").color = stateColor[state].BGColor;
        item.GetComponentByChildName<Image>("StateIcon").color = stateColor[state].TextColor;
        item.GetComponentByChildName<Text>("StateContent").color = stateColor[state].TextColor;
        item.GetComponentByChildName<Text>("StateContent").text = stateColor[state].Content;
    }

    private void SetOldMember(Transform tf, Member info)
    {
        SetAccountInfo(tf, info);
        UpdateUIState(tf, info);
    }

    /// <summary>
    /// 设置成员信息
    /// </summary>
    /// <param name="tf"></param>
    /// <param name="info"></param>
    private void SetAccountInfo(Transform tf, Member info)
    {
        if (!allMemberItem.ContainsKey(info.Id))
            allMemberItem.Add(info.Id, tf.gameObject);
        else
            allMemberItem[info.Id] = tf.gameObject;

        tf.GetComponentByChildName<Text>("PersonName").text = info.Nickname;

        var icon = tf.FindChildByName(NetworkManager.Instance.GetUserDevice(info.Id));
        {
            if (icon != null)
            {
                foreach (Transform child in icon.parent)
                {
                    child.gameObject.SetActive(false);
                }
                icon.gameObject.SetActive(true);
            }
        }
    }
    /// <summary>
    /// 注册UI事件
    /// </summary>
    /// <param name="tf"></param>
    /// <param name="info"></param>
    private void RegistMemberUIEvent(Transform tf, Member info)
    {
        //注册语音控制按钮
        var voiceToggle = tf.GetComponentByChildName<Button>("VoiceControlTog");
        {
            voiceToggle.onClick.RemoveAllListeners();
            voiceToggle.onClick.AddListener(() => NetworkManager.Instance.SwitchUserTalk(info.Id));
        }

        //注册踢人按钮,仅房主显示非本人的踢人按钮
        var kickBtn = tf.GetComponentByChildName<Button>("KickBtn");
        {
            kickBtn.onClick.RemoveAllListeners();
            kickBtn.onClick.AddListener(() =>
            {
                Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
                popupDic.Add("取消", new PopupButtonData(null));
                popupDic.Add("移出", new PopupButtonData(() => NetworkManager.Instance.KickOutUser(info.Id), true));
                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", $"将<color=#F6533F>{info.Nickname}</color>移出考核?", popupDic));
            });
        }

        //全屏按钮
        var FullSceneBtn = tf.GetComponentByChildName<Button>("FullSceneBtn");
        {
            FullSceneBtn.onClick.RemoveAllListeners();
            FullSceneBtn.onClick.AddListener(() =>
            {
                FullScene.GetComponentByChildName<Text>("PersonName").text = info.Nickname;

                var icon = FullScene.FindChildByName(NetworkManager.Instance.GetUserDevice(info.Id));
                {
                    foreach (Transform child in icon.parent)
                    {
                        child.gameObject.SetActive(false);
                    }

                    icon.gameObject.SetActive(true);
                }

                var voiceTog = FullScene.GetComponentByChildName<Button>("VoiceControlTog");
                voiceTog.onClick = voiceToggle.onClick;
                voiceTog.GetComponent<Image>().sprite = voiceToggle.GetComponent<Image>().sprite;

                var button = FullScene.GetComponentByChildName<Button>("KickBtn");
                {
                    button.onClick = kickBtn.onClick;
                    button.onClick.AddListener(() =>
                    {
                        FullScene.GetComponentByChildName<Button>("FullSceneClose").onClick.Invoke();
                    });
                }

                FullScene.GetComponentInChildren<RawImage>().texture = tf.GetComponentInChildren<RawImage>().texture;
                FullScene.GetComponentInChildren<AspectRatioFitter>().aspectRatio = tf.GetComponentInChildren<AspectRatioFitter>().aspectRatio;

                OpenFullScene(tf as RectTransform);
                //FullScene.gameObject.SetActive(true);

                FullScreenUserId = info.Id;
            });
        }
    }
    /// <summary>
    /// 更新UI状态
    /// </summary>
    /// <param name="tf"></param>
    /// <param name="info"></param>
    private void UpdateUIState(Transform tf, Member info)
    {
        //语音控制显示
        var voiceToggle = tf.GetComponentByChildName<Button>("VoiceControlTog");
        {
            if (info.Id != GlobalInfo.roomInfo?.creatorId)
                ButtonImageChange(voiceToggle, !info.IsTalk, info.IsChat);
            else
                ButtonImageChange(voiceToggle, false, info.IsChat);

            if (!GlobalInfo.IsHomeowner() && info.Id != GlobalInfo.account.id)
                voiceToggle.interactable = false;
        }
        //全屏用户同时更新全屏UI状态
        if (info.Id == FullScreenUserId)
        {
            var fullVoiceToggle = FullScene.GetComponentByChildName<Button>("VoiceControlTog");
            {
                ButtonImageChange(fullVoiceToggle, !info.IsTalk, info.IsChat);
            }
        }

        if (GlobalInfo.IsExamMode() && info.Id != GlobalInfo.account.id)
        {
            tf.FindChildByName("State").gameObject.SetActive(true);
            if (inExam)
            {
                SetMemberItemState(tf, (int)State.InExam);
            }
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

        button.GetComponent<Image>().sprite = button.GetComponentByChildName<Image>(buttonState).sprite;
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
    /// 新成员加入房间回调
    /// </summary>
    /// <param name="newJoinedId"></param>
    /// <param name="newJoinedName"></param>
    private void OnOtherJoin(int newJoinedId, string newJoinedName)
    {
        if (GlobalInfo.account.id == newJoinedId)
            return;
        UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo($"{newJoinedName}加入考核"));
        if (GlobalInfo.IsGroupMode())
            NetworkManager.Instance.SetUserColor(newJoinedId);
    }
    /// <summary>
    /// 成员离开房间回调
    /// </summary>
    /// <param name="leavedUserId"></param>
    /// <param name="leavedUserName"></param>
    private void OnOtherLeave(int leavedUserId, string leavedUserName)
    {
        UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo($"{leavedUserName}退出考核"));
        RemoveMember(leavedUserId);
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

            if (id == FullScreenUserId)
            {
                FullScene.gameObject.SetActive(false);
                FullScreenUserId = -1;
            }
        }
        WaitHint.SetActive(allMemberItem.Count == 0);
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
            UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo(GlobalInfo.isAllTalk ? "已解除全员禁言" : "已开启全员禁言"));
        }
    }
    private void AddView(MsgBase msg)
    {
        MsgIntString msgIntString = (MsgIntString)msg;
        int userId = msgIntString.arg1;
        string label = msgIntString.arg2;

        var target = Content.GetComponentByChildName<GameViewDecoder>(userId.ToString());
        if (target)
        {
            target.label = int.Parse(label);
            NetworkManager.Instance.AddUserVideo(label, target);
        }
    }
    #endregion

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        StopAllCoroutines();
        Timer.DelTimer(name);
        base.Close(uiData, callback);
        GlobalInfo.SetCourseMode(CourseMode.Training);
        GlobalInfo.canEditUserInfo = true;
    }

    protected override void InitHoverHint()
    {
#if UNITY_STANDALONE_WIN
        AddHoverHint(Prev, "上一页");
        AddHoverHint(Next, "下一页");
#endif
        AddHoverHint(this.GetComponentByChildName<Toggle>("Info"), "房间信息");
        AddHoverHint(this.GetComponentByChildName<Button>("Set"), "设置(ESC)");
        AddHoverHint(this.GetComponentByChildName<Button>("Quit"), "退出");
    }

    private bool logout;
    public override void GotoLogout()
    {
        Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
        popupDic.Add("取消", new PopupButtonData(null));
        popupDic.Add("退出登录", new PopupButtonData(() =>
        {
            logout = true;
            ExitRoom(false);
        }, true));
        UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "确定退出登录？", popupDic));//\n（解散房间）
    }

    /// <summary>
    /// 解散房间前，对正在进行的考核结束答题
    /// </summary>
    /// <param name="callback"></param>

    private void EndExamBeforeExitRoom(UnityAction callback)
    {
        if (inExam && activeExamId != -1)
        {
            RequestManager.Instance.EndExam(activeExamId, () =>
            {
                callback?.Invoke();
            }, (error) =>
            {
                Log.Error($"考核[{activeExamId}]结束答题失败：{error}");
                callback?.Invoke();
            });
        }
        else
        {
            callback?.Invoke();
        }
    }

    /// <summary>
    /// 计时协程
    /// </summary>
    private Coroutine countdownCoroutine;
    /// <summary>
    /// 计时
    /// </summary>
    /// <param name="endTime"></param>
    /// <returns></returns>
    private IEnumerator Timing(DateTime endTime)
    {
        var time = this.GetComponentByChildName<Text>("Time");
        {
            time.gameObject.SetActive(true);

            WaitForSecondsRealtime wait = new WaitForSecondsRealtime(1);
            while (endTime > GlobalInfo.ServerTime)
            {
                time.text = $"考核倒计时：{(endTime - GlobalInfo.ServerTime).ToString(@"hh\:mm\:ss")}";

                if (!inExam)
                {
                    //停止计时
                    time.gameObject.SetActive(false);
                    yield break;
                }

                yield return wait;
            }

            time.text = $"考核倒计时：00:00:00";

            time.gameObject.SetActive(false);

            //时间到需要主动停止考核
            StopExam((ushort)ExamPanelEvent.Timeout);
        };
    }

    #region 翻页动效
    private RectTransform Content;
    private Text Page;
    private Button_LinkMode Prev;
    private Button_LinkMode Next;
    private float viewHeight;
    private const float animeTime = 0.3f;
    private int currentPage
    {
        get
        {
            return _currentPage;
        }
        set
        {
            _currentPage = value;
            RefreshUI();
        }
    }
    private int _currentPage = 1;
    private int totalPage
    {
        get
        {
            return Mathf.CeilToInt(Content.rect.height / viewHeight);
        }
    }
    private PointComponent pointComponent;
    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="isPersonal">是不是个人模式</param>
    private void InitPage(bool isPersonal)
    {
        Content = this.FindChildByName("Content") as RectTransform;

#if UNITY_STANDALONE_WIN
        Prev = this.GetComponentByChildName<Button_LinkMode>("Prev");
        Prev.onClick.AddListener(() =>
        {
            currentPage--;
        });
        Next = this.GetComponentByChildName<Button_LinkMode>("Next");
        Next.onClick.AddListener(() =>
        {
            currentPage++;
        });
        SwitchPageAnim();
        Page = this.GetComponentByChildName<Text>("Page");
#endif

        var parentRect = Content.parent.GetComponent<RectTransform>().rect;
        var gridLayoutGroup = Content.GetComponent<GridLayoutGroup>();

#if UNITY_STANDALONE_WIN
        if (isPersonal)
        {
            var width = (parentRect.width - gridLayoutGroup.padding.horizontal - (gridLayoutGroup.spacing.x * (columnCount - 1))) / columnCount;
            var height = (parentRect.height - gridLayoutGroup.padding.vertical - (gridLayoutGroup.spacing.y * (rowCount - 1))) / rowCount;
            gridLayoutGroup.cellSize = new Vector2(width, height);
            viewHeight = parentRect.height;
        }
        else//小组考核房间人数限制
        {
            var width = (parentRect.width - gridLayoutGroup.padding.horizontal - (gridLayoutGroup.spacing.x * 2)) / 3;
            var height = (parentRect.height - gridLayoutGroup.padding.vertical - gridLayoutGroup.spacing.y) / 2;
            gridLayoutGroup.cellSize = new Vector2(width, height);
            Page.transform.parent.gameObject.SetActive(false);
        }
#endif

#if UNITY_ANDROID
        var width = (parentRect.width - gridLayoutGroup.padding.horizontal - (gridLayoutGroup.spacing.x * 2)) / 3;
        var height = (parentRect.height - gridLayoutGroup.padding.vertical - gridLayoutGroup.spacing.y) / 2;
        gridLayoutGroup.cellSize = new Vector2(width, height);
        viewHeight = parentRect.height;
        pointComponent = GetComponentInChildren<PointComponent>();
#endif
        RefreshUI();
    }

    #region 拖动相关
#if UNITY_ANDROID
    private Touch touch;
    private Vector2 touchPoint;
    private const float distance = 50;
    private void Update()
    {

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (currentPage > 1)
            {
                pointComponent.MoveDown();
                currentPage--;
            }
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (currentPage + 1 <= totalPage)
            {
                pointComponent.MoveUp();
                currentPage++;
            }
        }
#endif

        if (Input.touchCount > 0)
        {
            touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchPoint = touch.deltaPosition;
                    break;
                case TouchPhase.Ended:
                    var value = touch.deltaPosition.y - touchPoint.y;
                    {
                        if (value > distance)
                        {
                            if (currentPage + 1 <= totalPage)
                            {
                                pointComponent.MoveUp();
                                currentPage++;
                            }
                        }

                        if (value < -distance)
                        {
                            if (currentPage > 1)
                            {
                                pointComponent.MoveDown();
                                currentPage--;
                            }
                        }
                    }
                    break;
            }
        }
    }
#endif
    #endregion

    [ContextMenu("刷新界面")]
    private void RefreshUI()
    {
        //减去一条边
        Content.DOAnchorPos(Vector2.up * (viewHeight - 9) * (currentPage - 1), animeTime);

        foreach (GameViewDecoder gameViewDecoder in Content.GetComponentsInChildren<GameViewDecoder>(false))
        {
            int index = gameViewDecoder.transform.GetSiblingIndex();
            gameViewDecoder.enabled = index > (currentPage - 1) * (columnCount * rowCount) && index <= currentPage * (columnCount * rowCount);
        }

#if UNITY_STANDALONE_WIN
        Prev.interactable = currentPage != 1;
        Next.interactable = currentPage != totalPage;
        Page.text = $"{currentPage}/{totalPage}";
#endif

#if UNITY_ANDROID
        pointComponent.RefreshPoints(totalPage);
#endif
    }
    #endregion

    #region 全屏动效
    private RectTransform fullSceneRect;
    private Image FullScreenOnAirImage;
    private void InitFullScene()
    {
        fullSceneRect = this.GetComponentByChildName<RectTransform>("FullScene");
        fullSceneRect.AutoComponent<CanvasGroup>().alpha = 0;
        FullScreenOnAirImage = fullSceneRect.GetComponentByChildName<Image>("OnAir");
    }
    private void OpenFullScene(RectTransform item)
    {
        fullSceneRect.pivot = new Vector2(item.anchoredPosition.x / fullSceneRect.rect.width, 1 + (item.anchoredPosition.y / fullSceneRect.rect.height));
        fullSceneRect.anchoredPosition = Vector2.zero;
        fullSceneRect.sizeDelta = Vector2.zero;
        //SetPivot(fullSceneRect, new Vector2(item.anchoredPosition.x / fullSceneRect.rect.width, 1 + (item.anchoredPosition.y / fullSceneRect.rect.height)));
        fullSceneRect.localScale = Vector3.zero;
        fullSceneRect.gameObject.SetActive(true);
        fullSceneRect.DOScale(Vector3.one, animeTime);
        fullSceneRect.AutoComponent<CanvasGroup>().DOFade(1, animeTime);
    }
    private void CloseFullScene()
    {
        fullSceneRect.DOScale(Vector3.zero, animeTime).OnComplete(() =>
        {
            fullSceneRect.gameObject.SetActive(false);
        });
        fullSceneRect.AutoComponent<CanvasGroup>().DOFade(0, animeTime);
    }
    private void SetPivot(RectTransform rectTransform, Vector2 pivot)
    {
        Vector2 size = rectTransform.rect.size;
        Vector2 deltaPivot = rectTransform.pivot - pivot;
        Vector3 deltaPosition = new Vector3(deltaPivot.x * size.x, deltaPivot.y * size.y);

        rectTransform.pivot = pivot;
        rectTransform.localPosition -= deltaPosition;
    }
    #endregion

    #region 动效
    /// <summary>
    /// 切页按钮悬浮动效
    /// </summary>
    private void SwitchPageAnim()
    {
        Transform prevArrow = Prev.FindChildByName("Image");
        Transform nextArrow = Next.FindChildByName("Image");
        EventTrigger prevEventTrigger = Prev.AutoComponent<EventTrigger>();
        prevEventTrigger.AddEvent(EventTriggerType.PointerEnter, (arg) =>
        {
            if (!Prev.interactable)
                return;
            Sequence sequence = DOTween.Sequence();
            sequence.Join(prevArrow.DOLocalMoveY(3, 0.3f));
            sequence.Append(prevArrow.DOLocalMoveY(0, 0.3f));
            sequence.SetId("prevArrow");
        });
        prevEventTrigger.AddEvent(EventTriggerType.PointerExit, (arg) =>
        {
            DOTween.Kill("prevArrow", true);
        });
        EventTrigger nextEventTrigger = Next.AutoComponent<EventTrigger>();
        nextEventTrigger.AddEvent(EventTriggerType.PointerEnter, (arg) =>
        {
            if (!Next.interactable)
                return;
            Sequence sequence = DOTween.Sequence();
            sequence.Join(nextArrow.DOLocalMoveY(3, 0.3f));
            sequence.Append(nextArrow.DOLocalMoveY(0, 0.3f));
            sequence.SetId("nextArrow");
        });
        nextEventTrigger.AddEvent(EventTriggerType.PointerExit, (arg) =>
        {
            DOTween.Kill("nextArrow", true);
        });
    }

    protected override float joinAnimePlayTime => 0.3f;
    protected override float exitAnimePlayTime => 0.2f;

    public override void JoinAnim(UnityAction callback)
    {
        JoinSequence.Join(Join());
        base.JoinAnim(callback);
    }
    public override void ExitAnim(UnityAction callback)
    {
        ExitSequence.Join(Exit());
        base.ExitAnim(callback);
    }

    private Sequence Join()
    {
        Sequence sequence = DOTween.Sequence();
        sequence.AppendInterval(0.5f);
        sequence.Append(TopNavigation.DOAnchorPos3DY(0, JoinAnimePlayTime));
#if UNITY_STANDALONE
        sequence.Join(SideBar.DOAnchorPos3DX(0, JoinAnimePlayTime));
#endif
        sequence.Join(Bottom.DOAnchorPos3DY(0, JoinAnimePlayTime));
        sequence.Join(RootCanvasGroup.DOFade(1f, JoinAnimePlayTime));
        return sequence;
    }

    private Sequence Exit()
    {
        Sequence sequence = DOTween.Sequence();
        sequence.Join(TopNavigation.DOAnchorPos3DY(TopNavigation.sizeDelta.y, ExitAnimePlayTime));
#if UNITY_STANDALONE
        sequence.Join(SideBar.DOAnchorPos3DX(-SideBar.sizeDelta.x, ExitAnimePlayTime));
#endif
        sequence.Join(Bottom.DOAnchorPos3DY(-Bottom.sizeDelta.x, ExitAnimePlayTime));
        sequence.Join(RootCanvasGroup.DOFade(0.2f, ExitAnimePlayTime));
        return sequence;
    }
    #endregion
}