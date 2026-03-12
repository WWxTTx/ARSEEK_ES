using UnityEngine;
using UnityFramework.Runtime;

/// <summary>
/// RTI同步通道相关接口
/// </summary>
public interface IIMAgentHelper
{
    /// <summary>
    /// 发送同步消息
    /// </summary>
    /// <param name="msg"></param>
    void SendIMMsg(MsgBrodcastOperate msg);

    /// <summary>
    /// 同步缓存版本
    /// 直播房间无权限成员获取操作权时调用
    /// </summary>
    void SyncCachedVersion();

    /// <summary>
    /// 同步百科状态
    /// </summary>
    void SyncBaikeState();
}