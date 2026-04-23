using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityFramework.Runtime;

/// <summary>
/// 菜单选择UI
/// </summary>
public class UIOption : MonoBase
{
    [HideInInspector]
    public string id;

    public GameObject main;

    private CanvasGroup canvasGroup;

    private bool interactable;
    public bool Interactable
    {
        get { return interactable; }
        set
        {
            interactable = value;
            canvasGroup.interactable = value;
        }
    }

    protected string ActiveProp;

    protected override void InitComponents()
    {
        base.InitComponents();
        AddMsg(
            (ushort)SmallFlowModuleEvent.SelectFlow,
            (ushort)SmallFlowModuleEvent.SelectStep,
            (ushort)SmallFlowModuleEvent.CompleteStep,
            (ushort)SmallFlowModuleEvent.OperatingRecordClear,
            (ushort)SmallFlowModuleEvent.FocusChanged,
            (ushort)SmallFlowModuleEvent.ChangeProp,
            (ushort)SmallFlowModuleEvent.StartExecute,
            (ushort)SmallFlowModuleEvent.CompleteExecute
        );
        canvasGroup = transform.AutoComponent<CanvasGroup>();
    }

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="id">操作设备id</param>
    /// <param name="opName">正确操作名称，自由操作传null</param>
    /// <param name="onFinish">操作结束回调</param>

    public void Init(string id, List<string> options, string opName, Action<string> onFinish, Action onFail)
    {
        this.id = id;

        DOTween.Kill(id.ToString(), true);

        main.transform.RefreshItemsView(options, (item, option) =>
        {
            item.GetComponentInChildren<Text>().text = option;
            Button button = item.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                //自由模式
                if (string.IsNullOrEmpty(opName))
                {
                    //OnClose();
                    onFinish?.Invoke(option);
                }
                else
                {
                    //OnClose();
                    onFinish?.Invoke(option);

                    if (option.Equals(opName))
                    {
                        //OnClose();
                        //onFinish?.Invoke(option);
                    }
                    else
                    {
                        //错误操作
                        onFail?.Invoke();
                    }
                }
            });

            var image = item.GetComponentByChildName<Image>("Highlight");
            if (!string.IsNullOrEmpty(opName) && option.Equals(opName))
            {
                image.SetAlpha(1f);
                image.gameObject.SetActive(true);
                image.DOFade(0, 0.8f).SetLoops(-1, LoopType.Yoyo).SetId(id.ToString());
            }
            else
            {
                image.SetAlpha(0f);
                image.gameObject.SetActive(false);
            }
        });

        main.SetActive(true);
        SendMsg(new MsgBool((ushort)SmallFlowModuleEvent.ShowUIOperation, true));
    }

    public void SetActiveProp(string prop)
    {
        ActiveProp = prop;
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)SmallFlowModuleEvent.SelectFlow:
            case (ushort)SmallFlowModuleEvent.SelectStep:
            case (ushort)SmallFlowModuleEvent.CompleteStep:
            case (ushort)SmallFlowModuleEvent.OperatingRecordClear:
                OnClose();
                break;
            case (ushort)SmallFlowModuleEvent.FocusChanged:
                if (GlobalInfo.ShouldProcess((msg as MsgBrodcastOperate).senderId))
                {
                    OnClose();
                }
                break;
            case (ushort)SmallFlowModuleEvent.ChangeProp:
                ActiveProp = (msg as MsgString).arg;
                break;
            case (ushort)SmallFlowModuleEvent.StartExecute:
                var msgStringBool = msg as MsgStringBool;
                //协同/考核非本人操作
                if (msgStringBool.arg2)
                    return;
                if (!string.IsNullOrEmpty(this.id) && this.id.Equals(msgStringBool.arg1))
                    Interactable = false;
                break;
            case (ushort)SmallFlowModuleEvent.CompleteExecute:
                Interactable = true;
                break;
        }
    }

    /// <summary>
    /// 隐藏操作UI
    /// </summary>
    private void OnClose()
    {
        DOTween.Kill(id.ToString(), true);
        main.SetActive(false);
        SendMsg(new MsgBool((ushort)SmallFlowModuleEvent.ShowUIOperation, false));
    }
}