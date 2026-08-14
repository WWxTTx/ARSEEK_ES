using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;
using static UISmallSceneOperationHistory;

/// <summary>
/// 考核房间 考生
/// </summary>
public partial class ExamCoursePanel : OPLCoursePanel
{
    protected override bool CanLogout { get { return true; } }

    /// <summary>
    /// 开始考核后禁用设置
    /// </summary>
    public override bool canOpenOption => true;//!inExamInitSubmitCacheWithStatus

    /// <summary>
    /// 当前考核ID
    /// 避免状态同步时，执行非当前考核的操作
    /// </summary>
    private int examId;

    /// <summary>
    /// 已提交的百科视频列表，key：百科id, 视频路径，value：是否已提交记录
    /// </summary>
    private Dictionary<Tuple<int, string>, bool> videoDic = new Dictionary<Tuple<int, string>, bool>();

    private UISmallSceneModule smallSceneModule;

    private bool wikiInitialized = false;

    private CancellationTokenSource submitCts;
    private CancellationTokenSource mCountdownCts;

    private bool logout;

    public override void Open(UIData uiData = null)
    {
        GlobalInfo.waitExam = true;
        GlobalInfo.canEditUserInfo = false;
        base.Open(uiData);
        InitExam();
        InitRoomChannel();
#if UNITY_ANDROID || UNITY_IOS
        // 等待考核时 SideBar 不显示，StartExam 时才 SetVisible(true)
        CourseSideBar.SetVisible(false);
#endif
    }

    protected override void OnPrepareShow(UIData uiData)
    {
        NetworkManager.Instance.IsIMSync = false;
        InitData(() =>
        {
            //SetTitle(GlobalInfo.currentCourseInfo);
            Title.text = GlobalInfo.roomInfo.RoomName;

            NetworkManager.Instance.EnableLocalVideo(true);
            NetworkManager.Instance.IsIMSync = true;

            // 自动重连检查：不依赖房主消息
            CheckAutoReconnect();
        });
    }

    protected override void SetTitle(Course course)
    {
        base.SetTitle(course);
    }

    /// <summary>
    /// 检查是否需要自动重连（房间状态为考核中且倒计时未结束）
    /// </summary>
    private void CheckAutoReconnect()
    {
        string roomUuid = GlobalInfo.roomInfo.Uuid;
        int cachedExamId = ExamUtility.Instance.GetParticipantExamId(roomUuid);
        Log.Debug($"[ExamCoursePanel] CheckAutoReconnect roomUuid={roomUuid} cachedExamId={cachedExamId} Status={GlobalInfo.roomInfo?.Status}");

        if (GlobalInfo.roomInfo == null || GlobalInfo.roomInfo.Status != 2)
        {
            //房间不在考核状态，清理可能残留的考核缓存（处理房主结束考核时考生断线的情况）
            if (GlobalInfo.roomInfo != null)
            {
                if (cachedExamId > 0)
                {
                    Log.Debug($"[ExamCoursePanel] 房间不在考核状态，清理残留缓存 examId={cachedExamId}");
                    ExamUtility.Instance.DeleteParticipantExamCache(roomUuid);
                    GlobalInfo.ClearCachedRoom();
                }
            }
            return;
        }

        if (cachedExamId <= 0)
            return;

        DateTime? cachedEndTime = ExamUtility.Instance.GetParticipantExamEndTime(roomUuid);
        if (!cachedEndTime.HasValue || cachedEndTime.Value <= GlobalInfo.ServerTime)
        {
            ExamUtility.Instance.DeleteParticipantExamCache(roomUuid);
            GlobalInfo.ClearCachedRoom();
            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("确定", new PopupButtonData(() => Quit(), true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "您上次参与的考核已结束", popupDic, null, false));
            return;
        }

        Log.Debug($"[ExamCoursePanel] 检测到考核进行中，自动重连 examId={cachedExamId}");

        // 从本地恢复 cachedPacket
        PlayerPrefs.SetString(GlobalInfo.CachedRoom, roomUuid);

        var cachedExamineeRecords = ExamUtility.Instance.GetParticipantExamExamineeRecords(roomUuid);

        var msgExamStartData = new MsgExamStart(
            (ushort)ExamPanelEvent.Start,
            cachedExamId,
            GlobalInfo.ServerTime,
            cachedEndTime.Value,
            cachedExamineeRecords
        );

        NetworkManager.Instance.IsIMSyncState = true;
        OnExamStart(msgExamStartData);
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);

