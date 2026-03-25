using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;


public class ABPanelInfo : UIData
{
    public int ID;
    public string PanelName;

    public ABPanelInfo() { }
    public ABPanelInfo(int id, string panelName)
    {
        ID = id;
        PanelName = panelName;
    }
}

/// <summary>
/// 一点课课程
/// </summary>
public class OPLCoursePanel : HoverHintPanel
{
    protected override bool CanLogout { get { return true; } }
    public override bool canOpenOption => true;

    protected ABPanelInfo data;

    #region UI
    protected CanvasGroup RootCanvasGroup;
    /// <summary>
    /// 空白区域检测
    /// </summary>
    [HideInInspector]
    public Button EmptyClick;
    /// <summary>
    /// 顶部导航栏
    /// </summary>
    protected RectTransform TopNavigation;
    protected Text Title;
    /// <summary>
    /// 任务说明
    /// </summary>
    protected Toggle DescToggle;
    protected Transform DescPanel;
    protected Text DescText;
    /// <summary>
    /// 左侧边栏
    /// </summary>
    protected CourseSideBar CourseSideBar;
    protected RectTransform SideBar;
    /// <summary>
    /// 左侧中间和上方按钮父节点
    /// </summary>
    protected RectTransform BtnsContent;
    protected Image BtnsContentMask;
    /// <summary>
    /// 左侧边栏底部按钮
    /// </summary>
    protected RectTransform BottomBtns;
//三星水电的都是带人形的 暂时不支持vr
//#if UNITY_ANDROID || UNITY_IOS
//    protected Toggle ARTog;
//#endif
    /// <summary>
    /// 开始协同
    /// </summary>
    protected Button StartLive;
    /// <summary>
    /// 画笔
    /// </summary>
    protected Toggle Paint;
    /// <summary>
    /// 设置
    /// </summary>
    protected Toggle Setting;
    /// <summary>
    /// 退出课程
    /// </summary>
    protected Button QuitBtn;
#if UNITY_STANDALONE
    /// <summary>
    /// 编辑模式遮罩
    /// </summary>
    private Button EditCheckBtn;
#endif
    #endregion
    #region 通用子模块及父物体
    /// <summary>
    /// 底部百科模块父物体 
    /// </summary>
    [HideInInspector]
    public Transform BaikeModulePoint;
    /// <summary>
    /// 百科列表模块父物体 
    /// </summary>
    [HideInInspector]
    public Transform ListModulePoint;
    /// <summary>
    /// 左侧层级模块父物体
    /// </summary>
    [HideInInspector]
    public Transform HierarchyModulePoint;
    /// <summary>
    /// 右侧知识点模块父物体
    /// </summary>
    [HideInInspector]
    public Transform KnowledgeModulePoint;
    /// <summary>
    /// 侧边栏toggle弹窗父物体
    /// </summary>
    [HideInInspector]
    public Transform SideToggleMenuPoint;
    /// <summary>
    ///顶层全屏查看父物体
    /// </summary>
    [HideInInspector]
    public Transform ShowModulePoint;
    #endregion

    /// <summary>
    /// 是否初次加载百科
    /// </summary>
    protected bool firstBaike = true;

    protected bool encyclopediaModelLoaded = false;

    private float timeCourseStart;

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        AddMsg(new ushort[]{
#if UNITY_ANDROID || UNITY_IOS
            (ushort)ARModuleEvent.Tracking,
            (ushort)ARModuleEvent.Support,
            (ushort)ARModuleEvent.Unsupport,
#endif
            (ushort)CoursePanelEvent.Option,
            (ushort)CoursePanelEvent.EditMode,
            (ushort)CoursePanelEvent.ChangeModel,
            (ushort)CoursePanelEvent.Quit,
            (ushort)BaikeSelectModuleEvent.BaikeSelect,
            (ushort)HyperLinkEvent.HyperlinkImage,
            (ushort)HyperLinkEvent.HyperImgClose,
            (ushort)HyperLinkEvent.HyperlinkVideo,
            (ushort)HyperLinkEvent.HyperVideoClose,
            (ushort)HyperLinkEvent.HyperlinkDOC,
            (ushort)HyperLinkEvent.HyperlinkClose,
            (ushort)HyperLinkEvent.HyperlinkAudio,
            (ushort)HyperLinkEvent.HyperAudioClose,
            (ushort)RoomChannelEvent.JoinRoomSuccess,
            (ushort)RoomChannelEvent.JoinRoomFail
        });

        InitVariables();

