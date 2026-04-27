using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;
using UnityFramework.Runtime;
using System;
using Cysharp.Threading.Tasks;
using static UnityFramework.Runtime.RequestData;

public class InpuAndHistoryData : UIData
{
    public SmallFlowCtrl smallFlowCtrl;
    public UISmallSceneModule smallSceneModule;
    /// <summary>
    /// 是否是自由模式
    /// </summary>
    public bool isFree;
    /// <summary>
    /// 输入和历史记录初始化需要数据
    /// </summary>
    public InpuAndHistoryData(SmallFlowCtrl smallFlowCtrl, UISmallSceneModule smallSceneModule, bool isFree)
    {
        this.smallFlowCtrl = smallFlowCtrl;
        this.smallSceneModule = smallSceneModule;
        this.isFree = isFree;
    }
}

/// <summary>
/// 模拟操作 操作记录列表模块
/// </summary>
public class UISmallSceneOperationHistory : UIModuleBase
{
    /// <summary>
    /// 操作类型
    /// </summary>
    public enum OpType
    {
        Contact,//联系
        Input,//检查
        Operation,//操作
        System, //系统
    }

    public class OpRecordData
    {
        public int index;
        public string userNo;
        public string userName;
        public string msg;
        public string createTime;
        public int type;

        public OpRecordData() { }

        public OpRecordData(int index, string userNo, string userName, string msg, string createTime, OpType opType)
        {
            this.index = index;
            this.userNo = userNo;
            this.userName = userName;
            this.msg = msg;
            this.createTime = createTime;
            this.type = (int)opType;
        }

        public ExamineResultOperation ToExamineResult()
        {
            return new ExamineResultOperation()
            {
                index = index,
                userNo = userNo,
                userName = userName,
                msg = msg,
                createTime = createTime,
                type = type
            };
        }
    }

    private RectTransform Background;
    private Transform history;

    #region 记录
    private Transform input;
    private InputField inputField;
    private Button inputCancelBtn;
    private Button inputEnterBtn;
    private CanvasGroup inputCanvas;
    #endregion

    #region 联系
    private Transform contact;
    private InputField contactInputField;
    private Button contactCancelBtn;
    private Button contactEnterBtn;
    private CanvasGroup contactCanvas;
    #endregion

    private Transform content;
    private Transform item_OP;
    private Transform item_SYS;
    private Transform item_Input;
    private Transform item_Line;
    private Scrollbar scrollbar;

    private SmallFlowCtrl smallFlowCtrl;
    private UISmallSceneModule smallSceneModule;

    private List<GameObject> lines = new List<GameObject>();

    private List<Transform> items = new List<Transform>();

    private List<OpRecordData> opRecords = new List<OpRecordData>();

    public List<OpRecordData> OpRecordList => opRecords;

    /// <summary>
    /// 操作记录列表改变事件
    /// </summary>
    public UnityEvent<OpRecordData> OnRecordChanged = new UnityEvent<OpRecordData>();
    /// <summary>
    /// 操作记录项
    /// </summary>
    private Dictionary<int, Dictionary<int, Transform>> itemsDic = new Dictionary<int, Dictionary<int, Transform>>();
    /// <summary>
    /// 操作记录数据，int1：任务id，int2：步骤id，string：记录文本
    /// </summary>
    private Dictionary<int, Dictionary<int, OpRecordData>> opRecordDic = new Dictionary<int, Dictionary<int, OpRecordData>>();

    private Sequence sequence;
    private InpuAndHistoryData data;

    private bool show;

    /// <summary>
    /// 控制当前是否可以输入
    /// </summary>
    private bool _interactable = true;
    public bool inputInteractable
    {
        get
        {
            return _interactable;
        }
        set
        {
            _interactable = value;

            inputCanvas.alpha = _interactable ? 1 : 0.5f;
            inputCanvas.blocksRaycasts = _interactable;
        }
    }

    /// <summary>
    /// 是否允许更新操作记录（考核计时结束）
    /// </summary>

