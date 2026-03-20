using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityFramework.Runtime;


/// <summary>
/// 节点状态
/// </summary>
public enum TreeItemStatus
{
    None,
    /// <summary>
    /// 悬浮
    /// </summary>
    Hovered,
    /// <summary>
    /// 父节点被选中
    /// </summary>
    Wrapped,
    /// <summary>
    /// 自身被选中
    /// </summary>
    Selected
}

/// <summary>
/// 树状结构节点数据
/// </summary>
public class TreeViewItemData : MonoBehaviour
{
    public Image StatusImg;
    public Button_LinkMode ExpandBtn;
    public Image ExpandImg;
    public InputField Name;
    public Button_LinkMode ClickBtn;
    private CanvasGroup nameCanvas;
    public Image Attach;

    private string UUID;

    private int userId;
    /// <summary>
    /// 选中当前节点的用户ID，协同用
    /// </summary>
    public int UserId
    {
        get { return userId; }
        set
        {
            if (GlobalInfo.IsLiveMode())
                userId = value;
            else
                userId = 0;
            if (item.SideItemData != null)
                item.SideItemData.UserId = userId;
        }
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
        private set
        {
            mItemStatus = value;

            if (mItemStatus == TreeItemStatus.Selected && UserId != 0)
                StatusImg.color = NetworkManager.Instance.GetPlayerColor(UserId);
            else
                StatusImg.color = item.RootTreeView.statusColor[(int)mItemStatus];

            if (item.ItemType == ItemType.Main && item.SideItemData != null)
            {
                item.SideItemData.ItemStatus = value;
            }
            else if (item.SideItemData != null)
            {
                item.SideItemData.MainItem.ItemData.ItemStatus = value;
            }
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
            item.ChildTree.HighlightAllItem(value);
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

    private bool reselectable;
    /// <summary>
    /// 能否重复选择（在已被选中的情况下再次点击能否再次触发选中事件）
    /// </summary>
    public bool Reselectable
    {
        get { return reselectable; }
        set { reselectable = value; }
    }

    /// <summary>
    /// 数据绑定节点
    /// </summary>
    private TreeViewItem item;
    /// <summary>
    /// 数据绑定附件节点
    /// </summary>
    private TreeViewItem sideItem;
    /// <summary>
    /// 记录点击时间
    /// </summary>
    private float timer;

    private string no_breaking_space = "\u00A0";

    /// <summary>
    /// 绑定节点数据
    /// </summary>
    /// <param name="item"></param>
    /// <param name="uuid"></param>
    /// <param name="nodeName"></param>
    /// <param name="sideItem"></param>
    /// <param name="clickable"></param>
    /// <param name="reselectable"></param>
    public void Init(TreeViewItem item, string uuid, string nodeName, TreeViewItem sideItem,
        UnityAction<string, string, UnityAction<bool>> onEndEdit = null, bool clickable = true, bool reselectable = false)
    {
        this.item = item;
        this.sideItem = sideItem;

        UUID = uuid;
        transform.name = uuid;
        SetExpandBtnVisible(false);
        SetExpandStatus(true);
        IsSelected = false;
        Name.text = nodeName.Replace(" ", no_breaking_space); //nodeName;
        nameCanvas = Name.GetComponent<CanvasGroup>();
        Name.onEndEdit.AddListener((value) =>
        {
            StartCoroutine(DisableInput(Name));
            EditObjectName(value, onEndEdit);
        });

        //展开按钮
        //ExpandBtn.AddTarget(StatusImg);
        if (sideItem != null)
            ExpandBtn.AddTarget(sideItem.SideItemData.HoverImg);
        ExpandBtn.onClick.AddListener(() =>
        {
            if (item.IsExpand)
            {
                FormMsgManager.Instance.SendMsg(new MsgString((ushort)HierarchyEvent.Collapse, UUID));
            }
            else
            {
                FormMsgManager.Instance.SendMsg(new MsgString((ushort)HierarchyEvent.Expand, UUID));
            }
        });


        Reselectable = reselectable;
        if (clickable)
        {
            if (sideItem != null)
                ClickBtn.AddTarget(sideItem.SideItemData.HoverImg);
            ClickBtn.onClick.AddListener(() =>
            {
                //if (IsSelected && !Reselectable)//TODO old待删
                if (Time.time - timer < 1 && !Reselectable)//双击且可编辑
                {
                    //if (GlobalInfo.currentCourseInfo.creatorId == GlobalInfo.account.id && !GlobalInfo.IsLiveMode() && !GlobalInfo.IsExamMode())//TODO old待删
                    if (GlobalInfo.account.roleType == 1 && !GlobalInfo.IsLiveMode() && !GlobalInfo.IsExamMode())
                    {
                        prevObjectEditName = Name.text;
                        if (prevObjectEditName.Equals("未命名"))
                            Name.text = string.Empty;
                        nameCanvas.blocksRaycasts = true;
                        Name.interactable = true;
                        Name.Select();
                    }
                }
                else
                {
                    FormMsgManager.Instance.SendMsg(new MsgHierarchy((ushort)HierarchyEvent.Click, GlobalInfo.account.id, UUID, item));
                }
                timer = Time.time;
            });

            LayoutRebuilder.ForceRebuildLayoutImmediate(Name.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(item.CachedRectTransform);
        }
        else
        {
            ClickBtn.interactable = false;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(Name.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(item.CachedRectTransform);
    }


    IEnumerator DisableInput(InputField input)
    {
        yield return new WaitForEndOfFrame();
        input.interactable = false;
        nameCanvas.blocksRaycasts = false;
    }

    private void SetText(string txt)
    {
        Name.text = txt;
        this.WaitTime(0.1f, () => item.mNeedReposition = true);
    }

    public void SetAttachment(bool hasAttahment)
    {
        if (Attach)
        {
            Attach.gameObject.SetActive(true);
            Attach.color = hasAttahment ? Color.white : Color.black;
            Attach.SetAlpha(hasAttahment ? 1 : 0.3f);
        }
        else
        {
            sideItem.SideItemData.SetAttachment(hasAttahment);
        }
    }

    public void SetExpandBtnVisible(bool visible)
    {
        ExpandBtn.gameObject.SetActive(visible);
    }

    public void SetExpandStatus(bool expand)
    {
        if (expand)
            ExpandImg.transform.localEulerAngles = new Vector3(0, 0, 0);
        else
            ExpandImg.transform.localEulerAngles = new Vector3(0, 0, 90);
    }

    private string prevObjectEditName;
    /// <summary>
    /// 编辑节点名称
    /// </summary>
    private void EditObjectName(string name, UnityAction<string, string, UnityAction<bool>> callback = null)
    {
        if (name.Equals(prevObjectEditName))//未修改
            return;

        callback?.Invoke(UUID, name, (isSucces) =>
        {
            if (isSucces)
            {
                if (string.IsNullOrEmpty(name))
                    name = "未命名";
                SetText(name);
            }
            else
            {
                SetText(prevObjectEditName);
            }
        });
    }
}