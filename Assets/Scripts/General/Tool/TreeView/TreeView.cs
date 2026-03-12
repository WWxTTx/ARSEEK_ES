using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;

/// <summary>
/// 树状视图 绑定在ScrollView.Content上
/// </summary>
public class TreeView : TreeList
{
    /// <summary>
    /// 各状态底板颜色
    /// </summary>
    public Color[] statusColor;

    private RectTransform RectTransform;
    private ScrollRect ScrollRect;
    private RectTransform ScrollViewRect;
    /// <summary>
    /// viewport中点
    /// </summary>
    private float centerPosY;

    #region 事件
    public Action<TreeViewItem> OnItemExpandBegin;
    public Action<TreeViewItem> OnItemExpanding;
    public Action<TreeViewItem> OnItemExpandEnd;

    public Action<TreeViewItem> OnItemCollapseBegin;
    public Action<TreeViewItem> OnItemCollapsing;
    public Action<TreeViewItem> OnItemCollapseEnd;

    public Action<TreeList> OnTreeListRepositionFinish;

    public Action<TreeList> OnTreeListAddOneItem;
    public Action<TreeList> OnTreeListDeleteOneItem;

    //called when a custom event is raised. you can raise a custom event by 
    //call TreeViewItem:RaiseCustomEvent(CustomEvent customEvent,System.Object param)
    //to notify the TreeView something happens, such as a TreeViewItem is clicked.
    //This callback is a bridge to connect the TreeView and its child TreeViewItems.
    public Action<TreeViewItem, CustomEvent, int, string> OnItemCustomEvent;
    #endregion

    [SerializeField]
    float mExpandUseTime = 5f;

    [SerializeField]
    float mExpandClipMoveSpeed = 100f;

    Dictionary<string, ItemPool> mItemPoolDict = new Dictionary<string, ItemPool>();
    RectTransform mPoolRootTrans;
    Dictionary<int, TreeViewItem> mTreeViewItemDict = new Dictionary<int, TreeViewItem>();
    int mCurTreeItemDictVersion = 0;

    List<TreeViewItem> mAllTreeViewItemList = null;
    int mCurTreeItemListVersion = 0;

    bool mNeedRepositionView = true;

    bool mNeedRepositionAll = true;

    [SerializeField]
    List<GameObject> mItemPrefabList = new List<GameObject>();

    /// <summary>
    /// 展开动画类型
    /// </summary>
    [SerializeField]
    ExpandAnimType mExpandAnimType = ExpandAnimType.Clip;
    public ExpandAnimType ExpandAnimateType
    {
        get { return mExpandAnimType; }
        set { mExpandAnimType = value; }
    }

    public bool NeedRepositionAll
    {
        get { return mNeedRepositionAll; }
        set { mNeedRepositionAll = value; }
    }


    [SerializeField]
    float mItemIndent;
    [SerializeField]
    float mChildTreeListPadding;
    [SerializeField]
    float mItemPadding;

    /// <summary>
    /// 节点缩进
    /// </summary>
    public float ItemIndent
    {
        get { return mItemIndent; }
        set
        {
            mItemIndent = value;
            NeedRepositionAll = true;
        }
    }

    /// <summary>
    /// 子树边距
    /// </summary>

    public float ChildTreeListPadding
    {
        get { return mChildTreeListPadding; }
        set
        {
            mChildTreeListPadding = value;
            NeedRepositionAll = true;
        }
    }

    /// <summary>
    /// 节点编剧
    /// </summary>
    public float ItemPadding
    {
        get { return mItemPadding; }
        set
        {
            mItemPadding = value;
            NeedRepositionAll = true;
        }
    }

    public float ExpandUseTime
    {
        get { return mExpandUseTime; }
        set { mExpandUseTime = value; }
    }

    public float ExpandClipMoveSpeed
    {
        get { return mExpandClipMoveSpeed; }
        set { mExpandClipMoveSpeed = value; }
    }

    public bool NeedRepositionView
    {
        get { return mNeedRepositionView; }
        set { mNeedRepositionView = value; }
    }

    public List<TreeViewItem> AllTreeViewItemList
    {
        get
        {
            if (mAllTreeViewItemList != null)
            {
                if (mCurTreeItemDictVersion == mCurTreeItemListVersion)
                {
                    return mAllTreeViewItemList;
                }
            }
            mCurTreeItemListVersion = mCurTreeItemDictVersion;
            mAllTreeViewItemList = new List<TreeViewItem>(mTreeViewItemDict.Values);
            return mAllTreeViewItemList;
        }
    }

