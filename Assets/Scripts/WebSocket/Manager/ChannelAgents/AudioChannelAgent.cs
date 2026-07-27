using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityFramework.Runtime;
using Newtonsoft.Json.Linq;

/// <summary>
/// rta音频通道代理
/// </summary>
public class AudioChannelAgent : NetworkChannelAgentBase
{
    /// <summary>
    /// 支持录音时录音笔发送的变声处理
    /// </summary>
    [Header("本地采集")]
    public MicEncoderWithAudioFilter localMicEncoder;

    [Header("解码器预制体")]
    public GameObject audioDecoderPrefab;

    // private Dictionary<string, AudioDecoder> clientMicDecoders = new Dictionary<string, AudioDecoder>();
    /// <summary>
    /// 支持延迟控制的音频解码器
    /// </summary>
    private Dictionary<string, AudioDelayControlDecoder> clientMicDecoders = new Dictionary<string, AudioDelayControlDecoder>();

    public override void InitNetworkChannel()
    {
        networkChannel = new NetworkChannel(ChannelType.rta);

        localMicEncoder.label = int.Parse($"300{GlobalInfo.account.id}");
        localMicEncoder.OnDataByteReadyEvent.AddListener((byteData) =>
        {
            if (!IsChannelConnected())
                return;

            JObject payload = new JObject
            {
                [NetworkManager.LABEL] = localMicEncoder.label,
                [NetworkManager.DATA] = byteData
            };
            // 直播模式下标记广播，确保服务端将音频转发给所有观众（与视频帧行为一致）
            if (GlobalInfo.IsLiveMode())
                payload["broadcast"] = true;

            JObject jObject = new JObject()
            {
                [NetworkManager.TYPE] = NetworkManager.AUDIO,
                [NetworkManager.PAYLOAD] = payload
            };
            networkChannel.SendAsync(jObject.ToString());
        });

        AddMsg(new ushort[]
        {
            (ushort)RoomChannelEvent.TalkState
        });
    }

    protected override void OnChannelOpen()
    {
        base.OnChannelOpen();
        networkManager.EnableLocalMic(networkManager.IsUserChat(GlobalInfo.account.id));
    }

    protected override void OnChannelClosed()
    {
        base.OnChannelClosed();
        networkManager.EnableLocalMic(false);
        ClearRemoteMicDecoders();
    }

    protected override void OnChannelError()
    {
        base.OnChannelError();
        networkManager.EnableLocalMic(false);
        ClearRemoteMicDecoders();
    }

    public override void ProcessMessage(string message)
    {
        JObject jObject = JObject.Parse(message);
        if (jObject == null || jObject[NetworkManager.TYPE].ToString() != NetworkManager.AUDIO)
            return;

        string label = jObject[NetworkManager.PAYLOAD][NetworkManager.LABEL].ToString();
        int userId = int.Parse(label.ToString().Substring(3));

        // 不处理自己的音频（TTS语音和麦克风均已本地播放，回传会造成回声）
        if (userId == GlobalInfo.account.id)
            return;

        // 过滤非房主成员发送的音频帧（仅考试模式）
        if (GlobalInfo.IsExamMode() && !GlobalInfo.IsHomeowner() && userId != GlobalInfo.roomInfo?.creatorId)
            return;

        // IsUserChat客户端侧过滤：直播模式下跳过（服务端已做权限控制，客户端重复校验会误伤TTS语音）
        if (!GlobalInfo.IsLiveMode() && !networkManager.IsUserChat(userId))
            return;
        Debug.Log($"[TTS语音] 接收端: userId={userId} 数据已收到");

        // 确认为用户创建了音频解码器
        if (!clientMicDecoders.ContainsKey(label))
        {
            GameObject newDecoder = Instantiate(audioDecoderPrefab);
            newDecoder.name = $"MicDecoder_{label}";

            AudioDelayControlDecoder micDecoder = newDecoder.GetComponent<AudioDelayControlDecoder>();
            micDecoder.label = int.Parse(label);
            micDecoder.userId = userId;

            clientMicDecoders.Add(label, micDecoder);
        }

        // 输入、播放音频数据
        if (clientMicDecoders[label] != null)
        {
            clientMicDecoders[label].Action_ProcessData(jObject[NetworkManager.PAYLOAD][NetworkManager.DATA].ToObject<byte[]>());
        }
    }


    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)RoomChannelEvent.TalkState:
                if (!GlobalInfo.isAllTalk)
                {
                    ClearRemoteMicDecoders();
                }
                break;
        }
    }

    /// <summary>
    /// 移除指定用户的解码器
    /// </summary>
    /// <param name="userId">用户id</param>
    public void RemoveMicDecoder(int userId)
    {
        string micLable = $"300{userId}";
        if (clientMicDecoders.ContainsKey(micLable))
        {
            Destroy(clientMicDecoders[micLable].gameObject);
            clientMicDecoders.Remove(micLable);
        }
    }

    /// <summary>
    /// 清除用户解码器
    /// </summary>
    public void ClearRemoteMicDecoders()
    {
        // List<AudioDecoder> audioDecoders = clientMicDecoders.Values.ToList();
        List<AudioDelayControlDecoder> audioDecoders = clientMicDecoders.Values.ToList();
        for (int i = 0; i < audioDecoders.Count; i++)
        {
            if (audioDecoders[i])
                Destroy(audioDecoders[i].gameObject);
        }
        clientMicDecoders.Clear();
    }
}
