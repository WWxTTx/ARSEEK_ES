using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;

/// <summary>
/// 同步状态辅助工具类
/// </summary>
public partial class IMStateHelper
{
    /// <summary>
    /// 获取待处理的状态操作数量
    /// </summary>
    public int ReceivedStateOpCount { get { return stateReceive.Count; } }

    /// <summary>
    /// 获取待处理的缓存状态操作数量
    /// </summary>
    public int ReceivedCachedStateOpCount { get { return cachedStateReceive.Count; } }

    /// <summary>
    /// 待处理的缓存状态操作队列
    /// </summary>
    private Queue<MsgBrodcastOperate> cachedStateReceive = new Queue<MsgBrodcastOperate>();
    /// <summary>
    /// 待处理的状态操作队列
    /// </summary>
    private Queue<MsgBrodcastOperate> stateReceive = new Queue<MsgBrodcastOperate>();

    /// <summary>
    /// 待发送的状态操作列表
    /// </summary>
    private List<MsgBrodcastOperate> stateSend = new List<MsgBrodcastOperate>();
    #region 本地记录的操作状态，用于构成stateSend，房间内成员保持一致
    /// <summary>
    /// 用户画笔操作集合
    /// </summary>
    private Dictionary<int, List<MsgBrodcastOperate>> userOpsLineSend = new Dictionary<int, List<MsgBrodcastOperate>>();
    /// <summary>
    /// 只保留最新状态的操作集合
    /// </summary>
    private Dictionary<ushort, MsgBrodcastOperate> filterState = new Dictionary<ushort, MsgBrodcastOperate>();
    /// <summary>
    /// 保留权限用户最新状态的操作集合
    /// </summary>
    private Dictionary<string, MsgBrodcastOperate> userFilterState = new Dictionary<string, MsgBrodcastOperate>();
    /// <summary>
    /// 通用的操作集合
    /// </summary>
    private List<MsgBrodcastOperate> opsSend = new List<MsgBrodcastOperate>();
    #endregion

    /// <summary>
    /// 获取全局状态，用于封装IMPacket
    /// </summary>
    /// <returns></returns>
    public IMState GetState()
    {
        return new IMState
        {
            stateOps = GetStateList(),
            baikeState = GetBaikeState()
        };
    }

    /// <summary>
    /// 获取全局状态操作消息列表
    /// </summary>
    /// <returns></returns>
    private List<MsgBrodcastOperate> GetStateList()
    {
        stateSend.Clear();

        foreach (MsgBrodcastOperate op in filterState.Values)
        {
            stateSend.Add(op);
        }

        foreach (MsgBrodcastOperate op in userFilterState.Values)
        {
            if (GlobalInfo.IsUserOperator(op.senderId))
                stateSend.Add(op);
        }

        stateSend.AddRange(opsSend);

        foreach (KeyValuePair<int, List<MsgBrodcastOperate>> opsLineSend in userOpsLineSend)
        {
            if (GlobalInfo.IsUserOperator(opsLineSend.Key))
                stateSend.AddRange(opsLineSend.Value);
        }
        return stateSend;
    }

