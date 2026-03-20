using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;
using WebSocketSharp;
using System.Collections;

/// <summary>
/// 一点课课程列表模块
/// </summary>
public class OPLResourcesDownloadModule : ResourcesModule
{
    #region UI
    private CanvasGroup canvasGroup;
    private RectTransform rect;
#if UNITY_ANDROID || UNITY_IOS
    private Button Setting;
#endif
    /// <summary>
    /// 一键下载
    /// </summary>
    private Button AllDownload;
    /// <summary>
    /// 下载进度
    /// </summary>
    private GameObject Downloading;
    /// <summary>
    /// 进度条
    /// </summary>
#if UNITY_STANDALONE
    private Slider ProgressBar;
#else
    private Image ProgressFill;
#endif
    private Text ProgressValue;
    /// <summary>
    /// 已完成下载
    /// </summary>
    private Text Finished;
    /// <summary>
    /// 总下载数
    /// </summary>
    private Text Total;

    private Image CloseBtn;
    private Image OpenBtn;
    private RectTransform OpenBtnCtrl;
    #endregion

    #region 记录
    protected override string CurrentKeyword
    {
        get { return currentKeyword; }
        set
        {
            currentKeyword = value;
            ResourcesPanel.searchKeyword = currentKeyword;
        }
    }

    protected override int CurrentCategoryIndex
    {
        get { return currentCategoryIndex; }
        set
        {
            currentCategoryIndex = value;
            ResourcesPanel.categoryIndex = currentCategoryIndex;
        }
    }

    protected override string CurrentCategory
    {
        get { return currentCategory; }
        set
        {
            currentCategory = value;
            ResourcesPanel.category = currentCategory;
        }
    }

    protected override string CurrentSubCategory
    {
        get { return currentSubCategory; }
        set
        {
            currentSubCategory = value;
            ResourcesPanel.subCategory = currentSubCategory;
        }
    }

    protected override string CurrentTag
    {
        get { return currentTag; }
        set
        {
            currentTag = value;
            ResourcesPanel.courseTag = currentTag;
        }
    }
    #endregion

    private bool isOpen = false;
    private bool isShow = false;

    public override void Open(UIData uiData = null)
    {     
        base.Open(uiData);

        InitUIVariables();
        RegistUIEvent();

        isShow = false;
        OpenModule(() =>
        {
            isShow = true;
            InitCourseList();
        });
    }

    public override void Show(UIData uiData = null)
    {
        base.Show(uiData);
        if (isShow) OpenModule();

        if (uiData is ModuleData moduleData)
        {
            if (moduleData.popup)
            {
                OpenBtn?.GetComponent<Button>()?.onClick?.Invoke();
            }
            else
            {
                if (moduleData.anchor)
                {
                    var pos = moduleData.anchor.position;
                    Vector2 pivot = new Vector2(moduleData.pivot.x, 0.5f);
                    rect.pivot = pivot;
                    rect.anchorMin = pivot;
                    rect.anchorMax = pivot;
                    //rect.position = new Vector3(pos.x, 0, pos.z);
                    rect.anchoredPosition = (isAndroid ? -15f : -90f) * Vector2.right;
                }
            }
        }
    }

    protected override void InitDownloader()
    {
        base.InitDownloader();

        downloader.OnTotalDownloadCountChanged.AddListener((totalCount) =>
        {
            Total.text = $"/{totalCount}";
            Downloading.SetActive(true);
        });

        downloader.OnDownloadProgressChanged.AddListener((progress) =>
        {
#if UNITY_STANDALONE
            ProgressBar.value = progress;
#else
            ProgressFill.fillAmount = progress;
#endif
            ProgressValue.text = $"{(progress * 100).ToString("f0")}%";
        });

        downloader.OnReset.AddListener(() =>
        {
            Finished.text = "0";
            Total.text = "/0";
            ProgressValue.text = "0%";
#if UNITY_STANDALONE
            ProgressBar.value = 0;
#else
            ProgressFill.fillAmount = 0;
#endif
            Downloading.SetActive(false);
        });
    }

