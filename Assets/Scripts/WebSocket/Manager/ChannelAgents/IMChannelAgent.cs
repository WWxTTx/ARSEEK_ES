using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Interop;
using UnityEngine;
using UnityFramework.Runtime;

/// <summary>
/// rti同步通道代理类
/// </summary>
public class IMChannelAgent : NetworkChannelAgentBase
{
    /// <summary>
    /// 获取待发送的操作消息数量
    /// </summary>
    public int SendOpCount => opsQueue.Count;

    /// <summary>
    /// 获取已接收未处理的操作消息数量
    /// </summary>
    public int ReceivedOpCount => opsReceive.Count;

    /// <summary>
    /// 是否开始同步
    /// </summary>
    public bool IsStartSync
    {
        get { return _isStartSync; }
        set { Log.Debug("RTI是否开始同步:" + value); _isStartSync = value; }
    }

    /// <summary>
    /// 是否正在进行缓存状态同步
    /// </summary>
    public bool IsSyncCachedState
    {
        get { return _isSyncCachedState; }
        set { Log.Debug("RTI是否开始缓存状态同步:" + value); _isSyncCachedState = value; }
    }

    /// <summary>
    /// 是否正在进行状态同步
    /// </summary>
    public bool IsSyncState
    {
        get { return _isSyncState; }
        set { Log.Debug("RTI是否开始状态同步:" + value); _isSyncState = value; }
    }

    /// <summary>
    /// 是否正在进行百科状态同步
    /// </summary>
    public bool IsSyncBaikeState
    {
        get { return _isSyncBaikeState; }
        set { Log.Debug("RTI是否开始同步百科状态:" + value); _isSyncBaikeState = value; }
    }

    /// <summary>
    /// 是否正在等待操作响应
    /// </summary>
    public bool IsWaitingResponse
    {
        get { return _isWaitingResponse; }
        set { Log.Debug("RTI是否等待操作响应:" + value); _isWaitingResponse = value; }
    }

    /// <summary>
    /// 等待发送的操作列表
    /// </summary>
    private Queue<MsgBrodcastOperate> opsQueue = new Queue<MsgBrodcastOperate>();

    /// <summary>
    /// 缓存操作版本
    /// 直播房间无权限用户缓存但不执行，用于获取操作权时同步状态
    /// </summary>
    private IMPacket cachedPacket;

    /// <summary>
    /// 收到的操作消息队列
    /// </summary>
    private Queue<MsgBrodcastOperate> opsReceive = new Queue<MsgBrodcastOperate>();

    /// <summary>
    /// 当前广播执行的操作
    /// </summary>
    private MsgBrodcastOperate currentOp;

    private readonly System.Object stateLock = new System.Object();
    /// <summary>
    /// 当前待同步状态
    /// </summary>
    public IMState CurrentStateToSync
    {
        get { lock(stateLock) return currentState; }
        set { lock(stateLock) currentState = value; }
    }

    /// <summary>
    /// 状态同步工具类
    /// </summary>
    private IMStateHelper stateHelper;

    private Coroutine sendCoroutine = null;
    private WaitUntil sendCondition;
    private byte[] versionByte;
    private byte[] dataByte;
    private byte[] sendByte;
    private byte[] recvDataByte;

    private bool _isStartSync = false;
    private bool _isSyncCachedState = false;
    private bool _isSyncState = false;
    private bool _isSyncBaikeState = false;
    private bool _isWaitingResponse = false;
    /// <summary>
    /// 当前接收的同步消息包中的状态
    /// </summary>
    private IMState currentState;

    private object asynLock = new object();
    private float deltaTime;

    public override void InitNetworkChannel()
    {
        networkChannel = new NetworkChannel(ChannelType.rti);
        stateHelper = new IMStateHelper();

        AddMsg(new ushort[]
        {
            (ushort)NetworkChannelEvent.Open,
            (ushort)NetworkChannelEvent.Closed,
            (ushort)NetworkChannelEvent.Error
        });

        sendCondition = new WaitUntil(() => opsQueue.Count > 0 && !IsWaitingResponse);
    }

