using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;

/// <summary>
/// 一点课/考核百科列表模块
/// </summary>
public class BaikeSelectModule : UIModuleBase
{
    private CanvasGroup canvasGroup;

    private RectTransform Background;
    private Text Page;
    private Button Collapse;

    private ScrollRect BaikeScroll;
    private Transform Content;

    /// <summary>
    /// 列表元素
    /// </summary>
    private GameObject TextureItem;
    private GameObject TextItem;

    /// <summary>
    /// 这个ID用于控制 tdptd 这个地方不好 应该是开启页面时
    /// </summary>
    public static int selectID;

    public static int CurrentBaikeIndex;

    private Dictionary<int, Toggle> baikeToggles = new Dictionary<int, Toggle>();

    private string headerPrefix = "课件列表";

    //获取视频百科封面图
    private GetFirstVideoImage VideoPreviewGetter;
    private Dictionary<int, string> videoUrls = new Dictionary<int, string>();
    private Dictionary<int, Transform> videoItems = new Dictionary<int, Transform>();

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        AddMsg(new ushort[] {
#if UNITY_ANDROID || UNITY_IOS
            (ushort)ARModuleEvent.Tracking,
#endif
            (ushort)BaikeSelectModuleEvent.BaikeSelect
        });

        VideoPreviewGetter = this.AutoComponent<GetFirstVideoImage>();

        if (GlobalInfo.IsExamMode())
        {
            headerPrefix = "操作项列表";
        }

        InitVariables();
        InitBaikeList();
    }

    public override void Show(UIData uiData = null)
    {
        base.Show(uiData);
        if (selectID > 0 && baikeToggles.Count > 0)
        {
            if (baikeToggles.TryGetValue(selectID, out Toggle toggle))
            {
                toggle.SetIsOnWithoutNotify(true);
            }
            Page.text = $"{headerPrefix}{CurrentBaikeIndex + 1}/{ GlobalInfo.currentWikiList.Count}";
        }
        //解决视频百科封面未加载就隐藏了模块，导致中止协程、加载不完全的问题
        LoadVideoPreviews();
    }

    private void InitVariables()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        Background = this.GetComponentByChildName<RectTransform>("Background");
        Page = this.GetComponentByChildName<Text>("Page");
        Collapse = this.GetComponentByChildName<Button>("Collapse");
        BaikeScroll = this.GetComponentByChildName<ScrollRect>("ScrollView");
        Content = BaikeScroll.content;
        TextureItem = BaikeScroll.FindChildByName("TextureItem").gameObject;
        TextItem = BaikeScroll.FindChildByName("TextItem").gameObject;
        Collapse.onClick.AddListener(() => SendMsg(new MsgBase((ushort)BaikeSelectModuleEvent.Hide)));
    }

    /// <summary>
    /// 初始化列表
    /// </summary>
    private void InitBaikeList()
    {
        this.GetComponentByChildName<Text>("HideText").text = "当前课程暂无百科";
        this.FindChildByName("Hide").gameObject.SetActive(GlobalInfo.currentWikiList == null || GlobalInfo.currentWikiList.Count == 0);

        if (GlobalInfo.IsExamMode())
        {
            InitExamItems();
        }
        else
        {
            videoUrls.Clear();

            InitBaikeItems();
            LoadVideoPreviews();
        }
    }

    /// <summary>
    /// 初始化课件列表项
    /// </summary>
    private void InitBaikeItems()
    {
        baikeToggles.Clear();

        Content.RefreshMultipleItemsView(new List<GameObject>(2) { TextItem, TextureItem },GlobalInfo.currentWikiList, (item, info) =>
        {
            item.name = info.id.ToString();
            item.GetComponentByChildName<Text>("Title").text = info.name;
            item.GetComponentByChildName<Text>("Type").text = info.typeDescription;

            switch (info.typeId)
            {
                case (int)PediaType.Picture:
                    if (!string.IsNullOrEmpty(info.iconPath))
                    {
                        ResManager.Instance.LoadCoverImage(info.id.ToString(), ResManager.Instance.OSSDownLoadPath + info.iconPath, false, (texture) =>
                        {
                            if (item && texture)
                            {
                                RawImage cover = item.GetComponentByChildName<RawImage>("Cover");
                                cover.texture = texture;
                                AspectRatioFitter aspectRatioFitter = cover.AutoComponent<AspectRatioFitter>();
                                aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                                aspectRatioFitter.aspectRatio = (float)texture.width / texture.height;
                            }
                        });
                    }
                    break;
                case (int)PediaType.ANV:
                    if (FileExtension.Convert(info.iconPath).Equals(FileExtension.MP4))
                    {
                        if (!videoUrls.ContainsKey(info.id))
                        {
                            videoUrls.Add(info.id, ResManager.Instance.OSSDownLoadPath + info.iconPath);
                            videoItems.Add(info.id, item);
                        }                     
                    }
                    break;
            }

            Toggle toggle = item.GetComponentInChildren<Toggle>();
            {
                toggle.onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                    {
                        FormMsgManager.Instance.SendMsg(new MsgInt((ushort)BaikeSelectModuleEvent.BaikeSelect, info.id));
                        //ToolManager.SendBroadcastMsg(new MsgInt((ushort)BaikeSelectModuleEvent.BaikeSelect, info.id), true);
                    }
                });

                if (selectID > 0 && selectID == info.id)
                {
                    toggle.SetIsOnWithoutNotify(true);

                    SelectElement(toggle.transform.GetSiblingIndex(), BaikeScroll);
                }
            }

            BaikeItemOnHover(item, info.typeId);

            baikeToggles.Add(info.id, toggle);
        });
    }

    /// <summary>
    /// 初始化操作列表项
    /// </summary>
    private void InitExamItems()
    {
        baikeToggles.Clear();
        Content.RefreshItemsView(TextItem,GlobalInfo.currentWikiList, (item, info) =>
        {
            item.name = info.id.ToString();
            item.GetComponentByChildName<Text>("Title").text = $"({info.totalScore}分){info.name}";
            item.GetComponentByChildName<Text>("Type").text = info.typeDescription;

            Toggle toggle = item.GetComponentInChildren<Toggle>();
            {
                toggle.onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                    {
                        ToolManager.SendBroadcastMsg(new MsgInt()
                        {
                            msgId = (ushort)BaikeSelectModuleEvent.BaikeSelect,
                            arg = info.id
                        });
                    }
                });

                if (selectID > 0 && selectID == info.id)
                {
                    toggle.SetIsOnWithoutNotify(true);
                    SelectElement(toggle.transform.GetSiblingIndex(), BaikeScroll);
                }
            }
            baikeToggles.Add(info.id, toggle);
        });
    }

    /// <summary>
    /// 加载视频百科封面列表
    /// </summary>
    private void LoadVideoPreviews() 
    {
        if (videoUrls.Count > 0)
        {
            VideoPreviewGetter.LoadVideoPreviews2(videoUrls, (data) =>
            {
                if (videoItems.TryGetValue(data.id, out Transform vt))
                {
                    RawImage cover = vt.GetComponentByChildName<RawImage>("Cover");
                    cover.texture = data.texture;
                    AspectRatioFitter aspectRatioFitter = cover.AutoComponent<AspectRatioFitter>();
                    aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                    aspectRatioFitter.aspectRatio = (float)data.width / data.height;
                }
                videoUrls.Remove(data.id);
            });
        }
    }

    /// <summary>
    /// 定位到选择元素
    /// </summary>
    /// <param name="index"></param>
    /// <param name="scrollRect"></param>
    private void SelectElement(int index, ScrollRect scrollRect)
    {
        var layout = scrollRect.content.GetComponent<VerticalLayoutGroup>();
        var remainingHeight = scrollRect.content.rect.height - scrollRect.viewport.rect.height/* - layout.padding.top*/;

        if (index > 0)
        {
            foreach (RectTransform child in scrollRect.content)
            {
                if (child.gameObject.activeInHierarchy)
                {
                    remainingHeight -= (child.rect.height + layout.spacing);

                    if (--index <= 0 || remainingHeight <= 0)
                    {
                        break;
                    }
                }
            }
        }

        if (index > 0 || remainingHeight <= 0)
        {
            scrollRect.verticalNormalizedPosition = 0;
        }
        else
        {
            scrollRect.verticalNormalizedPosition = remainingHeight / (scrollRect.content.rect.height - scrollRect.viewport.rect.height);
        }
    }

    /// <summary>
    /// 消息处理，同步百科选择
    /// </summary>
    /// <param name="msg"></param>
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
            case (ushort)BaikeSelectModuleEvent.BaikeSelect:
                int baikeId = ((MsgBrodcastOperate)msg).GetData<MsgInt>().arg;
                if (baikeToggles.TryGetValue(baikeId, out Toggle toggle))
                {
                    toggle.SetIsOnWithoutNotify(true);
                }
                CurrentBaikeIndex = GlobalInfo.currentWikiList.FindIndex(wiki => wiki.id == baikeId);
                Page.text = $"{headerPrefix}{CurrentBaikeIndex + 1}/{ GlobalInfo.currentWikiList.Count}";
                break;
        }
    }

    #region 动效
