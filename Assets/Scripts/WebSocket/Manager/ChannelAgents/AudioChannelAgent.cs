using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityFramework.Runtime;
using Newtonsoft.Json.Linq;

/// <summary>
/// rta音频通道代理类
/// </summary>
public class AudioChannelAgent : NetworkChannelAgentBase
{
    //[Header("本地采集")]
    //public MicEncoder localMicEncoder;

    /// <summary>
    /// 支持录屏时录制本地麦克风声音的编码器
    /// </summary>
    [Header("本地采集")]
    public MicEncoderWithAudioFilter localMicEncoder;

    [Header("解码器预制体")]
    public GameObject audioDecoderPrefab;

    //private Dictionary<string, AudioDecoder> clientMicDecoders = new Dictionary<string, AudioDecoder>();
    /// <summary>
    /// 支持变速不变调的音频解码器
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

            JObject jObject = new JObject()
            {
                [NetworkManager.TYPE] = NetworkManager.AUDIO,
                [NetworkManager.PAYLOAD] = new JObject
                {
                    [NetworkManager.LABEL] = localMicEncoder.label,
                    [NetworkManager.DATA] = byteData
                }
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

        //考核成员不处理非房主的语音
        if (GlobalInfo.isExam && !GlobalInfo.IsHomeowner() && userId != GlobalInfo.roomInfo?.creatorId)
            return;

        //接收到已离线或闭麦用户的音频帧
        if (!networkManager.IsUserChat(userId))
            return;

        //确保为用户添加音频解码器
        if (!clientMicDecoders.ContainsKey(label))
        {
            GameObject newDecoder = Instantiate(audioDecoderPrefab);
            newDecoder.name = $"MicDecoder_{label}";

            AudioDelayControlDecoder micDecoder = newDecoder.GetComponent<AudioDelayControlDecoder>();
            micDecoder.label = int.Parse(label);
            micDecoder.userId = userId;

            clientMicDecoders.Add(label, micDecoder);
        }

        //解码、播放音频数据
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
    /// 移除指定用户的语音
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
    /// 清空用户语音
    /// </summary>
    public void ClearRemoteMicDecoders()
    {
        //List<AudioDecoder> audioDecoders = clientMicDecoders.Values.ToList();
        List<AudioDelayControlDecoder> audioDecoders = clientMicDecoders.Values.ToList();
        for (int i = 0; i < audioDecoders.Count; i++)
        {
            if (audioDecoders[i])
                Destroy(audioDecoders[i].gameObject);
        }
        clientMicDecoders.Clear();
    }
}