using Cysharp.Threading.Tasks;
using DG.Tweening;
using Newtonsoft.Json.Linq;
using RenderHeads.Media.AVProMovieCapture;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Interop;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.XR;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;
using static UnityFramework.Runtime.ServiceRequestData;

/// <summary>
/// 协同各通道相关接口
/// </summary>
public partial class NetworkManager : Singleton<NetworkManager>, INetworkManager, IRoomAgentHelper, IIMAgentHelper, IFrameAgentHelper, IAudioAgentHelper, IVideoAgentHelper
{
    #region 房间通道相关接口
    public void KickOutUser(int id)
    {
        if (mRoomChannelAgent == null || !mRoomChannelAgent.IsChannelConnected())
            return;
        Log.Debug("发送踢出成员消息" + id);
        //mRoomChannelAgent.SendCommand(id, outRoomCmd);
        JObject jObject = new JObject
        {
            [TYPE] = EVICT,
            [PAYLOAD] = new JObject()
            {
                ["memberId"] = id
            }
        };
        mRoomChannelAgent.SendCommand(jObject.ToString());
    }

    public void SilentAllMember(bool allowTalk)
    {
        if (mRoomChannelAgent == null || !mRoomChannelAgent.IsChannelConnected())
            return;
        Log.Debug("发送全员禁言控制消息" + allowTalk);
        JObject jObject = new JObject
        {
            [TYPE] = allowTalk ? SILENT_OFF : SILENT,
            [PAYLOAD] = new JObject() { }
        };

        mRoomChannelAgent.SendCommand(jObject.ToString());
    }

    public void SwitchUserTalk(int id)
    {
        if (mRoomChannelAgent == null || !mRoomChannelAgent.IsChannelConnected())
            return;
        Member member = mRoomChannelAgent.FindMemberById(id);
        if (member == null)
            return;
        Log.Debug("发送禁言控制消息" + id + "--" + !member.IsTalk);
        JObject jObject = new JObject
        {
            [TYPE] = !member.IsTalk ? SILENT_OFF : SILENT,
            [PAYLOAD] = new JObject()
            {
                ["memberId"] = id,
            }
        };
        mRoomChannelAgent.SendCommand(jObject.ToString());
    }

    public void SwitchUserChat(int id)
    {
        if (mRoomChannelAgent == null || !mRoomChannelAgent.IsChannelConnected())
            return;
        Member member = mRoomChannelAgent.FindMemberById(id);
        if (member == null)
            return;
        Log.Debug("发送个人聊天开关静音消息" + id + "--" + !member.IsChat);
        //mRoomChannelAgent.SendCommand(id, !member.IsChat ? enableChatCmd : disableChatCmd);
        JObject jObject = new JObject
        {
            [TYPE] = PROFILE,
            [PAYLOAD] = new JObject()
            {
                ["memberId"] = id,
                ["isTalk"] = member.IsTalk,
                ["isControl"] = member.IsControl,
                ["isMainScreen"] = member.IsMainScreen,
                ["isChat"] = !member.IsChat,
                ["colorNumber"] = member.ColorNumber
            }
        };
        mRoomChannelAgent.SendCommand(jObject.ToString());
    }

    public void SetUserControl(int id, bool give)
    {
        if (mRoomChannelAgent == null || !mRoomChannelAgent.IsChannelConnected())
            return;
        Member member = mRoomChannelAgent.FindMemberById(id);
        if (member == null)
            return;
        Log.Debug("发送设置操作权限消息" + id + "--" + give);
        if (give)
        {
            //mRoomChannelAgent.SendCommand(id, $"{{\"IsControl\":true,\"colorNumber\":\"{AllocPlayerColor(id).ColorToHexRGB()}\"}}");
            JObject jObject = new JObject
            {
                [TYPE] = PROFILE,
                [PAYLOAD] = new JObject()
                {
                    ["memberId"] = id,
                    ["isTalk"] = member.IsTalk,
                    ["isControl"] = true,
                    ["isMainScreen"] = member.IsMainScreen,
                    ["isChat"] = member.IsChat,
                    ["colorNumber"] = AllocPlayerColor(id).ColorToHexRGB()
                }
            };
            mRoomChannelAgent.SendCommand(jObject.ToString());
        }
        else
        {
            //mRoomChannelAgent.SendCommand(id, give ? giveControlCmd : takeControlCmd);

            JObject jObject = new JObject
            {
                [TYPE] = PROFILE,
                [PAYLOAD] = new JObject()
                {
                    ["memberId"] = id,
                    ["isTalk"] = member.IsTalk,
                    ["isControl"] = false,
                    ["isMainScreen"] = false,
                    ["isChat"] = member.IsChat,
                    ["colorNumber"] = string.Empty
                }
            };
            mRoomChannelAgent.SendCommand(jObject.ToString());
        }
    }

