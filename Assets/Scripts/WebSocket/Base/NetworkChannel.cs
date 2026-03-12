using System;
using UnityEngine;
using UnityFramework.Runtime;
using BestHTTP.WebSocket;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

/// <summary>
/// 网络通道
/// 封装websocket
/// </summary>
public class NetworkChannel
{
    /// <summary>
    /// 通道类型
    /// </summary>
    public int ChannelType => (int)channelType;

    /// <summary>
    /// websocket是否连接
    /// </summary>
    public bool IsConnect
    {
        get
        {
            if (webSocket != null)
                return webSocket.IsOpen;
            return false;
        }
    }

    /// <summary>
    /// 接收到json消息时调用
    /// </summary>
    public UnityEventString OnReceivedStringEvent;

    /// <summary>
    /// 接收到二进制消息时调用
    /// </summary>
    public UnityEventIntByteArray OnReceivedByteEvent;

    /// <summary>
    /// websocket对象
    /// </summary>
    private WebSocket webSocket;
    
    /// <summary>
    /// 通道类型
    /// </summary>
    private ChannelType channelType;

    /// <summary>
    /// 发送超时时间
    /// </summary>
    private float sendTimeoutSeconds = 5f;

    /// <summary>
    /// 发送队列
    /// </summary>
    private ConcurrentQueue<string> _sendQueue = new ConcurrentQueue<string>();
    private bool _isProcessing = false;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="channelType"></param>
    public NetworkChannel(ChannelType channelType)
    {
        this.channelType = channelType;
        OnReceivedStringEvent = new UnityEventString();
        OnReceivedByteEvent = new UnityEventIntByteArray();
        AntiInit();
    }

    /// <summary>
    /// 连接服务
    /// </summary>
    public void Connect(string url, string roomUuid)
    {
        webSocket = new WebSocket(new Uri(url));
        webSocket.StartPingThread = false;
        //建立连接header
        webSocket.InternalRequest.AddHeader("Authorization", $"Bearer {ApiData.AccessToken}");
        webSocket.InternalRequest.AddHeader("RoomUUID", roomUuid);
        webSocket.InternalRequest.AddHeader("ClientType", ApiData.ClientID);

        webSocket.OnOpen += OnOpen;
        //接收和发送的消息均采用JSON格式
        webSocket.OnMessage += OnMessage;
        webSocket.OnClosed += OnClosed;
        webSocket.OnError += OnError;

        webSocket.Open();
        DebugHelper.Debug(channelType, $"开始WebSocket连接,url: {url}");
    }

