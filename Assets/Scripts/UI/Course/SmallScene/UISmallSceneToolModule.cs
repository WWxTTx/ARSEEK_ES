using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityFramework.Runtime;
using UnityEditor;

public class UISmallSceneToolModule : UIModuleBase
{
    private SmallFlowCtrl smallFlowCtrl;

    /// <summary>
    /// 控制当前是否可以切换道具
    /// </summary>
    public bool interactable
    {
        get
        {
            return _interactable;
        }
        set
        {
            _interactable = value;

            GetComponent<CanvasGroup>().alpha = _interactable ? 1 : 0.5f;
            GetComponent<CanvasGroup>().blocksRaycasts = _interactable;
        }
    }
    private bool _interactable = true;
    public CanvasGroup ToolsCanvasGroup;
    /// <summary>
    /// 当前选中道具ID
    /// 观察、手部操作特殊
    /// </summary>
    public string prop;
    /// <summary>
    /// 当前选中安全工器具道具ID
    /// </summary>
    public HashSet<string> safetyProps = new HashSet<string>();
    /// <summary>
    /// 道具字典，key:道具id，value:道具item
    /// </summary>
    private Dictionary<string, Toggle> items = new Dictionary<string, Toggle>();
    /// <summary>
    /// 道具数量字典，key:道具id，value:道具数量
    /// </summary>
    public Dictionary<string, int> toolNumber = new Dictionary<string, int>();
    /// <summary>
    /// 提示高亮道具字典，key:道具实例化编号，value:提示高亮道具动画
    /// </summary>
    private Dictionary<int, Sequence> highlights = new Dictionary<int, Sequence>();
    /// <summary>
    /// UI选中颜色
    /// </summary>
    private Color textSelectColor;
    /// <summary>
    /// 测试使用
    /// </summary>
    private UISmallSceneModule smallSceneModule;

    private Transform ToolContent;
    private Transform GridContent;
    /// <summary>
    /// 当前高亮模式
    /// </summary>
    public string toolMode;
    /// <summary>
    /// 工具模式高亮动画
    /// </summary>
    private Sequence toolModeHighlight;
    private Toggle contactTog;
    private Toggle inputHistoryTog;

    private Toggle backpackTog;
    private Transform backpack;

    private List<Toggle> permanentToggles = new List<Toggle>();

    private Transform drawingTransform;
    private Toggle drawingToggle;

    private UISmallSceneMasterComputerPanel mSchematicPanel;
    /// <summary>
    /// 图纸面板
    /// </summary>
    public UISmallSceneMasterComputerPanel SchematicPanel
    {
        get
        {
            if (mSchematicPanel == null)
                mSchematicPanel = transform.GetComponentByChildName<UISmallSceneMasterComputerPanel>("SchematicPanel");
            return mSchematicPanel;
        }
    }

    private UISmallSceneMasterComputerPanel mMasterComputerPanel;
    /// <summary>
    /// 上位机面板
    /// </summary>
    public UISmallSceneMasterComputerPanel MasterComputerPanel
    {
        get
        {
            if (mMasterComputerPanel == null)
                mMasterComputerPanel = transform.GetComponentByChildName<UISmallSceneMasterComputerPanel>("MasterComputerPanel");
            return mMasterComputerPanel;
        }
    }

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        AddMsg(new ushort[]{
            (ushort)SmallFlowModuleEvent.OperatingRecordInput,
            (ushort)SmallFlowModuleEvent.StartExecute,
            (ushort)SmallFlowModuleEvent.CompleteExecute,
            (ushort)SmallFlowModuleEvent.CompleteStep,
            (ushort)SmallFlowModuleEvent.SelectFlow,
            (ushort)SmallFlowModuleEvent.SelectStep,
            (ushort)SmallFlowModuleEvent.OperatingRecordClear,
            (ushort)SmallFlowModuleEvent.SelectInput,
            (ushort)SmallFlowModuleEvent.SelectContact,
            (ushort)SmallFlowModuleEvent.ShowUIOperation,
            (ushort)SmallFlowModuleEvent.ShowTool,
            (ushort)SmallFlowModuleEvent.Guide
        });

        textSelectColor = "#F2AE5A".HexToColor();
        smallSceneModule = Transform.FindObjectOfType<UISmallSceneModule>().GetComponent<UISmallSceneModule>();