    private bool canCreateHistoryItem = true;

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        AddMsg(new ushort[]{
            (ushort)HistoryEvent.Show,
            (ushort)HistoryEvent.Hide,
            (ushort)SmallFlowModuleEvent.Input,
            (ushort)SmallFlowModuleEvent.Contact,
            (ushort)SmallFlowModuleEvent.StartExecute,
            (ushort)SmallFlowModuleEvent.CompleteExecute,
            (ushort)SmallFlowModuleEvent.CompleteStep,
            (ushort)SmallFlowModuleEvent.SelectFlow,
            (ushort)SmallFlowModuleEvent.SelectStep,
            (ushort)SmallFlowModuleEvent.OperatingRecordChange,
            (ushort)SmallFlowModuleEvent.OperatingRecord,
            (ushort)SmallFlowModuleEvent.OperatingRecordInput,
            (ushort)SmallFlowModuleEvent.OperatingRecordClear,
            (ushort)SmallFlowModuleEvent.SystemRecord,
            (ushort)SmallFlowModuleEvent.LeftFlex,
            (ushort)SmallFlowModuleEvent.SelectInput,
            (ushort)SmallFlowModuleEvent.SelectContact,
            (ushort)SmallFlowModuleEvent.FocusChanged,
            (ushort)SmallFlowModuleEvent.MasterComputerSelect,
            (ushort)ExamPanelEvent.Timeout,
            (ushort)ExamPanelEvent.LocalTimeout
        });

        data = uiData as InpuAndHistoryData;

        smallFlowCtrl = data.smallFlowCtrl;
        smallSceneModule = data.smallSceneModule;

        Background = this.GetComponentByChildName<RectTransform>("Background");
        this.GetComponentByChildName<Button>("Close").onClick.AddListener(() => SendMsg(new MsgBase((ushort)HistoryEvent.Hide)));

        history = transform.FindChildByName("History");
        content = history.FindChildByName("Content");
        item_OP = history.FindChildByName("Item_OP");
        item_SYS = history.FindChildByName("Item_SYS");
        item_Input = history.FindChildByName("Item_Input");
        item_Line = history.FindChildByName("Item_Line");
        scrollbar = history.GetComponentByChildName<Scrollbar>("Scrollbar Vertical");

        item_OP.gameObject.SetActive(false);
        item_SYS.gameObject.SetActive(false);
        item_Input.gameObject.SetActive(false);
        item_Line.gameObject.SetActive(false);
        //history.gameObject.SetActive(false);

