using UnityFramework.Runtime;

/// <summary>
/// 网络通道代理基类
/// </summary>
public abstract class NetworkChannelAgentBase : MonoBase
{
    protected NetworkManager networkManager;

    protected NetworkChannel networkChannel;

    /// <summary>
    /// 初始化通道
    /// </summary>
    public abstract void InitNetworkChannel();

    /// <summary>
    /// 连接通道
    /// </summary>
    /// </summary>
    public void Connect(NetworkManager networkManager, string url, string roomUuid)
    {
        this.networkManager = networkManager;

        AddMsg(new ushort[]
        {
            (ushort)NetworkChannelEvent.Open,
            (ushort)NetworkChannelEvent.Closed,
            (ushort)NetworkChannelEvent.Error
        });

        InitNetworkChannel();
        networkChannel.OnReceivedStringEvent.AddListener((message) => ProcessMessage(message));

        networkChannel.Connect(url, roomUuid);
    }

    /// <summary>
    /// 重连
    /// </summary>
    public void Reconnect(string url, string roomUuid)
    {
        if (networkChannel == null || IsChannelConnected())
            return;

        networkChannel.Close();
        networkChannel.Connect(url, roomUuid);
    }

    /// <summary>
    /// 消息处理
    /// </summary>
    /// <param name="message"></param>
    public abstract void ProcessMessage(string message);

    /// <summary>
    /// 关闭连接
    /// </summary>
    public void Close()
    {
        if (networkChannel != null)
        {
            networkChannel.Close();
        }
    }

    /// <summary>
    /// 通道是否连接
    /// </summary>
    /// <returns></returns>
    public bool IsChannelConnected()
    {
        return networkChannel != null && networkChannel.IsConnect;
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)NetworkChannelEvent.Open:
                if (((MsgInt)msg).arg == networkChannel.ChannelType)
                {
                    OnChannelOpen();
                }
                break;
            case (ushort)NetworkChannelEvent.Closed:
                MsgIntString channelClosed = (MsgIntString)msg;
                if (channelClosed.arg1 == networkChannel.ChannelType)
                {
                    OnChannelClosed();
                    networkManager.OnCommonChannelClosed(channelClosed.arg2);
                }
                break;
            case (ushort)NetworkChannelEvent.Error:
                if (((MsgInt)msg).arg == networkChannel.ChannelType)
                {
                    OnChannelError();
                    networkManager.OnCommonChannelError();
                }
                break;
        }
    }

    /// <summary>
    /// 通道成功建立连接事件
    /// </summary>
    protected virtual void OnChannelOpen()
    {

    }

    /// <summary>
    /// 通道连接关闭事件
    /// </summary>
    protected virtual void OnChannelClosed()
    {

    }

    /// <summary>
    /// 通道连接异常事件
    /// </summary>
    protected virtual void OnChannelError()
    {

    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Close();
    }

    private void OnApplicationQuit()
    {
        Close();
    }
}
