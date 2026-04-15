using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.MPE;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;

/// <summary>
/// 模拟操作 步骤列表模块
/// </summary>
public class UISmallSceneFlowModule : UIModuleBase
{
    private RectTransform Background;

    private SmallFlowCtrl smallFlowCtrl;
    private UISmallSceneModule smallSceneModule;

    [HideInInspector]
    /// <summary>
    /// 主树状视图
    /// </summary>
    public TreeView mTreeView;

    private const string itemPrefab = "ItemPrefab";

    public Dictionary<string, int> viewItemIds = new Dictionary<string, int>();
    /// <summary>
    /// 当前选择步骤节点Id
    /// </summary>
    private int selectedItem;
    /// <summary>
    /// 是否能触发点击事件
    /// </summary>
    private bool isOnClick = true;

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        AddMsg(new ushort[]{
            (ushort)OperationListEvent.Show,
            (ushort)HierarchyEvent.Expand,
            (ushort)HierarchyEvent.Collapse,
            (ushort)HierarchyEvent.Click,
            (ushort)HierarchyEvent.UpdateAttachment,
            (ushort)SmallFlowModuleEvent.StartExecute,
            (ushort)SmallFlowModuleEvent.CompleteExecute,
            (ushort)SmallFlowModuleEvent.SelectFlow,
            (ushort)SmallFlowModuleEvent.SelectStep,
            (ushort)SmallFlowModuleEvent.Guide,
            (ushort)SmallFlowModuleEvent.CompleteStep,
            (ushort)SmallFlowModuleEvent.ShowUIOperation
        });

        Background = this.GetComponentByChildName<RectTransform>("Background");
        this.GetComponentByChildName<Button>("Close").onClick.AddListener(() => SendMsg(new MsgBase((ushort)OperationListEvent.Hide)));

