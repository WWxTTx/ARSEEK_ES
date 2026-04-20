using DG.Tweening;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityFramework.Runtime;
using WebSocketSharp;
using static OPLResourcesDownloadModule;
using static UnityFramework.Runtime.RequestData;

/// <summary>
/// 通用 课程列表+下载模块
/// </summary>
public class ResourcesModule : UIModuleBase
{
    public class ModuleData : UIData
    {
        public Transform anchor { get; set; }
        public Vector2 pivot { get; set; }
        /// <summary>
        /// 是否全屏模式
        /// </summary>
        public bool fullScreen { get; set; }
        /// <summary>
        /// 是否弹出
        /// </summary>
        public bool popup { get; set; }

        /// <summary>
        /// 一级设备
        /// </summary>
        public string category { get; set; }
        /// <summary>
        /// 一级设备Index
        /// </summary>
        public int categoryIndex { get; set; }
        /// <summary>
        /// 二级设备
        /// </summary>
        public string subCategory { get; set; }
        /// <summary>
        /// 类型
        /// </summary>
        public string tag { get; set; }
        /// <summary>
        /// 搜索
        /// </summary>
        public string searchKeyword { get; set; }
    }

    protected class TagColorConfig
    {
        public Color Background;
        public Color Text;

        public TagColorConfig(string bg, string txt)
        {
            Background = bg.HexToColor();
            Text = txt.HexToColor();
        }
    }

    protected ResourcesDownloader downloader;

    #region 记录
    protected string currentKeyword;
    protected virtual string CurrentKeyword
    {
        get { return currentKeyword; }
        set { currentKeyword = value; }
    }

    protected string currentCategory;
    protected virtual string CurrentCategory
    {
        get { return currentCategory; }
        set { currentCategory = value; }
    }

    protected int currentCategoryIndex;
    protected virtual int CurrentCategoryIndex
    {
        get { return currentCategoryIndex; }
        set { currentCategoryIndex = value; }
    }

    protected string currentSubCategory;
    protected virtual string CurrentSubCategory
    {
        get { return currentSubCategory; }
        set { currentSubCategory = value; }
    }

    protected string currentTag;
    protected virtual string CurrentTag
    {
        get { return currentTag; }
        set { currentTag = value; }
    }
    #endregion

#if UNITY_ANDROID || UNITY_IOS
    protected Dropdown CategoryFilter;
#endif
    protected Dropdown_LinkMode SubCategoryFilter;
    protected Dropdown_LinkMode TagFilter;
    protected ScrollPageView ScrollPage;
    protected CustomScrollRect PreviousPage;
    protected CustomScrollRect NextPage;
    protected ScrollRect ResourcesAll;
    protected Transform ResourceContent;
    protected GameObject CourseItem;
    protected InputField Search;
    protected GameObject Empty;

    protected GameObject LoadMask;

    /// <summary>
    /// 课程列表
    /// </summary>
    protected List<Course> CourseList = new List<Course>();
    /// <summary>
    /// 课程一级设备二级设备字典
    /// </summary>
    protected Dictionary<string, List<string>> CategoryDic = new Dictionary<string, List<string>>();
    /// <summary>
    /// 课程类型
    /// </summary>
    protected Dictionary<int, string> tags = new Dictionary<int, string>();
    /// <summary>
    /// 课程一级设备标签Item集合
    /// </summary>
    protected List<Transform> CategoryTabItems = new List<Transform>();
    /// <summary>
    /// 课程一级设备下拉选项
    /// </summary>
    protected List<string> categoryOptions = new List<string>();
    /// <summary>
    /// 课程二级设备下拉选项
    /// </summary>
    protected List<string> subCategoryOptions = new List<string>();
    /// <summary>
    /// 课程类型下拉选项
    /// </summary>
    protected List<string> tagOptions = new List<string>();

    /// <summary>
    /// 当前允许显示的列表
    /// </summary>
    protected HashSet<string> showList = new HashSet<string>();

    /// <summary>
    /// 课程一级设备标签底部选中状态Image
    /// </summary>
    private RectTransform _CategoryImage;
    protected RectTransform CategoryImage
    {
        get
        {
            if (_CategoryImage == null)
            {
                _CategoryImage = this.GetComponentByChildName<RectTransform>("CategoryImage");
            }

            return _CategoryImage;
        }
    }
    /// <summary>
    /// 切换课程分类动效时长
    /// </summary>
    protected const float animeTime = 0.3f;
    /// <summary>
    /// 是否向右翻页
    /// </summary>
    protected bool nextPage;

