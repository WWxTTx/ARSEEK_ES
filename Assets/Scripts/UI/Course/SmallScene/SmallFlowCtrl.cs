using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;

/// <summary>
/// 步骤控制器 流程
/// </summary>
public class SmallFlowCtrl : MonoBase
{
    #region 常量 操作名称
    public static string focusFlag = "聚焦";
    public static string contactFlag = "联系";
    public static string historyFlag = "操作记录";
    public static string observeFlag = "观察";
    public static string inputFlag = "输入";
    public static string clickFlag = "点击";
    public static string switchOpenFlag = "打开";
    public static string switchCloseFlag = "关闭";
    public static string switchOnFlag = "合闸";
    public static string switchOffFlag = "分闸";
    public static string handFlag = "手部操作";
    public static string pickupFlag = "拾取";
    public static string usedFlag = "消耗";
    public static string retrieveFlag = "收回";
    public static string enterFlag = "进入";
    public static string exitFlag = "退出";
    public static string showFlag = "显示";
    public static string hideFlag = "隐藏";
    public static string backpackFlag = "工具箱";
    public static string navigationFlag = "导航";
    public static string drawingFlag = "图纸";
    public static string masterFlag = "上位机";
    public static string selectFlag = "选中";
    public static string unselectFlag = "取消选中";
    #region 初始视角特殊处理
    public static string cameraFlag = "相机位置";
    public static string playerFlag = "角色位置";
    public static string naviFlag = "导航1";
    #endregion
    #endregion

    /// <summary>
    /// 道具状态、背包工具模式排除的操作
    /// </summary>
    public static List<string> maskOperation;

    static SmallFlowCtrl()
    {
        maskOperation = new List<string>();
        {
            maskOperation.Add(showFlag);
            maskOperation.Add(hideFlag);
            //maskOperation.Add(enterFlag);
            //maskOperation.Add(exitFlag);
            maskOperation.Add(observeFlag);
            maskOperation.Add(pickupFlag);
            maskOperation.Add(usedFlag);
            maskOperation.Add(retrieveFlag);
            maskOperation.Add(focusFlag);
            maskOperation.Add(inputFlag);
            maskOperation.Add(clickFlag);
            maskOperation.Add(navigationFlag);
            maskOperation.Add(selectFlag);
            maskOperation.Add(unselectFlag);
        }
    }

    /// <summary>
    /// 拾取操作回调
    /// </summary>
    public UnityEvent<ModelOperation, bool> onPickup = new UnityEvent<ModelOperation, bool>();
    /// <summary>
    /// 消耗操作回调
    /// </summary>
    public UnityEvent<ModelOperation, bool> onUsed = new UnityEvent<ModelOperation, bool>();
    /// <summary>
    /// 重置消耗品数量事件
    /// </summary>
    public UnityEvent<ModelInfo> onResetToolNum = new UnityEvent<ModelInfo>();

    /// <summary>
    /// 执行自由操作事件（不计入操作记录列表）
    /// </summary>
    public UnityEvent OnFreeOperationInvoked = new UnityEvent();

    /// <summary>
    /// 图纸添加事件：参数-添加的图纸ModelInfo
    /// </summary>
    public UnityEvent<ModelInfo> onSchematicAdded = new UnityEvent<ModelInfo>();

    /// <summary>
    /// 任务集合
    /// </summary>
    public SmallFlow1[] flows;
    /// <summary>
    /// 全局视角
    /// </summary>
    public ModelOperation globalPerspective;
    /// <summary>
    /// 导航点集合
    /// </summary>
    public List<NavigationPoint> naviPoints;
    /// <summary>
    /// 所有操作道具集合
    /// </summary>
    public Dictionary<string, ModelOperation> operationIDs;
    /// <summary>
    /// 所有操作UI触发道具及联动物体集合
    /// </summary>
    public Dictionary<string, Transform> uiRotateModels = new Dictionary<string, Transform>();
    /// <summary>
    /// 所有自动触发道具集合
    /// </summary>
    public Dictionary<string, ModelOperation> autoProps;
    /// <summary>
    /// 背包道具集合
    /// </summary>
    public Dictionary<string, ModelInfo> toolIDs;
    /// <summary>
    /// 小地图相机正交大小
    /// </summary>
    public int orthographicSize = 10;

    /// <summary>
    /// 当前任务步骤集合
    /// </summary>
    public List<SmallStep1> nowFlowSteps
    {
        get
        {
            if (flows != null && index_NowFlow >= 0 && index_NowFlow < flows.Length)
                return flows[index_NowFlow].steps;
            else
                return null;
        }
    }
    /// <summary>
    /// 当前任务步骤
    /// </summary>
    public SmallStep1 nowFlowStep
    {
        get
        {
            if (nowFlowSteps != null && index_NowStep >= 0 && index_NowStep < nowFlowSteps.Count)
                return nowFlowSteps[index_NowStep];
            else
                return null;
        }
    }

    /// <summary>
    /// 当前任务id
    /// </summary>
    public int index_NowFlow;

    /// <summary>
    /// 总步骤顺序 用于语音数据匹配
    /// </summary>
    public int TotalStepIndex => flows.Take(index_NowFlow).Sum(f => f.steps.Count) + index_NowStep;

    /// <summary>
    /// 当前步骤id
    /// </summary>
    private int _index_NowStep;
    public int index_NowStep
    {
        get { return _index_NowStep; }
        set
        {
            _index_NowStep = value;

            // 自动播放：使用 DelayStart
            if (nowFlowStep != null && nowFlowStep.initState != null && nowFlowStep.initState.Count > 0)
            {
                ExecuteInitStateSequentially(nowFlowStep.initState, nowFlowStep, 0, 0, () =>
                {
                    ByStepPlayAudio();
                });
            }
            else
            {
                ByStepPlayAudio();
            }
        }
    }

    void ByStepPlayAudio()
    {
        if (isAutoPlay)
        {
            SpeechManager.Instance.DelayStart(nowFlowStep.ID, 0, TipType.StepName);
        }
        else
        {
            // 非自动播放：立即播放
            SpeechManager.Instance.PlayImmediate(nowFlowStep.ID, 0, TipType.StepName);
        }
    }

