using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;

/// <summary>
/// 知识点模块
/// </summary>
public class KnowledgeModule : UIModuleBase
{
    public Sprite[] FileSprites;

    private Transform currentObject;
    /// <summary>
    /// 当前知识点id
    /// </summary>
    private int currentKnowledgeID;
    /// <summary>
    /// 当前唯一id
    /// </summary>
    private string currentUUID;

    /// <summary>
    /// 动画期间禁止控制
    /// </summary>
    private CanvasGroup canvasGroup;

    private RectTransform background;
    //用于PC端编辑课件动效
#if UNITY_STANDALONE 
    private RectTransform ContentMask;
    private CanvasGroup ContentCanvasGroup;
#endif
    private Button Collapse;
#if UNITY_ANDROID || UNITY_IOS
    private Button Back;
#endif

    /// <summary>
    /// 顶部添加按钮
    /// </summary>
    private GameObject AddBtns;
    /// <summary>
    /// 添加知识点按钮
    /// </summary>
    private Button AddKnowledgeBtn;
    /// <summary>
    /// 添加超链接按钮
    /// </summary>
    private Button AddHyperlinkBtn;
    /// <summary>
    /// 列表页面
    /// </summary>
    private GameObject ListPanel;
    private ReorderableList ReorderableList;

#if UNITY_STANDALONE
    /// <summary>
    /// 更多操作面板
    /// </summary>
    private GameObject MorePanel;
    private RectTransform MoreBackground;
    private CanvasGroup MoreCanvasGroup;
    private Button EditBtn;
    private Button ReplaceBtn;
    private Button DeleteBtn;
#endif
    private GameObject HideGo;
    private Text HideText;
    private GameObject ListLoadAnim;
    /// <summary>
    /// 元素
    /// </summary>
    private GameObject KnowledgeItem;
    private GameObject HyperlinkItem;
    private GameObject ImageItem;
    private GameObject VideoItem;
    private GameObject AudioItem;

    /// <summary>
    /// 当前选中物体的知识点
    /// </summary>
    private List<Knowledgepoint> currentKnowledgepoints = new List<Knowledgepoint>();

    private GetFirstVideoImage VideoPreviewGetter;

    private Dictionary<int, string> videoUrls = new Dictionary<int, string>();

    private Dictionary<int, Transform> videoItems = new Dictionary<int, Transform>();

    private int draggedIndex;

    private DismantlingController dismantlingController;

    private AnimController animController;

    private SmallFlowCtrl smallFlowCtrl;

