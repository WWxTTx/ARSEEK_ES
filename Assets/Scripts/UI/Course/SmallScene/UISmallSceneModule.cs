using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Interop;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;

public enum ModelState
{
    /// <summary>
    /// 未选中
    /// </summary>
    Unselect,
    /// <summary>
    /// 聚焦中
    /// </summary>
    Focusing,
    /// <summary>
    /// 已聚焦
    /// </summary>
    Focused,
    /// <summary>
    /// 已选中
    /// </summary>
    Select,
    /// <summary>
    /// 操作中
    /// </summary>
    Operating,
    /// <summary>
    /// 他人操作中
    /// </summary>
    OtherOperating,
    /// <summary>
    /// 操作完成
    /// </summary>
    Operated,
}

public class SmallSceneData : UIData
{
    /// <summary>
    /// 任务
    /// </summary>
    public List<Flow> flows { get; set; }

    public SmallSceneData(List<Flow> flows)
    {
        this.flows = flows;
    }
}

/// <summary>
/// 模拟操作模块
/// </summary>
public class UISmallSceneModule : UIModuleBase
{
    #region 模型
    /// <summary>
    /// 角色预制体
    /// </summary>
    public GameObject PlayerPrefab;
    /// <summary>
    /// 场景预制体
    /// </summary>
    public GameObject ScenePrefab;
    /// <summary>
    /// 模拟操作控制器
    /// </summary>
    public SmallFlowCtrl smallFlowCtrl;
    /// <summary>
    /// 仿真系统
    /// </summary>
    public BaseSimuSystem simuSystem;
    public BaseMasterComputer masterComputer;
    /// <summary>
    /// 手机版滑动视野方向的射线接收
    /// </summary>
    public Image RoteInput;
    /// <summary>
    /// 控制角色
    /// </summary>
    private PlayerController playerController;
    /// <summary>
    /// 提示高亮操作模型
    /// </summary>
    private HashSet<Component> highlights = new HashSet<Component>();
    /// <summary>
    /// 选中高亮模型
    /// </summary>
    private ModelOperation _modelHighlight;
    private ModelOperation modelOperation_Highlight
    {
        get { return _modelHighlight; }
        set
        {
            if (_modelHighlight != null && _modelHighlight != value && _modelHighlight != modelOperation_Focused)
            {
                smallFlowCtrl.RemoveHint(_modelHighlight, 1);
            }

            _modelHighlight = value;

            smallFlowCtrl.AddHint(_modelHighlight, 1);
        }
    }

    /// <summary>
    /// 聚焦操作模型
    /// </summary>
    private ModelOperation _modelFocused;
    private ModelOperation modelOperation_Focused
    {
        get { return _modelFocused; }
        set
        {
            bool valueChanged = value != _modelFocused;
            string newFocusedId = string.Empty;
            if (valueChanged && _modelFocused != null)
            {
                // 取消聚焦时，移除选中高亮
                smallFlowCtrl.RemoveHint(_modelFocused, 1);
                //// 中断正在执行的聚焦操作
                //smallFlowCtrl.AbortOperation(_modelFocused, SmallFlowCtrl.focusFlag);
            }

            _modelFocused = value;

            if (_modelFocused != null)
            {
                newFocusedId = _modelFocused.GetComponent<ModelInfo>().ID;
            }
            if (valueChanged)
            {
                if (focusHint)
                {
                    focusHint.alpha = 0;
                    focusHint.blocksRaycasts = false;
                }

                if (_modelFocused != null)
                    focusMasterComputerDescrption = string.Empty;

                ToolManager.SendBroadcastMsg(new MsgStringString((ushort)SmallFlowModuleEvent.FocusChanged, FocusModelDescrption, newFocusedId), true);
            }
        }
    }

    private string focusMasterComputerDescrption = string.Empty;

    public string FocusModelDescrption
    {
        get
        {
            if (modelOperation_Focused == null)
            {
                if(!string.IsNullOrEmpty(focusMasterComputerDescrption))
                    return $"{focusMasterComputerDescrption}：";
                else
                    return string.Empty;
            }

            var modelInfo = modelOperation_Focused.transform.GetComponent<ModelInfo>();
            return string.Format("{0}{1}：", modelInfo.Name, string.IsNullOrEmpty(modelInfo.Code) ? string.Empty : $"({modelInfo.Code})");
        }
    }

    /// <summary>
    /// 选中操作模型
    /// </summary>
    private ModelOperation modelOperation_Select;
    /// <summary>
    /// 抓取的道具
    /// </summary>
    private ModelInfo prop;

    /// <summary>
    /// 主相机下道具节点
    /// </summary>
    private Transform ToolNode;
    /// <summary>
    /// 用户操作道具（多人协作）
    /// </summary>
    private Dictionary<ModelOperation, int> userOpModel = new Dictionary<ModelOperation, int>();
    #endregion

    #region 无角色
    private CameraMove cameraMove;
    private CameraMove CameraMove => cameraMove == null ? cameraMove = Camera.main.AutoComponent<CameraMove>() : cameraMove;
    private CameraRotate cameraRotate;
    private CameraRotate CameraRotate => cameraRotate == null ? cameraRotate = Camera.main.AutoComponent<CameraRotate>() : cameraRotate;
    private CameraZoom cameraZoom;
    private CameraZoom CameraZoom => cameraZoom == null ? cameraZoom = Camera.main.AutoComponent<CameraZoom>() : cameraZoom;
    #endregion

    #region 移动端
    /// <summary>
    /// 第一人称按钮
    /// </summary>
    private Button FirstPersonBtn;
    /// <summary>
    /// 第三人称按钮
    /// </summary>
    private Button ThirdPersonBtn;
    /// <summary>
    /// 移动端模拟锁定
    /// </summary>
    protected bool fakeCursorLocked;
    /// <summary>
    /// 移动端角色移动系数
    /// </summary>
    public float mobileMoveRatio;
    /// <summary>
    /// 移动端角色旋转系数
    /// </summary>
    public float mobleRotateRatio;

    private int playerLayerMask;
    #endregion

    /// <summary>
    /// 全局视角切换按钮
    /// </summary>
    private Button GlobalBtn;
    /// <summary>
    /// 聚焦模型提示UI
    /// </summary>
    private CanvasGroup focusHint;
    private Button CancelFocusBtn;

    private RectTransform rect;
    /// <summary>
    /// 焦点
    /// </summary>
    private Image Focus;
    /// <summary>
    /// 焦点正常状态
    /// </summary>
    public Sprite normal;
    /// <summary>
    /// 焦点选中可操作物体状态
    /// </summary>
    public Sprite select;
    /// <summary>
    /// 焦点选中道具状态
    /// </summary>
    private Image ToolSprite;

    /// <summary>
    /// 焦点选中可操作物体显示文字最大长度
    /// </summary>
    private int nameMax = 16;
    /// <summary>
    /// 焦点射线
    /// </summary>
    private Ray ray;
    /// <summary>
    /// 焦点射线碰撞信息
    /// </summary>
    private RaycastHit hitInfo;
    /// <summary>
    /// 可操作模型所在层
    /// </summary>
    private int modelLayerMask;
    /// <summary>
    /// 错误提示
    /// </summary>
    private Image error;
    /// <summary>
    /// 监控ui
    /// </summary>
    public RectTransform CameraView;
    /// <summary>
    /// 监控显示
    /// </summary>
    private RawImage CameraViewRawImage;
    private Transform activeMonitorCam;

    /// <summary>
    /// 工具栏
    /// </summary>
    public UISmallSceneToolModule toolModule;
    /// <summary>
    /// 任务栏
    /// </summary>
    public UISmallSceneFlowModule flowModule;
    /// <summary>
    /// 操作历史记录
    /// </summary>
    public UISmallSceneOperationHistory operationHistoryModule;

    /// <summary>
    /// 是否切换鼠标模式
    /// false:光标锁定在游戏窗口的中心
    /// </summary>
    private bool isAlt;
    /// <summary>
    /// 是否打开大地图
    /// </summary>
    private bool inMap;

    private bool isMouseDown = false;
    /// <summary>
    /// 鼠标拖动阈值, 超过该值判断为拖动，不会触发点击事件
    /// </summary>
    private float dragThreshold = 0.01f;
    private Vector3 lastMousePosition;

    private ModelState _modelState;
    public ModelState ModelState
    {
        get { return _modelState; }
        set
        {
            _modelState = value;

            if (GlobalBtn)
                GlobalBtn.interactable = smallFlowCtrl.globalPerspective != null && _modelState != ModelState.Operating && _modelState != ModelState.Focusing;

            switch (_modelState)
            {
                case ModelState.Unselect:
                    modelOperation_Highlight = modelOperation_Focused = modelOperation_Select = null;
                    EnableCameraControl(true);
                    SetSelect(false);
                    ModelManager.Instance.AdaptModelRestrict();
                    break;
                case ModelState.Focusing:
                    modelOperation_Select = null;
                    EnableCameraControl(false);
                    break;
                case ModelState.Focused:
                    if (focusHint && modelOperation_Focused != null)
                    {
                        focusHint.GetComponentInChildren<Text>().text = modelOperation_Focused.GetComponent<ModelInfo>().Name;
                        focusHint.alpha = 1;
                        focusHint.blocksRaycasts = true;
                    }
                    ModelManager.Instance.AdaptModelRestrict(modelOperation_Focused?.gameObject);
                    EnableCameraControl(true);
                    break;
                case ModelState.Select:
                    SetSelect(true);
                    break;
                case ModelState.Operating:
                    EnableCameraControl(false);
                    break;
                case ModelState.Operated:
                    modelOperation_Highlight = modelOperation_Focused;
                    ModelState = modelOperation_Focused == null ? ModelState.Unselect : ModelState.Focused;
                    break;
            }
        }
    }

    /// <summary>
    /// 当前是否为可操作的状态
    /// </summary>
    public bool IsOperatableState
    {
        get
        {
            return !ModelManager.Instance.CameraDotween && !isExecuteOperation &&
                (ModelState == ModelState.Unselect
                || ModelState == ModelState.Focused
                || ModelState == ModelState.OtherOperating
                || (ModelState == ModelState.Operating && modelOperation_Select?.GetComponent<ModelInfo>()?.InfoData?.InteractMode == InteractMode.Menu2D));//todo 上位机操作
        }
    }