    /// <summary>
    /// 课程标签颜色
    /// </summary>
    protected List<TagColorConfig> TagColors;

    protected const string ellipsisTextMask = "...";

    protected static string ALL = "全部";

    protected ModuleData moduleData;

    /// <summary>
    /// 存储被选中的标签
    /// </summary>
    protected List<string> selectTags = new List<string>();
    /// <summary>
    /// 标签类型：1-获取课程标签，3-获取考核课程标签
    /// </summary>
    protected int tagType = 1;
    /// <summary>
    /// 是否是第一次进入
    /// </summary>
    protected bool isFirstEnter = false;

    /// <summary>
    /// 标签列表是否处于下拉状态
    /// </summary>
    protected bool isDownOpen = false;
    protected bool IsDownOpen
    {
        get 
        {
            return isDownOpen;
        }
        set 
        {
            isDownOpen = value;
            RefreshTagListAsync().Forget();
            RefreshAllLayoutAsync().Forget();
        }
    }

    #region 标签列表所需数据
    /// <summary>
    /// 标签高度和宽度间隔
    /// </summary>
#if UNITY_ANDROID || UNITY_IOS
    protected float highSpace = 90f;
#else
    protected float highSpace = 50.5f;
#endif
    protected float widthSpace = 44f;
    /// <summary>
    /// 标签与上边和左边的间隔距离
    /// </summary>
#if UNITY_ANDROID || UNITY_IOS
    protected float highPadding = -21f;
    protected float leftPadding = 12f;
#else
    protected float highPadding = -12.5f;
    protected float leftPadding = 10f;
#endif
    protected float resourceContentOffsetTop = 170;
    /// <summary>
    /// 课程页面高度
    /// </summary>
    protected float pageSize = 830;
    /// <summary>
    /// 下拉按钮与最后按钮的间隔
    /// </summary>
    protected float upDownButtonSpace = 26;
    /// <summary>
    /// 下载进度条的posy的位置
    /// </summary>
    protected float downPropressPosY = -84;