    /// <summary>
    /// 是否有权限编辑课件资料
    /// </summary>
    private bool CanEditCourseware = true;

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        AddMsg(new ushort[]
        {
#if UNITY_ANDROID || UNITY_IOS
            (ushort)ARModuleEvent.Tracking,
#endif
            (ushort)IntegrationModuleEvent.AnimSelect,
            (ushort)SmallFlowModuleEvent.SelectFlow,
            (ushort)SmallFlowModuleEvent.SelectStep,
            (ushort)SmallFlowModuleEvent.SelectStep,
            (ushort)SmallFlowModuleEvent.Guide,
            (ushort)SmallFlowModuleEvent.CompleteStep
        });
        Init();
        RegisterEvent();
    }

    public override void Show(UIData uiData = null)
    {
        base.Show(uiData);
        InitPediaController();
        RefreshListByUUID();
    }

    /// <summary>
    /// 初始化变量
    /// </summary>
    private void Init()
    {
        VideoPreviewGetter = GetComponent<GetFirstVideoImage>();
        canvasGroup = GetComponent<CanvasGroup>();
        background = this.FindChildByName("BackGround").GetComponent<RectTransform>();
#if UNITY_STANDALONE
        ContentMask = transform.GetComponentByChildName<RectTransform>("ContentMask");
        ContentCanvasGroup = ContentMask.AutoComponent<CanvasGroup>();
#endif
        Collapse = this.GetComponentByChildName<Button>("Collapse");
        ListPanel = this.FindChildByName("ListPanel").gameObject;

        ReorderableList = this.GetComponentByChildName<ReorderableList>("ReorderableList");
        ReorderableList.AddOnBeginOrderListener((index) =>
        {
            draggedIndex = index + ReorderableList.ReferencePointsCount;
        });
        ReorderableList.AddOnEndOrderListener((index) =>
        {
            ReorderableList.Orderable = false;

            if (draggedIndex != index/* && knowledgepoints.ContainsKey(currentUniqueID)*/)
            {
                List<int> ids = new List<int>(ReorderableList.ElementsCount);
                for (int i = 0; i < ReorderableList.ElementsCount; i++)
                {
                    string name = ReorderableList.Content.transform.GetChild(i + ReorderableList.ReferencePointsCount).name;
                    ids.Add(int.Parse(name));
                }
                ids.Reverse();
                RequestManager.Instance.SortKnowledgepoint(ids, () =>
                {
                    Log.Debug("修改知识点顺序成功");
                    ReorderableList.UpdateIndex();
                }, (code, message) =>
                 {
                     Log.Error($"修改知识点顺序失败 {code} {message}");
                 });
            }
        });

        KnowledgeItem = this.FindChildByName("KnowledgeItem").gameObject;
        HyperlinkItem = this.FindChildByName("HyperlinkItem").gameObject;
        ImageItem = this.FindChildByName("ImageItem").gameObject;
        VideoItem = this.FindChildByName("VideoItem").gameObject;
        AudioItem = this.FindChildByName("AudioItem").gameObject;

        HideGo = this.FindChildByName("Hide").gameObject;
        HideText = this.GetComponentByChildName<Text>("HideText");
        ListLoadAnim = this.FindChildByName("LoadAnim").gameObject;

#if UNITY_STANDALONE
        MorePanel = this.FindChildByName("MorePanel").gameObject;
        MorePanel.GetComponent<Button>().onClick.AddListener(() => CloseMorePanel(ExitAnimePlayTime));
        MoreBackground = this.GetComponentByChildName<RectTransform>("MoreBackground");
        MoreCanvasGroup = MoreBackground.GetComponent<CanvasGroup>();
        EditBtn = MoreBackground.GetComponentByChildName<Button>("Edit");
        ReplaceBtn = MoreBackground.GetComponentByChildName<Button>("Replace");
        DeleteBtn = MoreBackground.GetComponentByChildName<Button>("Del");

        AddBtns = this.FindChildByName("AddBtns").gameObject;
        AddKnowledgeBtn = this.GetComponentByChildName<Button>("AddKnowledgeBtn");
        AddHyperlinkBtn = this.GetComponentByChildName<Button>("AddHyperlinkBtn");

        if (GlobalInfo.IsLiveMode() || GlobalInfo.IsExamMode() || GlobalInfo.account.roleType != 1)
        {
            CanEditCourseware = false;
        }
        AddBtns.SetActive(CanEditCourseware);
        ReorderableList.GetComponentByChildName<RectTransform>("List").offsetMax = new Vector2(0, CanEditCourseware ? - 64 : 0);
#endif
    }

    /// <summary>
    /// 获取百科控制器，获取选中模型的UUID
    /// </summary>
    private void InitPediaController()
    {
        switch (GlobalInfo.currentWiki.typeId)
        {
            case (int)PediaType.Disassemble:
                dismantlingController = ModelManager.Instance.modelRoot.GetComponentInChildren<DismantlingController>(true);
                if (dismantlingController)
                {
                    var SelectionModel = ModelManager.Instance.modelRoot.GetComponentInChildren<SelectionModel>(true);
                    if (SelectionModel)
                    {
                        SelectionModel?.onSelectModel.AddListener(SelectionEvent);
                        SelectionModel?.onClearSelection.AddListener(ClearSelectionEvent);
                        if (GlobalInfo.IsLiveMode())
                        {
                            if (GlobalInfo.IsOperator())
                                currentObject = dismantlingController.SelectionCtrl.GetUserSelectedGo(GlobalInfo.account.id)?.transform;
                            else
                                currentObject = dismantlingController.SelectionCtrl.GetUserSelectedGo(GlobalInfo.mainScreenId)?.transform;
                        }
                        else
                            currentObject = dismantlingController.localSelectModel?.transform;
                        currentUUID = currentObject ? currentObject.GetComponent<ModelInfo>()?.ID : string.Empty;
                    }
                }
                break;
            case (int)PediaType.Animation:
                animController = ModelManager.Instance.modelRoot.GetComponentInChildren<AnimController>(true);
                if (animController && animController.PlayableList != null && !string.IsNullOrEmpty(animController.CurrentPlayableProp))
                {
                    currentUUID = animController.PlayableList[animController.CurrentPlayableProp].UUID;
                }
                break;
            case (int)PediaType.Operation:
                smallFlowCtrl = ModelManager.Instance.modelRoot.GetComponentInChildren<SmallFlowCtrl>(true);
                if (smallFlowCtrl && smallFlowCtrl.flows != null && smallFlowCtrl.flows.Length > 0)
                {
                    int flowIndex = Mathf.Clamp(smallFlowCtrl.index_NowFlow, 0, smallFlowCtrl.flows.Length - 1);
                    int stepIndex= Mathf.Clamp(smallFlowCtrl.index_NowStep, 0, smallFlowCtrl.flows[flowIndex].steps.Count - 1);
                    currentUUID = smallFlowCtrl.flows[flowIndex].steps[stepIndex].ID;
                }
                break;
        }
    }

    /// <summary>
    /// 修复空格导致的换行问题
    /// </summary>
    private string no_breaking_space = "\u00A0";
    void ReplaceSpace(InputField inputField, string inputStr)
    {
        inputField.text = inputStr.Replace(" ", no_breaking_space);
    }

    /// <summary>
    /// 注册事件
    /// </summary>
    private void RegisterEvent()
    {
        Collapse.onClick.AddListener(() => SendMsg(new MsgBase((ushort)KnowledgeModuleEvent.Hide)));

#if UNITY_STANDALONE
        AddKnowledgeBtn.onClick.AddListener(() =>
        {
            SendMsg(new MsgBool((ushort)CoursePanelEvent.EditMode, true));

            HideGo.SetActive(false);

            Transform newKnowledge = Instantiate(KnowledgeItem, ReorderableList.Content.transform).transform;
            newKnowledge.SetSiblingIndex(ReorderableList.ReferencePointsCount);

            GameObject editPanel = newKnowledge.FindChildByName("EditPanel").gameObject;
            editPanel.SetActive(true);

            InputField title = newKnowledge.GetComponentByChildName<InputField>("Title");
            title.onValueChanged.AddListener((value) => ReplaceSpace(title, value));
            title.interactable = true;
            title.image.raycastTarget = true;
            title.Select();
            InputField content = newKnowledge.GetComponentByChildName<InputField>("Content");
            content.onValueChanged.AddListener((value) =>
            {
                ReplaceSpace(content, value);
                LayoutRebuilder.ForceRebuildLayoutImmediate(content.transform as RectTransform);
            });
            content.interactable = true;
            content.image.raycastTarget = true;

            newKnowledge.GetComponentByChildName<Button>("SaveEdit").onClick.AddListener(() =>
            {
                SendMsg(new MsgBool((ushort)CoursePanelEvent.EditMode, false));
                AddKnowledge(title, content);
            });
            newKnowledge.GetComponentByChildName<Button>("QuitEdit").onClick.AddListener(() =>
            {
                SendMsg(new MsgBool((ushort)CoursePanelEvent.EditMode, false));
                RemoveNewKnowledgeItem(newKnowledge);
                HideGo.SetActive(currentKnowledgepoints.Count == 0);
            });

            AddNewKnowledgeItem(newKnowledge);

            Canvas canvas = newKnowledge.AutoComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 1;
            newKnowledge.AutoComponent<GraphicRaycaster>();
        });

        AddHyperlinkBtn.onClick.AddListener(() =>
        {
            if (string.IsNullOrEmpty(currentUUID))
                return;
            SendMsg(new MsgBool((ushort)CoursePanelEvent.EditMode, true));
            OpenLinkDatabase(true);
        });
#endif
    }

    /// <summary>
    /// 请求当前选中模型知识点列表
    /// </summary>
    /// <param name="callback"></param>
    private void RefreshListByUUID(UnityAction callback = null)
    {
#if UNITY_STANDALONE
        CloseMorePanel();
#endif
        ReorderableList.ClearElement(true);
        ListPanel.SetActive(false);

        if (string.IsNullOrEmpty(currentUUID))
        {
            switch (GlobalInfo.currentWiki.typeId)
            {
                case (int)PediaType.Disassemble:
                    HideText.text = "当前未选中模型";
                    break;
                default:
                    HideText.text = "未配置有效ID";
                    break;
            }
            HideGo.SetActive(true);
            return;
        }

        RequestManager.Instance.GetKnowledgepointsByUUID(GlobalInfo.currentWiki.id, currentUUID, (list) =>
        {
            ListPanel.SetActive(true);
            currentKnowledgepoints = list;
            //按添加时间降序（新添加的在上面
            currentKnowledgepoints.Reverse();
            InstantiateList(callback);
        }, (code, msg) =>
        {
            Log.Error($"获取知识点失败 {currentUUID}, [{code}] {msg}");
            string error = "加载失败";
            switch (code)
            {
                case 0:
                    error = "网络连接断开，请检查网络设置";
                    break;
            }
            UIManager.Instance.OpenModuleUI<LocalTipModule_Button>(ParentPanel, ListPanel.transform,
                new LocalTipModule_Button.ModuleData(error, "刷新", () => RefreshListByUUID()));
            ListPanel.SetActive(true);
        });
    }

    /// <summary>
    /// 刷新列表
    /// </summary>
    /// <param name="callback"></param>
    private void InstantiateList(UnityAction callback)
    {
        ListLoadAnim.SetActive(false);
        ReorderableList.Viewport.SetActive(true);

        if (currentKnowledgepoints.Count == 0)
        {
            HideText.text = "未添加课件";
            HideGo.SetActive(true);
        }
        else
        {
            HideGo.SetActive(false);
        }

        SendMsg(new MsgStringInt((ushort)HierarchyEvent.UpdateAttachment, currentUUID, currentKnowledgepoints.Count));

        videoUrls.Clear();
        videoItems.Clear();

        ReorderableList.Content.transform.RefreshMultipleItemsView(new List<GameObject>() { KnowledgeItem, HyperlinkItem, ImageItem, VideoItem, AudioItem },
            currentKnowledgepoints, InitKnowledgepointItem, ReorderableList.ReferencePointsCount);

        if (videoUrls.Count > 0)
        {
            VideoPreviewGetter.LoadVideoPreviews2(videoUrls, (data) =>
            {
                if (videoItems.TryGetValue(data.id, out Transform vt))
                {
                    RawImage rawImage = vt.GetComponentByChildName<RawImage>("Texture");
                    rawImage.texture = data.texture;
                    rawImage.SetAlpha(1);
                    vt.GetComponentByChildName<Text>("Length").text = data.length;

                    AspectRatioFitter aspectRatioFitter = rawImage.AutoComponent<AspectRatioFitter>();
                    aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                    aspectRatioFitter.aspectRatio = (float)data.width / data.height;
                }
            });
        }

        RefreshLayouGroup();
        callback?.Invoke();
    }

    private void SelectionEvent(GameObject arg0, int arg1)
    {
        if (GlobalInfo.ShouldProcess(arg1))
        {
            UIManager.Instance.CloseModuleUI<LinkDatabaseModule>(ParentPanel);

#if UNITY_STANDALONE
            CloseMorePanel();
#endif
            currentObject = arg0?.transform;
            currentUUID = arg0 ? arg0.GetComponent<ModelInfo>()?.ID : string.Empty;

            //if(arg0)
            RefreshListByUUID();
        }
    }

    private void ClearSelectionEvent()
    {
        currentObject = null;
        currentUUID = string.Empty;
        RefreshListByUUID();
    }

    /// <summary>
    /// 初始化知识点元素
    /// </summary>
    /// <param name="item"></param>
    /// <param name="info"></param>
    private void InitKnowledgepointItem(Transform item, Knowledgepoint info)
    {
        item.name = info.id.ToString();

        if (info.type.Equals("TXT"))
        {
            InitTXTPoint(item, info);
        }
        else
        {
            string title = System.IO.Path.GetFileNameWithoutExtension(info.title);
            item.GetComponentByChildName<Text>("Title")?.EllipsisText(title, 2, "...");

#if UNITY_ANDROID || UNITY_IOS
            item.GetComponent<Button>().onClick.AddListener(() => OpenLink(info));
#elif UNITY_STANDALONE
            if (CanEditCourseware)
            {
                item.GetComponentByChildName<Button>("Drag").gameObject.SetActive(true);
                Button more = item.GetComponentByChildName<Button>("More");
                more.onClick.AddListener(() =>
                {
                    EditBtn.gameObject.SetActive(false);
                    ReplaceBtn.gameObject.SetActive(true);
                    ReplaceBtn.onClick.RemoveAllListeners();
                    ReplaceBtn.onClick.AddListener(() =>
                    {
                        SendMsg(new MsgBool((ushort)CoursePanelEvent.EditMode, true));

                        currentKnowledgeID = info.id;
                        CloseMorePanel();
                        OpenLinkDatabaseToReplace(info.id, info.docType);
                    });
                    DeleteBtn.onClick.RemoveAllListeners();
                    DeleteBtn.onClick.AddListener(() =>
                    {
                        DeleteKnowledge(item, info);
                        CloseMorePanel();
                    });

                    MoreBackground.anchoredPosition = new Vector2(-25, ReorderableList.Content.GetComponent<RectTransform>().localPosition.y + (item as RectTransform).localPosition.y + (item as RectTransform).sizeDelta.y / 2 - 154);
                    OpenMorePanel();
                });
                more.gameObject.SetActive(true);
            }
#endif

            switch (info.docType)
            {
                case FileExtension.DOC:
                case FileExtension.PPT:
                case FileExtension.XLS:
                case FileExtension.PDF:
#if UNITY_STANDALONE
                    item.GetComponentByChildName<Button>("Show").onClick.AddListener(() => OpenLink(info));
#endif
                    SetDocTypeIcon(info.docType, item.GetComponentByChildName<Image>("Type"));
                    break;
                case FileExtension.IMG:
#if UNITY_STANDALONE
                    item.GetComponent<Button>().onClick.AddListener(() => OpenLink(info));
#endif
                    RawImage image = item.GetComponentByChildName<RawImage>("Image");

                    string url = ResManager.Instance.OSSDownLoadPath + info.content;
                    ResManager.Instance.LoadKnowledgepointImage(info.id.ToString(), url, (arg1) =>
                    {
                        if (arg1 == null)
                        {
                            Log.Error($"图片加载失败 {url}");
                            return;
                        }
                        if (image != null)
                        {
                            image.texture = arg1;
                            image.SetAlpha(1);

                            AspectRatioFitter aspectRatioFitter = image.AutoComponent<AspectRatioFitter>();
                            aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                            aspectRatioFitter.aspectRatio = (float)arg1.width / arg1.height;
                        }
                    });
                    break;
                case FileExtension.MP4:
#if UNITY_STANDALONE
                    item.GetComponentByChildName<Button>("PlayBtn").onClick.AddListener(() => OpenLink(info));
#endif
                    if (!videoUrls.ContainsKey(info.id))
                    {
                        videoUrls.Add(info.id, ResManager.Instance.OSSDownLoadPath + info.content);
                        videoItems.Add(info.id, item);
                    }
                    break;
                case FileExtension.MP3:
#if UNITY_STANDALONE
                    item.GetComponentByChildName<Button>("Show").onClick.AddListener(() => OpenLink(info));
#endif
                    break;
            }
            ReorderableList.AddElement(item.gameObject);
        }

        KnowledgeItemOnHover(item, info.docType);
    }

    private void InitTXTPoint(Transform item, Knowledgepoint info)
    {
#if UNITY_ANDROID || UNITU_IOS
        item.GetComponentByChildName<Text>("Title").EllipsisText(info.title.Trim(), "...");
        Text content = item.GetComponentByChildName<Text>("Content");
        content.text = info.content;
        LayoutRebuilder.ForceRebuildLayoutImmediate(content.transform as RectTransform);
#else
        InputField title = item.GetComponentByChildName<InputField>("Title");
        string knowledgeTitle = info.title.Trim();
        title.text = knowledgeTitle.Length > 9 ? $"{knowledgeTitle.Substring(0, 9)}..." : knowledgeTitle;
        title.onValueChanged.RemoveAllListeners();
        title.onValueChanged.AddListener((value) => ReplaceSpace(title, value));
        title.interactable = false;
        title.image.raycastTarget = false;

        InputField content = item.GetComponentByChildName<InputField>("Content");
        content.onValueChanged.RemoveAllListeners();
        content.onValueChanged.AddListener((value) =>
        {
            ReplaceSpace(content, value);
        });
        content.text = info.content;
        content.interactable = false;
        content.image.raycastTarget = false;
        LayoutRebuilder.ForceRebuildLayoutImmediate(item as RectTransform);

        if (CanEditCourseware)
        {
            item.GetComponentByChildName<Button>("Drag").gameObject.SetActive(true);
            GameObject editPanel = item.FindChildByName("EditPanel").gameObject;
            editPanel.SetActive(false);
            Button more = item.GetComponentByChildName<Button>("More");
            more.onClick.RemoveAllListeners();
            more.onClick.AddListener(() =>
            {
                ReplaceBtn.gameObject.SetActive(false);
                EditBtn.gameObject.SetActive(true);
                EditBtn.onClick.RemoveAllListeners();
                EditBtn.onClick.AddListener(() =>
                {
                    SendMsg(new MsgBool((ushort)CoursePanelEvent.EditMode, true));

                    currentKnowledgeID = info.id;
                    //将当前item设置为可编辑状态
                    editPanel.SetActive(true);
                    CloseMorePanel();
                    more.gameObject.SetActive(false);

                    title.onValueChanged.AddListener((value) => ReplaceSpace(title, value));
                    title.image.raycastTarget = true;
                    title.interactable = true;
                    content.onValueChanged.AddListener((value) =>
                    {
                        ReplaceSpace(content, value);
                        LayoutRebuilder.ForceRebuildLayoutImmediate(content.transform as RectTransform);
                    });
                    content.image.raycastTarget = true;
                    content.interactable = true;

                    item.AutoComponent<Canvas>().overrideSorting = true;
                    item.AutoComponent<Canvas>().sortingOrder = 1;
                    item.AutoComponent<GraphicRaycaster>();
                });

                DeleteBtn.onClick.RemoveAllListeners();
                DeleteBtn.onClick.AddListener(() =>
                {
                    DeleteKnowledge(item, info);
                    CloseMorePanel();
                });
                MoreBackground.anchoredPosition = new Vector2(-25, ReorderableList.Content.GetComponent<RectTransform>().localPosition.y + (item as RectTransform).localPosition.y + (item as RectTransform).sizeDelta.y / 2 - 154);
                OpenMorePanel();
            });
            more.gameObject.SetActive(true);

            Button SaveEdit = item.GetComponentByChildName<Button>("SaveEdit");
            SaveEdit.onClick.RemoveAllListeners();
            SaveEdit.onClick.AddListener(() =>
            {
                SendMsg(new MsgBool((ushort)CoursePanelEvent.EditMode, false));
                if (item.TryGetComponent<GraphicRaycaster>(out GraphicRaycaster graphicRaycaster))
                    Destroy(graphicRaycaster);
                if (item.TryGetComponent<Canvas>(out Canvas canvas))
                {
                    canvas.overrideSorting = false;
                    Destroy(canvas);
                }
                EditKnowledge(info, title, content);
            });

            Button QuitEdit = item.GetComponentByChildName<Button>("QuitEdit");
            QuitEdit.onClick.RemoveAllListeners();
            QuitEdit.onClick.AddListener(() =>
            {
                SendMsg(new MsgBool((ushort)CoursePanelEvent.EditMode, false));
                if (item.TryGetComponent<GraphicRaycaster>(out GraphicRaycaster graphicRaycaster))
                    Destroy(graphicRaycaster);
                if (item.TryGetComponent<Canvas>(out Canvas canvas))
                {
                    canvas.overrideSorting = false;
                    Destroy(canvas);
                }

                editPanel.SetActive(false);
                more.gameObject.SetActive(true);

                title.image.raycastTarget = false;
                title.interactable = false;
                content.image.raycastTarget = false;
                content.interactable = false;

                title.text = info.title.Trim();
                content.text = info.content;
            });
        }
#endif
        item.GetComponent<VerticalLayoutGroup>();
        ReorderableList.AddElement(item.gameObject);
    }

    private void SetDocTypeIcon(string docType, Image image)
    {
        switch (docType)
        {
            case FileExtension.PPT:
                image.sprite = FileSprites[0];
                break;
            case FileExtension.DOC:
                image.sprite = FileSprites[1];
                break;
            case FileExtension.XLS:
                image.sprite = FileSprites[2];
                break;
            case FileExtension.PDF:
                image.sprite = FileSprites[3];
                break;
        }
    }

    /// 查看超链接
    /// </summary>
    /// <param name="title"></param>
    /// <param name="info"></param>
    private void OpenLink(Knowledgepoint info)
    {
        string url = info.content;
        MsgHyperlink msgHyperlink = null;
        switch (info.docType)
        {
            case FileExtension.DOC:
            case FileExtension.PPT:
            case FileExtension.XLS:
            case FileExtension.PDF:
                msgHyperlink = new MsgHyperlink((ushort)HyperLinkEvent.HyperlinkDOC, info.id, info.title, url, info.docType);
                break;
            case FileExtension.IMG:
                msgHyperlink = new MsgHyperlink((ushort)HyperLinkEvent.HyperlinkImage, info.id, ResManager.Instance.OSSDownLoadPath + info.title, url, info.docType);
                break;
            case FileExtension.MP4:
                msgHyperlink = new MsgHyperlink((ushort)HyperLinkEvent.HyperlinkVideo, info.id, ResManager.Instance.OSSDownLoadPath + info.title, url, info.docType);
                break;
            case FileExtension.MP3:
                msgHyperlink = new MsgHyperlink((ushort)HyperLinkEvent.HyperlinkAudio, info.id, ResManager.Instance.OSSDownLoadPath + info.title, url, info.docType);
                break;
            default:
                break;
        }

        MsgBrodcastOperate msgBrodcastOperate = new MsgBrodcastOperate(msgHyperlink.msgId, JsonTool.Serializable(msgHyperlink));
        SendMsg(msgBrodcastOperate);
    }

    /// <summary>
    /// 添加超链接，控制资源库显隐
    /// </summary>
    /// <param name="isShow"></param>
    private void OpenLinkDatabase(bool isShow)
    {
        if (isShow)
        {
            HashSet<string> list = new HashSet<string>();
            {
                foreach (var value in currentKnowledgepoints)
                {
                    if (!value.type.Equals(FileExtension.TXT))
                        list.Add(value.content);
                }
            }
            UIManager.Instance.OpenModuleUI<LinkDatabaseModule>(ParentPanel, transform.GetChild(0), new LinkDatabaseModule.LinkData(list));
        }
        else
        {
            UIManager.Instance.CloseModuleUI<LinkDatabaseModule>(ParentPanel);
        }
    }

    /// <summary>
    /// 修改超链接，控制资源库显隐
    /// </summary>
    /// <param name="isShow"></param>
    private void OpenLinkDatabaseToReplace(int id, string docType)
    {
        int activeIndex = 0;
        List<string> fileExtensions = new List<string>();
        switch (docType)
        {
            case FileExtension.PPT:
                activeIndex = 1;
                fileExtensions.Add("ppt");
                fileExtensions.Add("pptx");
                break;
            case FileExtension.IMG:
                activeIndex = 2;
                fileExtensions.Add("png");
                fileExtensions.Add("jpg");
                break;
            case FileExtension.DOC:
                activeIndex = 3;
                fileExtensions.Add("doc");
                fileExtensions.Add("docx");
                break;
            case FileExtension.XLS:
                activeIndex = 3;
                fileExtensions.Add("xls");
                fileExtensions.Add("xlsx");
                break;
            case FileExtension.PDF:
                activeIndex = 3;
                fileExtensions.Add("pdf");
                break;
            case FileExtension.MP4:
                activeIndex = 4;
                fileExtensions.Add("mp4");
                fileExtensions.Add("avi");
                break;
            case FileExtension.MP3:
                activeIndex = 4;
                fileExtensions.Add("mp3");
                fileExtensions.Add("ogg");
                fileExtensions.Add("wav");
                break;
        }

        HashSet<string> list = new HashSet<string>();
        {
            foreach (var value in currentKnowledgepoints)
            {
                if (!value.type.Equals(FileExtension.TXT))
                    list.Add(value.content);
            }
        }

        UIManager.Instance.OpenModuleUI<LinkDatabaseModule>(ParentPanel, transform.GetChild(0), new LinkDatabaseModule.LinkData(list, id, activeIndex, fileExtensions));
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
            case (ushort)IntegrationModuleEvent.AnimSelect:
                string animProp = ((MsgString)msg).arg;
                if (animController && animController.PlayableList.TryGetValue(animProp, out AnimController.AnimInfo animInfo))
                {
                    currentUUID = animInfo.UUID;
                    RefreshListByUUID();
                }
                break;
            case (ushort)SmallFlowModuleEvent.SelectFlow:
                MsgStringInt msgStringInt = ((MsgBrodcastOperate)msg).GetData<MsgStringInt>();
                if (smallFlowCtrl && smallFlowCtrl.flows != null)
                {
                    currentUUID = smallFlowCtrl.flows[msgStringInt.arg2].steps[0].ID;
                    RefreshListByUUID();
                }
                break;
            case (ushort)SmallFlowModuleEvent.SelectStep:
                MsgStringTuple<int, int, string> msgStringTuple = ((MsgBrodcastOperate)msg).GetData<MsgStringTuple<int, int, string>>();
                if (smallFlowCtrl && smallFlowCtrl.flows != null)
                {
                    currentUUID = smallFlowCtrl.flows[msgStringTuple.arg2.Item1].steps[msgStringTuple.arg2.Item2].ID;
                    RefreshListByUUID();
                }
                break;
            case (ushort)SmallFlowModuleEvent.Guide:
                MsgTuple<int,int,string> msgTuple = msg as MsgTuple<int, int, string>;
                if (smallFlowCtrl && smallFlowCtrl.flows != null)
                {
                    currentUUID = smallFlowCtrl.flows[msgTuple.arg.Item1].steps[msgTuple.arg.Item2].ID;
                    RefreshListByUUID();
                }
                break;
            case (ushort)SmallFlowModuleEvent.CompleteStep:
                if (!(msg is MsgIntInt))//跳步骤时消息不处理
                    return;
                if (smallFlowCtrl && smallFlowCtrl.flows != null)
                {
                    int nextStepIndex = ((MsgIntInt)msg).arg1 + 1;
                    if(nextStepIndex < smallFlowCtrl.flows[smallFlowCtrl.index_NowFlow].steps.Count)
                    {
                        currentUUID = smallFlowCtrl.flows[smallFlowCtrl.index_NowFlow].steps[nextStepIndex].ID;
                        RefreshListByUUID();
                    }
                    else if(smallFlowCtrl.index_NowFlow < smallFlowCtrl.flows.Length - 1)
                    {
                        //切换下一任务的第一个步骤
                        currentUUID = smallFlowCtrl.flows[smallFlowCtrl.index_NowFlow + 1].steps[0].ID;
                        RefreshListByUUID();
                    }
                }
                break;
        }
    }

    public override void Hide(UIData uiData = null, UnityAction callback = null)
    {
        ReorderableList.ClearElement();
        HideGo.SetActive(false);
        base.Hide(uiData, callback);
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        UIManager.Instance.CloseModuleUI<LinkDatabaseModule>(ParentPanel);

        SelectionModel selectionModel = ModelManager.Instance.modelRoot.GetComponentInChildren<SelectionModel>();
        if (selectionModel)
        {
            selectionModel.onSelectModel.RemoveListener(SelectionEvent);
            selectionModel.onClearSelection.RemoveListener(ClearSelectionEvent);
        }
        base.Close(uiData, callback);
    }

