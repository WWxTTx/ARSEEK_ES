using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.ServiceRequestData;

/// <summary>
/// 协同服务管理类
/// 模块类图及使用说明 https://www.processon.com/view/link/65a4964f15194521b125c832
/// </summary>
public partial class NetworkManager : Singleton<NetworkManager>, INetworkManager, IRoomAgentHelper, IIMAgentHelper, IFrameAgentHelper, IAudioAgentHelper, IVideoAgentHelper
{
    private RoomChannelAgent mRoomChannelAgent;
    private IMChannelAgent mIMChannelAgent;
    private AudioChannelAgent mAudioChannelAgent;
    private VideoChannelAgent mVideoChannelAgent;
    private FrameChannelAgent mFrameChannelAgent;

    /// <summary>
    /// 上一次请求登录的房间信息
    /// </summary>
    private string lastRoomUuid;
    private string lastRoomPassword;

    /// <summary>
    /// 当前重连次数
    /// </summary>
    private int attemptCount;

    /// <summary>
    /// 等待连接成功
    /// </summary>
    private Coroutine waitConnectCo;
    /// <summary>
    /// 重连
    /// </summary>
    private Coroutine delayReconnectCo;
    /// <summary>
    /// 强制结束协同
    /// </summary>
    private Coroutine forceShutdownCo;

    /// <summary>
    /// 是否正在加入房间
    /// </summary>
    private bool joiningRoom;
    /// <summary>
    /// 是否正在离开房间
    /// </summary>
    private bool isLeavingRoom = false;
    public bool IsLeavingRoom { get { return isLeavingRoom; } protected set { isLeavingRoom = value; } }
    /// <summary>
    /// 避免重复重连
    /// </summary>
    private bool lockReconnect = false;
    /// <summary>
    /// 避免重复退出
    /// </summary>
    private bool lockQuit = false;

    protected override void InstanceAwake()
    {
        base.InstanceAwake();

        requestBase = RequestManager.Instance.GetComponent<RequestBase>();

        mRoomChannelAgent = GetComponentInChildren<RoomChannelAgent>();
        mIMChannelAgent = GetComponentInChildren<IMChannelAgent>();
        mAudioChannelAgent = GetComponentInChildren<AudioChannelAgent>();
        mVideoChannelAgent = GetComponentInChildren<VideoChannelAgent>();
        mFrameChannelAgent = GetComponentInChildren<FrameChannelAgent>();

        ServiceApiData.SetWSUrls();
    }

    /// <summary>
    /// 加入房间(连接通道)
    /// </summary>
    /// <param name="roomInfo"></param>
    public void JoinRoom(RoomInfoModel roomInfo)
    {
        attemptCount = 0;
        lastRoomUuid = roomInfo.Uuid;
        lastRoomPassword = roomInfo.Password;
        joiningRoom = true;
        IsLeavingRoom = false;

        this.WaitTime(2f, () =>
        {
            mRoomChannelAgent.Connect(this, ServiceApiData.rtm_url, roomInfo.Uuid);
            mIMChannelAgent.Connect(this, ServiceApiData.rti_url, roomInfo.Uuid);
            mAudioChannelAgent.Connect(this, ServiceApiData.rta_url, roomInfo.Uuid);
            mVideoChannelAgent.Connect(this, ServiceApiData.rtv_url, roomInfo.Uuid);
            mFrameChannelAgent.Connect(this, ServiceApiData.rtc_url, roomInfo.Uuid);

            waitConnectCo = StartCoroutine(WaitConnectUntilTimeout());
        });
    }

    /// <summary>
    /// 等待全部通道连接成功直至超时
    /// </summary>
    /// <returns></returns>
    private IEnumerator WaitConnectUntilTimeout()
    {
        DateTime start = DateTime.Now;
        yield return new WaitUntil(() => IsAllChannelConnect() || IsTimeout(start));

        UIManager.Instance.CloseUI<LoadingPanel>();
        lockReconnect = false;

        //连接超时
        if (!IsAllChannelConnect())
        {
            TimeoutPopup();
            yield break;
        }

        attemptCount = 0;
        if (joiningRoom)
        {
            Log.Debug("加入房间成功");
            FormMsgManager.Instance.SendMsg(new MsgBase((ushort)RoomChannelEvent.JoinRoomSuccess));
            joiningRoom = false;
        }
    }

