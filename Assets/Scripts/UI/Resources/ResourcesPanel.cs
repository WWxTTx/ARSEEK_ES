using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Rendering;
using DG.Tweening;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;

public class ResourcesPanel : UIPanelBase
{
    protected override bool CanLogout { get { return true; } }
    public override bool canOpenOption => true;

    /// <summary>
    /// 是否重新请求课程列表
    /// </summary>
    public static bool request = true;
    /// <summary>
    /// 是否加载总览模型
    /// </summary>
    public static bool isOverview = true;
    /// <summary>
    /// 是否开启弹出层
    /// </summary>
    public static int state = -2;
    /// <summary>
    /// 记录资源下载模块课程一级设备选择
    /// </summary>
    public static string category;
    public static int categoryIndex = 0;
    /// <summary>
    /// 记录资源下载模块课程二级设备选择
    /// </summary>
    public static string subCategory;
    /// <summary>
    /// 记录资源下载模块课程类型选择
    /// </summary>
    public static string courseTag;
    /// <summary>
    /// 记录资源下载模块搜素
    /// </summary>
    public static string searchKeyword;

    public override void GotoLogout()
    {
        base.GotoLogout();
        request = true;
        isOverview = true;
        state = -2;
        category = string.Empty;
        categoryIndex = 0;
        searchKeyword = string.Empty;
        subCategory = string.Empty;
        courseTag = string.Empty;
    }

    public override void Open(UIData uiData = null)
    {
        Cursor.lockState = CursorLockMode.None;
        GlobalInfo.CursorLockMode = CursorLockMode.None;

#if UNITY_ANDROID || UNITY_IOS
        ModelManager.Instance.ControlClipPlane(1f);
#endif
        base.Open(uiData);

        AddMsg((ushort)ResourcesPanelEvent.SelectCourse);

        //ModelManager.Instance.ControlSceneLight(false);
        GetData((hasModel, overviewAnchors) =>
        {
            this.GetComponentByChildName<Button>("Exit").onClick.AddListener(Exit);

            //根据有无模型
            if (hasModel)
            {
                //ModelManager.Instance.ControlGlobalVolume(true, 1);
                InitQuality();

                InitOverview(overviewAnchors);

                if (state != -1)
                {
                    this.WaitTime(0.1f, () =>
                    {
                        var temp = this.GetComponentByChildName<Toggle>(state.ToString());
                        {
                            if (temp != null)
                            {
                                temp.isOn = true;
                            }
                        }
                    });
                }
                HasModel();
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                TapRecognizer.Instance.RegistOnLeftMouseDoubleClick(ResetCamera);
#else
                TapRecognizer.Instance.RegistOnRightMouseClick(ResetCamera);
#endif
            }
            else
            {
                //ModelManager.Instance.ControlGlobalVolume();
                NoModel();
            }
        });
    }

    private void ResetCamera()
    {
        ModelManager.Instance.ResetCameraPose(true, true);
    }