    public bool OtherOperating
    {
        get
        {
            return userOpModel.Any(item =>
                item.Key != null &&
                item.Value != GlobalInfo.account.id &&
                NetworkManager.Instance.IsUserOnline(item.Value));
        }
    }

    /// <summary>
    /// 是否正在执行操作（初始视角导航）
    /// </summary>
    private bool isExecuteOperation = false;

    [HideInInspector]
    public string FatalFinishMessage;
    /// <summary>
    /// 当前任务是否异常结束
    /// </summary>
    public bool FatalFinish => !string.IsNullOrEmpty(FatalFinishMessage);

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        //只有培训模式打开完整提示词
        if (CourseMode.Training == GlobalInfo.courseMode)
            UIManager.Instance.OpenModuleUI<UISmallSceneInfoModule>(ParentPanel, transform, null);

        AddMsg(new ushort[]{
             ushort.MaxValue,
            (ushort)OperationListEvent.Open,
            (ushort)OperationListEvent.Hide,
            (ushort)HistoryEvent.Open,
            (ushort)SmallFlowModuleEvent.LeftFlex,
            (ushort)SmallFlowModuleEvent.RightFlex,
            (ushort)SmallFlowModuleEvent.SelectFlow,
            (ushort)SmallFlowModuleEvent.SelectStep,
            (ushort)SmallFlowModuleEvent.Guide,
            (ushort)SmallFlowModuleEvent.SelectTool,
            (ushort)SmallFlowModuleEvent.SelectInput,
            (ushort)SmallFlowModuleEvent.SelectContact,
            (ushort)SmallFlowModuleEvent.Operate2D,
            (ushort)SmallFlowModuleEvent.FocusChanged,
            (ushort)SmallFlowModuleEvent.ClickObj,
            (ushort)SmallFlowModuleEvent.Look2D,
            (ushort)SmallFlowModuleEvent.Operate,
            (ushort)SmallFlowModuleEvent.MasterComputerSelect,
            (ushort)SmallFlowModuleEvent.CompleteStep,
            (ushort)SmallFlowModuleEvent.CompleteExecute,
            (ushort)SmallFlowModuleEvent.CompleteAll,
            (ushort)SmallFlowModuleEvent.HideMonitor,
            (ushort)SmallFlowModuleEvent.MaxMap,
            (ushort)SmallFlowModuleEvent.OperatingRecord,
            (ushort)SmallFlowModuleEvent.OpenCameraOperation,
            (ushort)SmallFlowModuleEvent.CloseCameraOperation,
            (ushort)RoomChannelEvent.UpdateControl,
            (ushort)RoomChannelEvent.OtherLeave,
            (ushort)SmallFlowModuleEvent.StartExecute
        });
        UniversalRenderPipelineUtils.SetRendererFeatureActive("ScreenSpaceAmbientOcclusion", false);

        InitModel(uiData);
        Init();
    }

    void InitModel(UIData uiData = null)
    {
        //隐藏最外层collider(VR端抓取用);
        if (ModelManager.Instance.modelGo.TryGetComponent(out BoxCollider boxCollider))
            boxCollider.enabled = false;

        playerLayerMask = ~(1 << LayerMask.NameToLayer("Player"));

        smallFlowCtrl = ModelManager.Instance.modelGo.AddComponent<SmallFlowCtrl>();
        simuSystem = ModelManager.Instance.modelGo.GetComponentInChildren<BaseSimuSystem>();
        masterComputer = ModelManager.Instance.modelGo.GetComponentInChildren<BaseMasterComputer>(true);

        GlobalInfo.EnableFlow = simuSystem == null;


        if (uiData != null)
        {
            SmallSceneData data = uiData as SmallSceneData;
            if (data.flows != null)
                smallFlowCtrl.Init(!GlobalInfo.IsExamMode());//todo强制引导视角
            else
            {
                smallFlowCtrl.Init(!GlobalInfo.IsExamMode());
                SaveFlowStepName();
            }
        }
        else
        {
            smallFlowCtrl.Init(!GlobalInfo.IsExamMode());
            SaveFlowStepName();
        }

        // 仿真系统初始化
        if (simuSystem != null)
        {
            simuSystem.Init(this, smallFlowCtrl);
            if(smallFlowCtrl.globalPerspective != null)
                smallFlowCtrl.SetFinalState(smallFlowCtrl.globalPerspective, smallFlowCtrl.globalPerspective.initState);
            //smallFlowCtrl.SwitchToGlobalPerspective();
            //考核 通知模块初始化完成
            FormMsgManager.Instance.SendMsg(new MsgBase((ushort)SmallFlowModuleEvent.CompleteStep));
        }

        ToolNode = Camera.main.transform.FindChildByName("ToolNode");
        Transform modelRoot = ModelManager.Instance.modelRoot;

        //根据配置设置有无漫游模式
        if (GlobalInfo.hasRole)
        {
            //漫游模式 使用预制体灯光
            ModelManager.Instance.ControlSceneLight(false);

            playerController = modelRoot.GetComponentInChildren<PlayerController>();
            if (playerController == null)
            {
                playerController = Instantiate(PlayerPrefab, modelRoot).GetComponent<PlayerController>();
                playerController.gameObject.layer = LayerMask.NameToLayer("Player");
                Instantiate(ScenePrefab, modelRoot);
            }
            ModelManager.Instance.AddSyncComponent(playerController.gameObject);
        }

        //考核去掉模型提示高亮，0提示高亮，1射线选中高亮
        if (GlobalInfo.IsExamMode())
            HighlightEffectManager.Instance.maskPriorityList.Add(0);
        else
            HighlightEffectManager.Instance.maskPriorityList.Clear();
    }

    void SaveFlowStepName()
    {
        EncyclopediaOperation encyclopediaModel = GlobalInfo.currentWiki as EncyclopediaOperation;
        encyclopediaModel.flows = new List<Flow>();
        for (int i = 0; i < smallFlowCtrl.flows.Length; i++)
        {
            SmallFlow1 smallFlow = smallFlowCtrl.flows[i];
            Flow flow = new Flow() { id = smallFlow.ID, title = smallFlow.flowName };
            flow.children = new List<Step>();
            for (int j = 0; j < smallFlow.steps.Count; j++)
            {
                SmallStep1 smallStep = smallFlow.steps[j];
                Step step = new Step() { id = smallStep.ID, title = smallStep.hint };
                flow.children.Add(step);
            }
            encyclopediaModel.flows.Add(flow);
        }

        //TODO 编辑文本替换缓存并把所有任务和步骤数据转化字符串
        RequestManager.Instance.ChangeStepNodeName(GlobalInfo.currentWiki.id, JsonTool.Serializable(encyclopediaModel.flows), () =>
        {
            Debug.Log("任务和步骤文本保存成功");
        }, (code, msg) =>
        {
            Debug.Log("任务和步骤文本保存失败，原因：" + msg);
        });
    }

    void Init()
    {
        TapRecognizer.Instance.RegistOnRightMouseClick(() =>
        {
            if (ModelState == ModelState.Focused)
            {
                ModelState = ModelState.Unselect;
                RefreshHighlight();
            }
        });

        TapRecognizer.Instance.RegistOnRightMouseDoubleClick(() =>
        {
            if (playerController == null)
                return;

            if (Cursor.lockState != CursorLockMode.None)
            {
                EnableCameraControl(false);
                SetSelect(true);
            }
            else if (isAlt && ModelState != ModelState.Operating && !isExecuteOperation)
            {
                EnableCameraControl(true);
                SetSelect(false);
            }
        });

        modelLayerMask = LayerMask.GetMask("Default") | LayerMask.GetMask("Model");

        rect = GetComponent<RectTransform>();

        Focus = transform.GetComponentByChildName<Image>("Focus");
        Focus.sprite = normal;
        Focus.transform.GetChild(0).gameObject.SetActive(false);

        #region 全局视角
        GlobalBtn = transform.GetComponentByChildName<Button>("Global");
        if (GlobalBtn)
        {
            GlobalBtn.onClick.AddListener(() =>
            {
                ModelState = ModelState.Unselect;
                smallFlowCtrl.SwitchToGlobalPerspective();
            });
            GlobalBtn.gameObject.SetActive(smallFlowCtrl.globalPerspective != null);
        }

        focusHint = transform.GetComponentByChildName<CanvasGroup>("FocusHint");
        focusHint.alpha = 0;
        focusHint.blocksRaycasts = false;
        CancelFocusBtn = focusHint.GetComponentByChildName<Button>("CancelFocus");
        CancelFocusBtn.onClick.AddListener(() =>
        {
            if (IsOperatableState)
            {
                ModelState = ModelState.Unselect;
                RefreshHighlight();
            }
        });
        #endregion

        error = transform.GetComponentByChildName<Image>("Error");
        error.color = new Color(error.color.r, error.color.g, error.color.b, 0);

        CameraView = transform.GetComponentByChildName<RectTransform>("CameraView");
        CameraViewRawImage = CameraView.GetComponentInChildren<RawImage>();

#if UNITY_ANDROID || UNITY_IOS
        Button FocusBtn = Focus.GetComponentInChildren<Button>(true);
        FocusBtn.onClick.AddListener(() =>
        {
            if (IsOperatableState)
            {
                OnModelClicked(modelOperation_Highlight);
            }
        });
        fakeCursorLocked = true;
#endif

        if (playerController)
        {
#if UNITY_ANDROID || UNITY_IOS
            Joystick moveJoystick = transform.GetComponentByChildName<Joystick>("MoveJoystick");
            Joystick rotateJoystick = transform.GetComponentByChildName<Joystick>("RotateJoystick");
            moveJoystick.gameObject.SetActive(true);
            rotateJoystick.gameObject.SetActive(true);
            playerController.SetJoystick(moveJoystick, rotateJoystick, mobileMoveRatio, mobleRotateRatio);

            FirstPersonBtn = transform.GetComponentByChildName<Button>("FirstPersonBtn");
            ThirdPersonBtn = transform.GetComponentByChildName<Button>("ThirdPersonBtn");

            FirstPersonBtn.onClick.AddListener(() =>
            {
                if (ModelState != ModelState.Unselect)
                    return;
                playerController.ToFirst();
                FirstPersonBtn.gameObject.SetActive(false);
                ThirdPersonBtn.gameObject.SetActive(true);
            });

            ThirdPersonBtn.onClick.AddListener(() =>
            {
                if (ModelState != ModelState.Unselect)
                    return;
                playerController.ToThird();
                ThirdPersonBtn.gameObject.SetActive(false);
                FirstPersonBtn.gameObject.SetActive(true);
            });

            FirstPersonBtn.gameObject.SetActive(false);
            ThirdPersonBtn.gameObject.SetActive(true);
#endif

#if UNITY_ANDROID || UNITY_IOS
#else
            if (!GlobalInfo.ShowPopup)
                Cursor.lockState = CursorLockMode.Locked;//鼠标锁定屏幕中心
            GlobalInfo.CursorLockMode = CursorLockMode.Locked;
#endif
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;//鼠标不锁定
            GlobalInfo.CursorLockMode = CursorLockMode.None;

            Camera mainCam = Camera.main;
            cameraMove = mainCam.AutoComponent<CameraMove>();
            cameraRotate = mainCam.AutoComponent<CameraRotate>();
            cameraZoom = mainCam.AutoComponent<CameraZoom>();

            ModelManager.Instance.AdaptModelRestrict();
            //默认居中显示
            //ModelManager.Instance.RevertCameraPose();
            //ModelManager.Instance.ResetCameraPose(true);
        }

        toolModule = (UISmallSceneToolModule)UIManager.Instance.OpenModuleUI<UISmallSceneToolModule>(ParentPanel, transform.parent);

        WarnTog = transform.GetComponentByChildName<Toggle>("Warn");
        WarnList = transform.FindChildByName("WarnList");
        if (WarnTog)
        {
            WarnTog.onValueChanged.AddListener((value) =>
            {
                WarnList.gameObject.SetActive(value);
                if (masterComputer && masterComputer.WarnToggle != null)
                    masterComputer.WarnToggle.SetIsOnWithoutNotify(value);
            });
        }

        if (masterComputer != null)
        {
            masterComputer.transform.SetParent(transform);
            masterComputer.transform.SetAsLastSibling();
            masterComputer.transform.localScale = Vector3.one;
            masterComputer.transform.localPosition = Vector3.zero;
            masterComputer.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            masterComputer.GetComponent<RectTransform>().offsetMax = Vector2.zero;

            WarnTog.gameObject.SetActive(true);
            WarnList.SetSiblingIndex(masterComputer.transform.GetSiblingIndex() + 1);
        }

        if (playerController && smallFlowCtrl.naviPoints.Count > 0)
        {
            UIManager.Instance.OpenModuleUI<UISmallSceneMinMapModule>(ParentPanel, transform.parent,
                new MinMapData(smallFlowCtrl.orthographicSize, smallFlowCtrl.naviPoints, playerController));
        }

        ToolSprite = transform.GetComponentByChildName<Image>("ToolSprite");
        InitSafetyToolsSprite();

        //打开流程列表模块,根据课程和考核分别设置左侧显示按钮
        SendMsg(new MsgBase((ushort)CoursePanelEvent.OperationListBtn));
    }

    void Update()
    {
        if (inMap || Focus == null)
            return;

        if (playerController == null)
        {
#if UNITY_ANDROID || UNITY_IOS
            #region 移动端
            //移动端操作
            if (/*Input.GetMouseButtonDown(0) && !GUITool.IsOverGUI(Input.mousePosition) && */IsOperatableState)
            {
                Focus.gameObject.SetActive(true);
                ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f)); /*Camera.main.ScreenPointToRay(Input.mousePosition);*/
                //新增限制射线检查距离，以避免穿透当前设备检查到远处的目标
                if (Physics.Raycast(ray, out hitInfo, 5/*Mathf.Infinity*/, modelLayerMask) && hitInfo.transform.GetComponent<ModelOperation>() != null)
                {
                    RaySelect();
                }
                else
                {
                    UnRaySelect();
                }
            }
            else//鼠标射线在UI上或正在操作时
                Focus.gameObject.SetActive(false);
            #endregion
