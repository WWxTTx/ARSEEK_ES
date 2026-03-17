using System.Linq;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.ServiceRequestData;
using static UnityFramework.Runtime.RequestData;
using WebSocketSharp;

/// <summary>
/// 创建协同房间模块
/// </summary>
public class CreateRoomModule : ResourcesModule
{
    public class RoomModuleData : UIData
    {
        /// <summary>
        /// 已选课程ID（从一点课开始协同时传入）
        /// </summary>
        public int courseId;

        public RoomModuleData(int courseId)
        {
            this.courseId = courseId;
        }
    }

    #region UI
    private CanvasGroup MaskCanvas;
    private RectTransform Background;
    private CanvasGroup BackgroundCanvas;

    /// <summary>
    /// 关闭模块
    /// </summary>
    private Button CloseBtn;
    //选择课程
    private GameObject SelectCourse;
    /// <summary>
    /// 取消、下一步
    /// </summary>
    private Button CancelBtn;
    private Button NextBtn;
    //房间信息
    private GameObject RoomInfo;
    /// <summary>
    /// 房间类型 直播房间
    /// </summary>
    protected Toggle LiveRoom;
    /// <summary>
    /// 房间类型 协同房间
    /// </summary>
    protected Toggle SynRoom;
    /// <summary>
    /// 房间名称、密码
    /// </summary>
    protected InputField_LinkMode RoomName;
    protected InputField_LinkMode RoomPassword;
    /// <summary>
    /// 上一步、取消
    /// </summary>
    private Button PrevBtn;
    private Button CreateBtn;
    protected Button CreateNowBtn;
#if UNITY_ANDROID || UNITY_IOS
    private Toggle Help;
#endif
    #endregion

    /// <summary>
    /// 当前选择课程ID
    /// </summary>
    protected int thisCourseId;

    /// <summary>
    /// 当前创建房间
    /// </summary>
    protected RoomInfoModel thisRoomInfo;
    /// <summary>
    /// 是否正在进入房间
    /// </summary>
    protected bool joiningRoom;

    /// <summary>
    /// 下面生成的课程开关
    /// </summary>
    private List<Toggle> courceToggles = new List<Toggle>();

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        AddMsg(new ushort[]
        {
            (ushort)RoomChannelEvent.UpdateRoomList,
            (ushort)RoomChannelEvent.JoinRoomSuccess,
            (ushort)RoomChannelEvent.JoinRoomFail
        });

        InitUIVariables();
        RegistUIEvent();