    /// <summary>
    /// 获取当前百科状态
    /// </summary>
    /// <returns></returns>
    private BaikeState GetBaikeState()
    {
        BaikeState baikeState = new BaikeState
        {
            baikeType = (int)GlobalInfo.currentBaikeType
        };

        switch (GlobalInfo.currentBaikeType)
        {
            case BaikeType.Dismantling:
                DismantlingBaikeState dismantlingBaikeState = new DismantlingBaikeState();

                DismantlingController dismantlingController = ModelManager.Instance.modelRoot.GetComponentInChildren<DismantlingController>(true);
                if (dismantlingController)
                {
                    dismantlingBaikeState.foldCtrl = dismantlingController.latestFoldableModel?.name;
                }

                SelectionModel selectionModel = ModelManager.Instance.modelRoot.GetComponentInChildren<SelectionModel>(true);
                if (selectionModel)
                {
                    dismantlingBaikeState.selectModels = new Dictionary<string, int>();
                    foreach (KeyValuePair<GameObject, int> um in selectionModel.userSelectModels)
                    {
                        dismantlingBaikeState.selectModels.Add(um.Key.name, um.Value);
                    }
                }

                baikeState.data = JsonTool.Serializable(dismantlingBaikeState);
                break;
            case BaikeType.SmallScene:
                SmallSceneBaikeState smallSceneBaikeState = new SmallSceneBaikeState();

                SmallFlowCtrl smallFlowCtrl = ModelManager.Instance.modelRoot.GetComponentInChildren<SmallFlowCtrl>(true);
                if(smallFlowCtrl != null)
                {
                    smallSceneBaikeState.flowIndex = smallFlowCtrl.index_NowFlow;
                    smallSceneBaikeState.stepIndex = smallFlowCtrl.index_NowStep;
                    smallSceneBaikeState.modelStates = smallFlowCtrl.GetModelStates();
                    smallSceneBaikeState.successOpDatas = smallFlowCtrl.successOPs.Select(o => new SuccessOpData()
                    {
                        id = o.operation.GetComponent<ModelInfo>().ID,
                        optionName = o.optionName,
                        propId = o.prop?.ID
                    }).ToList();
                }

                UISmallSceneModule smallSceneModule = UIManager.Instance.canvas.GetComponentInChildren<UISmallSceneModule>();
                if (smallSceneModule != null)
                    smallSceneBaikeState.simSystemState = smallSceneModule.simuSystem?.GetSystemState();

                UISmallSceneOperationHistory historyModule = UIManager.Instance.canvas.GetComponentInChildren<UISmallSceneOperationHistory>();
                if(historyModule != null)
                    smallSceneBaikeState.operations = historyModule.OpRecordList;

                baikeState.data = JsonTool.Serializable(smallSceneBaikeState);
                break;
            default:
                break;
        }
        return baikeState;
    }


    /// <summary>
    /// 同步操作版本,将操作列表添加到待处理队列并更新本地操作状态
    /// </summary>
    /// <param name="msg"></param>
    public void UpdateStateVersion(IMPacket packet)
    {
        Clear();

        List<MsgBrodcastOperate> states = packet.state.stateOps;
        for (int i = 0; i < states.Count; i++)
        {
            EnqueueStateOp(states[i]);
        }

        EnqueueStateOp(packet.data);
    }

    /// <summary>
    /// 添加状态消息并更新列表
    /// </summary>
    /// <param name="msg"></param>
    private void EnqueueStateOp(MsgBrodcastOperate msg)
    {
        if (msg == null)
            return;

        stateReceive.Enqueue(msg);
        UpdateState(msg);
    }

    /// <summary>
    /// 获取待同步的状态消息
    /// </summary>
    /// <param name="msg"></param>
    public MsgBrodcastOperate DequeueStateOp()
    {
        if (stateReceive.Count == 0)
            return null;
        return stateReceive.Dequeue();
    }

    /// <summary>
    /// 同步缓存操作版本,将操作列表添加到待处理队列并更新本地操作状态
    /// </summary>
    /// <param name="msg"></param>
    public void UpdateCachedStateVersion(IMPacket packet)
    {
        cachedStateReceive.Clear();

        List<MsgBrodcastOperate> states = packet.state.stateOps;
        for (int i = 0; i < states.Count; i++)
        {
            EnqueueCachedStateOp(states[i]);
        }

        EnqueueCachedStateOp(packet.data);
    }

    /// <summary>
    /// 添加缓存状态消息并更新列表
    /// </summary>
    /// <param name="msg"></param>
    private void EnqueueCachedStateOp(MsgBrodcastOperate msg)
    {
        if (msg == null)
            return;

        cachedStateReceive.Enqueue(msg);
        UpdateState(msg);
    }

