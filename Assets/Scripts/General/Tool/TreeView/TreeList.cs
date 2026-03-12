using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 树状列表
/// </summary>
public class TreeList : MonoBehaviour
{
    float mTreeListMaxWidth = 1000f;
    /// <summary>
    /// 当前树下全部节点
    /// </summary>
    protected List<TreeViewItem> mTreeItemList = new List<TreeViewItem>();
    protected TreeView mRootTreeView;
    protected TreeViewItem mParentTreeItem;

    protected RectTransform mCachedViewportRect;
    protected float mCachedViewportHeight;
    protected RectTransform mCachedRectTransform;
    protected bool mNeedReposition = true;

    protected float mContentTotalWidth;
    protected float mContentTotalHeight;

    string mDefaultItemPrefabName;

    public string DefaultItemPrefabName
    {
        get { return mDefaultItemPrefabName; }
        set { mDefaultItemPrefabName = value; }
    }

    public float TreeListMaxWidth
    {
        get { return mTreeListMaxWidth; }
        set { mTreeListMaxWidth = value; }
    }

    public float ContentTotalHeight
    {
        get { return mContentTotalHeight; }
    }
    public float ContentTotalWidth
    {
        get { return mContentTotalWidth; }
    }

    public bool NeedReposition
    {
        get { return mNeedReposition; }
    }

    public float CachedViewportHeight
    {
        get
        {
            if (mCachedViewportRect == null)
            {
                mCachedViewportRect = transform.parent.GetComponent<RectTransform>();
                mCachedViewportHeight = mCachedViewportRect.rect.size.y;
            }
            return mCachedViewportHeight;
        }
    }

    public RectTransform CachedRectTransform
    {
        get
        {
            if (mCachedRectTransform == null)
            {
                mCachedRectTransform = gameObject.GetComponent<RectTransform>();
            }
            return mCachedRectTransform;
        }
    }

    public TreeView RootTreeView
    {
        get { return mRootTreeView; }
        set { mRootTreeView = value; }
    }
    public TreeViewItem ParentTreeItem
    {
        get { return mParentTreeItem; }
        set { mParentTreeItem = value; }
    }


    public int ItemCount
    {
        get { return mTreeItemList.Count; }
    }

    public bool IsEmpty
    {
        get { return (ItemCount == 0); }
    }


    public bool IsRootTree
    {
        get { return System.Object.ReferenceEquals(RootTreeView, this); }
    }


    public void Init()
    {
        mNeedReposition = true;
        mContentTotalHeight = 0;
        mContentTotalWidth = 0;
        mTreeItemList.Clear();
    }


    //This method is used to expand all the child TreeViewIItem’s childtree. 
    //If immediate is set to true, then all the child TreeViewIItem’s childtree will expand immediately.
    public void ExpandAllItem(bool immediate = false)
    {
        mNeedReposition = true;
        int count = ItemCount;
        for (int i = 0; i < count; ++i)
        {
            mTreeItemList[i].ExpandAll(immediate);
        }
    }

    //This method is used to collapse all the child TreeViewIItem’s childtree.
    //If immediate is set to true, then all the child TreeViewIItem’s childtree will collapse immediately.
    public void CollapseAllItem(bool immediate = false)
    {
        mNeedReposition = true;
        int count = ItemCount;
        for (int i = 0; i < count; ++i)
        {
            mTreeItemList[i].CollapseAll(immediate);
        }
    }

    public void HighlightAllItem(bool highlight)
    {
        int count = ItemCount;
        for (int i = 0; i < count; ++i)
        {
            mTreeItemList[i].HighlightAll(highlight);
        }
    }

