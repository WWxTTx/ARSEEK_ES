using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;
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

    private bool logout;

    public override void Open(UIData uiData = null)
    {
        GlobalInfo.Loading = true;
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
            GlobalInfo.Loading = false;
            NetworkManager.Instance.IsIMSync = true;
        });
    }

    protected override void SetTitle(Course course)
    {
        base.SetTitle(course);
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
    /// 退出房间
    /// </summary>
    private void Quit()
    {
        if (inExam && ExamUtility.Instance.AllSubmit() && !NetworkManager.Instance.IsUserOnline(GlobalInfo.roomInfo.creatorId))
        {
            NetworkManager.Instance.SendIMMsg(new MsgBrodcastOperate((ushort)ExamPanelEvent.Flush, JsonTool.Serializable(new MsgBase((ushort)ExamPanelEvent.Flush))));
            NetworkManager.Instance.SendIMMsg(new MsgBrodcastOperate((ushort)ExamPanelEvent.Quit, JsonTool.Serializable(new MsgInt((ushort)ExamPanelEvent.Quit, examId))));

            RequestManager.Instance.EndExam(examId, () =>
            {
                StartCoroutine(_Quit());
            }, (error) =>
            {
                Log.Error($"考核结束答题失败：{error}");
                StartCoroutine(_Quit());
            });
        }
        else
        {
            NetworkManager.Instance.SendIMMsg(new MsgBrodcastOperate((ushort)ExamPanelEvent.Quit, JsonTool.Serializable(new MsgInt((ushort)ExamPanelEvent.Quit, examId))));
            StartCoroutine(_Quit());
        }
    }

    private IEnumerator _Quit()
    {
        //等待消息发送完成
        yield return new WaitUntil(() => NetworkManager.Instance.SendOpCount == 0);
        NetworkManager.Instance.ReleaseMicrophone();
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
        //UIManager.Instance.CloseUI<ExamCoursePanel>();
        if (logout)
            ToolManager.GoToLogin();
        else
            UIManager.Instance.OpenUI<ExamTrainingPanel>();
    }

    public override void Previous()
    {
        if (inExam)
        {
            var popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("取消", new PopupButtonData(null, false));
            popupDic.Add("退出房间", new PopupButtonData(() =>
            {
                Submit(Quit);
            }, true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "考核时间还未结束，确定提交考核并退出房间？", popupDic, showCloseBtn: false));
        }
        else
        {
            var popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("取消", new PopupButtonData(null, false));
            popupDic.Add("退出房间", new PopupButtonData(() =>
            {
                Quit();
            }, true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "确定退出房间？", popupDic, showCloseBtn: false));
        }
    }


    public override void GotoLogout()
    {
        Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
        popupDic.Add("取消", new PopupButtonData(null));
        popupDic.Add("退出登录", new PopupButtonData(() =>
        {
            logout = true;
            Quit();
        }, true));
        UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "确定退出登录？", popupDic, showCloseBtn: false));//\n（退出登录没有考核成绩）
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

    protected override void OnBaikeSelectEventReceived(MsgBase msg)
    {
        //未开始考核时，不切换百科
        if (!inExam)
        {
            NetworkManager.Instance.IsIMSync = true;
            return;
        }

        int baikeId = ((MsgBrodcastOperate)msg).GetData<MsgInt>().arg;
        if (!NetworkManager.Instance.IsIMSyncState)
        {
            UIManager.Instance.OpenUI<LoadingPanel>(UILevel.Loading);
            //切换百科前，提交考核记录
            SubmitExamRecord(true, false, (submitSuccess) =>
            {
                UIManager.Instance.CloseUI<LoadingPanel>();
                ChangeBaike(baikeId);
            });
        }
        else
        {
            ChangeBaike(baikeId);
        }  
    }

    /// <summary>
    /// 加载百科
    /// </summary>
    /// <param name="baikeId"></param>
    private void ChangeBaike(int baikeId)
    {
        Log.Debug("选择百科");
        OnBaikeChanged(baikeId);

        this.WaitTime(0.1f, () =>
        {
            wikiInitialized = false;
            LoadEncyclopedia(baikeId);
            //还原考核百科状态
            StartCoroutine(RecoveryExamWiki());
        });
    }

    /// <summary>
    /// 加载百科（习题、模拟操作）
    /// </summary>
    /// <param name="encyclopediaId">百科id</param>
    private void LoadEncyclopedia(int encyclopediaId)
    {
        //if (!wikiDic.ContainsKey(encyclopediaId))
        //{
        //    Log.Error($"未找到百科: {encyclopediaId}");
        //    return;
        //}
        //GlobalInfo.currentWiki = wikiDic[encyclopediaId];
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
                Log.Error(string.Format("百科{0}实例化失败", encyclopedia.id));
                UIManager.Instance.CloseUI<LoadingPanel>();
                NetworkManager.Instance.IsIMSync = true;
                return;
            }

            firstBaike = false;
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

            //等待FlowModule操作列表初始化完成
            this.WaitTime(0.15f, () =>
            {
                //个人考核 提交考核记录
                if (!GlobalInfo.IsGroupMode())
                {
                    // 每次操作记录变化时提交 
                    smallSceneModule.operationHistoryModule.OnRecordChanged.RemoveAllListeners();
                    smallSceneModule.smallFlowCtrl.OnFreeOperationInvoked.RemoveAllListeners();
                    smallSceneModule.operationHistoryModule.OnRecordChanged.AddListener(( recordData) =>
                    {
                        ExamUtility.Instance.EnqueueOperation(examId, GlobalInfo.currentWiki.id, recordData, GetExamineModelStates());
                    });
                    smallSceneModule.smallFlowCtrl.OnFreeOperationInvoked.AddListener(() =>
                    {
                        ExamUtility.Instance.EnqueueOperation(examId, GlobalInfo.currentWiki.id, null, GetExamineModelStates());
                    });
                }
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
    /// 是否开始考核
    /// </summary>
    private bool inExam;
    /// <summary>
    /// 是否已经提交成绩
    /// </summary>
    private bool inSubmit = false;
    /// <summary>
    /// 考核结束时间
    /// </summary>
    private DateTime endTime;
    /// <summary>
    /// 提交考核按钮
    /// </summary>
    private Button submit;

    private Coroutine mCountdownCoroutine;
    /// <summary>
    /// 剩余时长
    /// </summary>
    private int remainingSeconds;

    /// <summary>
    /// 考生是否主动提交(主动提交、退出房间或本地计时结束)
    /// </summary>
    private bool selfSumbit;

    private void InitExam()
    {
        AddMsg(new ushort[]{
            (ushort)ExamPanelEvent.Start,
            (ushort)ExamPanelEvent.Stop,
            (ushort)ExamPanelEvent.Timeout,
            (ushort)ExamPanelEvent.Submit,
            (ushort)ExamPanelEvent.ExerciseScore,
            (ushort)SmallFlowModuleEvent.CompleteStep,
            (ushort)SmallFlowModuleEvent.SelectStep
        });

        submit = this.GetComponentByChildName<Button>("Submit");
        {
            submit.onClick.AddListener(() =>
            {
                selfSumbit = true;
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
        if (inSubmit)
        {
            Log.Warning("重复提交");
            return;
        }
        inSubmit = true;
        SubmitExamRecord(true, true, (submitSuccess) =>
        {
            if (submitSuccess)
            {
                PlayerPrefs.DeleteKey(ExamTrainingPanel.flag);
                NetworkManager.Instance.SendIMMsg(new MsgBrodcastOperate((ushort)ExamPanelEvent.Submit, JsonTool.Serializable(new MsgInt((ushort)ExamPanelEvent.Submit, examId))));
            }
            callBack?.Invoke();
            inSubmit = false;
            //todo:Quit方法中考生结束答题时作为判断条件
            //inExam = false;
        });
    }

    /// <summary>
    /// 恢复百科状态
    /// </summary>
    /// <returns></returns>
    private IEnumerator RecoveryExamWiki()
    {
        //等待百科初始化完成
        yield return new WaitUntil(() => wikiInitialized);

        if (GlobalInfo.currentWiki != null && answersDic.ContainsKey(GlobalInfo.currentWiki.id))
        {
            switch (GlobalInfo.currentWiki.typeId)
            {
                case (int)PediaType.Operation:
                    AnswerOp answerOp = (answersDic[GlobalInfo.currentWiki.id] as AnswerOp);
                    if (answerOp != null)
                    {
                        //同步操作记录列表
                        List<OpRecordData> opRecordData = answerOp.operations?.Select(data => new OpRecordData()
                        {
                            index = data.index,
                            msg = data.msg,
                            userNo = data.userNo,
                            userName = data.userName,
                            createTime = data.createTime,
                            type = data.type
                        }).ToList();
                        if (opRecordData != null)
                        {
                            smallSceneModule.operationHistoryModule.UpdateOpRecordList(opRecordData);
                        }
                        //同步操作对象状态
                        if (answerOp.modelStates != null)
                        {
                            smallSceneModule.smallFlowCtrl.SetFinalState(answerOp.modelStates.Select(s=>new OpDicData()
                            {
                                id = s.id,
                                optionName = s.optionName,
                                uiTargetModelEulerZ = float.Parse(s.uiTargetModelEulerZ)
                            }).ToList(), 0, 0, null);
                        }
                        //同步仿真系统状态
                        if (answerOp.modelStates != null)
                        {
                            var systemState = answerOp.modelStates.Find(s => s.id.Equals("SimuSystemState"));
                            if (systemState != null && !string.IsNullOrEmpty(systemState.optionName))
                            {
                                smallSceneModule.simuSystem?.RecoverSystem(systemState.optionName);
                            }
                            //var fatal = answerOp.modelStates.Find(s => s.id.Equals("FatalFinishMessage"));
                            //if (fatal != null)
                            //{
                            //    smallSceneModule.FatalFinishMessage = fatal.optionName;
                            //}
                        }
                    }
                    break;
                case (int)PediaType.Exercise:
                    AnswerExercise answerExercise = (answersDic[GlobalInfo.currentWiki.id] as AnswerExercise);
                    if (answerExercise != null && !string.IsNullOrEmpty(answerExercise.operation))
                    {
                        OPLExerciseModule exerciseModule = GetComponentInChildren<OPLExerciseModule>();
                        if(exerciseModule != null)
                        {
                            switch ((GlobalInfo.currentWiki as EncyclopediaExercise).data.exercise.type)
                            {
                                case 1://选择题(单选;多选)
                                    List<int> ints = new List<int>();
                                    foreach (var str in answerExercise.operation)
                                        ints.Add((int)str - 65);
                                    exerciseModule.SelectAnswerToggles(ints);
                                    break;
                                case 2://判断题
                                    exerciseModule.SelectAnswerToggles(new List<int>() { answerExercise.operation.Equals("正确") ? 0 : 1 });
                                    break;
                            }
                        }
                    }
                    break;
            }
        }
        else
        {
            Log.Error($"还原考核状态失败 {GlobalInfo.currentWiki?.id}");
        }
        //还原考核提交状态后，再恢复执行后续状态同步操作（未提交）
        NetworkManager.Instance.IsIMSync = true;
    }

    private Coroutine submitCoroutine;
    /// <summary>
    /// 提交考核记录
    /// </summary>
    /// <param name="submitRecording">是否提交监控视频</param>
    /// <param name="showToast">是否弱提示</param>
    /// <param name="callBack"></param>
    private void SubmitExamRecord(bool submitRecording = true, bool showToast = true, Action<bool> callBack = null)
    {
        if (submitCoroutine != null)
        {
            StopCoroutine(submitCoroutine);
            submitCoroutine = null;
        }
        submitCoroutine = StartCoroutine(_submitExamRecord(submitRecording, showToast, callBack));
    }
    private IEnumerator _submitExamRecord(bool submitRecording = true, bool showToast = true, Action<bool> callBack = null)
    {
        if (GlobalInfo.currentWiki == null)
        {
            callBack?.Invoke(false);
            yield break;
        }

        SaveWikiRecord();


        //手机热闪退 暂时停用该功能
        #region 结束录屏并上传
        //if (GlobalInfo.currentWiki.typeId == (int)PediaType.Operation && submitRecording && GlobalInfo.ExamRecording)
        //{
        //    int uploadingBaikeId = GlobalInfo.currentWiki.id;
        //    ExamScreenRecording.Instance.StopRecordMovie();
        //    // 等待停止录制、文件写入完成后再合并视频，避免文件无法访问导致合并失败
        //    yield return new WaitUntil(() => !ExamScreenRecording.Instance.FileWriting && !ExamScreenRecording.Instance.Merging);
        //    string uploadFilePath = ExamScreenRecording.Instance.MergeVideo();

        //    //上传视频文件到阿里云/MinIO
        //    ExamScreenRecording.Instance.Upload(examId, uploadingBaikeId, uploadFilePath, (baikeId, objectName) =>
        //    {
        //        if (!string.IsNullOrEmpty(objectName))
        //        {
        //            var key = new Tuple<int, string>(baikeId, objectName);
        //            if (!videoDic.ContainsKey(key))
        //                videoDic.Add(key, false);
        //        }
        //    });
        //}
        ////等待全部文件合并、上传完成
        //if (submitRecording && GlobalInfo.ExamRecording && showToast)
        //{
        //    UIManager.Instance.OpenUI<LoadingPanel>(UILevel.Loading, new LoadingPanel.LoadingData() { msg = "提交中" });
        //    yield return new WaitUntil(() => ExamScreenRecording.Instance.TaskCompleted);
        //    UIManager.Instance.CloseUI<LoadingPanel>();
        //}
        #endregion

        yield return new WaitForEndOfFrame();

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
        ExamUtility.Instance.SubmitExamineResult_Operation(examId, baikeId, GetExamineModelStates(), () =>
        {
            Log.Debug($"考核{examId} 百科:{baikeId} 考核记录提交成功");

            if (!GlobalInfo.ExamRecording)
            {
                if (showToast)
                    UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo("考核记录提交成功！"));
                callBack?.Invoke(true);
                return;
            }

            #region 提交考核附件
            //每次提交 检查是否存在已上传成功但未记录的监控视频
            List<Accessory> accessoryList = videoDic.Where(v => !v.Value)
                .Select(v => new Accessory() { encyclopediaId = v.Key.Item1, filePath = v.Key.Item2 }).ToList();
            RequestManager.Instance.SubmitExamAccessory(examId, accessoryList, () =>
            {
                //标记已成功提交的视频
                foreach (var accessory in accessoryList)
                {
                    var video = videoDic.FirstOrDefault(v => v.Key.Item2.Equals(accessory.filePath));
                    if (videoDic.ContainsKey(video.Key))
                        videoDic[video.Key] = true;
                }

                if (showToast)
                    UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo("考核记录提交成功！"));
                callBack?.Invoke(true);
            }, (errorCode, errorMsg) =>
            {
                Log.Error($"考核{examId} 百科:{baikeId} 考核附件提交失败");
                if (showToast)
                    UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo("考核记录提交成功！"));
                callBack?.Invoke(true);
            });
            #endregion
        },
         (errorCode, errorMsg) =>
         {
             inSubmit = false;
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
            inSubmit = false;
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

        return modelStates.Select(m => new ExamineResultModelState()
        {
            id = m.id,
            index = modelStates.IndexOf(m),
            optionName = m.optionName,
            uiTargetModelEulerZ = m.uiTargetModelEulerZ.ToString()
        }).ToArray();
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
                OnExamStart((msg as MsgBrodcastOperate).GetData<MsgExamStart>());
                break;
            case (ushort)ExamPanelEvent.Stop:
                OnExamStop((msg as MsgBrodcastOperate).GetData<MsgInt>().arg);
                break;
            case (ushort)ExamPanelEvent.Timeout://房主端计时结束
                OnHostTimeout((msg as MsgBrodcastOperate).GetData<MsgInt>().arg);
                break;
            case (ushort)ExamPanelEvent.Submit:
                var submitMsg = msg as MsgBrodcastOperate;
                OnExamSubmit(submitMsg.senderId, submitMsg.GetData<MsgInt>().arg);
                break;
        }
    }

    /// <summary>
    /// 考核开始回调
    /// </summary>
    private void OnExamStart(MsgExamStart msgExamStartData)
    {
        if (!inExam)
        {
            UIManager.Instance.CloseUI<LoadingPanel>();
            this.FindChildByName("WaitHint").gameObject.SetActive(false);
            inExam = true;
            inSubmit = false;
            RequestManager.Instance.GetExamination(msgExamStartData.examId, (examination) =>
            {
                GlobalInfo.SaveExaminationInfo(examination);
                GlobalInfo.currentWikiList = examination.encyclopediaList;

                if (GlobalInfo.currentWikiList == null || GlobalInfo.currentWikiList.Count == 0)
                {
                    var popupDic = new Dictionary<string, PopupButtonData>();
                    {
                        popupDic.Add("确定", new PopupButtonData(Quit, true));
                        UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "该考核未添加习题", popupDic, null, false));
                    }
                }
                else
                {
                    ExamUtility.Instance.InitSubmitCache(msgExamStartData.examineeRecords);
                    StartTiming(() =>
                    {
                        StartExam(msgExamStartData);

                        //获取已提交的考核记录，用于还原百科操作记录列表
                        if (GlobalInfo.IsGroupMode())
                        {
                            ExamUtility.Instance.GetExamineResult(examId, OnExamRecordFetched, error =>
                            {
                                Log.Error($"获取考核[{examId}]结果失败, {error}");
                                NetworkManager.Instance.IsIMSync = true;
                            });
                        }
                        else
                        {
                            int recordId = ExamUtility.Instance.GetUserRecordId(GlobalInfo.account.id);
                            if(recordId != -1)
                            {
                                ExamUtility.Instance.GetExamineResultByRecordId(recordId, OnExamRecordFetched, error =>
                                {
                                    Log.Error($"获取考核[{examId}]结果失败, {error}");
                                    NetworkManager.Instance.IsIMSync = true;
                                });
                            }
                            else
                            {
                                //未参与考核？
                                Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
                                popupDic.Add("好的", new PopupButtonData(Quit, true));
                                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("错误", "请确认是否已经参加该次考核", popupDic, showCloseBtn: false));
                            }
                        }
                    });
                }
            }, (error) =>{
                Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
                popupDic.Add("好的", new PopupButtonData(Quit, true));
                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("错误", "获取考试信息失败！请重新加入房间", popupDic, showCloseBtn: false));
                Log.Error($"获取考试[{msgExamStartData.examId}]信息失败！原因为：{error}");
            });
        }
        else
        {
            NetworkManager.Instance.IsIMSync = true;
        }

        PlayerPrefs.SetString(ExamTrainingPanel.flag, JsonTool.Serializable(new Dictionary<string, int>()
        {
            { GlobalInfo.roomInfo.Uuid, GlobalInfo.account.id }
        }));
    }

    /// <summary> 
    /// 考核开始倒计时
    /// </summary>
    /// <param name="callBack"></param>
    private void StartTiming(UnityAction callBack)
    {
        Transform startTimingTrans = this.FindChildByName("StartTiming");
        var text = this.GetComponentByChildName<Text>("StartTimingText");
        // 状态同步，跳过倒计时
        if (NetworkManager.Instance.IsIMSyncState)
        {
            startTimingTrans.gameObject.SetActive(false);
            callBack?.Invoke();
            return;
        }
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
    /// 考核结果获取成功回调
    /// </summary>
    /// <param name="examId"></param>
    /// <param name="answers"></param>
    /// <param "accessories"></param>
    private void OnExamRecordFetched(int examId, List<Answer> answers, List<Accessory> accessories)
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
        else
        {
            //清空缓存的非本场考核的操作记录
            var keys = answersDic.Keys.ToList();
            for (int i = 0; i < keys.Count; i++)
            {
                if (answersDic[keys[i]] is AnswerOp)
                {
                    (answersDic[keys[i]] as AnswerOp).operations = null;
                }
            }
        }

        if (accessories != null)
        {
            //记录已提交的监控视频
            foreach (var accessory in accessories)
            {
                var key = new Tuple<int, string>(accessory.encyclopediaId, accessory.filePath);
                if (videoDic.ContainsKey(key))
                    videoDic[key] = true;
                else
                    videoDic.Add(key, true);
            }
        }
        NetworkManager.Instance.IsIMSync = true;
    }

    /// <summary>
    /// 开始考核
    /// </summary>
    /// <param name="data"></param>
    private void StartExam(MsgExamStart data)
    {
        submit.gameObject.SetActive(true);
        examId = data.examId;
        endTime = data.endTime;
        ExamScreenRecording.Instance.ExamId = examId;

        if (mCountdownCoroutine != null)
        {
            StopCoroutine(mCountdownCoroutine);
            mCountdownCoroutine = null;
        }
        mCountdownCoroutine = StartCoroutine(Timing(data.endTime));

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
    private IEnumerator Timing(DateTime endTime)
    {
        var time = this.GetComponentByChildName<Text>("Time");
        time.gameObject.SetActive(true);

        var wait = new WaitForSeconds(1);
        TimeSpan remainingTime;
        while (endTime > GlobalInfo.ServerTime)
        {
            remainingTime = endTime - GlobalInfo.ServerTime;
            time.text = $"考核倒计时：{remainingTime.ToString(@"hh\:mm\:ss")}";
            remainingSeconds = (int)remainingTime.TotalSeconds;
            //停止计时
            if (inSubmit)
                yield break;
            yield return wait;
        }

        time.text = $"考核倒计时：00:00:00";

        SendMsg(new MsgBase((ushort)ExamPanelEvent.LocalTimeout));

        selfSumbit = true;
        Submit(() =>
        {
            //var popupDic = new Dictionary<string, PopupButtonData>();
            //popupDic.Add("知道了", new PopupButtonData(() =>
            //{
            //    var popupDic = new Dictionary<string, PopupButtonData>();
            //    popupDic.Add("确定", new PopupButtonData(Quit, true));
            //    UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "考核提交成功，退出房间", popupDic, showCloseBtn: false));
            //}, true));
            //UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "考核时间到，考核结束，系统自动提交", popupDic, showCloseBtn: false));


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
        if (!inExam || stopExamId != examId || inSubmit || selfSumbit)
            return;
        if (inExam)
        {
            Submit(() =>
            {
                //var popupDic = new Dictionary<string, PopupButtonData>();
                //popupDic.Add("退出房间", new PopupButtonData(Quit, true));
                //UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "房主已结束考核，系统自动提交", popupDic, showCloseBtn: false));//考卷。\n去往【考核记录】查看成绩

                Dictionary<string, PopupButtonData> popupDic1 = new Dictionary<string, PopupButtonData>();
                popupDic1.Add("退出房间", new PopupButtonData(Quit, true));
                UIManager.Instance.OpenUI<PopupPanel_AutoConfirm>(UILevel.PopUp, new UIAutoPopupData("提示", "房主已结束考核，系统自动提交", popupDic1, 10, true, () =>
                {
                    Quit();
                }));
            }, false);
        }
    }

    /// <summary>
    /// 房主端计时结束回调
    /// </summary>
    private void OnHostTimeout(int timeoutExamId)
    {
        if (!inExam || timeoutExamId != examId)
            return;

        Submit(() =>
        {
            //var popupDic = new Dictionary<string, PopupButtonData>();
            //popupDic.Add("知道了", new PopupButtonData(() =>
            //{
            //    var popupDic = new Dictionary<string, PopupButtonData>();
            //    popupDic.Add("确定", new PopupButtonData(Quit, true));
            //    UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "考核提交成功，退出房间", popupDic, showCloseBtn: false));
            //}, true));
            //UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "考核时间到，考核结束，系统自动提交", popupDic, showCloseBtn: false));


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
    private void OnExamSubmit(int submitUserId, int submitExamId)
    {            
        if (submitExamId != examId)
            return;
        //todo 记录本场考核已提交人员
        //退出房间时 如果全部成员均已提交 则EndExam
        ExamUtility.Instance.UpdateSubmitCache(submitUserId);

        //状态同步，考核已提交
        if (NetworkManager.Instance.IsIMSyncState)
        {
            if (GlobalInfo.IsGroupMode() || submitUserId == GlobalInfo.account.id)
            {
                inExam = false;
                ModelManager.Instance.DestroyModels(true);
                UIManager.Instance.CloseAllModuleUI(this);
                GlobalInfo.currentWiki = null;
                if (mCountdownCoroutine != null)
                {
                    StopCoroutine(mCountdownCoroutine);
                    mCountdownCoroutine = null;
                }
                UpdateUIWhenExamStop();
            }
        }
        else
        {
            //小组考核，有成员提交时，其他成员同步提交
            if (GlobalInfo.IsGroupMode() && inExam)
            {
                Submit(() =>
                {
                    var popupDic = new Dictionary<string, PopupButtonData>();
                    popupDic.Add("确定", new PopupButtonData(() =>
                    {
                        Quit();
                    }, true));
                    UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "考核提交成功，退出房间", popupDic, showCloseBtn: false));
                }, false);
            }
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
                if (inExam && !inSubmit)
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
                ExamScreenRecording.Instance.StopRecordMovie();
                ExitRoom();
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
        if (inExam)
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
                        Log.Warning($"考核结束答题失败：{error}");
                        LeaveClosedRoom();
                    });
                }
                else
                {
                    LeaveClosedRoom();
                }
            });
        }
        else
        {
            LeaveClosedRoom();
        }
    }

    private void LeaveClosedRoom()
    {
        Dictionary<string, PopupButtonData> popupDic1 = new Dictionary<string, PopupButtonData>();
        popupDic1.Add("确定", new PopupButtonData(() => Quit()/*NetworkManager.Instance.EnsureLeaveRoom(string.Empty)*/, true));
        UIManager.Instance.OpenUI<PopupPanel_AutoConfirm>(UILevel.PopUp, new UIAutoPopupData("提示", "房主已解散房间", popupDic1, 10, true, () =>
        {
            Quit();
            //NetworkManager.Instance.EnsureLeaveRoom(string.Empty);
        }));
    }
    #endregion
}