    /// <summary>
    /// 依次执行初始视角中的操作（支持弹窗等待）
    /// </summary>
    /// <param name="initStates">初始视角操作列表</param>
    /// <param name="currentStep">当前步骤</param>
    /// <param name="index">当前操作索引</param>
    /// <param name="popupIndex">当前弹窗索引（第几个弹窗）</param>
    /// <param name="onComplete">所有操作完成后的回调</param>
    private void ExecuteInitStateSequentially(List<SmallStepState> initStates, SmallStep1 currentStep, int index, int popupIndex, Action onComplete)
    {
        if (initStates == null || index >= initStates.Count)
        {
            onComplete?.Invoke();
            return;
        }

        SmallStepState state = initStates[index];
        if (state.operation == null)
        {
            // 操作为空，继续下一个
            ExecuteInitStateSequentially(initStates, currentStep, index + 1, popupIndex, onComplete);
            return;
        }

        // 检查操作中是否有弹窗 考核模式不需要自动生成弹窗
        bool hasPopup = false;
        BehavePopup popupBehave = null;

        if (!GlobalInfo.isExam)
        {
            if (GlobalInfo.courseMode == CourseMode.Training)
            {
                foreach (var op in state.operation.operations)
                {
                    if (op.name.Equals(state.optionName))
                    {
                        if (op.behaveBases != null)
                        {
                            foreach (var behave in op.behaveBases)
                            {
                                if (behave is BehavePopup popup)
                                {
                                    hasPopup = true;
                                    popupBehave = popup;
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
            }
        }

        if (hasPopup && popupBehave != null && useGuide)
        {
            // 引导模式下有弹窗，显示弹窗，等弹窗确认后再设置最终状态并执行下一个操作
            int currentPopupIndex = popupIndex + 1; // 当前是第几个弹窗（从1开始）
            UnityAction onclick = () =>
            {
                // 弹窗确认后，设置最终状态，然后执行下一个操作
                SetFinalState(state.operation, state.optionName);
                ExecuteInitStateSequentially(initStates, currentStep, index + 1, currentPopupIndex, onComplete);
            };
            popupBehave.Execute(onclick);
            SpeechManager.Instance.PlayImmediate(currentStep.ID, currentPopupIndex, TipType.Tips);
        }
        else
        {
            // 没有弹窗或非引导模式，直接设置最终状态，然后继续下一个
            SetFinalState(state.operation, state.optionName);
            ExecuteInitStateSequentially(initStates, currentStep, index + 1, popupIndex, onComplete);
        }
    }


    /// <summary>
    /// 记录正在执行表现的<操作对象，操作名称>
    /// 防止多次执行
    /// </summary>
    private Dictionary<string, string> cache = new Dictionary<string, string>();

    /// <summary>
    /// 当前步骤已执行并列操作集合
    /// </summary>
    public List<SmallOp1> successOPs = new List<SmallOp1>();

    public Color selectHighlight = new Color(0.55f, 0.92f, 1f);
    public Color hintHighlight = Color.red;


    //是否开启强制引导视角（todo 后台配置）
    private bool useGuide;

    /// <summary>
    /// 是否为自动播放（true: 通过Next()自动进入下一步; false: 用户手动选择步骤）
    /// 自动播放：使用 DelayStart，等待角色停止移动、等待上一步结束提示播放完成
    /// 非自动播放：使用 PlayImmediate，立即打断当前播放，直接开始新的语音
    /// </summary>
    private bool isAutoPlay = true;

    /// <summary>
    /// 各步骤初始视角
    /// key:flowIndex value: (key:stepIndex value:stepIndex)
    /// </summary>
    private Dictionary<int, Dictionary<int, int>> stepView = new Dictionary<int, Dictionary<int, int>>();

    /// <summary>
    /// 是否初始进入
    /// </summary>
    private bool firstEnter;

    private CanvasGroup masterComputerCanvas;
    public bool MasterComputerInteractable
    {
        set
        {
            if (masterComputerCanvas == null)
                return;
            //masterComputerCanvas.alpha = value ? 1 : 0.5f;
            //masterComputerCanvas.blocksRaycasts = value;
        }
    }

    /// <summary>
    /// 初始化绑定步骤和任务完成事件
    /// </summary>
    public void Init(bool useGuide, List<Flow> flowsTex = null)
    {
        AddMsg(
            (ushort)ModelOperateEvent.Rotate
        );

        this.useGuide = useGuide;

        flows = GetComponentsInChildren<SmallFlow1>();
        if (flowsTex != null && flowsTex.Count > 0)
        {
            if (flowsTex.Count != flows.Length)
            {
                Log.Warning("检查配置：ab包配置任务与后台不一致");
            }
            else
            {
                for (int i = 0; i < flows.Length; i++)
                {
                    flows[i].flowName = flowsTex[i].title;
                    if (flows[i].steps.Count != flowsTex[i].children.Count)
                    {
                        Log.Warning("检查配置：ab包配置任务步骤与后台不一致");
                        continue;
                    }
                    else
                    {
                        for (int j = 0; j < flows[i].steps.Count; j++)
                        {
                            flows[i].steps[j].hint = flowsTex[i].children[j].title;
                        }
                    }
                }
            }
        }

        //记录各步骤对应的相机视角（步骤）
        int viewIndex = -1;
        for (int i = 0; i < flows.Length; i++)
        {
            viewIndex = -1;

            stepView.Add(i, new Dictionary<int, int>());

            for (int stepIndex = 0; stepIndex < flows[i].steps.Count; stepIndex++)
            {
                if (flows[i].steps[stepIndex].initState.FindIndex(s => s.optionName.Equals(cameraFlag)
                || s.optionName.Equals(playerFlag)) >= 0)
                {
                    viewIndex = stepIndex;
                }
                stepView[i].Add(stepIndex, viewIndex);
            }
        }

        ModelInfo[] modelInfos = GetComponentsInChildren<ModelInfo>(true);
        operationIDs = new Dictionary<string, ModelOperation>();
        naviPoints = new List<NavigationPoint>();
        autoProps = new Dictionary<string, ModelOperation>();
        //先加通用道具
        toolIDs = new Dictionary<string, ModelInfo>();
        ModelInfo[] tools = transform.Find("Backpack").GetComponentsInChildren<ModelInfo>(true);
        foreach (var item in tools)
        {
            AddmodeInfo(item);
        }
        //再加任务单独道具
        foreach (var item in modelInfos)
        {
            AddmodeInfo(item);
        }
    }

    void AddmodeInfo(ModelInfo modelInfo)
    {
        switch (modelInfo.PropType)
        {
            case PropType.Anchor:
                naviPoints.Add(new NavigationPoint() { Name = modelInfo.Name, Point = modelInfo.transform });
                break;
            case PropType.Map:
                int.TryParse(modelInfo.Name, out orthographicSize);
                break;
            case PropType.Operate:
            case PropType.Free:
                InitOperateProp(modelInfo);
                break;
            case PropType.BackPack:
            case PropType.BackPack_Original:
            case PropType.SafetyTool:
            case PropType.Schematics:
                InitBackpackProp(modelInfo);
                break;
            case PropType.MasterComputer:
                InitBackpackProp(modelInfo);
                //Init2DProps(modelInfo);
                break;
            case PropType.GlobalPerspective:
                globalPerspective = modelInfo.GetComponent<ModelOperation>();
                globalPerspective.initState = globalPerspective.currentState;
                break;
            case PropType.Auto:
                InitAutoProp(modelInfo);
                break;
            case PropType.Calibrator:
            case PropType.Animation:
            default:
                Debug.LogWarning($"存在未处理道具：{modelInfo.Name}");
                break;
        }
    }

    /// <summary>
    /// 初始化通用道具
    /// </summary>
    /// <param name="modelInfo"></param>
    private void InitOperateProp(ModelInfo modelInfo)
    {
        ModelOperation op = modelInfo.GetComponent<ModelOperation>();
        if (op == null)
            Debug.LogWarning($"存在未配置ModelOperation道具：{modelInfo.Name}");
        else
        {
            if (toolIDs.ContainsKey(modelInfo.ID))
                Debug.LogWarning($"存在重复UUID:{modelInfo.ID};背包道具：{toolIDs[modelInfo.ID].gameObject.name}-{modelInfo.gameObject.name}");
            if (operationIDs.ContainsKey(modelInfo.ID))
                Debug.LogWarning($"存在重复UUID:{modelInfo.ID};操作道具：{operationIDs[modelInfo.ID].gameObject.name}-{modelInfo.gameObject.name}");
            else
            {
                operationIDs.Add(modelInfo.ID, op);
                op.initState = op.currentState; 

                // 设置操作道具初始显示
                if (!string.IsNullOrEmpty(op.initState))
                    SetFinalState(op, op.initState, true);

                // ui操作联动模型
                if (modelInfo.InfoData != null && modelInfo.InfoData.InteractMode == InteractMode.OpUI)
                {
                    OpUIData info = modelInfo.InfoData.interactData as OpUIData;
                    if (info.targetObject != null)
                    {
                        uiRotateModels.Add(modelInfo.ID, info.targetObject);
                    }
                }
            }
        }
    }


    /// <summary>
    /// 初始化背包道具/上位机道具
    /// </summary>
    /// <param name="modelInfo"></param>
    private void InitBackpackProp(ModelInfo modelInfo)
    {
        ModelOperation op_BP = modelInfo.GetComponent<ModelOperation>();
        if (op_BP == null)
            Debug.LogWarning($"存在未配置ModelOperation背包道具：{modelInfo.Name}");
        else
        {
            if (operationIDs.ContainsKey(modelInfo.ID))
                Debug.LogWarning($"存在重复UUID:{modelInfo.ID};操作道具：{operationIDs[modelInfo.ID].gameObject.name}-{modelInfo.gameObject.name}");
            if (toolIDs.ContainsKey(modelInfo.ID))
                Debug.LogWarning($"存在重复UUID:{modelInfo.ID};背包道具：{toolIDs[modelInfo.ID].gameObject.name}-{modelInfo.gameObject.name}");
            else
            {
                if (modelInfo.PropType == PropType.SafetyTool)
                {
                    operationIDs.Add(modelInfo.ID, op_BP);
                }

                toolIDs.Add(modelInfo.ID, modelInfo);
                op_BP.initState = op_BP.currentState;
                SetFinalState(op_BP, op_BP.initState);//设置背包道具初始显示
            }
        }
    }

    /// <summary>
    /// 初始化上位机道具包含的2D道具
    /// </summary>
    /// <param name="modelInfo"></param>
    //private void Init2DProps(ModelInfo modelInfo)
    //{
    //    masterComputerCanvas = modelInfo.AutoComponent<CanvasGroup>();

    //    var modelInfos = modelInfo.GetComponentsInChildren<ModelInfo>();
    //    ModelOperation modelOperation;
    //    foreach (ModelInfo info in modelInfos)
    //    {
    //        modelOperation = info.GetComponent<ModelOperation>();
    //        if (modelOperation == null)
    //            continue;

    //        modelOperation.initState = modelOperation.currentState;

    //        switch (info.InfoData.InteractMode)
    //        {
    //            case InteractMode.Click2D:
    //                Button button = info.GetComponentInChildren<Button>();
    //                button.GetComponentInChildren<Text>().text = info.Name;
    //                button.onClick.AddListener(() =>
    //                {
    //                    //todo
    //                });
    //                break;
    //            case InteractMode.Menu2D:
    //                Dropdown dropdown = info.GetComponentInChildren<Dropdown>();
    //                dropdown.options = modelOperation.operations.Select(o => new Dropdown.OptionData(o.name)).ToList();
    //                dropdown.SetValueWithoutNotify(dropdown.options.FindIndex(o => o.text.Equals(modelOperation.initState)));
    //                dropdown.onValueChanged.AddListener((value) =>
    //                {
    //                    FormMsgManager.Instance.SendMsg(new Msg2DOperate((ushort)SmallFlowModuleEvent.Operate2D, modelOperation, dropdown.options[value].text));
    //                });
    //                break;
    //            default:
    //                break;
    //        }
    //    }
    //}

    /// <summary>
    /// 初始化自动道具
    /// </summary>
    /// <param name="modelInfo"></param>
    private void InitAutoProp(ModelInfo modelInfo)
    {
        ModelOperation op = modelInfo.GetComponent<ModelOperation>();
        if (op == null)
            Debug.LogWarning($"存在未配置ModelOperation道具：{modelInfo.Name}");
        else
        {
            operationIDs.Add(modelInfo.ID, op);
            autoProps.Add(modelInfo.ID, op);
            //ModelOperatingState.Add(op, string.Empty);

            op.initState = op.currentState;

            var operation = op.operations.Find(o => o.name.Equals(op.initState));
            if (operation != null)
            {
                ExecuteOperation(op, op.currentState, null, (operation) =>
                {
                    RunAction(operation.actions.FindAll(a => a.operation != null), null, 0);
                });
            }
        }
    }

    /// <summary>
    /// 操作执行失败，恢复操作对象状态
    /// </summary>
    /// <param name="modelOperation"></param>
    /// <param name="state"></param>
    public void RestoreState(ModelOperation modelOperation, string state)
    {
        if (modelOperation == null || string.IsNullOrEmpty(state))
            return;

        ModelInfo modelInfo = modelOperation.GetComponent<ModelInfo>();
        if (modelInfo == null)
            return;

        modelOperation.currentState = state;

        switch (modelInfo.InfoData.InteractMode)
        {
            case InteractMode.Menu2D:
                Dropdown dropdown = modelInfo.GetComponentInChildren<Dropdown>();
                dropdown.SetValueWithoutNotify(dropdown.options.FindIndex(o => o.text.Equals(modelOperation.currentState)));
                break;
            case InteractMode.OpUI:
                SetFinalState(modelOperation, state, true);
                var uiOperation = UIManager.Instance.canvas.GetComponentsInChildren<UIOperation>(true).Find(o => o.id.Equals(modelInfo.ID));
                if (uiOperation != null)
                {
                    uiOperation.SetFinalState(state);
                }
                break;
            default:
                SetFinalState(modelOperation, state, true);
                break;
        }
    }

    /// <summary>
    /// 判断输入文本是否正确
    /// </summary>
    /// <param name="optionName">记录或联系操作</param>
    /// <param name="input">输入文本</param>
    /// <returns></returns>
    public bool IsOnOperation(string optionName/*, string input*/)
    {
        if (index_NowStep < 0 || nowFlowSteps == null || index_NowStep >= nowFlowSteps.Count)
        {
            Debug.LogWarning("当前步骤数越界，无正确操作");
            return false;
        }

        //判断是否已执行操作
        for (int i = 0; i < successOPs.Count; i++)
        {
            if (successOPs[i].optionName.Equals(inputFlag) && successOPs[i].prop == null)
            {
                Debug.LogWarning("操作已执行！");
                return false;
            }
        }

        SmallOp1 data = nowFlowStep.ops.Find(value => value.operation.ID.Equals(optionName));
        if (data == null)
        {
            Debug.Log($"当前正确操作不是{optionName}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 用于考核模式任意操作判断释放是正确步骤
    /// </summary>
    /// <param name="optionName"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public bool IsOnOperation(string optionName, string id)
    {
        if (index_NowStep < 0 || nowFlowSteps == null || index_NowStep >= nowFlowSteps.Count)
        {
            Debug.LogWarning("当前步骤数越界，无正确操作");
            return false;
        }

        //判断是否已执行操作
        for (int i = 0; i < successOPs.Count; i++)
        {
            if (successOPs[i].optionName.Equals(inputFlag) && successOPs[i].prop == null)
            {
                Debug.LogWarning("操作已执行！");
                return false;
            }
        }

        SmallOp1 data = nowFlowStep.ops.Find(value => value.operation.ID.Equals(id) && value.optionName.Equals(optionName));
        if (data == null)
        {
            Debug.Log($"当前正确操作不是{optionName}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 获取正确的输入文本
    /// </summary>
    /// <returns></returns>
    public string GetOperationString()
    {
        if (nowFlowSteps == null || index_NowStep < 0 || index_NowStep >= nowFlowSteps.Count)
        {
            return string.Empty;
        }

        //判断是否已执行操作
        for (int i = 0; i < successOPs.Count; i++)
        {
            if (successOPs[i].optionName.Equals(inputFlag) && successOPs[i].prop == null)
            {
                return string.Empty;
            }
        }

        SmallOp1 data = nowFlowStep.ops.Find(value => value.optionName.Equals(inputFlag));
        if (data == null)
        {
            Debug.Log("当前正确操作不是输入文本");
            return string.Empty;
        }

        for (int i = 0; i < data.operation.operations.Count; i++)
        {
            OperationBase op = data.operation.operations[i];
            if (op.name.Equals(inputFlag))
                return op.hint_success;
        }
        return string.Empty;
    }

    /// <summary>
    /// 是否是正确行为
    /// </summary>
    /// <param name="operation">操作对象</param>
    /// <param name="prop">选择道具</param>
    /// <param name="data">正确操作数据</param>
    /// <returns></returns>
    public bool IsOnOperation(ModelOperation operation, ModelInfo prop, out SmallOp1 data)
    {
        data = null;

        if (operation != null)
        {
            if (index_NowStep < 0 || nowFlowSteps == null || index_NowStep >= nowFlowSteps.Count)
            {
                Debug.Log("当前步骤数越界，无正确操作");
                return false;
            }
            //判断是否已执行操作
            var executed = successOPs.Find(o => o.operation == operation && o.prop == prop);
            if (executed != null)
            {
                Debug.LogWarning("操作已执行！");
                return false;
            }
            //判断是否是正确操作物体
            data = nowFlowStep.ops.Find(value => value.operation == operation);
            if (data == null)
            {
                Debug.Log($"操作对象错误 当前对象为{operation.name}", operation);
                return false;
            }
            //判断是否选择正确道具
            if (data.prop != prop)
            {
                Debug.Log($"使用道具错误 当前道具为{(prop?.name ?? "空")} 正确道具为{(data.prop?.name ?? "空")}", prop);
                return false;
            }

            //todo 步骤限制条件待删，迁移为操作限制 判断是否满足限制条件
            //判断是否满足限制条件
            List<SmallStepState> conditions = nowFlowStep.conditions;
            for (int i = 0; i < conditions.Count; i++)
            {
                if (conditions[i].operation != null && conditions[i].operation.TryGetComponent(out ModelInfo modelInfo))
                {
                    if (modelInfo == null)
                    { Debug.LogError("道具未配置modelInfo"); return false; }

                    if (!conditions[i].operation.currentState.Equals(conditions[i].optionName))
                    { Debug.Log($"道具模式错误 当前道具模式为{conditions[i].operation.currentState} 正确模式为{conditions[i].optionName}"); return false; }
                }
            }
            Debug.Log(operation.name + "-" + "，是正确操作");
            return true;
        }
        else
        {
            Debug.LogWarning("操作对象为null");
            return false;
        }
    }

    /// <summary>
    /// 检查操作是否属于当前步骤的并列操作中
    /// </summary>
    /// <param name="operation">操作对象</param>
    /// <param name="optionName">操作名称</param>
    /// <param name="prop">道具</param>
    /// <returns>是否属于当前步骤的并列操作</returns>
    public bool IsOperationInCurrentStep(ModelOperation operation, string optionName)
    {
        if (operation == null)
            return false;

        if (nowFlowStep == null || nowFlowStep.ops == null)
            return false;

        // 检查操作是否在当前步骤的ops列表中
        return nowFlowStep.ops.Exists(op =>
            op.operation == operation &&
            op.optionName == optionName);
    }

    /// <summary>
    /// 选择任务
    /// </summary>
    public void SelectFlow(int index_Flow)
    {
        foreach (var operation in operationIDs)
        {
            RemoveHint(operation.Value, 0);
            RemoveHint(operation.Value, 1);
            Remove2DHint(operation.Value);
        }
        foreach (var tool in toolIDs)
        {
            if (tool.Value.PropType == PropType.BackPack_Original)
                onResetToolNum.Invoke(tool.Value);
        }

        index_NowFlow = index_Flow;
        _index_NowStep = 0;

        //设置当前任务前的任务步骤为已操作状态
        int indexFlow = -1;
        int indexStep = -1;

        //未操作的步骤(包括当前步骤)
        foreach (var flow in flows.Skip(index_NowFlow).Reverse())
        {
            foreach (var step in flow.steps.AsEnumerable().Reverse())
            {
                foreach (var operation in step.ops.AsEnumerable().Reverse())
                {
                    if (operation.operation != null)
                        SetFinalState(operation.operation, operation.operation.initState);
                    else
                        Debug.LogError(step.hint + "    没有配置操作对象    " + operation.optionName);
                }
            }
        }

        foreach (var flow in flows.Take(index_NowFlow))
        {
            indexFlow += 1;
            foreach (var step in flow.steps)
            {
                indexStep += 1;
                foreach (var operation in step.ops)
                {
                    SetFinalState(operation.operation, operation.optionName);
                    //跳大步骤也要设置之前联动的最终状态
                    foreach (var op in operation.actions)
                    {
                        SetFinalState(op.operation, op.optionName);
                    }
                    RefreshOpHistory(operation.operation, operation.optionName, indexFlow, indexStep);
                }
            }
        }

        cache.Clear();
        successOPs.Clear();
    }

    public SmallStep1 CurrentStep()
    {
        return nowFlowSteps[_index_NowStep];
    }

    /// <summary>
    /// 选择小步骤
    /// </summary>
    public void SelectStep(int stepIndex)
    {
        //初始全局视角
        if (!firstEnter && globalPerspective != null)
        {
            firstEnter = true;
            SetFinalState(globalPerspective, globalPerspective.initState);
            SwitchToGlobalPerspective(() => DoSelectStep(stepIndex));
        }
        else
        {
            DoSelectStep(stepIndex);
        }
    }

    public void SwitchToGlobalPerspective(UnityAction callback = null)
    {
        if (globalPerspective == null)
        {
            callback?.Invoke();
            return;
        }
        ExecutePerspectiveOperation(globalPerspective, observeFlag, (_) =>
        {
            callback?.Invoke();
        });
    }
    private SmallStepState GetPreviousCamState(int flowIndex, int stepIndex)
    {
        SmallStepState cameraState = null;
        var flowViews = stepView.Take(flowIndex + 1).Reverse().ToList();
        foreach (var f in flowViews)
        {
            var steps = f.Value.Reverse().ToList();
            foreach (var step in steps)
            {
                if ((f.Key < flowIndex || step.Key < stepIndex) && step.Value != -1)
                {
                    //Debug.LogError($"flow {flowIndex} step {stepIndex} 前序视角为 flow {f.Key} step {step.Value} {flows[f.Key].steps[step.Value].hint}, 目标视角{stepView[flowIndex][stepIndex]}");
                    cameraState = flows[f.Key].steps[step.Value].initState.FirstOrDefault(s => s.optionName.Equals(cameraFlag)
                    || s.optionName.Equals(playerFlag));
                    return cameraState;
                }
            }
        }
        return cameraState;
    }

    private void DoSelectStep(int stepIndex)
    {
        isAutoPlay = false; // 非自动播放模式（用户手动选择步骤）

        // 清空操作记录
        FormMsgManager.Instance.SendMsg(new MsgIntInt((ushort)SmallFlowModuleEvent.OperatingRecordClear, -1, -1));

        // todo 未配置初始视角的步骤，采用上一个步骤的视角？=> 在index_NowStep setter中执行
        // 漫游模式不采用，操作表现可能包含导航
        if (useGuide && stepView.ContainsKey(index_NowFlow) && stepView[index_NowFlow].ContainsKey(stepIndex))
        {
            if (stepView[index_NowFlow][stepIndex] != stepIndex)
            {
                SmallStepState cameraState = GetPreviousCamState(index_NowFlow, stepIndex);
                if (cameraState != null)
                {
                    SetFinalState(cameraState.operation, cameraState.optionName);
                }
            }
        }

        int indexStep = -1;
        foreach (var step in nowFlowSteps.Take(stepIndex))
        {
            indexStep += 1;
            foreach (var operation in step.ops)
            {
                SetFinalState(operation.operation, operation.optionName);
                foreach (var op in operation.actions)
                {
                    SetFinalState(op.operation, op.optionName);
                }
                RefreshOpHistory(operation.operation, operation.optionName, index_NowFlow, indexStep);
            }
        }

        // 设置当前步骤索引
        index_NowStep = stepIndex;
    }


    public string GetStep(int stepIndex)
    {
        if (stepIndex >= 0 && stepIndex < nowFlowSteps.Count)
        {
            return nowFlowSteps[stepIndex].ID;
        }
        return null;
    }


    /// <summary>
    /// 步骤引导
    /// </summary>
    /// <param name="flowIndex"></param>
    /// <param name="stepIndex"></param>
    public void StepGuide(int flowIndex, int stepIndex)
    {
        if (!GlobalInfo.hasRole && useGuide && stepView.ContainsKey(flowIndex) && stepView[flowIndex].ContainsKey(stepIndex))
        {
            if (stepView[flowIndex][stepIndex] != stepIndex)
            {
                SmallStepState cameraState = GetPreviousCamState(flowIndex, stepIndex);
                if (cameraState != null)
                {
                    SetFinalState(cameraState.operation, cameraState.optionName);
                }
            }

            if (flows != null && flowIndex >= 0 && flowIndex < flows.Length)
            {
                var steps = flows[flowIndex].steps;
                if(steps != null && stepIndex >= 0 && stepIndex < steps.Count)
                {
                    foreach (var target in steps[stepIndex].initState)
                    {
                        SetFinalState(target.operation, target.optionName);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 执行自由操作
    /// </summary>
    /// <param name="broadcastMsg">广播操作消息</param>
    /// <param name="data"></param>
    /// <param name="userNo">操作人工号</param>
    /// <param name="userName">操作人姓名</param>
    /// <param name="callback"></param>
    /// <param name="dummy">为true 表示非本人操作；不执行相机移动、角色导航等操作表现</param></param>
    public void TryExecuteFreeOperation(SmallOp1 data, string userNo, string userName, Action<bool> callback = null, bool dummy = true)
    {
        //TODO 操作执行过程中暂停操作同步==>切换百科等操作无法执行
        NetworkManager.Instance.IsIMSync = false;


        string modelInfoId = data.operation?.GetComponent<ModelInfo>()?.ID;
        bool isOnOperation = IsOnOperation(data.optionName, data.operation.ID);

        Debug.Log("调试 当前需要执行:" + nowFlowStep.ID + " 执行结果： " + isOnOperation);
        FormMsgManager.Instance.SendMsg(new MsgStringBool((ushort)SmallFlowModuleEvent.StartExecute, modelInfoId, dummy));

        ExecuteOperation(data.operation, data.optionName, data.prop, (op) =>
        {
            ModelOperationEventManager.Publish(new ModelStateEvent(modelInfoId, data.optionName));
            if (op != null)
            {
                OnFreeOperationInvoked?.Invoke();

                RunAction(op.actions.FindAll(a => a.operation != null), () =>
                {
                    if (!string.IsNullOrEmpty(op.hint_success))
                    {
                        FormMsgManager.Instance.SendMsg(new MsgOperatingRecord((ushort)SmallFlowModuleEvent.OperatingRecord,
                            string.Empty, op.hint_success, index_NowFlow, index_NowStep, ModelOperationIndex(data.operation), userNo, userName,
                            GlobalInfo.ServerTimeFormat, UISmallSceneOperationHistory.OpType.Operation));
                    }

                    callback?.Invoke(true);
                    //考核模式下 如果执行的步骤正确，就调用步骤结束
                    if(isOnOperation)
                    {
                        RecordSucessOp(data);
                        Next();
                    }
                    else
                    {
                        DOVirtual.DelayedCall(0.2f, () =>
                        {
                            FormMsgManager.Instance.SendMsg(new MsgString((ushort)SmallFlowModuleEvent.CompleteExecute, string.Empty));
                        });
                    }
                    MasterComputerInteractable = true;
                }, 0, dummy);
            }
            else
            {
                callback?.Invoke(true);
                if (isOnOperation)
                {
                    RecordSucessOp(data);
                    Next();
                }
                else
                {
                    DOVirtual.DelayedCall(0.2f, () =>
                    {
                        FormMsgManager.Instance.SendMsg(new MsgString((ushort)SmallFlowModuleEvent.CompleteExecute, string.Empty));
                    });
                }
            }
        }, dummy);
    }

    /// <summary>
    /// 执行操作
    /// </summary>
    /// <param name="data"></param>
    /// <param name="correctOp">是否是当前步骤的正确操作</param>
    /// <param name="userNo">操作人工号</param>
    /// <param name="userName">操作人姓名</param>
    /// <param name="callback"></param>
    /// <param name="dummy">为true 表示非本人操作；不执行相机移动、角色导航等操作表现</param>
    public void TryExecuteOperation(SmallOp1 data, bool correctOp, string userNo, string userName, Action<bool> callback = null, bool dummy = false)
    {
        string modelInfoId = data.operation != null ? data.operation.ID : string.Empty;
        FormMsgManager.Instance.SendMsg(new MsgStringBool((ushort)SmallFlowModuleEvent.StartExecute, modelInfoId, dummy));

        // Tips 类型语音,自动进入0 与 smallSceneModule.ShowHint冲突 增加了一个标志位，如果是配置了流程解说 则不重复执行提示0
        SpeechManager.Instance.PlayImmediate(nowFlowStep.ID, 0, TipType.Tips);
        ExecuteOperation(data.operation, data.optionName, data.prop, (op) =>
        {
            if (op != null)
            {
                ModelOperationEventManager.Publish(new ModelStateEvent(modelInfoId, data.optionName));
                Debug.Log("操作和执行完成！");
                ToolManager.SendBroadcastMsg(new MsgInt((ushort)SmallFlowModuleEvent.StepEnd, GlobalInfo.account.id));

                RunAction(op.actions.FindAll(a => a.operation != null), () =>
                {
                    SmallStep1 smallStep = nowFlowStep;

                    // 先构建联动操作列表，判断是否需要等待联动完成
                    List<OpLinkage> opLinkages = new List<OpLinkage>();
                    SmallOp1 smallOp1 = null;

                    for (int i = 0; i < smallStep.ops.Count; i++)
                    {
                        if (data.operation.ID == smallStep.ops[i].operation.ID && smallStep.ops[i].optionName == data.optionName)
                        {
                            smallOp1 = smallStep.ops[i];
                        }
                    }

                    if (smallOp1 != null)
                    {
                        for (int i = 0; i < smallOp1.actions.Count; i++)
                        {
                            OpLinkage opLinkage = new OpLinkage();
                            opLinkage.operation = smallOp1.actions[i].operation;
                            opLinkage.optionName = smallOp1.actions[i].optionName;
                            opLinkage.useCallback = smallOp1.actions[i].useCallback;
#if UNITY_EDITOR
                            opLinkage.state = smallOp1.actions[i].state;
#endif
                            opLinkages.Add(opLinkage);
                        }
                    }


                    //记录
                    string hint = op.hint_success;
                    if (string.IsNullOrEmpty(hint))
                        hint = "操作" + data.operation.GetComponent<ModelInfo>().Name;
                    if (data.prop != null)
                    {
                        switch (data.prop.PropType)
                        {
                            case PropType.BackPack:
                            case PropType.BackPack_Original:
                                hint = $"使用{data.prop.Name},{hint}";
                                break;
                            default:
                                break;
                        }
                    }
                    FormMsgManager.Instance.SendMsg(new MsgOperatingRecord((ushort)SmallFlowModuleEvent.OperatingRecord,
                    true ? nowFlowStep?.hint_success : string.Empty, hint, index_NowFlow, index_NowStep, ModelOperationIndex(data.operation), userNo, userName,
                    GlobalInfo.ServerTimeFormat, UISmallSceneOperationHistory.OpType.Operation));

                    SpeechManager.Instance.PlayImmediate(nowFlowStep.ID, 0, TipType.StepComplete);
                    if (opLinkages.Count != 0)
                    {
                        ExecuteFlowLinkOperation(opLinkages, () =>
                        {
                            callback?.Invoke(true);
                            Next();
                            if (correctOp)
                                RecordSucessOp(data);
                        }, 0, dummy);
                    }
                    else
                    {
                        callback?.Invoke(true);
                        Next();

                        if (correctOp)
                            RecordSucessOp(data);
                    }
                }, 0, dummy);
            }
            else
            {
                //Debug.Log("未配置操作：index_NowFlow = " + index_NowFlow + "; index_NowStep = " + index_NowStep);
                string hint = "操作" + data.operation.GetComponent<ModelInfo>().Name;
                if (data.prop != null)
                {
                    switch (data.prop.PropType)
                    {
                        case PropType.BackPack:
                        case PropType.BackPack_Original:
                            hint = $"使用{data.prop.Name},{hint}";
                            break;
                        default:
                            break;
                    }
                }

                FormMsgManager.Instance.SendMsg(new MsgOperatingRecord((ushort)SmallFlowModuleEvent.OperatingRecord,
                    string.Empty, hint, index_NowFlow, index_NowStep, ModelOperationIndex(data.operation), userNo, userName,
                    GlobalInfo.ServerTimeFormat, UISmallSceneOperationHistory.OpType.Operation));

                callback?.Invoke(false);
            }
        }, dummy);
    }

    private void ExecuteFlowLinkOperation(List<OpLinkage> opLinkages, Action callback, int index = 0, bool dummy = false)
    {
        if (opLinkages.Count == index)
        {
            callback?.Invoke();
            return;
        }

        // dummy 模式下跳过相机和移动相关操作
        if (dummy && IsDummySkipOperation(opLinkages[index].operation, opLinkages[index].optionName, true))
        {
            ExecuteFlowLinkOperation(opLinkages, callback, ++index, dummy);
            return;
        }

        var op = opLinkages[index];

        // 检查是否有弹窗（培训模式 + 引导模式 + 非考核）
        bool hasPopup = false;
        BehavePopup popupBehave = null;

        bool hasguide = false;
        BehavePlayerNavigation guideBehave = null;
        foreach (var operation in op.operation.operations)
        {
            if (operation.name.Equals(op.optionName))
            {
                if (operation.behaveBases != null)
                {
                    foreach (var behave in operation.behaveBases)
                    {
                        if (behave is BehavePopup popup)
                        {
                            hasPopup = true;
                            popupBehave = popup;
                            break;
                        }
                    }
                }
                break;
            }
        }
        foreach (var operation in op.operation.operations)
        {
            if (operation.name.Equals(op.optionName))
            {
                if (operation.behaveBases != null)
                {
                    foreach (var behave in operation.behaveBases)
                    {
                        if (behave is BehavePlayerNavigation guide)
                        {
                            hasguide = true;
                            guideBehave = guide;
                            break;
                        }
                    }
                }
                break;
            }
        }

        if (hasPopup)
        {
            // 有弹窗：显示弹窗，等确认后再 SetFinalState 并继续
            popupBehave.Execute(() =>
            {
                ExecuteFlowLinkOperation(opLinkages, callback, ++index, dummy);
            });
        }
        else if (hasguide)
        {
            // 有导航：导航到目标位置再继续
            guideBehave.Execute(() =>
            {
                ExecuteFlowLinkOperation(opLinkages, callback, ++index, dummy);
            });
        }
        else
        {
            // 设置主操作的最终状态，但不处理联动操作（联动操作通过ExecuteFlowLinkOperation处理以支持导航走过去）
            SetFinalState(op.operation, op.optionName, false, false);

            // 查找操作并处理其联动
            OperationBase linkedOp = null;
            for (int i = 0; i < op.operation.operations.Count; i++)
            {
                if (op.operation.operations[i].name.Equals(op.optionName))
                {
                    linkedOp = op.operation.operations[i];
                    break;
                }
            }

            if (linkedOp != null && linkedOp.actions != null && linkedOp.actions.Count > 0)
            {
                var linkedActions = linkedOp.actions.FindAll(a => a.operation != null);
                if (linkedActions.Count > 0)
                {
                    // 通过ExecuteFlowLinkOperation处理联动，这样导航行为会走过去而不是瞬移
                    ExecuteFlowLinkOperation(linkedActions, () =>
                    {
                        ExecuteFlowLinkOperation(opLinkages, callback, ++index, dummy);
                    }, 0, dummy);
                }
                else
                {
                    ExecuteFlowLinkOperation(opLinkages, callback, ++index, dummy);
                }
            }
            else
            {
                ExecuteFlowLinkOperation(opLinkages, callback, ++index, dummy);
            }
        }
    }

    /// <summary>
    /// 执行行为
    /// </summary>
    /// <param name="operation">操作道具</param>
    /// <param name="optionName">操作名称</param>
    /// <param name="prop"></param>
    /// <param name="callback"></param>
    public void ExecutePerspectiveOperation(ModelOperation operation, string optionName, Action<OperationBase> callback = null)
    {
        for (int i = 0; i < operation.operations.Count; i++)
        {
            if (operation.operations[i].name.Equals(optionName))
            {
                OperationBase op = operation.operations[i];
                ModelInfo info = operation.GetComponent<ModelInfo>();

                if (cache.TryGetValue(info.ID, out string executingOp))
                {
                    if (executingOp.Equals(optionName))
                    {
                        Debug.LogWarning(info.ID + "-" + optionName + "操作正在执行!");
                        callback?.Invoke(null);
                        return;
                    }
                    else
                    {
                        AbortOperation(operation, executingOp);
                    }
                }

                cache.Add(info.ID, optionName);
                Execute(op.behaveBases, 0, op.behaveBases.Count, () =>
                {
                    cache.Remove(info.ID);
                    callback?.Invoke(op);
                });
                return;
            }
        }

        Debug.LogWarning(operation.name + "-" + optionName + "操作没有配置");
        callback?.Invoke(null);
    }

    /// <summary>
    /// 执行行为
    /// </summary>
    /// <param name="operation">操作道具</param>
    /// <param name="optionName">操作名称</param>
    /// <param name="prop"></param>
    /// <param name="callback"></param>
    /// <param name="dummy">为true时不执行操作表现，用于协同/考核时跳过非本人操作的相机表现</param> 
    public void ExecuteOperation(ModelOperation operation, string optionName, ModelInfo prop = null, Action<OperationBase> callback = null, bool dummy = false)
    {
        for (int i = 0; i < operation.operations.Count; i++)
        {
            if (operation.operations[i].name.Equals(optionName))
            {
                OperationBase op = operation.operations[i];
                ModelInfo info = operation.GetComponent<ModelInfo>();

                //判断是否满足操作限制
                var conditionGroup = op.conditions.GroupBy(o => o.operation);
                foreach (var group in conditionGroup)
                {
                    if (!group.ToList().Select(op => op.optionName).Contains(group.Key.currentState))
                    {
                        Debug.LogWarning($"{info.ID}-{optionName}不满足限制条件:  道具:{group.Key.GetComponent<ModelInfo>().ID} 状态：{group.Key.currentState}");//:{op.conditions[j].optionName}
                        callback?.Invoke(null);
                        return;
                    }
                }

                if (cache.TryGetValue(info.ID, out string executingOp))
                {
                    if (executingOp.Equals(optionName))
                    {
                        Debug.LogWarning(info.ID + "-" + optionName + "操作正在执行!");
                        callback?.Invoke(null);
                        return;
                    }
                    else
                    {
                        AbortOperation(operation, executingOp);
                    }
                }

                RemoveHint(operation);
                Remove2DHint(operation);

                //Debug.Log(operation.name + "-执行行为-" + optionName);
                cache.Add(info.ID, optionName);

                //聚焦操作不影响上位机
                if (!optionName.Equals(focusFlag))
                {
                    MasterComputerInteractable = false;
                }

                Execute(op.behaveBases, 0, op.behaveBases.Count, () =>
                {
                    if(operation != null)
                    {
                        cache.Remove(info.ID);

                        // 排除不影响道具状态的操作
                        if (!(optionName.Equals(observeFlag)
                          || optionName.Equals(focusFlag)
                          || optionName.Equals(inputFlag)
                          || optionName.Equals(clickFlag)
                          || optionName.StartsWith(backpackFlag)
                          || optionName.StartsWith(retrieveFlag))) //todo 目前是特殊处理: 工具箱_"背包道具"
                        {
                            operation.currentState = optionName;
                        }

                        Debug.Log(operation.name + "-" + optionName + "操作执行完成");
                        CheckKeywords(operation, optionName, false);
                        callback?.Invoke(op);
                    }
                }, dummy);
                return;
            }
        }

        Debug.LogWarning(operation.name + "-" + optionName + "操作没有配置");
        callback?.Invoke(null);
    }

    // 需要在 dummy 模式下跳过的行为类型（相机和移动相关）
    private static readonly HashSet<BehaveType> DummySkipBehaveTypes = new HashSet<BehaveType>
    {
        BehaveType.CameraFollow,    // 相机跟随
        BehaveType.ObserveRotate,   // 围绕观察  
        BehaveType.Focus,           // 聚焦  
        BehaveType. Observe,        // 观察

        BehaveType.PlayerNavigation,// 角色寻路
        BehaveType.CustomScript,    // 自定义脚本  
        BehaveType.Thermometring,   // 测量温度  
    };
    private static readonly HashSet<BehaveType> DummySkipBehaveTypes_link = new HashSet<BehaveType>
    {
        BehaveType.CameraFollow,    // 相机跟随
        BehaveType.PlayerNavigation,// 角色寻路
        BehaveType.CustomScript,    // 自定义脚本  
    };

    private bool IsDummySkipBehavior(BehaveType behaveType, bool link)
    {
        if (link)
            return DummySkipBehaveTypes_link.Contains(behaveType);
        else
            return DummySkipBehaveTypes.Contains(behaveType);
    }

    /// <summary>
    /// 检查操作是否应该在 dummy 模式下被跳过
    /// </summary>
    /// <param name="operation">操作对象</param>
    /// <param name="optionName">操作名称</param>
    /// <returns>如果操作的所有行为都是需要跳过的类型则返回 true</returns>
    private bool IsDummySkipOperation(ModelOperation operation, string optionName, bool link)
    {
        if (operation == null || operation.operations == null)
            return false;

        foreach (var op in operation.operations)
        {
            if (op.name.Equals(optionName) && op.behaveBases != null && op.behaveBases.Count > 0)
            {
                // 检查是否所有行为都是需要跳过的类型
                foreach (var behave in op.behaveBases)
                {
                    if (!IsDummySkipBehavior(behave.behaveType, link))
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 执行行为组
    /// </summary>
    /// <param name="behaveBases"></param>
    /// <param name="index"></param>
    /// <param name="max"></param>
    /// <param name="onComplete"></param>
    /// <param name="dummy">为true时跳过相机和移动相关行为，用于协同/考核时避免B端同步执行A端的相机和移动操作</param>
    public void Execute(List<BehaveBase> behaveBases, int index, int max, UnityAction onComplete, bool dummy = false)
    {
        if (index < max)
        {
            // dummy 模式下跳过相机和移动相关行为
            if (dummy && IsDummySkipBehavior(behaveBases[index].behaveType, false))
            {
                Execute(behaveBases, ++index, max, onComplete, dummy);
                return;
            }

            //勾选等待执行完成时等待上一表现执行完成再执行下一表现，或执行最后一个表现时等待表现执行完成后再执行操作完成回调 新增：对自定义脚本的接口的UseCallback获取该行为是否要等待
            if (behaveBases[index].useCallBack || index == max - 1 || (behaveBases[index] is BehaveCustomScript customScript && behaveBases[index].ctrlGO.GetComponent<IBaseBehaviour>().UseCallback(customScript.Step)))
            {
                behaveBases[index].Execute(() =>
                {
                    Execute(behaveBases, ++index, max, onComplete, dummy);
                });
            }
            else
            {
                behaveBases[index].Execute(null);
                Execute(behaveBases, ++index, max, onComplete, dummy);
            }
        }
        else
        {
            WaitAudioEnd(onComplete).Forget();
        }
    }

    /// <summary>
    /// 协程版等待在联机时会出错
    /// </summary>
    /// <param name="onComplete"></param>
    /// <returns></returns>
    private async UniTaskVoid WaitAudioEnd(UnityAction onComplete)
    {
        await UniTask.Delay(1);
        await UniTask.WaitUntil(() => !SpeechManager.Instance.IsAudioPlaying);
        onComplete?.Invoke();
    }

    /// <summary>
    /// 执行联动操作
    /// </summary>
    public void RunAction(List<OpLinkage> actions, Action callBack = null, int index = 0, bool dummy = false)
    {
        if (index >= actions.Count)
        {
            callBack?.Invoke();
            return;
        }

        // dummy 模式下跳过相机和移动相关操作
        if (dummy && IsDummySkipOperation(actions[index].operation, actions[index].optionName, true))
        {
            RunAction(actions, callBack, ++index, dummy);
            return;
        }

        if (actions[index].useCallback)
        {
            ExecuteOperation(actions[index].operation, actions[index].optionName, null, isOn =>
            {
                if (isOn != null)
                    RunAction(actions[index].operation.operations.Find(value => value.name.Equals(actions[index].optionName)).actions.FindAll(a => a.operation != null), () => RunAction(actions, callBack, ++index, dummy), 0, dummy);
                else
                    RunAction(actions, callBack, ++index, dummy);
            }, dummy);
        }
        else
        {
            //ExecuteOperation(actions[index].operation, actions[index].optionName, null, isOn =>
            //{
            //    if (isOn != null)
            //        RunAction(actions[index].operation.operations.Find(value => value.name.Equals(actions[index].optionName)).actions, null, 0, dummy);
            //}, null, dummy);
            StartCoroutine(ExecuteAction(actions[index], dummy));
            RunAction(actions, callBack, ++index, dummy);
        }
    }

    /// <summary>
    /// 确保不等待执行完毕的联动操作正常执行
    /// </summary>
    /// <param name="action"></param>
    /// <param name="dummy"></param>
    /// <returns></returns>
    private IEnumerator ExecuteAction(OpLinkage action, bool dummy)
    {
        yield return null;
        ExecuteOperation(action.operation, action.optionName, null, isOn =>
        {
            if (isOn != null)
            {
                RunAction(action.operation.operations.Find(value => value.name.Equals(action.optionName)).actions.FindAll(a => a.operation != null), null, 0, dummy);
            }
        }, dummy);
    }

    /// <summary>
    /// 保存已完成操作
    /// </summary>
    /// <param name="op"></param>
    private void RecordSucessOp(SmallOp1 op)
    {
        lock (successOPs)
        {
            var executed = successOPs.Find(o => o.operation == op.operation && o.optionName.Equals(op.optionName) && o.prop == op.prop);
            if (executed == null)
            {
                successOPs.Add(op);
                //Debug.LogWarning($"操作和联动执行完！{successOPs.Count}");
            }
        }
    }

    /// <summary>
    /// 当前步骤是否已执行完全部并列操作
    /// </summary>
    /// <returns></returns>
    public bool IsStepCompleted()
    {
        if (!GlobalInfo.EnableFlow)
            return false;

        if (nowFlowStep != null && nowFlowStep.ops.Count == successOPs.Count)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 显示高亮或虚影,0为提示高亮，1为选中高亮
    /// </summary>
    /// <param name="component"></param>
    /// <param name="priority"></param>
    public void AddHint(Component component, int priority = 0)
    {
        if (component == null)
            return;

        if (component.TryGetComponent(out ModelRestrict modelRestrict))
        {
            if (modelRestrict.modelHighlight.highlightNode != null)
            {
                HighlightEffectManager.Instance.Add(modelRestrict.modelHighlight.highlightNode, priority == 0 ? hintHighlight : selectHighlight,
                   modelRestrict.modelHighlight.outlineWidth, modelRestrict.modelHighlight.visibility, modelRestrict.modelHighlight.constantWidth, priority);

                if (priority == 0)
                    HighlightEffectManager.Instance.HighlightFlashing(modelRestrict.modelHighlight.highlightNode);
                else
                    HighlightEffectManager.Instance.RemoveHighlightFlashing(modelRestrict.modelHighlight.highlightNode);
            }

            if (priority == 0 && modelRestrict.modelGhost.ghostNode != null)
            {
                modelRestrict.modelGhost.ghostNode.gameObject.SetActive(true);
            }
        }
    }
    /// <summary>
    /// 移除高亮或虚影
    /// </summary>
    /// <param name="component"></param>
    /// <param name="priority"></param>
    public void RemoveHint(Component component, int priority = 0)
    {
        if (component == null)
            return;

        if (component.TryGetComponent(out ModelRestrict modelRestrict))
        {
            if (modelRestrict.modelHighlight.highlightNode != null)
            {
                HighlightEffectManager.Instance.Remove(modelRestrict.modelHighlight.highlightNode, priority);
                if (priority == 0)
                    HighlightEffectManager.Instance.RemoveHighlightFlashing(modelRestrict.modelHighlight.highlightNode);
                else
                    HighlightEffectManager.Instance.HighlightFlashing(modelRestrict.modelHighlight.highlightNode);
            }

            if (priority == 0 && modelRestrict.modelGhost.ghostNode != null)
            {
                modelRestrict.modelGhost.ghostNode.gameObject.SetActive(false);
            }
        }
    }
    /// <summary>
    /// 显示2D操作高亮
    /// </summary>
    /// <param name="component"></param>
    public void Add2DHint(Component component)
    {
        if (component == null)
            return;

        var sequence = DOTween.Sequence();
        {
            var image = component.transform.GetComponentByChildName<Image>("Highlight");
            {
                image.gameObject.SetActive(true);
                image.SetAlpha(1f);
                sequence.Append(image.DOFade(0, 0.8f));
            }

            sequence.SetId(component.transform.GetInstanceID());
            sequence.SetLoops(-1, LoopType.Yoyo);
            sequence.OnKill(() =>
            {
                image.SetAlpha(0f);
            });
        }
    }

    /// <summary>
    /// 移除2D高亮
    /// </summary>
    /// <param name="component"></param>
    public void Remove2DHint(Component component)
    {
        if (component == null)
            return;

        var image = component.transform.GetComponentByChildName<Image>("Highlight");
        if (image)
        {
            DOTween.Kill(component.transform.GetInstanceID(), true);
        }
    }

    /// <summary>
    /// 设置为最终状态
    /// </summary>
    /// <param name="operation">设置物体</param>
    /// <param name="optionName">设置操作</param>
    public void SetFinalState(ModelOperation operation, string optionName, bool ignoreCondition = false, bool processLinkages = true)
    {
        if (operation && !string.IsNullOrEmpty(optionName))
        {
            for (int i = 0; i < operation.operations.Count; i++)
            {
                if (operation.operations[i].name.Equals(optionName))
                {
                    OperationBase op = operation.operations[i];

                    if (!ignoreCondition)
                    {
                        //判断是否满足操作限制
                        var conditionGroup = op.conditions.GroupBy(o => o.operation);
                        foreach (var group in conditionGroup)
                        {
                            if (!group.ToList().Select(op => op.optionName).Contains(group.Key.currentState))
                            {
                                Debug.LogWarning($"{group.Key.GetComponent<ModelInfo>().Name}道具当前状态：{group.Key.currentState},不满足限制条件");//:{op.conditions[j].optionName}
                                return;
                            }
                        }
                    }

                    CheckKeywords(operation, optionName, false);

                    if (!(optionName.Equals(observeFlag)
                        || optionName.Equals(focusFlag)
                        || optionName.Equals(inputFlag)
                        || optionName.Equals(clickFlag)
                        || optionName.StartsWith(backpackFlag)
                        || optionName.StartsWith(retrieveFlag)))//观察操作不影响道具状态  //todo 目前是特殊处理
                    {
                        operation.currentState = optionName;
                    }

                    for (int k = 0; k < op.behaveBases.Count; k++)
                    {
                        try
                        {
                            op.behaveBases[k].SetFinalState();
                        }
                        catch
                        {
                            //Debug.LogError(operation.name + "    " + op.behaveBases[k].behaveType + "  该物体没有配置最终状态");
                        }
                    }

                    if (processLinkages && op.actions != null && op.actions.Count > 0)
                    {
                        for (int m = 0; m < op.actions.Count; m++)
                        {
                            // todo 自动触发道具状态不受步骤切换影响
                            try
                            {
                                if (op.actions[m].operation.GetComponent<ModelInfo>().PropType != PropType.Auto)
                                    SetFinalState(op.actions[m].operation, op.actions[m].optionName);

                            }
                            catch
                            {
                                Debug.LogError(operation.name + "  该物体配置错误");
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 设置为最终状态
    /// 协同、考核状态同步
    /// </summary>
    public void SetFinalState(List<OpDicData> modelStates, int index_NowFlow, int index_NowStep, List<SuccessOpData> successOpDatas)
    {
        if (modelStates == null)
            return;

        foreach (var item in modelStates)
        {
            if (!operationIDs.ContainsKey(item.id))
                continue; 

            SetFinalState(operationIDs[item.id], item.optionName, true);

            if (uiRotateModels.TryGetValue(item.id, out Transform model) && model != null)
            {
                if ((operationIDs[item.id].GetComponent<ModelInfo>().InfoData.interactData as OpUIData).content != null)
                {
                    model.localEulerAngles = new Vector3((float)Math.Round(model.localEulerAngles.x, 1), (float)Math.Round(model.localEulerAngles.y, 1), item.uiTargetModelEulerZ);
                }
            }
        }
        // 设置流程和步骤索引（无论 EnableFlow 是否为 true 都需要设置）
        this.index_NowFlow = index_NowFlow;
        // 直接设置私有字段，避免触发 setter 中的完整逻辑
        _index_NowStep = index_NowStep;

        if (successOpDatas != null)
        {
            successOPs = successOpDatas.Select(d => new SmallOp1()
            {
                operation = GetModelOperation(d.id),
                optionName = d.optionName,
                prop = GetModelInfo(d.propId)
            }).ToList();
        }
    }

    /// <summary>
    /// 执行特殊操作回调
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="optionName"></param>
    /// <param name="reverse"></param>
    private void CheckKeywords(ModelOperation operation, string optionName, bool reverse)
    {
        if (optionName == pickupFlag)
        {
            onPickup.Invoke(operation, reverse);
        }
        else if (optionName == usedFlag)
        {
            onUsed.Invoke(operation, reverse);
        }
        else if (optionName == retrieveFlag)
        {
            onUsed.Invoke(operation, !reverse);
        }
    }

    /// <summary>
    /// 跳步骤时刷新操作记录显示
    /// </summary>
    /// <param name="operation">操作对象</param>
    /// <param name="optionName">操作名称</param>
    /// <param name="index_Flow">任务id</param>
    /// <param name="index_Step">步骤id</param>
    private void RefreshOpHistory(ModelOperation operation, string optionName, int index_Flow, int index_Step)
    {
        if (operation == null)
            return;
        OperationBase op = operation.operations.Find(value => value.name.Equals(optionName));
        if (op != null)
        {
            if (op.name.Equals(inputFlag))
                FormMsgManager.Instance.SendMsg(new MsgOperatingRecord((ushort)SmallFlowModuleEvent.OperatingRecordInput, string.Empty, op.hint_success, index_Flow, index_Step, -1,
                    string.Empty, string.Empty, GlobalInfo.ServerTimeFormat, UISmallSceneOperationHistory.OpType.Input));
            else if (op.name.Equals(contactFlag))
                FormMsgManager.Instance.SendMsg(new MsgOperatingRecord((ushort)SmallFlowModuleEvent.OperatingRecordInput, string.Empty, op.hint_success, index_Flow, index_Step, -1,
                    string.Empty, string.Empty, GlobalInfo.ServerTimeFormat, UISmallSceneOperationHistory.OpType.Contact));
            else
                FormMsgManager.Instance.SendMsg(new MsgOperatingRecord((ushort)SmallFlowModuleEvent.OperatingRecord, string.Empty, op.hint_success, index_Flow, index_Step, ModelOperationIndex(operation),
                    string.Empty, string.Empty, GlobalInfo.ServerTimeFormat, UISmallSceneOperationHistory.OpType.Operation));
        }
    }

    /// <summary>
    /// 下一步
    /// </summary>
    public void Next()
    {
        DOVirtual.DelayedCall(0.2f, () =>
        {
            if (IsStepCompleted())
            {
                successOPs.Clear();
                MasterComputerInteractable = true;
            }

            isAutoPlay = true; // 自动播放模式
            if (index_NowFlow <= flows.Length - 1)
            {
                if (index_NowStep < nowFlowSteps.Count - 1)
                {
                    FormMsgManager.Instance.SendMsg(new MsgIntInt((ushort)SmallFlowModuleEvent.CompleteStep, index_NowStep, flows.Take(index_NowFlow).Sum(value => value.steps.Count) + index_NowStep));
                    index_NowStep += 1;
                }
                else
                {
                    if (index_NowFlow + 1 > flows.Length - 1)
                    {
                        Debug.Log("已完成所有任务");
                    }
                    else
                    {
                        index_NowFlow += 1;
                        index_NowStep = 0;
                    }
                    FormMsgManager.Instance.SendMsg(new MsgIntInt((ushort)SmallFlowModuleEvent.CompleteStep, index_NowStep, flows.Take(index_NowFlow).Sum(value => value.steps.Count) + index_NowStep));
                }
            }



            DOVirtual.DelayedCall(0.2f, () =>
            {
                FormMsgManager.Instance.SendMsg(new MsgString((ushort)SmallFlowModuleEvent.CompleteExecute, string.Empty));
            });
        });
    }

    /// <summary>
    /// 获取操作对象
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public ModelOperation GetModelOperation(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;
        if (operationIDs.ContainsKey(id))
            return operationIDs[id];
        return null;
    }

    /// <summary>
    /// 获取模型信息
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public ModelInfo GetModelInfo(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;
        return toolIDs[id];
    }

    /// <summary>
    /// 获取操作对象在当前步骤并列操作列表中的index
    /// </summary>
    /// <param name="modelOperation"></param>
    /// <returns></returns>
    private int ModelOperationIndex(ModelOperation modelOperation)
    {
        if (nowFlowStep == null)
            return -1;
        int index = nowFlowStep.ops.FindIndex(op => op.operation == modelOperation);
        if (index < 0)
            return -1;
        return index/* + 1*/;
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);

        switch (msg.msgId)
        {
            case (ushort)ModelOperateEvent.Rotate:
                if ((msg as MsgBrodcastOperate).senderId == GlobalInfo.account.id)
                    return;

                // 同步其他成员UI操作联动模型旋转
                MsgModelRotate msgModelRotate = (msg as MsgBrodcastOperate).GetData<MsgModelRotate>();
                if (uiRotateModels.TryGetValue(msgModelRotate.id, out Transform model) && model != null)
                {
                    model.localEulerAngles = new Vector3((float)Math.Round(model.localEulerAngles.x, 1), (float)Math.Round(model.localEulerAngles.y, 1), msgModelRotate.angleZ);
                }
                break;
        }    
    }

    /// <summary>
    /// 中断行为
    /// </summary>
    /// <param name="operation">操作道具</param>
    /// <param name="optionName">操作名称</param>
    public void AbortOperation(ModelOperation operation, string optionName)
    {
        var modelInfoID = operation.ID;
        //if (cache.Contains(modelInfoId))
        {
            SetFinalState(operation, optionName);
            cache.Remove(modelInfoID);
        }
    }

    /// <summary>
    /// 中断行为
    /// </summary>
    /// <param name="operation">操作道具</param>
    /// <param name="optionName">操作名称</param>
    public void AbortAllOperations()
    {
        StopAllCoroutines();
        foreach (var modelOperation in operationIDs)
        {
            foreach(var op in modelOperation.Value.operations)
            {
                SetFinalState(modelOperation.Value, op.name);
            }
        }
        cache.Clear();
    }

    /// <summary>
    /// 获取全局操作对象状态
    /// 用于协同考核状态同步
    /// </summary>
    /// <returns></returns>
    public List<OpDicData> GetModelStates()
    {
        List<OpDicData> states = new List<OpDicData>();
        foreach (var op in operationIDs)
        {
            var modelInfo = op.Value.GetComponent<ModelInfo>();
            if (uiRotateModels.TryGetValue(op.Key, out Transform model) && model != null)
            {
                if ((modelInfo.InfoData.interactData as OpUIData).content != null)
                {
                    states.Add(new OpDicData()
                    {
                        id = op.Key,
                        optionName = op.Value.currentState,
                        uiTargetModelEulerZ = model.localEulerAngles.z
                    });
                }
                else
                {
                    if (!op.Value.currentState.Equals(op.Value.initState))
                    {
                        states.Add(new OpDicData(op.Key, op.Value.currentState));
                    }
                }
            }
            else
            {
                if (!op.Value.currentState.Equals(op.Value.initState))
                {
                    states.Add(new OpDicData(op.Key, op.Value.currentState));
                }
            }
        } 

        return states;
    }

    /// <summary>
    /// 通过Sprite动态创建并添加图纸的道具信息
    /// </summary>
    /// <param name="schematicSprite">图纸图片</param>
    public void AddSchematic(Sprite schematicSprite)
    {
        if (!toolIDs.ContainsKey(schematicSprite.name))
        {
            // 获取Backpack父节点
            Transform backpack = transform.Find("Backpack");

            // 创建图纸GameObject
            GameObject schematicObj = new GameObject(schematicSprite.name);
            schematicObj.transform.SetParent(backpack);

            // 添加ModelInfo组件
            ModelInfo modelInfo = schematicObj.AddComponent<ModelInfo>();
            modelInfo.ID = schematicSprite.name;
            modelInfo.Name = schematicSprite.name;
            modelInfo.PropType = PropType.Schematics;

            Image image = schematicObj.AddComponent<Image>();
            image.sprite = schematicSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            // 添加到工具字典
            toolIDs.Add(schematicSprite.name, modelInfo);
            // 触发图纸添加事件 进入下一步会刷新工具栏状态，需要先刷再生成，才能隐藏工具栏
            DOVirtual.DelayedCall(0.1f, () =>
            {
                onSchematicAdded.Invoke(modelInfo);
            });
        }
        else
        {
            // 触发图纸添加事件 进入下一步会刷新工具栏状态，需要先刷再生成，才能隐藏工具栏
            DOVirtual.DelayedCall(0.1f, () =>
            {
                onSchematicAdded.Invoke(toolIDs[schematicSprite.name]);
            });
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        StopAllCoroutines();
    }
}

public class OpDicData
{
    public string id;
    public string optionName;

    //OpUI触发
    public float uiTargetModelEulerZ;
    public OpDicData() { }

    public OpDicData(string id, string optionName)
    {
        this.id = id;
        this.optionName = optionName;
    }
}

public class SuccessOpData
{
    public string id;
    public string optionName;
    public string propId;

    public SuccessOpData() { }

    public SuccessOpData(string id, string optionName, string propId)
    {
        this.id = id;
        this.optionName = optionName;
        this.propId = propId;
    }
}