using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityFramework.Runtime;
using Cysharp.Threading.Tasks;

/// <summary>
/// Ua, Ub, Uc / Uab, Ubc, Uca
/// Ia, Ib, Ic
/// P Q cosφF 转速 这些关键值在不同情况下的值
/// </summary>
public class LCU_mlfsjs : MonoBase, IBaseBehaviour
{
    bool IBaseBehaviour.UseCallback(int step) => true;
    public Type GetStatusEnumType() => typeof(AvailableStatus);

    // 在 Awake 中提前注册消息，避免 Start 时序晚于消息到达
    protected virtual void Awake()
    {
        AddMsg(new ushort[] {
            (ushort)SmallFlowModuleEvent.SynchronizationLcu
        });
    }

    protected override void InitComponents()
    {
        // 消息注册已移至 Awake
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        if (msg.msgId == (ushort)SmallFlowModuleEvent.SynchronizationLcu)
        {
            MsgBrodcastOperate brodcastMsg = msg as MsgBrodcastOperate;
            // 过滤自己发送的消息
            if (brodcastMsg.senderId == GlobalInfo.account.id) return;

            MsgSyncCustomUI msgUI = brodcastMsg.GetData<MsgSyncCustomUI>();
            WaitStepInit(msgUI).Forget();
        }
    }

    async UniTaskVoid WaitStepInit(MsgSyncCustomUI msgUI)
    {
        SetImageRaycast(true);
        GlobalInfo.WaitUiOq = true;

        if (steps.Count == 0)
            DealEvent((AvailableStatus)msgUI.status);

        await UniTask.WaitUntil(() => steps.Count > 0, cancellationToken: this.GetCancellationTokenOnDestroy());

        // 逐一追齐到发送方的步骤进度（TryToNext 完成最后一步时自动触发 callback）
        isSyncing = true;
        while (currentStepIndex < msgUI.stepIndex && currentStepIndex < steps.Count)
        {
            string stepName = steps[currentStepIndex];
            if (TryToNext(stepName))
                ButenEvent(stepName);
            else
                break;
        }
        isSyncing = false;

        await UniTask.Yield();
        SetTip();
    }

    [SerializeField]
    public enum AvailableStatus
    {
        发送开机令,
        发送断路器合令,
        发送断路器分令,
        发送停机令,
    }


    public Image dlq;
    public Image zttb;
    public List<Text> bjxx;
    public List<Text> cs;
    UnityAction callback;
    UnityAction Othercallback;
    UISmallSceneModule smallSceneModule;

    AvailableStatus status;
    string modelOperationId;
    void IBaseBehaviour.Execute(int step, UnityAction callback)
    {
        if (smallSceneModule == null)
        {
            smallSceneModule = Transform.FindObjectOfType<UISmallSceneModule>().GetComponent<UISmallSceneModule>();
        }
        Othercallback = callback;
        Othercallback += () =>
        {
            steps.Clear();
            SetImageRaycast(true);
        };

        status = (AvailableStatus)step;
        modelOperationId = GetComponentInParent<ModelInfo>()?.ID;
        DealEvent(status);
    }

    public Text TsqztText;
    public TSQ_TsqXsp TSQ_TsqXsp;
    public void DealEvent(AvailableStatus status)
    {
        steps.Clear();
        switch (status)
        {
            case AvailableStatus.发送开机令:
                //增加流程检测
                if (!kzjm.activeSelf)
                {
                    string[] flow = { "控制", "开机" };
                    steps = flow.ToList();
                }
                else
                {
                    string[] flow = { "开机" };
                    steps = flow.ToList();
                }
                callback = () =>
                {
                    ButenEvent("空载运行");
                };
                StartFlow();
                break;
            case AvailableStatus.发送断路器合令:
                if (!kzjm.activeSelf)
                {
                    string[] flow = { "控制", "断路器合" };
                    steps = flow.ToList();
                }
                else
                {
                    string[] flow = { "断路器合" };
                    steps = flow.ToList();
                }
                callback = () =>
                {
                    ButenEvent("断路器断合");
                };
                StartFlow();
                break;
            case AvailableStatus.发送断路器分令:
                //增加流程检测
                if (!kzjm.activeSelf)
                {
                    string[] flow = { "控制", "断路器分" };
                    steps = flow.ToList();
                }
                else
                {
                    string[] flow = { "断路器分" };
                    steps = flow.ToList();
                }
                callback = () =>
                {
                    ButenEvent("断路器断开");
                };
                StartFlow();
                break;
            case AvailableStatus.发送停机令:
                //增加流程检测
                if (!kzjm.activeSelf)
                {
                    string[] flow = { "控制", "停机" };
                    steps = flow.ToList();
                }
                else
                {
                    string[] flow = { "停机" };
                    steps = flow.ToList();
                }
                callback = () =>
                {
                    ButenEvent("停机");
                };
                StartFlow();
                break;
        }
    }

