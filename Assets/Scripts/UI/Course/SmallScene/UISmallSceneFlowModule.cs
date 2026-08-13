using DG.Tweening;
using System;
using System.Collections.Generic;
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
    private bool isExpanded;

    public bool IsExpanded => isExpanded;

    private ModuleAutoSleep autoSleep;
    private RectTransform closeArrow;
    private float closeArrowDefaultZ;
    private bool arrowFlipped;

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
            (ushort)SmallFlowModuleEvent.SelectStep,
            (ushort)SmallFlowModuleEvent.Guide,
            (ushort)SmallFlowModuleEvent.CompleteStep,
            (ushort)SmallFlowModuleEvent.ShowUIOperation
        });

        Background = this.GetComponentByChildName<RectTransform>("Background");

        autoSleep = Background.gameObject.AddComponent<ModuleAutoSleep>();
        autoSleep.idleSeconds = 5f;
        autoSleep.watchArea = Background;
        autoSleep.onIdle.AddListener(() => SendMsg(new MsgBase((ushort)OperationListEvent.Hide)));

        closeArrow = this.GetComponentByChildName<RectTransform>("Close");
        if (closeArrow != null)
        {
            closeArrowDefaultZ = closeArrow.localEulerAngles.z;
        }
        this.GetComponentByChildName<Button>("Close").onClick.AddListener(() =>
        {
            if (IsExpanded) SendMsg(new MsgBase((ushort)OperationListEvent.Hide));
            else ExpandModule();
        });

        smallSceneModule = transform.parent.GetComponentInChildren<UISmallSceneModule>();
        smallFlowCtrl = ModelManager.Instance.modelGo.GetComponent<SmallFlowCtrl>();
        InitTreeView();

#if UNITY_STANDALONE
        var content = this.FindChildByName("Background")?.Find("View");
#else
        var content = this.FindChildByName("View");
