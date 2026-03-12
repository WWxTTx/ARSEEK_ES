using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityFramework.Runtime;
using Newtonsoft.Json.Linq;

/// <summary>
/// rtc帧同步通道代理类
/// </summary>
public class FrameChannelAgent : NetworkChannelAgentBase
{
    private byte[] dataByte;
    public override void InitNetworkChannel()
    {
        networkChannel = new NetworkChannel(ChannelType.rtc);
    }

    /// <summary>
    /// 发送帧同步数据
    /// </summary>
    /// <param name="byteData"></param>
    public void SendFrame(MsgBase msg)
    {
        //Send(EncodingMsg(msg));
        JObject jObject = new JObject
        {
            [NetworkManager.TYPE] = NetworkManager.RTC_OPERATION,
            [NetworkManager.PAYLOAD] = new JObject
            {
                [NetworkManager.DATA] = JsonTool.Serializable(new MsgBrodcastOperate(msg.msgId, JsonTool.Serializable(msg)))
            }
        };
        networkChannel.SendAsync(jObject.ToString());
    }

    private byte[] EncodingMsg(MsgBase msg)
    {
        dataByte = Encoding.UTF8.GetBytes(JsonTool.Serializable(new MsgBrodcastOperate(msg.msgId, JsonTool.Serializable(msg))));
        return dataByte;
    }

    public override void ProcessMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        JObject jObject = JObject.Parse(message);
        if (jObject == null || jObject[NetworkManager.TYPE].ToString() != NetworkManager.RTC_OPERATION)
            return;

        string datamsg = jObject[NetworkManager.PAYLOAD][NetworkManager.DATA].ToString();
        if (string.IsNullOrEmpty(datamsg))
            return;

        opsReceive.Enqueue(JsonTool.DeSerializable<MsgBrodcastOperate>(datamsg));
    }

    /// <summary>
    /// 收到的操作消息队列
    /// </summary>
    private Queue<MsgBrodcastOperate> opsReceive = new Queue<MsgBrodcastOperate>();

    /// <summary>
    /// 获取已接收未处理的操作消息数量
    /// </summary>
    public int ReceivedOpCount
    {
        get
        {
            return opsReceive.Count;
        }
    }

    private void FixedUpdate()
    {
        if (ReceivedOpCount > 0)
        {
            FormMsgManager.Instance.SendMsg(opsReceive.Dequeue());
        }
    }
}