    /// <summary>
    /// 获取待同步的缓存状态消息
    /// </summary>
    /// <param name="msg"></param>
    public MsgBrodcastOperate DequeueCachedStateOp()
    {
        if (cachedStateReceive.Count == 0)
            return null;
        return cachedStateReceive.Dequeue();
    }

    /// <summary>
    /// 更新全局状态操作消息列表
    /// </summary>
    /// <param name="msg"></param>
    public void UpdateState(MsgBrodcastOperate msg)
    {
        if (msg == null || string.IsNullOrEmpty(msg.data))
            return;

        ushort id = msg.msgId;

        // 添加调试日志 - 处理前
        Debug.Log($"[状态调试] UpdateState前 | msgId:{id} | filterState keys:[{string.Join(",", filterState.Keys)}]");

        //切换课程或切换百科 清除状态
        if (!GlobalInfo.IsExamMode() && MsgTruncate.IndexOf(id) > -1)
        {
            TruncateStateList(MsgTruncateIndependent);
        }
        //结束考核 清除状态
        if (GlobalInfo.IsExamMode() && MsgTruncateExam.IndexOf(id) > -1)
        {
            TruncateStateList(MsgTruncateIndependentExam);
        }
        if (GlobalInfo.IsExamMode() && MsgTruncateExamGroup.IndexOf(id) > -1)
        {
            TruncateStateList(MsgTruncateIndependentExamGroup);
        }

        MsgStateType msgType = GetMsgType(id);
        Debug.Log($"[状态调试] UpdateState | msgId:{id} | msgType:{msgType}");

        switch (msgType)
        {
            case MsgStateType.Default:
                opsSend.Add(msg);
                break;
            case MsgStateType.Update:
                UpdateStateList(msg);
                break;
            case MsgStateType.UpdateAppend:
                UpdateStateList(msg, true);
                break;
            case MsgStateType.UserUpdate:
                UpdateUserStateList(msg);
                break;
            case MsgStateType.UserUpdateAppend:
                UpdateUserStateList(msg, true);
                break;
            case MsgStateType.UserConflict:
                RemoveUserConflictStateList(msg);
                break;
            case MsgStateType.Paint:
                SavePaintOps(msg);
                break;
            case MsgStateType.Dismantling:
                SaveIntegrationOps(msg);
                break;
            case MsgStateType.NoneState:
            default:
                break;
        }

        // 添加调试日志 - 处理后
        Debug.Log($"[状态调试] UpdateState后 | msgId:{id} | filterState keys:[{string.Join(",", filterState.Keys)}]");
    }

    /// <summary>
    /// 获取操作对应的状态存储方式
    /// </summary>
    /// <param name="msgId"></param>
    /// <returns></returns>
    private MsgStateType GetMsgType(ushort msgId)
    {
        if (!GlobalInfo.IsExamMode() && MsgTypeMap.ContainsKey(msgId))
            return MsgTypeMap[msgId];
        if (GlobalInfo.IsExamMode() && MsgTypeMapExam.ContainsKey(msgId))
            return MsgTypeMapExam[msgId];
        return MsgStateType.NoneState;
    }

    /// <summary>
    /// 保留最新状态
    /// </summary>
    /// <param name="data">操作</param>
    private void UpdateStateList(MsgBrodcastOperate msg, bool append = false)
    {
        if (filterState.ContainsKey(msg.msgId))
        {
            if (append)
                OrderStateList(msg);
            else
                filterState[msg.msgId] = msg;
        }
        else
            filterState.Add(msg.msgId, msg);
    }

    /// <summary>
    /// 保留用户最新状态
    /// </summary>
    /// <param name="data">操作</param>
    private void UpdateUserStateList(MsgBrodcastOperate msg, bool append = false)
    {
        string key = $"{msg.senderId}:{msg.msgId}";

        if (userFilterState.ContainsKey(key))
        {
            if (append)
                OrderUserStateList(msg);
            else
                userFilterState[key] = msg;
        }
        else
        {
            userFilterState.Add(key, msg);
        }
    }