#else
            #region PC端
            if (rect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, Input.mousePosition, UIManager.Instance.canvas.worldCamera, out Vector2 position))
            {
                Focus.rectTransform.anchoredPosition = position;
            }
            if (!GUITool.IsOverGUI(Input.mousePosition) && IsOperatableState)
            {
                Focus.gameObject.SetActive(true);
                ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                //新增限制射线检查距离，以避免穿透当前设备检查到远处的目标
                if (Physics.Raycast(ray, out hitInfo, 5/*Mathf.Infinity*/, modelLayerMask) && hitInfo.transform.GetComponent<ModelOperation>() != null)
                {
                    RaySelect();
                    if (Input.GetMouseButtonDown(0) && IsOperatableState)
                    {
                        lastMousePosition = Input.mousePosition;
                        isMouseDown = true;
                    }
                    if (isMouseDown && Input.GetMouseButtonUp(0))
                    {
                        isMouseDown = false;
                        if (IsOperatableState && Vector3.Distance(lastMousePosition, Input.mousePosition) < dragThreshold)
                        {
                            OnModelClicked(modelOperation_Highlight);
                        }
                    }
                }
                else
                {
                    UnRaySelect();
                    isMouseDown = false;
                }
            }
            else//鼠标射线在UI上或正在操作时
                Focus.gameObject.SetActive(false);
            #endregion