        InitInputComponents();
        InitContactComponents();
#if UNITY_STANDALONE
#endif
    }

    private void InitInputComponents()
    {
        input = transform.FindChildByName("Input");
        inputField = input.GetComponentByChildName<InputField>("InputField");
        inputCancelBtn = input.GetComponentByChildName<Button>("Btn1");
        inputEnterBtn = input.GetComponentByChildName<Button>("Btn2");
        inputEnterBtn.interactable = false;
        inputCanvas = input.GetComponent<CanvasGroup>();

        inputField.onValidateInput = (string text, int charIndex, char addedChar) =>
        {
            //Enter提交
            if (addedChar == '\n')
            {
                inputEnterBtn.onClick?.Invoke();
                return '\0';
            }
            return addedChar;
        };

        inputField.onValueChanged.AddListener((str) =>
        {
            inputEnterBtn.interactable = !string.IsNullOrEmpty(str.Trim());
        });

        inputEnterBtn.onClick.AddListener(() =>
        {
            string value = inputField.text.Trim();
            if (!string.IsNullOrEmpty(value))
            {
                ToolManager.SendBroadcastMsg(new MsgTuple<string, string, string,string>()
                {
                    msgId = (ushort)SmallFlowModuleEvent.Input,
                    arg = new Tuple<string, string, string,string>(GlobalInfo.account.userNo, GlobalInfo.account.nickname, value/*smallFlowCtrl.IsOnOperation()*/ ,GlobalInfo.ServerTimeFormat)
                });
            }
            input.gameObject.SetActive(false);
        });

        inputCancelBtn.onClick.AddListener(() =>
        {
            FormMsgManager.Instance.SendMsg(new MsgStringBool((ushort)SmallFlowModuleEvent.SelectInput, string.Empty, false));
        });
    }

    private void InitContactComponents()
    {
        contact = transform.FindChildByName("Contact");
        contactInputField = contact.GetComponentByChildName<InputField>("InputField");
        contactCancelBtn = contact.GetComponentByChildName<Button>("Btn1");
        contactEnterBtn = contact.GetComponentByChildName<Button>("Btn2");
        contactEnterBtn.interactable = false;
        contactCanvas = contact.GetComponent<CanvasGroup>();

        contactInputField.onValidateInput = (string text, int charIndex, char addedChar) =>
        {
            //Enter提交
            if (addedChar == '\n')
            {
                contactEnterBtn.onClick?.Invoke();
                return '\0';
            }
            return addedChar;
        };

        contactInputField.onValueChanged.AddListener((str) =>
        {
            contactEnterBtn.interactable = !string.IsNullOrEmpty(str.Trim());
        });

        contactEnterBtn.onClick.AddListener(() =>
        {
            string value = contactInputField.text.Trim();
            if (!string.IsNullOrEmpty(value))
            {
                ToolManager.SendBroadcastMsg(new MsgTuple<string, string, string, string>()
                {
                    msgId = (ushort)SmallFlowModuleEvent.Contact,
                    arg = new Tuple<string, string, string, string>(GlobalInfo.account.userNo, GlobalInfo.account.nickname, value/*smallFlowCtrl.IsOnOperation()*/ , GlobalInfo.ServerTimeFormat)
                });
            }
            contact.gameObject.SetActive(false);
        });

        contactCancelBtn.onClick.AddListener(() =>
        {
            FormMsgManager.Instance.SendMsg(new MsgBool((ushort)SmallFlowModuleEvent.SelectContact, false));
        });
    }

    private void GotoNextStep()
    {
        inputInteractable = true;

        if (data.isFree)
        {
            if (itemsDic.ContainsKey(smallFlowCtrl.index_NowFlow)
                && itemsDic[smallFlowCtrl.index_NowFlow].ContainsKey(smallFlowCtrl.index_NowStep))
            {
                InputField inputItem = itemsDic[smallFlowCtrl.index_NowFlow][smallFlowCtrl.index_NowStep].GetComponent<InputField>();
                if (inputItem)
                    inputItem.interactable = false;
            }
        }
        smallFlowCtrl.Next();
    }

    /// <summary>
    /// 清除记录
    /// </summary>
    private void ClearRecord()
    {
        for (int i = 0; i < lines.Count; i++)
        {
            Destroy(lines[i]);
        }
        lines.Clear();

        foreach (var itemDatas in itemsDic)
        {
            foreach (var itemData in itemDatas.Value)
            {
                Destroy(itemData.Value.gameObject);
            }
        }
        itemsDic.Clear();

        opRecordDic.Clear();

        for (int i = 0; i < items.Count; i++)
        {
            Destroy(items[i].gameObject);
        }
        items.Clear();

        opRecords.Clear();
    }

    /// <summary>
    /// 更新操作记录
    /// </summary>
    public void UpdateOpRecordList(List<OpRecordData> data)
    {
        opRecords = data;
        if (opRecords == null)
            opRecords = new List<OpRecordData>();
        UpdateOpRecord();
    }

    /// <summary>
    /// 更新操作记录
    /// </summary>
    private void UpdateOpRecord()
    {
        for (int i = 0; i < lines.Count; i++)
        {
            Destroy(lines[i]);
        }
        lines.Clear();

        foreach (var itemDatas in itemsDic)
        {
            foreach (var itemData in itemDatas.Value)
            {
                Destroy(itemData.Value.gameObject);
            }
        }
        itemsDic.Clear();

        for (int i = 0; i < items.Count; i++)
        {
            Destroy(items[i].gameObject);
        }
        items.Clear();

        for (int i = 0; i < opRecords.Count; i++)
        {
            CreateItem(i, opRecords[i].userName, opRecords[i].msg, opRecords[i].createTime, (OpType)opRecords[i].type);
        }
        OpRecordShowLast(this.GetCancellationTokenOnDestroy()).Forget();
    }

    /// <summary>
    /// 追加操作记录
    /// </summary>
    /// <param name="index"></param>
    /// <param name="data"></param>
    private void AddOpRecord(int index, OpRecordData data)
    {
        CreateItem(index, data.userName, data.msg, data.createTime, (OpType)data.type);
    }

    /// <summary>
    /// 修改操作记录
    /// </summary>
    private void ChangeOpRecord(int index, OpRecordData data)
    {
        CreateItem(index, data.userName, data.msg, data.createTime, (OpType)data.type);
    }

    private void CreateItem(int index, string user, string msg, string createTime, OpType opType)
    {
        if (index >= 0 && index < items.Count)
        {
            switch (opType)
            {
                case OpType.Input:
                case OpType.Contact:
                    items[index].GetComponent<InputField>().text = msg;
                    break;
                default:
                    items[index].GetComponent<Text>().text = msg;
                    break;
            }
            return;
        }

        Transform itemTemp = null;
        switch (opType)
        {
            case OpType.Contact:
            case OpType.Input:
                itemTemp = item_Input;
                break;
            case OpType.System:
                itemTemp = item_SYS;
                break;
            default:
                itemTemp = item_OP;
                break;
        }

        Transform item = Instantiate(itemTemp, content);
        switch (opType)
        {
            case OpType.Contact:
            case OpType.Input:
                InputField input = item.GetComponent<InputField>();
                input.text = msg;
                input.onEndEdit.AddListener((str) =>
                {
                    //发送文本修改消息
                    ToolManager.SendBroadcastMsg(new MsgOperatingRecord((ushort)SmallFlowModuleEvent.OperatingRecordChange,
                        string.Empty, str, -1, -1, index, GlobalInfo.account.userNo, GlobalInfo.account.nickname, createTime, opType), true);

                    LayoutRebuilder.ForceRebuildLayoutImmediate(input.textComponent.rectTransform);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(content.GetComponent<RectTransform>());
                });
                break;
            default:
                item.GetComponent<Text>().text = msg;
                break;
        }
        item.gameObject.SetActive(true);
        items.Add(item);
    }

    /// <summary>
    /// 操作记录显示最新记录
    /// </summary>
    /// <returns></returns>
    private async UniTaskVoid OpRecordShowLast(System.Threading.CancellationToken ct)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(0.5), cancellationToken: ct);
        scrollbar.value = 0;
    }

    /// <summary>
    /// UI高亮提示
    /// </summary>
    private void RefreshUIHighlight()
    {
        if (!data.isFree && smallFlowCtrl != null && smallFlowCtrl.nowFlowStep != null)
        {
            foreach (var smallOp in smallFlowCtrl.nowFlowStep.ops)
            {
                if (smallOp.optionName.Equals(SmallFlowCtrl.inputFlag))
                {
                    if (sequence == null)//已高亮则不处理
                    {
                        sequence = DOTween.Sequence();
                        var image = input.GetComponentByChildName<Image>("Highlight");
                        {
                            image.gameObject.SetActive(true);
                            image.SetAlpha(1f);
                            sequence.Append(image.DOFade(0, 0.8f));
                        }

                        sequence.SetId("Highlight");
                        sequence.SetLoops(-1, LoopType.Yoyo);
                        sequence.OnKill(() =>
                        {
                            image.SetAlpha(0f);
                            sequence = null;
                        });
                    }
                    return;
                }
            }
        }

        if (sequence != null) sequence.Kill();
    }


    private void ProcessMsg(int index, string userNo, string userName, string hint, string createTime, int opType)
    {
        OpRecordData opRecord = new OpRecordData(index, userNo, userName, hint, createTime, (OpType)opType);

        if (opRecords.Count > 0 && index < opRecords.Count)
        {
            opRecords[index] = opRecord;
            if (show)
                ChangeOpRecord(index, opRecord);
        }
        else
        {
            if (show)
                AddOpRecord(opRecords.Count, opRecord);
            opRecords.Add(opRecord);
        }

        OnRecordChanged?.Invoke(opRecord);
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);

        switch (msg.msgId)
        {
            case (ushort)HistoryEvent.Show:
                OpenModule();
                break;
            case (ushort)HistoryEvent.Hide:
                HideModule();
                break;
            case (ushort)SmallFlowModuleEvent.Input:
                OnInput(msg);
                break;
            case (ushort)SmallFlowModuleEvent.Contact:
                OnContact(msg);
                break;
            case (ushort)SmallFlowModuleEvent.OperatingRecord:
                MsgOperatingRecord opMsg = msg as MsgOperatingRecord;
                if (opMsg.createHistoryItem && canCreateHistoryItem)
                {
                    ProcessMsg(opRecords.Count, opMsg.userNo, opMsg.userName, opMsg.opHint, opMsg.createTime, opMsg.opType);
                }
                break;
            case (ushort)SmallFlowModuleEvent.OperatingRecordInput:
                MsgOperatingRecord opInput = msg as MsgOperatingRecord;
                ProcessMsg(opRecords.Count, opInput.userNo, opInput.userName, opInput.opHint, opInput.createTime, opInput.opType);
                break;
            case (ushort)SmallFlowModuleEvent.OperatingRecordChange:
                MsgOperatingRecord opInputChange = ((MsgBrodcastOperate)msg).GetData<MsgOperatingRecord>();
                ProcessMsg(opInputChange.opIndex, opInputChange.userNo, opInputChange.userName, opInputChange.opHint, opInputChange.createTime, opInputChange.opType);
                break;
            case (ushort)SmallFlowModuleEvent.OperatingRecordClear:
                MsgIntInt msgIntInt = msg as MsgIntInt;
                if (msgIntInt.arg1 < 0)
                    ClearRecord();
                break;
            case (ushort)SmallFlowModuleEvent.SystemRecord:
                if (NetworkManager.Instance.IsIMSyncState || !canCreateHistoryItem)
                    return;
                MsgOperatingRecord opSystem = msg as MsgOperatingRecord;
                ProcessMsg(opRecords.Count, opSystem.userNo, opSystem.userName, opSystem.opHint, opSystem.createTime, opSystem.opType);
                break;
            case (ushort)SmallFlowModuleEvent.StartExecute:
                //协同/考核非本人操作
                if ((msg as MsgStringBool).arg2)
                    return;
                //inputInteractable = false;
                break;
            case (ushort)SmallFlowModuleEvent.CompleteStep:
                inputInteractable = true;
                break;
            case (ushort)SmallFlowModuleEvent.CompleteExecute:
                //inputInteractable = true;
                OnStepChanged();
                break;
            case (ushort)SmallFlowModuleEvent.SelectFlow:
            case (ushort)SmallFlowModuleEvent.SelectStep:
                OnStepChanged();
                break;
            case (ushort)SmallFlowModuleEvent.SelectInput:
                inputField.text = (msg as MsgStringBool).arg1;
                bool showInput = (msg as MsgStringBool).arg2;
                input.gameObject.SetActive(showInput);
                if (showInput)
                {
                    inputField.ActivateInputField();
                    SetCaretPositionNextFrame(inputField, this.GetCancellationTokenOnDestroy()).Forget();
                }
                break;
            case (ushort)SmallFlowModuleEvent.SelectContact:
                bool showContact = (msg as MsgBool).arg1;
                contact.gameObject.SetActive(showContact);
                if (showContact)
                {
                    contactInputField.ActivateInputField();
                    SetCaretPositionNextFrame(contactInputField, this.GetCancellationTokenOnDestroy()).Forget();
                }
                break;
            case (ushort)SmallFlowModuleEvent.FocusChanged:
                if(GlobalInfo.ShouldProcess((msg as MsgBrodcastOperate).senderId))
                {
                    if (inputField.gameObject.activeInHierarchy)
                    {
                        var focusData = (msg as MsgBrodcastOperate).GetData<MsgStringString>();
                        inputField.text = focusData.arg1;
                    }
                }        
                break;
            case (ushort)SmallFlowModuleEvent.MasterComputerSelect:
                if (GlobalInfo.ShouldProcess((msg as MsgBrodcastOperate).senderId))
                {
                    if (inputField.gameObject.activeInHierarchy)
                        inputField.text = $"{(msg as MsgBrodcastOperate).GetData<MsgElement>().name}：";
                }
                break;
            case (ushort)ExamPanelEvent.Timeout://房主端计时结束
            case (ushort)ExamPanelEvent.LocalTimeout:
                canCreateHistoryItem = false;
                break;
            default:
                break;
        }
    }

    private async UniTaskVoid SetCaretPositionNextFrame(InputField inputField, System.Threading.CancellationToken ct)
    {
        //等待一帧
        await UniTask.Yield(ct);
        //设置光标位置到末尾
        inputField.caretPosition = inputField.text.Length;
        inputField.selectionAnchorPosition = inputField.caretPosition;
        inputField.selectionFocusPosition = inputField.caretPosition;
    }

    private void OnInput(MsgBase msg)
    {
        if(smallSceneModule.FatalFinish)
        {
            smallSceneModule.ShowFatalPopup();
            return;
        }

        // TODO 避免操作表现执行过程中，记录操作导致步骤切换(考核不存在步骤)
        if (!smallSceneModule.IsOperatableState || (!GlobalInfo.IsExamMode() && smallSceneModule.OtherOperating))
        {
            UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("操作执行中，完成后再试"));
            return;
        }

        contactInputField.text = string.Empty;
        contactCancelBtn.onClick?.Invoke();

        //当前步骤是记录操作
        MsgTuple<string, string, string, string> msgTupleString = ((MsgBrodcastOperate)msg).GetData<MsgTuple<string, string, string, string>>();
        if (smallFlowCtrl.IsOnOperation(SmallFlowCtrl.inputFlag/*value*/))
        {
            //发送输入文本消息
            SendMsg(new MsgOperatingRecord((ushort)SmallFlowModuleEvent.OperatingRecordInput,
                string.Empty, msgTupleString.arg.Item3, smallFlowCtrl.index_NowFlow, smallFlowCtrl.index_NowStep, -1,
                msgTupleString.arg.Item1, msgTupleString.arg.Item2, msgTupleString.arg.Item4, OpType.Input));
            GotoNextStep();
        }
        else
        {
            //非考核模式需要错误提示
            if (!GlobalInfo.isExam && ((MsgBrodcastOperate)msg).senderId == GlobalInfo.account.id)
            {
                data.smallSceneModule.OnErrorShow();
                FormMsgManager.Instance.SendMsg(new MsgString((ushort)SmallFlowModuleEvent.CompleteExecute, string.Empty));
            }
            else if (GlobalInfo.isExam)
            {
                //发送输入文本消息
                SendMsg(new MsgOperatingRecord((ushort)SmallFlowModuleEvent.OperatingRecordInput,
                    string.Empty, msgTupleString.arg.Item3, smallFlowCtrl.index_NowFlow, smallFlowCtrl.index_NowStep, -1,
                    msgTupleString.arg.Item1, msgTupleString.arg.Item2, msgTupleString.arg.Item4, OpType.Input));
            }
        }
    }

    private void OnContact(MsgBase msg)
    {
        if (smallSceneModule.FatalFinish)
        {
            smallSceneModule.ShowFatalPopup();
            return;
        }
        // TODO
        if (!smallSceneModule.IsOperatableState || (!GlobalInfo.IsExamMode() && smallSceneModule.OtherOperating))
        {
            UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("操作执行中，完成后再试"));
            return;
        }


        contactInputField.text = string.Empty;
        contactCancelBtn.onClick?.Invoke();
        //当前步骤是联系操作
        MsgTuple<string, string, string, string> msgTupleString = ((MsgBrodcastOperate)msg).GetData<MsgTuple<string, string, string, string>>();
        if (smallFlowCtrl.IsOnOperation(SmallFlowCtrl.contactFlag))
        {
            //发送输入文本消息
            SendMsg(new MsgOperatingRecord((ushort)SmallFlowModuleEvent.OperatingRecordInput,
                string.Empty, msgTupleString.arg.Item3, smallFlowCtrl.index_NowFlow, smallFlowCtrl.index_NowStep, -1,
                msgTupleString.arg.Item1, msgTupleString.arg.Item2, msgTupleString.arg.Item4, OpType.Contact));
            GotoNextStep();
        }
        else
        {
            //考核模式不进行错误提示
            if (!GlobalInfo.isExam && ((MsgBrodcastOperate)msg).senderId == GlobalInfo.account.id)
            {
                data.smallSceneModule.OnErrorShow();
                FormMsgManager.Instance.SendMsg(new MsgString((ushort)SmallFlowModuleEvent.CompleteExecute, string.Empty));
            }
            else if (GlobalInfo.isExam)
            {
                //发送输入文本消息
                SendMsg(new MsgOperatingRecord((ushort)SmallFlowModuleEvent.OperatingRecordInput,
                    string.Empty, msgTupleString.arg.Item3, smallFlowCtrl.index_NowFlow, smallFlowCtrl.index_NowStep, -1,
                    msgTupleString.arg.Item1, msgTupleString.arg.Item2, msgTupleString.arg.Item4, OpType.Contact));
            }
        }
    }

    private void OnStepChanged()
    {
        contactInputField.text = string.Empty;
        inputField.text = data.smallSceneModule.FocusModelDescrption;
    }

    public override void Hide(UIData uiData = null, UnityAction callback = null)
    {
        base.Hide(uiData, callback);
        show = false;
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        base.Close(uiData, callback);
        if (inputCanvas != null)
        {
            inputCanvas.alpha = 0;
            inputCanvas.blocksRaycasts = false;
        }
        if (contactCanvas != null)
        {
            contactCanvas.alpha = 0;
            contactCanvas.blocksRaycasts = false;
        }
    }


    public void DisableInputItems()
    {
        foreach(var item in items)
        {
            if(item.TryGetComponent(out InputField inputField))
            {
                inputField.interactable = false;
            }
        }
    }

    #region 动效
    protected override float joinAnimePlayTime => 0.3f;
    protected override float exitAnimePlayTime => 0.2f;

    private void OpenModule()
    {
        show = true;
        Background.gameObject.SetActive(true);
        UpdateOpRecord();
#if UNITY_ANDROID || UNITY_IOS
        JoinSequence.Join(Background.DOAnchorPos3DX(0, JoinAnimePlayTime));
#else
        JoinSequence.Join(Background.DOAnchorPos3DX(44f, JoinAnimePlayTime));
#endif
    }

    private void HideModule()
    {
        show = false;
#if UNITY_ANDROID || UNITY_IOS
        Background.DOAnchorPos3DX(Background.sizeDelta.x, ExitAnimePlayTime).OnComplete(() => Background.gameObject.SetActive(false));
#else
        Background.DOAnchorPos3DX(-Background.sizeDelta.x, ExitAnimePlayTime);
#endif
    }

    public override void ExitAnim(UnityAction callback)
    {
#if UNITY_ANDROID || UNITY_IOS
        ExitSequence.Join(Background.DOAnchorPos3DX(Background.sizeDelta.x, ExitAnimePlayTime));
#else
        ExitSequence.Join(Background.DOAnchorPos3DX(-Background.sizeDelta.x, ExitAnimePlayTime));
#endif
        base.ExitAnim(callback);
    }
    #endregion
}

public class OpRecordDicData
{
    public int index_Flow;
    public int index_Step;
    public string userName;
    public string msg;
    public bool isInput;
    public OpRecordDicData(int index_Flow, int index_Step, string userName, string msg, bool isInput)
    {
        this.index_Flow = index_Flow;
        this.index_Step = index_Step;
        this.userName = userName;
        this.msg = msg;
        this.isInput = isInput;
    }
}