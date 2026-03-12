using System.Collections.Generic;
using UnityFramework.Runtime;
using static UISmallSceneOperationHistory;

/// <summary>
/// 通道类型
/// </summary>
public enum ChannelType
{
    /// <summary>
    /// 房间通道
    /// </summary>
    rtm,
    /// <summary>
    /// 互动通道
    /// </summary>
    rti,
    /// <summary>
    /// 帧同步通道
    /// </summary>
    rtc,
    /// <summary>
    /// 视频通道
    /// </summary>
    rtv,
    /// <summary>
    /// 音频通道
    /// </summary>
    rta
}

/// <summary>
/// 百科类型
/// </summary>
public enum BaikeType
{
    Editor = -1,
    None,
    Dismantling,
    Anime,
    SmallScene
}

/// <summary>
/// 同步消息包
/// </summary>
public class IMPacket
{
    /// <summary>
    /// 当前操作
    /// </summary>
    public MsgBrodcastOperate data;
    /// <summary>
    /// 状态
    /// </summary>
    public IMState state;
    /// <summary>
    /// 版本号
    /// </summary>
    public int version;
}

/// <summary>
/// 直播状态
/// </summary>
public class IMState
{
    /// <summary>
    /// 操作列表
    /// </summary>
    public List<MsgBrodcastOperate> stateOps;
    /// <summary>
    /// 百科状态
    /// </summary>
    public BaikeState baikeState;
}

/// <summary>
/// 百科状态
/// </summary>
public class BaikeState
{
    /// <summary>
    /// 百科类型
    /// </summary>
    public int baikeType;
    /// <summary>
    /// 状态数据 （BaseBaikeState序列化）
    /// </summary>
    public string data;
}

public class BaseBaikeState { }

/// <summary>
/// 拆解百科状态
/// </summary>
public class DismantlingBaikeState : BaseBaikeState
{
    /// <summary>
    /// 当前拆解层级
    /// </summary>
    public string foldCtrl;
    /// <summary>
    /// 当前用户选择
    /// </summary>
    public Dictionary<string, int> selectModels;
}

/// <summary>
/// 模拟操作百科状态
/// </summary>
public class SmallSceneBaikeState : BaseBaikeState
{
    /// <summary>
    /// 任务id
    /// </summary>
    public int flowIndex;
    /// <summary>
    /// 步骤id
    /// </summary>
    public int stepIndex;
    /// <summary>
    /// 操作道具状态
    /// </summary>
    public List<OpDicData> modelStates;
    /// <summary>
    /// 步骤已执行正确操作
    /// </summary>
    public List<SuccessOpData> successOpDatas;
    /// <summary>
    /// 操作记录
    /// </summary>
    public List<OpRecordData> operations;
    /// <summary>
    /// 仿真系统状态
    /// </summary>
    public string simSystemState;
}


/// <summary>
/// 视频帧消息包(音、视频百科状态)
/// </summary>
public class VideoPacket
{
    /// <summary>
    /// 当前百科索引
    /// </summary>
    public int baikeIndex;
    /// <summary>
    /// 播放地址
    /// </summary>
    public string url = string.Empty;
    /// <summary>
    /// 播放状态 播放true 暂停false
    /// </summary>
    public bool isPlay;
    /// <summary>
    /// 播放进度
    /// </summary>
    public float progressValue;
    /// <summary>
    /// 类型 音频0 视频1
    /// </summary>
    public int type;
}