        RoomModuleData createRoomModuleData = uiData != null ? uiData as RoomModuleData : null;
        if (createRoomModuleData == null || createRoomModuleData.courseId == 0)
        {
#if UNITY_STANDALONE
            SubCategoryFilter.gameObject.SetActive(false);
            TagFilter.gameObject.SetActive(false);
            Search.gameObject.SetActive(false);
#endif
            InitData();
        }
        else
        {
            SelectCourse.SetActive(false);
            PrevBtn.gameObject.SetActive(false);
            RoomInfo.SetActive(true);
            thisCourseId = createRoomModuleData.courseId;

            ////填充默认房间名称
            //if (GlobalInfo.courseDicExists.TryGetValue(thisCourseId, out Course course))
            //    RoomName.text = course.name;
        }
    }

    /// <summary>
    /// 初始化课程列表
    /// </summary>
    protected virtual void InitData()
    {
        RequestManager.Instance.GetCourseList(courseData =>
        {
            GlobalInfo.SaveCourseInfo(courseData);
            InitCourseList();
        }, failureMessage =>
        {
            var popupDic = new Dictionary<string, PopupButtonData>();
            {
                popupDic.Add("好的", new PopupButtonData(() => ToolManager.GoToLogin(), true));
                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("获取失败", "获取课程列表失败，请重新登录", popupDic, () => ToolManager.GoToLogin()));
            }
            Log.Error($"获取课程列表失败！原因为：{failureMessage}");
        });
    }

    private void InitUIVariables()
    {
        MaskCanvas = this.GetComponentByChildName<CanvasGroup>("Mask");
        Background = transform.GetComponentByChildName<RectTransform>("Background");
        BackgroundCanvas = Background.GetComponent<CanvasGroup>();

        CloseBtn = transform.GetComponentByChildName<Button>("Close");

        SelectCourse = transform.FindChildByName("SelectCourse").gameObject;
        Empty = transform.FindChildByName("Empty").gameObject;
        CancelBtn = transform.GetComponentByChildName<Button>("Cancel");
        NextBtn = transform.GetComponentByChildName<Button>("Next");

        RoomInfo = transform.FindChildByName("RoomInfo").gameObject;
        LiveRoom = transform.GetComponentByChildName<Toggle>("Live");
        SynRoom = transform.GetComponentByChildName<Toggle>("Syn");
        RoomName = transform.GetComponentByChildName<InputField_LinkMode>("RoomName");
        RoomPassword = transform.GetComponentByChildName<InputField_LinkMode>("RoomPassword");
        PrevBtn = transform.GetComponentByChildName<Button>("Prev");
        CreateBtn = transform.GetComponentByChildName<Button>("Create");
#if UNITY_ANDROID || UNITY_IOS
        Help = transform.GetComponentByChildName<Toggle>("Help");
        GameObject helpContent = transform.FindChildByName("HelpContent").gameObject;
        Help.onValueChanged.AddListener((isOn) => helpContent.SetActive(isOn));
#endif

        //设置标签列表所需数据
        pageSize = 484f;
    }

    private void RegistUIEvent()
    {
        CloseBtn.onClick.AddListener(CloseModule);
        CancelBtn.onClick.AddListener(CloseModule);
        NextBtn.onClick.AddListener(NextStep);
        PrevBtn.onClick.AddListener(PrevStep);
        RoomName.onValueChanged.AddListener((value) =>
        {
            RoomName.text = value.RemoveSpecialSymbols();
            CreateBtn.interactable = !string.IsNullOrEmpty(value) && (string.IsNullOrEmpty(RoomPassword.text) || RoomPassword.text.Length == 6);
            if(CreateNowBtn)
                CreateNowBtn.interactable = !string.IsNullOrEmpty(value) && (string.IsNullOrEmpty(RoomPassword.text) || RoomPassword.text.Length == 6);
        });
        RoomPassword.onValueChanged.AddListener(value =>
        {
            CreateBtn.interactable = !string.IsNullOrEmpty(RoomName.text) && (string.IsNullOrEmpty(value) || value.Length == 6);
            if (CreateNowBtn)
                CreateNowBtn.interactable = !string.IsNullOrEmpty(RoomName.text) && (string.IsNullOrEmpty(value) || value.Length == 6);
        });
        CreateBtn.onClick.AddListener(OnCreateRoomBtnClicked);
    }

    private void NextStep()
    {
        RoomInfo.SetActive(true);
        SelectCourse.SetActive(false);

        ////填充默认房间名称
        //if (GlobalInfo.courseDicExists.TryGetValue(thisCourseId, out Course course))
        //{
        //    RoomName.text = course.name;
        //}
    }
    private void PrevStep()
    {
        RoomName.text = string.Empty;
        RoomPassword.text = string.Empty;
        SelectCourse.SetActive(true);
        RoomInfo.SetActive(false);
    }
    protected virtual void OnCreateRoomBtnClicked()
    {
        RequestManager.Instance.GetCourse(thisCourseId, course =>
        {
            if (course.encyclopediaList == null || course.encyclopediaList.Count == 0)
            {
                UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, transform, new LocalTipModule.ModuleData("该课程未添加百科"));
                return;
            }
            CreateRoomAndJoin(RoomName.text, RoomPassword.text, 0, course.id, GetCourseTags(course.tags), course.iconPath);
        }, (error) =>
        {
            Log.Error($"获取课程[{thisCourseId}]失败，{error}");
        });
    }

    protected virtual void CloseModule()
    {
        UIManager.Instance.CloseModuleUI<CreateRoomModule>(ParentPanel);
    }

    #region 课程列表
    /// <summary>
    /// 加载课程列表
    /// </summary>
    /// <param name="moduleData"></param>
    protected override void InitCourseList()
    {
        base.InitCourseList();
        StartCoroutine(initCourseListCo());     
    }

    private IEnumerator initCourseListCo()
    {
        //避免阻塞动效
        yield return new WaitUntil(() => BackgroundCanvas.alpha >= 1);
        ScrollPage.gameObject.SetActive(true);

        GetTags(() =>
        {
            GetTeachCategories(() =>
            {
                //InitSubCategoryFilters();
                RankCourseList();
                InitList();
                InitCourseListState();

                //RefreshList(Search.text.Replace(" ", ""), CurrentSubCategory, CurrentTag);
                //ScrollPage.PageTo(1);

                //UIManager.Instance.CloseUI<LoadingPanel>();
                //PreviousPage.FindChildByName("LoadAnim").gameObject.SetActive(true);
                //NextPage.FindChildByName("LoadAnim").gameObject.SetActive(true);
            });
        });
    }

    protected override void InitCategoryTab()
    {
        base.InitCategoryTab();

        //SubCategoryFilter.gameObject.SetActive(true);
        //TagFilter.gameObject.SetActive(true);
        Search.gameObject.SetActive(true);
    }

    protected override void ChangeCategoryAnimOn(Transform item, Text tagText)
    {
        tagText.SetAlpha(0.9f);
    }

    protected override void ChangeCategoryAnimOff(Text tagText)
    {
        tagText.SetAlpha(0.5f);
    }

    /// <summary>
    /// 初始化列表
    /// </summary>
    /// <param name="moduleData"></param>
    private void InitList()
    {
        courceToggles.Clear();
        ResourceContent.RefreshItemsView(CourseItem, CourseList, (item, info) =>
        {
            Toggle Resources = item.GetComponentByChildName<Toggle>("Resources");

            item.name = info.id.ToString();
            downloader.AddImageTask(item.name, info.iconPath, Resources);
            item.GetComponentByChildName<Text>("Name").EllipsisText(info.name, ellipsisTextMask);

            string courseTag = GetCourseTags(info.tags);
            if (GlobalInfo.courseDicExists.ContainsKey(info.id))
            {
                GlobalInfo.courseDicExists[info.id].tags_readable = courseTag;
            }
            //增加当课程item下部文字过多时的显示详细信息的图标
            item.GetComponentByChildName<Text>("Type").text = courseTag;
            LayoutRebuilder.ForceRebuildLayoutImmediate(item.GetComponentByChildName<Text>("Type").GetComponent<RectTransform>());
            if (item.GetComponentByChildName<Text>("Type").GetComponent<RectTransform>().sizeDelta.x > item.GetComponentByChildName<Text>("Type").transform.parent.GetComponent<RectTransform>().sizeDelta.x)
            {
                item.FindChildByName("Detail").gameObject.SetActive(true);
            }
            item.GetComponentByChildName<Text>("DetailText").text = courseTag;

            //TagColorConfig tagColorConfig = TagColors[info.teachTagId % TagColors.Count];
            //item.GetComponentByChildName<Image>("TagBg").color = tagColorConfig.Background;
            //Text Tag = item.GetComponentByChildName<Text>("Tag");
            //Tag.text = info.teachTag;
            //Tag.color = tagColorConfig.Text;

            Resources.onValueChanged.RemoveAllListeners();
            Resources.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    thisCourseId = info.id;
                    NextBtn.interactable = true;
                    foreach (var item in courceToggles) 
                    {
                        if (item.isOn && item.transform.parent.name != thisCourseId.ToString()) 
                        {
                            item.isOn = false;
                        }
                    }
                }
                else
                {
                    if (!Resources.group.AnyTogglesOn())
                        NextBtn.interactable = false;
                }
            });
            //未获取到课程更新状态之前 不可点击
            Resources.GetComponent<Image>().raycastTarget = false;
            courceToggles.Add(Resources);
        });

        if (CategoryTabItems.Count > 0)
            CategoryTabItems[0].GetComponentInChildren<Toggle>().isOn = true;
    }

    /// <summary>
    /// 初始化课程列表状态
    /// </summary>
    private void InitCourseListState()
    {
        UIManager.Instance.OpenUI<LoadingPanel>();
        RequestManager.Instance.GetCourseABPackageList((courseABData) =>
        {
            GlobalInfo.SaveCourseABInfo(courseABData);
            downloader.UpdateResourcesState(ResourceContent, OnItemStateUpdate, OnItemActive);
        }, (msg) =>
        {
            Log.Error($"获取课程AB包失败！原因为：{msg}");
        });
    }

    /// <summary>
    /// 更新课程item状态显示
    /// </summary>
    /// <param name="item"></param>
    /// <param name="result"></param>
    /// <param name="courseId"></param>
    /// <param name="data"></param>
    private void OnItemStateUpdate(Transform item, int result, int courseId, List<CourseABPackage> data)
    {
        Button UpdateBtn = item.GetComponentByChildName<Button>("Update");
        Transform DownloadTrans = item.FindChildByName("Download");
        Text DownloadText = DownloadTrans.GetComponentInChildren<Text>();

        if (result == 0)
        {
            UpdateBtn.gameObject.SetActive(false);
            DownloadTrans.gameObject.SetActive(false);
        }
        else
        {
            //设置初始状态
            switch (result)
            {
                case 1:
                    UpdateBtn.GetComponentInChildren<Text>().text = "下载";
                    break;
                case 2:
                    UpdateBtn.GetComponentInChildren<Text>().text = "更新";
                    break;
                case 3:
                    UpdateBtn.GetComponentInChildren<Text>().text = "继续下载";
                    break;
            }

            UpdateBtn.onClick.RemoveAllListeners();
            UpdateBtn.onClick.AddListener(() =>
            {
                if (GlobalInfo.isOffLine)
                {
                    ToolManager.PleaseOnline();
                    return;
                }

                downloader.UpdateDownloadingCount(courseId);

                DownloadText.text = "下载中 0%";
                DownloadTrans.gameObject.SetActive(true);
                UpdateBtn.gameObject.SetActive(false);

                var downloadBackGround = DownloadTrans.GetChild(0);
                {
                    downloadBackGround.localScale = Vector3.zero;
                    DownloadText.SetAlpha(0);

                    DOTween.Sequence()
                    .Append(downloadBackGround.DOScale(Vector3.one, 0.2f))
                    .AppendInterval(0.1f)
                    .Append(DownloadText.DOFade(1, 0.3f));
                }

                downloader.AddABPackTask(courseId, data, DownloadText, DownloadTrans, UpdateBtn);
            });

            DownloadTrans.gameObject.SetActive(false);
            UpdateBtn.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// 修改item可交互状态
    /// </summary>
    /// <param name="item"></param>
    private void OnItemActive(Transform item)
    {
        Toggle Resources = item.GetComponentByChildName<Toggle>("Resources");
        Resources.GetComponent<Image>().raycastTarget = true;
    }

    protected string GetCourseTags(string courseTag)
    {
        //生成课程item下部标签文字
        string courseTagReadable = string.Empty;
        if (tags.Count > 0 && !courseTag.IsNullOrEmpty())
        {
            string[] strs = courseTag.Split(',');
            foreach (var item1 in tags)
            {
                for (int i = 0; i < strs.Length; i++)
                {
                    if (item1.Key.ToString() == strs[i])
                    {
                        strs[i] = item1.Value;
                    }
                }
            }
            courseTagReadable = strs[0];
            for (int i = 1; i < strs.Length; i++)
            {
                courseTagReadable += "/" + strs[i];
            }
        }
        return courseTagReadable;
    }


    protected override void OpenLocalTip()
    {
        base.OpenLocalTip();
        UIManager.Instance.OpenModuleUI<LocalTipModule_Button>(ParentPanel, SelectCourse.transform,
            new LocalTipModule_Button.ModuleData("获取课程列表失败", "刷新", InitCourseList));
    }
    #endregion

    #region 创建、加入房间
    /// <summary>
    /// 创建并加入房间
    /// </summary>
    /// <param name="go"></param>
    protected virtual void CreateRoomAndJoin(string roomName, string roomPassword, int duration, int courseId, string courseTitle, string courseIcon)
    {
        RoomType roomType = LiveRoom.isOn ? RoomType.Live : RoomType.Synergia;
        if(LiveRoom.isOn)
            UIManager.Instance.OpenUI<TransitionPanel>(UILevel.Loading, new TransitionPanel.TransitionData("正在进入直播...", false));
        else
            UIManager.Instance.OpenUI<TransitionPanel>(UILevel.Loading, new TransitionPanel.TransitionData("正在进入协同...", false));

        NetworkManager.Instance.CreateRoom(roomName, roomPassword, roomType, courseId, courseTitle, courseIcon, (roomUuid) =>
        {
            SendMsg(new MsgStringFloat((ushort)CoursePanelEvent.Transition, string.Empty, 1f));

            NetworkManager.Instance.GetRoomInfo(roomUuid, (roomInfoModel) =>
            {
                thisRoomInfo = roomInfoModel;
                joiningRoom = true;
                JoinRoom(roomPassword); ;
            }, (errCode, errMsg) =>
            {
                UIManager.Instance.CloseUI<TransitionPanel>();
                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("创建房间失败"));
                Log.Error($"创建房间失败！原因为：{errMsg}");
            });
        }, (code, failureMessage) =>
        {
            UIManager.Instance.CloseUI<TransitionPanel>();
            switch (code)
            {
                case 0:
                    UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("网络异常，创建房间失败"));
                    break;
                default:
                    UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("创建房间失败"));
                    break;
            }
            Log.Error($"创建房间失败！原因为：{failureMessage}");
        });
    }

    /// <summary>
    /// 加入房间
    /// </summary>
    /// <param name="id"></param>
    /// <param name="password"></param>
    protected void JoinRoom(string password)
    {
        thisRoomInfo.Password = password;
        NetworkManager.Instance.JoinRoom(thisRoomInfo);
    }

    /// <summary>
    /// 加入房间回调
    /// </summary>
    /// <param name="info"></param>
    protected virtual void JoinRoomCallback()
    {
        //记录当前房间信息
        GlobalInfo.isLive = true;
        GlobalInfo.UpdateCourseMode();
        GlobalInfo.roomInfo = thisRoomInfo;
        PlayerPrefs.SetString(GlobalInfo.lastSynergiaRoomId, GlobalInfo.roomInfo.Uuid);
        if (ParentPanel is TrainingPanel)
        {
            UIManager.Instance.CloseUI<TrainingPanel>();
            UIManager.Instance.OpenUI<OPLSynCoursePanel>();
        }
        else
        {
            UIManager.Instance.CloseUI<OPLCoursePanel>();
            UIManager.Instance.OpenUI<OPLSynCoursePanel>();
        }
        NetworkManager.Instance.SetUserColor(GlobalInfo.account.id);
        this.WaitTime(0.5f, () => UIManager.Instance.CloseUI<TransitionPanel>());
    }

    /// <summary>
    /// 加入房间失败回调
    /// </summary>
    /// <param name="info"></param>
    protected void JoinRoomFailed(string msg, float toastShowTime = 1.5f)
    {
        UIManager.Instance.CloseUI<TransitionPanel>();
        UIManager.Instance.CloseUI<LoadingPanel>();
        joiningRoom = false;

        if (!string.IsNullOrEmpty(msg))
        {
            UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo(msg, toastShowTime));
        }
    }
    #endregion

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)RoomChannelEvent.JoinRoomSuccess:
                JoinRoomCallback();
                break;
            case (ushort)RoomChannelEvent.JoinRoomFail:
                JoinRoomFailed(((MsgString)msg).arg);
                break;
        }
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        base.Close(uiData, callback);
        TrainingPanel.creatingRoom = false;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        ResourcesDownloader.DownloadingCount = 0;
        //释放不再引用的资源
        Resources.UnloadUnusedAssets();
    }

    #region 动效
    protected override float exitAnimePlayTime => 0.1f; 

    public override void JoinAnim(UnityAction callback)
    {
        //SoundManager.Instance.PlayEffect("Popup");
        MaskCanvas.alpha = 1f;
#if UNITY_STANDALONE
        JoinSequence.Join(DOTween.To(() => 0.2f * Vector3.one, (value) => Background.transform.localScale = value, Vector3.one, JoinAnimePlayTime));
#else
        JoinSequence.Join(DOTween.To(() => new Vector2(Background.sizeDelta.x, Background.anchoredPosition.y), (value) => Background.anchoredPosition = value, new Vector2(0f, Background.anchoredPosition.y), JoinAnimePlayTime));
#endif
        JoinSequence.Join(DOTween.To(() => 0f, (value) => BackgroundCanvas.alpha = value, 1f, JoinAnimePlayTime));
        base.JoinAnim(callback);
    }

    public override void ExitAnim(UnityAction callback)
    {
#if UNITY_STANDALONE
        ExitSequence.Join(DOTween.To(() => Vector3.one, (value) => Background.transform.localScale = value, 0.8f * Vector3.one, ExitAnimePlayTime));
#else
        ExitSequence.Join(DOTween.To(() => new Vector2(0f, Background.anchoredPosition.y), (value) => Background.anchoredPosition = value, new Vector2(Background.sizeDelta.x, Background.anchoredPosition.y), ExitAnimePlayTime));
#endif
        ExitSequence.Join(DOTween.To(() => 1f, (value) => BackgroundCanvas.alpha = value, 0f, ExitAnimePlayTime));
        ExitSequence.Join(DOTween.To(() => 1f, (value) => MaskCanvas.alpha = value, 0f, ExitAnimePlayTime));
        base.ExitAnim(callback);
    }
#endregion
}