        smallFlowCtrl = ModelManager.Instance.modelGo.GetComponent<SmallFlowCtrl>();
        {
            //背包原件道具消耗回调
            smallFlowCtrl.onUsed.AddListener((modelOperation, reverse) =>
            {
                ModelInfo modelInfo = modelOperation.GetComponent<ModelInfo>();
                if (!reverse)
                {
                    switch (modelInfo.PropType)
                    {
                        case PropType.BackPack_Original:
                            if (toolNumber.ContainsKey(modelInfo.ID))
                            {
                                if (--toolNumber[modelInfo.ID] == 0)
                                    items[modelInfo.ID].transform.gameObject.SetActive(false);
                                else
                                    items[modelInfo.ID].transform.GetComponentByChildName<Text>("Num").text = $"x{toolNumber[modelInfo.ID]}";
                            }
                            break;
                        case PropType.SafetyTool:
                            //items[modelInfo.ID].isOn = true;
                            items[modelInfo.ID].SetIsOnWithoutNotify(true);
                            items[modelInfo.ID].GetComponentInChildren<CanvasGroup>().alpha = 0.4f;
                            break;
                    }
                }
                else
                {
                    switch (modelInfo.PropType)
                    {
                        case PropType.BackPack_Original:
                            if (toolNumber.ContainsKey(modelInfo.ID))
                            {
                                if (toolNumber[modelInfo.ID] < (modelInfo.InfoData as ModelInfo_BackPackOriginal).num)
                                    items[modelInfo.ID].transform.GetComponentByChildName<Text>("Num").text = $"x{++toolNumber[modelInfo.ID]}";
                            }
                            break;
                        case PropType.SafetyTool:
                            items[modelInfo.ID].isOn = false;
                            break;
                    }
                    items[modelInfo.ID].transform.gameObject.SetActive(true);
                }
            });
            //重置背包原件道具数量回调
            smallFlowCtrl.onResetToolNum.AddListener(modelInfo =>
            {
                foreach (KeyValuePair<string, Toggle> toolItem in items)
                {
                    toolItem.Value.isOn = false;
                }
                switch (modelInfo.PropType)
                {
                    case PropType.BackPack_Original:
                        var data = modelInfo.InfoData as ModelInfo_BackPackOriginal;
                        {
                            toolNumber[modelInfo.ID] = data.num;
                            items[modelInfo.ID].transform.GetComponentByChildName<Text>("Num").text = $"x{data.num}";
                            items[modelInfo.ID].transform.gameObject.SetActive(data.InBackPack);
                        }
                        break;
                }
            });
            //拾取背包道具回调
            smallFlowCtrl.onPickup.AddListener((modelOperation, reverse) =>
            {
                ModelInfo modelInfo = modelOperation.GetComponent<ModelInfo>();
                if (reverse)
                    this.FindChildByName(modelInfo.Name).gameObject.SetActive(false);
                else
                {
                    this.FindChildByName(modelInfo.Name).gameObject.SetActive(true);
                    switch (modelInfo.PropType)
                    {
                        case PropType.SafetyTool:
                            items[modelInfo.ID].isOn = false;
                            break;
                    }
                }
            });
        }