#if UNITY_ANDROID || UNITY_IOS
    protected override float joinAnimePlayTime => 0.3f;
    protected override float exitAnimePlayTime => 0.3f;
#else
    protected override float joinAnimePlayTime => 0.3f;
    protected override float exitAnimePlayTime => 0.2f;
#endif

    public override void JoinAnim(UnityAction callback)
    {
#if UNITY_ANDROID || UNITY_IOS
        JoinSequence.Join(Background.DOAnchorPos3DX(0f, JoinAnimePlayTime));
#else
        JoinSequence.Join(Background.DOAnchorPos3DX(44f, JoinAnimePlayTime));
#endif
        base.JoinAnim(callback);
    }

    public override void ExitAnim(UnityAction callback)
    {
        ExitSequence.Join(Background.DOAnchorPos3DX(-Background.sizeDelta.x, ExitAnimePlayTime));
        base.ExitAnim(callback);
    }

    /// <summary>
    /// 课件列表悬浮动效 文本类底板透明度变为16%; 图片类底板透明度变为15%
    /// </summary>
    public void BaikeItemOnHover(Transform item, int type)
    {
        UIFade uiFade = item.AutoComponent<UIFade>();
        switch (type)
        {
            case 1:
            case 2:
                uiFade.Init(item.GetComponentByChildName<Image>("Hover"), 0.16f, 0f);
                break;
            default:
#if UNITY_STANDALONE
                uiFade.Init(item.GetComponent<Image>(), 0.16f, 0.08f);
#else
                uiFade.Init(item.GetChild(0).GetComponent<Image>(), 1f, 0f);
#endif
                break;
        }
    }
    #endregion
}