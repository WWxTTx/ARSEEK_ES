using UnityFramework.Runtime;

/// <summary>
/// ����ͨ����������
/// </summary>
public abstract class NetworkChannelAgentBase : MonoBase
{
    protected NetworkManager networkManager;

    protected NetworkChannel networkChannel;

    /// <summary>
    /// ��ʼ��ͨ��
    /// </summary>
    public abstract void InitNetworkChannel();

    /// <summary>
    /// ����ͨ��
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
    /// ����
    /// </summary>
    public void Reconnect(string url, string roomUuid)
    {
        if (networkChannel == null || IsChannelConnected())
            return;

        networkChannel.Close();
        networkChannel.Connect(url, roomUuid);
    }

    /// <summary>
    /// ��Ϣ����
    /// </summary>
    /// <param name="message"></param>
    public abstract void ProcessMessage(string message);

    /// <summary>
    /// �ر�����
    /// </summary>
    public void Close()
    {
        if (networkChannel != null)
        {
            networkChannel.Close();
        }
    }

    /// <summary>
    /// ͨ���Ƿ�����
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
    /// ͨ���ɹ����������¼�
    /// </summary>
    protected virtual void OnChannelOpen()
    {

    }

    /// <summary>
    /// ͨ�����ӹر��¼�
    /// </summary>
    protected virtual void OnChannelClosed()
    {

    }

    /// <summary>
    /// ͨ�������쳣�¼�
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