    /// <summary>
    /// 退出房间
    /// </summary>
    /// <param name="deleteRoom">是否删除房间</param>
    public void LeaveRoom(bool deleteRoom = true)
    {
        Log.Debug("退出协同房间");
        IsLeavingRoom = true;
        StopReconnect();
        UIManager.Instance.OpenUI<LoadingPanel>(UILevel.Loading);

        mIMChannelAgent.Clear(true);

        if (GlobalInfo.IsHomeowner())
        {
            if (deleteRoom)
            {
                DeleteRoom(GlobalInfo.roomInfo.Uuid, () =>
                {
                    FormMsgManager.Instance.SendMsg(new MsgBase((ushort)RoomChannelEvent.UpdateRoomList));
                }, (code, msg) =>
                {
                    Log.Error($"删除房间[{GlobalInfo.roomInfo.Uuid}]失败: {msg}");
                });
            }
        }

        // 发送退出消息给服务器，确保其他成员能收到离开通知
        SendLeaveMessage();

        //退出房间 主动断开连接
        CloseAllConnection();
        WaitForceQuit("退出房间成功");
    }

    /// <summary>
    /// 发送退出消息给服务器，确保服务器能广播 member_out 给其他成员
    /// </summary>
    private void SendLeaveMessage()
    {
        GlobalInfo.CreatedMode = false;
        if (mRoomChannelAgent != null && mRoomChannelAgent.IsChannelConnected())
        {
            try
            {
                // 发送退出消息，服务器收到后会广播 member_out 给其他成员
                JObject leaveMsg = new JObject
                {
                    [TYPE] = "leave",
                    [PAYLOAD] = new JObject()
                    {
                        ["memberId"] = GlobalInfo.account.id
                    }
                };
                mRoomChannelAgent.SendCommand(leaveMsg.ToString());
                Log.Debug("已发送退出房间消息到服务器");
            }
            catch (System.Exception ex)
            {
                Log.Warning($"发送退出消息失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 延迟发送退出消息后关闭连接
    /// </summary>
    /// <param name="delay">延迟时间（秒）</param>
    /// <returns></returns>
    private IEnumerator DelayThenCloseConnection(float delay = 0.5f)
    {
        yield return new WaitForSeconds(delay);
        CloseAllConnection();
    }

    /// <summary>
    /// 确保退出房间
    /// </summary>
    /// <param name="message"></param>
    public void EnsureLeaveRoom(string message)
    {
        if (lockQuit)
            return;
        lockQuit = true;
        StopReconnect();
        //退出房间 主动断开连接
        CloseAllConnection();
        StartCoroutine(_onLeaveRoom(message));
    }

    /// <summary>
    /// 等待全部通道成功关闭连接后再退出
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private IEnumerator _onLeaveRoom(string message)
    {
        WaitForceQuit(message);
        yield return new WaitUntil(() => IsAllChannelClosed());
        CancelForceQuit();

        FormMsgManager.Instance.SendMsg(new MsgString((ushort)RoomChannelEvent.LeaveRoom, message));

        lockQuit = false;
        IsLeavingRoom = false;
    }

    /// <summary>
    /// 通用通道onClosed事件
    /// </summary>
    /// <param name="message"></param>
    public void OnCommonChannelClosed(string message)
    {
        switch (message)
        {           
            //case heartCheckMsg:
            //    FormMsgManager.Instance.SendMsg(new MsgBase((ushort)NetworkChannelEvent.HeartMiss));
            //    DelayReconnect();
            //    break;
            //case remoteLoginMsg:
            //    PlayerPrefs.DeleteKey(GlobalInfo.lastSynergiaRoomId);
            //    EnsureLeaveRoom("您的账号已经在异地登录");
            //    break; 
            case ROOM_CLOSE://roomDisMsg
                if (!GlobalInfo.IsHomeowner())
                {
                    UIManager.Instance.CloseUI<PopupPanel>();
                    UIManager.Instance.CloseUI<PopupPanel_AutoConfirm>();
                    //由OPLSynCoursePanel ExamCoursePanel监听
                    FormMsgManager.Instance.SendMsg(new MsgBase((ushort)RoomChannelEvent.RoomClose));
                }
                break;
            case EVICT://kickedMsg
                //RTM收到消息，主动关闭其他连接
                Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
                popupDic.Add("知道了", new PopupButtonData(() =>
                {
                    CloseAllConnection();
                    EnsureLeaveRoom(string.Empty);
                }, true));
                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "你已被移出房间", popupDic, () =>
                {
                    CloseAllConnection();
                    EnsureLeaveRoom(string.Empty);
                }));
                break;
            case BYE://quitRoomMsg
                PlayerPrefs.DeleteKey(GlobalInfo.lastSynergiaRoomId);
                EnsureLeaveRoom("退出房间成功");
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 通用通道onError事件
    /// </summary>
    public void OnCommonChannelError()
    {
        DelayReconnect();
    }

    /// <summary>
    /// 主动断开连接
    /// </summary>
    private void CloseAllConnection()
    {
        mRoomChannelAgent.Close();
        mIMChannelAgent.Close();
        mFrameChannelAgent.Close();
        mAudioChannelAgent.Close();
        mVideoChannelAgent.Close();
    }

    /// <summary>
    /// 延迟重连
    /// </summary>
    private void DelayReconnect()
    {
        //退出房间过程中不进行重连
        if (IsLeavingRoom || lockQuit)
            return;

        StopReconnect();
        if(!joiningRoom)
            UIManager.Instance.OpenUI<LoadingPanel>(UILevel.Loading);
        delayReconnectCo = StartCoroutine(_delayReconnect());
    }

    private IEnumerator _delayReconnect()
    {
        yield return new WaitForSeconds(ReconnectDelay);
        if (lockReconnect || lockQuit)
            yield break;

        //静默重连失败后提示
        if (attemptCount >= MaxReconnectAttempt)
        {
            TimeoutPopup();
            yield break;
        }

        Log.Warning("开始重连");
        lockReconnect = true;
        attemptCount++;

        mFrameChannelAgent.Reconnect(ServiceApiData.rtc_url, lastRoomUuid);
        mAudioChannelAgent.Reconnect(ServiceApiData.rta_url, lastRoomUuid);
        mVideoChannelAgent.Reconnect(ServiceApiData.rtv_url, lastRoomUuid);
        mIMChannelAgent.Reconnect(ServiceApiData.rti_url, lastRoomUuid);
        mRoomChannelAgent.Reconnect(ServiceApiData.rtm_url, lastRoomUuid);

        waitConnectCo = StartCoroutine(WaitConnectUntilTimeout());
    }

    /// <summary>
    /// 停止重连
    /// </summary>
    private void StopReconnect()
    {
        lockReconnect = false;
        if (delayReconnectCo != null)
        {
            StopCoroutine(delayReconnectCo);
            delayReconnectCo = null;
        }
        if (waitConnectCo != null)
        {
            StopCoroutine(waitConnectCo);
            waitConnectCo = null;
        }
        UIManager.Instance.CloseUI<LoadingPanel>();
    }

    /// <summary>
    /// 退出房间超时时，强制关闭连接
    /// </summary>
    private void WaitForceQuit(string message)
    {
        CancelForceQuit();
        forceShutdownCo = StartCoroutine(_forceQuit(message));
    }
    private IEnumerator _forceQuit(string message)
    {
        yield return new WaitForSeconds(Timeout);
        BestHTTP.HTTPManager.OnQuit();
        FormMsgManager.Instance.SendMsg(new MsgString((ushort)RoomChannelEvent.LeaveRoom, message));
        lockQuit = false;
        IsLeavingRoom = false;
    }

    /// <summary>
    /// 成功退出房间取消强制退出
    /// </summary>
    private void CancelForceQuit()
    {
        if (forceShutdownCo != null)
        {
            StopCoroutine(forceShutdownCo);
            forceShutdownCo = null;
        }
    }

    /// <summary>
    /// 超时处理
    /// </summary>
    private void TimeoutPopup()
    {
        if (joiningRoom)//GlobalInfo.roomInfo == null
        {
            if(attemptCount >= MaxReconnectAttempt)
            {
                BestHTTP.HTTPManager.OnQuit();
                FormMsgManager.Instance.SendMsg(new MsgString((ushort)RoomChannelEvent.JoinRoomFail, "网络异常，加入房间失败"));
            }
            else
            {
                DelayReconnect();
            }
            return;
        }

        if (attemptCount >= MaxReconnectAttempt)
        {
            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("取消", new PopupButtonData(() =>
            {
                BestHTTP.HTTPManager.OnQuit();
                EnsureLeaveRoom(string.Empty);
            }, false));
            popupDic.Add("确定", new PopupButtonData(() =>
            {
                attemptCount = 0;
                DelayReconnect();
            }, true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "连接超时，是否重新连接?", popupDic, ()=>
            {
                BestHTTP.HTTPManager.OnQuit();
                EnsureLeaveRoom(string.Empty);
            }));
        }
        else
        {
            DelayReconnect();
        }
    }
  
    /// <summary>
    /// 是否超时
    /// </summary>
    /// <param name="sinceTime"></param>
    /// <returns></returns>
    private bool IsTimeout(DateTime sinceTime)
    {
        return (DateTime.Now - sinceTime).TotalSeconds > Timeout;
    }
}