        //记录课程开始时间
        timeCourseStart = Time.realtimeSinceStartup;
    }

    protected virtual void InitVariables()
    {
        RootCanvasGroup = transform.GetComponent<CanvasGroup>();
        TopNavigation = transform.GetComponentByChildName<RectTransform>("TopNavigation");
        Title = TopNavigation.transform.GetComponentByChildName<Text>("Title");
        DescToggle = TopNavigation.transform.GetComponentByChildName<Toggle>("DescToggle");
        if (DescToggle)
        {
            DescPanel = transform.FindChildByName("DescPanel");
            DescText = DescPanel?.GetComponentByChildName<Text>("Text");
        }
        SideBar = transform.GetComponentByChildName<RectTransform>("SideBar");
        CourseSideBar = SideBar.GetComponent<CourseSideBar>();
        BtnsContent = transform.GetComponentByChildName<RectTransform>("BtnsContent");
        BtnsContentMask = BtnsContent?.GetComponent<Image>();
        BottomBtns = transform.GetComponentByChildName<RectTransform>("BottomBtns");

#if UNITY_ANDROID || UNITY_IOS
        EmptyClick = transform.GetComponentByChildName<Button>("EmptyClick");
        EmptyClick.onClick.AddListener(() => CourseSideBar.ShowBaikeSelectModule(false));
        //ARTog = this.GetComponentByChildName<Toggle>("AR");
#endif
        Setting = transform.GetComponentByChildName<Toggle>("Setting");    
        StartLive = transform.GetComponentByChildName<Button>("StartLive");
        Paint = transform.GetComponentByChildName<Toggle>("Paint");
        QuitBtn = transform.GetComponentByChildName<Button>("Quit");

        BaikeModulePoint = transform.FindChildByName("BaikeModulePoint");
        ListModulePoint = transform.FindChildByName("ListModulePoint");
        HierarchyModulePoint = transform.FindChildByName("HierarchyModulePoint");
        KnowledgeModulePoint = transform.FindChildByName("KnowledgeModulePoint");
        SideToggleMenuPoint = transform.FindChildByName("SideToggleMenuPoint");
        ShowModulePoint = transform.FindChildByName("ShowModulePoint");

        CourseSideBar.InitUI(this);

#if UNITY_STANDALONE
        EditCheckBtn = transform.GetComponentByChildName<Button>("EditCheckBtn");
        if (EditCheckBtn)
            EditCheckBtn.onClick.AddListener(() => UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo("有内容未保存，请先编辑并保存")));
