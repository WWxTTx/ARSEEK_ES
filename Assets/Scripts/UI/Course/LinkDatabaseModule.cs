using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;
using Courseware = UnityFramework.Runtime.RequestData.Courseware;


/// <summary>
/// 资料库模块
/// </summary>
public partial class LinkDatabaseModule : UIModuleBase
{
    public Sprite[] FileSprites;

    private KnowledgeModule knowledgeModule;

    private GetFirstVideoImage VideoPreviewGetter;

    public class LinkData : UIData
    {
        public int replacedId;
        /// <summary>
        /// 限定标签Index
        /// </summary>
        public int activeTagIndex;
        /// <summary>
        /// 二次筛选文件类型
        /// </summary>
        public List<string> fileExtension;

        public HashSet<string> list;

        public LinkData(HashSet<string> list)
        {
            this.list = list;
        }

        public LinkData(HashSet<string> list, int replaceId, int activeTagIndex, List<string> fileExtension)
        {
            this.list = list;
            this.replacedId = replaceId;
            this.activeTagIndex = activeTagIndex;
            this.fileExtension = fileExtension;
        }
    }

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform Background;
    private Toggle ALLTog;
    private Toggle PPTTog;
    private Toggle IMAGETog;
    private Toggle WORDTog;
    private Toggle VIDEOTog;
    private Dictionary<int, Toggle> ToggleIndex = new Dictionary<int, Toggle>(5);

    private InputField Search;

    private Transform content;

    private GameObject LinkHide;

    /// <summary>
    /// 勾选项
    /// </summary>
    private List<int> send = new List<int>();

    private List<Courseware> allList;

    private Dictionary<Toggle, List<Tuple<GameObject, string>>> typeItems = new Dictionary<Toggle, List<Tuple<GameObject, string>>>();

    /// <summary>
    /// 图片序列
    /// </summary>
    private List<LoadImageData> imageList = new List<LoadImageData>();

    private Dictionary<int, string> videoUrls = new Dictionary<int, string>();

    private Dictionary<int, Transform> videoItems = new Dictionary<int, Transform>();

    private HashSet<string> list;

    private Sequence loadingSequence;

    private Toggle activeToggle;

    private LinkData linkData;

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

#if UNITY_ANDROID || UNITY_IOS
        AddMsg(new ushort[]
        {
            (ushort)ARModuleEvent.Tracking,
        });
#endif
        canvasGroup = GetComponent<CanvasGroup>();
        Background = transform.GetComponentByChildName<RectTransform>("BackGround");
        ALLTog = transform.GetComponentByChildName<Toggle>("ALL");
        PPTTog = transform.GetComponentByChildName<Toggle>("PPT");
        IMAGETog = transform.GetComponentByChildName<Toggle>("IMAGE");
        WORDTog = transform.GetComponentByChildName<Toggle>("WORD");
        VIDEOTog = transform.GetComponentByChildName<Toggle>("VIDEO");
        ToggleIndex = new Dictionary<int, Toggle>()
        {
            {0, ALLTog },
            {1, PPTTog },
            {2, IMAGETog },
            {3, WORDTog },
            {4, VIDEOTog },
        };
        LinkHide = this.FindChildByName("LinkHide").gameObject;
        Search = transform.GetComponentByChildName<InputField>("Search");
        Search.onValueChanged.AddListener((value) =>
        {
            RefreshLinkDetail(value.Trim());
        });

        content = transform.FindChildByName("LinkContent");
        knowledgeModule = ParentPanel.GetComponentInChildren<KnowledgeModule>();
        VideoPreviewGetter = knowledgeModule.GetComponent<GetFirstVideoImage>();

