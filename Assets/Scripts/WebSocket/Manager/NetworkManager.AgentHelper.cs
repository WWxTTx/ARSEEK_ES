using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.ServiceRequestData;
using Newtonsoft.Json.Linq;
using RenderHeads.Media.AVProMovieCapture;

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
    /// 是否正在同步操作
    /// </summary>
    public bool IsIMSync
    {
        get { return mIMChannelAgent.IsStartSync; }
        set
        {
            if (!GlobalInfo.IsLiveMode())
                return;
            mIMChannelAgent.IsStartSync = value;
        }
    }

    /// <summary>
    /// 是否正在状态同步
    /// </summary>
    public bool IsIMSyncState
    {
        get { return mIMChannelAgent.IsSyncState; }
    }

    /// <summary>
    /// 是否正在缓存状态同步
    /// </summary>
    public bool IsIMSyncCachedState
    {
        get { return mIMChannelAgent.IsSyncCachedState; }
    }

    /// <summary>
    /// 是否正在同步百科状态
    /// </summary>
    public bool IsIMSyncBaikeState
    {
        get { return mIMChannelAgent.IsSyncBaikeState; }
        set
        {
            if (!GlobalInfo.IsLiveMode())
                return;
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

    public void SyncCachedVersion()
    {
        mIMChannelAgent.SyncCachedVersion();
    }

    public void SyncBaikeState()
    {
        IMState currentState = mIMChannelAgent.CurrentStateToSync;
        if (currentState == null)
        {
            UIManager.Instance.CloseUI<LoadingPanel>();
            IsIMSync = true;
            return;
        }
        IsIMSyncBaikeState = true;
        StartCoroutine(_syncBaikeStateCo(currentState));
    }

    private IEnumerator _syncBaikeStateCo(IMState currentState)
    {
        ////todo 等待百科完成初始化
        //yield return new WaitForSeconds(0.5f);

        if (IsIMSyncCachedState || IsIMSyncState)
        {
            BaikeState currentBaikeState = currentState.baikeState;
            if (currentBaikeState == null || string.IsNullOrEmpty(currentBaikeState.data))
            {
                yield return new WaitForEndOfFrame();
                if (!IsIMSyncCachedState && !IsIMSyncState)
                    UIManager.Instance.CloseUI<LoadingPanel>();
                IsIMSync = true;
                IsIMSyncBaikeState = false;
                yield break;
            }

            GameObject model = ModelManager.Instance.modelGo;

            switch (GlobalInfo.currentBaikeType)
            {
                case BaikeType.Dismantling:
                    DismantlingBaikeState dismantilingBaikeState = JsonTool.DeSerializable<DismantlingBaikeState>(currentBaikeState.data);
                    if (dismantilingBaikeState != null && model)
                    {
                        DismantlingController dismantlingController = model.GetComponent<DismantlingController>();
                        if (dismantlingController)
                        {
                            Transform foldCtrl = ComponentExtend.FindChildByName(model.transform, dismantilingBaikeState.foldCtrl);
                            if (foldCtrl)
                            {
                                dismantlingController.latestFoldableModel = foldCtrl.GetComponent<ModelOperation>();
                                //跳转到当前拆解层级
                                dismantlingController.JumpToState(foldCtrl.gameObject);
                            }
                            else
                            {
                                //当前无拆解，初始状态全部组合
                                dismantlingController.JumpToState(null);
                            }
                            dismantlingController.isDispersing = false;
                            dismantlingController.isFolding = false;
                        }

                        yield return new WaitForSeconds(0.5f);
                        //同步选中模型
                        SelectionModel selectionModel = model.GetComponent<SelectionModel>();
                        if (dismantilingBaikeState.selectModels != null)
                        {
                            foreach (KeyValuePair<string, int> um in dismantilingBaikeState.selectModels)
                            {
                                if (IsIMSyncCachedState && um.Value == GlobalInfo.account.id)
                                    continue;
                                GameObject selectGo = model.transform.FindChildByName(um.Key)?.gameObject;
                                if (GlobalInfo.IsUserOperator(um.Value))
                                {
                                    selectionModel.SelectModel(selectGo, um.Value);
                                }
                            }
                        }
                    }
                    break;
                case BaikeType.SmallScene:
                    SmallSceneBaikeState smallSceneBaikeState = JsonTool.DeSerializable<SmallSceneBaikeState>(currentBaikeState.data);
                    if (smallSceneBaikeState != null && model)
                    {
                        SmallFlowCtrl smallFlowCtrl = model.GetComponentInChildren<SmallFlowCtrl>(true);
                        if (smallFlowCtrl != null)
                            smallFlowCtrl.SetFinalState(smallSceneBaikeState.modelStates, smallSceneBaikeState.flowIndex, smallSceneBaikeState.stepIndex, smallSceneBaikeState.successOpDatas);
                        
                        UISmallSceneOperationHistory historyModule = UIManager.Instance.canvas.GetComponentInChildren<UISmallSceneOperationHistory>(true);
                        if(historyModule != null)
                        {
                            historyModule.UpdateOpRecordList(smallSceneBaikeState.operations);
                        }

                        if (GlobalInfo.EnableFlow)
                        {
                            UISmallSceneFlowModule flowModule = UIManager.Instance.canvas.GetComponentInChildren<UISmallSceneFlowModule>(true);
                            if (flowModule != null)
                                flowModule.SelectNode(smallSceneBaikeState.flowIndex, smallSceneBaikeState.stepIndex);
                        }

                        UISmallSceneModule smallSceneModule = UIManager.Instance.canvas.GetComponentInChildren<UISmallSceneModule>();
                        if (smallSceneModule != null)
                        {
                            if (!string.IsNullOrEmpty(smallSceneBaikeState.simSystemState))
                            {
                                smallSceneModule.simuSystem?.RecoverSystem(smallSceneBaikeState.simSystemState);
                            }
                            smallSceneModule.RefreshHighlight();
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        yield return new WaitForFixedUpdate();

        if (!IsIMSyncCachedState && !IsIMSyncState)
            UIManager.Instance.CloseUI<LoadingPanel>();
        IsIMSync = true;
        IsIMSyncBaikeState = false;
        //完成百科状态同步后，清空待同步状态，避免切换百科后重复同步
        mIMChannelAgent.CurrentStateToSync = null;
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