    void SetScreen(int open)
    {
        if (open == 0)
        {
            cs[0].text = "0";
            cs[1].text = "0";
            cs[2].text = "0";
            cs[3].text = "0";
            cs[4].text = "0";
            cs[5].text = "0";
            cs[6].text = "0";//转速 
            cs[7].text = "0";
            cs[8].text = "0";
            cs[9].text = "0";
            cs[10].text = "0";
            cs[11].text = "0";
            cs[12].text = "0.962";
            cs[13].text = "0";

            cs[14].text = "停机态";//状态文字
            cs[14].color = gre;
            dlq.color = gre;
            zttb.color = gre;
        }
        else if(open == 1)
        {
            cs[0].text = "0";
            cs[1].text = "0";
            cs[2].text = "0";
            cs[3].text = "0";
            cs[4].text = "0";
            cs[5].text = "0";
            cs[6].text = "99";//转速
            cs[7].text = "0";
            cs[8].text = "0";
            cs[9].text = "0";
            cs[10].text = "11.8";
            cs[11].text = "3.1";
            cs[12].text = "0.962";
            cs[13].text = "50.1";

            cs[14].text = "空载态";//状态文字
            cs[14].color = yel;
            dlq.color = gre;
            zttb.color = yel;
        }
        else if (open == 2)
        {
            cs[0].text = "5.8";
            cs[1].text = "5.9";
            cs[2].text = "5.7";
            cs[3].text = "10.1";
            cs[4].text = "10.1";
            cs[5].text = "10.1";
            cs[6].text = "99";//转速 
            cs[7].text = "689";
            cs[8].text = "700";
            cs[9].text = "692";
            cs[10].text = "11.8";
            cs[11].text = "3.1";
            cs[12].text = "0.962";
            cs[13].text = "50.0";

            cs[14].text = "发电态";//状态文字
            cs[14].color = red;
            dlq.color = red;
            zttb.color = red;
        }
    }


    void IBaseBehaviour.SetFinalState()
    {
        SetImageRaycast(true);
    }

    public static string GetTime()
    {
        return DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    Color red = new Color(0.9f, 0f, 0.14f, 1);
    Color yel = new Color(0.59f, 0.22f, 0.11f, 1);
    Color gre = new Color(0f, 0.85f, 0f, 1);

    public GameObject bjjm;
    public GameObject kzjm;


    /// <summary>
    /// 执行按钮事件的实际逻辑
    /// </summary>
    public void ButenEvent(string eventname)
    {
        // 同步追赶时绕过校验，直接执行表现
        if (!isSyncing)
        {
            //没有触发对应流程时直接返回不执行（仅在流程进行中校验步骤，完成后允许自由触发）
            if (steps.Count > 0 && currentStepIndex < steps.Count)
            {
                //存在流程时执行错误 且不是考核给提示
                if (!TryToNext(eventname))
                {
                    if (!GlobalInfo.isExam)
                        smallSceneModule?.OnErrorShow();
                    return;
                }
                // 操作者：发送当前步骤进度同步给其他用户
                ToolManager.SendBroadcastMsg(new MsgSyncCustomUI((ushort)SmallFlowModuleEvent.SynchronizationLcu, (int)status, currentStepIndex), true);
            }
        }
        switch (eventname)
        {
            case "停机":
                SetScreen(0);
                break;
            case "负载运行":
                SetScreen(2);
                break;
            case "断路器断开":
                SetScreen(1);
                break;
            case "断路器断合":
                SetScreen(2);
                break;
            case "空载运行":
                SetScreen(1);
                break;
            case "转速降低":
                cs[6].text = "18";//转速手动停机时，先降到18，刹车后才降到0
                break;
        }
        if (eventname == "控制")
        {
            bjjm.SetActive(false);
            kzjm.SetActive(true);
        }
        else if (eventname == "报警")
        {
            bjjm.SetActive(true);
            kzjm.SetActive(false);
        }
        else
        {
            bjjm.SetActive(false);
            kzjm.SetActive(false);
        }
    }

    // 步骤列表
    List<string> steps = new List<string>();
    // 当前步骤
    int currentStepIndex = 0;
    bool isSyncing = false;
    public List<Button> UIButtons;

    public void StartFlow()
    {
        if (smallSceneModule && smallSceneModule.ModelState != ModelState.OtherOperating)
            SetImageRaycast(false);
        currentStepIndex = 0;
        SetTip();
    }

    void SetImageRaycast(bool show)
    {
        if (smallSceneModule && smallSceneModule.RoteInput)
            smallSceneModule.RoteInput.enabled = show;
    }

    void SetTip()
    {
        if (GlobalInfo.isExam) return;
        foreach (var item in UIButtons)
        {
            if (item.transform.Find("tip") != null)
            {
                if (steps.Count > currentStepIndex && item.gameObject.name == steps[currentStepIndex])
                {
                    item.transform.Find("tip").gameObject.SetActive(true);
                }
                else
                {
                    item.transform.Find("tip").gameObject.SetActive(false);
                }
            }
        }
    }


    // 检测当前步骤
    public bool TryToNext(string stepname)
    {
        if (steps.Count > currentStepIndex && stepname == steps[currentStepIndex])
        {
            currentStepIndex++;
            SetTip();
            if (currentStepIndex >= steps.Count)
            {
                callback?.Invoke();
                DOVirtual.DelayedCall(2, () => {
                    Othercallback?.Invoke();
                    Othercallback = null;
                });
            }
            return true;
        }
        return false;
    }
}