        transform.GetComponentByChildName<Button>("LinkEnter").onClick.AddListener(() =>
        {
            if (uploadingCount > 0)
            {
                UploadingPopup();
                return;
            }
            if (send.Count <= 0)
            {
                UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData("未选中任何资源！", 120));
                return;
            }
            if (linkData.replacedId != 0)
            {
                knowledgeModule.ReplaceLink(linkData.replacedId, send[0]);
            }
            else
            {
                knowledgeModule.AddLinks(send);
            }
            UIManager.Instance.CloseModuleUI<LinkDatabaseModule>(ParentPanel);
            SendMsg(new MsgBool((ushort)CoursePanelEvent.EditMode, false));
        });

        transform.GetComponentByChildName<Button>("LinkCancel").onClick.AddListener(() =>
        {
            if (uploadingCount > 0)
            {
                UploadingPopup();
                return;
            }
            UIManager.Instance.CloseModuleUI<LinkDatabaseModule>(ParentPanel);
            SendMsg(new MsgBool((ushort)CoursePanelEvent.EditMode, false));
        });

        transform.GetComponentByChildName<Button>("LinkDelete").onClick.AddListener(() =>
        {
            if (uploadingCount > 0)
            {
                UploadingPopup();
                return;
            }
            if (send.Count <= 0)
            {
                UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData("未选中任何资源！", 120));
                return;
            }

            OnPopupOpen();
            Dictionary<string, PopupButtonData> popupDic2 = new Dictionary<string, PopupButtonData>();
            popupDic2.Add("取消", new PopupButtonData(OnPopupClose));
            popupDic2.Add("确定", new PopupButtonData(() =>
            {
                DeleteCoursewares();
                OnPopupClose();
            }, true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "确定要删除所选课件吗？", popupDic2, OnPopupClose));
        });

        InitUploadUIEvents();

        list = new HashSet<string>();
        if (uiData != null)
        {
            linkData = uiData as LinkData;
            list = linkData.list;
        }

        allList = new List<Courseware>();

        GetResources(page, () =>
        {
            InitList(content, allList, send);
            InitToggle();
        });
    }

    /// <summary>
    /// 刷新课件列表
    /// </summary>
    private void RefreshResources()
    {
        allList.Clear();

        GetResources(page, () =>
        {
            InitList(content, allList, send);
            RefreshLinkDetail(Search.text.Trim());
        });
    }

    /// <summary>
    /// 删除课件资源
    /// </summary>
    private void DeleteCoursewares()
    {
        RequestManager.Instance.DeleteCoursewareResourceBatch(send, () =>
        {
            allList.RemoveAll(c => send.Contains(c.id));
            List<GameObject> deleteItems = new List<GameObject>(send.Count);
            foreach (Transform trans in content)
            {
                if (trans.GetComponentInChildren<Toggle>().isOn)
                    deleteItems.Add(trans.gameObject);
            }
            for (int i = 0; i < deleteItems.Count; i++)
            {
                DestroyImmediate(deleteItems[i]);
            }
            UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData("课件资源删除成功", 120));
            send.Clear();
        }, (code, error) =>
        {
            UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform.GetChild(0), new LocalTipModule.ModuleData("课件资源删除失败", 120));
            send.Clear();
        });
    }

    public override void Show(UIData uiData = null)
    {
        base.Show(uiData);

        InitSequence();
        loadingSequence.Play();

        if (GlobalInfo.InEditMode)
        {
            canvas = transform.AutoComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 1;
            transform.AutoComponent<GraphicRaycaster>();
        }
    }

    private void InitToggle()
    {
        ALLTog.onValueChanged.AddListener(isOn =>
        {
            if (isOn)
            {
                activeToggle = ALLTog;
                fileType = FormTool.FileType.CourseWare;
                RefreshLinkDetail(Search.text.Trim());
            }
        });

        PPTTog.onValueChanged.AddListener(isOn =>
        {
            if (isOn)
            {
                activeToggle = PPTTog;
                fileType = FormTool.FileType.PPT;
                RefreshLinkDetail(Search.text.Trim());
            }
        });

        IMAGETog.onValueChanged.AddListener(isOn =>
        {
            if (isOn)
            {
                activeToggle = IMAGETog;
                fileType = FormTool.FileType.Texture;
                RefreshLinkDetail(Search.text.Trim());
            }
        });

        WORDTog.onValueChanged.AddListener(isOn =>
        {
            if (isOn)
            {
                activeToggle = WORDTog;
                fileType = FormTool.FileType.DOC;
                RefreshLinkDetail(Search.text.Trim());
            }
        });

        VIDEOTog.onValueChanged.AddListener(isOn =>
        {
            if (isOn)
            {
                activeToggle = VIDEOTog;
                fileType = FormTool.FileType.Media;
                RefreshLinkDetail(Search.text.Trim());
            }
        });

        loadingSequence.Kill();
        transform.FindChildByName("Loading").gameObject.SetActive(false);

        foreach (KeyValuePair<int, Toggle> tog in ToggleIndex)
        {
            tog.Value.isOn = tog.Key == linkData.activeTagIndex;
            if (linkData.activeTagIndex != 0)
                tog.Value.interactable = tog.Key == linkData.activeTagIndex;
        }

        ALLTog.group.allowSwitchOff = false;

        TagOnHover(ALLTog);
        TagOnHover(PPTTog);
        TagOnHover(IMAGETog);
        TagOnHover(WORDTog);
        TagOnHover(VIDEOTog);
    }

    private int page = 1;
    private int pageSize = 100;

    /// <summary>
    /// 获取课件资源列表
    /// </summary>
    /// <param name="page"></param>
    /// <param name="callback"></param>
    private void GetResources(int page, UnityAction callback)
    {
        int totalPage;
        RequestManager.Instance.GetCoursewareList(GlobalInfo.currentWiki.id, page, pageSize, (resources) =>
        {
            totalPage = Mathf.CeilToInt((float)resources.paging.total / resources.paging.pagesize);

            AddData(resources.records);

            if (page < totalPage)
            {
                page++;
                GetResources(page, callback);
            }
            else
            {
                callback?.Invoke();
            }
        }, (code, msg) =>
        {
            callback?.Invoke();
        });
    }
    /// <summary>
    /// 添加课件资源
    /// </summary>
    /// <param name="coursewares"></param>
    private void AddData(List<Courseware> coursewares)
    {
        if (coursewares == null || coursewares.Count == 0)
            return;

        for (int i = 0; i < coursewares.Count; i++)
        {
            if (list != null && list.Contains(coursewares[i].filePath))
                continue;

            if (linkData.fileExtension != null && !string.IsNullOrEmpty(System.IO.Path.GetExtension(coursewares[i].filePath))
                && !linkData.fileExtension.Contains(System.IO.Path.GetExtension(coursewares[i].filePath).Substring(1)))
            {
                continue;
            }

            allList.Add(coursewares[i]);
        }
    }

    /// <summary>
    /// 初始化资源库列表
    /// </summary>
    /// <param name="content"></param>
    /// <param name="data"></param>
    /// <param name="send"></param>
    private void InitList(Transform content, List<Courseware> data, List<int> send)
    {
        videoUrls.Clear();
        videoItems.Clear();
        typeItems.Clear();

        content.UpdateItemsView(data, (item, info) =>
        {
            item.GetComponentByChildName<Text>("Title").text = System.IO.Path.GetFileNameWithoutExtension(info.fileName);

            Toggle toggle = item.GetComponentInChildren<Toggle>();
            toggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    if (!send.Exists(value => value == info.id))
                        send.Add(info.id);
                }
                else
                {
                    send.Remove(info.id);
                }
            });
            if (linkData.activeTagIndex == 0)
                toggle.group = null;

            switch (info.type)
            {
                case FileExtension.PPT:
                case FileExtension.DOC:
                    SetDocTypeIcon(info.docType, item.GetComponentByChildName<Image>("Type"));
                    item.GetComponentInChildren<Button>().onClick.AddListener(() =>
                    {
                        MsgHyperlink msgDoc = new MsgHyperlink((ushort)HyperLinkEvent.HyperlinkDOC, info);
                        SendMsg(new MsgBrodcastOperate(msgDoc.msgId, JsonTool.Serializable(msgDoc)));
                    });
                    AddTypeItem(info.type.Equals(FileExtension.PPT) ? PPTTog : WORDTog, item.gameObject);
                    break;
                case FileExtension.IMG:
                    item.FindChildByName(info.type)?.gameObject.SetActive(true);
                    RawImage image = item.GetComponentByChildName<RawImage>(info.type);

                    lock (imageList)
                    {
                        imageList.Add(new LoadImageData()
                        {
                            id = info.id.ToString(),
                            filePath = ResManager.Instance.OSSDownLoadPath + info.filePath,
                            call = texture =>
                            {
                                if (texture == null)
                                    return;
                                if (image)
                                {
                                    image.texture = texture;
                                    image.SetAlpha(1);
                                }
                            }
                        });
                    }
                    item.GetComponentInChildren<Button>().onClick.AddListener(() =>
                    {
                        MsgHyperlink msgImg = new MsgHyperlink((ushort)HyperLinkEvent.HyperlinkImage, info);
                        SendMsg(new MsgBrodcastOperate(msgImg.msgId, JsonTool.Serializable(msgImg)));
                    });
                    AddTypeItem(IMAGETog, item.gameObject);
                    break;
                case FileExtension.ANV:
                    switch (info.docType)
                    {
                        case FileExtension.MP3:
                            SetDocTypeIcon(info.docType, item.GetComponentByChildName<Image>("Type"));
                            item.GetComponentInChildren<Button>().onClick.AddListener(() =>
                            {
                                MsgHyperlink msgAudio = new MsgHyperlink((ushort)HyperLinkEvent.HyperlinkAudio, info);
                                SendMsg(new MsgBrodcastOperate(msgAudio.msgId, JsonTool.Serializable(msgAudio)));
                            });
                            break;
                        default:
                            item.FindChildByName(info.type)?.gameObject.SetActive(true);
                            item.GetComponentInChildren<Button>().onClick.AddListener(() =>
                            {
                                MsgHyperlink msgVideo = new MsgHyperlink((ushort)HyperLinkEvent.HyperlinkVideo, info);
                                SendMsg(new MsgBrodcastOperate(msgVideo.msgId, JsonTool.Serializable(msgVideo)));
                            });
                            if (!videoUrls.ContainsKey(info.id))
                            {
                                videoUrls.Add(info.id, ResManager.Instance.OSSDownLoadPath + info.filePath);
                                videoItems.Add(info.id, item);
                            }
                            break;
                    }
                    AddTypeItem(VIDEOTog, item.gameObject);
                    break;
            }

            if (send.Exists(value => value == info.id))
            {
                item.GetComponentInChildren<Toggle>().SetIsOnWithoutNotify(true);
            }
        });

        if (videoUrls.Count > 0)
        {
            VideoPreviewGetter.LoadVideoPreviews2(videoUrls, (data) =>
            {
                if (videoItems.TryGetValue(data.id, out Transform vt))
                {
                    if (vt)
                    {
                        RawImage rawImage = vt.GetComponentByChildName<RawImage>("A&V");
                        rawImage.texture = data.texture;
                        rawImage.SetAlpha(1);
                        vt.GetComponentByChildName<Text>("Length").text = data.length;
                    }
                }
            });
        }
        LinkHide.gameObject.SetActive(data.Count == 0);
    }

    private void SetDocTypeIcon(string docType, Image image)
    {
        image.gameObject.SetActive(true);
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
            case FileExtension.MP3:
                image.sprite = FileSprites[4];
                break;
        }
    }

    private void AddTypeItem(Toggle tog, GameObject item)
    {
        if (typeItems.ContainsKey(tog))
            typeItems[tog].Add(new Tuple<GameObject, string>(item, item.transform.GetComponentByChildName<Text>("Title").text));
        else
            typeItems.Add(tog, new List<Tuple<GameObject, string>>() { new Tuple<GameObject, string>(item, item.transform.GetComponentByChildName<Text>("Title").text) });
    }

    /// <summary>
    /// 分类显示
    /// </summary>
    /// <param name="toggle"></param>
    private void RefreshLinkDetail(string keyword)
    {
        foreach (Transform child in content)
            child.gameObject.SetActive(false);

        int count = 0;
        if (activeToggle == ALLTog)
        {
            foreach (KeyValuePair<Toggle, List<Tuple<GameObject, string>>> toggle in typeItems)
            {
                foreach (Tuple<GameObject, string> go in toggle.Value)
                {
                    if (go.Item1 != null && go.Item2.Contains(keyword))
                    {
                        go.Item1.SetActive(true);
                        count++;
                    }
                }
            }
        }
        else
        {
            if (typeItems.TryGetValue(activeToggle, out List<Tuple<GameObject, string>> items))
            {
                foreach (Tuple<GameObject, string> go in items)
                {
                    if (go.Item1 != null && go.Item2.Contains(keyword))
                    {
                        go.Item1.SetActive(true);
                        count++;
                    }
                }
            }
        }
        LinkHide.gameObject.SetActive(count == 0);
    }

    /// <summary>
    /// 图片的下载限制
    /// </summary>
    private const int imageDownloadMax = 1;
    /// <summary>
    /// 当前图片下载数
    /// </summary>
    private int currentImageDownload = 0;

    private void Update()
    {
        if (canvasGroup.alpha >= 1 && imageList.Count > 0 && currentImageDownload < imageDownloadMax)
        {
            currentImageDownload++;

            lock (imageList)
            {
                var tempData = imageList[0];
                tempData.Add(() =>
                {
                    currentImageDownload--;
                });

                ResManager.Instance.LoadKnowledgepointImage(tempData.id, tempData.filePath, tempData.call);
                imageList.RemoveAt(0);
            }
        }
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);

        switch (msg.msgId)
        {
            case (ushort)ARModuleEvent.Tracking:
                bool tracking = ((MsgBool)msg).arg1;
                canvasGroup.blocksRaycasts = !tracking;
                canvasGroup.DOFade(tracking ? 0 : 1, 0.3f);
                break;
            case (ushort)CoursePanelEvent.Option:
                bool open = ((MsgBool)msg).arg1;
                if (open) 
                    OnPopupOpen();
                else 
                    OnPopupClose();
                break;
        }
    }

    private void InitSequence()
    {
        float playtime = 0.65f;
        var Action = transform.FindChildByName("Action");

        Tweener t1 = Action.DOLocalRotate(new Vector3(0, 0, 25f), playtime);
        Tweener t2 = Action.DOLocalRotate(new Vector3(0, 0, 155f), playtime).SetEase(Ease.Linear);

        loadingSequence = DOTween.Sequence();
        loadingSequence.Append(t1);
        loadingSequence.Insert(0.1f + t1.Duration(), t2);

        foreach (Transform child in Action)
        {
            Transform r = child.FindChildByName("R");
            loadingSequence.Insert(0.1f + t1.Duration(), r.DOLocalRotate(new Vector3(0, 0, 76), playtime).SetLoops(2, LoopType.Yoyo)).SetEase(Ease.Linear);
        }

        loadingSequence.SetLoops(-1);
        loadingSequence.Pause();
    }

    private struct LoadImageData
    {
        public string id;
        public string filePath;
        public UnityAction<Texture2D> call;
        public void Add(UnityAction newAction)
        {
            call += result =>
            {
                newAction.Invoke();
            };
        }
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        if (knowledgeModule)
            knowledgeModule.Unfold();
        base.Close(uiData, callback);
    }

    #region 动效
    protected override float joinAnimePlayTime => 0.3f;
    protected override float exitAnimePlayTime => 0.3f;

    public override void JoinAnim(UnityAction callback)
    {
        JoinSequence.AppendCallback(() =>
        {
            if (knowledgeModule)
                knowledgeModule.Fold();
        });
        JoinSequence.Join(DOTween.To(() => new Vector2(Background.offsetMin.x, 1038f), (value) => Background.offsetMin = value, new Vector2(Background.offsetMin.x, 0f), JoinAnimePlayTime));
        JoinSequence.Join(DOTween.To(() => 0f, (value) => canvasGroup.alpha = value, 1f, JoinAnimePlayTime));
        JoinSequence.AppendCallback(() =>
        {
            OpenBtn.gameObject.SetActive(true);
            AddFileBtn.gameObject.SetActive(true);
        });
        base.JoinAnim(callback);
    }

    public override void ExitAnim(UnityAction callback)
    {
        ExitSequence.AppendCallback(() =>
        {
            OpenBtn.gameObject.SetActive(false);
            AddFileBtn.gameObject.SetActive(false);
        });
        ExitSequence.Join(DOTween.To(() => Background.offsetMin, (value) => Background.offsetMin = value, new Vector2(Background.offsetMin.x, 1038f), ExitAnimePlayTime));
        ExitSequence.Join(DOTween.To(() => 1f, (value) => canvasGroup.alpha = value, 0f, ExitAnimePlayTime));
        base.ExitAnim(callback);
    }

    public delegate bool OnPointerExitCondition<T>(T t);

    /// <summary>
    /// 标签悬浮动效 文本透明度默认0.5 悬浮1
    /// </summary>
    public void TagOnHover(Component item)
    {
        UIFade uiFade = item.AutoComponent<UIFade>();
        uiFade.OnPointerEnterCondition = () => item.GetComponent<Toggle>().isOn;
        uiFade.OnPointerExitCondition = () => item.GetComponent<Toggle>().isOn;
        uiFade.Init(item.GetComponentInChildren<Text>(), 0.68f, 0.5f);
    }

    private void UploadingPopup()
    {
        OnPopupOpen();
        Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
        popupDic.Add("确认", new PopupButtonData(OnPopupClose, true));
        UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "文件正在上传中，请稍后", popupDic, OnPopupClose));
    }

    private void OnPopupOpen()
    {
        //canvasGroup.alpha = 0.5f;
        //canvasGroup.interactable = false;
        if (canvas)
            canvas.overrideSorting = false;
    }
    private void OnPopupClose()
    {
        //canvasGroup.alpha = 1f;
        //canvasGroup.interactable = true;
        if (canvas)
            canvas.overrideSorting = GlobalInfo.InEditMode;
    }
    #endregion
}