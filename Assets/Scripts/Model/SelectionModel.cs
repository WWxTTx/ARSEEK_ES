using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityFramework.Runtime;

/// <summary>
/// 选中模型
/// </summary>
public class SelectionModel : MonoBase
{
    public class OnSelectModel : UnityEvent<GameObject, int> { }
    public OnSelectModel onSelectModel = new OnSelectModel();
    public OnSelectModel onDeSelectModel = new OnSelectModel();
    public UnityEvent onClearSelection = new UnityEvent();

    /// <summary>
    /// 用户选中模型集合
    /// </summary>
    public Dictionary<GameObject, int> userSelectModels = new Dictionary<GameObject, int>();

    public GameObject selectModel { get; private set; }

    /// <summary>
    /// 本地选中模型
    /// </summary>
    public GameObject localSelectModel { get; private set; }

    /// <summary>
    /// 描边固定颜色
    /// </summary>
    public Color highlightColor = new Color(0.9686275f, 0.3176471f, 0.2588235f);

    //public bool disableDeSelect;
    //private GameObject defaultSelectModel;
    ///// <summary>
    ///// 是否按下
    ///// </summary>
    //private bool isdown = false;
    ///// <summary>
    ///// 鼠标按下位置
    ///// </summary>
    //private Vector3 downPoint;
    ///// <summary>
    ///// 父节点居中位置
    ///// </summary>
    //private Vector3 centerPos;
    //private Camera mainCam;

    public bool CanDeselect = true;

    private void Awake()
    {
        TapRecognizer.Instance.RegistOnLeftMouseEmptyClick(Deselect);
    }

    protected override void Start()
    {
        base.Start();
        //mainCam = Camera.main;

        AddMsg(new ushort[] 
        {
            (ushort)ModelOperateEvent.Click,
            (ushort)RoomChannelEvent.OtherLeave,
            (ushort)RoomChannelEvent.UpdateMemberList,
            (ushort)RoomChannelEvent.UpdateControl,
            (ushort)StateEvent.PreSyncVersion
        });

        CollisionBoxMouseEvent[] events = GetComponentsInChildren<CollisionBoxMouseEvent>(true);
        for (int i = 0; i < events.Length; i++)
        {
            events[i].onClick1.AddListener(SetSelect);
        }

        //defaultSelectModel = ModelManager.Instance.modelGo;
    }
    public void SetSelect(GameObject go)
    {
        if (!CanDeselect)
            return;

        //if (ForbidSelectNull && go == null)
        //    return;

        if(go != null && userSelectModels.ContainsKey(go))
        {
            if(userSelectModels[go] != GlobalInfo.account.id)
            {
                UIManager.Instance.OpenModuleUI<ToastPanel>(null, UILevel.PopUp, new ToastPanelInfo("该模型已被其他成员选中"));
            }
            return;
        }
        ToolManager.SendBroadcastMsg(new MsgIntString((ushort)ModelOperateEvent.Click, GlobalInfo.account.id, ModelManager.Instance.GetUUIDByModel(go)), true);
    }

    /// <summary>
    /// 选择模型
    /// </summary>
    /// <param name="go"></param>
    /// <param name="userId"></param>
    public void SelectModel(GameObject go, int userId)
    {
        if (go && userSelectModels.ContainsKey(go))
            return;

        GameObject selectedModel = null;
        foreach (KeyValuePair<GameObject, int> um in userSelectModels)
        {
            if (um.Value.Equals(userId))
            {
                selectedModel = um.Key;
                break;
            }
        }

        if (selectedModel)
        {
            userSelectModels.Remove(selectedModel);
            Unhighlight(selectedModel);

            onDeSelectModel?.Invoke(selectedModel, userId);
        }

        selectModel = go;
        if (userId == GlobalInfo.account.id)
            localSelectModel = go;

        if (selectModel)
        {
            userSelectModels.Add(selectModel, userId);
            Highlight(selectModel, userId);

            onSelectModel?.Invoke(selectModel, userId);
        }
        else
        {          
            onSelectModel?.Invoke(null, userId);
        }
    }