#region 接口请求
    /// <summary>
    /// 添加知识点
    /// </summary>
    /// <param name="title"></param>
    /// <param name="content"></param>
    /// <param name="successCallback"></param>
    /// <param name="failureCallback"></param>
    /// <param name="autoSave"></param>
    private void AddKnowledge(InputField title, InputField content)
    {
        if (title.text.Replace(" ", string.Empty).Length <= 0 || content.text.Replace(" ", string.Empty).Length <= 0)
        {
            UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData("请输入知识点内容", 120));
            return;
        }

        RequestManager.Instance.AddKnowledgepoint(new AddKnowledgepointRequest(GlobalInfo.currentWiki.id, currentUUID, title.text, content.text), data =>
            {
                if (!GlobalInfo.currentWikiKnowledges.ContainsKey(currentUUID))
                {
                    GlobalInfo.currentWikiKnowledges.Add(currentUUID, new List<Knowledgepoint>());
                }
                GlobalInfo.currentWikiKnowledges[currentUUID].Add(data);

                RefreshListByUUID(() =>
                {
                    SendMsg(new MsgStringInt((ushort)HierarchyEvent.UpdateAttachment, currentUUID, 1));
                    UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData("知识点添加成功", 120));
                });
            }, (code, error) =>
            {
                switch ((ResultCode.KnowledgeAdd)code)
                {
                    case ResultCode.KnowledgeAdd.InternetError:
                        UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData("网络异常，知识点添加失败", 120));
                        break;
                    case ResultCode.KnowledgeAdd.Service_Exception:
                        UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData("服务器异常，知识点添加失败", 120));
                        break;
                    case ResultCode.KnowledgeAdd.OverLength:
                        UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData("知识点标题或内容过长", 120));
                        break;
                    case ResultCode.KnowledgeAdd.Invalid_Parameter:
                    case ResultCode.KnowledgeAdd.Insert_Failed:
                    case ResultCode.KnowledgeAdd.Successful:
                    default:
                        break;
                }
            });
    }

    /// <summary>
    /// 修改知识点
    /// </summary>
    /// <param name="info"></param>
    /// <param name="title"></param>
    /// <param name="content"></param>
    /// <param name="successCallback"></param>
    /// <param name="failureCallback"></param>
    /// <param name="autoSave"></param>
    private void EditKnowledge(Knowledgepoint info, InputField title, InputField content)
    {
        if (title.text.Replace(" ", string.Empty).Length <= 0 || content.text.Replace(" ", string.Empty).Length <= 0)
        {
            UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData("请输入知识点内容", 120));
            return;
        }

        RequestManager.Instance.EditKnowledgepoint(new EditKnowledgepointRequest(info.id, GlobalInfo.currentWiki.id, info.uuid, title.text, content.text), (data) =>
        {
            //foreach (var item in GlobalInfo.currentWikiKnowledges[data.uuid])
            //{
            //    if (item.id == currentKnowledgeID)
            //    {
            //        item.title = title.text;
            //        item.content = content.text;
            //        break;
            //    }
            //}
            RefreshListByUUID(() => UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData("知识点修改成功", 120)));
        }, (code, failureMessage) =>
        {
            switch ((ResultCode.KnowledgeEditor)code)
            {
                case ResultCode.KnowledgeEditor.InternetError:
                    foreach (var item in GlobalInfo.currentWikiKnowledges[info.uuid])
                    {
                        if (item.id == currentKnowledgeID)
                        {
                            title.text = item.title;
                            content.text = item.content;

                            UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData("网络异常，知识点修改失败", 120));
                            Log.Error($"修改知识点失败！原因为:{failureMessage}");
                            break;
                        }
                    }
                    break;
                case ResultCode.KnowledgeEditor.Service_Exception:
                    foreach (var item in GlobalInfo.currentWikiKnowledges[info.uuid])
                    {
                        if (item.id == currentKnowledgeID)
                        {
                            title.text = item.title;
                            content.text = item.content;

                            UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData("服务器异常，知识点修改失败", 120));
                            Log.Error($"修改知识点失败！原因为:{failureMessage}");
                            break;
                        }
                    }
                    break;
                case ResultCode.KnowledgeEditor.Teach_ResourceNoExist:
                    Dictionary<string, PopupButtonData> popupButtonData = new Dictionary<string, PopupButtonData>();
                    popupButtonData.Add("确认", new PopupButtonData(() =>
                    {
                        currentKnowledgeID = -1;
                        //todo 
                        //Enter.onClick.Invoke();
                    }, true));
                    popupButtonData.Add("取消", new PopupButtonData(() =>
                    {
                        //todo
                    }, false));
                    UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("错误", "修改知识点失败,因为知识点已经被删除，是否要新建一个知识点？", popupButtonData));
                    break;
                case ResultCode.KnowledgeEditor.Update_Failed:
                case ResultCode.KnowledgeEditor.Invalid_Parameter:
                case ResultCode.KnowledgeEditor.Successful:
                default:
                    break;
            }

        });
    }

    /// <summary>
    /// 删除知识点
    /// </summary>
    private void DeleteKnowledge(Transform item, Knowledgepoint info)
    {
        currentKnowledgeID = info.id;
        string knowledgeType = info.type.Equals(FileExtension.TXT) ? "知识点" : "超链接";
        var index = item.GetSiblingIndex();

        Dictionary<string, PopupButtonData> popupButtonData = new Dictionary<string, PopupButtonData>();
        popupButtonData.Add("确认", new PopupButtonData(() =>
        {
            RequestManager.Instance.DeleteKnowledgepoint(currentKnowledgeID, () =>
            {
                DeleteKnowledgeAnim(ReorderableList.Content.GetComponent<RectTransform>(), index, () =>
                {
                    Knowledgepoint removed;
                    if (GlobalInfo.currentWikiKnowledges.ContainsKey(info.uuid))
                    {
                        removed = GlobalInfo.currentWikiKnowledges[info.uuid].Find(k => k.id == currentKnowledgeID);
                        if (removed != null)
                            GlobalInfo.currentWikiKnowledges[info.uuid].Remove(removed);
                    }
                    removed = currentKnowledgepoints.Find(k => k.id == currentKnowledgeID);
                    if (removed != null)
                        currentKnowledgepoints.Remove(removed);

                    SendMsg(new MsgStringInt((ushort)HierarchyEvent.UpdateAttachment, currentUUID, currentKnowledgepoints.Count));
                    HideGo.SetActive(currentKnowledgepoints.Count == 0);
                    UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData("已删除", 120));

                    ////从最小化列表中移除
                    //SendMsg(new MsgInt((ushort)MinimizeLinkEvent.Remove, info.id));
                });
            }, (code, failureMessage) =>
            {
                switch ((ResultCode.KnowledgeDelet)code)
                {
                    case ResultCode.KnowledgeDelet.InternetError:
                        UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData($"网络异常，{knowledgeType}删除失败", 120));
                        break;
                    case ResultCode.KnowledgeDelet.Service_Exception:
                        UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData($"服务器异常，{knowledgeType}删除失败", 120));
                        break;
                    case ResultCode.KnowledgeDelet.Teach_ResourceNoExist:
                        UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData($"{knowledgeType}不存在或已删除", 120));
                        break;
                    case ResultCode.KnowledgeDelet.Delete_Failed:
                    case ResultCode.KnowledgeDelet.Invalid_Parameter:
                    case ResultCode.KnowledgeDelet.Successful:
                    default:
                        break;
                }
            });
        }, true));
        popupButtonData.Add("取消", new PopupButtonData(null, false));
        UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", $"确认删除该{knowledgeType}？", popupButtonData));
    }

    /// <summary>
    /// 批量添加超链接（知识点）
    /// </summary>
    /// <param name="ids">课件资源id</param>
    public void AddLinks(List<int> ids)
    {
        RequestManager.Instance.AddBatchKnowledgepoint(new AddBatchKnowledgepointRequest(GlobalInfo.currentWiki.id, currentUUID, ids), () =>
        {
            RefreshListByUUID(() =>
            {
                SendMsg(new MsgStringInt((ushort)HierarchyEvent.UpdateAttachment, currentUUID, 1));
                UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData("超链接添加成功", 120));
            });
        }, (code, error) =>
        {
            switch ((ResultCode.KnowledgeAdd)code)
            {
                case ResultCode.KnowledgeAdd.InternetError:
                    UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData("网络异常，超链接添加失败", 120));
                    break;
                case ResultCode.KnowledgeAdd.Service_Exception:
                    UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData("服务器异常，超链接添加失败", 120));
                    break;
                case ResultCode.KnowledgeAdd.OverLength:
                    UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData("超链接内容过长", 120));
                    break;
                case ResultCode.KnowledgeAdd.Invalid_Parameter:
                case ResultCode.KnowledgeAdd.Insert_Failed:
                case ResultCode.KnowledgeAdd.Successful:
                default:
                    break;
            }
        });
    }

    /// <summary>
    /// 替换超链接（知识点）
    /// </summary>
    /// <param name="knowledgePointId">修改的知识点id</param>
    /// <param name="resourcId">课件资源id</param>
    public void ReplaceLink(int knowledgePointId, int resourcId)
    {
        RequestManager.Instance.ReplaceKnowledgepoint(new ReplaceKnowledgepointRequest(knowledgePointId, resourcId), (info) =>
        {
            RefreshListByUUID(() =>
            {
                UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData("超链接修改成功", 120));
            });
        }, (code, error) =>
        {
            switch ((ResultCode.KnowledgeAdd)code)
            {
                case ResultCode.KnowledgeAdd.InternetError:
                    UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData("网络异常，超链接修改失败", 120));
                    break;
                case ResultCode.KnowledgeAdd.Service_Exception:
                    UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData("服务器异常，超链接修改失败", 120));
                    break;
                case ResultCode.KnowledgeAdd.OverLength:
                    UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData("超链接内容过长", 120));
                    break;
                case ResultCode.KnowledgeAdd.Invalid_Parameter:
                case ResultCode.KnowledgeAdd.Insert_Failed:
                case ResultCode.KnowledgeAdd.Successful:
                default:
                    break;
            }
        });
    }
    #endregion

    #region 动效
    protected override float joinAnimePlayTime => 0.3f;
    protected override float exitAnimePlayTime => 0.2f;

    public override void JoinAnim(UnityAction callback)
    {
        JoinSequence.Join(background.DOAnchorPos3DX(0, JoinAnimePlayTime));
        base.JoinAnim(callback);
    }

    public override void ExitAnim(UnityAction callback)
    {
        ExitSequence.Join(background.DOAnchorPos3DX(background.sizeDelta.x, ExitAnimePlayTime));
        base.ExitAnim(callback);
    }

    /// <summary>
    /// 添加知识点动效
    /// </summary>
    /// <param name="item"></param>
    private void AddNewKnowledgeItem(Transform item)
    {
        ReorderableList.Content.GetComponent<VerticalLayoutGroup>().childControlHeight = false;

        RectTransform rectTransform = item.GetComponent<RectTransform>();
        float targetSizeDeltaY = rectTransform.sizeDelta.y;
        rectTransform.pivot = new Vector2(0.5f, 0f);
        CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>();
        ContentSizeFitter contentSizeFitter = item.GetComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        item.gameObject.SetActive(true);

        Sequence sequence = DOTween.Sequence();
        sequence.Join(DOTween.To(() => new Vector2(rectTransform.sizeDelta.x, 0f), (value) => rectTransform.sizeDelta = value, new Vector2(rectTransform.sizeDelta.x, targetSizeDeltaY), JoinAnimePlayTime));
        sequence.Join(DOTween.To(() => 0f, (value) => canvasGroup.alpha = value, 1f, JoinAnimePlayTime));
        sequence.OnComplete(() =>
        {
            rectTransform.pivot = 0.5f * Vector2.one;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ReorderableList.Content.GetComponent<VerticalLayoutGroup>().childControlHeight = true;
        });
    }

    /// <summary>
    /// 取消知识点编辑动效
    /// </summary>
    /// <param name="item"></param>
    private void RemoveNewKnowledgeItem(Transform item)
    {
        ReorderableList.Content.GetComponent<VerticalLayoutGroup>().childControlHeight = false;

        RectTransform rectTransform = item.GetComponent<RectTransform>();
        rectTransform.pivot = new Vector2(0.5f, 0f);
        CanvasGroup canvasGroup = item.GetComponent<CanvasGroup>();
        ContentSizeFitter contentSizeFitter = item.GetComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        Sequence sequence = DOTween.Sequence();
        sequence.Join(rectTransform.DOSizeDelta(new Vector2(rectTransform.sizeDelta.x, 0), JoinAnimePlayTime));
        sequence.Join(DOTween.To(() => 1f, (value) => canvasGroup.alpha = value, 0f, JoinAnimePlayTime));
        sequence.OnComplete(() =>
        {
            Destroy(item.gameObject);
            ReorderableList.Content.GetComponent<VerticalLayoutGroup>().childControlHeight = true;
        });
    }

