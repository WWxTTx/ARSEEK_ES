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
    /// 同步百科状态
    /// </summary>
    void SyncBaikeState();
}