        switch (msg.msgId)
        {
            case (ushort)SmallFlowModuleEvent.CompleteStep:
                //正常操作完成消息不处理
                if (msg is MsgIntInt)
                    return;
                //模拟操作、习题百科初始化完成，可恢复百科状态
                wikiInitialized = true;
                break;
            default:
                ExamMsg(msg);
                RoomChannelMsg(msg);
                break;
        }
    }

    /// <summary>
    /// 考核模式下收到 BaikeSelect 消息，同步清理后切换百科
    /// </summary>
    protected override void OnBaikeSelectEventReceived(MsgBase msg)
    {
        int baikeId = msg is MsgBrodcastOperate broadcast
            ? broadcast.GetData<MsgInt>().arg
            : ((MsgInt)msg).arg;

        if (GlobalInfo.currentWiki != null && GlobalInfo.currentWiki.id == baikeId)
            return;

        BaikeSelectModule.selectID = baikeId;
        BaikeSelectModule.CurrentBaikeIndex = GlobalInfo.currentWikiList.FindIndex(wiki => wiki.id == baikeId);
        GlobalInfo.InSingleMode = false;
        ClearBaikeModules();
        ModelManager.Instance.DestroyModels(true);
        ModelManager.Instance.DestroyScripts(true);
        ModelManager.Instance.DestroySyncComponent();
        CourseSideBar.OnBaikeChanged();
        UIManager.Instance.CloseModuleUI<ExamToastPanel>(this);

        LoadEncyclopedia(baikeId);
    }

    /// <summary>
    /// 清除考核缓存（flag、参与者缓存、IM缓存）
    /// </summary>
    private void ClearExamCache()
    {
        GlobalInfo.waitExam = true;
        GlobalInfo.ClearCachedRoom();
        if (GlobalInfo.roomInfo != null)
        {
            ExamUtility.Instance.DeleteParticipantExamCache(GlobalInfo.roomInfo.Uuid);
        }
    }

    private void Quit()
    {
        //退出房间，立即删除flag，避免触发异常退出提示
        ClearExamCache();
        if (!GlobalInfo.waitExam && !NetworkManager.Instance.IsUserOnline(GlobalInfo.roomInfo.creatorId))
        {
            NetworkManager.Instance.SendIMMsg(new MsgBrodcastOperate((ushort)ExamPanelEvent.Flush, JsonTool.Serializable(new MsgBase((ushort)ExamPanelEvent.Flush))));
            NetworkManager.Instance.SendIMMsg(new MsgBrodcastOperate((ushort)ExamPanelEvent.Quit, JsonTool.Serializable(new MsgInt((ushort)ExamPanelEvent.Quit, examId))));
        }
        else
        {
            NetworkManager.Instance.SendIMMsg(new MsgBrodcastOperate((ushort)ExamPanelEvent.Quit, JsonTool.Serializable(new MsgInt((ushort)ExamPanelEvent.Quit, examId))));
        }
        DoQuit();
    }

    private void DoQuit()
    {
        NetworkManager.Instance.ReleaseMicrophone();
        ExitRoom();
        NetworkManager.Instance.LeaveRoom();
    }

    /// <summary>
    /// 退出课程
    /// </summary>
    protected override void ExitRoom()
    {
        GlobalInfo.currentWiki = null;
        GlobalInfo.currentCourseID = 0;
        BaikeSelectModule.selectID = 0;
        GlobalInfo.roomInfo = null;
        GlobalInfo.controllerIds.Clear();
        GlobalInfo.version = 0;
        GlobalInfo.isAllTalk = false;

        UIManager.Instance.CloseAllUI();
        if (logout)
            ToolManager.GoToLogin();
        else
        {
            UIManager.Instance.OpenUI<ExamTrainingPanel>();
        }
    }

    public override void Previous()
    {
        if (GlobalInfo.waitExam)
        {
            var popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("取消", new PopupButtonData(null, false));
            popupDic.Add("退出房间", new PopupButtonData(() =>
            {
                Quit();
            }, true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "确定退出房间？", popupDic, showCloseBtn: false));
        }
        else
        {
            var popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("取消", new PopupButtonData(null, false));
            popupDic.Add("退出房间", new PopupButtonData(() =>
            {
                Submit(() =>
                {
                    Dictionary<string, PopupButtonData> popupDic1 = new Dictionary<string, PopupButtonData>();
                    popupDic1.Add("确定", new PopupButtonData(() => Quit(), true));
                    UIManager.Instance.OpenUI<PopupPanel_AutoConfirm>(UILevel.PopUp, new UIAutoPopupData("提示", "考核提交成功，退出房间", popupDic1, 10, true, () =>
                    {
                        Quit();
                    }));
                }, false);
            }, true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "考核时间还未结束，确定提交考核并退出房间？", popupDic, showCloseBtn: false));
        }
    }


    public override void GotoLogout()
    {
        if (GlobalInfo.waitExam)
        {
            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("取消", new PopupButtonData(null));
            popupDic.Add("退出登录", new PopupButtonData(() =>
            {
                logout = true;
                Quit();
            }, true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "确定退出登录？", popupDic, showCloseBtn: false));
        }
        else
        {
            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("取消", new PopupButtonData(null, false));
            popupDic.Add("退出登录", new PopupButtonData(() =>
            {
                logout = true;
                Submit(() =>
                {
                    Dictionary<string, PopupButtonData> popupDic1 = new Dictionary<string, PopupButtonData>();
                    popupDic1.Add("确定", new PopupButtonData(() => Quit(), true));
                    UIManager.Instance.OpenUI<PopupPanel_AutoConfirm>(UILevel.PopUp, new UIAutoPopupData("提示", "考核提交成功，退出房间", popupDic1, 10, true, () =>
                    {
                        Quit();
                    }));
                }, false);
            }, true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "考核时间还未结束，确定提交考核并退出登录？", popupDic, showCloseBtn: false));
        }
      
    }


    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        base.Close(uiData, callback);

        GlobalInfo.SetCourseMode(CourseMode.Menu);
        GlobalInfo.canEditUserInfo = true;

        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }

    #region 百科控制相关
    /// <summary>
    /// 获取考试试卷
    /// </summary>
    /// <param name="callBack"></param>
    protected override void InitData(UnityAction callBack)
    {
        GlobalInfo.currentCourseID = GlobalInfo.roomInfo.CourseId;
        callBack?.Invoke();      
    }

    /// <summary>
    /// 加载百科（习题、模拟操作）
    /// </summary>
    /// <param name="encyclopediaId">百科id</param>
    private void LoadEncyclopedia(int encyclopediaId)
    {
        GlobalInfo.currentWiki = GlobalInfo.currentWikiList.Find(w => w.id == encyclopediaId);
        if (GlobalInfo.currentWiki == null)
        {
            Log.Error($"未找到百科: {encyclopediaId}");
            return;
        }

        Log.Debug($"加载百科: {encyclopediaId}");
        switch (GlobalInfo.currentWiki.typeId)
        {
            case (int)PediaType.Operation:
                LoadPediaWithModel();
                break;
            case (int)PediaType.Exercise:
                UIManager.Instance.OpenModuleUI<OPLExerciseModule>(this, BaikeModulePoint);
                CourseSideBar.ShowBaikeSelectModule(false);
                break;
            default:
                Log.Warning("百科类型异常：", GlobalInfo.currentWiki.typeId);
                break;
        }
    }

    /// <summary>
    /// 加载百科模型 
    /// </summary>
    /// <param name="encyclopedia"></param>
    protected override void LoadEncyclopediaModel(EncyclopediaModel encyclopedia)
    {
        var abList = encyclopedia.data.abPackageList.OrderByDescending(ab => ab.id).ToList();
        bool loadNavMesh = encyclopedia.typeId == (int)PediaType.Operation && (encyclopedia as EncyclopediaOperation).hasRole;
        ResManager.Instance.LoadSnapshotModelAsync(encyclopedia.id.ToString(), ResManager.Instance.OSSDownLoadPath + abList[0].filePath, loadNavMesh, true, (arg2) =>
        {
            GameObject go = ModelManager.Instance.CreateModel(arg2);

            if (go == null)
            {
                Log.Warning(string.Format("百科{0}实例化失败", encyclopedia.id));
                UIManager.Instance.CloseUI<LoadingPanel>();
                NetworkManager.Instance.IsIMSync = true;
                return;
            }

            go.name = go.name.Replace("(Clone)", string.Empty);

            //考核模拟操作不需要模型节点、知识点数据
            GlobalInfo.currentWikiNames.Clear();
            GlobalInfo.currentWikiKnowledges.Clear();
            CourseSideBar.KnowledgeTog.gameObject.SetActive(false);

            GlobalInfo.currentBaikeType = BaikeType.SmallScene;
            EncyclopediaOperation encyclopediaOperation = encyclopedia as EncyclopediaOperation;
            //根据配置设置有无漫游模式
            GlobalInfo.hasRole = encyclopediaOperation.hasRole;
            if (!encyclopediaOperation.hasRole)
                ModelManager.Instance.AddSyncComponent(Camera.main.gameObject);

            smallSceneModule = UIManager.Instance.OpenModuleUI<UISmallSceneModule>(this, BaikeModulePoint, new SmallSceneData(encyclopediaOperation.flows)) as UISmallSceneModule;
            SendMsg(new MsgBool((ushort)CoursePanelEvent.ChangeModel, encyclopedia.typeId != (int)PediaType.Operation));

            //单人考核的重连是单独的逻辑 不是在这里处理
            if(GlobalInfo.courseMode != CourseMode.Exam)
            {
                Debug.Log("执行多人考核状态恢复");
                NetworkManager.Instance.SyncBaikeState();
            }
           
            //提交考核记录事件绑定
            smallSceneModule.operationHistoryModule.OnRecordChanged.RemoveAllListeners();
            smallSceneModule.operationHistoryModule.OnRecordChanged.AddListener((recordData) =>
            {
                ExamUtility.Instance.EnqueueOperation(examId, GlobalInfo.currentWiki.id, recordData, GetExamineModelStates());
            });
        });
    }

    /// <summary>
    /// 百科切换回调，修改UI状态等
    /// </summary>
    /// <param name="newBaikeId"></param>
    protected override void OnBaikeChanged(int newBaikeId)
    {
        base.OnBaikeChanged(newBaikeId);
        UIManager.Instance.CloseModuleUI<ExamToastPanel>(this);
    }

    protected override void ClearBaikeModules(bool closeKnowledge = false)
    {
#if UNITY_ANDROID || UNITY_IOS
        EmptyClick.gameObject.SetActive(false);
#endif
        UIManager.Instance.CloseModuleUI<OPLPaintModule>(this);

        UIManager.Instance.CloseModuleUI<UISmallSceneModule>(this);
        UIManager.Instance.CloseModuleUI<OPLExerciseModule>(this);
        UIManager.Instance.CloseAllModuleUI<ShowImgModule>(this);
        UIManager.Instance.CloseAllModuleUI<ShowVideoModule>(this);

        Resources.UnloadUnusedAssets();
    }
    #endregion

    #region 考核部分
    /// <summary>
    /// 是否还原百科状态中
    /// </summary>
    public bool InSync;
    /// <summary>
    /// 考核结束时间
    /// </summary>
    private DateTime endTime;
    /// <summary>
    /// 提交考核按钮
    /// </summary>
    private Button submit;

    /// <summary>
    /// 剩余时长
    /// </summary>
    private int remainingSeconds;

    private void InitExam()
    {
        AddMsg(new ushort[]{
            (ushort)ExamPanelEvent.Start,
            (ushort)ExamPanelEvent.Stop,
            (ushort)ExamPanelEvent.Timeout,
            (ushort)ExamPanelEvent.Submit,
            (ushort)ExamPanelEvent.ExerciseScore,
            (ushort)SmallFlowModuleEvent.CompleteStep,
        });

        submit = this.GetComponentByChildName<Button>("Submit");
        {
            submit.onClick.AddListener(() =>
            {
                if (endTime > GlobalInfo.ServerTime)
                {
                    var popupDic = new Dictionary<string, PopupButtonData>();
                    popupDic.Add("取消", new PopupButtonData(null, false));
                    popupDic.Add("提交考核", new PopupButtonData(() =>
                    {
                        Submit(() =>
                        {
                            //var popupDic = new Dictionary<string, PopupButtonData>();
                            //popupDic.Add("确定", new PopupButtonData(() =>
                            //{
                            //    Quit();
                            //}, true));
                            //UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "考核提交成功，退出房间", popupDic, showCloseBtn: false));

                            Dictionary<string, PopupButtonData> popupDic1 = new Dictionary<string, PopupButtonData>();
                            popupDic1.Add("确定", new PopupButtonData(() => Quit(), true));
                            UIManager.Instance.OpenUI<PopupPanel_AutoConfirm>(UILevel.PopUp, new UIAutoPopupData("提示", "考核提交成功，退出房间", popupDic1, 10, true, () =>
                            {
                                Quit();
                            }));
                        }, false);
                    }, true));
                    UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "考核时间还未结束，确定提前提交？", popupDic, showCloseBtn: false));
                }
                else
                {
                    Submit(Quit);
                    Log.Error("已过考核结束时间提交，这种情况不应该发生才对");
                }
            });

            submit.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 提交考核
    /// </summary>
    /// <param name="callBack"></param>
    /// <param name="showResult"></param>
    private void Submit(Action callBack = null, bool showResult = true)
    {
        if (GlobalInfo.waitExam) return;

        mCountdownCts?.Cancel();
        GlobalInfo.waitExam = true;
        UpdateUIWhenExamStop();
        //正常提交考核，立即删除flag，避免退出房间后触发异常退出提示
        ClearExamCache();
        SubmitExamRecord(true, true, (submitSuccess) =>
        {
            if (submitSuccess)
            {
                NetworkManager.Instance.SendIMMsg(new MsgBrodcastOperate((ushort)ExamPanelEvent.Submit, JsonTool.Serializable(new MsgIntString((ushort)ExamPanelEvent.Submit, examId, GlobalInfo.account.nickname))));
            }
        });
        callBack?.Invoke();
    }

    /// <summary>
    /// 异步获取考核操作历史
    /// </summary>
    public void RefreshExamOpHistoryAsync(Action<AnswerOp> answerOpCallBack)
    {
        _RefreshExamOpHistoryAsync(answerOpCallBack).Forget();
    }

    /// <summary>
    /// 使用联机考核记录刷新操作历史
    /// </summary>
    private async UniTaskVoid _RefreshExamOpHistoryAsync(Action<AnswerOp> answerOpCallBack)
    {
        UIManager.Instance.OpenUI<LoadingPanel>();
        // 试卷百科对应答案列表，key：百科id，value：百科对应答案数据
        Dictionary<int, Answer> answersDic = new Dictionary<int, Answer>();
        UnityAction<int, List<Answer>, List<Accessory>> success = (id, answers, accessories) =>
        {
            if (answers != null)
            {
                foreach (var answer in answers)
                {
                    if (answersDic.ContainsKey(answer.baikeId))
                        answersDic[answer.baikeId] = answer;
                    else
                        answersDic.Add(answer.baikeId, answer);
                }
            }

            AnswerOp answerOp = answersDic[GlobalInfo.currentWiki.id] as AnswerOp;
            if (answerOp != null)
            {
                List<OpRecordData> opRecordData = answerOp.operations?.Select(data => new OpRecordData()
                {
                    index = data.index,
                    msg = data.msg,
                    userNo = data.userNo,
                    userName = data.userName,
                    createTime = data.createTime,
                    type = data.type,
                    score = data.score,
                    totalStepIndex = data.totalStepIndex
                }).ToList();

                // 调用回调函数，传递 answerOp 给调用者
                answerOpCallBack?.Invoke(answerOp);
            }
            else
            {
                answerOpCallBack?.Invoke(null);
            }

            UIManager.Instance.HideUI<LoadingPanel>();
        };

        UnityAction<string> error = errorMsg =>
        {
            UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo("考核记录获取失败"));
            answerOpCallBack?.Invoke(null);
            UIManager.Instance.HideUI<LoadingPanel>();
        };

        try
        {
            if (GlobalInfo.courseMode == CourseMode.Exam)
            {
                int recordId = ExamUtility.Instance.GetUserRecordId(GlobalInfo.account.id);
                ExamUtility.Instance.GetExamineResultByRecordId(recordId, success, error);
            }
            else
            {
                ExamUtility.Instance.GetExamineResult(examId, success, error);
            }
        }
        catch
        {
            UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo("网络超时，同步记录获取失败"));
            answerOpCallBack?.Invoke(null);
            UIManager.Instance.HideUI<LoadingPanel>();
        }
    }

    /// <summary>
    /// 恢复操作记录 和对应 模型状态
    /// </summary>
    private void RecoveryExam(AnswerOp savedAnswer)
    {
        UIManager.Instance.OpenUI<LoadingPanel>();
        Canvas canvas = UIManager.Instance.canvas;

        smallSceneModule = UIManager.Instance.canvas.GetComponentInChildren<UISmallSceneModule>(true);
        NetworkManager.Instance.IsIMSync = false;

        int flow = 0, step = -1;
        // 记录当前步骤中已完成的操作ID（用于部分完成恢复）
        HashSet<string> partialCompletedOps = new HashSet<string>();

        //优先用可靠的 totalStepIndex 定位进度；旧数据(全为-1)回退到 hint_success 字符串匹配
        if (savedAnswer != null && savedAnswer.operations != null && savedAnswer.operations.Count > 0)
        {
            var ctrl = smallSceneModule.smallFlowCtrl;
            var flows = ctrl.flows;

            // 模型状态字典：op.ID -> 最终 optionName，用于判定某 op 是否已完成
            Dictionary<string, string> modelStatesDict = savedAnswer.modelStates != null
                ? savedAnswer.modelStates.Where(ms => ms.id != null)
                    .GroupBy(ms => ms.id)
                    .ToDictionary(g => g.Key, g => g.Last().optionName)
                : new Dictionary<string, string>();

            // 用户操作过的最远步骤（步骤顺序门控，到达某步说明此前步骤已完成）
            int maxTotal = -1;
            foreach (var op in savedAnswer.operations)
            {
                if (op.totalStepIndex > maxTotal)
                    maxTotal = op.totalStepIndex;
            }
            if (maxTotal >= 0)
            {
                // 有可靠步骤索引：maxTotal 即用户到达的最远步骤
                ctrl.TotalIndexToFlowStep(maxTotal, out int reachedFlow, out int reachedStep);

                // 判定该最远步骤是否所有 op 都已完成（用模型状态比对）
                var reachedStepData = flows[reachedFlow].steps[reachedStep];
                bool reachedStepComplete = IsStepFullyCompleted(reachedStepData, modelStatesDict, partialCompletedOps);

                if (reachedStepComplete)
                {
                    // 最远步骤已完成，已完成进度=该步骤，待操作的是下一步
                    flow = reachedFlow;
                    step = reachedStep;
                    partialCompletedOps.Clear();
                }
                else
                {
                    // 最远步骤未完成：已完成进度=其前一步，partialCompletedOps 为当前步骤已完成的 op
                    if (reachedStep > 0)
                    {
                        flow = reachedFlow;
                        step = reachedStep - 1;
                    }
                    else if (reachedFlow > 0)
                    {
                        flow = reachedFlow - 1;
                        step = flows[flow].steps.Count - 1;
                    }
                    else
                    {
                        // 停留在第一个步骤且未完成
                        flow = 0;
                        step = -1;
                    }
                }
            }
            else
            {
                // 旧数据：totalStepIndex 全为 -1，回退到 hint_success 字符串按序匹配
                int opIndex = 0;
                for (int f = 0; f < flows.Length; f++)
                {
                    bool flowMatched = false;
                    for (int s = 0; s < flows[f].steps.Count; s++)
                    {
                        int preOpIndex = opIndex;
                        if (TryMatchStepInOps(flows[f].steps[s], savedAnswer.operations.ToList(), ref opIndex))
                        {
                            flow = f;
                            step = s;
                            flowMatched = true;
                            partialCompletedOps.Clear();
                        }
                        else
                        {
                            if (opIndex > preOpIndex && flows[f].steps[s].ops != null)
                            {
                                foreach (var op in flows[f].steps[s].ops)
                                {
                                    if (op.operation != null && modelStatesDict.TryGetValue(op.operation.ID, out string state))
                                    {
                                        if (state == op.optionName)
                                            partialCompletedOps.Add(op.operation.ID);
                                    }
                                }
                            }
                            break;
                        }
                    }
                    if (!flowMatched)
                        break;
                }
            }
        }

        Log.Debug($"考核重连恢复进度 flow:{flow} step:{step} partialOps:{partialCompletedOps.Count}");

        smallSceneModule.smallFlowCtrl.ignoreMove = false;
        //同步操作对象状态 恢复步骤
        if (step == -1)
        {
            //停留在第一个步骤且未完成：恢复到首步，补回已完成的部分 op（首步未完成，不能 Next）
            smallSceneModule.smallFlowCtrl.SelectStep(flow, 0, false, savedAnswer);
            if (partialCompletedOps.Count > 0)
            {
                smallSceneModule.smallFlowCtrl.SetCompletedOpIds(partialCompletedOps);
            }
        }
        else
        {
            smallSceneModule.smallFlowCtrl.SelectStep(flow, step, false, savedAnswer);
            //历史是已完成的，需要操作的是下一步
            smallSceneModule.smallFlowCtrl.Next(allowPositionRestore: true);

            // 恢复新步骤中部分完成的操作（Next会触发ClearCompletedOps，所以在此之后恢复）
            if (partialCompletedOps.Count > 0)
            {
                smallSceneModule.smallFlowCtrl.SetCompletedOpIds(partialCompletedOps);
            }
        }
        smallSceneModule.RefreshHighlight();


        //完成恢复，打开消息处理
        NetworkManager.Instance.IsIMSync = true;
    }


    /// <summary>
    /// 在操作记录中按顺序查找匹配当前步骤的所有 ops 的 hint_success
    /// 一个步骤有多个 ops 时，需要匹配到 ops.Count 条记录才算完成
    /// 匹配成功 opIndex 前进，失败则不移动
    /// </summary>
    private bool TryMatchStepInOps(SmallStep1 stepData, List<ExamineResultOperation> operations, ref int opIndex)
    {
        if (stepData == null || opIndex >= operations.Count)
            return false;

        if (stepData.ops == null || stepData.ops.Count == 0)
            return false;

        // 收集该步骤所有合法的 hint_success 值
        HashSet<string> validHints = new HashSet<string>();
        if (!string.IsNullOrEmpty(stepData.hint_success))
            validHints.Add(stepData.hint_success);
        foreach (var op in stepData.ops)
        {
            var opList = op.operation?.operations;
            if (opList != null)
            {
                foreach (var opBase in opList)
                {
                    if (!string.IsNullOrEmpty(opBase.hint_success))
                        validHints.Add(opBase.hint_success);
                }
            }
        }

        if (validHints.Count == 0)
            return false;

        // 每个op生成一条操作记录，需要匹配 ops.Count 条
        int expectedCount = stepData.ops.Count;
        int matchCount = 0;
        int searchStart = opIndex;

        for (int i = searchStart; i < operations.Count; i++)
        {
            string msg = operations[i].msg;
            if (string.IsNullOrEmpty(msg))
                continue;

            if (validHints.Contains(msg))
            {
                matchCount++;
                opIndex = i + 1;

                if (matchCount >= expectedCount)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 用最终模型状态判定某步骤是否所有 op 都已完成。
    /// 未完成时，已完成的 op ID 写入 partialOps（先清空）。
    /// </summary>
    private bool IsStepFullyCompleted(SmallStep1 stepData, Dictionary<string, string> modelStatesDict, HashSet<string> partialOps)
    {
        partialOps.Clear();
        if (stepData?.ops == null || stepData.ops.Count == 0)
            return true;

        bool allDone = true;
        foreach (var op in stepData.ops)
        {
            if (op.operation == null)
                continue;

            if (modelStatesDict.TryGetValue(op.operation.ID, out string state) && state == op.optionName)
                partialOps.Add(op.operation.ID);
            else
                allDone = false;
        }

        if (allDone)
            partialOps.Clear();
        return allDone;
    }

    private void SubmitExamRecord(bool submitRecording = true, bool showToast = true, Action<bool> callBack = null)
    {
        submitCts?.Cancel();
        submitCts = new CancellationTokenSource();
        _submitExamRecord(submitRecording, showToast, callBack, submitCts.Token).Forget();
    }
    private async UniTaskVoid _submitExamRecord(bool submitRecording = true, bool showToast = true, Action<bool> callBack = null, CancellationToken ct = default)
    {
        var currentWiki = GlobalInfo.currentWiki;
        if (currentWiki == null)
        {
            callBack?.Invoke(false);
            return;
        }

        await UniTask.WaitForEndOfFrame(this, ct);

        // await之后重新获取，防止异步期间状态变更
        currentWiki = GlobalInfo.currentWiki;
        if (currentWiki == null)
        {
            callBack?.Invoke(false);
            return;
        }

        switch (currentWiki.typeId)
        {
            case (int)PediaType.Operation:
                SubmitOperationEncyclopedia(currentWiki.id, showToast, submitRecording, callBack);
                break;
            case (int)PediaType.Exercise:
                OPLExerciseModule exercise = GetComponentInChildren<OPLExerciseModule>();
                if (exercise != null)
                {
                    string operation = string.Empty;
                    EncyclopediaExercise encyclopediaExercise = currentWiki as EncyclopediaExercise;
                    if (encyclopediaExercise?.data?.exercise != null)
                    {
                        switch (encyclopediaExercise.data.exercise.type)
                        {
                            case 1://选择题(单选;多选)
                                operation = exercise._selectedAnswers.Aggregate(string.Empty, (current, i) => current + ((char)('A' + i)).ToString());
                                break;
                            case 2://判断题
                                if (exercise._selectedAnswers.Count == 1)
                                    operation = exercise._selectedAnswers[0] == 0 ? "正确" : "错误";
                                else
                                    operation = string.Empty;
                                break;
                            case 3://操作题
                            default:
                                break;
                        }
                    }
                    string questionTitle = exercise.GetComponentByChildName<Text>("Title")?.text ?? "";
                    // 注意：这里传递0分，因为实际的分数已经在SubmitCurrentExerciseRecord()中即时提交过了
                    // 这个方法主要用于最终整体提交或补提交场景
                    SubmitExerciseEncyclopedia(currentWiki.id, operation, questionTitle, 0, showToast, submitRecording, callBack);
                }
                break;
        }
    }

    /// <summary>
    /// 保存操作百科考核记录
    /// </summary>
    /// <param name="baikeId"></param>
    /// <param name="showToast"></param>
    /// <param name="submitRecording"></param>
    /// <param name="callBack"></param>
    private void SubmitOperationEncyclopedia(int baikeId, bool showToast, bool submitRecording, Action<bool> callBack)
    {
        if (showToast)
            UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo("考核记录提交成功！"));
        callBack?.Invoke(true);

        //ExamUtility.Instance.SubmitExamineResult_Operation(examId, 0, baikeId, GetExamineModelStates(), () =>
        //{
        //    Log.Debug($"考核{examId} 百科:{baikeId} 考核记录提交成功");

        //    if (!GlobalInfo.ExamRecording)
        //    {
        //        if (showToast)
        //            UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo("考核记录提交成功！"));
        //        callBack?.Invoke(true);
        //        return;
        //    }

        //    #region 提交考核附件
        //    //每次提交 检查是否存在已上传成功但未记录的监控视频
        //    //List<Accessory> accessoryList = videoDic.Where(v => !v.Value)
        //    //    .Select(v => new Accessory() { encyclopediaId = v.Key.Item1, filePath = v.Key.Item2 }).ToList();
        //    //RequestManager.Instance.SubmitExamAccessory(examId, accessoryList, () =>
        //    //{
        //    //    //标记已成功提交的视频
        //    //    foreach (var accessory in accessoryList)
        //    //    {
        //    //        var video = videoDic.FirstOrDefault(v => v.Key.Item2.Equals(accessory.filePath));
        //    //        if (videoDic.ContainsKey(video.Key))
        //    //            videoDic[video.Key] = true;
        //    //    }

        //    //    if (showToast)
        //    //        UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo("考核记录提交成功！"));
        //    //    callBack?.Invoke(true);
        //    //}, (errorCode, errorMsg) =>
        //    //{
        //    //    Log.Error($"考核{examId} 百科:{baikeId} 考核附件提交失败");
        //    //    if (showToast)
        //    //        UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo("考核记录提交成功！"));
        //    //    callBack?.Invoke(true);
        //    //});
        //    #endregion
        //},
        // (errorCode, errorMsg) =>
        // {
        //     Log.Error($"考核{examId} 百科:{baikeId} 考核记录提交失败：{errorMsg}");
        //     //TODO待完善异常处理
        //     if (showToast)
        //     {
        //         var popupDic = new Dictionary<string, PopupButtonData>();
        //         popupDic.Add("重新提交", new PopupButtonData(() =>
        //         {
        //             SubmitExamRecord(submitRecording, showToast, callBack);
        //         }, false));
        //         popupDic.Add("退出房间", new PopupButtonData(Quit, true));
        //         UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("错误提示", "考核记录提交失败！", popupDic, showCloseBtn: false));
        //     }
        // });
    }

    /// <summary>
    /// 提交习题百科考核记录
    /// </summary>
    /// <param name="baikeId"></param>
    /// <param name="operation"></param>
    /// <param name="msg"></param>
    /// <param name="score"></param>
    /// <param name="showToast"></param>
    /// <param name="submitRecording"></param>
    /// <param name="callBack"></param>
    private void SubmitExerciseEncyclopedia(int baikeId, string operation, string msg, float score, bool showToast, bool submitRecording, Action<bool> callBack)
    {
        ExamUtility.Instance.SubmitExamineResult_Exercise(examId, baikeId, operation, msg, score, () =>
        {
            Log.Debug($"考核{examId} 百科:{baikeId} 考核记录提交成功");
            if (showToast)
                UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo("考核记录提交成功！"));
            callBack?.Invoke(true);
        },
        (errorCode, errorMsg) =>
        {
            Log.Error($"考核{examId} 百科:{baikeId} 考核记录提交失败：{errorMsg}");
            //TODO待完善异常处理
            if (showToast)
            {
                var popupDic = new Dictionary<string, PopupButtonData>();
                popupDic.Add("重新提交", new PopupButtonData(() =>
                {
                    SubmitExamRecord(submitRecording, showToast, callBack);
                }, false));
                popupDic.Add("退出房间", new PopupButtonData(Quit, true));
                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("错误提示", "考核记录提交失败！", popupDic, showCloseBtn: false));
            }
        });
    }

    /// <summary>
    /// 获取考核操作百科模型状态
    /// </summary>
    /// <returns></returns>
    private ExamineResultModelState[] GetExamineModelStates()
    {
        List<OpDicData> modelStates = new List<OpDicData>();
        if (smallSceneModule != null && smallSceneModule.smallFlowCtrl != null)
        {
            modelStates = smallSceneModule.smallFlowCtrl.GetModelStates();
        }
        //特殊状态
        if (smallSceneModule != null && smallSceneModule.simuSystem != null)
        {
            modelStates.Add(new OpDicData("SimuSystemState", smallSceneModule.simuSystem.GetSystemState()));
        }
        //modelStates.Add(new OpDicData("FatalFinishMessage", smallSceneModule.FatalFinishMessage));

        var results = modelStates.Select(m => new ExamineResultModelState()
        {
            id = m.id,
            index = modelStates.IndexOf(m),
            optionName = m.optionName,
            uiTargetModelEulerZ = m.uiTargetModelEulerZ.ToString()
        }).ToList();

        return results.ToArray();
    }

    /// <summary>
    /// 考核相关消息处理
    /// </summary>
    /// <param name="msg"></param>
    private void ExamMsg(MsgBase msg)
    {
        switch (msg.msgId)
        {
            case (ushort)ExamPanelEvent.Start:
                if(GlobalInfo.waitExam)
                    OnExamStart((msg as MsgBrodcastOperate).GetData<MsgExamStart>());
                break;
            case (ushort)ExamPanelEvent.Stop:
                if(!GlobalInfo.waitExam)
                    OnExamStop((msg as MsgBrodcastOperate).GetData<MsgInt>().arg);
                break;
            case (ushort)ExamPanelEvent.Timeout:
                OnHostTimeout((msg as MsgBrodcastOperate).GetData<MsgInt>().arg);
                break;
            case (ushort)ExamPanelEvent.ExerciseScore:
                currentExerciseScore = ((MsgInt)msg).arg;
                Log.Debug($"[ExamCoursePanel] ExerciseScore received: score={currentExerciseScore}, currentWikiId={GlobalInfo.currentWiki?.id}");
                SubmitCurrentExerciseRecord();
                break;
            case (ushort)ExamPanelEvent.Submit:
                if (!GlobalInfo.waitExam)
                {
                    var submitMsg = msg as MsgBrodcastOperate;
                    OnExamSubmit(submitMsg.senderId, submitMsg.GetData<MsgIntString>().arg1, submitMsg.GetData<MsgIntString>().arg2);
                }
                break;
        }
    }

    private int currentExerciseScore = 0;

    private void SubmitCurrentExerciseRecord()
    {
        var currentWiki = GlobalInfo.currentWiki;
        if (currentWiki == null || currentWiki.typeId != (int)PediaType.Exercise)
            return;

        var exercise = GetComponentInChildren<OPLExerciseModule>();
        if (exercise == null)
            return;

        string operation = "";
        var encyclopediaExercise = currentWiki as EncyclopediaExercise;
        if (encyclopediaExercise?.data?.exercise != null)
        {
            switch (encyclopediaExercise.data.exercise.type)
            {
                case 1:
                    operation = exercise._selectedAnswers.Aggregate("", (cur, i) => cur + ((char)('A' + i)).ToString());
                    break;
                case 2:
                    operation = exercise._selectedAnswers.Count == 1
                        ? (exercise._selectedAnswers[0] == 0 ? "正确" : "错误")
                        : "";
                    break;
            }
        }

        string questionTitle = exercise.GetComponentByChildName<Text>("Title")?.text ?? "";

        Log.Debug($"[ExamCoursePanel] SubmitCurrentExerciseRecord: wikiId={currentWiki.id}, score={currentExerciseScore}, operation={operation}");
        SubmitExerciseEncyclopedia(currentWiki.id, operation, questionTitle, currentExerciseScore, false, false, null);
    }

    /// <summary>
    /// 考核开始回调
    /// </summary>
    private void OnExamStart(MsgExamStart msgExamStartData)
    {
        Log.Debug($"[ExamCoursePanel] OnExamStart examId={msgExamStartData.examId}, waitExam={GlobalInfo.waitExam}");
        ExamUtility.Instance.ClearAllExerciseAnswers();
        // 打开Loading，持续到LoadEncyclopediaModel中模型加载完毕
        UIManager.Instance.OpenUI<LoadingPanel>();
        this.FindChildByName("WaitHint").gameObject.SetActive(false);
        RequestManager.Instance.GetExamination(msgExamStartData.examId, (examination) =>
        {
            GlobalInfo.SaveExaminationInfo(examination);
            GlobalInfo.currentWikiList = examination.encyclopediaList;

            if (GlobalInfo.currentWikiList == null || GlobalInfo.currentWikiList.Count == 0)
            {
                UIManager.Instance.CloseUI<LoadingPanel>();
                var popupDic = new Dictionary<string, PopupButtonData>();
                {
                    popupDic.Add("确定", new PopupButtonData(Quit, true));
                    UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "该考核未添加", popupDic, null, false));
                }
            }
            //仅允许在未开始考核时检查重连和开始考核
            else
            {
                // 初始化 examineeRecords 用于获取提交记录
                ExamUtility.Instance.InitSubmitCache(msgExamStartData.examineeRecords);

                if (GlobalInfo.waitExam)
                {
                    GlobalInfo.waitExam = false;

                    // 立即保存缓存，避免倒计时期间断开导致无法重连
                    PlayerPrefs.SetString(GlobalInfo.CachedRoom, GlobalInfo.roomInfo.Uuid);
                    ExamUtility.Instance.SetParticipantExamCache(
                        GlobalInfo.roomInfo.Uuid,
                        msgExamStartData.examId,
                        msgExamStartData.endTime,
                        msgExamStartData.examineeRecords
                    );

                    if (NetworkManager.Instance.IsIMSyncState)
                        StartExam(msgExamStartData);
                    else
                    {
                        //正常开始：走倒计时流程，3s结束后开始考核（模型在首次位置同步时懒加载创建）
                        //关闭loading让用户看到倒计时
                        UIManager.Instance.CloseUI<LoadingPanel>();
                        StartTiming(() =>
                        {
                            StartExam(msgExamStartData);
                        });
                    }
                }
               
            }
        }, (error) => {
            UIManager.Instance.CloseUI<LoadingPanel>();
            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("好的", new PopupButtonData(Quit, true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("错误", "获取考试信息失败！请重新加入房间", popupDic, showCloseBtn: false));
            Log.Error($"获取考试[{msgExamStartData.examId}]信息失败！原因为：{error}");
        });

    }

    /// <summary> 
    /// 考核开始倒计时
    /// </summary>
    /// <param name="callBack"></param>
    private void StartTiming(UnityAction callBack)
    {
        Transform startTimingTrans = this.FindChildByName("StartTiming");
        var text = this.GetComponentByChildName<Text>("StartTimingText");

        Log.Debug("开始倒计时");
        startTimingTrans.gameObject.SetActive(true);
        float index = 0;
        text.text = "3";
        SoundManager.Instance.PlayEffect("Countdown");
        DOTween.To(() => index, x => index = x, 3, 3).OnUpdate(() =>
        {
            if (index > 2)
                text.text = "1";
            else if (index > 1)
                text.text = "2";
        }).SetEase(Ease.Linear).OnComplete(() =>
        {
            startTimingTrans.gameObject.SetActive(false);
            callBack?.Invoke();
        });
    }

    /// <summary>
    /// 开始考核
    /// </summary>
    /// <param name="data"></param>
    private void StartExam(MsgExamStart data)
    {
        Log.Debug($"[ExamCoursePanel] StartExam examId={data.examId} waitExam={GlobalInfo.waitExam}->false endTime={data.endTime}");
        GlobalInfo.waitExam = false;

        //主动加载百科模型（现在不在房间内切百科了，房主重连时不发送百科选择消息了）
        int baikeId = GlobalInfo.currentWikiList[0].id;

        if (baikeId != 0)
        {
            wikiInitialized = false;
            UIManager.Instance.OpenUI<LoadingPanel>();
            LoadEncyclopedia(baikeId);
        }

        submit.gameObject.SetActive(true);
        examId = data.examId;
        endTime = data.endTime;
        ExamScreenRecording.Instance.ExamId = examId;

        mCountdownCts?.Cancel();
        mCountdownCts = new CancellationTokenSource();
        Timing(data.endTime, mCountdownCts.Token).Forget();

#if UNITY_STANDALONE
        var mid = this.GetComponentByChildName<CanvasGroup>("MidBtns");
        mid.alpha = 1;
        mid.blocksRaycasts = true;
#else
        CourseSideBar.SetVisible(true);
#endif

        CourseSideBar.SetBaikePage();

        //仅单人考核走 RecoveryExam（服务器记录定进度+补状态）；多人考核重连由 SyncBaikeState 缓存路径恢复
        if (NetworkManager.Instance.IsIMSyncState && GlobalInfo.courseMode == CourseMode.Exam)
            RefreshExamOpHistoryAsync(answerOp =>{
                // 网络请求成功，从 answersDic 提取模型状态并恢复进度 主要目的是为了恢复正确流程的初始视角和联动步骤
                RecoveryExam(answerOp);
            });
    }

    /// <summary>
    /// 考核计时, 计时结束后自动提交
    /// </summary>
    /// <param name="endTime"></param>
    /// <returns></returns>
    private async UniTaskVoid Timing(DateTime endTime, CancellationToken ct)
    {
        var time = this.GetComponentByChildName<Text>("Time");
        time.gameObject.SetActive(true);

        TimeSpan remainingTime;
        while (endTime > GlobalInfo.ServerTime)
        {
            remainingTime = endTime - GlobalInfo.ServerTime;
            time.text = $"考核倒计时：{remainingTime.ToString(@"hh\:mm\:ss")}";
            remainingSeconds = (int)remainingTime.TotalSeconds;
            //停止计时
            if (GlobalInfo.waitExam || ct.IsCancellationRequested)
                return;
            await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: ct);
        }

        time.text = $"考核倒计时：00:00:00";

        SendMsg(new MsgBase((ushort)ExamPanelEvent.LocalTimeout));

        Submit(() =>
        {
            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("确定", new PopupButtonData(() =>
            {
                Dictionary<string, PopupButtonData> popupDic1 = new Dictionary<string, PopupButtonData>();
                popupDic1.Add("确定", new PopupButtonData(Quit, true));
                UIManager.Instance.OpenUI<PopupPanel_AutoConfirm>(UILevel.PopUp, new UIAutoPopupData("提示", "考核提交成功，退出房间", popupDic1, 10, true, () =>
                {
                    Quit();
                }));
            }, true));
            UIManager.Instance.OpenUI<PopupPanel_AutoConfirm>(UILevel.PopUp, new UIAutoPopupData("提示", "考核时间到，考核结束，系统自动提交", popupDic, 10, true, () =>
            {
                Quit();
            }));
        }, false);
    }

    /// <summary>
    /// 考核结束回调
    /// </summary>
    /// <param name="stopExamId"></param>
    private void OnExamStop(int stopExamId)
    {
        Log.Debug($"[ExamCoursePanel] OnExamStop stopExamId={stopExamId} curExamId={examId} waitExam={GlobalInfo.waitExam}");
        Submit(() =>
        {
            Dictionary<string, PopupButtonData> popupDic1 = new Dictionary<string, PopupButtonData>();
            popupDic1.Add("退出房间", new PopupButtonData(Quit, true));
            UIManager.Instance.OpenUI<PopupPanel_AutoConfirm>(UILevel.PopUp, new UIAutoPopupData("提示", "房主已结束考核，系统自动提交", popupDic1, 10, true, () =>
            {
                Quit();
            }));
        }, false);
    }

    /// <summary>
    /// 房主端计时结束回调
    /// </summary>
    private void OnHostTimeout(int timeoutExamId)
    {
        Submit(() =>
        {
            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("确定", new PopupButtonData(() =>
            {
                Dictionary<string, PopupButtonData> popupDic1 = new Dictionary<string, PopupButtonData>();
                popupDic1.Add("确定", new PopupButtonData(Quit, true));
                UIManager.Instance.OpenUI<PopupPanel_AutoConfirm>(UILevel.PopUp, new UIAutoPopupData("提示", "考核提交成功，退出房间", popupDic1, 10, true, () =>
                {
                    Quit();
                }));
            }, true));
            UIManager.Instance.OpenUI<PopupPanel_AutoConfirm>(UILevel.PopUp, new UIAutoPopupData("提示", "考核时间到，考核结束，系统自动提交", popupDic, 10, true, () =>
            {
                Quit();
            }));
        }, false);
    }

    /// <summary>
    /// 考核提交回调
    /// </summary>
    /// <param name="submitUserId"></param>
    /// <param name="submitExamId"></param>
    private void OnExamSubmit(int submitUserId, int submitExamId, string name)
    {
        if (submitExamId != examId)
            return;

        //退出房间时 如果全部成员均已提交 则EndExam
        ExamUtility.Instance.UpdateSubmitCache(submitUserId);

        //小组考核中，被动提交（其他成员提交时同步提交）
        if (GlobalInfo.IsGroupMode() && submitUserId != GlobalInfo.account.id && !GlobalInfo.waitExam)
        {
            mCountdownCts?.Cancel();

            //提交考核记录
            SubmitExamRecord(true, true, null);

            ModelManager.Instance.DestroyModels(true);
            UIManager.Instance.CloseAllModuleUI(this);
            UpdateUIWhenExamStop();
            ClearExamCache();

            //显示弹窗并退出
            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("确定", new PopupButtonData(() => Quit(), true));
            UIManager.Instance.OpenUI<PopupPanel_AutoConfirm>(UILevel.PopUp, new UIAutoPopupData("提示", string.Format("考生【{0}】主动提交考核，考试结束", name), popupDic, 10, true, () => Quit()));
            return;
        }

        //自己提交
        if (submitUserId == GlobalInfo.account.id)
        {
            ModelManager.Instance.DestroyModels(true);
            UIManager.Instance.CloseAllModuleUI(this);
            GlobalInfo.currentWiki = null;
            if (mCountdownCts != null)
            {
                mCountdownCts.Cancel();
                mCountdownCts = null;
            }
            UpdateUIWhenExamStop();

            //被动退出是正常退出流程，立即删除flag
            ClearExamCache();
        }

    }

    private void UpdateUIWhenExamStop()
    {
        submit.gameObject.SetActive(false);
        this.GetComponentByChildName<Text>("Time").gameObject.SetActive(false);
        this.FindChildByName("WaitHint").gameObject.SetActive(true);
        CourseSideBar.Clear();
#if UNITY_STANDALONE
        var mid = this.GetComponentByChildName<CanvasGroup>("MidBtns");
        mid.alpha = 0.5f;
        mid.blocksRaycasts = false;
#else
        CourseSideBar.SetVisible(false);
#endif
    }
    #endregion

    #region 房间通道部分

    private Button voiceControlTog;
    private Image onAir;

    private void InitRoomChannel()
    {
        AddMsg(
            (ushort)RoomChannelEvent.UpdateMemberList,
            (ushort)MediaChannelEvent.MicOnAir,
            (ushort)RoomChannelEvent.OtherJoin,
            (ushort)RoomChannelEvent.OtherLeave,
            (ushort)RoomChannelEvent.TalkState,
            (ushort)RoomChannelEvent.LeaveRoom,
            (ushort)RoomChannelEvent.RoomInfo,
            (ushort)RoomChannelEvent.RoomClose
        );

        onAir = this.GetComponentByChildName<Image>("OnAir");
        voiceControlTog = this.GetComponentByChildName<Button>("VoiceControlTog");
        voiceControlTog.onClick.AddListener(() =>
        {
            NetworkManager.Instance.SwitchUserChat(GlobalInfo.account.id);
        });
    }

    private void RoomChannelMsg(MsgBase msg)
    {
        switch (msg.msgId)
        {
            case (ushort)RoomChannelEvent.UpdateMemberList:
                var self = NetworkManager.Instance.GetRoomMemberList().Find(value => value.Id == GlobalInfo.account.id);
                if (self != null)
                {
                    ButtonImageChange(!self.IsTalk, self.IsChat);
                }
                break;
            case (ushort)MediaChannelEvent.MicOnAir:
                if (((MsgInt)msg)?.arg == GlobalInfo.account.id)
                {
                    onAir.DOFade(1f, 0f);
                    onAir.DOFade(0f, 1f);
                }
                break;
            case (ushort)RoomChannelEvent.OtherJoin:
                break;
            case (ushort)RoomChannelEvent.OtherLeave:
                break;
            case (ushort)RoomChannelEvent.TalkState:
                if (((MsgBoolBool)msg).arg2)
                {
                    UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo(GlobalInfo.isAllTalk ? "已解除全员禁言" : "已开启全员禁言"));
                }
                var talkSelf = NetworkManager.Instance.GetRoomMemberList().Find(value => value.Id == GlobalInfo.account.id);
                if (talkSelf != null)
                {
                    ButtonImageChange(!GlobalInfo.isAllTalk, talkSelf.IsChat);
                }
                break;
            case (ushort)RoomChannelEvent.LeaveRoom:
                if (GlobalInfo.roomInfo == null) break;
                ExamScreenRecording.Instance.StopRecordMovie();
                // 考核尚未正式开始(未进入StartExam)，清除缓存不留重连入口
                if (examId == 0)
                    ClearExamCache();
                else
                    GlobalInfo.waitExam = true;
                NetworkManager.Instance.SendIMMsg(new MsgBrodcastOperate((ushort)ExamPanelEvent.Quit, JsonTool.Serializable(new MsgInt((ushort)ExamPanelEvent.Quit, examId))));
                DoQuit();
                break;
            case (ushort)RoomChannelEvent.RoomInfo:
                Title.text = (msg as MsgBrodcastOperate).GetData<MsgString>().arg;
                break;
            case (ushort)RoomChannelEvent.RoomClose:
                OnRoomClose();
                break;
        }
    }

    /// <summary>
    /// 语音按钮替换状态图片
    /// </summary>
    /// <param name="isShut">是否禁言</param>
    /// <param name="isChat">是否开启麦克风</param>
    private void ButtonImageChange(bool isShut, bool isChat)
    {
#if UNITY_ANDROID
        var text = voiceControlTog.GetComponentByChildName<Text>("VoiceText");
#endif
        string buttonState = "CloseToSpeak";
        if (isShut)
            buttonState = "BannedToPost";
        else if (isChat)
            buttonState = "OpenToSpeak";

        voiceControlTog.image.sprite = voiceControlTog.GetComponentByChildName<Image>(buttonState).sprite;
        switch (buttonState)
        {
            case "BannedToPost":
#if UNITY_ANDROID
                text.text = "禁言中";
#endif
                voiceControlTog.interactable = GlobalInfo.IsHomeowner();
                break;
            case "OpenToSpeak":
#if UNITY_ANDROID
                text.text = "开麦中";
#endif
                voiceControlTog.interactable = true;
                break;
            case "CloseToSpeak":
#if UNITY_ANDROID
                text.text = "闭麦中";
#endif
                voiceControlTog.interactable = true;
                break;
            default:
                break;
        }
    }


    /// <summary>
    /// 房间解散回调
    /// </summary>
    private void OnRoomClose()
    {
        if (GlobalInfo.waitExam)
            LeaveClosedRoom();
        else
        {
            Submit(() =>
            {
                if (!NetworkManager.Instance.IsUserOnline(GlobalInfo.roomInfo.creatorId))
                {
                    RequestManager.Instance.EndExam(examId, () =>
                    {
                        LeaveClosedRoom();
                    }, (error) =>
                    {
                        Log.Warning($"考核结束失败：{error}");
                        LeaveClosedRoom();
                    });
                }
                else
                {
                    LeaveClosedRoom();
                }
            });
        }
    }

    private void LeaveClosedRoom()
    {
        Dictionary<string, PopupButtonData> popupDic1 = new Dictionary<string, PopupButtonData>();
        popupDic1.Add("确定", new PopupButtonData(() => Quit()/*NetworkManager.Instance.EnsureLeaveRoom(string.Empty)*/, true));
        UIManager.Instance.OpenUI<PopupPanel_AutoConfirm>(UILevel.PopUp, new UIAutoPopupData("提示", "房主已解散房间", popupDic1, 10, true, () =>
        {
            Quit();
        }));
    }
    #endregion
}