    protected override void OnChannelOpen()
    {
        base.OnChannelOpen();
        sendCoroutine = StartCoroutine(SendCoroutine());
    }

    protected override void OnChannelClosed()
    {
        base.OnChannelClosed();
        StopSendCoroutine();
        Clear();
    }

    protected override void OnChannelError()
    {
        base.OnChannelError();
        StopSendCoroutine();
        Clear();
    }

    /// <summary>
    /// 发送同步消息
    /// </summary>
    /// <param name="msg"></param>
    public void SendOperationData(MsgBrodcastOperate msg)
    {
        if (IsSyncState)
            return;

        lock (asynLock)
        {
            opsQueue.Enqueue(msg);
        }
    }

    /// <summary>
    /// 处理操作消息
    /// </summary>
    private void LateUpdate()
    {
        if (stateHelper == null || GlobalInfo.roomInfo == null)
            return;

        deltaTime += Time.deltaTime;
        //先执行缓存状态消息
        while (IsStartSync && !IsSyncBaikeState && stateHelper.ReceivedCachedStateOpCount > 0 && deltaTime > 0.01f && ModelManager.Instance.CameraControl && !GlobalInfo.waitExam)
        {
            deltaTime = 0;

            if (GlobalInfo.playTimeRatio > 0)
            {
                GlobalInfo.uiAnimRatio = 0f;
                GlobalInfo.playTimeRatio = 0f;
                IsSyncCachedState = true;
                UIManager.Instance.OpenUI<LoadingPanel>(UILevel.Loading);
            }

            currentOp = stateHelper.DequeueCachedStateOp();
            TryExecuteCurrentOp();///
        }
        //完成缓存状态同步
        if (IsStartSync && IsSyncCachedState && stateHelper.ReceivedCachedStateOpCount == 0 && GlobalInfo.playTimeRatio < 1f)
        {
            UIManager.Instance.CloseUI<LoadingPanel>();
            IsSyncCachedState = false;
            GlobalInfo.uiAnimRatio = 1f;
            GlobalInfo.playTimeRatio = 1f;
        }

        //执行状态消息
        //等待百科状态同步完成再执行后续操作 //&& !GlobalInfo.isARTracking 
        while (IsStartSync && !IsSyncCachedState && !IsSyncBaikeState && stateHelper.ReceivedStateOpCount > 0 && deltaTime > 0.01f && ModelManager.Instance.CameraControl && !GlobalInfo.waitExam)
        {
            deltaTime = 0;

            if (GlobalInfo.playTimeRatio > 0)
            {
                GlobalInfo.uiAnimRatio = 0f;
                GlobalInfo.playTimeRatio = 0f;
                IsSyncState = true;
                UIManager.Instance.OpenUI<LoadingPanel>(UILevel.Loading);
            }

            currentOp = stateHelper.DequeueStateOp();
            TryExecuteCurrentOp();
        }
        //完成状态同步
        if (IsStartSync && IsSyncState && stateHelper.ReceivedStateOpCount == 0 && GlobalInfo.playTimeRatio < 1f)
        {
            UIManager.Instance.CloseUI<LoadingPanel>();
            IsSyncState = false;
            GlobalInfo.uiAnimRatio = 1f;
            GlobalInfo.playTimeRatio = 1f;
        }

        //执行操作消息 //&& !GlobalInfo.isARTracking
        while (IsStartSync && !IsSyncState && !IsSyncCachedState && ReceivedOpCount > 0 && deltaTime > 0.01f && ModelManager.Instance.CameraControl && !GlobalInfo.waitExam)
        {
            deltaTime = 0;
            currentOp = opsReceive.Dequeue();
            TryExecuteCurrentOp();
        }
    }