#endif
        }
        else
        {
#if UNITY_ANDROID || UNITY_IOS
            if (fakeCursorLocked)
#else
            if (Cursor.lockState != CursorLockMode.None)
#endif
            {
                ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f));
                if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, modelLayerMask) && hitInfo.transform.GetComponent<ModelOperation>() != null)
                {
                    RaySelect();
#if UNITY_STANDALONE
                    if (Input.GetMouseButtonDown(0) && IsOperatableState)
                    {
                        lastMousePosition = Input.mousePosition;
                        isMouseDown = true;
                    }
                    if (isMouseDown && Input.GetMouseButtonUp(0))
                    {
                        isMouseDown = false;
                        if (IsOperatableState && Vector3.Distance(lastMousePosition, Input.mousePosition) < dragThreshold)
                        {
                            OnModelClicked(modelOperation_Highlight);
                        }
                    }
#endif
                }
                else
                {
                    UnRaySelect();
                    isMouseDown = false;
                }
            }
        }
    }

    /// <summary>
    /// 选中高亮，显示名称、编号、状态等
    /// </summary>
    void RaySelect()
    {
        var rayResult = hitInfo.transform.GetComponent<ModelOperation>();

        if (rayResult == modelOperation_Focused)
        {
            modelOperation_Highlight = rayResult;

            Focus.sprite = normal;
            Focus.transform.GetChild(0).gameObject.SetActive(false);
            return;
        }

        //已经显示操作UI的操作对象不能重复点击
        if (rayResult != modelOperation_Highlight && rayResult != modelOperation_Select)
        {
            modelOperation_Highlight = rayResult;

            Focus.sprite = select;
            Focus.transform.GetChild(0).gameObject.SetActive(true);

            var hitModelInfo = hitInfo.transform.GetComponent<ModelInfo>();
            string modelName = hitModelInfo.Name;
            //if (modelName.Length > nameMax)
            //    modelName = modelName.Substring(0, nameMax - 1) + "...";

            if (!SmallFlowCtrl.maskOperation.Contains(modelOperation_Highlight.currentState))
            {
                if (string.IsNullOrEmpty(hitModelInfo.Code))
                    Focus.GetComponentInChildren<Text>().text = $"<color=#D0D0D0>名称：</color>{modelName}\n<color=#D0D0D0>状态：</color>{modelOperation_Highlight.currentState}";
                else
                    Focus.GetComponentInChildren<Text>().text = $"<color=#D0D0D0>名称：</color>{modelName}\n<color=#D0D0D0>编号：</color>{hitModelInfo.Code}\n<color=#D0D0D0>状态：</color>{modelOperation_Highlight.currentState}";
            }
            else
            {
                if (string.IsNullOrEmpty(hitModelInfo.Code))
                    Focus.GetComponentInChildren<Text>().text = $"<color=#D0D0D0>名称：</color>{modelName}";
                else
                    Focus.GetComponentInChildren<Text>().text = $"<color=#D0D0D0>名称：</color>{modelName}\n<color=#D0D0D0>编号：</color>{hitModelInfo.Code}";
            }
        }
    }

    /// <summary>
    /// 取消选中高亮
    /// </summary>
    void UnRaySelect()
    {
        Focus.sprite = normal;
        Focus.transform.GetChild(0).gameObject.SetActive(false);

        //聚焦物体时，高亮效果不消失
        if (modelOperation_Highlight != modelOperation_Focused)
        {
            modelOperation_Highlight = null;
        }
    }

    /// <summary>
    /// 点击操作对象
    /// 1.进入局部视角
    /// 2.已进入，同原操作流程
    /// </summary>
    private void OnModelClicked(ModelOperation modelOperation)
    {
        if (modelOperation == null)
            return;

        if (FatalFinish)
        {
            ShowFatalPopup();
            return;
        }

        if (OperatePermissionOccupied(modelOperation, string.Empty) || OtherOperating)
        {
            UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("其他成员正在操作"));
            return;
        }

        var focusOperation = modelOperation.operations.FirstOrDefault(o => o.name.Equals(SmallFlowCtrl.focusFlag));
        var modelInfo = modelOperation.GetComponent<ModelInfo>();

        //操作对象配置了聚焦操作，且当前未聚焦
        //if (focusOperation != null && modelOperation_Focused != modelOperation)
        //{
        //    ModelState = ModelState.Focusing;
        //    modelOperation_Focused = modelOperation;

        //    FormMsgManager.Instance.SendMsg(new MsgStringBool((ushort)SmallFlowModuleEvent.StartExecute, modelInfo.ID, false));
        //    smallFlowCtrl.ExecuteOperation(modelOperation_Focused, SmallFlowCtrl.focusFlag, null, (_) =>
        //    {
        //        FormMsgManager.Instance.SendMsg(new MsgString((ushort)SmallFlowModuleEvent.CompleteExecute, modelInfo.ID));
        //        ModelState = ModelState.Focused;
        //        OnModelFocused(modelOperation_Focused);
        //    });
        //}
        //else
        {
            if (modelOperation == modelOperation_Focused)
            {
                if (modelInfo.InfoData != null &&
                    (modelInfo.InfoData.InteractMode == InteractMode.OpUI
                  /*  || modelInfo.InfoData.InteractMode == InteractMode.ListUI)*/))
                {
                    // 已经聚焦的操作对象，且配置了操作UI
                    return;
                }
            }

            ModelState = ModelState.Select;
            modelOperation_Select = modelOperation;

            // 对其他操作对象进行操作时，聚焦对象取消聚焦
            if (modelOperation_Select != modelOperation_Focused)
            {
                modelOperation_Focused = null;
                ////执行操作表现前 移除高亮
                //if (modelOperation_Select != null)
                //{
                //    smallFlowCtrl.RemoveHint(modelOperation_Select, 1);
                //    highlights.Remove(modelOperation_Select);
                //}
            }

            ToolManager.SendBroadcastMsg(new MsgOperation((ushort)SmallFlowModuleEvent.ClickObj, modelOperation_Select.GetComponent<ModelInfo>().ID, null, null));
        }
    }

    //private void OnModelFocused(ModelOperation modelOperation)
    //{
    //    var modelInfo = modelOperation.GetComponent<ModelInfo>();
    //    if (modelInfo.InfoData != null)
    //    {
    //        SmallOp1 data = new SmallOp1 { operation = modelOperation, prop = prop };

    //        switch (modelInfo.InfoData.InteractMode)
    //        {
    //            case InteractMode.OpUI:
    //                modelOperation_Select = modelOperation;
    //                Drag(modelInfo, string.Empty, modelOperation_Select.currentState, (opName) =>
    //                {
    //                    data.optionName = opName;
    //                    data.prop = prop;
    //                    ToolManager.SendBroadcastMsg(new MsgOperation((ushort)SmallFlowModuleEvent.Operate, data.operation.GetComponent<ModelInfo>().ID, data.optionName, data.prop?.ID, IsCorrectOperation(data.operation, out SmallOp1 expectOp) && opName.Equals(expectOp.optionName)), true);
    //                });
    //                return;
    //            case InteractMode.ListUI:
    //                modelOperation_Select = modelOperation;
    //                List<string> options = modelOperation.operations.Select(o => o.name)
    //                    .Where(o => !SmallFlowCtrl.maskOperation.Contains(o) && !o.StartsWith(SmallFlowCtrl.backpackFlag)).ToList();
    //                List(modelInfo, string.Empty, options, (opName) =>
    //                {
    //                    data.optionName = opName;
    //                    data.prop = prop;
    //                    ToolManager.SendBroadcastMsg(new MsgOperation((ushort)SmallFlowModuleEvent.Operate, data.operation.GetComponent<ModelInfo>().ID, data.optionName, data.prop?.ID, IsCorrectOperation(data.operation, out SmallOp1 expectOp) && opName.Equals(expectOp.optionName)), true);
    //                });
    //                return;
    //            case InteractMode.Click:
    //            case InteractMode.Switch:
    //            default:
    //                break;
    //        }
    //    }
    //}

    private bool IsCorrectOperation(ModelOperation modelOperation, out SmallOp1 data)
    {
        data = null;
        var result = smallFlowCtrl.IsOnOperation(modelOperation, prop, out data);
        return result && !data.optionName.Equals(SmallFlowCtrl.inputFlag);//确保正确操作为输入时，操作对象执行其他操作不会切换步骤
    }

    /// <summary>
    /// 执行2D操作
    /// </summary>
    /// <param name="modelOperation"></param>
    /// <param name="optionName"></param>
    public void SelectAndExecute2D(ModelOperation modelOperation, string optionName)
    {
        smallFlowCtrl.Remove2DHint(modelOperation);

        if (FatalFinish)
        {
            smallFlowCtrl.RestoreState(modelOperation, modelOperation.currentState);
            ShowFatalPopup();
            return;
        }

        if (IsOperatableState && prop != null)
        {
            if (OperatePermissionOccupied(modelOperation, optionName) || OtherOperating)
            {
                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("其他成员正在操作"));
                smallFlowCtrl.RestoreState(modelOperation, modelOperation.currentState);
                return;
            }

            ModelState = ModelState.Select;
            modelOperation_Select = modelOperation;

            ToolManager.SendBroadcastMsg(new MsgOperation2D((ushort)SmallFlowModuleEvent.Look2D, modelOperation_Select.GetComponent<ModelInfo>().ID, optionName), true);
        }
    }

    /// <summary>
    /// 获取操作名称（用于考核模式操作检查）
    /// </summary>
    /// <param name="modelOperation">操作对象</param>
    /// <returns>操作名称</returns>
    private string GetOperationName(ModelOperation modelOperation)
    {
        ModelInfo modelInfo = modelOperation.GetComponent<ModelInfo>();
        if (modelInfo.InfoData == null)
            return SmallFlowCtrl.clickFlag;

        switch (modelInfo.InfoData.InteractMode)
        {
            case InteractMode.Click:
                if (prop != null)
                {
                    string opWithProp = $"{SmallFlowCtrl.backpackFlag}_{prop.Name}";
                    if (modelOperation.operations.Any(o => o.name.Equals(opWithProp)))
                        return opWithProp;
                }
                // 检查点击操作是否存在
                if (modelOperation.operations.Any(o => o.name.Equals(SmallFlowCtrl.clickFlag)))
                    return SmallFlowCtrl.clickFlag;
                return SmallFlowCtrl.observeFlag;

            case InteractMode.Switch:
                if (modelOperation.currentState.Equals(SmallFlowCtrl.switchOpenFlag))
                    return SmallFlowCtrl.switchCloseFlag;
                if (modelOperation.currentState.Equals(SmallFlowCtrl.switchCloseFlag))
                    return SmallFlowCtrl.switchOpenFlag;
                if (modelOperation.currentState.Equals(SmallFlowCtrl.switchOnFlag))
                    return SmallFlowCtrl.switchOffFlag;
                if (modelOperation.currentState.Equals(SmallFlowCtrl.switchOffFlag))
                    return SmallFlowCtrl.switchOnFlag;
                return string.Empty;

            default:
                return SmallFlowCtrl.clickFlag;
        }
    }

    /// <summary>
    /// 执行操作
    /// </summary>
    private void TryExecuteOp(ModelOperation modelOperation, int sender)
    {
        if (modelOperation == null)
            return;

        ModelState = ModelState.Operating;
        if (prop != null && prop.PropType != PropType.MasterComputer)
            prop.gameObject.SetActive(false);

        bool isOnOperation = IsCorrectOperation(modelOperation, out SmallOp1 data);
        if (GlobalInfo.IsExamMode())
        {
            ExecuteOperation(modelOperation, isOnOperation, data?.optionName);
        }
        else
        {
            if (isOnOperation)
            {
                ExecuteOperation(modelOperation, isOnOperation, data?.optionName);
            }
            else
            {
                //提示对错
                OnErrorShow();
            }
        }
    }

    /// <summary>
    /// 执行安全工器具操作
    /// </summary>
    public void TryExecuteToolOp(ModelInfo modelInfo, string optionName, bool errorShow = true)
    {
        if (modelInfo == null)
            return;

        var modelOperation = modelInfo.GetComponent<ModelOperation>();
        bool isOnOperation = smallFlowCtrl.IsOnOperation(modelInfo.GetComponent<ModelOperation>(), modelInfo, out SmallOp1 data) && data.optionName.Equals(optionName);
        ToolManager.SendBroadcastMsg(new MsgOperation((ushort)SmallFlowModuleEvent.Operate, modelInfo.ID, optionName, modelInfo.ID, isOnOperation), true);
    }

    /// <summary>
    /// 执行操作，UI则显示操作UI
    /// </summary>
    /// <param name="modelOperation">操作对象</param>
    /// <param name="correctOp">是否是当前步骤的正确操作</param>
    /// <param name="uiopName">当前步骤正确操作名称，用于UI操作提示</param>
    /// <param name="free">是否为不计入记录的自由操作</param>
    private void ExecuteOperation(ModelOperation modelOperation, bool correctOp, string uiopName = "", bool free = false)
    {
        SmallOp1 data = new SmallOp1
        {
            operation = modelOperation,
            prop = prop
        };

        ModelInfo modelInfo = modelOperation.GetComponent<ModelInfo>();
        if (modelInfo.InfoData != null)
        {
            switch (modelInfo.InfoData.InteractMode)
            {
                case InteractMode.Click:
                    data.optionName = SmallFlowCtrl.clickFlag;
                    // todo 未配置点击操作，尝试执行观察操作，兼容角色漫游配置
                    if (modelOperation.operations.FirstOrDefault(o => o.name.Equals(SmallFlowCtrl.clickFlag)) == null)
                    {
                        data.optionName = SmallFlowCtrl.observeFlag;
                    }
                    //工具操作
                    if (prop != null)
                    {
                        string opWithProp = $"{SmallFlowCtrl.backpackFlag}_{prop.Name}";
                        if (modelOperation.operations.Any(o => o.name.Equals(opWithProp)))
                        {
                            data.optionName = opWithProp;
                        }
                    }
                    break;
                case InteractMode.Switch:
                    if (modelOperation.currentState.Equals(SmallFlowCtrl.switchOpenFlag))
                        data.optionName = SmallFlowCtrl.switchCloseFlag;
                    else if (modelOperation.currentState.Equals(SmallFlowCtrl.switchCloseFlag))
                        data.optionName = SmallFlowCtrl.switchOpenFlag;

                    if (modelOperation.currentState.Equals(SmallFlowCtrl.switchOnFlag))
                        data.optionName = SmallFlowCtrl.switchOffFlag;
                    else if (modelOperation.currentState.Equals(SmallFlowCtrl.switchOffFlag))
                        data.optionName = SmallFlowCtrl.switchOnFlag;

                    //工具操作
                    if (prop != null)
                    {
                        string opWithProp = $"{SmallFlowCtrl.backpackFlag}_{prop.Name}";
                        if (modelOperation.operations.Any(o => o.name.Equals(opWithProp)))
                        {
                            data.optionName = opWithProp;
                        }
                    }
                    break;
                case InteractMode.OpUI:
                    Drag(modelInfo, uiopName, modelOperation.currentState, (opName) =>
                    {
                        data.optionName = opName;
                        correctOp = correctOp && (free || opName.Equals(uiopName));
                        ToolManager.SendBroadcastMsg(new MsgOperation((ushort)SmallFlowModuleEvent.Operate, data.operation.GetComponent<ModelInfo>().ID, data.optionName, data.prop?.ID, correctOp), true);
                    });
                    return;
                case InteractMode.ListUI:
                    {
                        var opWithProp = $"{SmallFlowCtrl.backpackFlag}_{prop?.Name}";
                        if (prop != null && modelOperation.operations.Any(o => o.name.Equals(opWithProp)))
                        {
                            data.optionName = opWithProp;
                            break;
                        }
                        else
                        {
                            List<string> options = modelOperation.operations.Select(o => o.name)
                                .Where(o => !SmallFlowCtrl.maskOperation.Contains(o) && !o.StartsWith(SmallFlowCtrl.backpackFlag)).ToList();
                            List(modelInfo, uiopName, options, (opName) =>
                            {
                                data.optionName = opName;
                                correctOp = correctOp && (free || opName.Equals(uiopName));
                                ToolManager.SendBroadcastMsg(new MsgOperation((ushort)SmallFlowModuleEvent.Operate, data.operation.GetComponent<ModelInfo>().ID, data.optionName, data.prop?.ID, correctOp));
                            });
                            return;
                        }
                    }
                default:
                    Debug.LogWarning("未处理触发方式：" + modelInfo.InfoData.InteractMode.ToString());
                    break;
            }
        }
        ToolManager.SendBroadcastMsg(new MsgOperation((ushort)SmallFlowModuleEvent.Operate, data.operation.GetComponent<ModelInfo>().ID, data.optionName, data.prop?.ID, correctOp));
    }

    /// <summary>
    /// 执行2D操作
    /// </summary>
    private void TryExecute2DOp(ModelOperation modelOperation, string optionName)
    {
        string currentState = modelOperation.currentState;

        ModelState = ModelState.Operating;
        bool isOnOperation = smallFlowCtrl.IsOnOperation(modelOperation, prop, out SmallOp1 data) && prop != null && data.optionName.Equals(optionName);
            //todo 隐藏错误提示
            if (isOnOperation)
            {
                //SoundManager.Instance.PlayEffect("TrueProblem");
            }
            else
            {
                smallFlowCtrl.RestoreState(modelOperation, currentState);
                //OnErrorShow();
            }
        
        Execute2DOperation(modelOperation, optionName, isOnOperation);
    }

    /// <summary>
    /// 执行2D操作
    /// </summary>
    /// <param name="modelOperation">操作对象</param>
    /// <param name="optionName"></param>
    /// <param name="correctOp">是否是当前步骤的正确操作</param>
    private void Execute2DOperation(ModelOperation modelOperation, string optionName, bool correctOp)
    {
        SmallOp1 data = new SmallOp1();
        data.operation = modelOperation;
        data.prop = prop;
        data.optionName = optionName;
        ToolManager.SendBroadcastMsg(new MsgOperation((ushort)SmallFlowModuleEvent.Operate, data.operation.GetComponent<ModelInfo>().ID, data.optionName, data.prop?.ID, correctOp), true);
    }


    /// <summary>
    /// 执行操作回调
    /// </summary>
    /// <param name="success"></param>
    /// <param name="modelOperation">执行失败时还原道具状态</param>
    /// <param name="restoredState">执行失败时还原道具状态</param>
    /// <param name="freeOperation">是否为不计入记录的操作</param>
    private void OnExecuteCompleted(bool success, bool freeOperation = false, ModelOperation modelOperation = null, string restoredState = null)
    {
        RetakeBackpackModel(true);
        if (success)
        {
        }
        else//操作执行失败
        {
            if (!GlobalInfo.IsExamMode() && !freeOperation)
                OnErrorShow();

            smallFlowCtrl.RestoreState(modelOperation, restoredState);
        }
    }

    /// <summary>
    /// 拖拽旋转类UI
    /// </summary>
    /// <param name="modelInfo"></param>
    /// <param name="opName"></param>
    /// <param name="currentState"></param>
    /// <param name="callback"></param>
    private void Drag(ModelInfo modelInfo, string opName, string currentState, Action<string> callback)
    {
        OpUIData info = modelInfo.InfoData.interactData as OpUIData;

        if (modelInfo.TryGetComponent(out MouseDragRotate mouseDragRotate))
        {
            mouseDragRotate.Setup(modelInfo.ID, currentState, info.targetObject);

            mouseDragRotate.Interactable = true;
            mouseDragRotate.OnDragFinish = null;
            mouseDragRotate.OnDragFinish += (opName) =>
            {
                callback?.Invoke(opName);
                //mouseDragRotate.Interactable = false;
            };

            mouseDragRotate.OnModelClicked = null;
            mouseDragRotate.OnModelClicked += () =>
            {
                //todo 目前是特殊处理  
                if (prop != null)
                {
                    callback?.Invoke($"{SmallFlowCtrl.backpackFlag}_{prop.Name}");
                    //mouseDragRotate.Interactable = false;
                }
            };
        }
        else if (modelInfo.TryGetComponent(out MouseDragMove mouseDragMove))
        {
            mouseDragMove.Setup(modelInfo.ID, currentState, info.targetObject);

            mouseDragMove.Interactable = true;
            mouseDragMove.OnDragFinish = null;
            mouseDragMove.OnDragFinish += (opName) =>
            {
                callback?.Invoke(opName);
                //mouseDragRotate.Interactable = false;
            };

            mouseDragMove.OnModelClicked = null;
            mouseDragMove.OnModelClicked += () =>
            {
                //todo 目前是特殊处理  
                if (prop != null)
                {
                    callback?.Invoke($"{SmallFlowCtrl.backpackFlag}_{prop.Name}");
                    //mouseDragRotate.Interactable = false;
                }
            };
        }
        else//兼容UI配置
        {
            UIOperation ui = null;
            UIOperation[] us = transform.GetComponentsInChildren<UIOperation>();
            for (int i = 0; i < us.Length; i++)
            {
                if (us[i].id.Equals(modelInfo.ID))
                    ui = us[i];
            }
            if (ui == null)
            {
                ui = Instantiate(info.content, transform).GetComponent<UIOperation>();
                //在toolModule上
                ui.transform.SetSiblingIndex(transform.childCount - 3);
            }
            ui.Init(modelInfo.ID, currentState, opName, info.targetObject, callback, OnErrorShow);
            //todo
            ui.SetActiveProp(prop?.Name);
        }
    }

    /// <summary>
    /// 菜单选择类UI
    /// </summary>
    /// <param name="modelInfo"></param>
    /// <param name="opName"></param>
    /// <param name="callback"></param>
    private void List(ModelInfo modelInfo, string opName, List<string> options, Action<string> callback)
    {
        OpUIData info = modelInfo.InfoData.interactData as OpUIData;
        UIOption ui = null;
        UIOption[] us = transform.GetComponentsInChildren<UIOption>();
        for (int i = 0; i < us.Length; i++)
        {
            if (us[i].id.Equals(modelInfo.ID))
                ui = us[i];
        }
        if (ui == null)
            ui = Instantiate(info.content, transform).GetComponent<UIOption>();

        ui.Init(modelInfo.ID, options, opName, callback, OnErrorShow);
    }

    /// <summary>
    /// 错误提示
    /// </summary>
    public void OnErrorShow()
    {
        if (GlobalInfo.IsExamMode())
            return;
        SoundManager.Instance.PlayEffect("FalseProblem");
        DOTween.Kill("ErrorShow");
        error.color = new Color(error.color.r, error.color.g, error.color.b, 1);
        error.DOFade(0, 0.5f).SetLoops(3).SetEase(Ease.InOutQuad).SetId("ErrorShow");
        DOVirtual.DelayedCall(1, () =>
        {
            ToolManager.SendBroadcastMsg(new MsgBase((ushort)SmallFlowModuleEvent.CompleteExecute));
        });
    }

    private void RetakeBackpackModel(bool self)
    {
        if (prop != null)
        {
            if (prop.PropType == PropType.MasterComputer)
                return;

            if (prop.PropType == PropType.BackPack_Original && toolModule.toolNumber[prop.ID] <= 0)
            {
                ChangeProp(null);
                return;
            }

            if (self && prop.PropType != PropType.MasterComputer)
                ChangeProp(prop);
        }
    }

    /// <summary>
    /// 设置是否选中物体
    /// </summary>
    /// <param name="isSelect">true-显示操作列表，false-重置为默认显示</param>
    protected void SetSelect(bool isSelect)
    {
        isAlt = isSelect;
        if (playerController)
        {
            Focus.enabled = !isSelect;
            Focus.transform.GetChild(0).gameObject.SetActive(isSelect ? false : modelOperation_Highlight != null);
#if UNITY_ANDROID || UNITY_IOS
            fakeCursorLocked = !isSelect;
#else
            if (isSelect)
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                if (!GlobalInfo.ShowPopup)
                    Cursor.lockState = CursorLockMode.Locked;
            }
            GlobalInfo.CursorLockMode = isSelect ? CursorLockMode.None : CursorLockMode.Locked;
#endif
        }
    }

    private void EnableCameraControl(bool enabled)
    {
        if (playerController)
        {
            playerController.enabled = enabled;
            // 只有在启用且没有外部 DOTween 控制相机时才重建跟随 tween
            // 禁用时不需要调用 ToLast()，因为外部动画（如 BehaveObserve）会接管相机
            if (enabled && !ModelManager.Instance.CameraDotween)
            {
                playerController.ToLast();
            }
        }
        else
        {
            ////通过CameraControl控制 会暂停协同操作同步
            //ModelManager.Instance.CameraControl = enabled;
            CameraMove.enabled = enabled;
            CameraRotate.enabled = enabled;
            CameraZoom.enabled = enabled;
        }
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)OperationListEvent.Open:
                bool show = ((MsgBool)msg).arg1;
                flowModule = (UISmallSceneFlowModule)UIManager.Instance.OpenModuleUI<UISmallSceneFlowModule>(ParentPanel, transform.parent);
                if (show)
                {
                    SendMsg(new MsgBase((ushort)OperationListEvent.Show));
                    SendMsg(new MsgBool((ushort)SmallFlowModuleEvent.LeftFlex, /*true*/!GlobalInfo.IsExamMode()));
                }
                break;
            case (ushort)OperationListEvent.Hide:
                UIManager.Instance.HideModuleUI<UISmallSceneFlowModule>(ParentPanel);
                break;
            case (ushort)HistoryEvent.Open:
                bool showMoule = ((MsgBool)msg).arg1;
                operationHistoryModule = (UISmallSceneOperationHistory)UIManager.Instance.OpenModuleUI<UISmallSceneOperationHistory>(ParentPanel, transform.parent, new InpuAndHistoryData(smallFlowCtrl, this, GlobalInfo.IsExamMode()));
                if (showMoule)
                {
                    SendMsg(new MsgBase((ushort)HistoryEvent.Show));
                    SendMsg(new MsgBool((ushort)SmallFlowModuleEvent.LeftFlex, /*true*/!GlobalInfo.IsExamMode()));
                }
                break;