    /// <summary>
    /// 初始化物品
    /// </summary>
    private void InitUIVariables()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rect = transform.GetComponentByChildName<RectTransform>("Background");
#if UNITY_ANDROID || UNITY_IOS
        Setting = transform.GetComponentByChildName<Button>("Setting");
#endif
        AllDownload = transform.GetComponentByChildName<Button>("AllDownload");
        Downloading = transform.FindChildByName("Downloading").gameObject;
#if UNITY_STANDALONE
        ProgressBar = transform.GetComponentByChildName<Slider>("ProgressBar");
#else
        ProgressFill = transform.GetComponentByChildName<Image>("ProgressFill");
#endif
        ProgressValue = transform.GetComponentByChildName<Text>("ProgressValue");
        Finished = transform.GetComponentByChildName<Text>("Finished");
        Total = transform.GetComponentByChildName<Text>("Total");

        OpenBtn = transform.GetComponentByChildName<Image>("Open");
        OpenBtnCtrl = OpenBtn?.GetComponentByChildName<RectTransform>("Control");
        CloseBtn = transform.GetComponentByChildName<Image>("Close"); 
    }
    /// <summary>
    /// 初始化按钮点击效果
    /// </summary>
    private void RegistUIEvent()
    {
        AllDownload.onClick.AddListener(()=>downloader.DownloadAllResources(()=>
        {    
            Transform courseItem;
            //触发需要更新AB包的课程的Update按钮事件
            foreach (int id in downloader.CourseNeedUpdate)
            {
                courseItem = ResourceContent.FindChildByName(id.ToString());
                if (courseItem != null)
                {
                    courseItem.GetComponentByChildName<Button>("Update").onClick.Invoke();
                }
            }
        }, ()=>
        {
            UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, Search.transform.parent, new LocalTipModule.ModuleData("资源正在下载中!"));
        },()=>
        {
            UIManager.Instance.OpenModuleUI<LocalTipModule>(ParentPanel, Search.transform.parent, new LocalTipModule.ModuleData("暂无可下载资源!"));
        }));

        #region 好像已经弃用了
        if (CloseBtn)
        {
            var width = rect.rect.width;
            CloseBtn.raycastTarget = false;
            CloseBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                OpenBtnCtrl.localEulerAngles = Vector3.zero;
                Move(rect, width, () =>
                {
                    CloseBtn.raycastTarget = false;
                    OpenBtn.raycastTarget = true;
                });
            });
        }

        if (OpenBtn)
        {
            var vector = new Vector3(0, 0, 180);
            OpenBtn.raycastTarget = true;
            OpenBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                OpenBtnCtrl.localEulerAngles = vector;
                Move(rect, 0, () =>
                {
                    CloseBtn.raycastTarget = true;
                    OpenBtn.raycastTarget = false;
                });
            });
        }
        #endregion

#if UNITY_ANDROID || UNITY_IOS
        Setting.onClick.AddListener(() => UIManager.Instance.OpenUI<OptionPanel>(UILevel.Fixed));