    public void Highlight(GameObject go, int userId)
    {
        if (go != null)
        {
            ModelManager.Instance.ControlHighlightEffect(go.transform, true, GlobalInfo.IsLiveMode() ? NetworkManager.Instance.GetPlayerColor(userId) : highlightColor);
        }
    }

    public void Unhighlight(GameObject go)
    {
        if (go != null)
        {
            ModelManager.Instance.ControlHighlightEffect(go.transform, false);
        }
    }

    /// <summary>
    /// 清空选择
    /// </summary>
    public void ClearSelection()
    {
        foreach (KeyValuePair<GameObject, int> userSelect in userSelectModels)
        {
            GameObject go = userSelect.Key;
            if (go)
            {
                Unhighlight(go);
                onDeSelectModel?.Invoke(userSelect.Key, userSelect.Value);
            }
        }
        userSelectModels.Clear();
        onClearSelection?.Invoke();
    }

    /// <summary>
    /// 获取用户选择物体
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public GameObject GetUserSelectedGo(int userId)
    {
        foreach (KeyValuePair<GameObject, int> um in userSelectModels)
        {
            if (um.Value == userId)
                return um.Key;
        }
        return null;
    }

    private void Deselect()
    {
        //减少冗余操作发送
        if (localSelectModel != null)
        {
            SetSelect(null);
        }
    }

    /// <summary>
    /// 设置是否可选中
    /// </summary>
    /// <param name="tf"></param>
    /// <param name="state"></param>
    public void SetSelectState(Transform tf, bool state)
    {
        //本身的box
        CollisionBoxMouseEvent box = tf.GetComponent<CollisionBoxMouseEvent>();
        if (box != null)
            box.SetState(state);
    }

    /// <summary>
    /// 关闭所有的box
    /// </summary>
    public void CloseAllCollider()
    {
        CollisionBoxMouseEvent[] collisionBoxMouseEvents = GetComponentsInChildren<CollisionBoxMouseEvent>(true);
        for (int i = 0; i < collisionBoxMouseEvents.Length; i++)
        {
            collisionBoxMouseEvents[i].SetState(false);
        }
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)ModelOperateEvent.Click:
                MsgIntString userSelectedName = ((MsgBrodcastOperate)msg).GetData<MsgIntString>();
                int userId = userSelectedName.arg1;
                if (GlobalInfo.IsLiveMode() && !GlobalInfo.IsUserOperator(userId))
                    return;
                if (NetworkManager.Instance.IsIMSyncCachedState && userId == GlobalInfo.account.id)
                    return;
                GameObject go = ModelManager.Instance.GetModelByUUID(userSelectedName.arg2);
                SelectModel(go, userId);
                break;
            case (ushort)RoomChannelEvent.OtherLeave:
                int leavedUser = ((MsgIntString)msg).arg1;
                if(leavedUser != GlobalInfo.roomInfo.creatorId)
                    SelectModel(null, leavedUser);
                break;
            case (ushort)RoomChannelEvent.UpdateControl:
                MsgIntBool msgIntBool = (MsgIntBool)msg;
                if (msgIntBool.arg1 != GlobalInfo.account.id && !msgIntBool.arg2)
                {
                    SelectModel(null, msgIntBool.arg1);
                }
                break;
            //case (ushort)RoomChannelEvent.UpdateMemberList:
            //    foreach (KeyValuePair<GameObject, int> userModel in userSelectModels)
            //    {
            //        HighlightPlus.HighlightEffect highlightEffect = userModel.Key.GetComponent<HighlightPlus.HighlightEffect>();
            //        if (highlightEffect)
            //            highlightEffect.outlineColor = NetworkManager.Instance.GetPlayerColor(userModel.Value);
            //    }
            //    break;
            case (ushort)StateEvent.PreSyncVersion:
                ClearSelection();
                break;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        TapRecognizer.Instance?.UnRegistOnLeftMouseEmptyClick(Deselect);
    }
}