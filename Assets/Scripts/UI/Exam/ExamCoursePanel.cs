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
    public override bool canOpenOption => true;//!inExam

    /// <summary>
    /// 当前考核ID
    /// 避免状态同步时，执行非当前考核的操作
    /// </summary>
    private int examId;

    /// <summary>
    /// 试卷百科对应答案列表，key：百科id，value：百科对应答案数据
    /// </summary>
    private Dictionary<int, Answer> answersDic = new Dictionary<int, Answer>();

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
        GlobalInfo.canEditUserInfo = false;
        base.Open(uiData);
        InitExam();
        InitRoomChannel();
    }

    protected override void OnPrepareShow(UIData uiData)
    {
        NetworkManager.Instance.IsIMSync = false;
        InitData(() =>
        {
            //SetTitle(GlobalInfo.currentCourseInfo);
            Title.text = GlobalInfo.roomInfo.RoomName;

            NetworkManager.Instance.EnableLocalVideo(true);

            UIManager.Instance.CloseUI<LoadingPanel>();
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
        if (GlobalInfo.roomInfo == null || GlobalInfo.roomInfo.Status != 2)
            return;

        string roomUuid = GlobalInfo.roomInfo.Uuid;
        int cachedExamId = ExamUtility.Instance.GetParticipantExamId(roomUuid);
        if (cachedExamId <= 0)
            return;

        DateTime? cachedEndTime = ExamUtility.Instance.GetParticipantExamEndTime(roomUuid);
        if (!cachedEndTime.HasValue || cachedEndTime.Value <= GlobalInfo.ServerTime)
        {
            ExamUtility.Instance.DeleteParticipantExamCache(roomUuid);
            PlayerPrefs.DeleteKey(ExamTrainingPanel.flag);
            return;
        }

        Log.Debug($"[ExamCoursePanel] 检测到考核进行中，自动重连 examId={cachedExamId}");

        // 从本地恢复 cachedPacket
        PlayerPrefs.SetString(ExamTrainingPanel.flag, roomUuid);

        var msgExamStartData = new MsgExamStart(
            (ushort)ExamPanelEvent.Start,
            cachedExamId,
            GlobalInfo.ServerTime,
            cachedEndTime.Value,
            null
        );

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
    /// 重写 考核模式下收到 BaikeSelect 消息只更新选中索引，不再触发
    /// OnBaikeChanged（销毁模型）和 LoadEncyclopedia（重复加载）
    /// 统一在收到开始考核消息后直接调用
    /// </summary>
    protected override void OnBaikeSelectEventReceived(MsgBase msg)
    {
        int baikeId = ((MsgInt)msg).arg;
        BaikeSelectModule.selectID = baikeId;
        BaikeSelectModule.CurrentBaikeIndex = GlobalInfo.currentWikiList.FindIndex(wiki => wiki.id == baikeId);
    }

    /// <summary>
    /// 清除考核缓存（flag、参与者缓存、IM缓存）
    /// </summary>
    private void ClearExamCache()
    {
        GlobalInfo.waitExam = true;
        PlayerPrefs.DeleteKey(ExamTrainingPanel.flag);
        if (GlobalInfo.roomInfo != null)
        {
            ExamUtility.Instance.DeleteParticipantExamCache(GlobalInfo.roomInfo.Uuid);
        }
    }

    private void Quit()
    {
        //退出房间，立即删除flag，避免触发异常退出提示
        ClearExamCache();
        if (!GlobalInfo.waitExam && ExamUtility.Instance.AllSubmit() && !NetworkManager.Instance.IsUserOnline(GlobalInfo.roomInfo.creatorId))
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

        GlobalInfo.SetCourseMode(CourseMode.Training);
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
        UIManager.Instance.OpenUI<LoadingPanel>(UILevel.Loading);
        bool loadNavMesh = encyclopedia.typeId == (int)PediaType.Operation && (encyclopedia as EncyclopediaOperation).hasRole;
        //ResManager.Instance.LoadModel(encyclopedia.id.ToString(), ResManager.Instance.OSSDownLoadPath + abList[0].filePath, loadNavMesh, false, (arg2) =>
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
            UIManager.Instance.CloseUI<LoadingPanel>();

            encyclopediaModelLoaded = true;

            //单人考核的重连是单独的逻辑 不是在这里处理
            if(GlobalInfo.courseMode != CourseMode.Exam)
            {
                Debug.Log("执行多人考核状态恢复");
                NetworkManager.Instance.SyncBaikeState(SetStateByHistory);
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
    /// 恢复操作记录 和对应 模型状态
    /// </summary>
    private async UniTaskVoid RecoveryExam(CancellationToken ct)
    {
        UIManager.Instance.OpenUI<LoadingPanel>();
        Canvas canvas = UIManager.Instance.canvas;

        await UniTask.WaitUntil(() => canvas.GetComponentInChildren<UISmallSceneModule>(true) != null, cancellationToken: ct);
        smallSceneModule = UIManager.Instance.canvas.GetComponentInChildren<UISmallSceneModule>(true);
        NetworkManager.Instance.IsIMSync = false;

        int flow = 0, step = 0;
        {
            //按顺序匹配：从 flows 第一个步骤开始，在 operations 中按顺序查找，找到则进度+1继续找下一个
            if (GlobalInfo.currentWiki != null && answersDic.ContainsKey(GlobalInfo.currentWiki.id))
            {
                AnswerOp savedAnswer = answersDic[GlobalInfo.currentWiki.id] as AnswerOp;
                if (savedAnswer != null && savedAnswer.operations != null && savedAnswer.operations.Count > 0)
                {
                    var flows = smallSceneModule.smallFlowCtrl.flows;
                    int opIndex = 0;
                    for (int f = 0; f < flows.Length; f++)
                    {
                        bool flowMatched = false;
                        for (int s = 0; s < flows[f].steps.Count; s++)
                        {
                            if (TryMatchStepInOps(flows[f].steps[s], savedAnswer.operations.ToList(), ref opIndex))
                            {
                                flow = f;
                                step = s;
                                flowMatched = true;
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (!flowMatched)
                            break;
                    }
                }
            }
        }
        
        Log.Debug($"考核重连恢复进度 flow:{flow} step:{step}");

        //同步操作对象状态 恢复步骤
        smallSceneModule.smallFlowCtrl.SelectFlow(flow, false);
        smallSceneModule.smallFlowCtrl.SelectStep(step, false);

        await UniTask.Delay(TimeSpan.FromSeconds(0.1));
        SetStateByHistory();
        smallSceneModule.smallFlowCtrl.Next(true);

        //完成恢复，打开消息处理
        await UniTask.Delay(TimeSpan.FromSeconds(0.1));
        UIManager.Instance.CloseUI<LoadingPanel>();
        NetworkManager.Instance.IsIMSync = true;
    }

    public void SetStateByHistory()
    {
        List<OpRecordData> opRecordData = null;
        AnswerOp answerOp = null;
        if (GlobalInfo.currentWiki != null && answersDic.ContainsKey(GlobalInfo.currentWiki.id))
        {
            answerOp = (answersDic[GlobalInfo.currentWiki.id] as AnswerOp);
            if (answerOp != null)
            {
                //同步操作记录列表
                opRecordData = answerOp.operations?.Select(data => new OpRecordData()
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
            }

            //用服务器的历史记录覆盖当前记录
            smallSceneModule.operationHistoryModule.UpdateOpRecordList(opRecordData ?? new List<OpRecordData>());

            //用操作记录还原场景变动
            smallSceneModule.smallFlowCtrl.SetFinalState(answerOp.modelStates?.Select(s => new OpDicData()
            {
                id = s.id,
                optionName = s.optionName,
                uiTargetModelEulerZ = float.Parse(s.uiTargetModelEulerZ)
            }).ToList() ?? new List<OpDicData>());
        }
    }

    /// <summary>
    /// 在操作记录中按顺序查找匹配当前步骤的 hint_success
    /// 匹配成功 opIndex 前进，失败则不移动
    /// </summary>
    private bool TryMatchStepInOps(SmallStep1 stepData, List<ExamineResultOperation> operations, ref int opIndex)
    {
        if (stepData == null || opIndex >= operations.Count)
            return false;

        for (int i = opIndex; i < operations.Count; i++)
        {
            string msg = operations[i].msg;
            if (string.IsNullOrEmpty(msg))
                continue;

            //匹配 SmallStep1.hint_success
            if (!string.IsNullOrEmpty(stepData.hint_success) && stepData.hint_success == msg)
            {
                opIndex = i + 1;
                return true;
            }

            //hint_success 为空时，匹配 ops[0].operation.operations 中的 hint_success
            if (string.IsNullOrEmpty(stepData.hint_success) && stepData.ops != null && stepData.ops.Count > 0)
            {
                var opList = stepData.ops[0].operation?.operations;
                if (opList != null)
                {
                    for (int j = 0; j < opList.Count; j++)
                    {
                        if (opList[j].hint_success == msg)
                        {
                            opIndex = i + 1;
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private void SubmitExamRecord(bool submitRecording = true, bool showToast = true, Action<bool> callBack = null)
    {
        submitCts?.Cancel();
        submitCts = new CancellationTokenSource();
        _submitExamRecord(submitRecording, showToast, callBack, submitCts.Token).Forget();
    }
    private async UniTaskVoid _submitExamRecord(bool submitRecording = true, bool showToast = true, Action<bool> callBack = null, CancellationToken ct = default)
    {
        if (GlobalInfo.currentWiki == null)
        {
            callBack?.Invoke(false);
            return;
        }
        SaveWikiRecord();

        await UniTask.WaitForEndOfFrame(this, ct);

        switch (GlobalInfo.currentWiki.typeId)
        {
            case (int)PediaType.Operation:
                SubmitOperationEncyclopedia(GlobalInfo.currentWiki.id, showToast, submitRecording, callBack);
                break;
            case (int)PediaType.Exercise:
                OPLExerciseModule exercise = GetComponentInChildren<OPLExerciseModule>();
                if(exercise != null)
                {
                    string operation = string.Empty;
                    EncyclopediaExercise encyclopediaExercise = GlobalInfo.currentWiki as EncyclopediaExercise;
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
                    SubmitExerciseEncyclopedia(GlobalInfo.currentWiki.id, operation, showToast, submitRecording, callBack);
                }            
                break;
        }  
    }

    /// <summary>
    /// 记录百科状态
    /// </summary>
    private void SaveWikiRecord()
    {
        if (GlobalInfo.currentWiki != null)
        {
            switch (GlobalInfo.currentWiki.typeId)
            {
                case (int)PediaType.Operation:
                    if(answersDic.ContainsKey(GlobalInfo.currentWiki.id))
                    {
                        AnswerOp answerOp = answersDic[GlobalInfo.currentWiki.id] as AnswerOp;
                        UISmallSceneOperationHistory his = GetComponentInChildren<UISmallSceneOperationHistory>();
                        if (his != null)
                        {
                            answerOp.operations = his.OpRecordList.Select(data => data.ToExamineResult()).ToList();
                        }
                        if (smallSceneModule != null)
                        {
                            if (smallSceneModule.smallFlowCtrl != null)
                            {
                                answerOp.modelStates = GetExamineModelStates().ToList();
                            }
                        }
                    }
                    break;
                case (int)PediaType.Exercise:
                    AnswerExercise answerExercise = answersDic[GlobalInfo.currentWiki.id] as AnswerExercise;
                    OPLExerciseModule exercise = GetComponentInChildren<OPLExerciseModule>();
                    EncyclopediaExercise encyclopediaExercise = GlobalInfo.currentWiki as EncyclopediaExercise;
                    switch (encyclopediaExercise.data.exercise.type)
                    {
                        case 1://选择题(单选;多选)
                            answerExercise.operation = exercise._selectedAnswers.Aggregate(string.Empty, (current, i) => current + ((char)('A' + i)).ToString());
                            break;
                        case 2://判断题
                            if (exercise._selectedAnswers.Count == 1)
                                answerExercise.operation = exercise._selectedAnswers[0] == 0 ? "正确" : "错误";
                            else
                                answerExercise.operation = string.Empty;
                            break;
                        case 3://操作题
                        default:
                            Log.Error("未处理题型");
                            break;
                    }
                    break;
                default:
                    Log.Error("考核百科类型错误！");
                    break;
            }
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
    /// <param name="showToast"></param>
    /// <param name="submitRecording"></param>
    /// <param name="callBack"></param>
    private void SubmitExerciseEncyclopedia(int baikeId, string operation, bool showToast, bool submitRecording, Action<bool> callBack)
    {    
        ExamUtility.Instance.SubmitExamineResult_Exercise(examId, baikeId, operation, () =>
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
                Log.Debug($"[ExamCoursePanel] 收到ExamPanelEvent.Start，waitExam={GlobalInfo.waitExam}, IsIMSync={NetworkManager.Instance.IsIMSync}");
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
            case (ushort)ExamPanelEvent.Submit:
                if (!GlobalInfo.waitExam)
                {
                    var submitMsg = msg as MsgBrodcastOperate;
                    OnExamSubmit(submitMsg.senderId, submitMsg.GetData<MsgIntString>().arg1, submitMsg.GetData<MsgIntString>().arg2);
                }
                break;
        }
    }

    /// <summary>
    /// 考核开始回调
    /// </summary>
    private void OnExamStart(MsgExamStart msgExamStartData)
    {
        Log.Debug($"[ExamCoursePanel] OnExamStart examId={msgExamStartData.examId}, waitExam={GlobalInfo.waitExam}");
        UIManager.Instance.CloseUI<LoadingPanel>();
        this.FindChildByName("WaitHint").gameObject.SetActive(false);
        RequestManager.Instance.GetExamination(msgExamStartData.examId, (examination) =>
        {
            GlobalInfo.SaveExaminationInfo(examination);
            GlobalInfo.currentWikiList = examination.encyclopediaList;

            if (GlobalInfo.currentWikiList == null || GlobalInfo.currentWikiList.Count == 0)
            {
                var popupDic = new Dictionary<string, PopupButtonData>();
                {
                    popupDic.Add("确定", new PopupButtonData(Quit, true));
                    UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "该考核未添加", popupDic, null, false));
                }
            }
            //仅允许在未开始考核时检查重连和开始考核
            else
            {
                // 考核已开始（waitExam=false），仍需初始化 examineeRecords 用于提交记录
                if (msgExamStartData.examineeRecords != null && msgExamStartData.examineeRecords.Count > 0)
                {
                    ExamUtility.Instance.InitSubmitCache(msgExamStartData.examineeRecords);
                }

                if (GlobalInfo.waitExam)
                {
                    Log.Debug($"[ExamCoursePanel] OnExamStart 进入考核流程，waitExam=true，将设为false");
                    GlobalInfo.waitExam = false;
                    PlayerPrefs.SetString(ExamTrainingPanel.flag, GlobalInfo.roomInfo.Uuid);

                    // 保存参与者考核缓存，用于异常退出后自动重连
                    ExamUtility.Instance.SetParticipantExamCache(
                        GlobalInfo.roomInfo.Uuid,
                        msgExamStartData.examId,
                        msgExamStartData.endTime
                    );

                    //先获取已提交的考核记录，根据结果判断是否重连
                    ExamUtility.Instance.GetExamineResult(msgExamStartData.examId, (id, answers, accessories) =>
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

                        bool isRecovery = answers != null && answers.Any(a =>
                            a is AnswerOp op && op.operations != null && op.operations.Count > 0);

                        if (isRecovery)
                        {
                            //重连：跳过倒计时，直接开始考核并恢复状态
                            this.FindChildByName("StartTiming").gameObject.SetActive(false);
                            StartExam(msgExamStartData);
                            if (GlobalInfo.courseMode == CourseMode.Exam)
                            {
                                //如果是联机考核，直接获取最终步骤来获取进度 这里是单独处理单人考核这种特殊类型 同一个房间，但是有各自的进度
                                Debug.Log("执行单人考核状态恢复");
                                RecoveryExam(this.GetCancellationTokenOnDestroy()).Forget();
                            }
                        }
                        else
                        {
                            //正常开始：保存房间信息，走倒计时流程
                            StartTiming(() =>
                            {
                                StartExam(msgExamStartData);
                            });
                        }
                    }, error =>
                    {
                        Log.Error($"获取考核[{examId}]结果失败 {error}");
                        StartTiming(() =>
                        {
                            StartExam(msgExamStartData);
                        });
                    });
                }
               
            }
        }, (error) => {
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
        GlobalInfo.waitExam = false;

        //主动加载百科模型（现在不在房间内切百科了，房主重连时不发送百科选择消息了）
        int baikeId = 0;
        var activeAnswer = answersDic.Values.FirstOrDefault(a => a is AnswerOp op && op.operations?.Count > 0);
        if (activeAnswer != null)
            baikeId = activeAnswer.baikeId;
        else if (GlobalInfo.currentWikiList?.Count > 0)
            baikeId = GlobalInfo.currentWikiList[0].id;

        if (baikeId != 0)
        {
            wikiInitialized = false;
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
        var side = this.GetComponentByChildName<CanvasGroup>("SideBar");
        side.alpha = 1;
        side.interactable = true;
#endif
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

        //自己提交
        if (GlobalInfo.IsGroupMode() || submitUserId == GlobalInfo.account.id)
        {
            ModelManager.Instance.DestroyModels(true);
            PlayerManager.Instance.ClearUserIndicators();
            UIManager.Instance.CloseAllModuleUI(this);
            GlobalInfo.currentWiki = null;
            if (mCountdownCts != null)
            {
                mCountdownCts.Cancel();
                mCountdownCts = null;
            }
            UpdateUIWhenExamStop();
        }

        //小组考核，有成员提交时，其他成员同步提交
        if (GlobalInfo.IsGroupMode() && !GlobalInfo.waitExam)
        {
            //立即显示弹窗，不依赖提交完成
            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("确定", new PopupButtonData(() => Quit(), true));
            UIManager.Instance.OpenUI<PopupPanel_AutoConfirm>(UILevel.PopUp, new UIAutoPopupData("提示", string.Format("考生【{0}】主动提交考核，考试结束", name), popupDic, 10, true, () => Quit()));
        }

        //被动退出是正常退出流程，立即删除flag
        ClearExamCache();
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
        var side = this.GetComponentByChildName<CanvasGroup>("SideBar");
        side.alpha = 1f;
        side.interactable = false;
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
                if (!GlobalInfo.waitExam)
                {
                    int joinedUser = ((MsgIntString)msg).arg1;
                    if (joinedUser == GlobalInfo.roomInfo.creatorId)
                    {
                        // 通知房主同步考核剩余时长
                        NetworkManager.Instance.SendIMMsg(new MsgBrodcastOperate((ushort)ExamPanelEvent.Resume, JsonTool.Serializable(new MsgInt((ushort)ExamPanelEvent.Resume, remainingSeconds))));
                    }
                }
                break;
            case (ushort)RoomChannelEvent.OtherLeave:
                break;
            case (ushort)RoomChannelEvent.TalkState:
                if (((MsgBoolBool)msg).arg2)
                {
                    UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo(GlobalInfo.isAllTalk ? "已解除全员禁言" : "已开启全员禁言"));
                }
                break;
            case (ushort)RoomChannelEvent.LeaveRoom:
                if (GlobalInfo.roomInfo == null) break;
                ExamScreenRecording.Instance.StopRecordMovie();
                ClearExamCache();
                if (!GlobalInfo.waitExam)
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