    public void SetUserMainView(int id, bool give)
    {
        if (mRoomChannelAgent == null || !mRoomChannelAgent.IsChannelConnected())
            return;
        Member member = mRoomChannelAgent.FindMemberById(id);
        if (member == null)
            return;

        Log.Debug("发送设置主画面消息" + id + "--" + give);
        if (give)
        {
            //mRoomChannelAgent.SendCommand(id, $"{{\"IsControl\":true,\"IsMainScreen\":true,\"colorNumber\":\"{AllocPlayerColor(id).ColorToHexRGB()}\"}}");
            JObject jObject = new JObject
            {
                [TYPE] = PROFILE,
                [PAYLOAD] = new JObject()
                {
                    ["memberId"] = id,
                    ["isTalk"] = member.IsTalk,
                    ["isControl"] = true,
                    ["isMainScreen"] = true,
                    ["isChat"] = member.IsChat,
                    ["colorNumber"] = AllocPlayerColor(id).ColorToHexRGB()
                }
            };
            mRoomChannelAgent.SendCommand(jObject.ToString());
        }
        else
        {
            //mRoomChannelAgent.SendCommand(id, give ? giveMainCmd : takeMainCmd);

            JObject jObject = new JObject
            {
                [TYPE] = PROFILE,
                [PAYLOAD] = new JObject()
                {
                    ["memberId"] = id,
                    ["isTalk"] = member.IsTalk,
                    ["isControl"] = false,
                    ["isMainScreen"] = false,
                    ["isChat"] = member.IsChat,
                    ["colorNumber"] = string.Empty
                }
            };
            mRoomChannelAgent.SendCommand(jObject.ToString());
        }
    }

    public void SetUserColor(int id)
    {
        if (mRoomChannelAgent == null || !mRoomChannelAgent.IsChannelConnected())
            return;
        Member member = mRoomChannelAgent.FindMemberById(id);
        if (member == null)
            return;

        Log.Debug("发送设置颜色消息" + id);
        //mRoomChannelAgent.SendCommand(id, $"{{\"colorNumber\":\"{AllocPlayerColor(id).ColorToHexRGB()}\"}}");

        JObject jObject = new JObject
        {
            [TYPE] = PROFILE,
            [PAYLOAD] = new JObject()
            {
                ["memberId"] = id,
                ["isTalk"] = member.IsTalk,
                ["isControl"] = member.IsControl,
                ["isMainScreen"] = member.IsMainScreen,
                ["isChat"] = member.IsChat,
                ["colorNumber"] = AllocPlayerColor(id).ColorToHexRGB()
            }
        };
        mRoomChannelAgent.SendCommand(jObject.ToString());
    }

    public bool IsUserOnline(int id)
    {
        return mRoomChannelAgent.onlineUsers.ContainsKey(id);
    }

    public bool IsUserChat(int id)
    {
        if (GlobalInfo.roomInfo == null || !IsUserOnline(id))
            return false;
        return (id == GlobalInfo.roomInfo.creatorId || mRoomChannelAgent.onlineUsers[id].IsTalk) && mRoomChannelAgent.onlineUsers[id].IsChat;
    }


    public string GetUserName(int id)
    {
        //if(id == GlobalInfo.account.id)
        //    return GlobalInfo.account.nickname;
        if (!IsUserOnline(id))
            return string.Empty;
        return mRoomChannelAgent.onlineUsers[id].Nickname;
    }

    public string GetUserDevice(int id)
    {
        if (!IsUserOnline(id))
            return string.Empty;

        return mRoomChannelAgent.onlineUsers[id].ClientType;
    }