    /// <summary>
    /// 初始化树状视图
    /// </summary>
    public void InitView()
    {
        RectTransform = transform.GetComponent<RectTransform>();
        ScrollRect = transform.GetComponentInParent<ScrollRect>();
        ScrollViewRect = ScrollRect.GetComponent<RectTransform>();

        Transform centerPoint = ScrollRect.viewport.FindChildByName("CenterPoint");
        centerPosY = RectTransform.parent.InverseTransformPoint(centerPoint.position).y;

        CachedRectTransform.anchorMax = new Vector2(0, 1);
        CachedRectTransform.anchorMin = CachedRectTransform.anchorMax;
        CachedRectTransform.pivot = new Vector2(0, 1);
        GameObject poolObj = new GameObject();
        poolObj.name = "ItemPool";
        RectTransform tf = poolObj.GetComponent<RectTransform>();
        if (tf == null)
        {
            tf = poolObj.AddComponent<RectTransform>();
        }
        tf.anchorMax = new Vector2(0.5f, 0.5f);
        tf.anchorMin = tf.anchorMax;
        tf.pivot = new Vector2(0.5f, 0.5f);
        tf.SetParent(CachedRectTransform);
        tf.anchoredPosition3D = Vector3.zero;
        mPoolRootTrans = tf;
        RootTreeView = this;
        ParentTreeItem = null;
        InitItemPool();
    }

    /// <summary>
    /// 初始化节点预制体对象池
    /// </summary>
    void InitItemPool()
    {
        foreach (GameObject itemPrefab in mItemPrefabList)
        {
            if (itemPrefab == null)
            {
                Debug.LogError("A item prefab is null ");
                return;
            }
            string prefabName = itemPrefab.name;
            if (mItemPoolDict.ContainsKey(prefabName))
            {
                Debug.LogError("A item prefab with name " + prefabName + " has existed!");
                return;
            }
            RectTransform rtf = itemPrefab.GetComponent<RectTransform>();
            if (rtf == null)
            {
                Debug.LogError("RectTransform component is not found in the prefab " + prefabName);
                return;
            }
            rtf.anchorMax = new Vector2(0, 1);
            rtf.anchorMin = rtf.anchorMax;
            rtf.pivot = new Vector2(0, 1);
            TreeViewItem tItem = itemPrefab.GetComponent<TreeViewItem>();
            if (tItem == null)
            {
                itemPrefab.AddComponent<TreeViewItem>();
            }
            ItemPool pool = new ItemPool(itemPrefab, 2);
            pool.Init();
            mItemPoolDict.Add(prefabName, pool);
        }
    }

    /// <summary>
    /// 新建节点
    /// </summary>
    /// <param name="itemPrefabName"></param>
    /// <returns></returns>
    public TreeViewItem NewTreeItem(string itemPrefabName)
    {
        ItemPool pool = null;
        if (mItemPoolDict.TryGetValue(itemPrefabName, out pool) == false)
        {
            return null;
        }
        TreeViewItem item = pool.GetItem();
        mTreeViewItemDict.Add(item.ItemId, item);
        mCurTreeItemDictVersion++;
        return item;
    }

    /// <summary>
    /// 回收节点
    /// </summary>
    /// <param name="item"></param>
    public void RecycleTreeItem(TreeViewItem item)
    {
        if (item == null)
        {
            return;
        }
        if (string.IsNullOrEmpty(item.ItemPrefabName))
        {
            return;
        }
        ItemPool pool = null;
        if (mItemPoolDict.TryGetValue(item.ItemPrefabName, out pool) == false)
        {
            return;
        }
        item.ParentTreeList = null;
        mTreeViewItemDict.Remove(item.ItemId);
        mCurTreeItemDictVersion++;
        item.CachedRectTransform.SetParent(mPoolRootTrans);
        pool.RecycleItem(item);
    }


    /// <summary>
    /// 通过uid获取节点
    /// </summary>
    /// <param name="itemId"></param>
    /// <returns></returns>
    public TreeViewItem GetTreeItemById(int itemId)
    {
        TreeViewItem item = null;
        if (mTreeViewItemDict.TryGetValue(itemId, out item))
        {
            return item;
        }
        return null;
    }

