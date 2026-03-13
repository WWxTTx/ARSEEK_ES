using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Ua, Ub, Uc / Uab, Ubc, Uca
/// Ia, Ib, Ic
/// P Q cosφF 转速 这些关键值在不同情况下的值
/// </summary>
public class LCU_mlfsjs : MonoBehaviour, IBaseBehaviour
{
    bool IBaseBehaviour.UseCallback(int step) => true;
    public Type GetStatusEnumType() => typeof(AvailableStatus);
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
    UnityAction endEvent;
    UnityAction callback;
    UISmallSceneModule smallSceneModule;

    Tween SetTime;
    AvailableStatus status;
    void IBaseBehaviour.Execute(int step, UnityAction callback)
    {
        if (smallSceneModule == null)
        {
            smallSceneModule = Transform.FindObjectOfType<UISmallSceneModule>().GetComponent<UISmallSceneModule>();
        }
        this.callback = callback;

        status = (AvailableStatus)step;
        DealEvent();
    }

    public Text TsqztText;
    public TSQ_TsqXsp TSQ_TsqXsp;
    public void DealEvent()
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
                StartFlow();
                break;
        }
    }

    public GameObject DlqZsd;
    public Transform DlqZsdJk;
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
            cs[10].text = "11.8";
            cs[11].text = "3.1";
            cs[12].text = "0.962";
            cs[13].text = "50.1";

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
            cs[6].text = "100.00";//转速 
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

    void Start()
    {
        if (SetTime != null)
        {
            SetTime.Kill();
            SetTime = null;
        }
        SetTime = DOVirtual.DelayedCall(1, () =>
        {
            cs[15].text = GetTime();
        }).SetLoops(-1);
    }

    void IBaseBehaviour.SetFinalState()
    {
        SetTime.Kill();
        SetTime = null;
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

    public void ButenEvent(string eventname)
    {
        TryToNext(eventname);
        switch (eventname)
        {
            case "停机":
                SetScreen(0);
                break;
            case "负载运行":
                SetScreen(2);
                smallSceneModule.ModelState = ModelState.Operated;
                break;
            case "断路器断开":
                SetScreen(1);
                smallSceneModule.ModelState = ModelState.Operated;
                break;
            case "断路器断合":
                SetScreen(2);
                smallSceneModule.ModelState = ModelState.Operated;
                break;
            case "空载运行":
                SetScreen(1);
                smallSceneModule.ModelState = ModelState.Operated;
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
    public List<Button> UIButtons;

    public void StartFlow()
    {
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
    public void TryToNext(string stepname)
    {
        if (steps.Count > currentStepIndex && stepname == steps[currentStepIndex])
        {
            currentStepIndex++;
            SetTip();
            if (currentStepIndex >= steps.Count)
            {
                callback?.Invoke();
                endEvent?.Invoke();
                DOVirtual.DelayedCall(2, () =>
                {
                    endEvent?.Invoke();
                    SetImageRaycast(true);
                });
            }
        }
    }
}