    public bool IsFirstActiveUser()
    {
        if (mRoomChannelAgent.roomMembers == null || mRoomChannelAgent.roomMembers.Count == 0)
            return true;
        return GlobalInfo.account.id == mRoomChannelAgent.roomMembers[0].Id;
    }

    public List<Member> GetRoomMemberList()
    {
        return mRoomChannelAgent.roomMembers;
    }

    public int GetRoomMemberCount()
    {
        return mRoomChannelAgent.roomMembers.Count;
    }

    public List<Member> GetRandomMemberList(int count)
    {
        if (mRoomChannelAgent.roomMembers.Count < count)
            return mRoomChannelAgent.roomMembers;

        List<Member> randomMembers = new List<Member>
        {
            mRoomChannelAgent.roomMembers.Find(member => member.Id == GlobalInfo.roomInfo.creatorId)
        };
        randomMembers.AddRange(mRoomChannelAgent.roomMembers.Select(member => member).Where(m => m.Id != GlobalInfo.roomInfo.creatorId).ToList().Shuffle().Take(count - 1));
        return randomMembers;
    }
    #endregion

    #region 同步通道相关接口
    /// <summary>
    /// 是否同步操作的总开关 打开才会同步
    /// </summary>
    public bool IsIMSync
    {
        get { return mIMChannelAgent.IsStartSync; }
        set
        {
            if (mIMChannelAgent.IsStartSync && !value)
            {
                Debug.LogWarning($"执行同步关闭，调用来源: {UnityEngine.StackTraceUtility.ExtractStackTrace()}");
            }
            mIMChannelAgent.IsStartSync = value;
        }
    }

    /// <summary>
    /// 标志位，当前触发断线重连
    /// </summary>
    public bool IsIMSyncState = false;

    /// <summary>
    /// 是否正在同步百科状态
    /// </summary>
    public bool IsIMSyncBaikeState
    {
        get { return mIMChannelAgent.IsSyncBaikeState; }
        set
        {
            mIMChannelAgent.IsSyncBaikeState = value;
        }
    }

    /// <summary>
    /// 待发送的操作消息数量
    /// </summary>
    public int SendOpCount
    {
        get { return mIMChannelAgent.SendOpCount; }
    }

    public void SendIMMsg(MsgBrodcastOperate msg)
    {
        if (mIMChannelAgent == null || !mIMChannelAgent.IsChannelConnected())
        {
            Debug.LogWarning($"连接已断开 发送失败 {JsonTool.Serializable(msg)}");
            return;
        }
        mIMChannelAgent.SendOperationData(msg);
    }

    Action ExamCoursePanelAction;
    public void SyncBaikeState(Action callback = null)
    {
        ExamCoursePanelAction = callback;
        SyncBaikeStateAndCatchAsync();
    }

