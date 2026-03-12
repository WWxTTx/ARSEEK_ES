using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;
using UnityFramework.Runtime;

/// <summary>
/// 拆解百科模型层级模块
/// </summary>
public class ModelHierarchyModule : UIModuleBase
{
    private CanvasGroup canvasGroup;
    private RectTransform Background;
    private Button Collapse;
    private CanvasGroup TreeView;
    /// <summary>
    /// 主树状视图
    /// </summary>
    private TreeView mTreeView;
    /// <summary>
    /// 侧边图标树状视图
    /// </summary>
    private TreeView mSideTreeView;

    private Dictionary<string, int> viewItemIds = new Dictionary<string, int>();
    //private Dictionary<string, int> currentWikiAnims = new Dictionary<string, int>();
    //private string currentSelectUUID;

    private const string defaultNodeName = "未命名";

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);
        mTreeView = transform.GetComponentByChildName<TreeView>("Content");
        mSideTreeView = transform.GetComponentByChildName<TreeView>("IconContent");

        AddMsg(new ushort[]
        {
#if UNITY_ANDROID || UNITY_IOS
            (ushort)ARModuleEvent.Tracking,
#endif
            (ushort)HierarchyEvent.UpdateAttachment,
            (ushort)HierarchyEvent.Expand,
            (ushort)HierarchyEvent.Collapse,
            (ushort)HierarchyEvent.Click,
            (ushort)HierarchyEvent.Interactable
        });

        //canvasGroup = GetComponent<CanvasGroup>();
        Background = this.GetComponentByChildName<RectTransform>("BackGround");
        Collapse = this.GetComponentByChildName<Button>("Collapse");
        TreeView = this.GetComponentByChildName<CanvasGroup>("TreeView");
        Collapse.onClick.AddListener(() => ToolManager.SendBroadcastMsg(new MsgBase((ushort)HierarchyEvent.Hide)));

        InitTreeView();
    }

    public override void Show(UIData uiData = null)
    {
        base.Show(uiData);
        InitSelectionEvent();
    }

    /// <summary>
    /// 初始化树状结构
    /// </summary>
    public void InitTreeView()
    {
        mTreeView.OnTreeListAddOneItem = OnTreeListAddOneItem;
        mTreeView.OnItemExpandBegin = OnItemExpandBegin;
        mTreeView.OnItemCollapseBegin = OnItemCollapseBegin;
        mTreeView.OnItemCustomEvent = OnItemCustomEvent;
        mTreeView.InitView();
        mSideTreeView.InitView();

        InitTreeList(ModelManager.Instance.modelGo.transform, mTreeView, mSideTreeView);
        mTreeView.CollapseAllItem();
        mSideTreeView.CollapseAllItem();

        ModelInfo modelInfo = ModelManager.Instance.modelGo.GetComponent<ModelInfo>();
        if (modelInfo)
        {
            TreeViewItem itemExpand = mTreeView.GetTreeItemById(viewItemIds[modelInfo.ID]);
            if (itemExpand)
            {
                itemExpand.Expand();
                itemExpand.SideItemData.Item.Expand();
            }
        }

        this.WaitTime(0.1f, () =>
        {
            mTreeView.NeedRepositionAll = true;
            mSideTreeView.NeedRepositionAll = true;
        });
    }

    private const string iconPrefab = "IconPrefab";
    private const string itemPrefab = "ItemPrefab";


    private void InitTreeList(Transform model, TreeList parentTree, TreeList sideParentTree)
    {
        TreeViewItem item = null;
        TreeViewItemData data;
        TreeViewItem icon = null;
        SideTreeViewItemData iconData;
        //bool hasAnim = false;
        bool hasAttachment;

        ModelInfo modelInfo = model.GetComponent<ModelInfo>();
        if (modelInfo && modelInfo.PropType == PropType.Operate && parentTree != null)
        {
            icon = sideParentTree.AppendItem(iconPrefab);
            iconData = icon.AutoComponent<SideTreeViewItemData>();

            item = parentTree.AppendItem(itemPrefab);
            data = item.AutoComponent<TreeViewItemData>();

            GlobalInfo.currentWikiNames.TryGetValue(modelInfo.ID, out string modelName);

            //hasAnim = currentWikiAnims.ContainsKey(model.name);
            hasAttachment = !string.IsNullOrEmpty(modelInfo.ID) && GlobalInfo.currentWikiKnowledges.ContainsKey(modelInfo.ID);

            iconData.Init(icon, modelInfo.ID, /*hasAnim,*/ hasAttachment, item);
            icon.BindData(iconData);

            data.Init(item, modelInfo.ID, string.IsNullOrEmpty(modelName) ? defaultNodeName : modelName, icon, ChangeHierarchyName);
            item.BindData(iconData);
            item.BindData(data);

            if (!viewItemIds.ContainsKey(modelInfo.ID))
                viewItemIds.Add(modelInfo.ID, item.ItemId);
            else
                Log.Error($"存在重复UUID {modelInfo.ID}");
        }

        foreach (Transform child in model)
        {
            InitTreeList(child, item?.ChildTree, icon?.ChildTree);
        }
    }

    void ChangeHierarchyName(string UUID,string nodeName,UnityAction<bool> callback)
    {
        RequestManager.Instance.ChangeModelNodeName(GlobalInfo.currentWiki.id, UUID, nodeName, () =>
        {
            if (!GlobalInfo.currentWikiNames.ContainsKey(UUID))
                GlobalInfo.currentWikiNames.Add(UUID, nodeName);
            else
                GlobalInfo.currentWikiNames[UUID] = nodeName;

            callback?.Invoke(true);
        }, (code, msg) =>
        {
            UIManager.Instance.OpenModuleUI<ToastPanel>(null, UILevel.PopUp, new ToastPanelInfo("修改模型节点名称失败"));
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

    //int mCurSelectedItemId = 0;
    private Dictionary<int, int> userSelectedItems = new Dictionary<int, int>();

    /// <summary>
    /// 模型节点选中事件
    /// </summary>
    /// <param name="item"></param>
    /// <param name="customEvent"></param>
    /// <param name="userId"></param>
    /// <param name="uuid"></param>
    void OnItemCustomEvent(TreeViewItem item, CustomEvent customEvent, int userId, string uuid)
    {
        if (customEvent == CustomEvent.ItemClicked)
        {
            if (userSelectedItems.TryGetValue(userId, out int selectedItem))
            {
                if (selectedItem > 0)
                {
                    if (item != null && item.ItemId == selectedItem)
                        return;

                    TreeViewItem newSelectedItem = mTreeView.GetTreeItemById(selectedItem);
                    if (newSelectedItem != null)
                    {
                        newSelectedItem.ItemData.IsSelected = false;
                    }
                    userSelectedItems.Remove(userId);
                    //if (userId == GlobalInfo.account.id)
                    //    mCurSelectedItemId = 0;
                }
            }
            

            if (item != null)
            {
                item.ItemData.UserId = userId;
                item.ItemData.IsSelected = true;

                userSelectedItems.Add(userId, item.ItemId);
                //if (userId == GlobalInfo.account.id)
                //    mCurSelectedItemId = item.ItemId;
            }
        }
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);

        switch (msg.msgId)
        {
#if UNITY_ANDROID || UNITY_IOS
            case (ushort)ARModuleEvent.Tracking:
                bool tracking = ((MsgBool)msg).arg1;
                canvasGroup.blocksRaycasts = !tracking;
                canvasGroup.DOFade(tracking ? 0 : 1, 0.3f);
                break;
#endif
            case (ushort)HierarchyEvent.UpdateAttachment:
                MsgStringInt msgStringInt = ((MsgStringInt)msg);
                if (viewItemIds.ContainsKey(msgStringInt.arg1))
                {
                    TreeViewItem item2 = mTreeView.GetTreeItemById(viewItemIds[msgStringInt.arg1]);
                    if (item2)
                    {
                        item2.SideItemData.SetAttachment(msgStringInt.arg2 > 0);
                    }
                }
                break;
            case (ushort)HierarchyEvent.Expand:
                MsgString msgStringExpand = (MsgString)msg;
                if (viewItemIds.ContainsKey(msgStringExpand.arg))
                {
                    TreeViewItem itemExpand = mTreeView.GetTreeItemById(viewItemIds[msgStringExpand.arg]);
                    if (itemExpand)
                    {
                        itemExpand.Expand();
                        itemExpand.SideItemData.Item.Expand();
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
                        itemCollapse.SideItemData.Item.Collapse();
                    }
                }
                break;
            case (ushort)HierarchyEvent.Click:
                MsgHierarchy msgHierarchy = (MsgHierarchy)msg;
                bool jumpToSelect = false;
                if (msgHierarchy.item != null)
                {
                    foreach (KeyValuePair<int, int> userSelected in userSelectedItems)
                    {
                        TreeViewItem newSelectedItem = mTreeView.GetTreeItemById(userSelected.Value);
                        if (newSelectedItem != null)
                        {
                            if (msgHierarchy.item.ParentTreeItem != newSelectedItem.ParentTreeItem)
                            {
                                jumpToSelect = true;
                                break;
                            }
                        }
                    }

                    if (userSelectedItems.Count == 0)
                        jumpToSelect = true;
                }

                if (jumpToSelect || GlobalInfo.InSingleMode)
                {
                    ToolManager.SendBroadcastMsg(new MsgStringBool((ushort)IntegrationModuleEvent.JumpToSelect, msgHierarchy.uuid, GlobalInfo.InSingleMode), true);
                }
                else
                {
                    ToolManager.SendBroadcastMsg(new MsgIntString((ushort)ModelOperateEvent.Click, msgHierarchy.userId, msgHierarchy.uuid), true);
                }
                break;
            case (ushort)HierarchyEvent.Interactable:
                TreeView.interactable = ((MsgBool)msg).arg1;
                break;
        }
    }

    private bool selectEventRegistered = false;
    private void InitSelectionEvent()
    {
        SelectionModel selectionModel = ModelManager.Instance.modelGo.GetComponent<SelectionModel>();
        if (selectionModel && !selectEventRegistered)
        {
            selectEventRegistered = true;

            selectionModel.onSelectModel.AddListener((go, userId) =>
            {
                string uuid = ModelManager.Instance.GetUUIDByModel(go);
                //if (userId == GlobalInfo.account.id)
                //    currentSelectUUID = uuid;

                if (!string.IsNullOrEmpty(uuid))
                {
                    OnModelSelect(uuid, userId);
                }
                else
                {
                    OnItemCustomEvent(null, CustomEvent.ItemClicked, userId, uuid);
                }
            });

            selectionModel.onDeSelectModel.AddListener((go, userId) =>
            {
                string uuid = ModelManager.Instance.GetUUIDByModel(go);
                OnItemCustomEvent(null, CustomEvent.ItemClicked, userId, uuid);
            });

            foreach (KeyValuePair<GameObject, int> userSelect in selectionModel.userSelectModels)
            {
                OnModelSelect(ModelManager.Instance.GetUUIDByModel(userSelect.Key), userSelect.Value);
            }
        }
    }

    private void OnModelSelect(string uuid, int userId)
    {
        if (string.IsNullOrEmpty(uuid) || !viewItemIds.ContainsKey(uuid))
            return;

        TreeViewItem item = mTreeView.GetTreeItemById(viewItemIds[uuid]);
        if (item == null)
            return;

        mTreeView.ExpandParent(item);
        mSideTreeView.ExpandParent(item.SideItemData.Item);

        if (userId == GlobalInfo.account.id)
        {
            mTreeView.MoveToItem(item);
            mSideTreeView.MoveToItem(item.SideItemData.Item);
        }

        OnItemCustomEvent(item, CustomEvent.ItemClicked, userId, uuid);
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        GlobalInfo.currentWikiNames.Clear();
        GlobalInfo.currentWikiKnowledges.Clear();
        base.Close(uiData, callback);
    }

    #region 动效
#if UNITY_ANDROID || UNITY_IOS
    //protected override float joinAnimePlayTime => 0.3f;
    protected override float exitAnimePlayTime => 0.3f;
#else
    protected override float joinAnimePlayTime => 0.3f;
    protected override float exitAnimePlayTime => 0.2f;
#endif

    public override void JoinAnim(UnityAction callback)
    {
#if UNITY_ANDROID || UNITY_IOS
        JoinSequence.Join(Background.DOAnchorPos3DX(0, JoinAnimePlayTime));
#else
        JoinSequence.Join(Background.DOAnchorPos3DX(44f, JoinAnimePlayTime));
#endif
        base.JoinAnim(callback);
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