    /// <summary>
    /// 展开父节点
    /// </summary>
    /// <param name="item"></param>
    public void ExpandParent(TreeViewItem item)
    {
        if (item.ParentTreeItem == null)
        {
            return;
        }
        item.ExpandParent();
        ExpandParent(item.ParentTreeItem);
    }

    /// <summary>
    /// 定位到指定节点
    /// </summary>
    /// <param name="item"></param>
    public void MoveToItem(TreeViewItem item)
    {
        if (item == null)
            return;

        var worldPos = RectTransform.InverseTransformPoint(item.transform.position);
        float tempSelectY = worldPos.y;
        //transform.DOLocalMoveY(-(tempSelectY - centerPosY), 0.35f);
        int scaler = 3;
#if UNITY_ANDROID || UNITY_IOS
        scaler = 1;
#endif
        if (tempSelectY > centerPosY)
            ScrollRect.DOVerticalNormalizedPos(Mathf.Clamp01(1 + (tempSelectY + item.CachedRectTransform.sizeDelta.y * scaler) / RectTransform.sizeDelta.y), 0.35f);
        else
            ScrollRect.DOVerticalNormalizedPos(Mathf.Clamp01(1 + (tempSelectY - item.CachedRectTransform.sizeDelta.y * scaler) / RectTransform.sizeDelta.y), 0.35f);

        ScrollRect.DOHorizontalNormalizedPos(Mathf.Clamp01(worldPos.x / CachedRectTransform.sizeDelta.x), 0.35f);
    }


    void Update()
    {
        int count = mTreeItemList.Count;
        for (int i = 0; i < count; ++i)
        {
            TreeViewItem tItem = mTreeItemList[i];
            tItem.OnUpdate();
            if (tItem.NeedReposition)
            {
                NeedRepositionView = true;
            }
        }
        NeedRepositionAll = false;
        if (NeedRepositionView)
        {
            NeedRepositionView = false;
            DoReposition();
            if (OnTreeListRepositionFinish != null)
            {
                OnTreeListRepositionFinish(this);
            }
        }
    }

    private void OnDestroy()
    {
        List<ItemPool> pools = mItemPoolDict.Values.ToList();
        for (int i = 0; i < pools.Count; i++)
        {
            pools[i].Dispose();
        }
    }
}

#region 对象池
public class ItemPool
{
    GameObject mPrefabObj;
    string mPrefabName;
    int mInitCreateCount = 1;
    List<TreeViewItem> mPooledItemList = new List<TreeViewItem>();
    static int mCurItemIdCount = 0;
    public ItemPool(GameObject prefabObj, int createCount)
    {
        mPrefabObj = prefabObj;
        mPrefabName = mPrefabObj.name;
        mInitCreateCount = createCount;
    }
    public void Init()
    {
        mPrefabObj.SetActive(false);
        for (int i = 0; i < mInitCreateCount; ++i)
        {
            TreeViewItem tViewItem = CreateItem();
            RecycleItem(tViewItem);
        }
    }
    public TreeViewItem GetItem()
    {
        mCurItemIdCount++;
        int count = mPooledItemList.Count;
        TreeViewItem tItem = null;
        if (count == 0)
        {
            tItem = CreateItem();
        }
        else
        {
            tItem = mPooledItemList[count - 1];
            mPooledItemList.RemoveAt(count - 1);
            tItem.gameObject.SetActive(true);
        }
        tItem.ItemId = mCurItemIdCount;
        return tItem;

    }
    public TreeViewItem CreateItem()
    {

        GameObject go = GameObject.Instantiate<GameObject>(mPrefabObj);
        go.SetActive(true);
        TreeViewItem tViewItem = go.GetComponent<TreeViewItem>();
        tViewItem.ItemPrefabName = mPrefabName;
        return tViewItem;
    }
    public void RecycleItem(TreeViewItem item)
    {
        item.gameObject.SetActive(false);
        mPooledItemList.Add(item);
    }

    public void Dispose()
    {
        mPrefabObj.SetActive(false);
        for (int i = 0; i < mPooledItemList.Count; ++i)
        {
            GameObject.Destroy(mPooledItemList[i].gameObject);
        }
    }
}
#endregion

#region 自定义事件
public enum CustomEvent
{
    ItemClicked
}
#endregion