#if UNITY_STANDALONE
            case (ushort)SmallFlowModuleEvent.LeftFlex:
                if (SafetyContent!= null)
                {
                    bool leftFlex = ((MsgBool)msg).arg1;
                    float leftAnchorPos = leftFlex ? 306f : 50f;
                    SafetyContent.transform.parent.GetComponent<RectTransform>().DOAnchorPos3DX(leftAnchorPos, 0.3f);
                }
                break;
            case (ushort)SmallFlowModuleEvent.RightFlex:
                bool rightFlex = ((MsgBool)msg).arg1;
                float RightAnchorPos = rightFlex ? -324f : -24f;
                CameraView.DOAnchorPos3DX(RightAnchorPos, 0.3f);
                break;
#endif
            //监控画面的显示和关闭 改成由Monitor自己控制
            //case ushort.MaxValue:
            //    MsgBehaveEvent msgBehaveEvent = msg as MsgBehaveEvent;
            //    var target = msgBehaveEvent.arg;
            //    {
            //        switch (target.behaveType)
            //        {
            //            case BehaveType.Monitor:
            //                activeMonitorCam = msgBehaveEvent.behaveTrans;
            //                if (CameraViewRawImage != null)
            //                    CameraViewRawImage.texture = msgBehaveEvent.behaveTrans?.GetComponent<Camera>()?.targetTexture;
            //                if(CameraView != null)
            //                {
            //                    //显示在上位机上
            //                    CameraView.transform.SetAsLastSibling();
            //                    CameraView.gameObject.SetActive(true);
            //                }
            //                break;
            //            //case BehaveType.Popup:
            //            //    OnFatalFinish((target as BehavePopup).message);
            //            //    break;
            //        }
            //    }
            //    break;
            //case (ushort)SmallFlowModuleEvent.HideMonitor:
            //    if ((msg as MsgTransform).arg == activeMonitorCam)
            //    {
            //        if (CameraViewRawImage != null)
            //            CameraViewRawImage.texture = null;
            //        if (CameraView != null)
            //            CameraView.gameObject.SetActive(false);
            //    }
            //    break;
            case (ushort)SmallFlowModuleEvent.SelectFlow:
                smallFlowCtrl.SelectFlow(((MsgBrodcastOperate)msg).GetData<MsgStringInt>().arg2);
                smallFlowCtrl.SelectStep(0);
                OnStepChanged();
                break;
            case (ushort)SmallFlowModuleEvent.SelectStep:
                MsgStringTuple<int, int, string> msgStringTuple = ((MsgBrodcastOperate)msg).GetData<MsgStringTuple<int, int, string>>();
                UIManager.Instance.CloseUI<PopupPanel>(new UIPopupData(string.Empty, msgStringTuple.arg2.Item3, null));
                smallFlowCtrl.SelectFlow(msgStringTuple.arg2.Item1);
                smallFlowCtrl.SelectStep(msgStringTuple.arg2.Item2);

                Debug.Log("状态调试 任务选中" + msgStringTuple.arg2.Item1 + "步骤选中" + msgStringTuple.arg2.Item2);
                OnStepChanged();
                break;
            case (ushort)SmallFlowModuleEvent.Guide:
                ModelState = ModelState.Unselect;
                MsgTuple<int, int, string> msgTuple = msg as MsgTuple<int, int, string>;
                smallFlowCtrl.StepGuide(msgTuple.arg.Item1, msgTuple.arg.Item2);
                StepHighlight(msgTuple.arg.Item1, msgTuple.arg.Item2);
                break;
            case (ushort)SmallFlowModuleEvent.SelectTool:
                OnPropChanged((msg as MsgString).arg);
                break;
            case (ushort)SmallFlowModuleEvent.SelectInput:
                //if ((msg as MsgStringBool).arg2)
                //    OnPropChanged(string.Empty);
                break;
            case (ushort)SmallFlowModuleEvent.SelectContact:
                //if ((msg as MsgBool).arg1)
                //    OnPropChanged(string.Empty);
                break;
            case (ushort)SmallFlowModuleEvent.Operate2D:
                Msg2DOperate msg2DOperate = msg as Msg2DOperate;
                SelectAndExecute2D(msg2DOperate.operation, msg2DOperate.optionName);
                break;
            case (ushort)SmallFlowModuleEvent.CompleteExecute:
                // 操作完成时释放发送者的操作权限
                MsgBrodcastOperate brodcastMsg = msg as MsgBrodcastOperate;
                ReleaseOperatePermission();

                ModelState = ModelState.Unselect;
                RefreshHighlight();

                //进行下一步时直接取消手中道具
                OnPropChanged(string.Empty);
                toolModule.CloseBackpack();
                toolModule.CancelDrawingToggle();  // 关闭图纸面板
                toolModule.ShowTool(true);  // 显示工具栏
                break;
            case (ushort)SmallFlowModuleEvent.OperatingRecord:
                MsgOperatingRecord opMsg = msg as MsgOperatingRecord;
                ShowHint(opMsg.opHint, -1);
                break;
            case (ushort)SmallFlowModuleEvent.FocusChanged:
                int userIdFocus = ((MsgBrodcastOperate)msg).senderId;
                var focusData = (msg as MsgBrodcastOperate).GetData<MsgStringString>();
                ModelOperation modelOperationFocused = smallFlowCtrl.GetModelOperation(focusData.arg2);
                // 选中对象冲突
                if (modelOperationFocused != null && modelOperationFocused == modelOperation_Focused && userIdFocus != GlobalInfo.account.id)
                {
                    ModelState = ModelState.Unselect;
                }
                //AcquireOperatePermission(userIdFocus, modelOperationFocused, string.Empty);
                break;
            case (ushort)SmallFlowModuleEvent.ClickObj:
                ModelOperation modelOperation = smallFlowCtrl.GetModelOperation((msg as MsgBrodcastOperate).GetData<MsgOperation>().modelOperation);
                int sender = ((MsgBrodcastOperate)msg).senderId;
                // 选中对象冲突
                if (modelOperation != null && modelOperation == modelOperation_Select && sender != GlobalInfo.account.id)
                {
                    ModelState = ModelState.Unselect;
                }
                if (sender == GlobalInfo.account.id)
                {
                    TryExecuteOp(modelOperation, sender);
                    // 考核模式 操作就获得操作权限 没有正确判断
                }
                AcquireOperatePermission(sender, modelOperation);
                break;
            //case (ushort)SmallFlowModuleEvent.Look2D:
            //    MsgOperation2D msgOperation2D = (msg as MsgBrodcastOperate).GetData<MsgOperation2D>();
            //    ModelOperation modelOperation2d = smallFlowCtrl.GetModelOperation(msgOperation2D.modelOperation);
            //    int userIdLook2d = ((MsgBrodcastOperate)msg).senderId;
            //    // 选中对象冲突
            //    if (modelOperation2d != null && modelOperation2d == modelOperation_Select && userIdLook2d != GlobalInfo.account.id)
            //    {
            //        ModelState = ModelState.Unselect;
            //    }
            //    //获得对modelOperation的操作权，执行本地操作
            //    AcquireOperatePermission(userIdLook2d, modelOperation2d, msgOperation2D.operationName);
            //    if (userIdLook2d == GlobalInfo.account.id && !NetworkManager.Instance.IsIMSyncState)
            //    {
            //        TryExecute2DOp(modelOperation2d, msgOperation2D.operationName);
            //    }
            //    break;
            case (ushort)SmallFlowModuleEvent.Operate:
                int userIdOp = ((MsgBrodcastOperate)msg).senderId;
                MsgOperation msgOp = ((MsgBrodcastOperate)msg).GetData<MsgOperation>();
                {
                    Debug.Log($"状态调试 Operate收到消息 - senderId:{userIdOp}, modelOperation:{msgOp.modelOperation}, operationName:{msgOp.operationName}, 当前用户:{GlobalInfo.account.id}");

                    SmallOp1 data = new SmallOp1();
                    data.operation = smallFlowCtrl.GetModelOperation(msgOp.modelOperation);
                    data.prop = smallFlowCtrl.GetModelInfo(msgOp.propId);
                    data.optionName = msgOp.operationName;

                    //协同、考核是否为用户本人操作
                    bool self = ((MsgBrodcastOperate)msg).senderId == GlobalInfo.account.id;
                    ModelState = self ? ModelState.Operating : ModelState.OtherOperating;

                    //记录执行操作前的道具状态
                    string oldState = data.operation.currentState;

                    if(GlobalInfo.isExam)
                    {
                        smallFlowCtrl.TryExecuteFreeOperation(data, msgOp.userNo, msgOp.userName, GlobalInfo.courseMode == CourseMode.Exam ? false : !self);
                    }
                    else
                    {
                        smallFlowCtrl.TryExecuteOperation(data, msgOp.correctOp, msgOp.userNo, msgOp.userName, (isOn) =>
                        {
                            ModelState = ModelState.Operated;
                            if (self)
                            {
                                RetakeBackpackModel(true);
                                if(!isOn)
                                    smallFlowCtrl.RestoreState(data.operation, oldState);
                            }
                        }, !self);
                    }
                }
                break;
            case (ushort)SmallFlowModuleEvent.MasterComputerSelect:
                //协同、考核是否为用户本人操作
                int senderId = ((MsgBrodcastOperate)msg).senderId;
                if (senderId == GlobalInfo.account.id)
                {
                    ModelState = ModelState.Unselect;
                    MsgElement msgElement = (msg as MsgBrodcastOperate).GetData<MsgElement>();
                    focusMasterComputerDescrption = msgElement.name;
                }
                break;
            case (ushort)ShortcutEvent.PressAnyKey:
                ShortcutManager.Instance.CheckShortcutKey(msg, new Dictionary<string, Action>()
                {
                    {
                        ShortcutManager.SmallScene_SwitchCursor, ()=>
                        {
                            if (ModelState == ModelState.Operating || GlobalInfo.InPaintMode || GlobalInfo.ShowPopup || inMap)
                                return;
                            if(playerController == null)
                                return;
                            if (!isAlt || !isExecuteOperation)
                            {
                                EnableCameraControl(isAlt);
                                SetSelect(!isAlt);
                            }
                        }
                    },
                    {
                        ShortcutManager.OpenOption, ()=>
                        {
                            if(playerController == null)
                                return;
                            if (!isAlt)
                            {
                                EnableCameraControl(false);
                                SetSelect(true);
                            }
                        }
                    }
                });
                break;
            case (ushort)SmallFlowModuleEvent.MaxMap:
                inMap = ((MsgBool)msg).arg1;
                break;
            case (ushort)SmallFlowModuleEvent.CloseCameraOperation:
                StartCoroutine(WaitSelect(true));
                break;
            case (ushort)SmallFlowModuleEvent.OpenCameraOperation:
                StartCoroutine(WaitSelect(false));
                break;
            case (ushort)RoomChannelEvent.UpdateControl:
                MsgIntBool msgIntBool = (MsgIntBool)msg;
                // 成员失去操作权，释放占用的操作对象
                if (!msgIntBool.arg2)
                {
                    ReleaseOperatePermission(msgIntBool.arg1);
                }
                break;
            case (ushort)RoomChannelEvent.OtherLeave:
                ReleaseOperatePermission(((MsgIntString)msg).arg1);
                break;
        }
    }
    //bool allOver = false;

    private void OnStepChanged()
    {
        //针对监控功能 跳步骤打的补丁 不应该写在这儿 但是先将就用
        if (CameraViewRawImage != null)
            CameraViewRawImage.texture = null;
        if(CameraView != null)
            CameraView.gameObject.SetActive(false);

        RetakeBackpackModel(true);
        ModelState = ModelState.Unselect;
        ClearHighlight();
        RefreshHighlight();
        userOpModel.Clear();
    }

    /// <summary>
    /// 重连后重置UI交互状态
    /// </summary>
    public void ResetUIState()
    {
        // 重置操作权限记录
        userOpModel.Clear();
        // 重置模型状态（会自动重置focusHint的blocksRaycasts）
        ModelState = ModelState.Unselect;
        // 刷新高亮显示
        RefreshHighlight();
    }

    private void OnPropChanged(string propID)
    {
        if (!string.IsNullOrEmpty(propID) && propID.Equals(SmallFlowCtrl.handFlag))
        {
            ChangeProp(null);
            RefreshHighlight();
        }
        else
        {
            if (prop != null && (smallFlowCtrl.toolIDs[prop.ID].PropType == PropType.BackPack
                              || smallFlowCtrl.toolIDs[prop.ID].PropType == PropType.BackPack_Original))
            {
                smallFlowCtrl.toolIDs[prop.ID].GetComponent<ModelOperation>().currentState = SmallFlowCtrl.unselectFlag;
            }

            if (string.IsNullOrEmpty(propID))
            {
                ChangeProp(null);
            }
            else
            {
                if (smallFlowCtrl.toolIDs[propID].PropType == PropType.BackPack || smallFlowCtrl.toolIDs[propID].PropType == PropType.BackPack_Original)
                {
                    smallFlowCtrl.toolIDs[propID].GetComponent<ModelOperation>().currentState = SmallFlowCtrl.selectFlag;
                }
                ChangeProp(smallFlowCtrl.toolIDs[propID]);
            }
            RefreshHighlight();
        }
    }

    public void ShowFatalPopup()
    {
        SoundManager.Instance.PlayEffect("FalseProblem");
        DOTween.Kill("ErrorShow");
        error.color = new Color(error.color.r, error.color.g, error.color.b, 1);
        error.DOFade(0, 0.5f).SetLoops(3).SetEase(Ease.InOutQuad).SetId("ErrorShow");

        if (GlobalInfo.IsExamMode() || GlobalInfo.IsLiveMode())
        {
            Dictionary<string, PopupButtonData> popupData = new Dictionary<string, PopupButtonData>();
            popupData.Add("知道了", new PopupButtonData(null, true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", FatalFinishMessage + (GlobalInfo.IsExamMode() ? "\r\n当前试题已结束" : "\r\n当前百科已结束"), popupData, null, false));
        }
        else
        {
            Dictionary<string, PopupButtonData> popupData = new Dictionary<string, PopupButtonData>();
            if (!GlobalInfo.IsLiveMode())
            {
                popupData.Add("退出课程", new PopupButtonData(() =>
                {
                    ToolManager.SendBroadcastMsg(new MsgBase((ushort)CoursePanelEvent.Quit), true);
                }, false));
            }
            popupData.Add("重新开始", new PopupButtonData(() =>
            {
                ToolManager.SendBroadcastMsg(new MsgInt((ushort)BaikeSelectModuleEvent.BaikeSelect, GlobalInfo.currentWiki.id), true);
            }, true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", FatalFinishMessage + "\r\n当前百科已结束", popupData, null, false));
        }
    }

    /// <summary>
    /// 等待初始视角导航结束，恢复相机控制、操作
    /// </summary>
    /// <param name="ison"></param>
    /// <param name="time"></param>
    /// <returns></returns>
    private IEnumerator WaitSelect(bool ison, float time = 0.3f)
    {
        yield return null;
        isExecuteOperation = ison;
        //安卓端 根据当前第三人称视角设置 自动重新定位相机  
        EnableCameraControl(!ison);
        SetSelect(ison);
    }

    #region 协同操作权限处理
    /// <summary>
    /// 获取对操作对象的操作权
    /// </summary>
    /// <param name="modelOperation"></param>
    private void AcquireOperatePermission(int userId, ModelOperation modelOperation)
    {
        //释放当前占用的操作对象
        ReleaseOperatePermission(userId);

        if (modelOperation == null)
            return;

        userOpModel.Add(modelOperation,userId);
    }

    /// <summary>
    /// 释放对操作对象的操作权
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="modelOperation"></param>
    /// <param name="operationName"></param>
    private void ReleaseOperatePermission()
    {
        userOpModel.Clear();
    }

    private void ReleaseOperatePermission(int userId)
    {
        List<ModelOperation> operations = new List<ModelOperation>();
        foreach (var userOp in userOpModel)
        {
            if (userOp.Value == userId)
            {
                operations.Add(userOp.Key);
            }
        }
        foreach (var op in operations)
        {
            userOpModel.Remove(op);
        }
    }


    /// <summary>
    /// 检查操作对象操作权是否被占用
    /// </summary>
    /// <param name="modelOperation"></param>
    /// <param name="operationName"></param>
    private bool OperatePermissionOccupied(ModelOperation modelOperation, string operationName)
    {
        int userId;

        if (userOpModel.TryGetValue(modelOperation, out userId) && userId != GlobalInfo.account.id && NetworkManager.Instance.IsUserOnline(userId))
            return true;

        if (string.IsNullOrEmpty(operationName))
            return false;

        //联动操作对象的操作权
        var operation = modelOperation.operations.FirstOrDefault(o => o.name.Equals(operationName));
        if (operation != null)
        {
            var linkageModels = operation.actions.Select(action => action.operation).Where(o => o != null).ToList();
            foreach (var linkageModel in linkageModels)
            {
                if (userOpModel.TryGetValue(linkageModel, out userId) && userId != GlobalInfo.account.id && NetworkManager.Instance.IsUserOnline(userId))
                    return true;
            }
        }
        return false;
    }
    #endregion

    /// <summary>
    /// 显示提示信息弹窗
    /// opIndex -1 是自定义脚本的提示，没用制作对应语音
    /// </summary>
    /// <param name="hint"></param>
    /// <param name="stepHint"></param>
    /// <param name="opIndex"></param>
    public void ShowHint(string stepHint, int opIndex)
    {
        if (stepHint == "")
            return;

        //语音模式使用语音提示，非语音模式设置弹窗
        if(SpeechManager.Instance.SpeechMode && opIndex != -1)
        {
            SpeechManager.Instance.PlayImmediate(smallFlowCtrl.CurrentStep().ID, opIndex, TipType.Tips);
        }
        else
        {
            UIManager.Instance.CloseAllModuleUI<ToastPanel>(ParentPanel);
            UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo(stepHint));
        }
    }

    private void ChangeProp(ModelInfo info)
    {
        if (prop != null && prop.PropType != PropType.MasterComputer)
        {
            prop.transform.SetParent(ModelManager.Instance.modelGo.transform);
            prop.gameObject.SetActive(false);

            //还原图标
            if (ToolSprite != null)
            {
                ToolSprite.SetAlpha(0);
                ToolSprite.sprite = null;
            }
        }
        prop = info;
        if (prop != null)
        {
            if (prop.PropType != PropType.MasterComputer)
            {
                AlignToController(prop);
                prop.gameObject.SetActive(true);
            }

            //显示选中工具图标
            if (ToolSprite != null)
            {
                Sprite sprite = null;
                switch (prop.PropType)
                {
                    case PropType.BackPack:
                        ModelInfo_BackPack modelInfo_BackPack = prop.InfoData as ModelInfo_BackPack;
                        sprite = modelInfo_BackPack.Icon2D;
                        break;
                    case PropType.BackPack_Original:
                        ModelInfo_BackPackOriginal modelInfo_BackPackOriginal = prop.InfoData as ModelInfo_BackPackOriginal;
                        sprite = modelInfo_BackPackOriginal.Icon2D;
                        break;
                }
                if (sprite != null)
                {
                    ToolSprite.sprite = sprite;
                    ToolSprite.SetAlpha(1);
                }
            }

            FormMsgManager.Instance.SendMsg(new MsgString((ushort)SmallFlowModuleEvent.ChangeProp, prop.Name));
        }
    }

    /// <summary>
    /// 根据抓取位置设置道具相对主相机下道具节点的位置
    /// </summary>
    /// <param name="prop"></param>
    protected void AlignToController(ModelInfo prop)
    {
        if (prop == null)
            return;

        Transform grabbable = prop.transform;
        Transform attachPoint = null;
        switch (prop.PropType)
        {
            case PropType.BackPack:
                attachPoint = (prop.InfoData as ModelInfo_BackPack).GrabPointTrans;
                break;
            case PropType.BackPack_Original:
                attachPoint = (prop.InfoData as ModelInfo_BackPackOriginal).GrabPointTrans;
                break;
        }
        if (attachPoint == null)
            attachPoint = grabbable;

        grabbable.rotation = ToolNode.rotation * Quaternion.Inverse(Quaternion.Inverse(grabbable.rotation) * attachPoint.rotation);
        grabbable.position = ToolNode.position + (grabbable.position - attachPoint.position);
        grabbable.SetParent(ToolNode);
    }

    private void ClearHighlight()
    {
        foreach (var component in highlights)
        {
            smallFlowCtrl.RemoveHint(component);
            smallFlowCtrl.Remove2DHint(component);
        }
        highlights.Clear();
    }

    /// <summary>
    /// 刷新高亮提示操作模型
    /// </summary>
    public void RefreshHighlight()
    {
        if (GlobalInfo.IsExamMode())
            return;

        var newHighlights = new HashSet<Component>();

        if (smallFlowCtrl.nowFlowStep != null)
        {
            foreach (var smallOp in smallFlowCtrl.nowFlowStep.ops)
            {
                // 待操作对象已聚焦时，不添加提示高亮
                if (smallOp.operation == modelOperation_Focused)
                {
                    newHighlights.Add(smallOp.operation);
                }
                else
                {
                    if (prop != null)
                    {
                        if (smallOp.prop == prop)
                        {
                            if (!highlights.Contains(smallOp.operation) || !smallOp.operation.TryGetComponent<HighlightPlus.HighlightEffect>(out _))
                            {
                                if (prop.PropType == PropType.MasterComputer)
                                    smallFlowCtrl.Add2DHint(smallOp.operation);
                                else
                                    smallFlowCtrl.AddHint(smallOp.operation);
                            }

                            newHighlights.Add(smallOp.operation);
                        }
                    }
                    else
                    {
                        if (!highlights.Contains(smallOp.operation) || !smallOp.operation.TryGetComponent<HighlightPlus.HighlightEffect>(out _))
                            smallFlowCtrl.AddHint(smallOp.operation);

                        newHighlights.Add(smallOp.operation);
                    }
                }

            }
        }

        foreach (var component in highlights.Except(newHighlights))
        {
            smallFlowCtrl.RemoveHint(component);
            smallFlowCtrl.Remove2DHint(component);
        }

        highlights = newHighlights;
    }

    private void StepHighlight(int flowIndex, int stepIndex)
    {
        var newHighlights = new HashSet<Component>();

        if (smallFlowCtrl.flows != null && flowIndex >= 0 && flowIndex < smallFlowCtrl.flows.Length)
        {
            if (smallFlowCtrl.flows[flowIndex].steps != null && stepIndex >= 0 && stepIndex < smallFlowCtrl.flows[flowIndex].steps.Count)
            {
                var step = smallFlowCtrl.flows[flowIndex].steps[stepIndex];
                foreach (var smallOp in step.ops)
                {
                    if (smallOp.operation == null)
                        continue;

                    if (!highlights.Contains(smallOp.operation) || !smallOp.operation.TryGetComponent<HighlightPlus.HighlightEffect>(out _))
                        smallFlowCtrl.AddHint(smallOp.operation);

                    newHighlights.Add(smallOp.operation);
                }
            }
        }

        foreach (var component in highlights.Except(newHighlights))
        {
            smallFlowCtrl.RemoveHint(component);
            smallFlowCtrl.Remove2DHint(component);
        }

        highlights = newHighlights;
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        base.Close(uiData, callback);

        SpeechManager.Instance.StopSpeech();

        // 注意：不要在这里调用 CloseModuleUI，因为如果是通过 CloseAllModuleUI 关闭的，
        // 会导致在遍历列表时修改列表，引发异常或跳过元素。
        // 子模块的关闭由 CloseAllModuleUI 统一处理。
        // UIManager.Instance.CloseModuleUI<UISmallSceneFlowModule>(ParentPanel);
        // UIManager.Instance.CloseModuleUI<UISmallSceneToolModule>(ParentPanel);
        // UIManager.Instance.CloseModuleUI<UISmallSceneOperationHistory>(ParentPanel);
        // UIManager.Instance.CloseModuleUI<UISmallSceneMinMapModule>(ParentPanel);
        // UIManager.Instance.CloseAllModuleUI<ToastPanel>(ParentPanel);

        UnityEngine.AI.NavMesh.RemoveAllNavMeshData();

        ModelManager.Instance.ControlSceneLight(true, LightShadows.Soft);
        ModelManager.Instance.ResetSceneLight();
        UniversalRenderPipelineUtils.SetRendererFeatureActive("ScreenSpaceAmbientOcclusion", true);

        ChangeProp(null);
        ModelOperationEventManager.UnsubscribeAll();

        this.WaitTime(0.1f, () =>
        {
            Cursor.lockState = CursorLockMode.None;
            GlobalInfo.CursorLockMode = CursorLockMode.None;
            ModelManager.Instance.CameraDotween = false;
        });
    }

    #region 告警
    public Toggle WarnTog { get; private set; }
    private Transform WarnList;
    public GameObject warnItemPrefab;
    private Dictionary<int, Transform> warnItemsDic = new Dictionary<int, Transform>();

    /// <summary>
    /// 增加报警记录
    /// </summary>
    /// <param name="warnCode"></param>
    /// <param name="warnMsg"></param>
    public void AppendWarn(int warnCode, string warnMsg)
    {
        if (warnItemsDic.ContainsKey(warnCode))
        {
            //warnItemsDic[warnCode].transform.SetAsLastSibling();
            //warnItemsDic[warnCode].gameObject.SetActive(true);
        }
        else
        {
            Transform warnItem = Instantiate(warnItemPrefab, warnItemPrefab.transform.parent).transform;
            warnItem.GetComponent<Text>().text = $">>{DateTime.Now:yyyy/MM/dd HH:mm:ss} {warnMsg}";
            warnItem.gameObject.SetActive(true);

            warnItemsDic.Add(warnCode, warnItem);

            //自动弹出报警
            WarnTog.isOn = true;
        }
    }

    /// <summary>
    /// 报警复归
    /// </summary>
    /// <param name="warnCode"></param>

    public void RemoveWarn(int warnCode)
    {
        if (warnItemsDic.ContainsKey(warnCode))
        {
            warnItemsDic[warnCode].gameObject.SetActive(false);
        }
    }


    public Dictionary<int, string> GetWarnState()
    {
        return warnItemsDic.Where(item => item.Value.gameObject.activeSelf)
            .ToDictionary(item => item.Key, item => item.Value.GetComponent<Text>().text);
    }

    public void RecoverWarnState(bool warn, Dictionary<int, string> warnRecords)
    {
        foreach (var item in warnItemsDic)
        {
            item.Value.gameObject.SetActive(false);
        }
        foreach (var record in warnRecords)
        {
            if (!warn && record.Key <= 3)
                continue;

            if (warnItemsDic.ContainsKey(record.Key))
            {
                warnItemsDic[record.Key].GetComponent<Text>().text = record.Value;
                warnItemsDic[record.Key].gameObject.SetActive(true);
            }
            else
            {
                Transform warnItem = Instantiate(warnItemPrefab, warnItemPrefab.transform.parent).transform;
                warnItem.GetComponent<Text>().text = $">>{DateTime.Now:yyyy/MM/dd HH:mm:ss} {record.Value}";
                warnItem.gameObject.SetActive(true);

                warnItemsDic.Add(record.Key, warnItem);
            }
        }
    }
    #endregion

    #region 安全工器具
    /// <summary>
    /// 安全工器具穿戴指示
    /// </summary>
    private Transform SafetyContent;

    private Dictionary<string, GameObject> SafetyToolSprites = new Dictionary<string, GameObject>();

    private void InitSafetyToolsSprite()
    {
        SafetyContent = transform.FindChildByName("SafetyContent");
        if (SafetyContent == null)
            return;

        var safetyTools = smallFlowCtrl.toolIDs.Select(t => t)
            .Where(t => t.Value.PropType == PropType.SafetyTool).ToList();

        SafetyContent.RefreshItemsView(safetyTools, (item, tool) =>
        {
            Sprite sprite = (tool.Value.InfoData as ModelInfo_SafetyTool).Icon2D;
            Image image = item.GetComponent<Image>();
            if (sprite == null)
            {
                image.SetAlpha(0);
            }
            else
            {
                item.GetComponent<Image>().sprite = sprite;
                image.SetAlpha(1);
            }

            if (SafetyToolSprites.ContainsKey(tool.Key))
                Log.Warning($"存在重复安全工器具ID");
            else
                SafetyToolSprites.Add(tool.Key, item.gameObject);
        }, false);

        SafetyContent.transform.parent.gameObject.SetActive(/*safetyTools.Count > 0*/
            safetyTools.Count(s => (s.Value.InfoData as ModelInfo_SafetyTool).Icon2D != null) > 0);
    }

    public void ChangeSafetyTool(string id, bool selected)
    {
        if (SafetyToolSprites.TryGetValue(id, out GameObject sprite) && sprite != null)
        {
            sprite.SetActive(selected);
        }
    }
    #endregion
}