    async void SyncBaikeStateAndCatchAsync()
    {
        try
        {
            await _syncBaikeState();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    int step = 0;
    int flow = 0;
    private async UniTask _syncBaikeState()
    {
        IMState currentState = mIMChannelAgent.currentState;
        GameObject model = ModelManager.Instance.modelGo;
        // 重连只能发生在有重连信息,已重建场景后
        if (currentState == null || currentState.baikeState == null || currentState.baikeState.data == null || model == null)
        {
            IsIMSyncBaikeState = false;
            return;
        }
        IsIMSyncBaikeState = true;

        UIManager.Instance.OpenUI<LoadingPanel>();

        //消息中为空的可能会覆盖正确的
        SmallSceneBaikeState smallSceneBaikeState = JsonTool.DeSerializable<SmallSceneBaikeState>(currentState.baikeState.data);
        flow = smallSceneBaikeState.flowIndex;
        step = smallSceneBaikeState.stepIndex;

        Debug.Log("当前获得的任务进度为：" + flow + "   " + step);
        if (smallSceneBaikeState != null  && IsIMSyncState && (step > 0 || flow > 0))
        {
            // 等待 UISmallSceneFlowModule 初始化完成
            await WaitForFlowModule();

            // 发送任务进度跳转消息，通过flow和step索引定位步骤
            UISmallSceneModule smallSceneModule = UIManager.Instance.canvas.GetComponentInChildren<UISmallSceneModule>(true);
            if (smallSceneModule != null && smallSceneModule.smallFlowCtrl.flows != null)
            {
                ToolManager.SendBroadcastMsg(new MsgStringTuple<int, int, string>()
                {
                    msgId = (ushort)SmallFlowModuleEvent.SelectStep,
                    arg1 = smallSceneModule.smallFlowCtrl.flows[flow].steps[step].ID, // 步骤UUID
                    arg2 = new System.Tuple<int, int, string>(flow, step, string.Empty) // flow索引, step索引, 预留
                });
            }

            await UniTask.Delay(500, ignoreTimeScale: true);
            // 用服务端操作记录覆盖本地，不上传
            if (smallSceneModule.operationHistoryModule != null)
                smallSceneModule.operationHistoryModule.UpdateOpRecordList(smallSceneBaikeState.operations);

            //多人考核需要在联机步骤恢复的基础上增加操作记录对应操作还原
            if (GlobalInfo.courseMode == CourseMode.OnlineExam)
            {
                ExamCoursePanelAction?.Invoke();
            }

            // 等待步骤切换操作全部执行完成（最多等1秒）
            float waitElapsed = 0f;
            while (smallSceneModule?.smallFlowCtrl?.IsExecuting == true && waitElapsed < 1f)
            {
                await UniTask.Delay(100);
                waitElapsed += 0.1f;
            }

            // 恢复他人操作权限状态：如果最后一条操作是其他人的Operate，需要阻止刚重连的人操作
            RestoreOtherUserOperationState();
        }

        await UniTask.WaitForFixedUpdate();
        //恢复消息执行
        mIMChannelAgent.IsStartSync = true;
        IsIMSync = true;
        IsIMSyncBaikeState = false;
        //完成百科状态同步后，清空待同步状态，避免切换百科后重复同步
        mIMChannelAgent.currentState = null;
        //清空 stateOps 重放队列 — baikeState 已包含完整最终状态，无需重放
        mIMChannelAgent.Clear();
        GlobalInfo.controllerIds = new HashSet<int>(GlobalInfo.controllerIds);

        await UniTask.WaitForFixedUpdate();
        UIManager.Instance.CloseUI<LoadingPanel>();
        IsIMSyncState = false;
    }

    /// <summary>
    /// 等待 UISmallSceneFlowModule 初始化完成（不选中步骤）
    /// 用于重连恢复：只等待初始化完成，避免 InitTreeView 的 SelectFlow(0) 覆盖步骤
    /// </summary>
    /// <returns></returns>
    private async UniTask WaitForFlowModule()
    {
        float timeout = 3f;
        float elapsed = 0f;

        UISmallSceneFlowModule flowModule = null;

        while (elapsed < timeout)
        {
            flowModule = UIManager.Instance.canvas.GetComponentInChildren<UISmallSceneFlowModule>(true);
            if (flowModule != null && flowModule.viewItemIds != null && flowModule.viewItemIds.Count > 0)
                break;
            await UniTask.Delay(100);
            elapsed += 0.1f;
        }

        if (flowModule == null || flowModule.viewItemIds == null || flowModule.viewItemIds.Count == 0)
        {
            Log.Warning("UISmallSceneFlowModule 未初始化，跳过步骤同步");
        }
    }

    /// <summary>
    /// 清除离线用户操作
    /// </summary>
    /// <param name="userId"></param>
    public void ClearUserIMState(int userId)
    {
        mIMChannelAgent.ClearUserOps(userId);
    }
    #endregion

    /// <summary>
    /// 恢复操作权限状态：防止重连后userOpModel为空，导致刚重连的人可以在别人操作时进行操作
    /// 检查currentOp，如果是他人的Operate或UI同步消息，则恢复对应的操作权限记录
    /// </summary>
    private void RestoreOtherUserOperationState()
    {
        var currentOp = mIMChannelAgent.currentOp;
        if (currentOp == null)
            return;


        // 获取UISmallSceneModule
        UISmallSceneModule uiModule = UIManager.Instance.canvas.GetComponentInChildren<UISmallSceneModule>(true);
        if (uiModule == null)
        {
            Log.Warning("RestoreOtherUserOperationState: UISmallSceneModule not found");
            return;
        }

        ushort msgId = currentOp.msgId;
        if (msgId == (ushort)SmallFlowModuleEvent.Operate ||
                 msgId == (ushort)SmallFlowModuleEvent.SynchronizationTsq ||
                 msgId == (ushort)SmallFlowModuleEvent.SynchronizationLcu ||
                 msgId == (ushort)SmallFlowModuleEvent.SynchronizationZlqzz)
        {
            int senderId = currentOp.senderId;
            //是自己的操作 则再次操作
            if (mIMChannelAgent.currentOp.senderId == GlobalInfo.account.id)
            {
                // Operate消息直接发送
                if (msgId == (ushort)SmallFlowModuleEvent.Operate)
                {
                    FormMsgManager.Instance.SendMsg(mIMChannelAgent.currentOp);
                }
                else
                {
                    // 在UI同步消息发送前，先构造Operate消息发送
                    if (ModelManager.Instance.modelGo != null)
                    {
                        SmallFlowCtrl smallFlowCtrl = ModelManager.Instance.modelGo.GetComponent<SmallFlowCtrl>();
                        if (smallFlowCtrl != null)
                        {
                            SmallOp1 modelOp = smallFlowCtrl.GetStepOperationBehaviors();
                            // 构造Operate消息发送
                            MsgOperation msgOp = new MsgOperation((ushort)SmallFlowModuleEvent.Operate, modelOp.operation.ID, modelOp.optionName, modelOp.prop?.ID, true);
                            FormMsgManager.Instance.SendMsg(new MsgBrodcastOperate((ushort)SmallFlowModuleEvent.Operate, JsonTool.Serializable(msgOp)));
                        }
                    }
                    DOVirtual.DelayedCall(0.3f, () =>
                    {
                        FormMsgManager.Instance.SendMsg(mIMChannelAgent.currentOp);
                    });
                }
            }
            else
                RestoreOperationPermission(senderId, uiModule);
        }
    }

    /// <summary>
    /// 恢复 Operate 消息的操作权限
    /// </summary>
    private void RestoreOperatePermission(MsgBrodcastOperate currentOp, int senderId, UISmallSceneModule uiModule)
    {
        // 获取操作消息数据
        MsgOperation msgOp = currentOp.GetData<MsgOperation>();
        if (msgOp == null)
            return;

        // 获取模型操作对象
        ModelOperation modelOp = null;
        if (!string.IsNullOrEmpty(msgOp.modelOperation))
        {
            if (ModelManager.Instance.modelGo != null)
            {
                SmallFlowCtrl smallFlowCtrl = ModelManager.Instance.modelGo.GetComponent<SmallFlowCtrl>();
                if (smallFlowCtrl != null)
                {
                    modelOp = smallFlowCtrl.GetModelOperation(msgOp.modelOperation);
                }
            }
        }

        if (modelOp != null)
        {
            // 恢复操作权限：将他人操作记录到userOpModel
            uiModule.AcquireOperatePermission(senderId, modelOp);
            Log.Debug($"RestoreOtherUserOperationState: 恢复用户 {senderId} 对 {msgOp.modelOperation} 的操作权限记录");
        }
        else
        {
            Log.Warning($"RestoreOtherUserOperationState: 无法找到模型操作对象 {msgOp.modelOperation}，senderId={senderId}");
        }
    }

    /// <summary>
    /// 恢复 UI 自定义脚本操作权限
    /// </summary>
    private void RestoreOperationPermission(int senderId, UISmallSceneModule uiModule)
    {
        // 获取模型操作对象
        ModelOperation modelOp = null;
        if (ModelManager.Instance.modelGo != null)
        {
            SmallFlowCtrl smallFlowCtrl = ModelManager.Instance.modelGo.GetComponent<SmallFlowCtrl>();
            if (smallFlowCtrl != null)
            {
                modelOp = smallFlowCtrl.GetStepOperation();
            }
        }

        if (modelOp != null)
        {
            // 恢复操作权限：将他人操作记录到userOpModel
            uiModule.AcquireOperatePermission(senderId, modelOp);
        }
    }

    #region 帧同步通道相关接口
    public void SendFrameMsg(MsgBase msg)
    {
        if (mFrameChannelAgent == null || !mFrameChannelAgent.IsChannelConnected())
            return;
        mFrameChannelAgent.SendFrame(msg);
    }
    #endregion

    #region 音频通道相关接口
    public void RequestMicrophone(CaptureAudioFromMultipleSources AudioFromMultipleSources = null, UnityAction callback = null)
    {
        if (mAudioChannelAgent.localMicEncoder)
        {
            //麦克风已经初始化成功
            if (mAudioChannelAgent.localMicEncoder.MicStartSuccess)
            {
                callback?.Invoke();
            }
            else
            {
                mAudioChannelAgent.localMicEncoder.OnMicInitSuccess.RemoveAllListeners();
                mAudioChannelAgent.localMicEncoder.OnMicInitSuccess.AddListener((audioFilterReadForwarder) =>
                {
                    if (AudioFromMultipleSources != null)
                        AudioFromMultipleSources.AddAudioFilterReadForwarder(audioFilterReadForwarder);
                    callback?.Invoke();
                });
                mAudioChannelAgent.localMicEncoder.InitMic();
            }
        }
    }

    public void ReleaseMicrophone()
    {
        if (mAudioChannelAgent.localMicEncoder)
        {
            mAudioChannelAgent.localMicEncoder.ReleaseMic();
        }
    }

    public void EnableLocalMic(bool enabled)
    {
        if (mAudioChannelAgent.localMicEncoder)
        {
            if (enabled)
            {
                //麦克风初始化
                RequestMicrophone(null, () => mAudioChannelAgent.localMicEncoder.StartCapture());
            }
            else
                mAudioChannelAgent.localMicEncoder.StopCapture();
        }
    }

    public void RemoveUserAudio(int userId)
    {
        mAudioChannelAgent.RemoveMicDecoder(userId);
    }
    #endregion

    #region 视频通道相关接口
    public void EnableLocalVideo(bool enabled)
    {
        //协同房间不需要分享主画面
        if (GlobalInfo.roomInfo != null && GlobalInfo.roomInfo.RoomType == (int)RoomType.Synergia)
            return;

        if (mVideoChannelAgent != null && mVideoChannelAgent.localGameViewEncoder != null)
        {
            mVideoChannelAgent.localGameViewEncoder.FastMode = false;
            mVideoChannelAgent.localGameViewEncoder.gameObject.SetActive(enabled);
            if (enabled)
            {
                this.WaitTime(1f, () =>
                {
                    mVideoChannelAgent.localGameViewEncoder.FastMode = true;
                    mVideoChannelAgent.localGameViewEncoder.EnableAsyncGPUReadback = true;
                });
            }
        }
    }

    public void AddUserVideo(string label, GameViewDecoder gameViewDecoder)
    {
        if (gameViewDecoder == null)
            return;
        mVideoChannelAgent.AddGameViewDecoder(label, gameViewDecoder);
    }

    public void RemoveUserVideo(int userId, bool destroy = true)
    {
        mVideoChannelAgent.RemoveGameViewDecoder(userId, destroy);
    }

    public void ClearUserVideo(bool destroy = true)
    {
        mVideoChannelAgent.ClearRemoteVideoDecoders(destroy);
    }

    public void UpdateVideoPacket(string url, bool isPlay, int type, float progressValue = 0)
    {
        mVideoChannelAgent.UpdateVideoPacket(url, isPlay, progressValue, type);
    }

    public string GetVideoPacket()
    {
        return mVideoChannelAgent.GetVideoPacket();
    }

    public void ClearVideoPacket()
    {
        mVideoChannelAgent.ClearVideoPacket();
    }
    #endregion

    #region 成员颜色分配相关接口
    private Color DefaultPlayerColor;
    private Dictionary<Color, int> ColorFlag;
    private Dictionary<int, Color> PlayerColorDic;
    private System.Object flagLock = new System.Object();

    public void InitColor()
    {
        lock (flagLock)
        {
            DefaultPlayerColor = "#CE2879".HexToColor();//"#C02C24"
            ColorFlag = new Dictionary<Color, int>()
                {
                    //{ "#DD6B0E".HexToColor(), 0},
                    //{ "#14A857".HexToColor(), 0},
                    //{ "#5DAA0A".HexToColor(), 0},
                    //{ "#DCA800".HexToColor(), 0},
                    //{ "#4371FF".HexToColor(), 0},
                    //{ "#855AFF".HexToColor(), 0}
                    { "#2EDC7C".HexToColor(), 0},
                    { "#FFA551".HexToColor(), 0},
                    { "#855AFF".HexToColor(), 0},
                    { "#DCA800".HexToColor(), 0},
                    { "#4371FF".HexToColor(), 0}
                };
            PlayerColorDic = new Dictionary<int, Color>() { };
        }
    }

    /// <summary>
    /// 更新全局成员颜色
    /// </summary>
    /// <param name="members"></param>
    public void UpdatePlayerColor(List<Member> members)
    {
        if (members == null || members.Count == 0)
            return;

        lock (flagLock)
        {
            foreach (Member member in members)
            {
                Color color = member.ColorNumber.HexToColor();
                if (ColorFlag.ContainsKey(color))
                {
                    ColorFlag[color] = 1;

                    if (PlayerColorDic.ContainsKey(member.Id))
                        PlayerColorDic[member.Id] = color;
                    else
                        PlayerColorDic.Add(member.Id, color);
                }
            }
        }
    }

    /// <summary>
    /// 分配颜色
    /// </summary>
    /// <param name="userId"></param>
    public Color AllocPlayerColor(int userId)
    {
        lock (flagLock)
        {
            if (userId == GlobalInfo.roomInfo.creatorId)
                return DefaultPlayerColor;

            if (!PlayerColorDic.ContainsKey(userId))
            {
                Color color = DefaultPlayerColor;
                foreach (KeyValuePair<Color, int> keyValue in ColorFlag)
                {
                    if (keyValue.Value == 0)
                    {
                        ColorFlag[keyValue.Key] = 1;
                        color = keyValue.Key;
                        break;
                    }
                }
                PlayerColorDic.Add(userId, color);
            }
            return PlayerColorDic[userId];
        }
    }

    /// <summary>
    /// 回收颜色
    /// </summary>
    /// <param name="userId"></param>
    public void ReleasePlayerColor(int userId)
    {
        lock (flagLock)
        {
            if (userId == GlobalInfo.roomInfo.creatorId)
                return;

            if (PlayerColorDic.TryGetValue(userId, out Color color))
            {
                if (ColorFlag.ContainsKey(color))
                {
                    ColorFlag[color] = 0;
                }
                PlayerColorDic.Remove(userId);
            }
        }
    }

    /// <summary>
    /// 回收全部颜色
    /// </summary>
    public void ReleasePlayerColor()
    {
        lock (flagLock)
        {
            List<Color> colorList = ColorFlag.Keys.ToList();
            foreach (Color c in colorList)
            {
                ColorFlag[c] = 0;
            }
            PlayerColorDic.Clear();
        }
    }

    /// <summary>
    /// 获取指定用户的颜色
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public Color GetPlayerColor(int userId)
    {
        if (userId == GlobalInfo.roomInfo.creatorId)
            return DefaultPlayerColor;

        Color color = Color.white;
        if (PlayerColorDic.ContainsKey(userId))
            color = PlayerColorDic[userId];
        return color;
    }

    public Color GetPlayerColor(string userName)
    {
        int userId = mRoomChannelAgent.onlineUsers.FirstOrDefault(x => x.Value.Nickname.Equals(userName)).Key;

        if (userId == GlobalInfo.roomInfo.creatorId)
            return DefaultPlayerColor;

        Color color = Color.white;
        if (PlayerColorDic.ContainsKey(userId))
            color = PlayerColorDic[userId];
        return color;
    }
    #endregion

    /// <summary>
    /// 是否全部通道成功连接
    /// </summary>
    /// <returns></returns>
    public bool IsAllChannelConnect()
    {
        return mRoomChannelAgent.IsChannelConnected() && mIMChannelAgent.IsChannelConnected() &&
            mAudioChannelAgent.IsChannelConnected() && mVideoChannelAgent.IsChannelConnected() && mFrameChannelAgent.IsChannelConnected();
    }

    /// <summary>
    /// 是否全部通道关闭连接
    /// </summary>
    /// <returns></returns>
    public bool IsAllChannelClosed()
    {
        return !mRoomChannelAgent.IsChannelConnected() && !mIMChannelAgent.IsChannelConnected() &&
            !mAudioChannelAgent.IsChannelConnected() && !mVideoChannelAgent.IsChannelConnected() && !mFrameChannelAgent.IsChannelConnected();
    }
}