    private static string cachedStateLog = "<color=#DCA800>执行缓存状态消息:</color>";
    private static string stateLog = "<color=#C02C24>执行状态消息:</color>";
    private static string opLog = "<color=#14A857>执行消息:</color>";

    /// <summary>
    /// 执行操作
    /// </summary>
    private void TryExecuteCurrentOp()
    {
        if (currentOp == null)
            return;

        Log.Debug($"{(IsSyncCachedState ? cachedStateLog : IsSyncState ? stateLog : opLog)} {JsonTool.Serializable(currentOp)}");

        if (string.IsNullOrEmpty(currentOp.data))
            return;

        if (currentOp.msgId == (ushort)CoursePanelEvent.SwitchResource
            || currentOp.msgId == (ushort)BaikeSelectModuleEvent.BaikeSelect
            || currentOp.msgId == (ushort)ExamPanelEvent.Start)
        {
            IsStartSync = false;
        }
        try
        {
            FormMsgManager.Instance.SendMsg(currentOp);
        }
        catch (Exception e)
        {
            if (networkManager.IsLeavingRoom)
            {
                IsStartSync = true;
                return;
            }

            Log.Error($"执行消息错误: {e.Message}");
            IsStartSync = false;
            Invoke("Delayed", 2);
            return;
        }
    }

    /// <summary>
    /// 重新广播
    /// </summary>
    private void Delayed()
    {
        Log.Debug("重新广播消息:" + JsonTool.Serializable(currentOp));

        try
        {
            FormMsgManager.Instance.SendMsg(currentOp);
            IsStartSync = true;
        }
        catch (Exception e)
        {
            Log.Error("重新广播消息失败，错误为:" + e);

            if (networkManager.IsLeavingRoom)
            {
                IsStartSync = true;
                return;
            }
        }
    }

    public override void ProcessMessage(string message)
    {
        if (!enabled) return;

        if (string.IsNullOrEmpty(message))
            return;

        JObject jObject = JObject.Parse(message);
        if (jObject == null)
            return;

        string type = jObject[NetworkManager.TYPE].ToString();
        
        switch (type)
        {
            case NetworkManager.RTI_ACTION:
                int version = int.Parse(jObject[NetworkManager.PAYLOAD][NetworkManager.VERSION].ToString());
                string datamsg = jObject[NetworkManager.PAYLOAD][NetworkManager.COMMAND].ToString();
                if (string.IsNullOrEmpty(datamsg))
                {
                    IsWaitingResponse = false;
                    return;
                }

                try
                {
                    DebugHelper.Info(ChannelType.rti, $"[recv] {version}{datamsg}");

                    IMPacket packet = JsonTool.DeSerializable<IMPacket>(datamsg);
                    if (packet != null)
                    {
                        cachedPacket = packet;

                        if (GlobalInfo.IsOperator())
                        {
                            //中途加入或本地为旧版本
                            if (GlobalInfo.version < version - 1)
                            {
                                SyncVersion(packet);
                            }
                            else
                            {
                                // 修复：新消息到来时重新启用同步，确保消息能被执行
                                if (!IsStartSync)
                                {
                                    IsStartSync = true;
                                }
                                opsReceive.Enqueue(packet.data);
                                stateHelper.UpdateState(packet.data);
                            }
                            //更新本地版本
                            GlobalInfo.version = version;
                        }
                    }
                }
                catch (Exception e)
                {
                    GlobalInfo.version = version;
                    Log.Error("同步版本失败: " + e.Message + ", data: " + datamsg);
                }
                finally
                {
                    IsWaitingResponse = false;
                }
                break;
        }
    }

