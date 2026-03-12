using UnityFramework.Runtime;

/// <summary>
/// RTC帧同步通道相关接口
/// </summary>
public interface IFrameAgentHelper
{
    /// <summary>
    /// 发送帧同步消息
    /// </summary>
    /// <param name="msg"></param>
    void SendFrameMsg(MsgBase msg);
}