#endif

        if (StartLive)
        {
            StartLive.onClick.AddListener(() => UIManager.Instance.OpenModuleUI<CreateRoomModule>(this, transform, new CreateRoomModule.RoomModuleData(GlobalInfo.currentCourseInfo.id)));
            StartLive.interactable = GlobalInfo.account.allowCoordination && GlobalInfo.currentCourseInfo.online;
        }

        if (Paint)
        {
            Paint.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    CollapseBtns();
                    var module = UIManager.Instance.OpenModuleUI<OPLPaintModule>(this, SideToggleMenuPoint);
                    module.closeDelegate = () => Paint.SetIsOnWithoutNotify(false);
                    UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo("当前为标注模式"));
                }
                else
                {
                    UIManager.Instance.CloseModuleUI<OPLPaintModule>(this);
                    ExpandBtns();
                }
            });
        }

        if (DescToggle)
        {
            DescToggle.onValueChanged.AddListener((isOn) =>
            {
                GlobalInfo.ShowPopup = isOn;
                if (isOn)
                {
                    DescPanel.GetComponent<ShowAnimation>().Show();
                    Cursor.lockState = CursorLockMode.None;
                    GlobalInfo.CursorLockMode = CursorLockMode.None;
                }
                else
                {
                    DescPanel.GetComponent<ShowAnimation>().Close();
                    //DescPanel.GetComponentByChildName<Button>("ConfirmBtn").GetComponentInChildren<Text>().text = "知道了";
                    if(!GlobalInfo.MultiplePopup)
                        Cursor.lockState = GlobalInfo.CursorLockMode;
                }
            });
        }

        Setting.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
                UIManager.Instance.OpenUI<OptionPanel>(UILevel.Fixed);
        });

        QuitBtn.onClick.AddListener(Previous);
    }

    public override void Show(UIData uiData = null)
    {
        OnPrepareShow(uiData);
        base.Show(uiData);
    }

    protected virtual void OnPrepareShow(UIData uiData)
    {
        if (uiData != null)
        {
            data = uiData as ABPanelInfo;
            GlobalInfo.currentCourseID = data.ID;

//#if UNITY_ANDROID || UNITY_IOS
//            if (ARTog)
//            {
//                ARTog.onValueChanged.AddListener((arg) =>
//                {
//                    GlobalInfo.isAR = arg;
//                    if (arg)
//                    {
//                        CourseSideBar.ShowBaikeSelectModule(false);
//                        UIManager.Instance.OpenModuleUI<ARModule>(this, SideToggleMenuPoint, new ARModuleData(ARTog));
//                    }
//                    else
//                        UIManager.Instance.CloseModuleUI<ARModule>(this);
//                });

//                if (uiData != null && ((ABPanelInfo)uiData).PanelName == typeof(ARPanel).ToString())
//                {
//                    ARTog.isOn = true;
//                }
//            }
//#endif
            InitData(() =>
            {
                UIManager.Instance.CloseUI<LoadingPanel>();

                SetTitle(GlobalInfo.currentCourseInfo);

                if (GlobalInfo.currentWikiList != null && GlobalInfo.currentWikiList.Count > 0)
                {
                    CourseSideBar.SetBaikePage();
                    //加载第一个百科
                    ToolManager.SendBroadcastMsg(new MsgInt()
                    {
                        msgId = (ushort)BaikeSelectModuleEvent.BaikeSelect,
                        arg = GlobalInfo.currentWikiList?[0]?.id ?? 0
                    });
                }
            });
        }
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);

        switch (msg.msgId)
        {
//#if UNITY_ANDROID || UNITY_IOS
//            case (ushort)ARModuleEvent.Tracking:
//                bool tracking = ((MsgBool)msg).arg1;
//                CourseSideBar.Fade(tracking);
//                break;
//            case (ushort)ARModuleEvent.Support:
//                if(ARTog)
//                    ARTog.interactable = true;
//                break;
//            case (ushort)ARModuleEvent.Unsupport:
//                if (ARTog)
//                {
//                    ARTog.isOn = false;
//                    ARTog.interactable = false;
//                }
//                break;
//#endif
            case (ushort)CoursePanelEvent.Option:
                Setting.SetIsOnWithoutNotify(((MsgBool)msg).arg1);
                break;
            case (ushort)CoursePanelEvent.Quit:
#if UNITY_ANDROID || UNITY_IOS
                SendMsg(new MsgBase((ushort)ARModuleEvent.ExitCourse));
#endif
                ExitRoom();
                break;
#if UNITY_STANDALONE
            case (ushort)CoursePanelEvent.EditMode:
                GlobalInfo.InEditMode = ((MsgBool)msg).arg1;
                EditCheckBtn.gameObject.SetActive(GlobalInfo.InEditMode);
                break;
#endif
            #region 百科列表模块
            case (ushort)BaikeSelectModuleEvent.BaikeSelect:
                OnBaikeSelectEventReceived(msg);
                break;
            #endregion
            //切换模型
            case (ushort)CoursePanelEvent.ChangeModel:
                if ((msg as MsgBool).arg1)
                {
                    SelectionModel selectionModel = ModelManager.Instance.modelGo?.AutoComponent<SelectionModel>();
                    if (selectionModel != null)
                    {
                        selectionModel.onSelectModel.AddListener((go, userId) => ChangeSelectModel(go?.transform, userId));
                        selectionModel.onDeSelectModel.AddListener((go, userId) => DeSelectModel(go?.transform, userId));
                    }
                }
                break;
            #region 知识点模块         
            case (ushort)HyperLinkEvent.HyperlinkImage:
                MsgHyperlink image = ((MsgBrodcastOperate)msg).GetData<MsgHyperlink>();
                ShowLinkModuleData imgData = new ShowLinkModuleData(image, () => SendMsg(new MsgHyperlinkClose((ushort)HyperLinkEvent.HyperImgClose, image)));
                UIManager.Instance.OpenModuleUI<ShowImgModule>(this, ShowModulePoint, imgData);
                break;
            case (ushort)HyperLinkEvent.HyperlinkVideo:
                MsgHyperlink video = ((MsgBrodcastOperate)msg).GetData<MsgHyperlink>();
                ShowLinkModuleData videoData = new ShowLinkModuleData(video, () =>
                {
                    MsgBrodcastOperate closeVideo = new MsgBrodcastOperate((ushort)HyperLinkEvent.HyperVideoClose, JsonTool.Serializable(new MsgHyperlinkClose((ushort)HyperLinkEvent.HyperVideoClose, video)));
                    SendMsg(closeVideo);
                    if (GlobalInfo.IsMainScreen())
                        NetworkManager.Instance.ClearVideoPacket();
                });
                UIManager.Instance.OpenModuleUI<ShowVideoModule>(this, ShowModulePoint, videoData);
                break;
            case (ushort)HyperLinkEvent.HyperlinkAudio:
                MsgHyperlink audio = ((MsgBrodcastOperate)msg).GetData<MsgHyperlink>();
                ShowLinkModuleData audioData = new ShowLinkModuleData(audio, () =>
                 {
                     MsgBrodcastOperate closeAudio = new MsgBrodcastOperate((ushort)HyperLinkEvent.HyperAudioClose, JsonTool.Serializable(new MsgHyperlinkClose((ushort)HyperLinkEvent.HyperAudioClose, audio)));
                     SendMsg(closeAudio);
                     if (GlobalInfo.IsMainScreen())
                         NetworkManager.Instance.ClearVideoPacket();
                 });
                UIManager.Instance.OpenModuleUI<ShowAudioModule>(this, ShowModulePoint, audioData);
                break;
            case (ushort)HyperLinkEvent.HyperlinkDOC:
                MsgHyperlink docLink = ((MsgBrodcastOperate)msg).GetData<MsgHyperlink>();
                ShowLinkModuleData docData = new ShowLinkModuleData(docLink, () => SendMsg(new MsgHyperlinkClose((ushort)HyperLinkEvent.HyperlinkClose, docLink)));
                UIManager.Instance.OpenModuleUI<ShowLinkModule>(this, ShowModulePoint, docData);
                break;
            case (ushort)HyperLinkEvent.HyperImgClose:
                UIManager.Instance.CloseModuleUI<ShowImgModule>(this, new ShowLinkModuleData((MsgHyperlinkClose)msg));
                break;
            case (ushort)HyperLinkEvent.HyperVideoClose:
                UIManager.Instance.CloseModuleUI<ShowVideoModule>(this, new ShowLinkModuleData(((MsgBrodcastOperate)msg).GetData<MsgHyperlinkClose>()));
                break;
            case (ushort)HyperLinkEvent.HyperAudioClose:
                UIManager.Instance.CloseModuleUI<ShowAudioModule>(this, new ShowLinkModuleData(((MsgBrodcastOperate)msg).GetData<MsgHyperlinkClose>()));
                break;
            case (ushort)HyperLinkEvent.HyperlinkClose:
                UIManager.Instance.CloseModuleUI<ShowLinkModule>(this, new ShowLinkModuleData((MsgHyperlinkClose)msg));
                break;
            #endregion             
        }
    }

    protected virtual void OnBaikeSelectEventReceived(MsgBase msg)
    {
        int baikeId = ((MsgBrodcastOperate)msg).GetData<MsgInt>().arg;
        OnBaikeChanged(baikeId);
        encyclopediaModelLoaded = false;
        this.WaitTime(0.1f, () => LoadEncyclopedia(baikeId));
    }

    /// <summary>
    /// 修改用户当前选中模型
    /// </summary>
    /// <param name="go"></param>
    /// <param name="userId"></param>
    protected virtual void ChangeSelectModel(Transform go, int userId)
    {

    }

    /// <summary>
    /// 取消用户当前选中模型
    /// </summary>
    /// <param name="go"></param>
    /// <param name="userId"></param>
    protected virtual void DeSelectModel(Transform go, int userId)
    {

    }

    /// <summary>
    /// 百科切换回调，修改UI状态等，在加载百科模型前调用
    /// </summary>
    /// <param name="newBaikeId"></param>
    protected virtual void OnBaikeChanged(int newBaikeId)
    {
        DOVirtual.DelayedCall(0, () =>
        {
            BaikeSelectModule.selectID = newBaikeId;
            BaikeSelectModule.CurrentBaikeIndex = GlobalInfo.currentWikiList.FindIndex(wiki => wiki.id == newBaikeId);

            GlobalInfo.InSingleMode = false;
            //漫游模式会把相机放到模型中 需要先关闭漫游场景让相机回到原来的位置
            ClearBaikeModules();
            ModelManager.Instance.DestroyModels(true);
            ModelManager.Instance.DestroyScripts(true);
            ModelManager.Instance.DestroySyncComponent();

            CourseSideBar.OnBaikeChanged();
        });
    }

    public override void Previous()
    {
        base.Previous();
        Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
        popupDic.Add("取消", new PopupButtonData(null));
        popupDic.Add("退出", new PopupButtonData(() =>
        {
#if UNITY_ANDROID || UNITY_IOS
            SendMsg(new MsgBase((ushort)ARModuleEvent.ExitCourse));
#endif
            ExitRoom();
        }, true));
        UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "确定要退出课程吗?", popupDic));
    }

    /// <summary>
    /// 退出课程
    /// </summary>
    protected virtual void ExitRoom()
    {
        GlobalInfo.currentWiki = null;
        UIManager.Instance.CloseUI<OPLCoursePanel>();
        UIManager.Instance.OpenUI(data.PanelName);
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        int courseId = GlobalInfo.currentCourseID;
        int duration = (int)(Time.realtimeSinceStartup - timeCourseStart);

        ModelManager.Instance.DestroyScripts();
        ModelManager.Instance.DestroyModels(true);
        ResManager.Instance.StopAllDownLoad();
#if UNITY_ANDROID || UNITY_IOS
        GlobalInfo.isAR = false;
#endif
        BaikeSelectModule.selectID = 0;
        BaikeSelectModule.CurrentBaikeIndex = 0;
        GlobalInfo.currentCourseID = 0;

        Resources.UnloadUnusedAssets();
        System.GC.Collect();

        if (!transform.name.StartsWith(typeof(OPLCoursePanel).ToString()))
        {
            base.Close(uiData, callback);
        }
        else
        {
            RequestManager.Instance.AddCourseStudyDuration(courseId, duration, () =>
            {
                base.Close(uiData, callback);
            }, (msg) =>
            {
                base.Close(uiData, callback);
            });
        }
    }


    protected override void InitHoverHint()
    {
        AddHoverHint(CourseSideBar.HierarchyTog, "结构认知");
        AddHoverHint(CourseSideBar.AnimListTog, "动画列表");
        AddHoverHint(CourseSideBar.OperationListTog, "操作列表");
        AddHoverHint(CourseSideBar.KnowledgeTog, "课件资料");
        AddHoverHint(CourseSideBar.HistoryTog, "操作记录列表");
        AddHoverHint(CourseSideBar.ShowBaike, "列表");
        AddHoverHint(CourseSideBar.Prev, GlobalInfo.isExam ? "上一个试题" : "上一个课件");
        AddHoverHint(CourseSideBar.Next, GlobalInfo.isExam ? "下一个试题" : "下一个课件");
        AddHoverHint(StartLive, "协同");
        AddHoverHint(Paint, "画笔");
        AddHoverHint(Setting, "设置(ESC)");
        AddHoverHint(QuitBtn, "退出");
        //AddHoverHint(DescToggle, "说明", HoverOrientation.Bottom);
    }

    public override void GotoLogout()
    {
        var coursewareModule = GetComponentInChildren<LinkDatabaseModule>();
        if(coursewareModule != null && coursewareModule.UploadingCount > 0)
        {
            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("取消", new PopupButtonData(null));
            popupDic.Add("退出", new PopupButtonData(() =>
            {
                StorageManager.Instance.ReleaseAllTask();
                ToolManager.GoToLogin();
            }, true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "文件正在上传中，退出登录会中断上传，\r\n确定要退出登录吗？", popupDic));
        }
        else
        {
            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("取消", new PopupButtonData(null));
            popupDic.Add("退出", new PopupButtonData(() => ToolManager.GoToLogin(), true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("登出提示", "确定要退出登录吗?", popupDic));
        }      
    }

    #region 百科控制相关
    protected virtual void InitData(UnityAction callBack)
    {
        RequestManager.Instance.GetCourse(GlobalInfo.currentCourseID, (course) =>
        {
            GlobalInfo.SaveCourseInfo(course);
            GlobalInfo.currentWikiList = course.encyclopediaList;

            if (GlobalInfo.currentWikiList == null || GlobalInfo.currentWikiList.Count == 0)
            {
                var popupDic = new Dictionary<string, PopupButtonData>();
                {
                    popupDic.Add("确定", new PopupButtonData(null, true));
                    UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "该课程未添加百科", popupDic, null, false));
                }
            }
            callBack.Invoke();
        }, (failureMessage) =>
        {
            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("好的", new PopupButtonData(() => ToolManager.GoToLogin(), true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("错误", "获取课程失败，请重新登录", popupDic));
            Log.Error($"获取课程失败！原因为：{failureMessage}");
        });
    }

    /// <summary>
    /// 加载百科
    /// </summary>
    /// <param name="encyclopediaId"></param>
    private void LoadEncyclopedia(int encyclopediaId)
    {
        RequestManager.Instance.GetEncyclopedia(encyclopediaId, (encyclopedia, answer) =>
        {
            GlobalInfo.currentWiki = encyclopedia;
            if (GlobalInfo.currentWiki == null)
                return;
            GlobalInfo.hasRole = false;
            switch (GlobalInfo.currentWiki.origin)
            {
                case (int)PediaOrigin.Link:
                    UIManager.Instance.OpenModuleUI<WebModule>(this, BaikeModulePoint, new WebBaikeData((GlobalInfo.currentWiki as EncyclopediaLink).data));
                    NetworkManager.Instance.IsIMSync = true;
                    return;
                case (int)PediaOrigin.Project:
                case (int)PediaOrigin.ABPackage:
                default:
                    switch (GlobalInfo.currentWiki.typeId)
                    {
                        case (int)PediaType.Picture:
                            UIManager.Instance.OpenModuleUI<ShowImgModule>(this, BaikeModulePoint, new ShowLinkModuleData(GlobalInfo.currentWiki, FileExtension.IMG));
                            CourseSideBar.ShowBaikeSelectModule(false);
                            NetworkManager.Instance.IsIMSync = true;
                            break;
                        case (int)PediaType.ANV:
                            string fileExtension = FileExtension.Convert((GlobalInfo.currentWiki as EncyclopediaLink).data);
                            if (!string.IsNullOrEmpty(fileExtension))
                            {
                                if (fileExtension.Equals(FileExtension.MP4))
                                {
                                    UIManager.Instance.OpenModuleUI<ShowVideoModule>(this, BaikeModulePoint, new ShowLinkModuleData(GlobalInfo.currentWiki, FileExtension.MP4));
                                }
                                else if (fileExtension.Equals(FileExtension.MP3))
                                {
                                    UIManager.Instance.OpenModuleUI<ShowAudioModule>(this, BaikeModulePoint, new ShowLinkModuleData(GlobalInfo.currentWiki, FileExtension.MP3));
                                }
                            }
                            CourseSideBar.ShowBaikeSelectModule(false);
                            NetworkManager.Instance.IsIMSync = true;
                            break;
                        case (int)PediaType.Doc:
                            UIManager.Instance.OpenModuleUI<ShowLinkModule>(this, BaikeModulePoint, new ShowLinkModuleData(GlobalInfo.currentWiki, FileExtension.DOC));
                            CourseSideBar.ShowBaikeSelectModule(false);
                            NetworkManager.Instance.IsIMSync = true;
                            break;
                        case (int)PediaType.Disassemble:
                        case (int)PediaType.Animation:
                        case (int)PediaType.Operation:
                            LoadPediaWithModel();
                            break;
                        case (int)PediaType.Exercise:
                            UIManager.Instance.OpenModuleUI<OPLExerciseModule>(this, BaikeModulePoint);
                            CourseSideBar.ShowBaikeSelectModule(false);
                            NetworkManager.Instance.IsIMSync = true;
                            break;
                        default:
                            break;
                    }
                    break;
            }
        }, (msg) =>
        {
            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("好的", new PopupButtonData(() =>
            {
                if (GlobalInfo.IsLiveMode())
                    ExitRoom();
                else
                    ToolManager.GoToLogin();
            }, true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("错误", "获取百科失败，请重新登录", popupDic));
            Log.Error($"获取百科失败！原因为：{msg}");
        });
    }

    protected virtual void SetTitle(Course course)
    {
        //Title.EllipsisText($"<color=\"#7A7D8F\">课程/{course?.teachCategory}·{course?.teachSubCategory}·{course?.teachTag}/</color>{course?.name}", "...");
        Title.text = course?.name;

        if (DescToggle)
        {
            DescText.text = string.IsNullOrEmpty(course.remarks) ? "无"/* $"本次工作内容为{course.name}任务"*/ : course.remarks;
            if (string.IsNullOrEmpty(course.remarks))
            {
                StartCoroutine(ShowDescription(false));
            }
            else
            {
                StartCoroutine(ShowDescription(true));
            }
        }
    }

    protected IEnumerator ShowDescription(bool show)
    {
        yield return new WaitUntil(() => encyclopediaModelLoaded);
        yield return null;
        //DescToggle.gameObject.SetActive(true);
        DescToggle.isOn = show;
    }

    /// <summary>
    /// 拆分、动画、操作 模型百科
    /// </summary>
    protected virtual void LoadPediaWithModel()
    {
        EncyclopediaModel encyclopediaModel = GlobalInfo.currentWiki as EncyclopediaModel;

        if (encyclopediaModel.data == null || encyclopediaModel.data.abPackageList == null || encyclopediaModel.data.abPackageList.Count == 0)
        {
            NetworkManager.Instance.IsIMSync = true;
            Log.Error($"打开的百科为空! 百科ID:{encyclopediaModel.id}");
            CourseSideBar.ShowBaikeSelectModule(false);
//#if UNITY_ANDROID || UNITY_IOS
//            ARTog.interactable = false;
//#endif
            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("确定", new PopupButtonData(null, true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", string.Format("该{0}未同步，请在备课后台同步课程后重试", GlobalInfo.isExam ? "考核" : "百科"), popupDic));
        }
        else
        {
            if (GlobalInfo.isAR && firstBaike)
            {
                SendMsg(new MsgUnityAction((ushort)CoursePanelEvent.ModelLocate, () => LoadEncyclopediaModel(encyclopediaModel)));
            }
            else
            {
                LoadEncyclopediaModel(encyclopediaModel);
            }
            GetFileStatus(encyclopediaModel.data.projectId);
        }
    }

    /// <summary>
    /// 加载百科模型 
    /// </summary>
    /// <param name="encyclopedia"></param>
    protected virtual void LoadEncyclopediaModel(EncyclopediaModel encyclopedia)
    {
        var abList = encyclopedia.data.abPackageList.OrderByDescending(ab => ab.id).ToList();
        UIManager.Instance.OpenUI<LoadingPanel>(UILevel.Loading);
        bool loadNavMesh = encyclopedia.typeId == (int)PediaType.Operation && (encyclopedia as EncyclopediaOperation).hasRole;
        //ResManager.Instance.LoadModel(encyclopedia.id.ToString(), ResManager.Instance.OSSDownLoadPath + abList[0].filePath, loadNavMesh, false, (arg2) =>
        ResManager.Instance.LoadModelAsync(encyclopedia.id.ToString(), ResManager.Instance.OSSDownLoadPath + abList[0].filePath, loadNavMesh, true, (arg2) =>
        {
            GameObject go = ModelManager.Instance.CreateModel(arg2);

            if (go == null)
            {
                Log.Error(string.Format("百科{0}实例化失败", encyclopedia.id));
                UIManager.Instance.CloseUI<LoadingPanel>();
                NetworkManager.Instance.IsIMSync = true;
                return;
            }

            firstBaike = false;
            go.name = go.name.Replace("(Clone)", string.Empty);

            GlobalInfo.currentWikiNames.Clear();
            if (encyclopedia.modelNodes != null)
            {
                foreach (ModelNode node in encyclopedia.modelNodes)
                {
                    if (GlobalInfo.currentWikiNames.ContainsKey(node.uuid))
                        GlobalInfo.currentWikiNames[node.uuid] = node.nodeName;
                    else
                        GlobalInfo.currentWikiNames.Add(node.uuid, node.nodeName);
                }
            }
            GlobalInfo.currentWikiKnowledges.Clear();
            if (encyclopedia.knowledgePointList != null)
            {
                foreach (Knowledgepoint kp in encyclopedia.knowledgePointList)
                {
                    if (string.IsNullOrEmpty(kp.uuid))
                        continue;

                    string uuid = kp.uuid;
                    if (GlobalInfo.currentWikiKnowledges.ContainsKey(kp.uuid))
                        GlobalInfo.currentWikiKnowledges[kp.uuid].Add(kp);
                    else
                        GlobalInfo.currentWikiKnowledges.Add(kp.uuid, new List<Knowledgepoint>() { kp });
                }
            }

            switch (encyclopedia.typeId)
            {
                case (int)PediaType.Operation:
                    EncyclopediaOperation encyclopediaOperation = encyclopedia as EncyclopediaOperation;
                    GlobalInfo.currentBaikeType = BaikeType.SmallScene;
                    //根据配置设置有无漫游模式
                    if (!encyclopediaOperation.hasRole)
                        ModelManager.Instance.AddSyncComponent(Camera.main.gameObject);
                    else
                        GlobalInfo.hasRole = encyclopediaOperation.hasRole;

                    UIManager.Instance.OpenModuleUI<UISmallSceneModule>(this, BaikeModulePoint, new SmallSceneData(encyclopediaOperation.flows));
                    break;
                case (int)PediaType.Animation:
                    GlobalInfo.currentBaikeType = BaikeType.Anime;
                    ModelManager.Instance.InitScripts();
                    ModelManager.Instance.AddSyncComponent(Camera.main.gameObject);

                    AnimModule animModule = (AnimModule)UIManager.Instance.OpenModuleUI<AnimModule>(this, BaikeModulePoint);
                    animModule.ChangeBaike(go);
                    break;
                case (int)PediaType.Disassemble:
                    GlobalInfo.currentBaikeType = BaikeType.Dismantling;
                    ModelManager.Instance.InitScripts();
                    ModelManager.Instance.AddSyncComponent(Camera.main.gameObject);

                    DismantlingModule dismantlingModule = (DismantlingModule)UIManager.Instance.OpenModuleUI<DismantlingModule>(this, BaikeModulePoint);
                    dismantlingModule.ChangeBaike(go);
                    break;
            }

            encyclopediaModelLoaded = true;
            SendMsg(new MsgBool((ushort)CoursePanelEvent.ChangeModel, encyclopedia.typeId != (int)PediaType.Operation));
            //请求同步相机
            NetworkManager.Instance.SendFrameMsg(new MsgBase((ushort)GazeEvent.SyncCamera));
            //同步百科状态
            NetworkManager.Instance.SyncBaikeState();
        });
    }

    /// <summary>
    /// 获取百科同步状态
    /// </summary>
    /// <param name="projectId"></param>
    protected void GetFileStatus(int projectId)
    {
        //非工程百科
        if (projectId == 0)
            return;

        RequestManager.Instance.GetProjectStatus(projectId, (status) =>
        {
            //0: 未打包，1: 打包成功，2: 申请打包，3: 打包中，4: 打包失败
            if (status != 1)
                UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo(string.Format("该{0}存在最新版本，可在备课后台同步课程后查看", GlobalInfo.isExam ? "考核" : "百科")));
        }, (msg) =>
        {
            Log.Error($"获取工程文件[{projectId}]状态失败, {msg}");
        });
    }

    protected virtual void ClearBaikeModules(bool closeKnowledge = true)
    {
        Paint.SetIsOnWithoutNotify(false);
#if UNITY_ANDROID || UNITY_IOS
        EmptyClick.gameObject.SetActive(false);
#endif

        UIManager.Instance.CloseModuleUI<OPLPaintModule>(this);
        ExpandBtns();

        if (GlobalInfo.currentWikiList != null)
        {
            Encyclopedia pedia = GlobalInfo.currentWikiList.Find(wiki => wiki.id == BaikeSelectModule.selectID);
            if (pedia != null)
            {
                int pediaType = pedia.typeId;
                if (pediaType == (int)PediaType.Picture || pediaType == (int)PediaType.ANV || pediaType == (int)PediaType.Doc || pediaType == (int)PediaType.Exercise)
                {
                    closeKnowledge = true;
                }

                if (pediaType == (int)PediaType.Disassemble || pediaType == (int)PediaType.Animation)
                {
                    SendMsg(new MsgBase((ushort)ARModuleEvent.Support));
                }
                else
                {
                    SendMsg(new MsgBase((ushort)ARModuleEvent.Unsupport));
                }
            }
        }

        UIManager.Instance.CloseModuleUI<DismantlingModule>(this);
        UIManager.Instance.CloseModuleUI<AnimModule>(this);
        UIManager.Instance.CloseModuleUI<WebModule>(this);
        UIManager.Instance.CloseModuleUI<UISmallSceneModule>(this);
        UIManager.Instance.CloseModuleUI<OPLExerciseModule>(this);
        UIManager.Instance.CloseAllModuleUI<ShowImgModule>(this);
        UIManager.Instance.CloseAllModuleUI<ShowAudioModule>(this);
        UIManager.Instance.CloseAllModuleUI<ShowVideoModule>(this);
        UIManager.Instance.CloseAllModuleUI<ShowLinkModule>(this);

        Resources.UnloadUnusedAssets();
    }
    #endregion  

    #region 动效
    protected override float joinAnimePlayTime => 0.3f;
    protected override float exitAnimePlayTime => 0.2f;

    public override void JoinAnim(UnityAction callback)
    {
        JoinSequence.Join(Join());
        base.JoinAnim(callback);
    }
    public override void ExitAnim(UnityAction callback)
    {
        ExitSequence.Join(Exit());
        base.ExitAnim(callback);
    }

    protected virtual Sequence Join()
    {
        Sequence sequence = DOTween.Sequence();
        sequence.Join(TopNavigation.DOAnchorPos3DY(0, JoinAnimePlayTime));
#if UNITY_STANDALONE
        sequence.Join(SideBar.DOAnchorPos3DX(0, JoinAnimePlayTime));
        sequence.Join(BottomBtns.DOAnchorPos3DX(0, JoinAnimePlayTime));
#else
        sequence.Join(SideBar.DOAnchorPos3DX(100f, JoinAnimePlayTime));
#endif
        sequence.Join(RootCanvasGroup.DOFade(1f, JoinAnimePlayTime));
        return sequence;
    }

    protected virtual Sequence Exit()
    {
        Sequence sequence = DOTween.Sequence();
        sequence.Join(TopNavigation.DOAnchorPos3DY(TopNavigation.sizeDelta.y, ExitAnimePlayTime));
        sequence.Join(SideBar.DOAnchorPos3DX(-SideBar.sizeDelta.x, ExitAnimePlayTime));
#if UNITY_STANDALONE
        sequence.Join(BottomBtns.DOAnchorPos3DX(-BottomBtns.sizeDelta.x, ExitAnimePlayTime));
#endif
        sequence.Join(RootCanvasGroup.DOFade(0.2f, ExitAnimePlayTime));
        return sequence;
    }

    /// <summary>
    /// 画笔切换 收起左侧、中间按钮
    /// </summary>
    protected void CollapseBtns(UnityAction callback = null)
    {
#if UNITY_STANDALONE
        Sequence sequence = DOTween.Sequence();
        sequence.Join(BtnsContent.DOAnchorPos3DY(82f, ExitAnimePlayTime));
        sequence.Join(DOTween.To(() => 0.7f, value => BtnsContentMask.fillAmount = value, 0, ExitAnimePlayTime));
        //sequence.Join(BtnsContent.DOSizeDelta(new Vector2(BtnsContent.sizeDelta.x, 34f), ExitAnimePlayTime));
        sequence.OnComplete(() => callback?.Invoke());
#else
        callback?.Invoke();
#endif
    }
    /// <summary>
    /// 画笔切换 展开左侧、中间按钮
    /// </summary>
    protected void ExpandBtns(UnityAction callback = null)
    {
#if UNITY_STANDALONE
        Sequence sequence = DOTween.Sequence();
        sequence.Join(BtnsContent.DOAnchorPos3DY(0f, JoinAnimePlayTime));
        sequence.Join(DOTween.To(() => 0f, value => BtnsContentMask.fillAmount = value, 0.7f, ExitAnimePlayTime));
        //sequence.Join(BtnsContent.DOSizeDelta(new Vector2(BtnsContent.sizeDelta.x, 620f), JoinAnimePlayTime));
        sequence.OnComplete(() => callback?.Invoke());
#else
        callback?.Invoke();
#endif
    }
    #endregion
}