using System.Collections.Generic;
using UnityEngine;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.ServiceRequestData;

public enum DeviceType
{
    PC,
    Mobile,
    Hololens,
    VR
}

/// <summary>
/// 角色同步管理类
/// </summary>
public class PlayerManager : MonoBase
{
    public static PlayerManager Instance;

    /// <summary>
    /// 同步预制体
    /// </summary>
    public GameObject indicatorPrefab;

    /// <summary>
    /// 房间成员字典
    /// </summary>
    private Dictionary<int, GazeIndicator> userIndicators = new Dictionary<int, GazeIndicator>();


    protected override void InitComponents()
    {
        Instance = this;

        AddMsg(new ushort[]{
            (ushort)CoursePanelEvent.SwitchResource,
            (ushort)BaikeSelectModuleEvent.BaikeSelect,
            (ushort)GazeEvent.UserPose,
            (ushort)RoomChannelEvent.LeaveRoom,
            (ushort)StateEvent.PreSyncVersion,
            (ushort)RoomChannelEvent.UpdateMemberList
        });
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)CoursePanelEvent.SwitchResource:
            case (ushort)BaikeSelectModuleEvent.BaikeSelect:
            case (ushort)RoomChannelEvent.LeaveRoom:
            case (ushort)StateEvent.PreSyncVersion:
                //切换课程、百科清空所有成员
                ClearUserIndicators();
                break;
            case (ushort)GazeEvent.UserPose:
                if (ModelManager.Instance.modelGo == null)
                    return;
                MsgIntVector3Vector4 msgIntVector = ((MsgBrodcastOperate)msg).GetData<MsgIntVector3Vector4>();
                if (msgIntVector == null)
                    return;
                //更新成员位置
                SyncUser(msgIntVector.arg, msgIntVector.v3, msgIntVector.v4);
                break;
            case (ushort)RoomChannelEvent.UpdateMemberList:
                //清理已在服务器成员列表中不存在的用户模型
                //模型创建由SyncUser在首次收到位置数据时懒加载，避免场景未就绪时空创建
                List<Member> currentMembers = NetworkManager.Instance.GetRoomMemberList();
                if (currentMembers != null)
                {
                    List<int> toRemove = new List<int>();
                    foreach (int uid in userIndicators.Keys)
                    {
                        if (!currentMembers.Exists(m => m.Id == uid))
                            toRemove.Add(uid);
                    }
                    foreach (int uid in toRemove)
                        RemoveUser(uid);
                }
                break;
        }
    }

    /// <summary>
    /// 同步用户位置朝向
    /// </summary>
    /// <param name="id"></param>
    /// <param name="vector3"></param>
    /// <param name="vector4"></param>
    private void SyncUser(int id, Vector3 vector3, Vector4 vector4)
    {
        if (GlobalInfo.account.id == id)
            return;

        if (!NetworkManager.Instance.IsUserOnline(id))
        {
            RemoveUser(id);
            return;
        }

        if (!userIndicators.ContainsKey(id))
        {
            // 保底：位移时发现模型缺失，检查是否仍在成员列表中再创建
            var members = NetworkManager.Instance.GetRoomMemberList();
            if (members != null && members.Exists(m => m.Id == id))
                TryAddNewUser(id);
            if (!userIndicators.ContainsKey(id))
                return;
        }

        userIndicators[id].UpdatePose(vector3, vector4);
    }

    /// <summary>
    /// 新成员加入且不是自己 新建操作模型
    /// </summary>
    /// <param name="id"></param>
    private void TryAddNewUser(int id)
    {
        //检查用户是否在线，不在线不创建（onlineUsers由OtherLeaveRoom立即更新+MEMBER_LIST重建）
        if (!NetworkManager.Instance.IsUserOnline(id))
            return;

        //等待考核时不创建模型
        if (GlobalInfo.waitExam)
            return;

        //限制 非自身 多人考核非房主 协同
        if (id!= GlobalInfo.account.id && (GlobalInfo.courseMode == CourseMode.Collaboration || (GlobalInfo.courseMode == CourseMode.OnlineExam && !GlobalInfo.IsHomeowner())) )
        {
            GameObject go = Instantiate(indicatorPrefab, transform);
            GazeIndicator gazeIndicator = go.GetComponent<GazeIndicator>();
            gazeIndicator.Init(id);
            userIndicators.Add(id, gazeIndicator);
        }
    }

    /// <summary>
    /// 移除用户模型
    /// </summary>
    /// <param name="id"></param>
    public void RemoveUser(int id)
    {
        if (userIndicators.ContainsKey(id))
        {
            if(userIndicators[id])
                Destroy(userIndicators[id].gameObject);
            userIndicators.Remove(id);
        }
    }

    /// <summary>
    /// 批量移除指定用户的视线标记
    /// </summary>
    /// <param name="userIds">要移除的用户ID列表</param>
    public void RemoveUsers(List<int> userIds)
    {
        if (userIds == null || userIds.Count == 0)
            return;

        foreach (int id in userIds)
        {
            RemoveUser(id);
        }
    }

    /// <summary>
    /// 修改用户视线显示状态
    /// 抓取物体时隐藏射线
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="select"></param>
    public void ChangeUserSelectState(int id, bool select)
    {
        if (userIndicators.ContainsKey(id))
        {
            userIndicators[id].ShowLine(!select);
        }
    }

    /// <summary>
    /// 清空视线标记
    /// </summary>
    public void ClearUserIndicators()
    {
        if (userIndicators.Count == 0)
            return;

        List<GameObject> indicators = new List<GameObject>();
        foreach (GazeIndicator gi in userIndicators.Values)
            indicators.Add(gi.gameObject);

        for (int i = 0; i < indicators.Count; i++)
            Destroy(indicators[i]);

        userIndicators.Clear();
    }

    /// <summary>
    /// 考核开始时调用，刷新所有在线成员的角色模型
    /// </summary>
    public void RefreshAllUsers()
    {
        List<Member> currentMembers = NetworkManager.Instance.GetRoomMemberList();
        if (currentMembers == null)
            return;

        foreach (Member m in currentMembers)
        {
            if (!userIndicators.ContainsKey(m.Id))
                TryAddNewUser(m.Id);
        }
    }
}