    /// <summary>
    /// 同步版本
    /// </summary>
    private void SyncVersion(IMPacket packet)
    {
        //开始进行版本同步前，清除一些状态
        SendMsg(new MsgBase((ushort)StateEvent.PreSyncVersion));

        GlobalInfo.isLive = true;
        GlobalInfo.UpdateCourseMode();
        //确保进入课程模块后再进行消息同步
        IsStartSync = UIManager.Instance.IsOpen<ExamPanel>() || UIManager.Instance.IsOpen<ExamCoursePanel>() || UIManager.Instance.IsOpen<OPLSynCoursePanel>();/*true;*/
        IsSyncBaikeState = false;
        opsReceive.Clear();

        CurrentStateToSync = packet.state;
        stateHelper.UpdateStateVersion(packet);

        deltaTime = 0;
    }

    /// <summary>
    /// 同步缓存版本
    /// </summary>
    public void SyncCachedVersion()
    {
        //DebugHelper.Info(ChannelType.rti, $"[cached] {cachedPacket.version}{JsonTool.Serializable(cachedPacket)}");

        //开始进行版本同步前，清除一些状态
        SendMsg(new MsgBase((ushort)StateEvent.PreSyncVersion));

        GlobalInfo.isLive = true;
        GlobalInfo.UpdateCourseMode();
        IsStartSync = true;
        IsSyncBaikeState = false;
        opsReceive.Clear();

        CurrentStateToSync = cachedPacket.state;
        stateHelper.UpdateCachedStateVersion(cachedPacket);
        Debug.Log($"<color=#14A857>重连者添加缓存状态:</color>)" + JsonTool.Serializable(cachedPacket));

        deltaTime = 0;

        GlobalInfo.version = cachedPacket.version;
    }

    /// <summary>
    /// 发送协程
    /// </summary>
    /// <returns></returns>
    private IEnumerator SendCoroutine()
    {
        IsStartSync = true;
        GlobalInfo.version = 0;

        while (true)
        {
            //等待接收到响应再发送下一条
            yield return sendCondition;

            lock (asynLock)
                Send(opsQueue.Dequeue());
        }
    }

    /// <summary>
    /// 将MsgBrodcastOperate封装成IMPacket进行发送
    /// </summary>
    /// <param name="msg"></param>
    private void Send(MsgBrodcastOperate msg)
    {
        if (!networkChannel.IsConnect)
            return;

        int version = GlobalInfo.version + 1;

        IMPacket packet = new IMPacket
        {
            data = msg,
            state = stateHelper.GetState(),
            version = version,
        };

        string data = JsonTool.Serializable(packet);

        JObject jObject = new JObject
        {
            [NetworkManager.TYPE] = NetworkManager.OPERATION,
            [NetworkManager.PAYLOAD] = new JObject
            {
                [NetworkManager.VERSION] = version,
                [NetworkManager.COMMAND] = data
            }
        };

        IsWaitingResponse = true;
        DebugHelper.Info(ChannelType.rti, $"[send] {jObject.ToString()}");
        networkChannel.SendAsync(jObject.ToString());
    }

    /// <summary>
    /// 停止发送
    /// </summary>
    private void StopSendCoroutine()
    {
        if (sendCoroutine != null)
        {
            StopCoroutine(sendCoroutine);
            sendCoroutine = null;
        }
    }

    /// <summary>
    /// 清除用户操作
    /// </summary>
    /// <param name="userId"></param>
    public void ClearUserOps(int userId)
    {
        if (userId == GlobalInfo.roomInfo.creatorId)
            return;
        stateHelper.TruncateStateList(userId);
    }

    /// <summary>
    /// 清除
    /// </summary>
    /// <param name="waitResponse">是否仍需等待rti返回、同步版本</param>
    public void Clear(bool waitResponse = false)
    {
        lock (asynLock)
            opsQueue.Clear();
        opsReceive.Clear();
        CurrentStateToSync = null;
        cachedPacket = null;
        stateHelper.Clear(true);
        IsStartSync = false;
        IsSyncCachedState = false;
        IsSyncState = false;
        if (!waitResponse)
            IsWaitingResponse = false;
        GlobalInfo.controllerIds.Clear();
    }
}