    /// <summary>
    /// 标签行数
    /// </summary>
    protected int lineCount = 0;
#endregion

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);
        moduleData = uiData as ModuleData;
        InitDownloader();
        InitUI();
    }

    /// <summary>
    /// 初始化下载组件
    /// </summary>
    protected virtual void InitDownloader()
    {
        downloader = transform.AutoComponent<ResourcesDownloader>();
    }

    /// <summary>
    /// 第三步
    /// </summary>
    private void InitUI()
    {
#if UNITY_ANDROID || UNITY_IOS
        CategoryFilter = transform.GetComponentByChildName<Dropdown>("CategoryFilter");
#endif
        SubCategoryFilter = transform.GetComponentByChildName<Dropdown_LinkMode>("TypeFilter");
        TagFilter = transform.GetComponentByChildName<Dropdown_LinkMode>("TagFilter");
        ScrollPage = this.GetComponentByChildName<ScrollPageView>("PageScrollList");
        PreviousPage = this.GetComponentByChildName<CustomScrollRect>("PreviousPage");
        NextPage = this.GetComponentByChildName<CustomScrollRect>("NextPage");
        ResourcesAll = this.GetComponentByChildName<ScrollRect>("ResourcesAll");
        resourceContentOffsetTop = ResourcesAll.GetComponent<RectTransform>().offsetMax.y;
        ResourceContent = this.FindChildByName("ResourceContent");
        CourseItem = this.FindChildByName("CourseItem").gameObject;
        Search = this.GetComponentByChildName<InputField>("Search");
        LoadMask = transform.FindChildByName("LoadMask")?.gameObject;
        Empty = transform.FindChildByName("Empty")?.gameObject;

        TagColors = new List<TagColorConfig>(8)
        {
            new TagColorConfig("#FFD9D5", "#C85847"),//Dismantle
            new TagColorConfig("#DAC5FF", "#6D4EBB"),//Assemble
            new TagColorConfig("#DBFFEB", "#4EB0BB"),//Usage
            new TagColorConfig("#E5C8FF", "#914A92"),//Maintenance
            new TagColorConfig("#FFBABA", "#D85A5A"),//Repair
            new TagColorConfig("#B9F8FF", "#4EB0BB"),//Adjustment
            new TagColorConfig("#FFEBDB", "#925E4A"),//Examine
            new TagColorConfig("#FFC3ED", "#D85AA9")//Trail
        };

        Button Clear = Search.GetComponentByChildName<Button>("Clear");
        Search.onValueChanged.AddListener(content =>
        {
            CurrentKeyword = content;
            Clear.gameObject.SetActive(content.Length != 0);
        });
        Search.onEndEdit.AddListener(content =>
        {
            string value = content.Replace(" ", "");
            if (string.IsNullOrEmpty(value))
                Search.text = string.Empty;

            RefreshList();
        });
        Clear.onClick.AddListener(() =>
        {
            Search.text = "";
            RefreshList();
        });

        //打开和关闭标签列表扩展
        transform.FindChildByName("TagDown").GetComponent<Button>().onClick.AddListener(() =>
        {
            IsDownOpen = true;
        });
        transform.FindChildByName("TagUp").GetComponent<Button>().onClick.AddListener(() =>
        {
            IsDownOpen = false;
        });
    }

    protected virtual void InitCourseList()
    {
        LoadMask?.SetActive(true);
    }

    /// <summary>
    /// 获取课程分类元数据
    /// </summary>
    /// <param name="callback"></param>
    protected void GetTeachCategories(UnityAction callback)
    {
        RequestManager.Instance.GetTeachCategoryList((data) =>
        {
            //CategoryDic.Clear();
            //foreach (TeachCategory category in data)
            //{
            //    List<string> subCategory = new List<string>();
            //    foreach (TeachCategory sub in category.subCategory)
            //        subCategory.Add(sub.category);
            //    CategoryDic.Add(category.category, subCategory);
            //}

            InitCategoryTab();
            callback?.Invoke();
        }, (msg) =>
        {
            Log.Error("获取课程分类列表失败");
            OpenLocalTip();
        });
    }

    /// <summary>
    /// 初始化课程一级设备Tab
    /// </summary>
    protected virtual void InitCategoryTab()
    {
        List<string> categories = new List<string>();
        //categories = CategoryDic.Keys.ToList();
        categories.Add(ALL);
        foreach (var item in tags)
        {
            categories.Add(item.Value);
        }

        CategoryTabItems.Clear();
        Transform content = transform.FindChildByName("CategoryContent");
        content.AddItemsView(categories, (item, info) =>
        {
            Text tagText = item.GetComponent<Text>();
            tagText.text = info;

            CategoryTabItems.Add(item);

            item.GetComponentInChildren<Toggle>().onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    CurrentCategory = info;

                    if (!selectTags.Contains(info))
                    {
                        selectTags.Add(info);
                    }
                    item.GetComponentInChildren<Text>().color = new Vector4(251f/255f, 89f/255f, 85f/255f, 1f);
                }
                else
                {
                    if (selectTags.Contains(info))
                    {
                        selectTags.Remove(info);
                    }
                    item.GetComponentInChildren<Text>().color = new Vector4(1f, 1f, 1f, 0.5f);
                }
                RefreshList();
            });
        });

        //LayoutRebuilder.ForceRebuildLayoutImmediate(content.GetComponent<RectTransform>());
        //content.GetComponent<HorizontalLayoutGroup>().enabled = false;

        //排列标签列表
        RefreshTagListAsync().Forget();
        RefreshAllLayoutAsync().Forget();

        //还原记录的状态
        {
            if (moduleData != null)
            {
                CurrentCategory = moduleData.category;
                CurrentCategoryIndex = moduleData.categoryIndex;
            }
            if (string.IsNullOrEmpty(CurrentCategory) && categories.Count > 0)
                CurrentCategory = categories[0];

            //foreach (Transform child in content)
            //{
            //    Text tagText = child.GetComponent<Text>();
            //    if (child.gameObject.activeSelf && (string.IsNullOrEmpty(CurrentCategory) || tagText.text.Equals(CurrentCategory)))
            //    {
            //        var toggle = child.GetComponentInChildren<Toggle>();
            //        {
            //            toggle.SetIsOnWithoutNotify(true);
            //            if (toggle.group != null)
            //            {
            //                toggle.group.allowSwitchOff = false;
            //            }
            //        }
            //        ChangeCategoryAnimOn(child, tagText);
            //        break;
            //    }
            //}

            
        }
    }