#endif
    }

    /// <summary>
    /// 加载课程列表
    /// </summary>
    /// <param name="moduleData"></param>
    protected override void InitCourseList()
    {
        base.InitCourseList();

        ScrollPage.gameObject.SetActive(true);
        AllDownload.gameObject.SetActive(true);
        Search.gameObject.SetActive(true);

        if (!string.IsNullOrEmpty(moduleData.searchKeyword))
            Search.text = moduleData.searchKeyword;

        //todo:增加tag的读取

        //CurrentSubCategory = moduleData.subCategory;
        //加载存储的标签
        CurrentTag = moduleData.tag;
        //if (!string.IsNullOrEmpty(CurrentTag))
        //{
        //    ResourceContent.GetComponent<ToggleGroup>().allowSwitchOff = true;
        //}
        GetTags(() =>
        {
            //TagFilter.SetValueWithoutNotify(tagOptions.IndexOf(moduleData.tag));
            GetTeachCategories(() =>
            {
                //InitSubCategoryFilters();
                //SubCategoryFilter.SetValueWithoutNotify(subCategoryOptions.IndexOf(moduleData.subCategory));

                RankCourseList();
                InitList();
                InitCourseListState();

                //RefreshList(moduleData.searchKeyword, moduleData.subCategory, moduleData.tag);
                //RefreshList();
                //ScrollPage.PageTo(1);

                //UIManager.Instance.CloseUI<LoadingPanel>();
                //PreviousPage.FindChildByName("LoadAnim").gameObject.SetActive(true);
                //NextPage.FindChildByName("LoadAnim").gameObject.SetActive(true);
                //LoadMask?.SetActive(false);
            });
        });
    }

    /// <summary>
    /// 初始化列表
    /// </summary>
    /// <param name="moduleData"></param>
    private void InitList()
    {
        CanvasGroup canvasGroup = ResourceContent.AutoComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;

        ResourceContent.RefreshItemsView(CourseItem, CourseList, (item, info) =>
        {
            Button Resources = item.GetComponentByChildName<Button>("Resources");

            item.name = info.id.ToString();
            downloader.AddImageTask(item.name, info.iconPath, Resources);
            item.GetComponentByChildName<Text>("Name").EllipsisText(info.name, ellipsisTextMask);

            //生成课程item下部标签文字
            string str = string.Empty;
            if (tags.Count > 0 && !info.tags.IsNullOrEmpty())
            {
                string[] strs = info.tags.Split(',');
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
                str = strs[0];
                for (int i = 1; i < strs.Length; i++)
                {
                    str += "/" + strs[i];
                }
            }

            if (GlobalInfo.courseDicExists.ContainsKey(info.id))
            {
                GlobalInfo.courseDicExists[info.id].tags_readable = str;
            }

            //增加当课程item下部文字过多时的显示详细信息的图标
            item.GetComponentByChildName<Text>("Type").text = str;
            LayoutRebuilder.ForceRebuildLayoutImmediate(item.GetComponentByChildName<Text>("Type").GetComponent<RectTransform>());
            if (item.GetComponentByChildName<Text>("Type").GetComponent<RectTransform>().sizeDelta.x > item.GetComponentByChildName<Text>("Type").transform.parent.GetComponent<RectTransform>().sizeDelta.x)
            {
                item.FindChildByName("Detail").gameObject.SetActive(true);
            }
            item.GetComponentByChildName<Text>("DetailText").text = str;

            //TagColorConfig tagColorConfig = TagColors[info.teachTagId % TagColors.Count];
            //item.GetComponentByChildName<Image>("TagBg").color = tagColorConfig.Background;
            //Text Tag = item.GetComponentByChildName<Text>("Tag");
            //Tag.text = info.teachTag;
            //Tag.color = tagColorConfig.Text;

            Resources.onClick.RemoveAllListeners();
            Resources.onClick.AddListener(() => OnItemClick(info));
            //未获取到课程更新状态之前 不可点击
            Resources.GetComponent<Image>().raycastTarget = false;
        });
        //todo
        this.WaitTime(0.01f, () =>
        {
            //根据存储的标签信息，选中对应的标签
            if (CurrentTag.IsNullOrEmpty())
            {
                //选中第一个标签
                if (CategoryTabItems.Count > 0)
                    CategoryTabItems[0].GetComponentInChildren<Toggle>().isOn = true;
            }
            else
            {
                string[] strs = CurrentTag.Split(',');
                foreach (string str in strs)
                {
                    foreach (var item in CategoryTabItems)
                    {
                        if (item.GetComponentInChildren<Text>().text == str)
                        {
                            item.GetComponentInChildren<Toggle>().isOn = true;
                        }
                    }
                }
            }
            //ResourceContent.GetComponent<ToggleGroup>().allowSwitchOff = false;
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
        });     
    }

    /// <summary>
    /// 列表元素点击事件
    /// </summary>
    /// <param name="info"></param>
    private void OnItemClick(Course info)
    {
        //存储当前的选中的标签，存储格式：标签，标签，标签
        CurrentTag = "";
        for (int i = 0; i < selectTags.Count; i++)
        {
            if (i == 0)
            {
                CurrentTag = selectTags[i];
            }
            else
            {
                CurrentTag += "," + selectTags[i];
            }
        }

        if (ResourcesDownloader.DownloadingCount > 0 && !GlobalInfo.IsLiveMode())
        {
            var popupDic = new Dictionary<string, PopupButtonData>();
            {
                popupDic.Add("否", new PopupButtonData(null));
                popupDic.Add("是", new PopupButtonData(() => EnterCourse(info), true));
                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "进入课程会中断下载，确认要进入吗？", popupDic, null, false));
            }
        }
        else
        {
            EnterCourse(info);
        }
    }
    /// <summary>
    /// 进入课程
    /// </summary>
    /// <param name="info"></param>
    private void EnterCourse(Course info)
    {
        GlobalInfo.currentCourseInfo = info;
        SendMsg(new MsgInt((ushort)ResourcesPanelEvent.SelectCourse, info.id));
        ExitAnim(null);
    }

    /// <summary>
    /// 初始化课程列表状态
    /// </summary>

    public void InitCourseListState()
    {
        if (ResourcesPanel.request)
        {
            RequestManager.Instance.GetCourseABPackageList((courseABData) =>
            {
                ResourcesPanel.request = false;
                GlobalInfo.SaveCourseABInfo(courseABData);

                downloader.UpdateResourcesState(ResourceContent, OnItemStateUpdate, OnItemActive);
                AllDownload.interactable = true;
            }, (msg) =>
            {
                Log.Error($"获取课程AB包失败！原因为：{msg}");
            });
        }
        else
        {
            downloader.UpdateResourcesState(ResourceContent, OnItemStateUpdate, OnItemActive);
            AllDownload.interactable = true;
        }
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

                downloader.AddABPackTask(courseId, data, DownloadText, DownloadTrans, UpdateBtn, (count) => Finished.text = count.ToString());
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
        Button Resources = item.GetComponentByChildName<Button>("Resources");
        Resources.GetComponent<Image>().raycastTarget = true;
    }

    protected override void OpenLocalTip()
    {
        base.OpenLocalTip();
        UIManager.Instance.OpenModuleUI<LocalTipModule_Button>(ParentPanel, ScrollPage.transform,
          new LocalTipModule_Button.ModuleData("获取课程列表失败", "刷新", InitCourseList));
    }

    #region 动效
    private void Move(RectTransform target, float x, UnityAction callBack = null)
    {
        canvasGroup.blocksRaycasts = false;
        target.DOAnchorPosX(x, JoinAnimePlayTime).OnComplete(() =>
        {
            callBack?.Invoke();
            canvasGroup.blocksRaycasts = true;
        });
    }

    /// <summary>
    /// 打开动效
    /// </summary>
    /// <param name="callback"></param>
    public void OpenModule(UnityAction callback = null)
    {
        SendMsg(new MsgBase((ushort)UIAnimEvent.ShowAnimMask));
        DOTween.To(() => 0f, (value) => canvasGroup.alpha = (value), 1f, JoinAnimePlayTime).OnComplete(() =>
        {
            canvasGroup.blocksRaycasts = true;
            SendMsg(new MsgBase((ushort)UIAnimEvent.HideAnimMask));
            isOpen = true;
            callback();
        });
    }

    public override void ExitAnim(UnityAction callback)
    {
        closeDelegate?.Invoke();
        if (isOpen)
        {
            ExitSequence.Append(DOTween.To(() => 1f, (value) => canvasGroup.alpha = (value), 0f, ExitAnimePlayTime).OnComplete(() =>
            {
                canvasGroup.blocksRaycasts = false;
                isOpen = false;
            }));
        }
        base.ExitAnim(callback);
    }
    #endregion

    protected override void OnDestroy()
    {
        base.OnDestroy();
        ResourcesDownloader.DownloadingCount = 0;
        Resources.UnloadUnusedAssets();
        StopAllCoroutines();
    }
}