    /// <summary>
    /// 保留最新状态并且插入到末尾
    /// </summary>
    /// <param name="data">操作</param>
    private void OrderStateList(MsgBrodcastOperate msg)
    {
        List<MsgBrodcastOperate> saved = new List<MsgBrodcastOperate>();
        foreach (ushort id in filterState.Keys)
        {
            if (id != msg.msgId)
            {
                saved.Add(filterState[id]);
            }
        }
        filterState.Clear();
        foreach (MsgBrodcastOperate m in saved)
        {
            filterState.Add(m.msgId, m);
        }
        filterState.Add(msg.msgId, msg);
    }


    /// <summary>
    /// 保留用户最新状态并且插入到末尾
    /// </summary>
    /// <param name="data">操作</param>
    private void OrderUserStateList(MsgBrodcastOperate msg)
    {
        string key = $"{msg.senderId}:{msg.msgId}";

        List<MsgBrodcastOperate> saved = new List<MsgBrodcastOperate>();
        foreach (string id in userFilterState.Keys)
        {
            if (!id.Equals(key))
                saved.Add(userFilterState[id]);
        }

        userFilterState.Clear();
        foreach (MsgBrodcastOperate m in saved)
        {
            userFilterState.Add($"{m.senderId}:{m.msgId}", m);
        }

        userFilterState.Add(key, msg);
    }


    /// <summary>
    /// 移除用户互斥操作
    /// 例如：关闭模块，移除开启模块及模块内部所有操作
    /// </summary>
    /// <param name="data">操作</param>
    private void RemoveUserConflictStateList(MsgBrodcastOperate msg)
    {
        //if (MsgConflictMap.TryGetValue(msg.msgId, out List<ushort> msgConflicts))
        //{
        //    List<string> conflictKey = new List<string>();

        //    for (int i = 0; i < msgConflicts.Count; i++)
        //    {
        //        conflictKey.Add($"{msg.senderId}:{msgConflicts[i]}");
        //    }

        //    List<MsgBrodcastOperate> saved = new List<MsgBrodcastOperate>();

        //    foreach (string id in userFilterState.Keys)
        //    {
        //        if (!conflictKey.Contains(id))
        //            saved.Add(userFilterState[id]);
        //    }

        //    userFilterState.Clear();
        //    foreach (MsgBrodcastOperate m in saved) 
        //    {
        //        userFilterState.Add($"{m.senderId}:{m.msgId}", m);
        //    }
        //}
    }

