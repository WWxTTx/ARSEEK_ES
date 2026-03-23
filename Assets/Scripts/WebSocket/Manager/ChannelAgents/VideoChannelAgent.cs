using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityFramework.Runtime;
using Newtonsoft.Json.Linq;
using System.IO;

/// <summary>
/// rtv视频通道代理类
/// </summary>
public class VideoChannelAgent : NetworkChannelAgentBase
{
    [Header("本地采集")]
    public GameViewEncoder localGameViewEncoder;

    private Dictionary<string, GameViewDecoder> clientGameViewDecoders = new Dictionary<string, GameViewDecoder>();

    private string gameViewPrefix = "100";
    private string paintPrefix = "700";

    private int frameIndex;
    private int frameStart = 60;
    public override void InitNetworkChannel()
    {
        networkChannel = new NetworkChannel(ChannelType.rtv);

#if UNITY_ANDROID || UNITY_IOS
        localGameViewEncoder.CaptureMode = GameViewCaptureMode.Rect;
        localGameViewEncoder.RectOffsetPosition = new Vector2(0, Mathf.CeilToInt(96f * ((float)Screen.height / GlobalInfo.CanvasHeight)));
#else
        localGameViewEncoder.CaptureMode = GameViewCaptureMode.Rect;
        localGameViewEncoder.RectOffsetPosition = new Vector2(Mathf.CeilToInt(44f * ((float)Screen.width / GlobalInfo.CanvasWidth)), Mathf.CeilToInt(56f * ((float)Screen.height / GlobalInfo.CanvasHeight)));
#endif
        localGameViewEncoder.Resize = GameViewResize.Half;
        localGameViewEncoder.StreamFPS = 6f;//15f;
        localGameViewEncoder.chunkSize = 65536;//1009 Max frame length of 65536 has been exceeded.
        localGameViewEncoder.FastMode = false;
        localGameViewEncoder.label = int.Parse($"100{GlobalInfo.account.id}");
        localGameViewEncoder.OnDataByteReadyEvent.RemoveAllListeners();
        localGameViewEncoder.OnDataByteReadyEvent.AddListener((byteData) =>
        {
            if (!IsChannelConnected())
                return;
            //退出时byteData可能为null
            if (byteData == null)
                return;

            JObject jObject = new JObject()
            {
                [NetworkManager.TYPE] = NetworkManager.VIDEO,
                [NetworkManager.PAYLOAD] = new JObject
                {
                    //消息是否转发给非房主
                    ["broadcast"]= true,
                    [NetworkManager.LABEL] = localGameViewEncoder.label,
                    [NetworkManager.DATA] = Convert.ToBase64String(byteData) //byteData
                }
            };
            networkChannel.SendAsync(jObject.ToString());
        });
    }

    //写byte[]到fileName
    private void WriteByteToFile(string fileName, byte[] pReadByte)
    {
        FileStream pFileStream = null;
        try
        {
            pFileStream = new FileStream(fileName, FileMode.OpenOrCreate);
            pFileStream.Write(pReadByte, 0, pReadByte.Length);
        }
        catch
        {
            return;
        }
        finally
        {
            if (pFileStream != null)
                pFileStream.Close();
        }
        return;
    }


    protected override void OnChannelOpen()
    {
        base.OnChannelOpen();
        //考核非房主或协同主画面
        networkManager.EnableLocalVideo((GlobalInfo.IsExamMode() && !GlobalInfo.IsHomeowner()) || GlobalInfo.IsMainScreen());
    }

    protected override void OnChannelClosed()
    {
        base.OnChannelClosed();
        networkManager.EnableLocalVideo(false);
        ClearRemoteVideoDecoders(false);
    }

    protected override void OnChannelError()
    {
        base.OnChannelError();
        networkManager.EnableLocalVideo(false);
        ClearRemoteVideoDecoders(false);
    }

    public override void ProcessMessage(string message)
    {
        JObject jObject = JObject.Parse(message);
        if (jObject == null || jObject[NetworkManager.TYPE].ToString() != NetworkManager.VIDEO)
            return;

        string label = jObject[NetworkManager.PAYLOAD][NetworkManager.LABEL].ToString();
        int userId = int.Parse(label.ToString().Substring(3));

        if (label.StartsWith(gameViewPrefix))
        {
            //考试模式非房主不处理视频帧数据
            if (GlobalInfo.IsExamMode() && !GlobalInfo.IsHomeowner())
            {
                return;
            }
            //非考试模式并且有操作权限不接受主屏数据
            if (!GlobalInfo.IsExamMode() && GlobalInfo.IsOperator())
            {
                return;
            }

            if (!clientGameViewDecoders.ContainsKey(label))
            {
                FormMsgManager.Instance.SendMsg(new MsgIntString((ushort)MediaChannelEvent.AddView, userId, label));
            }

            if (clientGameViewDecoders.ContainsKey(label))
            {
                if (clientGameViewDecoders[label] != null)
                {
                    //clientGameViewDecoders[label].Action_ProcessImageData(jObject[NetworkManager.PAYLOAD][NetworkManager.DATA].ToObject<byte[]>());
                    string base64 = jObject[NetworkManager.PAYLOAD][NetworkManager.DATA].ToObject<string>();
                    // 补齐填充 '='
                    switch (base64.Length % 4)
                    {
                        case 2: base64 += "=="; break;
                        case 3: base64 += "="; break;
                    }
                    clientGameViewDecoders[label].Action_ProcessImageData(Convert.FromBase64String(base64));
                }
            }
        }
        //else if (label.StartsWith(paintPrefix))
        //{
        //    FormMsgManager.Instance.SendMsg(new MsgSharedTexture((ushort)SharedPaintModuleEvent.PaintReceived, userId, byteData));
        //}
    }

    public void AddGameViewDecoder(string label, GameViewDecoder gameViewDecoder)
    {
        if (!clientGameViewDecoders.ContainsKey(label))
        {
            clientGameViewDecoders.Add(label, gameViewDecoder);
        }
    }

    public void RemoveGameViewDecoder(int userId, bool destroy)
    {
        string viewLabel = $"100{userId}";
        if (clientGameViewDecoders.ContainsKey(viewLabel))
        {
            if (destroy)
                Destroy(clientGameViewDecoders[viewLabel].gameObject);
            clientGameViewDecoders.Remove(viewLabel);
        }
    }

    public void ClearRemoteVideoDecoders(bool destroy)
    {
        if (destroy)
        {
            List<GameViewDecoder> viewDecoders = clientGameViewDecoders.Values.ToList();
            for (int i = 0; i < viewDecoders.Count; i++)
            {
                if (viewDecoders[i])
                    Destroy(viewDecoders[i].gameObject);
            }
        }
        clientGameViewDecoders.Clear();
    }

    #region 视频帧消息包

    private object packetLock = new object();
    private VideoPacket videoPacket;

    public void UpdateVideoPacket(string url, bool isPlay, float progressValue, int type)
    {
        lock (packetLock)
        {
            videoPacket = new VideoPacket()
            {
                url = url,
                isPlay = isPlay,
                progressValue = progressValue,
                type = type
            };
        }
    }

    public string GetVideoPacket()
    {
        lock (packetLock)
        {
            if (videoPacket == null)
            {
                videoPacket = new VideoPacket();
            }
            videoPacket.baikeIndex = BaikeSelectModule.CurrentBaikeIndex;
            return JsonTool.Serializable(videoPacket);
        }
    }

    public void ClearVideoPacket()
    {
        lock (packetLock)
        {
            videoPacket = null;
        }
    }
    #endregion
}