        smallSceneModule = transform.parent.GetComponentInChildren<UISmallSceneModule>();
        smallFlowCtrl = ModelManager.Instance.modelGo.GetComponent<SmallFlowCtrl>();
        InitTreeView();
    }

    /// <summary>
    /// 初始化树状结构
    /// </summary>
    public void InitTreeView()
    {
        mTreeView = transform.GetComponentByChildName<TreeView>("Content");
        mTreeView.OnTreeListAddOneItem = OnTreeListAddOneItem;
        mTreeView.OnItemExpandBegin = OnItemExpandBegin;
        mTreeView.OnItemCollapseBegin = OnItemCollapseBegin;
        mTreeView.OnItemCustomEvent = OnItemCustomEvent;
        mTreeView.InitView();

        InitFlowTreeList(smallFlowCtrl.flows, mTreeView);
        mTreeView.CollapseAllItem();

        if (GlobalInfo.EnableFlow)
        {
            mTreeView.NeedRepositionAll = true;
            if (smallFlowCtrl.flows.Length > 0 && mTreeView != null)
            {
                //默认选择第一步
                TreeViewItem treeViewItem = mTreeView.GetTreeItemById(viewItemIds[smallFlowCtrl.flows[0].ID]);
                MsgStringInt msgStringInt = new MsgStringInt((ushort)SmallFlowModuleEvent.SelectFlow, smallFlowCtrl.flows[0].ID, treeViewItem.transform.GetSiblingIndex());
                FormMsgManager.Instance.SendMsg(new MsgBrodcastOperate()
                {
                    senderId = GlobalInfo.account.id,
                    msgId = msgStringInt.msgId,
                    data = JsonTool.Serializable(msgStringInt)
                });
            }
            //this.WaitTime(0.1f, () =>
            //{
                //避免中途界面销毁
                //try
                {
               
                }
                //catch { }
            //});
        }    
    }

    /// <summary>
    /// 初始化任务树节点
    /// </summary>
    /// <param name="flows"></param>
    /// <param name="parentTree"></param>
    private void InitFlowTreeList(SmallFlow1[] flows, TreeList parentTree)
    {
        TreeViewItem item = null;
        TreeViewItemData data;

        int index = 0;
        foreach (var smallFlow in flows)
        {
            item = parentTree.AppendItem(itemPrefab);
            data = item.AutoComponent<TreeViewItemData>();

            data.Init(item, smallFlow.ID, $"{++index}.{smallFlow.flowName}", null, ChangeFlowName);
            item.BindData(data);

            if (!viewItemIds.ContainsKey(smallFlow.ID))
                viewItemIds.Add(smallFlow.ID, item.ItemId);
            else
                Debug.LogWarning($"存在重复UUID {smallFlow.ID}");

            InitStepTreeList(smallFlow.steps, item?.ChildTree, smallFlow.ID);
        }
    }

    /// <summary>
    /// 初始化任务子树步骤列表
    /// </summary>
    /// <param name="steps"></param>
    /// <param name="parentTree"></param>
    private void InitStepTreeList(List<SmallStep1> steps, TreeList parentTree, string flowID)
    {
        TreeViewItem item = null;
        TreeViewItemData data;

        foreach (var step in steps)
        {
            item = parentTree.AppendItem(itemPrefab);
            data = item.AutoComponent<TreeViewItemData>();

            data.Init(item, step.ID, step.hint, null, (id, nodeName, callback) => ChangeStepName(flowID, id, nodeName, callback));
            data.SetAttachment(!string.IsNullOrEmpty(step.ID) && GlobalInfo.currentWikiKnowledges.ContainsKey(step.ID));
            item.BindData(data);

            if (!viewItemIds.ContainsKey(step.ID))
                viewItemIds.Add(step.ID, item.ItemId);
            else
                Debug.LogWarning($"存在重复UUID {step.ID}");
        }
    }

    #region 树节点事件
    void ChangeFlowName(string id, string nodeName, UnityAction<bool> callback)
    {
        //编辑文本替换缓存
        EncyclopediaOperation encyclopediaModel = GlobalInfo.currentWiki as EncyclopediaOperation;
        Flow flow = encyclopediaModel.flows.Find(value => value.id == id);
        string temp = flow.title;
        flow.title = nodeName;
        //提交所有任务和步骤数据转化字符串
        RequestManager.Instance.ChangeStepNodeName(GlobalInfo.currentWiki.id, JsonTool.Serializable(encyclopediaModel.flows), () =>
        {
            callback?.Invoke(true);
        }, (code, msg) =>
        {
            flow.title = temp;
            UIManager.Instance.OpenModuleUI<ToastPanel>(null, UILevel.PopUp, new ToastPanelInfo("修改任务名称失败"));
            callback?.Invoke(false);
        });
    }

    void ChangeStepName(string flowID, string id, string nodeName, UnityAction<bool> callback)
    {
        EncyclopediaOperation encyclopediaModel = GlobalInfo.currentWiki as EncyclopediaOperation;
        Flow flow = encyclopediaModel.flows.Find(value => value.id == flowID);
        Step step = flow.children.Find(value => value.id == id);
        string temp = step.title;
        step.title = nodeName;
        //提交所有任务和步骤数据转化字符串
        RequestManager.Instance.ChangeStepNodeName(GlobalInfo.currentWiki.id, JsonTool.Serializable(encyclopediaModel.flows), () =>
        {
            callback?.Invoke(true);
        }, (code, msg) =>
        {
            step.title = temp;
            UIManager.Instance.OpenModuleUI<ToastPanel>(null, UILevel.PopUp, new ToastPanelInfo("修改步骤名称失败"));
            callback?.Invoke(false);
        });
    }

    void OnTreeListAddOneItem(TreeList treeList)
    {
        int count = treeList.ItemCount;
        TreeViewItem parentTreeItem = treeList.ParentTreeItem;
        if (count > 0 && parentTreeItem != null)
        {
            parentTreeItem.ItemData.SetExpandBtnVisible(true);
            parentTreeItem.ItemData.SetExpandStatus(parentTreeItem.IsExpand);
        }
    }

    void OnItemExpandBegin(TreeViewItem item)
    {
        item.ItemData.SetExpandStatus(true);
    }

    void OnItemCollapseBegin(TreeViewItem item)
    {
        item.ItemData.SetExpandStatus(false);
    }

    void OnItemCustomEvent(TreeViewItem item, CustomEvent customEvent, int userId, string uuid)
    {
        if (customEvent == CustomEvent.ItemClicked)
        {
            if (selectedItem > 0)
            {
                TreeViewItem newSelectedItem = mTreeView.GetTreeItemById(selectedItem);
                if (newSelectedItem != null)
                {
                    newSelectedItem.ItemData.IsSelected = false;
                }
            }

            if (item != null)
            {
                //item.ItemData.UserId = userId;
                item.ItemData.IsSelected = true;

                selectedItem = item.ItemId;
            }
        }
    }
    #endregion

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);

        switch (msg.msgId)
        {
            case (ushort)OperationListEvent.Show:
                OpenModule();
                break;
            case (ushort)HierarchyEvent.Expand:
                MsgString msgStringExpand = (MsgString)msg;
                if (viewItemIds.ContainsKey(msgStringExpand.arg))
                {
                    TreeViewItem itemExpand = mTreeView.GetTreeItemById(viewItemIds[msgStringExpand.arg]);
                    if (itemExpand)
                    {
                        itemExpand.Expand();
                    }
                }
                break;
            case (ushort)HierarchyEvent.Collapse:
                MsgString msgStringCollapse = (MsgString)msg;
                if (viewItemIds.ContainsKey(msgStringCollapse.arg))
                {
                    TreeViewItem itemCollapse = mTreeView.GetTreeItemById(viewItemIds[msgStringCollapse.arg]);
                    if (itemCollapse)
                    {
                        itemCollapse.Collapse();
                    }
                }
                break;
            case (ushort)HierarchyEvent.UpdateAttachment:
                MsgStringInt msgStringInt = ((MsgStringInt)msg);
                if (viewItemIds.ContainsKey(msgStringInt.arg1))
                {
                    TreeViewItem item2 = mTreeView.GetTreeItemById(viewItemIds[msgStringInt.arg1]);
                    if (item2)
                    {
                        item2.ItemData.SetAttachment(msgStringInt.arg2 > 0);
                    }
                }
                break;
            case (ushort)SmallFlowModuleEvent.StartExecute:
                isOnClick = false;
                break;
            case (ushort)SmallFlowModuleEvent.ShowUIOperation:
                //todo 显示UI操作不禁用流程切换
                //isOnClick = false;
                break;
            case (ushort)SmallFlowModuleEvent.CompleteExecute:
                isOnClick = true;
                break;
            case (ushort)HierarchyEvent.Click:
                if (smallSceneModule.FatalFinish)
                {
                    smallSceneModule.ShowFatalPopup();
                    return;
                }
                if (!isOnClick || smallSceneModule.OtherOperating)
                {
                    UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("操作执行中，完成后再试")); 
                    return; 
                }
                MsgHierarchy msgHierarchy = (MsgHierarchy)msg;
                TreeViewItem item = msgHierarchy.item;
                if (item == null)
                    return;
                if (item.ParentTreeItem == null)
                {
                    ToolManager.SendBroadcastMsg(new MsgStringInt((ushort)SmallFlowModuleEvent.SelectFlow, msgHierarchy.uuid, item.transform.GetSiblingIndex()));
                }
                else
                {
                    MsgStringTuple<int, int, string> temp = new MsgStringTuple<int, int, string>()
                    {
                        msgId = (ushort)SmallFlowModuleEvent.SelectStep,
                        arg1 = msgHierarchy.uuid,
                        arg2 = new Tuple<int, int, string>(item.ParentTreeItem.transform.GetSiblingIndex(), item.transform.GetSiblingIndex(), string.Empty)
                    };

                    if (!GlobalInfo.SetFanelstate)
                    {
                        GlobalInfo.SetFanelstate = true;
                        MsgBrodcastOperate msgBrodcastOperate = new MsgBrodcastOperate(temp.msgId, JsonTool.Serializable(temp));
                        SendMsg(msgBrodcastOperate);
                        return;
                    }

                    if (GlobalInfo.EnableFlow)
                    {
                        ToolManager.SendBroadcastMsg(temp);
                    }
                    else
                    {
                        SendMsg(new MsgTuple<int, int, string>()
                        {
                            msgId = (ushort)SmallFlowModuleEvent.Guide,
                            arg = new Tuple<int, int, string>(item.ParentTreeItem.transform.GetSiblingIndex(), item.transform.GetSiblingIndex(), msgHierarchy.uuid)
                        });
                    }
                }
                break;
            case (ushort)SmallFlowModuleEvent.SelectFlow:
                MsgStringInt msgFlowIDIndex = ((MsgBrodcastOperate)msg).GetData<MsgStringInt>();
                {
                    int flowIndex = msgFlowIDIndex.arg2;
                    if (!viewItemIds.ContainsKey(msgFlowIDIndex.arg1))
                        return;
                    TreeViewItem flowItem = mTreeView.GetTreeItemById(viewItemIds[msgFlowIDIndex.arg1]);
                    if (flowItem == null)
                        return;
                    string stepUID = smallFlowCtrl.flows[flowIndex].steps[0].ID;
                    if (!viewItemIds.ContainsKey(stepUID))
                        return;
                    TreeViewItem stepItem = mTreeView.GetTreeItemById(viewItemIds[stepUID]);
                    if (stepItem == null)
                        return;
                    mTreeView.ExpandParent(stepItem);
                    OnItemCustomEvent(stepItem, CustomEvent.ItemClicked, ((MsgBrodcastOperate)msg).senderId, stepUID);
                }
                break;
            case (ushort)SmallFlowModuleEvent.SelectStep:
                MsgStringTuple<int, int, string> msgStringTuple = ((MsgBrodcastOperate)msg).GetData<MsgStringTuple<int, int, string>>();
                {
                    if (!viewItemIds.ContainsKey(msgStringTuple.arg1))
                        return;
                    TreeViewItem stepItem = mTreeView.GetTreeItemById(viewItemIds[msgStringTuple.arg1]);
                    if (stepItem == null)
                        return;
                    mTreeView.ExpandParent(stepItem);
                    OnItemCustomEvent(stepItem, CustomEvent.ItemClicked, ((MsgBrodcastOperate)msg).senderId, msgStringTuple.arg1);
                }
                break;
            case (ushort)SmallFlowModuleEvent.Guide:
                MsgTuple<int, int, string> msgTuple = msg as MsgTuple<int, int, string>;
                {
                    if (msgTuple == null || !viewItemIds.ContainsKey(msgTuple.arg.Item3))
                        return;
                    TreeViewItem stepItem = mTreeView.GetTreeItemById(viewItemIds[msgTuple.arg.Item3]);
                    if (stepItem == null)
                        return;
                    mTreeView.ExpandParent(stepItem);
                    OnItemCustomEvent(stepItem, CustomEvent.ItemClicked, GlobalInfo.account.id, msgTuple.arg.Item3);
                }
                break;
            case (ushort)SmallFlowModuleEvent.CompleteStep:
                if (!(msg is MsgIntInt))//跳步骤时消息不处理
                    return;

                // arg1 是新步骤索引（SmallFlowCtrl.Next() 已自增后发送）
                int newStepIndex = ((MsgIntInt)msg).arg1;
                if (smallFlowCtrl.index_NowFlow > smallFlowCtrl.flows.Length - 1 || newStepIndex > smallFlowCtrl.nowFlowSteps.Count - 1)
                    return;

                // 直接高亮新步骤（不需要 +1，因为 arg1 已经是新步骤索引）
                TreeViewItem stepitem = mTreeView.GetTreeItemById(viewItemIds[smallFlowCtrl.nowFlowSteps[newStepIndex].ID]);
                if (stepitem == null)
                    return;
                mTreeView.ExpandParent(stepitem);
                mTreeView.MoveToItem(stepitem);
                OnItemCustomEvent(stepitem, CustomEvent.ItemClicked, GlobalInfo.account.id, smallFlowCtrl.nowFlowSteps[newStepIndex].ID);
                break;
        }
    }


    /// <summary>
    /// 安全地选中步骤节点（带边界检查）
    /// 用于协同状态同步
    /// </summary>
    /// <param name="step">任务索引</param>
    /// <param name="flow">步骤索引</param>
    /// <returns>是否成功选中</returns>
    public void TrySelectNode(int flow, int step)
    {
        var steps = smallFlowCtrl.flows[flow].steps;
        string stepUID = steps[step].ID;

        TreeViewItem stepItem = mTreeView.GetTreeItemById(viewItemIds[stepUID]);
        if (stepItem == null)
        {
            Log.Warning($"stepItem for stepUID {stepUID} 为 null，无法选择步骤节点");
        }

        mTreeView.ExpandParent(stepItem);
        Log.Debug("正在执行重置最终步骤" + flow + "  " + step);
        OnItemCustomEvent(stepItem, CustomEvent.ItemClicked, GlobalInfo.account.id, stepUID);
        ProcessEvent(new MsgHierarchy((ushort)HierarchyEvent.Click, GlobalInfo.account.id, GlobalInfo.roomInfo.Uuid, stepItem));
    }

    #region 动效
    protected override float joinAnimePlayTime => 0.3f;
    protected override float exitAnimePlayTime => 0.2f;

    private void OpenModule()
    {
#if UNITY_ANDROID || UNITY_IOS
        JoinSequence.Join(Background.DOAnchorPos3DX(0, JoinAnimePlayTime));
#else
        if (!GlobalInfo.IsExamMode())
            JoinSequence.Join(Background.DOAnchorPos3DX(44f, JoinAnimePlayTime));
#endif
    }

    public override void ExitAnim(UnityAction callback)
    {
#if UNITY_ANDROID || UNITY_IOS
        ExitSequence.Join(Background.DOAnchorPos3DX(Background.sizeDelta.x, ExitAnimePlayTime));
#else
        ExitSequence.Join(Background.DOAnchorPos3DX(-Background.sizeDelta.x, ExitAnimePlayTime));
#endif
        base.ExitAnim(callback);
    }
    #endregion
}