        InitToolList();
    }

    /// <summary>
    /// 初始化道具列表
    /// </summary>
    private void InitToolList()
    {
        ToolContent = this.FindChildByName("Content");
        GridContent = this.FindChildByName("GridContent");

        InitContactToggle();
        InitInputToggle();
        //InitLookToggle();
        //InitHandToggle();
        InitDrawingToggle();
        InitMasterComputerToggle();
        InitBackpack();

        //添加快捷键提示
#if UNITY_STANDALONE
        var list = items.Values.Where(toggle => toggle.transform.parent == ToolContent && toggle.gameObject.activeInHierarchy).ToList();
        for (int i = 0; i < list.Count; i++)
        {
            list[i].GetComponentByChildName<Text>("ShortcutKey").text = $"F{i + 1}";// (i + 1).ToString();
        }
#endif

        ToolContent.GetComponentInParent<ScrollRect>().horizontalNormalizedPosition = 0f;
    }

    #region 常驻工具初始化
    /// <summary>
    /// 初始化联系工具
    /// </summary>
    private void InitContactToggle()
    {
        var contact = ToolContent.FindChildByName(SmallFlowCtrl.contactFlag);
        var text = contact.GetComponentByChildName<Text>("Name");
        contactTog = contact.GetComponentInChildren<Toggle>();
        contactTog.onValueChanged.AddListener(isOn =>
        {
            if (isOn)
                CloseBackpack();
            text.color = isOn ? textSelectColor : Color.white;
            SendMsg(new MsgBool((ushort)SmallFlowModuleEvent.SelectContact, isOn));
            RefreshTip();
        });
        permanentToggles.Add(contactTog);
        items.Add(SmallFlowCtrl.contactFlag, contactTog);
    }

    /// <summary>
    /// 初始化记录工具
    /// </summary>
    private void InitInputToggle()
    {
        var inputHistory = ToolContent.FindChildByName(SmallFlowCtrl.historyFlag);
        var text = inputHistory.GetComponentByChildName<Text>("Name");
        inputHistoryTog = inputHistory.GetComponentInChildren<Toggle>();
        inputHistoryTog.onValueChanged.AddListener(isOn =>
        {
            if (isOn)
                CloseBackpack();
            text.color = isOn ? textSelectColor : Color.white;
            SendMsg(new MsgStringBool((ushort)SmallFlowModuleEvent.SelectInput, smallSceneModule.FocusModelDescrption, isOn));
            RefreshTip();
        });
        permanentToggles.Add(inputHistoryTog);
        items.Add(SmallFlowCtrl.historyFlag, inputHistoryTog);
    }

    /// <summary>
    /// 初始化观察
    /// </summary>
    private void InitLookToggle()
    {
        var look = ToolContent.FindChildByName(SmallFlowCtrl.observeFlag);
        var text = look.GetComponentByChildName<Text>("Name");
        var toggle = look.GetComponentInChildren<Toggle>();
        toggle.onValueChanged.AddListener(isOn =>
        {
            text.color = isOn ? textSelectColor : Color.white;
            if (isOn)
            {
                CloseBackpack();
                prop = SmallFlowCtrl.observeFlag;
                SendMsg(new MsgString((ushort)SmallFlowModuleEvent.SelectTool, SmallFlowCtrl.observeFlag));
                RefreshTip();
            }
            else if (!toggle.group.AnyTogglesOn())
            {
                prop = null;
                SendMsg(new MsgString((ushort)SmallFlowModuleEvent.SelectTool, null));
                RefreshTip();
            }
        });
        permanentToggles.Add(toggle);
        items.Add(SmallFlowCtrl.observeFlag, toggle);

        look.gameObject.SetActive(false);
    }

    /// <summary>
    /// 初始化手部操作工具
    /// </summary>
    private void InitHandToggle()
    {
        var hand = ToolContent.FindChildByName(SmallFlowCtrl.handFlag);
        var text = hand.GetComponentByChildName<Text>("Name");
        var toggle = hand.GetComponentInChildren<Toggle>();
        toggle.onValueChanged.AddListener(isOn =>
        {
            text.color = isOn ? textSelectColor : Color.white;

            if (isOn)
            {
                CloseBackpack();
                prop = SmallFlowCtrl.handFlag;
                SendMsg(new MsgString((ushort)SmallFlowModuleEvent.SelectTool, SmallFlowCtrl.handFlag));
                RefreshTip();
            }
            else if (!toggle.group.AnyTogglesOn())
            {
                prop = null;
                SendMsg(new MsgString((ushort)SmallFlowModuleEvent.SelectTool, null));
                RefreshTip();
            }
        });
        permanentToggles.Add(toggle);
        items.Add(SmallFlowCtrl.handFlag, toggle);

        hand.gameObject.SetActive(false);
    }

    /// <summary>
    /// 初始化图纸工具
    /// </summary>
    private void InitDrawingToggle()
    {
        drawingTransform = ToolContent.FindChildByName(SmallFlowCtrl.drawingFlag);
        drawingToggle = drawingTransform.GetComponentInChildren<Toggle>();
        var text = drawingTransform.GetComponentByChildName<Text>("Name");

        // 初始状态
        bool hasSchematics = smallFlowCtrl.toolIDs.Any(t => t.Value.PropType == PropType.Schematics);
        drawingTransform.gameObject.SetActive(hasSchematics);

        drawingToggle.onValueChanged.AddListener(isOn =>
        {
            text.color = isOn ? textSelectColor : Color.white;
            if (isOn)
            {
                CloseBackpack();
                prop = null;
                SendMsg(new MsgString((ushort)SmallFlowModuleEvent.SelectTool, null));
                RefreshTip();
                SchematicPanel.ShowView();
            }
            else
            {
                SchematicPanel.HideView();
            }
            if (!drawingToggle.group.AnyTogglesOn())
            {
                prop = null;
                SendMsg(new MsgString((ushort)SmallFlowModuleEvent.SelectTool, null));
                RefreshTip();
            }
        });
        permanentToggles.Add(drawingToggle);
        items.Add(SmallFlowCtrl.drawingFlag, drawingToggle);

        if (hasSchematics)
        {
            RefreshSchematicList();
        }

        // 监听图纸添加事件
        smallFlowCtrl.onSchematicAdded.AddListener(OnSchematicAdded);
    }

    /// <summary>
    /// 图纸添加回调
    /// </summary>
    private void OnSchematicAdded(ModelInfo schematicInfo)
    {
        // 新增：事件添加图纸时自动显示完成按钮
        SchematicPanel.Over.gameObject.SetActive(true);
        RefreshDrawingToggle();
        SchematicPanel.ShowView();
        ShowTool(false);  // 隐藏工具栏（无延迟）
    }

    /// <summary>
    /// 刷新图纸按钮显示
    /// </summary>
    public void RefreshDrawingToggle()
    {
        var schematics = smallFlowCtrl.toolIDs
            .Where(t => t.Value.PropType == PropType.Schematics)
            .Select(t => t.Value)
            .ToList();

        bool hasSchematics = schematics.Count > 0;
        drawingTransform.gameObject.SetActive(hasSchematics);

        if (hasSchematics)
        {
            RefreshSchematicList();
        }
    }

    /// <summary>
    /// 刷新图纸列表视图
    /// </summary>
    private void RefreshSchematicList()
    {
        var schematics = smallFlowCtrl.toolIDs
            .Where(t => t.Value.PropType == PropType.Schematics)
            .Select(t => t.Value)
            .Select(prop => new DrawingData()
            {
                name = prop.Name,
                sprite = prop.GetComponent<Image>()?.sprite
            })
            .ToList();

        SchematicPanel.SetViews(schematics, textSelectColor);
    }

    /// <summary>
    /// 初始化上位机工具
    /// </summary>
    private void InitMasterComputerToggle()
    {
        var master = ToolContent.FindChildByName(SmallFlowCtrl.masterFlag);
        if (smallSceneModule.masterComputer != null)
        {
            master.gameObject.SetActive(true);
            var text = master.GetComponentByChildName<Text>("Name");
            var toggle = master.GetComponentInChildren<Toggle>();
            toggle.onValueChanged.AddListener(isOn =>
            {
                text.color = isOn ? textSelectColor : Color.white;
                if (isOn)
                {
                    smallSceneModule.masterComputer.gameObject.SetActive(true);
                    ShowTool(false, 1f);
                }
                else
                {
                    smallSceneModule.masterComputer.gameObject.SetActive(false);
                    ShowTool(true);
                }
            });
            items.Add(SmallFlowCtrl.masterFlag, toggle);
        }
        else
        {
            var masterComputerProp = smallFlowCtrl.toolIDs.FirstOrDefault(t => t.Value.PropType == PropType.MasterComputer).Value;
            if (masterComputerProp != null)
            {
                master.gameObject.SetActive(true);
                var text = master.GetComponentByChildName<Text>("Name");
                var toggle = master.GetComponentInChildren<Toggle>();
                toggle.onValueChanged.AddListener(isOn =>
                {
                    text.color = isOn ? textSelectColor : Color.white;
                    if (isOn)
                    {
                        CloseBackpack();
                        prop = masterComputerProp.ID;
                        SendMsg(new MsgString((ushort)SmallFlowModuleEvent.SelectTool, masterComputerProp.ID));
                        RefreshTip();

                        Sprite masterSprite = masterComputerProp.transform.Find("WindowView/View/Show")?.GetComponent<Image>()?.sprite;
                        MasterComputerPanel.ShowDrawing(masterComputerProp.Name, masterSprite);
                    }
                    else
                    {
                        MasterComputerPanel.HideView();
                    }
                    if (!toggle.group.AnyTogglesOn())
                    {
                        prop = null;
                        SendMsg(new MsgString((ushort)SmallFlowModuleEvent.SelectTool, null));
                        RefreshTip();
                    }
                });
                permanentToggles.Add(toggle);
                items.Add(masterComputerProp.ID, toggle);
            }
            else
            {
                master.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 初始化背包
    /// </summary>
    private void InitBackpack()
    {
        var backpackToolPrefab = GridContent.GetChild(0);
        var safetyToolPrefab = GridContent.GetChild(1);

        foreach (var info in smallFlowCtrl.toolIDs)
        {
            Transform item = null;
            switch (info.Value.PropType)
            {
                case PropType.SafetyTool:
                    item = Instantiate(safetyToolPrefab, GridContent);
                    break;
                case PropType.BackPack:
                case PropType.BackPack_Original:
                    item = Instantiate(backpackToolPrefab, GridContent);
                    break;
                default:
                    break;
            }

            if (item != null)
            {
                item.name = info.Value.Name;
                SetIcon(item, info.Value);
                //SetMode(item, info.Value);
                SetToggle(item, info.Value);
#if UNITY_STANDALONE
                SetTouch(item);
#endif
            }
        }

        backpackTog = transform.GetComponentByChildName<Toggle>("工具箱");
        backpack = transform.FindChildByName("Backpack");

        backpackTog.onValueChanged.AddListener((isOn) =>
        {
            backpack.gameObject.SetActive(isOn);
            if (isOn)
                PermanentTogglesOff();
            RefreshTip();
        });
        backpackTog.transform.SetAsLastSibling();
        items.Add(SmallFlowCtrl.backpackFlag, backpackTog);

    }
    #endregion

    /// <summary>
    /// 设置ICON
    /// </summary>
    /// <param name="item"></param>
    /// <param name="info"></param>
    private void SetIcon(Transform item, ModelInfo info)
    {
        switch (info.PropType)
        {
            case PropType.BackPack_Original:
                {
                    var num = item.GetComponentByChildName<Text>("Num");
                    {
                        num.transform.parent.gameObject.SetActive(true);
                        num.text = $"x{(info.InfoData as ModelInfo_BackPackOriginal).num}";
                    }

                    toolNumber.Add(info.ID, (info.InfoData as ModelInfo_BackPackOriginal).num);

                    var data = info.InfoData as ModelInfo_BackPackOriginal;
                    {
                        item.GetComponentByChildName<Image>("Icon").sprite = data.Icon;
                        item.gameObject.SetActive(data.InBackPack);
                    }
                }
                break;
            case PropType.BackPack:
                {
                    var data = info.InfoData as ModelInfo_BackPack;
                    {
                        item.GetComponentByChildName<Image>("Icon").sprite = data.Icon;
                        item.gameObject.SetActive(data.InBackPack);
                    }
                }
                break;
            case PropType.SafetyTool:
                {
                    var data = info.InfoData as ModelInfo_SafetyTool;
                    {
                        item.GetComponentByChildName<Image>("Icon").sprite = data.Icon;
                        item.gameObject.SetActive(data.InBackPack);
                    }
                }
                break;
        }
    }

    /// <summary>
    /// 设置工具模式
    /// </summary>
    /// <param name="item"></param>
    /// <param name="info"></param>
    private void SetMode(Transform item, ModelInfo info)
    {
        if (info.TryGetComponent(out ModelOperation modelOperation))
        {
            var modes = modelOperation.operations.Where(value => !SmallFlowCtrl.maskOperation.Contains(value.name)).Select(value => value.name).ToList();

            if (modes.Count > 0)
            {
                item.FindChildByName("Modes").RefreshItemsView(modes, (modeItem, modeName) =>
                {
                    modeItem.name = modeName;

                    var text = modeItem.GetComponentByChildName<Text>("Name");
                    text.text = modeName;

                    modeItem.GetComponentByChildName<Image>("Icon").sprite = item.GetComponentByChildName<Image>("Icon").sprite;

                    if (modelOperation.currentState == modeName)
                    {
                        modeItem.GetComponent<Toggle>().SetIsOnWithoutNotify(true);
                    }

                    modeItem.GetComponent<Toggle>().onValueChanged.AddListener(isOn =>
                    {
                        text.color = isOn ? textSelectColor : Color.white;

                        if (isOn)
                        {
                            item.GetComponentInChildren<Toggle>().isOn = true;
                            //执行模式对应的操作表现
                            if (modelOperation.GetOperations().TryGetValue(modeName, out OperationBase OperationBase))
                            {
                                modelOperation.currentState = modeName;
                                smallFlowCtrl.Execute(OperationBase.behaveBases, 0, OperationBase.behaveBases.Count, () =>
                                {
                                    RefreshTip();
                                });
                            }

                            //选中高亮提示模式，关闭高亮提示
                            if (toolMode.Equals(modeName) && toolModeHighlight != null)
                                toolModeHighlight.Kill();
                        }
                        else
                        {
                            //选中高亮提示外模式，恢复高亮提示
                            if (toolMode.Equals(modeName))
                                toolModeHighlight = UIHighlight(modeItem);
                        }
                    });
                });

                Toggle tog = item.GetComponentByChildName<Toggle>(modelOperation.currentState);
                if (tog)
                    tog.isOn = true;
            }
        }
    }

    /// <summary>
    /// 设置道具点击触发事件
    /// </summary>
    /// <param name="item"></param>
    /// <param name="info"></param>
    private void SetToggle(Transform item, ModelInfo info)
    {
        var text = item.GetComponentByChildName<Text>("Name");

        text.text = info.Name;

        // 设置道具名称悬浮滚动动效
        SetTouch(item);

        var toggle = item.GetComponentInChildren<Toggle>();

        switch (info.PropType)
        {
            case PropType.SafetyTool:
                toggle.onValueChanged.AddListener(isOn =>
                {
                    toggle.GetComponentInChildren<CanvasGroup>().alpha = isOn ? 0.4f : 1f;
                    if (isOn)
                    {
                        safetyProps.Add(info.ID);
                        smallSceneModule.TryExecuteToolOp(info, SmallFlowCtrl.usedFlag);
                    }
                    else
                    {
                        smallSceneModule.TryExecuteToolOp(info, SmallFlowCtrl.pickupFlag);
                        safetyProps.Remove(info.ID);
                    }
                    RefreshTip();
                });
                break;
            default:
                toggle.onValueChanged.AddListener(isOn =>
                {
                    text.color = isOn ? textSelectColor : Color.white;
                    if (isOn)
                    {
                        SendMsg(new MsgString((ushort)SmallFlowModuleEvent.SelectTool, info.ID));
                        prop = info.ID;

                        this.GetComponentByChildName<Toggle>(info.GetComponent<ModelOperation>()?.currentState)?.SetIsOnWithoutNotify(true);
                        RefreshTip();

                        SetModeSelected(item, info);

                        Transform Modes = transform.FindChildByName("ExternalModes"); //item.FindChildByName("Modes");
                        if (Modes && Modes.childCount > 1)
                        {
                            //Modes.gameObject.SetActive(true);

                            //工具模式高亮提示
                            Transform item = null;
                            for (int i = 0; i < Modes.childCount; i++)
                            {
                                if (smallFlowCtrl.nowFlowStep != null && smallFlowCtrl.nowFlowStep.conditions.Find(value => value.optionName == Modes.GetChild(i).name) != null)
                                {
                                    toolMode = Modes.GetChild(i).name;
                                    Toggle Toggle = Modes.GetChild(i).GetComponentInChildren<Toggle>();
                                    if (!Toggle.isOn)
                                        item = Modes.GetChild(i);//工具需提示模式
                                    else
                                        Toggle.onValueChanged.Invoke(true);//工具模式与UI一致
                                    break;
                                }
                            }
                            if (item)
                                toolModeHighlight = UIHighlight(item);
                        }
                    }
                    else
                    {
                        ClearModes();

                        ////关闭工具模式高亮提示
                        //Transform Modes = item.FindChildByName("Modes");
                        //if (Modes && Modes.childCount > 1)
                        //{
                        //    Modes.gameObject.SetActive(false);

                        //    if (toolModeHighlight != null)
                        //    { toolModeHighlight.Kill(); toolMode = string.Empty; }
                        //}

                        if (!toggle.group.AnyTogglesOn())
                        {
                            prop = null;
                            SendMsg(new MsgString((ushort)SmallFlowModuleEvent.SelectTool, null));
                            RefreshTip();
                        }
                    }
                });
                break;
        }
        items.Add(info.ID, toggle);
    }

    /// <summary>
    /// 设置工具模式
    /// </summary>
    /// <param name="item"></param>
    /// <param name="info"></param>
    private void SetModeSelected(Transform item, ModelInfo info)
    {
        Transform trans = transform.FindChildByName("ExternalModes");
        if (info.TryGetComponent(out ModelOperation modelOperation))
        {
            var modes = modelOperation.operations.Where(value => !SmallFlowCtrl.maskOperation.Contains(value.name)).Select(value => value.name).ToList();

            if (modes.Count > 0)
            {
                trans.RefreshItemsView(modes, (modeItem, modeName) =>
                {
                    modeItem.name = modeName;

                    var text = modeItem.GetComponentByChildName<Text>("Name");
                    text.text = modeName;

                    modeItem.GetComponentByChildName<Image>("Icon").sprite = item.GetComponentByChildName<Image>("Icon").sprite;

                    var modeToggle = modeItem.GetComponent<Toggle>();
                    if (modelOperation.currentState == modeName)
                    {
                        modeToggle.SetIsOnWithoutNotify(true);
                    }
                    modeToggle.onValueChanged.RemoveAllListeners();
                    modeToggle.onValueChanged.AddListener(isOn =>
                    {
                        text.color = isOn ? textSelectColor : Color.white;

                        if (isOn)
                        {
                            item.GetComponentInChildren<Toggle>().isOn = true;

                            //执行模式对应的操作表现
                            if (modelOperation.GetOperations().TryGetValue(modeName, out OperationBase OperationBase))
                            {
                                modelOperation.currentState = modeName;
                                smallFlowCtrl.Execute(OperationBase.behaveBases, 0, OperationBase.behaveBases.Count, () =>
                                {
                                    RefreshTip();
                                });
                            }

                            //选中高亮提示模式，关闭高亮提示
                            if (toolMode.Equals(modeName) && toolModeHighlight != null)
                                toolModeHighlight.Kill();
                        }
                        else
                        {
                            //选中高亮提示外模式，恢复高亮提示
                            if (toolMode.Equals(modeName))
                                toolModeHighlight = UIHighlight(modeItem);
                        }
                    });
                });

                trans.gameObject.SetActive(true);

                Toggle tog = item.GetComponentByChildName<Toggle>(modelOperation.currentState);
                if (tog)
                    tog.isOn = true;
            }
            else
            {
                if (trans.childCount > 1)
                {
                    for (int i = trans.childCount - 1; i > 0; i--)
                    {
                        if (trans.GetChild(i).gameObject)
                        {
                            Object.DestroyImmediate(trans.GetChild(i).gameObject);
                        }
                    }
                }
            }
        }
    }

    private void ClearModes()
    {
        Transform trans = transform.FindChildByName("ExternalModes");
        trans.gameObject.SetActive(false);

        if (toolModeHighlight != null)
        { toolModeHighlight.Kill(); toolMode = string.Empty; }
    }

    /// <summary>
    /// 设置道具名称悬浮动效
    /// </summary>
    /// <param name="item"></param>
    private void SetTouch(Transform item)
    {
        // 查路径: item/Scroll View/Viewport/Content/Name
        var scrollView = item.FindChildByName("Scroll View");
        if (scrollView != null)
        {
            var viewport = scrollView.FindChildByName("Viewport");
            if (viewport != null)
            {
                var content = viewport.FindChildByName("Content");
                if (content != null)
                {
                    var text = content.GetComponentByChildName<Text>("Name");
                    if (text != null)
                    {
                        text.AutoComponent<UITextScroll>();
                    }
                }
            }
        }
    }

    /// <summary>
    /// 关闭工具箱 取消选中背包工具
    /// </summary>
    public void CloseBackpack()
    {
        //var list = items.Values.Where(toggle => toggle.transform.parent == GridContent).ToList();
        var list = items.Where(item => item.Value.transform.parent == GridContent
            && smallFlowCtrl.toolIDs.ContainsKey(item.Key)
            && smallFlowCtrl.toolIDs[item.Key].PropType != PropType.SafetyTool).Select(item => item.Value).ToList();

        foreach (var toggle in list)
            toggle.isOn = false;

        backpackTog.isOn = false;
    }

    /// <summary>
    /// 取消选中图纸按钮
    /// </summary>
    public void CancelDrawingToggle()
    {
        if (drawingToggle != null && drawingToggle.isOn)
        {
            drawingToggle.isOn = false;
        }
    }

    /// <summary>
    /// 关闭常驻工具
    /// </summary>

    private void PermanentTogglesOff()
    {
        foreach (Toggle tog in permanentToggles)
            tog.isOn = false;
    }


    /// <summary>
    /// 刷新提示高亮
    /// </summary>
    private void RefreshTip()
    {
        if (!GlobalInfo.EnableFlow || smallFlowCtrl.nowFlowStep == null || GlobalInfo.isExam)
        {
            StopHighlight();
            return;
        }

        Dictionary<int, Sequence> newHighlights = new Dictionary<int, Sequence>();
        Transform tmpTooltem = null;
        int tmpToolID;
        foreach (var smallOp in smallFlowCtrl.nowFlowStep.ops.Except(smallFlowCtrl.successOPs, new SmallOpEqualityComparer()))
        {
            if (smallOp.optionName.Equals(SmallFlowCtrl.contactFlag))//联系
            {
                if (!contactTog.isOn)
                    tmpTooltem = items[SmallFlowCtrl.contactFlag].transform;
            }
            else if (smallOp.optionName.Equals(SmallFlowCtrl.inputFlag))//输入
            {
                if (!inputHistoryTog.isOn)
                    tmpTooltem = items[SmallFlowCtrl.historyFlag].transform;
            }
            else if (smallOp.optionName.Equals(SmallFlowCtrl.observeFlag))//观察
            {
                if (string.IsNullOrEmpty(prop) || !prop.Equals(SmallFlowCtrl.observeFlag))
                    tmpTooltem = items[SmallFlowCtrl.observeFlag].transform;
            }
            else /*if (!smallOp.optionName.Equals(SmallFlowCtrl.inputFlag))//不是输入*/
            {
                if (smallOp.prop == null)//空手操作
                {
                    //// 检查玩家当前是否已经选中了"手部操作"
                    //if (string.IsNullOrEmpty(prop) || !prop.Equals(SmallFlowCtrl.handFlag))
                    //    // 如果没有选中"手部操作"，则获取手部操作的UI项用于高亮提示
                    //    tmpTooltem = items[SmallFlowCtrl.handFlag].transform;
                }
                else//道具操作
                {
                    //if (smallOp.prop.PropType == PropType.MasterComputer)
                    //{
                    //    tmpToolID = smallOp.operation.transform.GetInstanceID();
                    //    if (!newHighlights.ContainsKey(tmpToolID))
                    //    {
                    //        if (!highlights.ContainsKey(tmpToolID))
                    //            newHighlights.Add(tmpToolID, UIHighlight(smallOp.operation.transform));
                    //        else
                    //            newHighlights.Add(tmpToolID, highlights[tmpToolID]);
                    //    }
                    //}

                    if (string.IsNullOrEmpty(prop) || !prop.Equals(smallOp.prop.ID))
                    {
                        if (!backpackTog.isOn && (smallOp.prop.PropType == PropType.BackPack
                            || smallOp.prop.PropType == PropType.BackPack_Original
                            || smallOp.prop.PropType == PropType.SafetyTool))
                        {
                            tmpToolID = backpackTog.transform.GetInstanceID();
                            if (!newHighlights.ContainsKey(tmpToolID))
                            {
                                if (!highlights.ContainsKey(tmpToolID))
                                    newHighlights.Add(tmpToolID, UIHighlight(backpackTog.transform));
                                else
                                    newHighlights.Add(tmpToolID, highlights[tmpToolID]);
                            }
                        }

                        if (items.ContainsKey(smallOp.prop.ID))
                            tmpTooltem = items[smallOp.prop.ID].transform;
                    }
                }
            }

            if (tmpTooltem)
            {
                tmpToolID = tmpTooltem.GetInstanceID();
                if (!newHighlights.ContainsKey(tmpToolID))
                {
                    if (!highlights.ContainsKey(tmpToolID))
                        newHighlights.Add(tmpToolID, UIHighlight(tmpTooltem));
                    else
                        newHighlights.Add(tmpToolID, highlights[tmpToolID]);
                }
            }
        }

        foreach (var component in highlights.Except(newHighlights))
        {
            component.Value.Kill();
        }

        highlights = newHighlights;
    }
    private Sequence UIHighlight(Transform item)
    {
        var sequence = DOTween.Sequence();
        {
            var image = item.GetComponentByChildName<Image>("Highlight");
            {
                image.gameObject.SetActive(true);
                image.SetAlpha(1f);
                sequence.Append(image.DOFade(0, 0.8f));
            }

            var text = item.GetComponentByChildName<Text>("Name");
            {
                text.SetAlpha(1f);
                sequence.Join(text.DOFade(0, 0.8f));
            }

            sequence.SetId(item.GetInstanceID());
            sequence.SetLoops(-1, LoopType.Yoyo);
            sequence.OnKill(() =>
            {
                image.SetAlpha(0f);
                text.SetAlpha(1f);
            });
        }

        return sequence;
    }

    private void StopHighlight()
    {
        foreach (var highlight in highlights)
        {
            highlight.Value.Kill();
        }

        highlights.Clear();
    }
    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);

        switch (msg.msgId)
        {
            case (ushort)SmallFlowModuleEvent.SelectFlow:
            case (ushort)SmallFlowModuleEvent.SelectStep:
            case (ushort)SmallFlowModuleEvent.CompleteExecute:
                RefreshTip();
                interactable = true;
                break;
            case (ushort)SmallFlowModuleEvent.StartExecute:
            case (ushort)SmallFlowModuleEvent.OperatingRecordInput:
                //interactable = false;
                break;
            case (ushort)SmallFlowModuleEvent.ShowUIOperation:
                //todo 显示UI操作不禁用工具栏(聚焦操作对象自动显示OpUI)
                //interactable = !(msg as MsgBool).arg1;
                break;
            case (ushort)SmallFlowModuleEvent.CompleteStep:
                interactable = true;
                break;
            case (ushort)SmallFlowModuleEvent.OperatingRecordClear:
                MsgIntInt msgIntInt = msg as MsgIntInt;
                if (msgIntInt.arg1 >= 0 && msgIntInt.arg2 >= 0)
                    interactable = true;
                break;
            case (ushort)SmallFlowModuleEvent.SelectInput:
                inputHistoryTog.isOn = (msg as MsgStringBool).arg2;
                break;
            case (ushort)SmallFlowModuleEvent.SelectContact:
                contactTog.isOn = (msg as MsgBool).arg1;
                break;
            case (ushort)SmallFlowModuleEvent.ShowTool:
                ShowTool((msg as MsgBool).arg1);
                break;
            case (ushort)SmallFlowModuleEvent.Guide:
                MsgTuple<int, int, string> msgTuple = msg as MsgTuple<int, int, string>;
                if (smallFlowCtrl.flows[msgTuple.arg.Item1].steps[msgTuple.arg.Item2].ops.Any(o => o.prop != null && o.prop.PropType == PropType.MasterComputer))
                {
                    items[SmallFlowCtrl.masterFlag].isOn = true;
                }
                break;
            default:
                ShortcutMsg(msg);
                break;
        }
    }

    private bool show = true;

    /// <summary>
    /// 显示工具栏
    /// </summary>
    /// <param name="show"></param>
    public void ShowTool(bool show, float delay = 0f)
    {
        if (this.show == show)
            return;
        this.show = show;
        DOTween.Kill("ShowTool");
        Sequence sequence = DOTween.Sequence();
        sequence.AppendInterval(delay);
        sequence.Append(ToolsCanvasGroup.DOFade(show ? 1 : 0, delay)).SetId("ShowTool");
    }

    #region 快捷键部分
    private List<string> flags = new List<string>()
    {
        ShortcutManager.SmallScene_OpenItem01,
        ShortcutManager.SmallScene_OpenItem02,
        ShortcutManager.SmallScene_OpenItem03,
        ShortcutManager.SmallScene_OpenItem04,
        ShortcutManager.SmallScene_OpenItem05,
        ShortcutManager.SmallScene_OpenItem06,
        ShortcutManager.SmallScene_OpenItem07,
        ShortcutManager.SmallScene_OpenItem08,
        ShortcutManager.SmallScene_OpenItem09,
        ShortcutManager.SmallScene_OpenItem10
    };
    //todo 组合快捷键？
    private void ShortcutMsg(MsgBase msg)
    {
        if (/*inputHistoryTog.isOn || */!interactable)
            return;

        if (msg.msgId == (ushort)ShortcutEvent.PressAnyKey)
        {
            var data = msg as MsgShortcut;

            if (data.state == 1)
            {
                foreach (var item in data.keys)
                {
                    TrySelect(flags.FindIndex(0, value => value == item));
                }
            }
        }
    }
    private void TrySelect(int index)
    {
        if (index == -1)
            return;

        if (interactable)
        {
            var list = items.Values.Where(toggle => toggle.transform.parent == ToolContent && toggle.gameObject.activeInHierarchy).ToList();
            {
                if (list.Count > index)
                {
                    list[index].isOn = !list[index].isOn;
                }
            }
        }
    }
    #endregion
}