    /// <summary>
    /// 获取课程数据
    /// </summary>
    /// <param name="callBack"></param>
    private void GetData(UnityAction<bool, CourseAnchor[]> callBack)
    {
        UIManager.Instance.OpenUI<LoadingPanel>(UILevel.Loading);

        if (!request)
        {
            GetOverviewData(callBack);
        }
        else
        {
            RequestManager.Instance.GetCourseList(courseData =>
            {
                //request = false;
                GlobalInfo.SaveCourseInfo(courseData);
                GetOverviewData(callBack);
            }, failureMessage =>
            {
                var popupDic = new Dictionary<string, PopupButtonData>();
                {
                    popupDic.Add("好的", new PopupButtonData(() => ToolManager.GoToLogin(), true));
                    UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("获取失败", "获取课程失败，请重新登录", popupDic, Previous));
                }
                Log.Error($"获取课程失败！原因为：{failureMessage}");
            });

            //NewRequestManager.Instance.GetCourseList(courseData =>
            //{
            //    NewRequestManager.Instance.GetCourseABPackageList((courseABData) =>
            //    {
            //        request = false;
            //        GlobalInfo.SaveCourseInfo(courseData, courseABData);
            //        GetOverviewData(callBack);
            //    }, (msg) =>
            //    {
            //        var popupDic = new Dictionary<string, PopupButtonData>();
            //        {
            //            popupDic.Add("好的", new PopupButtonData(() => ToolManager.GoToLogin(), true));
            //            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("获取失败", "获取课程失败，请重新登录", popupDic, Previous));
            //        }
            //        Log.Error($"获取课程失败！原因为：{msg}");
            //    });
            //}, failureMessage =>
            //{
            //    var popupDic = new Dictionary<string, PopupButtonData>();
            //    {
            //        popupDic.Add("好的", new PopupButtonData(() => ToolManager.GoToLogin(), true));
            //        UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("获取失败", "获取课程失败，请重新登录", popupDic, Previous));
            //    }
            //    Log.Error($"获取课程失败！原因为：{failureMessage}");
            //});
        }
    }

    private void GetOverviewData(UnityAction<bool, CourseAnchor[]> callBack)
    {
        //test
        callBack.Invoke(false, null);

        //if (GlobalInfo.overview_id != -1)
        //{
        //    NewRequestManager.Instance.GetOverview(GlobalInfo.overview_id, overviewData =>
        //    {
        //        if (overviewData == null || string.IsNullOrEmpty(overviewData.abPackageUrl))
        //        {
        //            callBack.Invoke(false, null);
        //            return;
        //        }
        //        LoadModel(GlobalInfo.overview_id.ToString(), ResManager.Instance.abDownLoadPath + overviewData.abPackageUrl, callBack, overviewData.overviewKey);
        //    }, overviewMessage =>
        //    {
        //        callBack.Invoke(false, null);
        //    });
        //}
    }

    private void LoadModel(string saveName, string url, UnityAction<bool, CourseAnchor[]> callBack, CourseAnchor[] anchors/* OverviewModelData overviewData*/)
    {
        UIManager.Instance.OpenUI<LoadingPanel2>(UILevel.PopUp, new LoadingPanel2.PanelData()
        {
            tip = "加载模型中...",
            slider = slider =>
            {
                ResManager.Instance.LoadModelAsync(saveName, url, false, false, model =>
                {
                    LoadModelResult(saveName, url, callBack, anchors, model);
                }, progress =>
                {
                    slider.value = progress;
                });
            }
        });
    }

    private void LoadModelResult(string saveName, string url, UnityAction<bool, CourseAnchor[]> callBack, CourseAnchor[] anchors, GameObject model)
    {
        if (model != null)
        {
            ModelManager.Instance.CreateModel(model);
            ModelManager.Instance.SetLightLayer(ModelManager.Instance.modelGo);
            callBack?.Invoke(true, anchors);
            UIManager.Instance.CloseUI<LoadingPanel2>();
        }
        else//下载或加载失败
        {
            UIManager.Instance.CloseUI<LoadingPanel2>();

            if (isOverview)
            {
                var popupDic = new Dictionary<string, PopupButtonData>();
                {
                    popupDic.Add("无总览模式", new PopupButtonData(() =>
                    {
                        isOverview = false;
                        callBack.Invoke(false, null);
                    }));
                    popupDic.Add("重新加载", new PopupButtonData(() => LoadModel(saveName, url, callBack, anchors), true));

                    UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "总览模型加载失败", popupDic, Previous));
                }
            }
            else
            {
                callBack.Invoke(false, null);
            }
        }
    }

    /// <summary>
    /// 退出
    /// </summary>
    private void Exit()
    {
        //if (OPLResourcesDownloadModule.DownloadingCount > 0)
        if (ResourcesDownloader.DownloadingCount > 0)
        {
            var popupDic = new Dictionary<string, PopupButtonData>();
            {
                popupDic.Add("否", new PopupButtonData(null));
                popupDic.Add("是", new PopupButtonData(BackToHomePage, true));
                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "离开会中断课程下载，确认要离开吗？", popupDic, null, false));
            }
        }
        else
        {
            BackToHomePage();
        }
    }

    private void BackToHomePage()
    {
        UIManager.Instance.CloseUI<ResourcesPanel>();
        UIManager.Instance.OpenUI<HomePagePanel>();
    }

    private void InitQuality()
    {
        Transform model = ModelManager.Instance.modelGo.transform;
        if (model == null)
            return;

        Light dirLight = model.GetChild(0).GetComponent<Light>();
        Volume globalVolume = model.GetChild(1).GetComponent<Volume>();

        switch (PlayerPrefs.GetString(GlobalInfo.qualityCacheKey))
        {
            case "Low":
                if (dirLight)
                    dirLight.enabled = false;
                if (globalVolume)
                    globalVolume.enabled = false;
                break;
            case "Middle":
                if (dirLight)
                {
                    dirLight.shadows = LightShadows.None;
                    dirLight.enabled = true;
                }
                if (globalVolume)
                    globalVolume.enabled = true;
                break;
            case "High":
                if (dirLight)
                {
                    dirLight.shadows = LightShadows.Soft;
                    dirLight.enabled = true;
                }
                if (globalVolume)
                    globalVolume.enabled = true;
                break;
        }
    }

    private void HasModel()
    {
        //var parent = this.FindChildByName("HasModel");
        //{
        //    var PopModule = parent.GetComponentByChildName<RectTransform>("PopModule");
        //    UIManager.Instance.OpenModuleUI<ResourcesDownloadModule>(this, PopModule,
        //        new ResourcesDownloadModule.ModuleData() { popup = state == -1, tag = tag, tagId = tagId, searchKeyword = searchKeyword }, FormData.ResourcesPopupModulePath);
        //    parent.gameObject.SetActive(true);
        //}

        var hadModel = transform.FindChildByName("HasModel");
        var PopModule = hadModel.GetComponentByChildName<RectTransform>("PopModule");
        UIManager.Instance.OpenModuleUI<OPLResourcesDownloadModule>(this, PopModule, new OPLResourcesDownloadModule.ModuleData()
        {
            popup = state == -1,
            category = category,
            categoryIndex = categoryIndex,
            searchKeyword = searchKeyword,
            subCategory = subCategory,
            tag = courseTag
        }, FormData.ResourcesPopupModulePath);
        hadModel.gameObject.SetActive(true);

        var NoModel = transform.GetComponentByChildName<RectTransform>("NoModel");
        NoModel.gameObject.SetActive(false);
    }

    private void NoModel()
    {
        //var NoModel = transform.GetComponentByChildName<RectTransform>("NoModel");
        //UIManager.Instance.OpenModuleUI<ResourcesDownloadModule>(this, NoModel,
        //    new ResourcesDownloadModule.ModuleData() { fullScreen = true, tag = tag, tagId = tagId, searchKeyword = searchKeyword }, FormData.ResourcesFullScreenModulePath);
        //NoModel.gameObject.SetActive(true);

        var NoModel = transform.GetComponentByChildName<RectTransform>("NoModel");
        UIManager.Instance.OpenModuleUI<OPLResourcesDownloadModule>(this, NoModel, new OPLResourcesDownloadModule.ModuleData()
        {
            fullScreen = true,
            category = category,
            categoryIndex = categoryIndex,
            searchKeyword = searchKeyword,
            subCategory = subCategory,
            tag = courseTag
        }, FormData.ResourcesFullScreenModulePath);
        NoModel.gameObject.SetActive(true);
    }

    public override void Previous()
    {
        base.Previous();
        Exit();
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
#if UNITY_ANDROID || UNITY_IOS
        ModelManager.Instance.ControlClipPlane();
#endif
        //ModelManager.Instance.ControlGlobalVolume();
        Option_GeneralModule.InitQuality();
        ModelManager.Instance.DestroyScripts(true);
        ModelManager.Instance.DestroyModels(true);
        ModelManager.Instance.ClearModelUUID();

        ResManager.Instance.StopAllDownLoad();
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        TapRecognizer.Instance?.UnRegistOnLeftMouseDoubleClick(ResetCamera);
#else
        TapRecognizer.Instance?.UnRegistOnRightMouseClick(ResetCamera);
#endif
        base.Close(uiData, callback);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }

    #region 总览部分
    /// <summary>
    /// 标签的图标
    /// </summary>
    public Sprite spriteTogSprite_Normal;
    public Sprite spriteTogSprite_Select;
    /// <summary>
    /// 操作名
    /// </summary>
    private const string SelectOperationName = "选中";
    /// <summary>
    /// 操作名
    /// </summary>
    private const string DeSelectOperationName = "取消选中";

    /// <summary>
    /// 总览模型
    /// </summary>
    private GameObject overviewModel;
    /// <summary>
    /// 所有楼层
    /// </summary>
    private List<ModelInfo> storeys;
    /// <summary>
    /// 楼层及其对应toggle字典，key-楼层,value-toggle
    /// </summary>
    private Dictionary<ModelInfo, Toggle> storeyDic = new Dictionary<ModelInfo, Toggle>();
    /// <summary>
    /// 所有锚点（标签）
    /// </summary>
    private Dictionary<string, System.Tuple<ModelInfo, List<int>>> anchors;
    /// <summary>
    /// 选中控制器
    /// </summary>
    private SelectionModel overviewSelectionModel;
    /// <summary>
    /// 详情面板
    /// </summary>
    private RectTransform Details;

    /// <summary>
    /// 初始化总览部分
    /// </summary>
    /// <param name="data"></param>
    private void InitOverview(CourseAnchor[] anchors)
    {
        AddMsg((ushort)ResourcesPanelEvent.Details);

        overviewModel = ModelManager.Instance.modelRoot.GetChild(0).gameObject;
        Details = this.GetComponentByChildName<RectTransform>("Details");

        var Mask = Details.parent.GetComponent<Button>();
        {
            Mask.onClick.AddListener(() =>
            {
                DetailsExitAnim(() =>
                {
                    Mask.gameObject.SetActive(false);
                    SendMsg(new MsgBase((ushort)SpriteTogEvent.Close));
                });
            });
        }

        InitOverviewModel(anchors);
        InitOverviewAnchor();
        InitOverviewStorey();

        var toggles = transform.FindChildByName("Toggles").GetComponent<RectTransform>();
        var posY = toggles.rect.height + 10f;
        if (isAndroid) posY = 20f;
        toggles.gameObject.SetActive(true);
        toggles.anchoredPosition = Vector2.down * posY;
        toggles.DOAnchorPosY(posY, JoinAnimePlayTime);
    }

    /// <summary>
    /// 初始化总览模型
    /// </summary>
    private void InitOverviewModel(CourseAnchor[] courseAnchors)
    {
        Dictionary<string, List<int>> cache = new Dictionary<string, List<int>>();
        {
            anchors = new Dictionary<string, System.Tuple<ModelInfo, List<int>>>();
            storeys = new List<ModelInfo>();

            foreach (var value in courseAnchors)
            {
                if (!string.IsNullOrEmpty(value.key))
                {
                    if (!cache.ContainsKey(value.key))
                    {
                        cache.Add(value.key, new List<int>());
                    }

                    cache[value.key].Add(value.courseId);
                }
            }

            //tdptd 是否可以选中 从配置端解决 客户端只负责处理 在配置端内进行判断补充目前临时处理添加canselect
            bool canSelect = false;

            foreach (ModelInfo modelInfo in overviewModel.GetComponentsInChildren<ModelInfo>())
            {
                if (modelInfo.PropType == PropType.Operate)
                {
                    ModelManager.Instance.AddModelUUID(modelInfo);

                    storeys.Add(modelInfo);

                    //if (modelInfo.InteractMode == InteractMode.Click)
                    //{
                    //    modelInfo.gameObject.AddComponent<CollisionBoxMouseEvent>();
                    //    canSelect = true;
                    //}
                }

                if (modelInfo.PropType == PropType.Anchor && cache.ContainsKey(modelInfo.ID))
                {
                    if (!anchors.ContainsKey(modelInfo.ID))
                        anchors.Add(modelInfo.ID, new System.Tuple<ModelInfo, List<int>>(modelInfo, cache[modelInfo.ID]));
                }
            }

            if (canSelect)
            {
                overviewSelectionModel = overviewModel.AddComponent<SelectionModel>();
            }
        }


        if (overviewModel.TryGetComponent(out overviewSelectionModel))
        {
            overviewSelectionModel.onSelectModel.AddListener((target, userID) =>
            {
                if (target == null)
                    return;

                if (target.TryGetComponent(out ModelInfo storey))
                {
                    if (storeyDic.ContainsKey(storey))
                    {
                        storeyDic[storey].isOn = true;
                    }
                }
            });
        }

        ModelManager.Instance.AdaptModelRestrict(overviewModel);
    }
    /// <summary>
    /// 初始化锚点（标签）
    /// </summary>
    private void InitOverviewAnchor()
    {
        if (anchors.Count <= 0)
        {
            Debug.LogWarning("没有配置锚点");
            return;
        }


        List<int> tempCourseList = new List<int>();

        foreach (string anchor in anchors.Keys.ToList())
        {
            tempCourseList.Clear();

            //移除当前学校内没有的课程
            foreach (var courseID in anchors[anchor].Item2)
            {
                if (GlobalInfo.courseDicExists.ContainsKey(courseID))
                {
                    tempCourseList.Add(courseID);
                }
            }

            if (tempCourseList.Count > 0)
            {
                anchors[anchor] = new System.Tuple<ModelInfo, List<int>>(anchors[anchor].Item1, new List<int>(tempCourseList));
                anchors[anchor].Item1.gameObject.AddComponent<SpriteTog>().SetSprite(spriteTogSprite_Normal, spriteTogSprite_Normal, spriteTogSprite_Normal, spriteTogSprite_Select);
                anchors[anchor].Item1.gameObject.AddComponent<LookAtTagert>().Init(Camera.main.transform, true, 0.1f);
            }
        }
    }
    #endregion

    #region 楼层部分
    /// <summary>
    /// 初始化楼层
    /// </summary>
    private void InitOverviewStorey()
    {
        if (storeys.Count <= 0)
        {
            this.FindChildByName("HasModel").FindChildByName("Toggles").gameObject.SetActive(false);
            Debug.LogWarning("没有配置楼层");
            return;
        }

        //排序
        storeys.OrderBy(s => int.Parse(s.ID));

        storeyDic = new Dictionary<ModelInfo, Toggle>();
        {
            int index = 0;
            this.FindChildByName("LevelContent").RefreshItemsView(storeys, (item, info) =>
            {
                item.GetComponent<Text>().text = info.Name;

                Toggle toggle = item.GetComponentInChildren<Toggle>();
                {
                    storeyDic.Add(info, toggle);

                    toggle.name = (index++).ToString();

                    toggle.onValueChanged.RemoveAllListeners();
                    toggle.onValueChanged.AddListener((isOn) =>
                    {
                        if (isOn)
                        {
                            state = int.Parse(toggle.name);

                            CloseAllModel();
                            ShowOneModel(info);

                            if (overviewSelectionModel)
                            {
                                overviewSelectionModel.SetSelect(info.gameObject);
                            }
                        }

                        toggle.interactable = !isOn;
                    });
                }
            });
        }

        var All = this.GetComponentByChildName<Toggle>("All");
        {
            All.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    CloseAllModel();
                    ShowAllModel();

                    if (overviewSelectionModel)
                    {
                        overviewSelectionModel.SetSelect(null);
                    }
                }
            });
            All.isOn = true;
            All.group.allowSwitchOff = false;
        }

    }
    /// <summary>
    /// 显示单一层
    /// </summary>
    /// <param name="storey">层物体</param>
    public void ShowOneModel(ModelInfo storey)
    {
        //设置标签显示
        foreach (ModelInfo Child in storey.GetComponentsInChildren<ModelInfo>(true))
        {
            if (Child.PropType == PropType.Anchor)
            {
                Child.gameObject.SetActive(true);
            }
        }

        //ModelManager.Instance.ResetCamera();

        if (storey.TryGetComponent(out ModelOperation modelOperation))
        {
            if (modelOperation.GetOperations().TryGetValue(SelectOperationName, out OperationBase operation))
            {
                foreach (BehaveBase behaveBase in operation.behaveBases)
                {
                    behaveBase.Execute();
                }
            }
        }

        //隐藏collider
        BoxCollider boxCollider = storey.GetComponent<BoxCollider>();
        if (boxCollider)
            boxCollider.enabled = false;

        ModelManager.Instance.AdaptModelRestrict(storey.gameObject);
    }
    /// <summary>
    /// 显示所有层
    /// </summary>
    public void ShowAllModel()
    {
        foreach (var anchor in anchors.Values)
        {
            anchor.Item1.gameObject.SetActive(true);
        }

        //ModelManager.Instance.ResetCamera();

        if (overviewModel.TryGetComponent(out ModelOperation modelOperation))
        {
            if (modelOperation.GetOperations().TryGetValue(SelectOperationName, out OperationBase operation))
            {
                foreach (BehaveBase behaveBase in operation.behaveBases)
                {
                    behaveBase.Execute();
                }
            }
        }

        ModelManager.Instance.AdaptModelRestrict(overviewModel);
    }
    /// <summary>
    /// 关闭所有层
    /// </summary>
    public void CloseAllModel()
    {
        foreach (var anchor in anchors.Values)
        {
            anchor.Item1.gameObject.SetActive(false);
        }

        foreach (ModelInfo storey in storeyDic.Keys)
        {
            if (storey.TryGetComponent(out ModelOperation modelOperation))
            {
                if (modelOperation.GetOperations().TryGetValue(DeSelectOperationName, out OperationBase operation))
                {
                    foreach (BehaveBase behaveBase in operation.behaveBases)
                    {
                        behaveBase.Execute();
                    }
                }
            }

            BoxCollider boxCollider = storey.GetComponent<BoxCollider>();
            if (boxCollider)
                boxCollider.enabled = true;
        }
    }
    #endregion

    #region Mask进出场动画
    /// <summary>
    /// Details进场动画
    /// </summary>
    /// <param name="callback">回调</param>
    private void DetailsJoinAnim(UnityAction callback = null)
    {
        if (Details != null)
        {
            SendMsg(new MsgBase((ushort)UIAnimEvent.ShowAnimMask));
            var co = Details.GetComponent<Image>().color;
            Details.GetComponent<Image>().color = new Color(co.r, co.g, co.b, 0);
            Details.GetComponent<Image>().DOColor(new Color(co.r, co.g, co.b, 1), ExitAnimePlayTime);

            Details.transform.localScale = Vector3.one * 0.001f;
            Details.transform.DOScale(Vector3.one, JoinAnimePlayTime).OnComplete(() =>
            {
                SendMsg(new MsgBase((ushort)UIAnimEvent.HideAnimMask));
                callback();
            });
        }
    }

    /// <summary>
    /// Details退场动画
    /// </summary>
    /// <param name="callback">回调</param>
    private void DetailsExitAnim(UnityAction callback = null)
    {
        if (Details != null)
        {
            SendMsg(new MsgBase((ushort)UIAnimEvent.ShowAnimMask));
            var co = Details.GetComponent<Image>().color;
            Details.GetComponent<Image>().DOColor(new Color(co.r, co.g, co.b, 0), ExitAnimePlayTime);
            Details.transform.DOScale(Vector3.one * 0.001f, ExitAnimePlayTime).OnComplete(() =>
            {
                SendMsg(new MsgBase((ushort)UIAnimEvent.HideAnimMask));
                callback();
            });
        }
    }

    public override void JoinAnim(UnityAction callback)
    {
        var exit = this.FindChildByName("Exit").GetComponent<RectTransform>();
        float offset = 16f;
#if UNITY_ANDROID || UNITY_IOS
        offset = 112f;
#endif
        var posX = exit.rect.width + offset;
        exit.gameObject.SetActive(true);
        exit.anchoredPosition = new Vector2(-posX, exit.anchoredPosition.y);
        JoinSequence.Append(exit.DOAnchorPosX(offset, JoinAnimePlayTime));
        base.JoinAnim(callback);
    }

    #endregion

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);

        switch (msg.msgId)
        {
            case (ushort)ResourcesPanelEvent.SelectCourse:
                state = -1;
                UIManager.Instance.CloseUI<ResourcesPanel>();
                UIManager.Instance.OpenUI<OPLCoursePanel>(UILevel.Normal, new ABPanelInfo(((MsgInt)msg).arg, typeof(ResourcesPanel).ToString()));
                break;
            case (ushort)ResourcesPanelEvent.Details:
                MsgStringVector2 data = msg as MsgStringVector2;
                {
                    Course target = null;

                    Details.anchoredPosition = data.vec;
                    Details.FindChildByName("Content").RefreshItemsView(anchors[data.arg].Item2, (item, info) =>
                    {
                        var Title = item.GetComponentInChildren<Text>(true);
                        {
                            if(GlobalInfo.courseDicExists.TryGetValue(info, out target))
                            {
                                Title.EllipsisText(target.name, "...");
                            }
                        }

                        var Button = item.GetComponentInChildren<Button>(true);
                        {
                            Button.onClick.RemoveAllListeners();
                            Button.GetComponent<CanvasGroup>().alpha = 0.5f;

                            // RequestManager.Instance.GetBaikeInfo(target.id, data =>
                            if (GlobalInfo.courseDicExists.TryGetValue(target.id, out Course course))
                            {
                                GlobalInfo.courseABDic.TryGetValue(target.id, out List<CourseABPackage> data);

                                ResManager.Instance.CheckUpdate(data, result =>
                                {
                                    if (result == 0)
                                    {
                                        Button.GetComponent<CanvasGroup>().alpha = 1f;
                                        Button.onClick.AddListener(() =>
                                        {
                                            GlobalInfo.currentCourseInfo = target;
                                            UIManager.Instance.CloseUI<ResourcesPanel>();
                                            UIManager.Instance.OpenUI<OPLCoursePanel>(UILevel.Normal, new ABPanelInfo(info, typeof(ResourcesPanel).ToString()));
                                        });
                                    }
                                    else
                                    {
                                        Button.onClick.AddListener(() =>
                                        {
                                            UIManager.Instance.OpenModuleUI<ToastPanel>(this, UILevel.PopUp, new ToastPanelInfo("资源未下载，请下载后重试"));
                                        });
                                    }
                                });
                            }
                            //}, failureMessage =>
                            //{
                            //    Log.Error($"获取百科失败！原因为：{failureMessage}");
                            //});
                        }
                    });
                }
                Details.parent.gameObject.SetActive(true);
                DetailsJoinAnim();

                this.WaitTime(0.001f, RefreshLayouGroup);
                break;
        }
    }
}