    /// <summary>
    /// 关闭连接
    /// </summary>
    public void Close()
    {
        if (webSocket == null || !webSocket.IsOpen)
        {
            DebugHelper.Debug(channelType, "已关闭WebSocket连接");
            return;
        }
        DebugHelper.Debug(channelType, "关闭WebSocket连接");
        webSocket.Close();
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    /// <param name="message"></param>
    public void Send(string message)
    {
        if (IsConnect)
            webSocket.Send(message);
    }

    /// <summary>
    /// 异步发送
    /// </summary>
    /// <param name="message"></param>
    public async void SendAsync(string message)
    {
        await SendAsyncWithTimeout(message);
    }

    /// <summary>
    /// 队列发送 待完善
    /// </summary>
    /// <param name="message"></param>
    public void EnqueueSend(string message)
    {
        _sendQueue.Enqueue(message);
        TryProcessQueue();
    }

    private async void TryProcessQueue()
    {
        if (_isProcessing) return;
        _isProcessing = true;
        while (_sendQueue.TryDequeue(out string msg))
        {
            await SendAsyncWithTimeout(msg);
            // 让出一帧
            await Task.Yield(); 
        }
        _isProcessing = false;
    }

    /// <summary>
    /// 异步发送消息，带超时控制
    /// </summary>
    /// <param name="message">要发送的消息</param>
    /// <returns>是否发送成功</returns>
    private async Task<bool> SendAsyncWithTimeout(string message)
    {
        if (!IsConnect)
        {
            Log.Warning($"[{channelType}] WebSocket连接已关闭");
            return false;
        }

        // 包装 Send 调用到后台线程（避免阻塞主线程）
        Task<bool> sendTask = Task.Run(() =>
        {
            try
            {
                webSocket.Send(message);
                return true; // 发送成功
            }
            catch (Exception ex)
            {
                Log.Error($"[{channelType}] WebSocket发送异常: {ex.Message}");
                return false;
            }
        });

        // 设置超时（使用 Unity 兼容的 float → int 毫秒）
        var timeoutMs = (int)(sendTimeoutSeconds * 1000);
        var delayTask = Task.Delay(timeoutMs);

        // 等待任务完成或超时
        Task completedTask = await Task.WhenAny(sendTask, delayTask);

        if (completedTask == sendTask)
        {
            bool result = await sendTask;
            return result;
        }
        else
        {
            Log.Warning($"[{channelType}] WebSocket发送超时");
            return false;
        }
    }
  
    /// <summary>
    /// 注销初始化
    /// </summary>
    private void AntiInit()
    {
        if (webSocket != null)
        {
            webSocket.OnOpen -= OnOpen;
            webSocket.OnMessage -= OnMessage;
            webSocket.OnError -= OnError;
            webSocket.OnClosed -= OnClosed;
        }
        webSocket = null;
    }

    #region 事件处理
    private void OnOpen(WebSocket ws)
    {
        DebugHelper.Debug(channelType, "成功建立连接");
        FormMsgManager.Instance.SendMsg(new MsgInt((ushort)NetworkChannelEvent.Open, ChannelType));
    }

    private void OnClosed(WebSocket ws, ushort code, string message)
    {
        DebugHelper.Warning(channelType, $"OnClosed {code} {message}");

        //1000 Bye!
        //3001 evit
        //3002 room_close
        if(code == 1000 || code == 3001 || code == 3002)
        {
            AntiInit();
            FormMsgManager.Instance.SendMsg(new MsgIntString((ushort)NetworkChannelEvent.Closed, ChannelType, message));
        }
        else
        {
            //todo 未知原因关闭 重连
            Close();
            AntiInit();
            FormMsgManager.Instance.SendMsg(new MsgInt((ushort)NetworkChannelEvent.Error, ChannelType));
        }
    }

    private void OnError(WebSocket ws, Exception ex)
    {
        DebugHelper.Error(channelType, $"OnError {ex?.Message}");
        string errorMsg = string.Empty;
#if !UNITY_WEBGL || UNITY_EDITOR
        if (ws.InternalRequest.Response != null)
            errorMsg = string.Format("Status Code from Server: {0} and Message: {1}", ws.InternalRequest.Response.StatusCode, ws.InternalRequest.Response.Message);
#endif
        if (!string.IsNullOrEmpty(errorMsg))
            DebugHelper.Error(channelType, errorMsg);

        Close();
        AntiInit();
        FormMsgManager.Instance.SendMsg(new MsgInt((ushort)NetworkChannelEvent.Error, ChannelType));
    }

    private void OnMessage(WebSocket webSocket, string message)
    {
        try
        {
            JObject jObject = JObject.Parse(message);
            if (jObject == null)
                return;

            string type = jObject[NetworkManager.TYPE].ToString();
            switch (type)
            {
                case NetworkManager.PING://接收服务端发送的心跳ping消息 
                    //DebugHelper.Warning(channelType, NetworkManager.PING);
                    //发送心跳pong
                    SendAsync(NetworkManager.PONG);
                    break;
                case NetworkManager.ERROR:
                    //错误消息
                    DebugHelper.Error(channelType, $"OnErrorMessage {jObject[NetworkManager.CODE]} {jObject[NetworkManager.MESSAGE]}");
                    break;
                case NetworkManager.ROOM_CLOSE:
                    //TODO
                    break;
                default:
                    //各通道消息由通道代理处理
                    OnReceivedStringEvent?.Invoke(message);
                    break;
            }
        }
        catch(Exception e)
        {
            DebugHelper.Error(channelType, $"{e.Message}");
        }
    }
    #endregion
}