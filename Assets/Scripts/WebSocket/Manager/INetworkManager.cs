using static UnityFramework.Runtime.ServiceRequestData;

/// <summary>
/// 协同请求接口
/// </summary>
public interface INetworkManager
{
    /// <summary>
    /// 加入房间
    /// </summary>
    /// <param name="roomInfo"></param>
    void JoinRoom(RoomInfoModel roomInfo);

    /// <summary>
    /// 退出房间
    /// <param name="deleteRoom">房主退出是否删除房间</param>
    /// </summary>
    void LeaveRoom(bool deleteRoom);
}