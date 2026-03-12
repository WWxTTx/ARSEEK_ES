using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 模型层级结构附加节点数据
/// </summary>
public class SideTreeViewItemData : MonoBehaviour
{
    public Image StatusImg;
    public Image HoverImg;
    public Image Anim;
    public Image Attach;

    private string UUID;

    private int userId;
    /// <summary>
    /// 选中当前节点的用户ID，协同用
    /// </summary>
    public int UserId
    {
        get { return userId; }
        set { userId = value; }
    }

    private TreeItemStatus mItemStatus;
    /// <summary>
    /// 状态
    /// </summary>
    public TreeItemStatus ItemStatus
    {
        get
        {
            return mItemStatus;
        }
        set
        {
            mItemStatus = value;
            if (Item == null || Item.RootTreeView == null)
                return;

            if (mItemStatus == TreeItemStatus.Selected && UserId != 0)
                StatusImg.color = NetworkManager.Instance.GetPlayerColor(UserId);
            else
                StatusImg.color = Item.RootTreeView.statusColor[(int)mItemStatus];
        }
    }

    /// <summary>
    /// 是否被选中
    /// </summary>
    public bool IsSelected
    {
        get { return ItemStatus == TreeItemStatus.Selected; }
        set
        {
            ItemStatus = value ? TreeItemStatus.Selected : TreeItemStatus.None;
            Item.ChildTree.HighlightAllItem(value);
        }
    }

    /// <summary>
    /// 父节点是否被选中
    /// </summary>
    public bool IsWrapped
    {
        get { return ItemStatus == TreeItemStatus.Wrapped; }
        set { ItemStatus = value ? TreeItemStatus.Wrapped : TreeItemStatus.None; }
    }

    public TreeViewItem Item { get; private set; }

    public TreeViewItem MainItem { get; private set; }

    /// <summary>
    /// 绑定节点
    /// </summary>
    /// <param name="item"></param>
    /// <param name="uuid"></param>
    /// <param name="hasAttahment"></param>
    /// <param name="mainItem"></param>
    public void Init(TreeViewItem item, string uuid, /*bool hasAnim,*/ bool hasAttahment, TreeViewItem mainItem)
    {
        this.Item = item;
        this.MainItem = mainItem;

        UUID = uuid;
        transform.name = uuid;
        IsSelected = false;
        //Anim.color = hasAttahment ? Color.white : Color.black;
        //Anim.SetAlpha(hasAnim ? 1 : 0.3f);
        Attach.color = hasAttahment ? Color.white : Color.black;
        Attach.SetAlpha(hasAttahment ? 1 : 0.3f);
    }


    public void SetAttachment(bool hasAttahment)
    {
        Attach.color = hasAttahment ? Color.white : Color.black;
        Attach.SetAlpha(hasAttahment ? 1 : 0.3f);
    }
}