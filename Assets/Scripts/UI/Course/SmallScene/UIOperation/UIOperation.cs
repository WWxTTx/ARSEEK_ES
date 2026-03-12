using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityFramework.Runtime;

public class UIOperation : MonoBase
{
    [HideInInspector]
    public string id;
    [HideInInspector]
    public string currentState;
    [HideInInspector]
    public Transform model;
    [HideInInspector]
    public Action<string> onFinish;
    [HideInInspector]
    public Action onFail;

    public GameObject main;

    protected bool mIsSelect;
    public virtual bool isSelect
    {
        get { return mIsSelect; }
        set { mIsSelect = value; }
    }

    private bool interactable;
    public bool Interactable
    {
        get { return interactable; }
        set { interactable = value; }
    }
    /// <summary>
    /// 目标点
    /// </summary>
    public Transform targetPoint;
    /// <summary>
    /// 跟随鼠标点
    /// </summary>
    public Transform followPoint;
    protected Image follow;

    /// <summary>
    /// 当前目标点标志位
    /// </summary>
    [HideInInspector]
    public int index;

    /// <summary>
    /// 碰撞列表
    /// </summary>
    protected List<GameObject> triggerList = new List<GameObject>();

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

        var children = GetComponentsInChildren<Transform>(true);
        foreach(var child in children)
        {
            child.gameObject.layer = LayerMask.NameToLayer(GlobalInfo.OpUILayer);
        }
    }

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="id">操作设备id</param>
    /// <param name="currentState">操作物体当前状态</param>
    /// <param name="opName">正确操作名称，自由操作传null</param>
    /// <param name="model">联动模型</param>
    /// <param name="onFinish">操作结束回调</param>
    /// <param name="onFail">操作错误回调</param>
    public virtual void Init(string id, string currentState, string opName, Transform model, Action<string> onFinish, Action onFail = null)
    {
        this.id = id;
        this.model = model;
        this.onFinish = onFinish;
        this.onFail = onFail;
        index = 0;
        triggerList.Clear();
        this.currentState = currentState;/*string.Empty;*/
        Interactable = true;
        follow = followPoint.GetChild(0).GetComponent<Image>();
        SendMsg(new MsgBool((ushort)SmallFlowModuleEvent.ShowUIOperation, true));
    }

    public void SetActiveProp(string prop)
    {
        ActiveProp = prop;
    }

    public virtual void OnTrigger(GameObject collider)
    {

    }

    public void Update()
    {
        if (!Interactable)
            return;

        OnUpdate();
    }

    protected virtual void OnUpdate()
    {

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
    protected virtual void OnClose()
    {
        main.SetActive(false);
        SendMsg(new MsgBool((ushort)SmallFlowModuleEvent.ShowUIOperation, false));
    }

    public virtual void SetFinalState(string opName)
    {

    }
}