    /// <summary>
    /// 添加节点
    /// </summary>
    /// <param name="prefabName"></param>
    /// <param name="userData"></param>
    /// <returns></returns>
    //This method is used to append a new item to a TreeList (TreeView is also a TreeList). 
    //prefabName is the name of the item prefab gameobject, and should be one element of the ItemPrefabList in the TreeView.
    //userData is the custom data of the new created TreeViewItem, and you can get the useData by call TreeViewItem.UserData.
    public TreeViewItem AppendItem(string prefabName = "", System.Object userData = null)
    {
        if (string.IsNullOrEmpty(prefabName))
        {
            prefabName = DefaultItemPrefabName;
        }
        TreeViewItem tViewItem = mRootTreeView.NewTreeItem(prefabName);
        if (tViewItem == null)
        {
            Debug.LogError("AppendItem return null ");
            return null;
        }
        InitTreeViewItem(tViewItem);
        tViewItem.Init();
        tViewItem.UserData = userData;
        tViewItem.ItemIndex = mTreeItemList.Count;
        mTreeItemList.Add(tViewItem);
        tViewItem.OnActived();
        UpdateAllItemSiblingIndex();
        mNeedReposition = true;
        if (IsRootTree)
        {
            RootTreeView.NeedRepositionView = true;
        }
        if (RootTreeView.OnTreeListAddOneItem != null)
        {
            RootTreeView.OnTreeListAddOneItem(this);
        }
        return tViewItem;
    }
    /// <summary>
    /// 初始化节点
    /// 指定根节点和父节点
    /// </summary>
    /// <param name="tViewItem"></param>
    void InitTreeViewItem(TreeViewItem tViewItem)
    {
        tViewItem.CachedRectTransform.SetParent(CachedRectTransform);
        tViewItem.CachedRectTransform.localEulerAngles = Vector3.zero;
        tViewItem.CachedRectTransform.localScale = Vector3.one;
        tViewItem.RootTreeView = RootTreeView;
        tViewItem.ParentTreeList = this;
    }

    /// <summary>
    /// 在指定索引位置插入节点
    /// </summary>
    /// <param name="itemIndex"></param>
    /// <param name="prefabName"></param>
    /// <param name="userData"></param>
    /// <returns></returns>
    // This method is used to insert a new item to a TreeList at itemIndex Pos.
    //All the child items of a TreeList are indexed from top to bottom starting at 0. 
    //That is to say the top most item of a TreeList is indexed 0 and the right below item is indexed 1.
    public TreeViewItem InsertItem(int itemIndex, string prefabName = "", System.Object userData = null)
    {
        if (itemIndex < 0 || itemIndex > mTreeItemList.Count)
        {
            return null;
        }
        if (string.IsNullOrEmpty(prefabName))
        {
            prefabName = DefaultItemPrefabName;
        }
        TreeViewItem tViewItem = mRootTreeView.NewTreeItem(prefabName);
        InitTreeViewItem(tViewItem);
        tViewItem.Init();
        tViewItem.UserData = userData;
        mTreeItemList.Insert(itemIndex, tViewItem);
        ResetAllItemIndex();
        tViewItem.OnActived();
        UpdateAllItemSiblingIndex();
        mNeedReposition = true;
        if (IsRootTree)
        {
            RootTreeView.NeedRepositionView = true;
        }
        if (RootTreeView.OnTreeListAddOneItem != null)
        {
            RootTreeView.OnTreeListAddOneItem(this);
        }
        return tViewItem;
    }

    /// <summary>
    /// 通过索引删除节点
    /// </summary>
    /// <param name="itemIndex"></param>
    public void DeleteItem(int itemIndex)
    {
        TreeViewItem tItem = GetItemByIndex(itemIndex);
        if (tItem == null)
        {
            return;
        }
        mTreeItemList.RemoveAt(itemIndex);
        ResetAllItemIndex();
        tItem.Clear();
        mRootTreeView.RecycleTreeItem(tItem);
        mNeedReposition = true;
        UpdateAllItemSiblingIndex();
        if (IsRootTree)
        {
            RootTreeView.NeedRepositionView = true;
        }
        if (RootTreeView.OnTreeListDeleteOneItem != null)
        {
            RootTreeView.OnTreeListDeleteOneItem(this);
        }
    }