    /// <summary>
    /// 保存画板操作
    /// </summary>
    /// <param name="msg"></param>
    private void SavePaintOps(MsgBrodcastOperate msg)
    {
        switch (msg.msgId)
        {
            case (ushort)PaintEvent.SyncPaint:
                //画板画线操作记录
                if (userOpsLineSend.ContainsKey(msg.senderId))
                    userOpsLineSend[msg.senderId].Add(msg);
                else
                    userOpsLineSend.Add(msg.senderId, new List<MsgBrodcastOperate>() { msg });
                break;
            case (ushort)PaintEvent.SyncUndo:
                if (userOpsLineSend.ContainsKey(msg.senderId))
                {
                    if (userOpsLineSend[msg.senderId].Count > 0)
                        userOpsLineSend[msg.senderId].RemoveAt(userOpsLineSend[msg.senderId].Count - 1);
                }
                break;
            case (ushort)PaintEvent.SyncReset:
                //清空画板时清除画线操作数据
                if (userOpsLineSend.ContainsKey(msg.senderId))
                {
                    userOpsLineSend[msg.senderId].Clear();
                    userOpsLineSend.Remove(msg.senderId);
                }
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 保存拆解特殊操作
    /// </summary>
    /// <param name="msg"></param>
    public void SaveIntegrationOps(MsgBrodcastOperate msg)
    {
        switch (msg.msgId)
        {
            case (ushort)IntegrationModuleEvent.JumpToSelect:
                MsgStringBool jumpSelectMsg = msg.GetData<MsgStringBool>();
                if (jumpSelectMsg.arg2)
                {
                    MsgBase msgString = new MsgBase((ushort)IntegrationModuleEvent.Check);
                    UpdateUserStateList(new MsgBrodcastOperate()
                    {
                        senderId = msg.senderId,
                        msgId = msgString.msgId,
                        data = JsonTool.Serializable(msgString)
                    }, true);
                }
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 切换百科时清空除MsgTruncateIndependent包含以外的状态
    /// </summary>
    /// <param name="independentMsg"></param>
    private void TruncateStateList(List<ushort> independentMsg)
    {
        Debug.Log($"[状态调试] TruncateStateList被调用 | 保留消息IDs:[{string.Join(",", independentMsg)}]");

        List<MsgBrodcastOperate> saved = new List<MsgBrodcastOperate>();
        foreach (ushort id in filterState.Keys)
        {
            //不受影响的状态
            if (independentMsg.IndexOf(id) > -1)
                saved.Add(filterState[id]);
        }
        filterState.Clear();
        foreach (MsgBrodcastOperate msg in saved)
        {
            filterState.Add(msg.msgId, msg);
        }

        saved.Clear();
        foreach (string key in userFilterState.Keys)
        {
            //不受影响的状态
            if (independentMsg.IndexOf(ushort.Parse(key.Split(':')[1])) > -1)
                saved.Add(userFilterState[key]);
        }
        userFilterState.Clear();
        foreach (MsgBrodcastOperate msg in saved)
        {
            userFilterState.Add($"{msg.senderId}:{msg.msgId}", msg);
        }

        opsSend.Clear();
        userOpsLineSend.Clear();

        Debug.Log($"[状态调试] TruncateStateList后 | filterState keys:[{string.Join(",", filterState.Keys)}]");
    }

    /// <summary>
    /// 清除指定类型的操作
    /// </summary>
    /// <param name="msgStateType"></param>
    public void TruncateStateList(MsgStateType msgStateType)
    {
        List<MsgBrodcastOperate> saved = new List<MsgBrodcastOperate>();
        foreach (ushort id in filterState.Keys)
        {
            MsgStateType msgType = GetMsgType(id);
            if (msgType != msgStateType)
                saved.Add(filterState[id]);
        }
        filterState.Clear();
        foreach (MsgBrodcastOperate msg in saved)
        {
            filterState.Add(msg.msgId, msg);
        }
    }

    /// <summary>
    /// 清除指定用户的操作
    /// </summary>
    /// <param name="userId"></param>
    public void TruncateStateList(int userId)
    {
        List<MsgBrodcastOperate> saved = new List<MsgBrodcastOperate>();
        foreach (string key in userFilterState.Keys)
        {
            if (int.Parse(key.Split(':')[0]) != userId)
                saved.Add(userFilterState[key]);
        }
        userFilterState.Clear();
        foreach (MsgBrodcastOperate msg in saved)
        {
            userFilterState.Add($"{msg.senderId}:{msg.msgId}", msg);
        }

        if (userOpsLineSend.ContainsKey(userId))
        {
            userOpsLineSend[userId].Clear();
            userOpsLineSend.Remove(userId);
        }
    }

    /// <summary>
    /// 清空状态
    /// </summary>
    public void Clear(bool clearCache = false)
    {
        stateReceive.Clear();
        filterState.Clear();
        foreach (List<MsgBrodcastOperate> opsLine in userOpsLineSend.Values)
            opsLine.Clear();
        userOpsLineSend.Clear();
        opsSend.Clear();
        stateSend.Clear();
        if (clearCache)
            cachedStateReceive.Clear();
    }
}