#if UNITY_ANDROID || UNITY_IOS
    /// <summary>
    /// 初始化课程一级设备下拉列表
    /// </summary>
    protected void InitCategoryFilter()
    {
        List<string> categories = new List<string>();
        categories = CategoryDic.Keys.ToList();

        categoryOptions.Clear();
        foreach (string category in CategoryDic.Keys)
        {
            categoryOptions.Add(category);
        }
        CategoryFilter.ClearOptions();
        CategoryFilter.AddOptions(categoryOptions.Select(c => new Dropdown.OptionData(c)).ToList());
        CategoryFilter.onValueChanged.AddListener((index) =>
        {
            CurrentCategory = categoryOptions[index];

            //InitSubCategoryFilters();
            //ResetTagFilterValue();
            RefreshList(/*Search.text.Replace(" ", ""), currentSubCategory, currentTag*/);

            ResourcesAll.verticalNormalizedPosition = 1;
        });

        //还原记录的状态d
        Log.Debug("测试");
        if (moduleData != null)
        {
            CurrentCategory = moduleData.category;
            CurrentCategoryIndex = moduleData.categoryIndex;
        }
        if (string.IsNullOrEmpty(CurrentCategory))
            CurrentCategory = categories[0];

        Log.Debug("测试1");
        CategoryFilter.SetValueWithoutNotify(categories.IndexOf(CurrentCategory));
    }
