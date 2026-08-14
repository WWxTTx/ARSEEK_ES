using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityFramework.Runtime;
using static UISmallSceneOperationHistory;
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
    public static string handFlag = "手部操作";
    public static string pickupFlag = "拾取";
    public static string usedFlag = "消耗";
    public static string retrieveFlag = "收回";
    public static string showFlag = "显示";
    public static string hideFlag = "隐藏";
    public static string backpackFlag = "工具箱";
    public static string navigationFlag = "导航";
    public static string drawingFlag = "图纸";
    public static string masterFlag = "上位机";
    public static string selectFlag = "选中";
    public static string unselectFlag = "取消选中";
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
    /// 步骤前进后触发（在Next中，进度已更新后），参数：flow, step
    /// 也在自由操作完成时触发（Over中，进度不变）
    /// </summary>
    public UnityAction<int, int> OnStepAdvanced;

    /// <summary>
    /// 图纸添加事件：参数-添加的图纸ModelInfo
    /// </summary>
    public UnityEvent<ModelInfo> onSchematicAdded = new UnityEvent<ModelInfo>();
    /// <summary>
    /// 弹窗状态变化事件：参数-true=弹窗显示，false=弹窗关闭
    /// </summary>
    public UnityEvent<bool> onPopupStateChanged = new UnityEvent<bool>();

    /// <summary>
    /// 任务集合
    /// </summary>
    public SmallFlow1[] flows;
    /// <summary>
    /// 全局视角
    /// </summary>
    public ModelOperation globalPerspective;
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
    /// 将扁平步骤索引转换为 (flow, step)
    /// </summary>
    public void TotalIndexToFlowStep(int totalIndex, out int flow, out int step)
    {
        int remaining = totalIndex;
        for (int f = 0; f < flows.Length; f++)
        {
            if (remaining < flows[f].steps.Count)
            {
                flow = f;
                step = remaining;
                return;
            }
            remaining -= flows[f].steps.Count;
        }
        flow = 0;
        step = 0;
    }

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
            ClearCompletedOps();
            // 不在此处重置 ignoreMove：CompleteStep 同步端依赖它保持 true 来抑制导航，由 Next() / SelectStep 调用方设置。
            if (nowFlowStep != null && nowFlowStep.initState != null && nowFlowStep.initState.Count > 0)
            {
                ExecuteInitStateSequentially(nowFlowStep.initState, nowFlowStep, 0, 0, () =>
                {
                    if (LoadingPanel.Loading)
                        UIManager.Instance.CloseUI<LoadingPanel>();
                    AimCameraAtFirstOp();
                    SpeechManager.Instance.PlayImmediate(nowFlowStep.ID, 0, TipType.StepName);
                });
            }
            else
            {
                AimCameraAtFirstOp();
                SpeechManager.Instance.PlayImmediate(nowFlowStep.ID, 0, TipType.StepName);
            }
        }
    }

    /// <summary>
    /// 统计初始视角中弹窗语音的数量（非考核模式下有BehavePopup的initState项数）
    /// </summary>
    private int GetInitStatePopupVoiceCount()
    {
        if (GlobalInfo.isExam || nowFlowStep?.initState == null)
            return 0;
        int count = 0;
        foreach (var state in nowFlowStep.initState)
        {
            if (state.operation == null) continue;
            foreach (var op in state.operation.operations)
            {
                if (op.name.Equals(state.optionName) && op.behaveBases != null)
                {
                    foreach (var behave in op.behaveBases)
                    {
                        if (behave is BehavePopup)
                        {
                            count++;
                            break;
                        }
                    }
                    break;
                }
            }
        }
        return count;
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

        if (hasPopup && popupBehave != null && !GlobalInfo.isExam)
        {
            // 引导模式下有弹窗，显示弹窗，等弹窗确认后再设置最终状态并执行下一个操作
            int currentPopupIndex = popupIndex + 1; // 当前是第几个弹窗（从1开始）
            UnityAction onclick = () =>
            {
                onPopupStateChanged.Invoke(false);
                // 弹窗确认后，设置最终状态，然后执行下一个操作
                SetFinalState(state.operation, state.optionName, false, true);
                ExecuteInitStateSequentially(initStates, currentStep, index + 1, currentPopupIndex, onComplete);
                ToolManager.SendBroadcastMsg(new MsgBool((ushort)SmallFlowModuleEvent.ClousePop, true));
            };
            onPopupStateChanged.Invoke(true);
            popupBehave.Execute(onclick);
            SpeechManager.Instance.PlayImmediate(currentStep.ID, currentStep.ops.Count + currentPopupIndex - 1, TipType.Tips);
        }
        else
        {
            // 没有弹窗或非引导模式，直接设置最终状态，然后继续下一个
            SetFinalState(state.operation, state.optionName, false, true);
            ExecuteInitStateSequentially(initStates, currentStep, index + 1, popupIndex, onComplete);
        }
    }

    #region 角色对准与导航（自动辅助）
    private PlayerController _playerController;
    /// <summary>
    /// 角色控制器（懒加载缓存）。统一从 ModelManager.modelRoot 下查找。
    /// </summary>
    private PlayerController playerController
    {
        get
        {
            if (_playerController == null && ModelManager.Instance != null && ModelManager.Instance.modelRoot != null)
                _playerController = ModelManager.Instance.modelRoot.GetComponentInChildren<PlayerController>();
            return _playerController;
        }
    }
    /// 将相机对准当前步骤第一个可操作对象。
    /// 在每个步骤开头（index_NowStep 设置、初始视角执行完成后）调用。
    /// </summary>
    private void AimCameraAtFirstOp()
    {
        if (nowFlowStep == null || nowFlowStep.ops == null)
            return;

        //仅培训和直播有效；协同模式下非本人操作跳过
        if (!GlobalInfo.IsExamMode() && (!GlobalInfo.isCooperation || IsCurrentOperationExecutor))
        {
            SmallOp1 firstOp = nowFlowStep.ops.FirstOrDefault(o => o.operation != null);
            if (firstOp == null)
                return;

            //找高光标记的位置
            Transform target = firstOp.operation.GetComponent<ModelRestrict>().modelHighlight.highlightNodes[0].transform;
            //延迟0.1f是为了避免锁定和设置最终状态在同一帧执行，导致锁定到设置最终状态前的位置
            DOVirtual.DelayedCall(0.1f, () =>
            {
                playerController?.AimAtTarget(target);
            });
        }
    }

    /// <summary>
    /// 非考核模式下，将相机对准当前步骤中下一个未完成的操作对象。
    /// 每次操作完成后调用，如果步骤未结束则瞄准下一个需点击的目标。
    /// </summary>
    private void AimCameraAtNextOp()
    {
        if (GlobalInfo.isExam)
            return;
        if (nowFlowStep == null || nowFlowStep.ops == null)
            return;

        SmallOp1 nextOp = nowFlowStep.ops.FirstOrDefault(o => o.operation != null && !completedOpIds.Contains(o.operation.ID));
        if (nextOp == null)
            return;

        //找高光标记的位置
        Transform target = nextOp.operation.GetComponent<ModelRestrict>().modelHighlight.highlightNodes[0].transform;
        DOVirtual.DelayedCall(0.1f, () =>
        {
            playerController?.AimAtTarget(target);
        });
    }


    private async UniTaskVoid NavigateNearTargetAsync(PlayerController pc, Transform target, Action proceed)
    {
        pc.KillCameraTweens();
        pc.Model.GetComponent<Animator>().SetBool("isMove", true);
        pc.StartNavigation(target, false);
        await UniTask.WaitUntil(() =>
        {
            if (!pc) return true;
            return Vector3.Distance(pc.transform.position, target.position) <= 0.5f || pc.NavEnd;
        });
        if (!pc)
            return;
        pc.EndNavigation();
        pc.Model.GetComponent<Animator>().SetBool("isMove", false);
        proceed();
    }

    #endregion


    /// <summary>
    /// 记录正在执行表现的<操作对象，操作名称>
    /// 防止多次执行
    /// </summary>
    private Dictionary<string, string> cache = new Dictionary<string, string>();

    /// <summary>
    /// 是否有操作正在执行中
    /// </summary>
    public bool IsExecuting => cache != null && cache.Count > 0;

    /// <summary>
    /// 当前步骤已完成的操作对象ID集合
    /// </summary>
    private HashSet<string> completedOpIds = new HashSet<string>();

    /// <summary>
    /// 标记操作完成，返回true表示当前步骤所有操作均已完成
    /// </summary>
    public bool MarkOpCompleted(SmallOp1 op)
    {
        if (op?.operation == null) return false;
        completedOpIds.Add(op.operation.ID);
        return IsStepComplete();
    }

    /// <summary>
    /// 当前步骤所有操作是否全部完成
    /// </summary>
    public bool IsStepComplete()
    {
        if (nowFlowStep == null) return true;
        foreach (var op in nowFlowStep.ops)
        {
            if (op.operation != null && !completedOpIds.Contains(op.operation.ID))
                return false;
        }
        return true;
    }

    /// <summary>
    /// 指定操作是否已完成
    /// </summary>
    public bool IsOpCompleted(SmallOp1 op)
    {
        return op?.operation != null && completedOpIds.Contains(op.operation.ID);
    }

    /// <summary>
    /// 当前步骤是否有操作执行错误（含未使用道具、使用错道具），用于并列操作整步不给分
    /// </summary>
    private bool _stepHasIncorrectOp;

    /// <summary>
    /// 清空当前步骤完成记录
    /// </summary>
    public void ClearCompletedOps()
    {
        completedOpIds.Clear();
        _stepHasIncorrectOp = false;
    }

    /// <summary>
    /// 获取已完成操作ID集合（用于序列化/网络传输）
    /// </summary>
    public HashSet<string> GetCompletedOpIds()
    {
        return new HashSet<string>(completedOpIds);
    }

    /// <summary>
    /// 恢复已完成操作ID集合，并还原对应操作的模型状态
    /// </summary>
    public void SetCompletedOpIds(HashSet<string> ids)
    {
        completedOpIds.Clear();
        if (ids != null)
        {
            foreach (var id in ids)
                completedOpIds.Add(id);

            // 用已完成的op ID找到当前步骤中对应的SmallOp1，用其optionName恢复模型状态
            if (nowFlowStep?.ops != null)
            {
                foreach (var opId in ids)
                {
                    var op = nowFlowStep.ops.Find(o => o.operation?.ID == opId);
                    if (op != null && op.operation != null)
                        SetFinalState(op.operation, op.optionName, true);
                }
            }
        }
    }

    public Color selectHighlight = new Color(0.55f, 0.92f, 1f);
    public Color hintHighlight = Color.red;

    /// <summary>
    /// 跳步骤时抑制图纸在非当前初始视角展开
    /// </summary>
    private bool isRestoringPreviousStates;

    /// <summary>
    /// 标记弹窗关闭是否 跳过导航本地表现
    /// </summary>
    public bool ignoreMove = false;

    /// <summary>
    /// 当前操作的实际执行者（非协同同步方）。联动弹窗等场景用此判断是否执行完整行为。
    /// </summary>
    public bool IsCurrentOperationExecutor { get; private set; } = true;

    /// <summary>
    /// 本次操作是否持有相机锁定。执行开始时获取，整条链（操作联动+步骤联动）走完后在 Over 中释放。
    /// </summary>
    private bool cameraLockHeld;

    /// <summary>
    /// 相机锁定代次。每次开始新操作时自增，使上一次操作的延迟释放回调失效。
    /// </summary>
    private int cameraLockGeneration;

    private Func<bool> audioNotPlayingPredicate;

    /// <summary>
    /// 初始化绑定步骤和任务完成事件
    /// </summary>
    public void Init(List<Flow> flowsTex = null)
    {
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
                    flows[i].ID = flowsTex[i].id;
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
                            flows[i].steps[j].ID = flowsTex[i].children[j].id;
                            flows[i].steps[j].hint = flowsTex[i].children[j].title;
                        }
                    }
                }
            }
        }

        ModelInfo[] modelInfos = GetComponentsInChildren<ModelInfo>(true);
        operationIDs = new Dictionary<string, ModelOperation>();
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

        // 进入任务统一以第三人称视角开始
        if (playerController != null)
            playerController.ToThird();
    }

    void AddmodeInfo(ModelInfo modelInfo)
    {
        switch (modelInfo.PropType)
        {
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
                Log.Warning($"存在未处理道具：{modelInfo.Name}");
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
            Log.Warning($"存在未配置ModelOperation道具：{modelInfo.Name}");
        else
        {
            if (toolIDs.ContainsKey(modelInfo.ID))
                Log.Warning($"存在重复UUID:{modelInfo.ID};背包道具：{toolIDs[modelInfo.ID].gameObject.name}-{modelInfo.gameObject.name}");
            if (operationIDs.ContainsKey(modelInfo.ID))
                Log.Warning($"存在重复UUID:{modelInfo.ID};操作道具：{operationIDs[modelInfo.ID].gameObject.name}-{modelInfo.gameObject.name}");
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
            Log.Warning($"存在未配置ModelOperation背包道具：{modelInfo.Name}");
        else
        {
            if (operationIDs.ContainsKey(modelInfo.ID))
                Log.Warning($"存在重复UUID:{modelInfo.ID};操作道具：{operationIDs[modelInfo.ID].gameObject.name}-{modelInfo.gameObject.name}");
            if (toolIDs.ContainsKey(modelInfo.ID))
                Log.Warning($"存在重复UUID:{modelInfo.ID};背包道具：{toolIDs[modelInfo.ID].gameObject.name}-{modelInfo.gameObject.name}");
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
    /// 初始化自动道具
    /// </summary>
    /// <param name="modelInfo"></param>
    private void InitAutoProp(ModelInfo modelInfo)
    {
        ModelOperation op = modelInfo.GetComponent<ModelOperation>();
        if (op == null)
            Log.Warning($"存在未配置ModelOperation道具：{modelInfo.Name}");
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
    /// 获取开关类型操作的互斥的另一状态名。
    /// 对 Switch 类型，从 operations 列表中找 name != currentState 且不是保留标志的操作名。
    /// </summary>
    public string GetOppositeSwitchState(ModelOperation modelOperation)
    {
        if (modelOperation == null || modelOperation.operations == null)
            return null;

        string currentState = modelOperation.currentState;

        foreach (var op in modelOperation.operations)
        {
            if (op.name != currentState && !IsReservedOperationName(op.name))
                return op.name;
        }
        return null;
    }

    private bool IsReservedOperationName(string opName)
    {
        return opName == focusFlag
            || opName == observeFlag
            || opName == clickFlag
            || opName == contactFlag
            || opName == inputFlag
            || opName == showFlag
            || opName == hideFlag
            || opName.StartsWith(backpackFlag);
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
            Log.Warning("当前步骤数越界，无正确操作");
            return false;
        }

        SmallOp1 data = nowFlowStep.ops.Find(value => value.optionName.Equals(optionName));
        if (data == null)
        {
            Log.Debug($"当前正确操作不是{optionName}");
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
            Log.Warning("当前步骤数越界，无正确操作");
            return false;
        }

        SmallOp1 data = nowFlowStep.ops.Find(value => value.operation.ID.Equals(id) && value.optionName.Equals(optionName));
        if (data == null)
        {
            Log.Debug($"当前正确操作不是{optionName}");
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

        SmallOp1 data = nowFlowStep.ops.Find(value => value.optionName.Equals(inputFlag));
        if (data == null)
        {
            Log.Debug("当前正确操作不是输入文本");
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
                Log.Debug("当前步骤数越界，无正确操作");
                return false;
            }
            //判断是否是正确操作物体（跳过已完成的op）
            data = nowFlowStep.ops.Find(value => value.operation == operation && !completedOpIds.Contains(operation.ID));
            if (data == null)
            {
                Log.Debug($"操作对象错误 当前对象为{operation.name}", operation);
                return false;
            }
            //判断是否选择正确道具
            if (data.prop != prop)
            {
                Log.Debug($"使用道具错误 当前道具为{(prop?.name ?? "空")} 正确道具为{(data.prop?.name ?? "空")}", prop);
                return false;
            }

            //步骤限制条件已移除，现在这里判断的是单个操作，限制条件是否满足
            List<OpRestrict> conditions = data.operation.operations.Find(value => value.name == operation.currentState).conditions;
            for (int i = 0; i < conditions.Count; i++)
            {
                if (conditions[i].optionName != conditions[i].operation.currentState)
                {
                    Log.Debug($"{operation.name} 当前状态为{conditions[i].operation.currentState} 正确条件为{conditions[i].optionName}"); 
                    return false;
                }
            }
            Log.Debug($"{operation.name} 满足操作条件");
            return true;
        }
        else
        {
            Log.Warning("操作对象为null");
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

    public SmallStep1 CurrentStep()
    {
        return nowFlowSteps[_index_NowStep];
    }

    /// <summary>
    /// 通过当前任务的索引获取应当执行的ModelOperation对象和正确行为的名称
    /// 优先返回未完成的op
    /// </summary>
    public SmallOp1 GetStepOperationBehaviors()
    {
        var steps = flows[index_NowFlow].steps;
        var step = steps[index_NowStep];
        return step.ops.FirstOrDefault(op => !completedOpIds.Contains(op.operation.ID)) ?? step.ops[0];
    }

    public ModelOperation GetStepOperation()
    {
        var steps = flows[index_NowFlow].steps;
        SmallStep1 step = steps[index_NowStep];
        var op = step.ops.FirstOrDefault(o => !completedOpIds.Contains(o.operation.ID)) ?? step.ops[0];
        return op.operation;
    }

    /// <summary>
    /// 选择步骤（合并原 SelectFlow + SelectStep，增加 flowIndex 参数）
    /// 重置流程：基准初始视角 → 前置任务步骤 → 前置步骤 → 目标步骤初始视角
    /// </summary>
    /// <param name="flowIndex">目标任务索引</param>
    /// <param name="stepIndex">目标步骤索引</param>
    /// <param name="ResetByFlow">是否刷新操作记录</param>
    /// <param name="answerOp">考核模式联机恢复数据</param>
    public void SelectStep(int flowIndex, int stepIndex, bool ResetByFlow, AnswerOp answerOp = null)
    {
        // 1. 清除高亮、重置工具数量
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

        // 跳步骤是生命周期边界：强制清空相机锁定，防止上一步异常路径漏放锁导致相机永久不跟随
        cameraLockHeld = false;
        cameraLockGeneration++;
        ModelManager.Instance.CameraLockReset();

        index_NowFlow = flowIndex;
        _index_NowStep = 0;
        ClearCompletedOps();

        isRestoringPreviousStates = true;

        //2.重置所有操作到预制体默认初始状态 恢复之前的flows的变化
        ResetAllToInitState();

        // 跨 Flow 跳转：Flow0 Step0 初始视角作为全局基准
        if (flows.Length > 0 && flows[0].steps.Count > 0)
        {
            var firstStepInitStates = flows[0].steps[0].initState;
            if (firstStepInitStates != null && firstStepInitStates.Count > 0)
            {
                foreach (var state in firstStepInitStates)
                {
                    if (state.operation != null)
                        SetFinalState(state.operation, state.optionName, true, true);
                }
            }
        }

        // 应用所有前置 Flow 中每个 Step 的操作（含递归联动）
        int indexFlow = -1;
        int indexStep = -1;
        foreach (var flow in flows.Take(flowIndex))
        {
            indexFlow += 1;
            indexStep = -1;
            foreach (var step in flow.steps)
            {
                indexStep += 1;
                foreach (var operation in step.ops)
                {
                    if (operation.operation == null)
                    {
                        Log.Error($"{step.hint} 没有配置操作对象 - {operation.optionName}");
                        continue;
                    }
                    SetFinalStateWithLinkages(operation.operation, operation.optionName,
                        ignoreCondition: true, ignoreMove: true);
                }
                // 步骤级联动（全部并列操作之后执行）
                if (step.actions != null)
                {
                    foreach (var linkage in step.actions)
                    {
                        if (linkage.operation == null)
                            continue;
                        SetFinalStateWithLinkages(linkage.operation, linkage.optionName,
                            ignoreCondition: true, ignoreMove: true);
                    }
                }
            }
        }

        // 刷新操作记录：先清空，再重建所有前置 Flow 的记录
        if (ResetByFlow)
        {
            FormMsgManager.Instance.SendMsg(new MsgIntInt((ushort)SmallFlowModuleEvent.OperatingRecordClear, -1, -1));

            int rfFlow = -1;
            int rfStep = -1;
            foreach (var flow in flows.Take(flowIndex))
            {
                rfFlow += 1;
                rfStep = -1;
                foreach (var step in flow.steps)
                {
                    rfStep += 1;
                    foreach (var operation in step.ops)
                    {
                        RefreshOpHistory(operation.operation, operation.optionName, rfFlow, rfStep);
                    }
                }
            }
        }

        // 3. 应用当前 Flow 中目标 Step 之前的步骤操作（含递归联动）
        int indexStep2 = -1;
        foreach (var step in nowFlowSteps.Take(stepIndex))
        {
            indexStep2 += 1;
            foreach (var operation in step.ops)
            {
                if (operation.operation == null)
                    continue;
                SetFinalStateWithLinkages(operation.operation, operation.optionName,
                    ignoreCondition: true, ignoreMove: true);
                if (ResetByFlow)
                    RefreshOpHistory(operation.operation, operation.optionName, index_NowFlow, indexStep2);
            }
            // 步骤级联动（全部并列操作之后执行）
            if (step.actions != null)
            {
                foreach (var linkage in step.actions)
                {
                    if (linkage.operation == null)
                        continue;
                    SetFinalStateWithLinkages(linkage.operation, linkage.optionName,
                        ignoreCondition: true, ignoreMove: true);
                }
            }
        }
        isRestoringPreviousStates = false;

        // 4. 设置当前步骤索引（触发 ExecuteInitStateSequentially 执行目标步骤的初始视角）
        index_NowStep = stepIndex;

        // 5. 显式设置角色位置（从上一步联动或当前步骤初始视角中搜索导航点）
        ApplyPlayerPositionForStepJump(stepIndex);

        // 6. 考核模式：用联机考核记录覆盖初始视角状态（考核记录优先级最高）
        // 必须在 index_NowStep 之后执行，确保分层：默认状态 → initState → 考核操作记录
        if (GlobalInfo.isExam && !ResetByFlow)
        {
            if (answerOp != null)
                SetExamModelStateData(answerOp);
            else if (NetworkManager.Instance.IsIMSyncState)
                FindObjectOfType<ExamCoursePanel>().GetComponent<ExamCoursePanel>().RefreshExamOpHistoryAsync(SetExamModelStateData);
        }
    }


    /// <summary>
    /// 使用联机考核记录设置的模型状态
    /// </summary>
    public void SetExamModelStateData(AnswerOp answerOp)
    {
        if (answerOp == null)
            return;

        List<OpDicData> examModelStates =  answerOp.modelStates?.Select(s => new OpDicData()
        {
            id = s.id,
            optionName = s.optionName,
            uiTargetModelEulerZ = float.Parse(s.uiTargetModelEulerZ)
        }).ToList();

        if (examModelStates != null)
            SetFinalState(examModelStates);

        // 使用联机考核记录刷新操作历史
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

        UIManager.Instance.canvas.GetComponentInChildren<UISmallSceneModule>(true).operationHistoryModule.UpdateOpRecordList(opRecordData ?? new List<OpRecordData>());
    }

    /// <summary>
    /// 从当前步骤的初始视角(initState)和上一步的联动步骤(actions)中向前搜索
    /// 直到找到包含Navigation数据的步骤，提取其目标Transform，找不到则返回全局视角的Transform。
    /// 供 TryExecuteOperation 的导航距离检查使用。
    /// </summary>
    private Transform ResolveStepNavigationTarget()
    {
        // 跨 flow 向前搜索角色位置
        for (int f = index_NowFlow; f >= 0; f--)
        {
            List<SmallStep1> steps = flows[f].steps;
            int startS = (f == index_NowFlow) ? index_NowStep : steps.Count - 1;

            for (int s = startS; s >= 0; s--)
            {
                SmallStep1 step = steps[s];

                // 1. 检查步骤 s 的初始视角中是否有 Navigation
                foreach (var state in step.initState)
                {
                    if (state.operation != null && state.operation.name == "Navigation")
                    {
                        Transform t = GetTransformFromNavigationOp(state.operation, state.optionName);
                        if (t != null) return t;
                    }
                }

                // 2. 检查前一个步骤的联动操作中是否有 Navigation
                SmallStep1 prevStep = null;
                if (s > 0)
                    prevStep = steps[s - 1];
                else if (f > 0)
                    prevStep = flows[f - 1].steps[flows[f - 1].steps.Count - 1];

                if (prevStep != null)
                {
                    foreach (var action in prevStep.actions)
                    {
                        if (action.operation != null && action.operation.name == "Navigation")
                        {
                            Transform t = GetTransformFromNavigationOp(action.operation, action.optionName);
                            if (t != null) return t;
                        }
                    }
                }
            }
        }

        // 回退到全局视角
        if (globalPerspective != null && !string.IsNullOrEmpty(globalPerspective.initState))
        {
            Transform t = GetTransformFromNavigationOp(globalPerspective, globalPerspective.initState);
            if (t != null) return t;
        }

        return null;
    }

    /// <summary>
    /// 从 Navigation 操作的 Pose 或 PlayerNavigation 行为中提取目标 Transform。
    /// </summary>
    private static Transform GetTransformFromNavigationOp(ModelOperation operation, string optionName)
    {
        OperationBase op = operation.operations?.Find(o => o.name.Equals(optionName));
        if (op?.behaveBases == null) return null;

        foreach (var behave in op.behaveBases)
        {
            if (behave is BehavePlayerNavigation nav && nav.ctrlGO != null)
                return nav.ctrlGO.transform;
            if (behave is BehavePose pose && pose.ctrlGO != null)
                return pose.ctrlGO.transform;
        }
        return null;
    }

    /// <summary>
    /// 选择步骤跳转时，显式执行角色位置相关设置，其他SetFinalState将跳过位置的设置，仅由此方法设置
    /// 从当前步骤的初始视角(initState)和上一步的联动步骤(actions)中向前搜索
    /// 直到找到包含位置数据的步骤，找不到则设置为默认位置和视角
    /// </summary>
    private void ApplyPlayerPositionForStepJump(int stepIndex)
    {
        // 不重置 ignoreMove：由调用方决定是否应用位置
        // CompleteStep 同步端设 true 抑制导航；SelectStep/断线重连设 false 允许设置初始位置
        bool found = false;
        // 跨 flow 向前搜索角色位置
        for (int f = index_NowFlow; f >= 0 && !found; f--)
        {
            List<SmallStep1> steps = flows[f].steps;
            int startS = (f == index_NowFlow) ? stepIndex : steps.Count - 1;

            for (int s = startS; s >= 0 && !found; s--)
            {
                SmallStep1 step = steps[s];

                // 1. 检查步骤 s 的初始视角中是否有位移
                foreach (var state in step.initState)
                {
                    if (state.operation != null && (state.operation.name == "Navigation"))
                    {
                        ExecutePositionBehaviors(state.operation, state.optionName);
                        found = true;
                        return;
                    }
                }

                // 2. 检查前一个步骤的联动操作中是否有位置相关的行为
                SmallStep1 prevStep = null;
                if (s > 0)
                {
                    prevStep = steps[s - 1];
                }
                else if (f > 0)
                {
                    prevStep = flows[f - 1].steps[flows[f - 1].steps.Count - 1];
                }

                if (prevStep != null)
                {
                    foreach (var action in prevStep.actions)
                    {
                        if (action.operation != null && (action.operation.name == "Navigation"))
                        {
                            ExecutePositionBehaviors(action.operation, action.optionName);
                            found = true;
                            return;
                        }
                    }
                }
            }
        }

        // 未找到任何位置设置，使用全局视角作为默认位置
        if (!found)
        {
            Log.Debug($"[PositionJump] 未找到位置，使用全局视角 globalPerspective={globalPerspective != null} initState={globalPerspective?.initState}");
            if (globalPerspective != null && !string.IsNullOrEmpty(globalPerspective.initState))
            {
                ExecutePositionBehaviors(globalPerspective, globalPerspective.initState);
            }
        }
    }


    /// <summary>
    /// 直接执行位置相关的行为（Pose或PlayerNavigation），绕过 ignoreMove 检查
    /// 但协同模式下非操作者被CompleteStep同步时跳过，仅显式跳步骤(SelectStep)时应用位置
    /// </summary>
    private void ExecutePositionBehaviors(ModelOperation operation, string optionName)
    {
        if (ignoreMove)
            return;

        OperationBase op = operation.operations?.Find(o => o.name.Equals(optionName));
        if (op?.behaveBases == null)
        {
            return;
        }
        foreach (var behave in op.behaveBases)
        {
            if (behave.behaveType == BehaveType.Pose || behave.behaveType == BehaveType.PlayerNavigation)
            {
                try { behave.SetFinalState(); }
                catch (System.Exception e) { Log.Debug($"[PositionJump] 行为执行异常: {e.Message}"); }
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
    /// <param name="self">为true 表示是本人操作；为false表示是其他玩家操作（不执行相机移动、角色导航等操作表现）</param></param>
    public void TryExecuteFreeOperation(SmallOp1 data, string userNo, string userName, bool self)
    {
        VariableJoystick.tapRotationTriggered = false;
        GlobalInfo.WaitUiOq = false;
        NetworkManager.Instance.IsIMSync = false;
        ignoreMove = !self;  // 其他玩家操作时忽略移动和动画
        IsCurrentOperationExecutor = self;
        // 上一次操作若走了异常路径没能释放，这里先补放并推进代次，使旧的延迟释放回调失效
        BeginCameraLockScope();
        if (self && IsCameraStationaryOperation(data.operation))
        {
            ModelManager.Instance.CameraLockAcquire();
            cameraLockHeld = true;
        }

        string modelInfoId = data.operation?.GetComponent<ModelInfo>()?.ID;
        SmallOp1 stepOp;
        bool isOnOperation = IsOnOperation(data.operation, data.prop, out stepOp);
        string effectiveOptionName = stepOp?.optionName ?? data.optionName;

        Log.Debug("调试 当前需要执行:" + nowFlowStep.ID + " 执行结果： " + isOnOperation);
        FormMsgManager.Instance.SendMsg(new MsgStringBool((ushort)SmallFlowModuleEvent.StartExecute, modelInfoId, self));

        ExecuteOperation(data.operation, effectiveOptionName, data.prop, (op) =>
        {
            ModelOperationEventManager.Publish(new ModelStateEvent(modelInfoId, effectiveOptionName));
            if (op != null)
            {
                OnFreeOperationInvoked?.Invoke();

                RunAction(op.actions.FindAll(a => a.operation != null), () =>
                {
                    Log.Debug("联动操作 操作对象的联动执行完成");
                    if (!string.IsNullOrEmpty(op.hint_success) && IsCurrentOperationExecutor)
                    {
                        // 仅本人操作才发送分数上传消息
                        SendOperatingRecordMsg(stepOp ?? data, op, userNo, userName, isOnOperation && IsCurrentOperationExecutor, data.prop);
                    }
                    // 全部并列操作完成后，执行步骤级联动再进入下一步
                    if (isOnOperation && MarkOpCompleted(stepOp ?? data))
                    {
                        List<OpLinkage> opLinkages = BuildLinkageOperations(nowFlowStep);
                        if (opLinkages.Count != 0)
                        {
                            ExecuteFlowLinkOperation(opLinkages, () =>
                            {
                                Log.Debug("联动操作 流程的联动执行完成");
                                Next();
                            }, 0);
                        }
                        else
                            Next();
                    }
                    else
                        Over();
                }, 0);
            }
            else
            {
                Log.Warning("执行了TryExecuteOperation未处理的分支");
            }
        });
    }

    void Over()
    {
        OnStepAdvanced?.Invoke(index_NowFlow, index_NowStep);
        if (IsCurrentOperationExecutor)
            {
                // 步骤未结束时，瞄准下一个需点击的操作目标
                AimCameraAtNextOp();
                // 延迟回调期间可能已开始新操作，用当前代次做校验，避免放掉新操作的锁
                int generation = cameraLockGeneration;
                //发送步骤操作结束消息
                DOVirtual.DelayedCall(0.1f, () =>
                {
                    ReleaseCameraLock(generation);
                    ToolManager.SendBroadcastMsg(new MsgBase((ushort)SmallFlowModuleEvent.CompleteExecute));
                });
            }
        else
            ReleaseCameraLock(cameraLockGeneration);
    }

    /// <summary>
    /// 本次操作（含操作对象联动、步骤联动）全部执行完毕，释放相机锁定，允许相机重新跟随角色。
    /// </summary>
    /// <param name="generation">获取锁时的代次；与当前代次不符说明已开始新操作，本次释放作废</param>
    private void ReleaseCameraLock(int generation)
    {
        if (!cameraLockHeld || generation != cameraLockGeneration)
            return;
        cameraLockHeld = false;
        ModelManager.Instance.CameraLockRelease();
    }

    /// <summary>
    /// 开始新操作前调用：补放上一次操作可能残留的锁，并推进代次使旧的延迟释放回调失效。
    /// </summary>
    private void BeginCameraLockScope()
    {
        ReleaseCameraLock(cameraLockGeneration);
        cameraLockGeneration++;
    }

    /// <summary>
    /// 开关门并不隐藏模型
    /// </summary>
    private bool IsCameraStationaryOperation(ModelOperation operation)
    {
        ModelInfo modelInfo = operation.GetComponent<ModelInfo>();
        if (modelInfo?.InfoData?.InteractMode == InteractMode.Switch && modelInfo.Name.Contains("门"))
            return false;

        return true;
    }

    /// <summary>
    /// 执行操作
    /// </summary>
    /// <param name="data"></param>
    /// <param name="correctOp">是否是当前步骤的正确操作</param>
    /// <param name="userNo">操作人工号</param>
    /// <param name="userName">操作人姓名</param>
    /// <param name="callback"></param>
    /// <param name="self">为true 表示非本人操作；不执行相机移动、角色导航等操作表现</param>
    public void TryExecuteOperation(SmallOp1 data, bool correctOp, string userNo, string userName, bool self)
    { 
        GlobalInfo.WaitUiOq = false;
        ignoreMove = !self;  // 其他玩家操作时忽略移动和动画
        IsCurrentOperationExecutor = self;
        // 上一次操作若走了异常路径没能释放，这里先补放并推进代次，使旧的延迟释放回调失效
        BeginCameraLockScope();
        string modelInfoId = data.operation != null ? data.operation.ID : string.Empty;
        if (self && IsCameraStationaryOperation(data.operation))
        {
            ModelManager.Instance.CameraLockAcquire();
            cameraLockHeld = true;
        }
        FormMsgManager.Instance.SendMsg(new MsgStringBool((ushort)SmallFlowModuleEvent.StartExecute, modelInfoId, self));

        // 并列操作序号，用于匹配对应语音
        int opIndex = nowFlowStep.ops.FindIndex(o => o.operation == data.operation && o.optionName == data.optionName);
        if (opIndex < 0) opIndex = 0;
        SpeechManager.Instance.PlayImmediate(nowFlowStep.ID, opIndex, TipType.Tips);
        ExecuteOperation(data.operation, data.optionName, data.prop, (op) =>
        {
            if (op != null)
            {
                ModelOperationEventManager.Publish(new ModelStateEvent(modelInfoId, data.optionName));

                RunAction(op.actions.FindAll(a => a.operation != null), () =>
                {
                    if (!string.IsNullOrEmpty(op.hint_success) && IsCurrentOperationExecutor)
                    {
                        SendOperatingRecordMsg(data, op, userNo, userName, correctOp, data.prop);
                    }
                    WaitUadioToNext(() =>
                    {
                        int stepOpIndex = nowFlowStep.ops.FindIndex(o => o.operation == data.operation && o.optionName == data.optionName);
                        if (stepOpIndex < 0) stepOpIndex = 0;
                        SpeechManager.Instance.PlayImmediate(nowFlowStep.ID, stepOpIndex, TipType.StepComplete);

                        // 全部并列操作完成后，执行步骤级联动再进入下一步
                        if (MarkOpCompleted(data))
                        {
                            List<OpLinkage> opLinkages = BuildLinkageOperations(nowFlowStep);
                            if (opLinkages.Count != 0)
                            {
                                ExecuteFlowLinkOperation(opLinkages, () =>
                                {
                                    Next();
                                }, 0);
                            }
                            else
                                Next();
                        }
                        else
                            Over();
                    }).Forget();
                }, 0);
            }
            else
            {
                Log.Warning("执行了TryExecuteOperation未处理的分支");
            }
        });
    }


    /// <summary>
    /// 培训模式等待结束提示播放完成才进入下一步
    /// </summary>
    async UniTaskVoid WaitUadioToNext(Action action)
    {
        if (SpeechManager.Instance.SpeechMode)
        {
            await UniTask.Yield();
            audioNotPlayingPredicate = () => !SpeechManager.Instance.audioSource.isPlaying;
            await UniTask.WaitUntil(audioNotPlayingPredicate, cancellationToken: this.GetCancellationTokenOnDestroy());
            action.Invoke();
        }
        else
            action.Invoke();
    }

    /// <summary>
    /// 查找操作中指定类型的行为
    /// </summary>
    /// <typeparam name="T">行为类型</typeparam>
    /// <param name="operation">操作对象</param>
    /// <param name="optionName">操作名称</param>
    /// <param name="found">是否找到</param>
    /// <param name="behave">找到的行为实例</param>
    private void FindBehave<T>(ModelOperation operation, string optionName, ref bool found, ref T behave) where T : BehaveBase
    {
        foreach (var op in operation.operations)
        {
            if (op.name.Equals(optionName))
            {
                if (op.behaveBases != null)
                {
                    foreach (var b in op.behaveBases)
                    {
                        if (b is T t)
                        {
                            found = true;
                            behave = t;
                            return;
                        }
                    }
                }
                return;
            }
        }
    }

    /// <summary>
    /// 执行联动操作流程
    /// </summary>
    /// <param name="opLinkages">联动操作列表</param>
    /// <param name="callback">完成回调</param>
    /// <param name="index">当前执行索引</param>
    private void ExecuteFlowLinkOperation(List<OpLinkage> opLinkages, Action callback, int index = 0)
    {
        if (opLinkages.Count == index)
        {
            callback?.Invoke();
            return;
        }

        bool dummy = !IsCurrentOperationExecutor;

        // 非执行者跳过相机和移动相关操作
        if (dummy && IsDummySkipOperation(opLinkages[index].operation, opLinkages[index].optionName, true))
        {
            ExecuteFlowLinkOperation(opLinkages, callback, ++index);
            return;
        }

        var op = opLinkages[index];

        // 检查是否有弹窗（非考核）
        bool hasPopup = false;
        BehavePopup popupBehave = null;

        bool hasguide = false;
        BehavePlayerNavigation guideBehave = null;

        FindBehave(op.operation, op.optionName, ref hasPopup, ref popupBehave);
        FindBehave(op.operation, op.optionName, ref hasguide, ref guideBehave);

        if (hasPopup)
        {
            // 统计当前联动前有多少个弹窗联动，用于计算语音序号
            int popupVoiceIndex = 0;
            for (int i = 0; i < index; i++)
            {
                bool prevHasPopup = false;
                BehavePopup prevPopup = null;
                bool prevHasGuide = false;
                BehavePlayerNavigation prevGuide = null;
                FindBehave(opLinkages[i].operation, opLinkages[i].optionName, ref prevHasPopup, ref prevPopup);
                if (prevHasPopup) popupVoiceIndex++;
            }
            SpeechManager.Instance.PlayImmediate(nowFlowStep.ID, nowFlowStep.ops.Count + GetInitStatePopupVoiceCount() + popupVoiceIndex, TipType.Tips);

            // 有弹窗：显示弹窗，等确认后再继续（不再依赖闭包捕获的dummy，由IsCurrentOperationExecutor判断）
            onPopupStateChanged.Invoke(true);
            popupBehave.Execute(() =>
            {
                onPopupStateChanged.Invoke(false);
                ToolManager.SendBroadcastMsg(new MsgBool((ushort)SmallFlowModuleEvent.ClousePop, true));
                ExecuteFlowLinkOperation(opLinkages, callback, ++index);
            });
        }
        else if (hasguide)
        {
            if(GlobalInfo.courseMode == CourseMode.Training)
            {
                guideBehave.Execute(() =>
                {
                    //培训模式需要走到下一个位置才能开始下一步语音
                    ExecuteFlowLinkOperation(opLinkages, callback, ++index);
                });
            }
            else if (ignoreMove || GlobalInfo.IsExamMode())
            {
                // 考核模式下不执行导航，或者被标记为跳过导航时跳过
                ExecuteFlowLinkOperation(opLinkages, callback, ++index);
            }
            else
            {
                guideBehave.Execute();
                ExecuteFlowLinkOperation(opLinkages, callback, ++index);
            }
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
                        ExecuteFlowLinkOperation(opLinkages, callback, ++index);
                    }, 0);
                }
                else
                {
                    ExecuteFlowLinkOperation(opLinkages, callback, ++index);
                }
            }
            else
            {
                ExecuteFlowLinkOperation(opLinkages, callback, ++index);
            }
        }
    }

    /// <summary>
    /// 完成联系/输入等UI操作：标记完成 + 执行步骤级联动 + 进入下一步
    /// 解决 OnContact/OnInput 直接调 GotoNextStep → Next 而跳过联动和 MarkOpCompleted 的问题
    /// </summary>
    public void CompleteUIOperation(string optionName)
    {
        SmallOp1 data = nowFlowStep?.ops?.Find(value => value.optionName.Equals(optionName));
        if (data == null) return;

        if (MarkOpCompleted(data))
        {
            List<OpLinkage> opLinkages = BuildLinkageOperations(nowFlowStep);
            if (opLinkages.Count != 0)
            {
                ExecuteFlowLinkOperation(opLinkages, () =>
                {
                    Next();
                }, 0);
            }
            else
                Next();
        }
        else
            Over();
    }

    /// <summary>
    /// 构建步骤级联动操作列表（全部并列操作完成后执行）
    /// </summary>
    /// <param name="smallStep">当前步骤</param>
    /// <returns>联动操作列表</returns>
    private List<OpLinkage> BuildLinkageOperations(SmallStep1 smallStep)
    {
        List<OpLinkage> opLinkages = new List<OpLinkage>();
        if (smallStep?.actions == null)
            return opLinkages;

        for (int i = 0; i < smallStep.actions.Count; i++)
        {
            if (smallStep.actions[i].operation == null)
                continue;

            OpLinkage opLinkage = new OpLinkage();
            opLinkage.operation = smallStep.actions[i].operation;
            opLinkage.optionName = smallStep.actions[i].optionName;
            opLinkage.useCallback = smallStep.actions[i].useCallback;
#if UNITY_EDITOR
            opLinkage.state = smallStep.actions[i].state;
#endif
            opLinkages.Add(opLinkage);
        }
        return opLinkages;
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
                        Log.Warning(info.ID + "-" + optionName + "操作正在执行!");
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

        Log.Warning(operation.name + "-" + optionName + "操作没有配置");
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
    public void ExecuteOperation(ModelOperation operation, string optionName, ModelInfo prop = null, Action<OperationBase> callback = null)
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
                        Log.Warning($"{info.ID}-{optionName}不满足限制条件:  道具:{group.Key.GetComponent<ModelInfo>().ID} 状态：{group.Key.currentState}");//:{op.conditions[j].optionName}
                        callback?.Invoke(null);
                        return;
                    }
                }

                if (cache.TryGetValue(info.ID, out string executingOp))
                {
                    if (executingOp.Equals(optionName))
                    {
                        Log.Warning(info.ID + "-" + optionName + "操作正在执行!");
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

                //Log.Debug(operation.name + "-执行行为-" + optionName);
                cache.Add(info.ID, optionName);

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

                        Log.Debug(operation.name + "-" + optionName + "操作执行完成");
                        CheckKeywords(operation, optionName, false);
                        callback?.Invoke(op);
                    }
                });
                return;
            }
        }

        Log.Warning(operation.name + "-" + optionName + "操作没有配置");
        callback?.Invoke(null);
    }

    // 需要在 dummy 模式下 同步过程中跳过的行为类型 相机和移动和表现 
    private static readonly HashSet<BehaveType> DummySkipBehaveTypes = new HashSet<BehaveType>
    {
        BehaveType.CameraFollow,    // 相机跟随
        BehaveType.ObserveRotate,   // 围绕观察
        BehaveType.Focus,           // 聚焦
        BehaveType. Observe,        // 观察

        BehaveType.PlayerNavigation,// 角色寻路
        BehaveType.Thermometring,   // 测量温度
    };

    // 需要在 dummy 模式下 联动同步过程中跳过的行为类型（相机和移动相关）
    private static readonly HashSet<BehaveType> DummySkipBehaveTypes_link = new HashSet<BehaveType>
    {
        BehaveType.PlayerNavigation,// 角色寻路
        BehaveType.Pose
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
                // 检查是否所有行为包含需要跳过的类型
                foreach (var behave in op.behaveBases)
                {
                    if (IsDummySkipBehavior(behave.behaveType, link))
                    {
                        return true;
                    }
                }
                return false;
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
    public void Execute(List<BehaveBase> behaveBases, int index, int max, UnityAction onComplete)
    {
        if (index < max)
        {
            var currentBehave = behaveBases[index];

            // 跳过相机和移动相关行为
            if (!IsCurrentOperationExecutor && IsDummySkipBehavior(currentBehave.behaveType, false))
            {
                Execute(behaveBases, ++index, max, onComplete);
                return;
            }

            //勾选等待执行完成时等待上一表现执行完成再执行下一表现，或执行最后一个表现时等待表现执行完成后再执行操作完成回调 新增：对自定义脚本的接口的UseCallback获取该行为是否要等待
            bool isLastBehavior = index == max - 1;
            bool isCustomScriptWithCallback = currentBehave is BehaveCustomScript customScript && currentBehave.ctrlGO != null && currentBehave.ctrlGO.GetComponent<IBaseBehaviour>()?.UseCallback(customScript.Step) == true;
            bool shouldWait = currentBehave.useCallBack || isLastBehavior || isCustomScriptWithCallback;

            if (shouldWait)
            {
                currentBehave.Execute(() =>
                {
                    Execute(behaveBases, ++index, max, onComplete);
                });
            }
            else
            {
                currentBehave.Execute(null);
                Execute(behaveBases, ++index, max, onComplete);
            }
        }
        else
            onComplete.Invoke();
    }

    /// <summary>
    /// 执行联动操作
    /// </summary>
    public void RunAction(List<OpLinkage> actions, Action callBack = null, int index = 0)
    {
        if (index >= actions.Count)
        {
            callBack?.Invoke();
            return;
        }

        //  非自身操作跳过相机和移动相关操作
        if (!IsCurrentOperationExecutor && IsDummySkipOperation(actions[index].operation, actions[index].optionName, false))
        {
            RunAction(actions, callBack, ++index);
            return;
        }

        if (actions[index].useCallback)
        {
            ExecuteOperation(actions[index].operation, actions[index].optionName, null, isOn =>
            {
                if (isOn != null)
                    RunAction(actions[index].operation.operations.Find(value => value.name.Equals(actions[index].optionName)).actions.FindAll(a => a.operation != null), () => RunAction(actions, callBack, ++index), 0);
                else
                    RunAction(actions, callBack, ++index);
            });
        }
        else
        {
            ExecuteAction(actions[index]).Forget();
            RunAction(actions, callBack, ++index);
        }
    }

    /// <summary>
    /// 确保不等待执行完毕的联动操作正常执行
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    private async UniTaskVoid ExecuteAction(OpLinkage action)
    {
        await UniTask.Yield();
        ExecuteOperation(action.operation, action.optionName, null, isOn =>
        {
            if (isOn != null)
            {
                RunAction(action.operation.operations.Find(value => value.name.Equals(action.optionName)).actions.FindAll(a => a.operation != null), null, 0);
            }
        });
    }

    /// <summary>
    /// 仅设置点击目标（无高亮效果），用于考核模式下路由点击到正确的 ModelOperation
    /// </summary>
    public void SetClickTarget(Component component)
    {
        if (component == null)
            return;

        if (component.TryGetComponent(out ModelRestrict modelRestrict))
        {
            var operation = component.GetComponent<ModelOperation>();
            if (operation == null)
                return;

            foreach (var node in modelRestrict.modelHighlight.highlightNodes)
            {
                if (node == null)
                    continue;

                var boxEvents = node.GetComponentsInChildren<CollisionBoxMouseEvent>(true);
                foreach (var be in boxEvents)
                    be.Target = operation;
            }
        }
    }

    /// <summary>
    /// 清除点击目标
    /// </summary>
    public void ClearClickTarget(Component component)
    {
        if (component == null)
            return;

        if (component.TryGetComponent(out ModelRestrict modelRestrict))
        {
            foreach (var node in modelRestrict.modelHighlight.highlightNodes)
            {
                if (node == null)
                    continue;

                var boxEvents = node.GetComponentsInChildren<CollisionBoxMouseEvent>(true);
                foreach (var be in boxEvents)
                    be.Target = null;
            }
        }
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
            var operation = component.GetComponent<ModelOperation>();

            foreach (var node in modelRestrict.modelHighlight.highlightNodes)
            {
                if (node == null)
                    continue;

                HighlightEffectManager.Instance.Add(node, priority == 0 ? hintHighlight : selectHighlight,
                   modelRestrict.modelHighlight.outlineWidth, modelRestrict.modelHighlight.visibility, modelRestrict.modelHighlight.constantWidth, priority);

                if (priority == 0)
                    HighlightEffectManager.Instance.HighlightFlashing(node);
                else
                    HighlightEffectManager.Instance.RemoveHighlightFlashing(node);

                if (operation != null)
                {
                    var boxEvents = node.GetComponentsInChildren<CollisionBoxMouseEvent>(true);
                    foreach (var be in boxEvents)
                        be.Target = operation;
                }
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
            foreach (var node in modelRestrict.modelHighlight.highlightNodes)
            {
                if (node == null)
                    continue;

                HighlightEffectManager.Instance.Remove(node, priority);
                if (priority == 0)
                    HighlightEffectManager.Instance.RemoveHighlightFlashing(node);
                else
                    HighlightEffectManager.Instance.HighlightFlashing(node);
            }

            if (priority == 0 && modelRestrict.modelGhost.ghostNode != null)
            {
                modelRestrict.modelGhost.ghostNode.gameObject.SetActive(false);
            }
        }
    }

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
    /// 设置最终状态并递归处理所有嵌套联动
    /// </summary>
    private void SetFinalStateWithLinkages(ModelOperation operation, string optionName,
        bool ignoreCondition = false, bool ignoreMove = false)
    {
        if (operation == null || string.IsNullOrEmpty(optionName))
            return;

        OperationBase op = operation.operations?.Find(o => o.name.Equals(optionName));
        if (op == null)
            return;

        if (!ignoreCondition && op.conditions != null)
        {
            var conditionGroup = op.conditions.GroupBy(c => c.operation);
            foreach (var group in conditionGroup)
            {
                if (!group.ToList().Select(c => c.optionName).Contains(group.Key.currentState))
                {
                    Log.Warning($"{operation.name} 条件不满足");
                    return;
                }
            }
        }

        CheckKeywords(operation, optionName, false);

        if (!IsStateIndependentOption(optionName))
        {
            operation.currentState = optionName;
        }

        ExecuteBehaviorsSafely(op.behaveBases, ignoreMove);

        if (op.actions != null)
        {
            foreach (var linkage in op.actions)
            {
                if (linkage.operation == null)
                    continue;

                ModelInfo linkageInfo = linkage.operation.GetComponent<ModelInfo>();
                if (linkageInfo != null && linkageInfo.PropType == PropType.Auto)
                    continue;

                SetFinalStateWithLinkages(linkage.operation, linkage.optionName,
                    ignoreCondition: false, ignoreMove);
            }
        }
    }

    private bool IsStateIndependentOption(string optionName)
    {
        return optionName.Equals(observeFlag)
            || optionName.Equals(focusFlag)
            || optionName.Equals(inputFlag)
            || optionName.Equals(clickFlag)
            || optionName.StartsWith(backpackFlag)
            || optionName.StartsWith(retrieveFlag);
    }

    /// <summary>
    /// 安全执行行为列表，处理隐藏对象问题
    /// 先激活对象，执行其他行为，最后处理隐藏行为
    /// </summary>
    private void ExecuteBehaviorsSafely(List<BehaveBase> behaveBases, bool ignoreMove)
    {
        if (behaveBases == null || behaveBases.Count == 0)
            return;

        List<BehaveActivate> activateBehaviors = new List<BehaveActivate>();
        List<BehaveBase> otherBehaviors = new List<BehaveBase>();

        foreach (var behave in behaveBases)
        {
            if (ignoreMove && (behave.behaveType == BehaveType.Pose ||
                behave.behaveType == BehaveType.PlayerNavigation))
                continue;

            if (behave is BehaveActivate activate)
                activateBehaviors.Add(activate);
            else
                otherBehaviors.Add(behave);
        }

        //先执行激活行为（使对象显示）
        foreach (var activate in activateBehaviors)
        {
            if (activate.ctrlGO != null && activate.isActive)
            {
                activate.ctrlGO.SetActive(true);
            }
        }

        //执行其他行为（对象已显示）
        foreach (var behave in otherBehaviors)
        {
            try
            {
                behave.SetFinalState();
            }
            catch
            {
                Log.Debug($"行为 {behave.behaveType} 执行失败");
            }
        }

        //执行隐藏行为
        foreach (var activate in activateBehaviors)
        {
            if (activate.ctrlGO != null && !activate.isActive)
            {
                activate.ctrlGO.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 设置为最终状态
    /// </summary>
    /// <param name="operation">设置物体</param>
    /// <param name="optionName">设置操作</param>
    /// 是否跳过操作前置条件检查。
    /// 是否递归处理联动操作
    /// 是否跳过角色位移
    public void SetFinalState(ModelOperation operation, string optionName, bool ignoreCondition = false, bool processLinkages = true)
    {
        // 移除旧的考核模式 hack：导航抑制已在 ExecuteFlowLinkOperation 的 hasguide 分支统一处理
        // 此处强制设置 ignoreMove 会破坏断线重连时的位置恢复
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
                                Log.Warning($"{group.Key.GetComponent<ModelInfo>().Name}道具当前状态：{group.Key.currentState},不满足限制条件");//:{op.conditions[j].optionName}
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
                            if (ignoreMove && (op.behaveBases[k].behaveType == BehaveType.Pose || op.behaveBases[k].behaveType == BehaveType.PlayerNavigation))
                                continue;
                            op.behaveBases[k].SetFinalState();
                        }
                        catch
                        {
                            Log.Debug(operation.name + "    " + op.behaveBases[k].behaveType + "  该物体没有配置最终状态");
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
                                    SetFinalState(op.actions[m].operation, op.actions[m].optionName, false, true);
                            }
                            catch (Exception e)
                            {
                                Log.Error($"{operation.name}  actions[{m}]配置错误: operation={(op.actions[m].operation == null ? "null" : op.actions[m].operation.name)}, optionName={op.actions[m].optionName}, 异常: {e.Message}");
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 将所有操作道具重置到初始状态
    /// 用于协同/考核状态恢复前的准备
    /// </summary>
    public void ResetAllToInitState()
    {
        foreach (var modelOperation in operationIDs)
        {
            if (!string.IsNullOrEmpty(modelOperation.Value.initState)
                && !modelOperation.Value.currentState.Equals(modelOperation.Value.initState))
            {
                SetFinalState(modelOperation.Value, modelOperation.Value.initState, ignoreCondition: true, processLinkages: false);
            }
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
        UISmallSceneOperationHistory.OpType opType = UISmallSceneOperationHistory.OpType.Input;
        if (op != null)
        {
            if (op.name.Equals(contactFlag)) 
                opType = UISmallSceneOperationHistory.OpType.Contact;
            else
                opType = UISmallSceneOperationHistory.OpType.Operation;
            FormMsgManager.Instance.SendMsg(new MsgOperatingRecord((ushort)SmallFlowModuleEvent.OperatingRecordInput, op.hint_success, -1,
                      string.Empty, string.Empty, opType));
        }
    }

    /// <summary>
    /// 下一步
    /// </summary>
    /// <param name="allowPositionRestore">是否允许恢复位置（用于断线重连恢复到当前步骤）</param>
    public void Next(bool allowPositionRestore = false)
    {
        if (GlobalInfo.WaitUiOq)
        {
            return;
        }

        // 等待UI操作时不再走到 Over，这里必须补放相机锁，否则相机会永久不跟随角色
        ReleaseCameraLock(cameraLockGeneration);
        UIManager.Instance.canvas.GetComponentInChildren<UISmallSceneModule>(true).ReleaseCursorFree();
        if (index_NowFlow <= flows.Length - 1)
        {
            // 位置只在初始化场景允许：断线重连恢复到当前步骤时放行
            // 正常推进：考核模式全员抑制；非考核协同仅操作者恢复，队友抑制
            if (allowPositionRestore)
                ignoreMove = false;
            else
                ignoreMove = GlobalInfo.IsExamMode() || !IsCurrentOperationExecutor;
            if (index_NowStep < nowFlowSteps.Count - 1)
            {
                index_NowStep += 1;
            }
            else
            {
                if (index_NowFlow + 1 > flows.Length - 1)
                {
                    Log.Debug("已完成所有任务");
                }
                else
                {
                    index_NowFlow += 1;
                    index_NowStep = 0;
                }
            }
        }

        //发送步骤完成消息
        if (IsCurrentOperationExecutor)
        {
            ToolManager.SendBroadcastMsg(new MsgIntInt((ushort)SmallFlowModuleEvent.CompleteStep, index_NowFlow, index_NowStep));
            Over();
        }
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
    /// 特殊情况使用的操作记录广播
    /// </summary>
    public void RecordCurrentStepOperations()
    {
        if (nowFlowStep == null || nowFlowStep.ops == null)
            return;

        foreach (var opData in nowFlowStep.ops)
        {
            if (opData.operation == null)
                continue;

            OperationBase op = opData.operation.operations.Find(o => o.name.Equals(opData.optionName));
            if(op!= null)
            {
                string hint = flows[index_NowFlow].steps[index_NowStep].hint_success;
                if (hint == "" || hint == "完成提示")
                    hint = op.hint_success;

                float score = GetStepScore(true);
                ToolManager.SendBroadcastMsg(new MsgOperatingRecord(
                    (ushort)SmallFlowModuleEvent.OperatingRecord,
                    hint,
                    -1,
                    GlobalInfo.account.userNo,
                    GlobalInfo.account.nickname,
                    OpType.Operation,
                    true,
                    score,
                    TotalStepIndex));
                return;
            }
        }
        
    }

    /// <summary>
    /// 发送操作记录消息
    /// </summary>
    /// <param name="data">操作数据</param>
    /// <param name="op">操作配置，为null时直接使用道具名称作为提示</param>
    /// <param name="userNo">操作人工号</param>
    /// <param name="userName">操作人姓名</param>
    /// <param name="isCorrect">是否正确操作</param>
    /// <param name="actualProp">实际使用的道具</param>
    private void SendOperatingRecordMsg(SmallOp1 data, OperationBase op, string userNo, string userName, bool isCorrect = false, ModelInfo actualProp = null)
    {
        //操作成功的文本提示
        string stepHint = flows[index_NowFlow].steps[index_NowStep].hint_success;
        string hint = stepHint;
        bool fallback = hint == "" || hint == "完成提示" || !isCorrect;
        if (fallback)
        {
            //未在流程中配置步骤完成提示
            hint = op.hint_success;
        }

        // 道具使用记录
        if (actualProp != null)
            hint = $"已使用{actualProp.Name}，" + hint;

        // 并列操作中任意一个错误（含未使用道具、使用错道具），整步不给分
        if (!isCorrect)
            _stepHasIncorrectOp = true;
        float score = GetStepScore(isCorrect && !_stepHasIncorrectOp);

        MsgOperatingRecord msg = new MsgOperatingRecord(
            (ushort)SmallFlowModuleEvent.OperatingRecord,
            hint,
            -1,
            userNo,
            userName,
            OpType.Operation,
            true,
            score,
            TotalStepIndex);
        ToolManager.SendBroadcastMsg(msg);
    }

    /// <summary>
    /// 获取当前步骤分数（正确操作时从服务端数据按索引取步骤分数）
    /// </summary>
    /// <param name="isCorrect">是否正确操作</param>
    /// <returns></returns>
    private float GetStepScore(bool isCorrect)
    {
        if (!isCorrect)
            return 0;

        EncyclopediaOperation encyclopediaOp = GlobalInfo.currentWiki as EncyclopediaOperation;
        if (encyclopediaOp == null || encyclopediaOp.flows == null)
            return 0;

        if (index_NowFlow < 0 || index_NowFlow >= encyclopediaOp.flows.Count)
            return 0;

        Flow flow = encyclopediaOp.flows[index_NowFlow];
        if (flow.children == null || index_NowStep < 0 || index_NowStep >= flow.children.Count)
            return 0;

        return flow.children[index_NowStep].score;
    }

    /// <summary>
    /// 获取IBaseBehaviour对应的操作对象ID
    /// </summary>
    /// <param name="behaviour">自定义行为脚本</param>
    /// <returns>操作对象ID（ModelInfo.ID）</returns>
    public string GetCurrentOperationId(IBaseBehaviour behaviour)
    {
        MonoBehaviour mono = behaviour as MonoBehaviour;
        if (mono == null)
            return string.Empty;
        ModelInfo modelInfo = mono.GetComponentInParent<ModelInfo>();
        return modelInfo != null ? modelInfo.ID : string.Empty;
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
        //恢复前置步骤状态时跳过图纸添加，图纸只在对应步骤初始视角中打开
        if (isRestoringPreviousStates)
            return;

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


            DOVirtual.DelayedCall(0.3f, () =>
            {
                onSchematicAdded.Invoke(modelInfo);
            });
        }
        else
        {
            // 触发图纸添加事件 进入下一步会刷新工具栏状态，需要先刷再生成，才能隐藏工具栏
            DOVirtual.DelayedCall(0.3f, () =>
            {
                onSchematicAdded.Invoke(toolIDs[schematicSprite.name]);
            });
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    /// <summary>
    /// 设置为最终状态
    /// 协同状态同步 已
    /// </summary>
    public void SetFinalState(List<OpDicData> modelStates)
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