#if UNITY_STANDALONE
    /// <summary>
    /// 打开“更多”面板动效
    /// </summary>
    private void OpenMorePanel()
    {
        MorePanel.SetActive(true);
        Sequence sequence = DOTween.Sequence();
        sequence.Join(MoreBackground.DOScale(Vector3.one, JoinAnimePlayTime));
        sequence.Join(MoreCanvasGroup.DOFade(1f, JoinAnimePlayTime));
    }

    /// <summary>
    /// 关闭“更多”面板动效
    /// </summary>
    private void CloseMorePanel(float duration = 0.1f)
    {
        Sequence sequence = DOTween.Sequence();
        sequence.Join(MoreBackground.DOScale(0.2f * Vector3.one, duration));
        sequence.Join(MoreCanvasGroup.DOFade(0f, duration));
        sequence.OnComplete(() => MorePanel.SetActive(false));
    }
#endif

    /// <summary>
    /// 删除知识点动效
    /// </summary>
    /// <param name="index"></param>
    private void DeleteKnowledgeAnim(RectTransform content, int index, UnityAction callback = null)
    {
        if (content.childCount == 0 || content.childCount <= index || index < 0) return;

        SendMsg(new MsgBase((ushort)UIAnimEvent.ShowAnimMask));

        VerticalLayoutGroup verticalLayoutGroup = content.GetComponent<VerticalLayoutGroup>();
        verticalLayoutGroup.childControlHeight = false;

        Transform item = content.GetChild(index);
        RectTransform rectTransform = item.GetComponent<RectTransform>();

        Sequence sequence = DOTween.Sequence();
        //被删除知识点整体缩小
        sequence.Join(rectTransform.DOScale(0.7f * Vector3.one, 0.4f));
        //延时0.2秒 下方知识点上移
        if(rectTransform.TryGetComponent(out ContentSizeFitter contentSizeFitter))
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        sequence.Insert(0.2f, rectTransform.DOSizeDelta(new Vector2(rectTransform.sizeDelta.x, -verticalLayoutGroup.spacing), 0.2f));
        //延时0.3秒 被删除知识点透明度
        CanvasGroup canvasGroup = item.AutoComponent<CanvasGroup>();
        canvasGroup.ignoreParentGroups = true;
        sequence.Insert(0.3f, DOTween.To(() => 1f, (value) => canvasGroup.alpha = value, 0f, 0.1f));

        sequence.OnComplete(() =>
        {
            DestroyImmediate(content.GetChild(index).gameObject);
            verticalLayoutGroup.childControlHeight = true;
            SendMsg(new MsgBase((ushort)UIAnimEvent.HideAnimMask));
            callback();
        });
    }

    /// <summary>
    /// 折叠面板，打开资料库时调用
    /// </summary>
    public void Fold()
    {
#if UNITY_STANDALONE
        Sequence sequence = DOTween.Sequence();
        sequence.Join(DOTween.To(() => ContentMask.offsetMin, (value) => ContentMask.offsetMin = value, new Vector2(ContentMask.offsetMin.x, 1026f), JoinAnimePlayTime));
        sequence.Join(ContentCanvasGroup.DOFade(0f, JoinAnimePlayTime));
#endif
    }

    /// <summary>
    /// 展开面板，关闭资料库时调用
    /// </summary>
    public void Unfold()
    {
#if UNITY_STANDALONE
        Sequence sequence = DOTween.Sequence();
        sequence.AppendInterval(0.2f);
        sequence.Append(DOTween.To(() => ContentMask.offsetMin, (value) => ContentMask.offsetMin = value, new Vector2(ContentMask.offsetMin.x, 0), JoinAnimePlayTime));
        sequence.Join(ContentCanvasGroup.DOFade(1f, JoinAnimePlayTime));
#endif
    }

    /// <summary>
    /// 课件资料列表悬浮动效 
    /// 视频、图片放大10%; 其他底板透明度变化
    /// </summary>
    public void KnowledgeItemOnHover(Transform item, string type)
    {
        switch (type)
        {
#if UNITY_STANDALONE
            case "TXT":
                UIFade txtFade = item.AutoComponent<UIFade>();
                txtFade.Init(item.GetChild(0).GetComponent<Image>(), 0.16f, 0.08f);
                break;
#endif
            case FileExtension.MP4:
            case FileExtension.IMG:
                UIScale uiScale = item.AutoComponent<UIScale>();
                uiScale.Init(item.GetChild(0), 1.1f, 1f);
                break;
            default:
#if UNITY_STANDALONE
                UIFade uiFade = item.AutoComponent<UIFade>();
                uiFade.Init(item.GetComponent<Image>(), 0.16f, 0.08f);
#else
                UIFade uiFade = item.AutoComponent<UIFade>();
                uiFade.Init(item.GetChild(0).GetComponent<Image>(), 1f, 0f);
#endif
                break;
        }
    }
#endregion
}