    /// <summary>
    /// 获取节点
    /// </summary>
    /// <param name="itemIndex"></param>
    /// <returns></returns>
    public TreeViewItem GetItemByIndex(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= mTreeItemList.Count)
        {
            return null;
        }
        return mTreeItemList[itemIndex];
    }

    /// <summary>
    /// 更新全部节点的sibling index
    /// </summary>
    private void UpdateAllItemSiblingIndex()
    {
        int count = mTreeItemList.Count;
        for (int i = 0; i < count; ++i)
        {
            mTreeItemList[i].CachedRectTransform.SetSiblingIndex(i);
        }
    }

    /// <summary>
    /// 删除节点
    /// </summary>
    /// <param name="item"></param>
    public void DeleteItem(TreeViewItem item)
    {
        DeleteItem(item.ItemIndex);
    }

    /// <summary>
    /// 清空树，回收全部节点
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < mTreeItemList.Count; ++i)
        {
            mTreeItemList[i].Clear();
            mRootTreeView.RecycleTreeItem(mTreeItemList[i]);
        }
        mTreeItemList.Clear();
    }

    public void ResetAllItemIndex()
    {
        for (int i = 0; i < mTreeItemList.Count; ++i)
        {
            mTreeItemList[i].ItemIndex = i;
        }
    }

    public void OnUpdate()
    {
        int count = mTreeItemList.Count;
        for (int i = 0; i < count; ++i)
        {
            TreeViewItem tItem = mTreeItemList[i];
            tItem.OnUpdate();
            if (tItem.NeedReposition)
            {
                mNeedReposition = true;
            }
        }
    }

    /// <summary>
    /// 按规则对节点排序
    /// </summary>
    /// <param name="sortComp"></param>
    public void Sort(System.Comparison<TreeViewItem> sortComp)
    {
        mTreeItemList.Sort(sortComp);
        ResetAllItemIndex();
        mNeedReposition = true;
    }

    /// <summary>
    /// 刷新显示
    /// </summary>
    public void Reposition()
    {
        if (NeedReposition == false)
        {
            return;
        }
        DoReposition();
        if (mRootTreeView.OnTreeListRepositionFinish != null)
        {
            mRootTreeView.OnTreeListRepositionFinish(this);
        }
    }
    public void DoReposition()
    {
        mNeedReposition = false;
        int count = mTreeItemList.Count;
        mContentTotalHeight = 0;
        mContentTotalWidth = 0;

        CachedRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0);
        if (IsRootTree == false)
        {
            CachedRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, TreeListMaxWidth);
        }
        if (count == 0)
        {
            return;
        }

        float mContentPreviousHeight = CachedRectTransform.rect.size.y;
        float mContentPreviousWidth = CachedRectTransform.rect.size.x;

        float xOffset = CachedRectTransform.pivot.x * CachedRectTransform.sizeDelta.x;
        float itemPadding = 0;
        if (ParentTreeItem != null)
        {
            itemPadding = ParentTreeItem.ChildTreeItemPadding;
        }
        else
        {
            itemPadding = RootTreeView.ItemPadding;
        }
        float curY = 0;
        if (ParentTreeItem != null)
        {
            curY = -ParentTreeItem.ChildTreeListPadding;
        }
        for (int i = 0; i < count; ++i)
        {
            TreeViewItem tItem = mTreeItemList[i];
            tItem.CachedRectTransform.anchoredPosition3D = new Vector3(xOffset, curY, 0);
            tItem.Reposition();

            curY = curY - tItem.TotalHeight - itemPadding;
            if (tItem.MaxWidth > mContentTotalWidth)
            {
                mContentTotalWidth = tItem.MaxWidth;
            }
        }
        mContentTotalHeight = -curY - itemPadding;

        var rectSize = CachedRectTransform.rect.size;

        CachedRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, mContentTotalHeight);

        if (IsRootTree)
        {
            CachedRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, mContentTotalWidth);
        }
    }
}