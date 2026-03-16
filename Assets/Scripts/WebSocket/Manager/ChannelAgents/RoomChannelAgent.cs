using System.Collections.Generic;
using UnityEngine;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.ServiceRequestData;
using Newtonsoft.Json.Linq;

public class RoomChannelAgent : NetworkChannelAgentBase
{
    /// <summary>
    /// 房间成员列表
    /// </summary>
    public List<Member> roomMembers = new List<Member>();
    private int prevMainScreenId;
    /// <summary>
    /// 房间在线成员字典
    /// </summary>
    public Dictionary<int, Member> onlineUsers = new Dictionary<int, Member>();
    private Dictionary<int, Member> tempOnlineUsers = new Dictionary<int, Member>();
    private Dictionary<int, Member> cacheOnlineUsers = new Dictionary<int, Member>();

    private float deltaTime;

    /// <summary>
    /// 消息接收队列
    /// </summary>
    private Queue<string> msgQueue = new Queue<string>();
    private string currMsg;

    public override void InitNetworkChannel()
    {
        networkChannel = new NetworkChannel(ChannelType.rtm);
        networkManager.InitColor();
    }

    protected override void OnChannelOpen()
    {
        base.OnChannelOpen();
        Clear();
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    /// <param name="cmd"></param>
    public void SendCommand(string cmd)
    {
        DebugHelper.Error(ChannelType.rtm, $"[send] {cmd}");
        networkChannel.SendAsync(cmd);
    }

    /// <summary>
    /// 获取用户信息
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>

    public Member FindMemberById(int id)
    {
        foreach (var item in roomMembers)
        {
            if (item.Id == id)
            {
                return item;
            }
        }
        return null;
    }

    /// <summary>
    /// 消息处理
    /// </summary>
    private void LateUpdate()
    {
        deltaTime += Time.deltaTime;

        while (GlobalInfo.roomInfo != null && msgQueue.Count > 0 && deltaTime > 0.01f)
        {
            deltaTime = 0;

            currMsg = msgQueue.Dequeue();
            JObject jObject = JObject.Parse(currMsg);
            if (jObject != null)
            {
                string type = jObject[NetworkManager.TYPE].ToString();

                switch (type)
                {
                    case NetworkManager.MEMBER_LIST:
                        //成员列表消息
                        UpdateRoomMembers(JsonTool.DeSerializable<List<Member>>(jObject[NetworkManager.PAYLOAD]["members"].ToString()));
                        break;
                    case NetworkManager.MEMBER_IN:
                        //成员进入房间消息
                        OtherJoinRoom(int.Parse(jObject[NetworkManager.PAYLOAD]["member"]["id"].ToString()), jObject[NetworkManager.PAYLOAD]["member"]["nickName"].ToString());
                        break;
                    case NetworkManager.MEMBER_OUT:
                        //成员离开房间消息
                        OtherLeaveRoom(int.Parse(jObject[NetworkManager.PAYLOAD]["member"]["id"].ToString()), jObject[NetworkManager.PAYLOAD]["member"]["nickName"].ToString());
                        break;
                    case NetworkManager.SILENT_ALL:
                        //全员禁言消息
                        UpdateAllPlayerTalkState(false, true);
                        break;
                    case NetworkManager.SILENT_OFF_ALL:
                        //全员取消禁言消息
                        UpdateAllPlayerTalkState(true, true);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// 更新用户
    /// </summary>
    /// <param name="newId">发生更新的用户id</param>
    /// <param name="members"></param>
    private void UpdateRoomMembers(List<Member> members)
    {
        if (roomMembers == null || roomMembers.Count == 0)
        {
            if (GlobalInfo.roomInfo.RoomType == (int)RoomType.Synergia && members.Find(m => m.Id == GlobalInfo.roomInfo.creatorId) == null)
            {
                // 加入房间时若房主异常离线，提示退出房间
                // 避免协同房间无房主为新成员分配权限颜色
                Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
                popupDic.Add("确定", new PopupButtonData(() =>
                {
                    ModelManager.Instance.DestroySyncComponent();
                    NetworkManager.Instance.ReleaseMicrophone();
                    NetworkManager.Instance.LeaveRoom();
                }, true));
                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "房主不在房间内，请退出房间重试", popupDic, null, false));
            }
        }

        roomMembers = members;

        //缓存当前用户列表
        cacheOnlineUsers.Clear();
        foreach (KeyValuePair<int, Member> valuePair in onlineUsers)
        {
            cacheOnlineUsers.Add(valuePair.Key, valuePair.Value);
        }
        //更新在线用户列表
        onlineUsers.Clear();
        foreach (var member in roomMembers)
        {
            if (!onlineUsers.ContainsKey(member.Id))
                onlineUsers.Add(member.Id, member);
        }

        //在线用户状态发生变化isTalk/isControl/isChat/isMainScreen
        foreach (var member in onlineUsers)
        {
            if (cacheOnlineUsers.TryGetValue(member.Key, out Member prevMemberState))
            {
                UpdateMember(member.Key, prevMemberState);
            }
        }

        UpdateGlobalValue(roomMembers);

        SendMsg(new MsgBase((ushort)RoomChannelEvent.UpdateMemberList));
    }

    /// <summary>
    /// 更新全员闭麦状态
    /// </summary>
    /// <param name="isAllTalk"></param>
    private void UpdateAllPlayerTalkState(bool isAllTalk, bool showToast = true)
    {
        GlobalInfo.isAllTalk = isAllTalk;
        SendMsg(new MsgBoolBool((ushort)RoomChannelEvent.TalkState, isAllTalk, showToast));

        if (!GlobalInfo.isAllTalk)
        {
            if (!GlobalInfo.IsHomeowner())
                networkManager.EnableLocalMic(false);
        }
        else
        {
            Member member = FindMemberById(GlobalInfo.account.id);
            if (member != null && member.Id != GlobalInfo.roomInfo.creatorId)
            {
                if (member.IsChat)
                    networkManager.EnableLocalMic(true);
                else
                    networkManager.EnableLocalMic(false);
            }
        }
    }

    /// <summary>
    /// 成员加入房间
    /// </summary>
    /// <param name="newJoinedId"></param>
    private void OtherJoinRoom(int newJoinedId, string newJoinedName)
    {
        // 新协同服务分为member_in 和member_list; 接收到member_in时newJoinerId可能还不在当前成员列表中
        //if (!onlineUsers.TryGetValue(newJoinedId, out Member newJoinedMember))
        //    return;

        if (!GlobalInfo.IsHomeowner() && newJoinedId == GlobalInfo.roomInfo.creatorId)
        {
            UpdateAllPlayerTalkState(roomMembers[0].IsTalk, false);
        }

        //SendMsg(new MsgIntString((ushort)RoomChannelEvent.OtherJoin, newJoinedMember.Id, newJoinedMember.Nickname));
        SendMsg(new MsgIntString((ushort)RoomChannelEvent.OtherJoin, newJoinedId, newJoinedName));
    }

    /// <summary>
    /// 成员离开房间
    /// </summary>
    /// <param name="memberId"></param>
    /// <param name="memberNickName"></param>
    private void OtherLeaveRoom(int memberId, string memberNickName)
    {
        if (GlobalInfo.IsHomeowner())
        {
            networkManager.ReleasePlayerColor(memberId);
            if (memberId == prevMainScreenId)
                networkManager.SetUserMainView(GlobalInfo.account.id, true);
        }

        SendMsg(new MsgIntString((ushort)RoomChannelEvent.OtherLeave, memberId, memberNickName));
        //networkManager.ClearUserIMState(memberId);
        networkManager.RemoveUserAudio(memberId);
        networkManager.RemoveUserVideo(memberId, false);
    }

    /// <summary>
    /// 更新用户状态
    /// </summary>
    /// <param name="newId">用户Id</param>
    /// <param name="prevMemberState">用户之前的状态</param>
    private void UpdateMember(int newId, Member prevMemberState)
    {
        if (onlineUsers.TryGetValue(newId, out Member newMember))
        {
            //用户禁言状态改变
            if (newMember.IsTalk != prevMemberState.IsTalk)
            {
                if (GlobalInfo.account.id == newMember.Id)
                {
                    networkManager.EnableLocalMic(newMember.IsTalk && newMember.IsChat);
                    UIManager.Instance.OpenModuleUI<ToastPanel>(null, UILevel.PopUp, new ToastPanelInfo(newMember.IsTalk ? "禁言解除" : "你已被禁言"));
                }
                else
                {
                    //移除被禁言用户的AudioDecoder
                    if (!newMember.IsTalk)
                        networkManager.RemoveUserAudio(newMember.Id);
                }
            }

            //用户主画面改变
            if (newMember.IsMainScreen != prevMemberState.IsMainScreen)
            {
                SendMsg(new MsgIntBool((ushort)RoomChannelEvent.UpdateMainScreen, newMember.Id, newMember.IsMainScreen));
            }

            //用户操作权限改变
            if (newMember.IsControl != prevMemberState.IsControl)
            {
                //networkManager.ClearUserIMState(newMember.Id);
                SendMsg(new MsgIntBool((ushort)RoomChannelEvent.UpdateControl, newMember.Id, newMember.IsControl));
            }

            //用户语音状态改变
            if (newMember.IsChat != prevMemberState.IsChat)
            {
                if (GlobalInfo.account.id == newMember.Id)
                {
                    networkManager.EnableLocalMic(newMember.IsChat);
                }
                else
                {
                    //移除关麦用户的AudioDecoder
                    if (!newMember.IsChat)
                        networkManager.RemoveUserAudio(newMember.Id);
                }
            }
        }
    }

    /// <summary>
    /// 更新房间全局值：当前主画面、操作用户ID、用户颜色
    /// </summary>
    /// <param name="members"></param>
    private void UpdateGlobalValue(List<Member> members)
    {
        prevMainScreenId = GlobalInfo.mainScreenId;
        //更新主画面ID
        GlobalInfo.mainScreenId = -1;
        foreach (Member m in members)
        {
            if (m.IsMainScreen && m.Id != GlobalInfo.roomInfo.creatorId)
            {
                GlobalInfo.mainScreenId = m.Id;
                break;
            }
        }
        if (GlobalInfo.mainScreenId == -1)
            GlobalInfo.mainScreenId = GlobalInfo.roomInfo.creatorId;

        //更新权限ID
        GlobalInfo.controllerIds.Clear();
        foreach (Member m in members)
        {
            if (m.IsControl)
                GlobalInfo.controllerIds.Add(m.Id);
            else
                GlobalInfo.controllerIds.Remove(m.Id);
        }
        if (GlobalInfo.controllerIds.Count == 0)
            GlobalInfo.controllerIds.Add(GlobalInfo.roomInfo.creatorId);

        //更新用户颜色
        networkManager.UpdatePlayerColor(members);
    }

    public override void ProcessMessage(string message)
    { 
        if (!enabled) return;
        DebugHelper.Info(ChannelType.rtm, $"[recv] {message}");
        msgQueue.Enqueue(message);
    }

    public void Clear()
    {
        roomMembers?.Clear();
        onlineUsers.Clear();
        tempOnlineUsers.Clear();
        msgQueue.Clear();

        if (!GlobalInfo.IsHomeowner())
            networkManager.ReleasePlayerColor();
    }
}