#endif
        if (content != null)
        {
            var text = content.GetComponentInChildren<Text>();
            var canvasGroup = content.GetComponent<CanvasGroup>();
            SpeechManager.Instance.RegisterTipDisplay(canvasGroup, text);
        }
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

        if (smallFlowCtrl.flows.Length > 0 && mTreeView != null)
        {
            //默认选择第一步
            string firstStepUID = smallFlowCtrl.flows[0].steps[0].ID;
            MsgStringTuple<int, int, string> msgStringTuple = new MsgStringTuple<int, int, string>()
            {
                msgId = (ushort)SmallFlowModuleEvent.SelectStep,
                arg1 = firstStepUID,
                arg2 = new Tuple<int, int, string>(0, 0, string.Empty)
            };
            FormMsgManager.Instance.SendMsg(new MsgBrodcastOperate()
            {
                senderId = GlobalInfo.account.id,
                msgId = msgStringTuple.msgId,
                data = JsonTool.Serializable(msgStringTuple)
            });
        }
        if (!GlobalInfo.isExam)
        {
            mTreeView.NeedRepositionAll = true;
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
                    int flowIndex = item.transform.GetSiblingIndex();
                    string stepUID = smallFlowCtrl.flows[flowIndex].steps[0].ID;
                    ToolManager.SendBroadcastMsg(new MsgStringTuple<int, int, string>()
                    {
                        msgId = (ushort)SmallFlowModuleEvent.SelectStep,
                        arg1 = stepUID,
                        arg2 = new Tuple<int, int, string>(flowIndex, 0, string.Empty)
                    });
                }
                else
                {
                    if (!GlobalInfo.isExam)
                    {
                        // 发送任务进度跳转广播消息
                        ToolManager.SendBroadcastMsg(new MsgStringTuple<int, int, string>()
                        {
                            msgId = (ushort)SmallFlowModuleEvent.SelectStep, // 消息ID：选择步骤事件
                            arg1 = msgHierarchy.uuid,                        // arg1: 步骤UUID，用于TreeView中定位步骤项
                            arg2 = new Tuple<int, int, string>(
                                item.ParentTreeItem.transform.GetSiblingIndex(), // Item1: flow索引（父级任务流程在兄弟节点中的位置）
                                item.transform.GetSiblingIndex(),                 // Item2: step索引（当前步骤在兄弟节点中的位置）
                                string.Empty                                      // Item3: 预留字段，当前未使用
                            )
                        });
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
            case (ushort)SmallFlowModuleEvent.SelectStep:
                // 接收任务进度跳转消息，展开并选中对应步骤
                MsgStringTuple<int, int, string> msgStringTuple = ((MsgBrodcastOperate)msg).GetData<MsgStringTuple<int, int, string>>();
                {
                    // arg1: 步骤UUID，用于在viewItemIds字典中查找TreeViewItem
                    if (!viewItemIds.ContainsKey(msgStringTuple.arg1))
                        return;
                    TreeViewItem stepItem = mTreeView.GetTreeItemById(viewItemIds[msgStringTuple.arg1]);
                    if (stepItem == null)
                        return;
                    mTreeView.ExpandParent(stepItem);
                    // arg1: 步骤UUID，传递给OnItemCustomEvent用于标识被点击的步骤
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
                MsgIntInt newStepIndex = ((MsgBrodcastOperate)msg).GetData<MsgIntInt>();
                var steps = smallFlowCtrl.flows[newStepIndex.arg1].steps;
                string stepUid = steps[newStepIndex.arg2].ID;

                TreeViewItem stepitem = mTreeView.GetTreeItemById(viewItemIds[stepUid]);
                if (stepitem == null)
                    return;
                mTreeView.ExpandParent(stepitem);
                mTreeView.MoveToItem(stepitem);
                OnItemCustomEvent(stepitem, CustomEvent.ItemClicked, ((MsgBrodcastOperate)msg).senderId, stepUid);
                break;
        }
    }

    #region 动效
    protected override float joinAnimePlayTime => 0.3f;
    protected override float exitAnimePlayTime => 0.2f;

    public void ExpandModule()
    {
        isExpanded = true;
#if UNITY_ANDROID || UNITY_IOS
        Background.DOAnchorPos3DX(0, joinAnimePlayTime);
#else
        if (!GlobalInfo.IsExamMode())
            Background.DOAnchorPos3DX(44f, joinAnimePlayTime);
#endif
#if UNITY_ANDROID || UNITY_IOS
        if (autoSleep != null) autoSleep.Activate();
#endif
        FlipArrow(true);
        transform.SetAsLastSibling();
        // 展开时收起其他两个模块
        SendMsg(new MsgBase((ushort)BaikeSelectModuleEvent.Hide));
        SendMsg(new MsgBase((ushort)HistoryEvent.Hide));
        ShowCloseHideOthers();
    }

    public void CollapseModule()
    {
        isExpanded = false;
#if UNITY_ANDROID || UNITY_IOS
        if (autoSleep != null) autoSleep.Deactivate();
#endif
        FlipArrow(false);
#if UNITY_ANDROID || UNITY_IOS
        Background.DOAnchorPos3DX(ModuleSlideUtility.GetCollapsedBackgroundX(Background, closeArrow, true, -10f), exitAnimePlayTime);
#else
        Background.DOAnchorPos3DX(ModuleSlideUtility.GetCollapsedBackgroundX(Background, closeArrow, false), exitAnimePlayTime);
#endif
    }

    private void OpenModule()
    {
        ExpandModule();
    }

    public override void ExitAnim(UnityAction callback)
    {
        CollapseModule();
        base.ExitAnim(callback);
    }

    public void HideCloseButton()
    {
        var closeBtn = transform.FindChildByName("Close");
        if (closeBtn != null) closeBtn.gameObject.SetActive(false);
    }

    private void ShowCloseHideOthers()
    {
        var closeBtn = transform.FindChildByName("Close");
        if (closeBtn != null) closeBtn.gameObject.SetActive(true);
        var parent = transform.parent;
        if (parent == null) return;
        var baike = parent.GetComponentInChildren<BaikeSelectModule>(true);
        if (baike != null) baike.HideCloseButton();
        var history = parent.GetComponentInChildren<UISmallSceneOperationHistory>(true);
        if (history != null) history.HideCloseButton();
    }

    private void FlipArrow(bool toFlipped)
    {
        if (closeArrow == null || arrowFlipped == toFlipped) return;
        arrowFlipped = toFlipped;
        float z = toFlipped ? closeArrowDefaultZ + 180f : closeArrowDefaultZ;
        closeArrow.DOLocalRotate(new Vector3(0, 0, z), 0.25f);
    }
    #endregion
}