#endif

    ///// <summary>
    ///// 初始化课程二级设备下拉列表
    ///// </summary>
    //protected void InitSubCategoryFilters()
    //{
    //    subCategoryOptions.Clear();
    //    subCategoryOptions.Add(ALL);

    //    if (CategoryDic.ContainsKey(CurrentCategory))
    //    {
    //        foreach (string type in CategoryDic[CurrentCategory])
    //        {
    //            if (!subCategoryOptions.Contains(type))
    //                subCategoryOptions.Add(type);
    //        }
    //    }

    //    SubCategoryFilter.ClearOptions();
    //    SubCategoryFilter.AddOptions(subCategoryOptions.Select(t => new Dropdown_LinkMode.OptionData(t)).ToList());
    //    SubCategoryFilter.onValueChanged.AddListener((index) =>
    //    {
    //        CurrentSubCategory = index == 0 ? string.Empty : subCategoryOptions[index];
    //        //RefreshList(Search.text.Replace(" ", ""), CurrentSubCategory, CurrentTag);
    //        RefreshList();
    //    });
    //    CurrentSubCategory = string.Empty;
    //}

    /// <summary>
    /// 获取全部课程标签列表
    /// </summary>
    /// <param name="callback"></param>
    protected void GetTags(UnityAction callback)
    {
        RequestManager.Instance.GetTagList(tagType, (data) =>
        {
            tags.Clear();
            foreach (Record tag in data.records)
            {
                tags.Add(tag.id, tag.tag);
            }

            //InitTagFilters();
            callback?.Invoke();
        }, (msg) =>
        {
            Log.Error("获取课程标签列表失败");
            OpenLocalTip();
        });
    }

    ///// <summary>
    ///// 初始化课程标签下拉框
    ///// </summary>
    //protected void InitTagFilters()
    //{
    //    tagOptions.Clear();
    //    tagOptions.Add(ALL);

    //    tagOptions.AddRange(tags.Values);

    //    TagFilter.ClearOptions();
    //    TagFilter.AddOptions(tagOptions.Select(d => new Dropdown_LinkMode.OptionData(d)).ToList());
    //    TagFilter.onValueChanged.AddListener((index) =>
    //    {
    //        CurrentTag = index == 0 ? string.Empty : tagOptions[index];
    //        //RefreshList(Search.text.Replace(" ", ""), CurrentSubCategory, CurrentTag);
    //        RefreshList();
    //    });
    //}

    ///// <summary>
    ///// 重置课程标签
    ///// </summary>
    //protected void ResetTagFilterValue()
    //{
    //    TagFilter.SetValueWithoutNotify(0);
    //    CurrentTag = string.Empty;
    //}

    /// <summary>
    /// 刷新课程列表显示
    /// </summary>
    /// <param name="search"></param>
    /// <param name="subCategory"></param>
    /// <param name="tag"></param>
    protected virtual void RefreshList()
    {
        ShowLoad(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid ShowLoad(System.Threading.CancellationToken ct)
    {
        if (isFirstEnter)
        {
            isFirstEnter = false;
        }
        else
        {
            UIManager.Instance.OpenUI<LoadingPanel>();
            transform.FindChildByName("PageScrollList").gameObject.SetActive(false);
        }

        showList = new HashSet<string>();
        //开启自动布局控制
        ResourceContent.GetComponent<GridLayoutGroup>().enabled = true;
        ResourceContent.GetComponent<ContentSizeFitter>().enabled = true;

        //foreach (var value in GlobalInfo.courseDic)
        //{
        //    UseTagGetCourseID(value.Value, ref showList);
        //}
        UseTagGetCourseID(GlobalInfo.courseDicExists.Values.ToList(), ref showList);

        Empty.SetActive(showList.Count == 0);

        foreach (Transform child in transform.FindChildByName("ResourceContent"))
        {
            child.gameObject.SetActive(showList.Contains(child.name));
        }

        ////禁用自动布局控制
        //LayoutRebuilder.ForceRebuildLayoutImmediate(ResourceContent as RectTransform);
        //ResourceContent.GetComponent<GridLayoutGroup>().enabled = false;
        //ResourceContent.GetComponent<ContentSizeFitter>().enabled = false;

        await UniTask.Delay(System.TimeSpan.FromSeconds(0.8f), cancellationToken: ct);
        UIManager.Instance.CloseUI<LoadingPanel>();
#if UNITY_STANDALONE
        transform.FindChildByName("PageScrollList").gameObject.SetActive(true);
#endif
    }

    /// <summary>
    /// 通过选中的课程标签选择显示的课程
    /// </summary>
    /// <param name="courseList"></param>
    /// <param name="list"></param>
    private void UseTagGetCourseID(List<Course> courseList, ref HashSet<string> list)
    {
        bool valid = true;

        foreach (var course in courseList)
        {
            if (course.tags.IsNullOrEmpty()) continue;

            //把后端课程中的标签数据（id组成的字符串）转换为对应的标签名字（标签名字具有唯一性）
            string[] strs = course.tags.Split(',');
            foreach (var item in tags)
            {
                for (int i = 0; i < strs.Length; i++)
                {
                    if (item.Key.ToString() == strs[i])
                    {
                        strs[i] = item.Value;
                    }
                }
            }

            //valid = true;
            //if (selectTags.Count == 0)
            //{
            //    valid = false;
            //}
            //foreach (string str in selectTags)
            //{
            //    if (!strs.Contains(str) || !course.name.Contains(Search.text))
            //    {
            //        valid = false;
            //    }
            //}

            valid = false;
            foreach (string str in strs)
            {
                if (selectTags.Contains(ALL) || selectTags.Contains(str) || tags.Count == 0)//标签列表为空
                {
                    valid = true;
                    break;
                }
            }
            valid &= course.name.Contains(Search.text);

            if (valid)
            {
                list.Add(course.id.ToString());
            }
        }
    }

    ///// <summary>
    ///// 筛选列表
    ///// </summary>
    ///// <param name="search">搜索关键字</param>
    ///// <param name="subCategory">二级设备</param>
    ///// <param name="tag">课程类别</param>
    ///// <param name="courseList"></param>
    ///// <param name="list"></param>
    //protected void GetShowList(string search, string subCategory, string tag, List<Course> courseList, ref HashSet<string> list)
    //{
    //    bool valid = true;


    //    foreach (var course in courseList)
    //    {
    //        valid = true;

    //        if (!string.IsNullOrEmpty(search) && !course.name.Contains(search))
    //            valid = false;
    //        if (!string.IsNullOrEmpty(subCategory) && !subCategory.Equals(ALL) && !course.teachSubCategory.Equals(subCategory))
    //            valid = false;
    //        if (!string.IsNullOrEmpty(tag) && !tag.Equals(ALL) && !course.teachTag.Equals(tag))
    //            valid = false;

    //        if (valid)
    //            list.Add(course.id.ToString());
    //    }
    //}

    /// <summary>
    /// 加载所有课程
    /// </summary>
    protected virtual void RankCourseList()
    {
        CourseList.Clear();

        // 确保优先加载初始分类下的课程封面图
        //if (CategoryDic.Count > 0 && (moduleData == null || string.IsNullOrEmpty(moduleData.category)))
        //{
        //    if (GlobalInfo.courseDic.TryGetValue(CategoryDic.Keys.First(), out List<Course> courses))
        //        CourseList.AddRange(courses);

        //    foreach (var value in GlobalInfo.courseDic)
        //    {
        //        if (!value.Key.Equals(CategoryDic.Keys.First()))
        //            CourseList.AddRange(value.Value);
        //    }
        //}
        //else
        //{
        //    if (moduleData != null && GlobalInfo.courseDic.TryGetValue(moduleData.category, out List<Course> courses))
        //        CourseList.AddRange(courses);

        //    foreach (var value in GlobalInfo.courseDic)
        //    {
        //        if (!value.Key.Equals(moduleData.category))
        //            CourseList.AddRange(value.Value);
        //    }
        //}

        foreach(var item in GlobalInfo.courseDicExists.Values) 
        {
            CourseList.Add(item);
        }
    }

    protected virtual void OpenLocalTip()
    {

    }

    /// <summary>
    /// 排列标签列表
    /// </summary>
    public async UniTaskVoid RefreshTagListAsync()
    {
        await UniTask.WaitForEndOfFrame(this);

        float high = highPadding;
        float width = leftPadding;

        int sum = -1;

        lineCount = 0;

        for (int i = 0; i < CategoryTabItems.Count; i++)
        {
            CategoryTabItems[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(width, high);
            width += CategoryTabItems[i].GetComponent<RectTransform>().sizeDelta.x;
            if (width > transform.FindChildByName("CategoryContent").GetComponent<RectTransform>().sizeDelta.x)
            {
                if (lineCount == 0) 
                {
                    sum = i - 1;
                }
                lineCount++;
                high -= highSpace;
                width = leftPadding;
                CategoryTabItems[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(width, high);
                width += CategoryTabItems[i].GetComponent<RectTransform>().sizeDelta.x;
            }
            width += widthSpace;
        }
  
        if (lineCount >= 1) {
            if (isDownOpen)
            {
                transform.FindChildByName("TagDown").gameObject.SetActive(false);
                transform.FindChildByName("TagUp").gameObject.SetActive(true);
                transform.FindChildByName("TagUp").parent = CategoryTabItems[CategoryTabItems.Count - 1];
                transform.FindChildByName("TagUp").GetComponent<RectTransform>().anchoredPosition = new Vector2(upDownButtonSpace + CategoryTabItems[CategoryTabItems.Count - 1].GetComponent<RectTransform>().sizeDelta.x, 0);
            }
            else 
            {
                transform.FindChildByName("TagDown").gameObject.SetActive(true);
                transform.FindChildByName("TagUp").gameObject.SetActive(false);
                transform.FindChildByName("TagDown").parent = sum == -1 ? CategoryTabItems[CategoryTabItems.Count - 1] : CategoryTabItems[sum];
                transform.FindChildByName("TagDown").GetComponent<RectTransform>().anchoredPosition = new Vector2(upDownButtonSpace + (sum == -1 ? CategoryTabItems[CategoryTabItems.Count - 1].GetComponent<RectTransform>().sizeDelta.x : CategoryTabItems[sum].GetComponent<RectTransform>().sizeDelta.x), 0);
            }
        }

    }

    /// <summary>
    /// 设置标签，下载进度条，课程页面的布局
    /// </summary>
    public async UniTaskVoid RefreshAllLayoutAsync()
    {
        await UniTask.WaitForEndOfFrame(this);
        await UniTask.WaitForEndOfFrame(this);

        RectTransform content_rectTransform = transform.FindChildByName("Category").GetComponent<RectTransform>();
        RectTransform downLoading_rectTransform = transform.FindChildByName("Downloading").GetComponent<RectTransform>();
#if UNITY_ANDROID || UNITY_IOS
        RectTransform resourceAll_rectTransform = transform.FindChildByName("ResourcesAll").GetComponent<RectTransform>();
#else
        RectTransform pageScrollList_rectTransform = transform.FindChildByName("PageScrollList").GetComponent<RectTransform>();
#endif
        if (isDownOpen)
        {
            content_rectTransform.sizeDelta = new Vector2(content_rectTransform.sizeDelta.x, highSpace * (lineCount + 1));
        }
        else
        {
            content_rectTransform.sizeDelta = new Vector2(content_rectTransform.sizeDelta.x, highSpace);
        }
        downLoading_rectTransform.anchoredPosition = new Vector2(downLoading_rectTransform.anchoredPosition.x, IsDownOpen ? (downPropressPosY - highSpace * lineCount) : downPropressPosY);
#if UNITY_ANDROID || UNITY_IOS
        resourceAll_rectTransform.offsetMax = new Vector2(resourceAll_rectTransform.offsetMax.x, IsDownOpen ? resourceContentOffsetTop - highSpace * lineCount : resourceContentOffsetTop);
#else
        pageScrollList_rectTransform.sizeDelta = new Vector2(pageScrollList_rectTransform.sizeDelta.x, IsDownOpen ? (pageSize - highSpace * lineCount) : pageSize);
#endif
    }

#region 动效
    //protected void PageLeft(bool scrolled)
    //{
    //    if (scrolled)
    //    {
    //        if (CurrentCategoryIndex <= 0) return;
    //        CurrentCategoryIndex--;
    //    }
    //    var previousPage = PreviousPage.GetComponent<RectTransform>();
    //    PageAnim(previousPage);
    //    ScrollPage.IsRightDrag = true;
    //    if (CurrentCategoryIndex == 0) ScrollPage.IsLeftDrag = false;
    //}
    //protected void PageRight(bool scrolled)
    //{
    //    if (scrolled)
    //    {
    //        if (CurrentCategoryIndex >= CategoryTabItems.Count - 1) return;
    //        CurrentCategoryIndex++;
    //    }
    //    var nextPage = NextPage.GetComponent<RectTransform>();
    //    PageAnim(nextPage);
    //    ScrollPage.IsLeftDrag = true;
    //    if (CurrentCategoryIndex == CategoryTabItems.Count - 1) ScrollPage.IsRightDrag = false;
    //}

    //protected void PageAnim(RectTransform page)
    //{
    //    float playtime = 0.65f;
    //    var Action = page.FindChildByName("Action");
    //    Tweener t1 = Action.DOLocalRotate(new Vector3(0, 0, 25f), playtime);
    //    Tweener t2 = Action.DOLocalRotate(new Vector3(0, 0, 155f), playtime).SetEase(Ease.Linear);
    //    SendMsg(new MsgBase((ushort)UIAnimEvent.ShowAnimMask));

    //    var s = DOTween.Sequence();
    //    s.Append(t1);
    //    s.Insert(0.1f + t1.Duration(), t2);

    //    foreach (Transform child in Action)
    //    {
    //        Transform r = child.FindChildByName("R");
    //        s.Insert(0.1f + t1.Duration(), r.DOLocalRotate(new Vector3(0, 0, 76), playtime).SetLoops(2, LoopType.Yoyo)).SetEase(Ease.Linear);
    //    }

    //    this.WaitTime(0.35f, () => s.Kill(true));
    //    s.OnComplete(() =>
    //    {
    //        ScrollPage.PageTo(1);
    //        SendMsg(new MsgBase((ushort)UIAnimEvent.HideAnimMask));
    //    });
    //}

    /// <summary>
    /// 切换课程分类动效
    /// </summary>
    protected virtual void ChangeCategoryAnimOn(Transform item, Text tagText)
    {
        CategoryImage.DOAnchorPos(Vector2.right * (item as RectTransform).anchoredPosition.x, animeTime);
        CategoryImage.DOSizeDelta(new Vector2((item as RectTransform).sizeDelta.x * 1.25f, 3), animeTime);
        DOTween.To(() => tagText.fontSize, value => tagText.fontSize = value, 20, animeTime);
    }
    protected virtual void ChangeCategoryAnimOff(Text tagText)
    {
        DOTween.To(() => tagText.fontSize, value => tagText.fontSize = value, 16, animeTime);
    }
#endregion

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        base.Close(uiData, callback);
        ResManager.Instance.StopAllDownLoad();
    }
}
