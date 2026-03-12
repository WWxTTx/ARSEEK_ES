using UnityFramework.Runtime;

/// <summary>
/// 常量
/// </summary>
public partial class NetworkManager : Singleton<NetworkManager>, INetworkManager, IRoomAgentHelper, IIMAgentHelper, IFrameAgentHelper, IAudioAgentHelper, IVideoAgentHelper
{
    /// <summary>
    /// 最大静默重连次数
    /// </summary>
    private static int MaxReconnectAttempt = 3;
    /// <summary>
    /// 心跳间隔（毫秒）
    /// </summary>
    private static int HeartIntervalMilis = 15000;//5000
    /// <summary>
    /// 超时
    /// </summary>
    private static int Timeout = 10;
    /// <summary>
    /// 重连延时
    /// </summary>
    private static int ReconnectDelay = 3;

    #region 旧协同服务
    /// <summary>
    /// 设置房间用户为主画面指令同时赋予操作权限
    /// </summary>
    private const string giveMainCmd = "{\"IsControl\":true,\"IsMainScreen\":true}";
    /// <summary>
    /// 取消设置房间用户为主画面指令
    /// </summary>
    private const string takeMainCmd = "{\"IsMainScreen\":false}";
    /// <summary>
    /// 赋予房间用户操作权限指令
    /// </summary>
    private const string giveControlCmd = "{\"IsControl\":true}";
    /// <summary>
    /// 收回房间用户操作权限指令
    /// </summary>
    private const string takeControlCmd = "{\"IsControl\":false,\"IsMainScreen\":false,\"colorNumber\":\"\"}";
    /// <summary>
    /// 取消用户禁言指令（仅房主可发送）
    /// </summary>
    private const string allowedTalkCmd = "{\"IsTalk\":true}";
    /// <summary>
    /// 用户禁言指令（仅房主可发送）
    /// </summary>
    private const string forbidTalkCmd = "{\"IsTalk\":false}";
    /// <summary>
    /// 用户开启麦克风指令
    /// </summary>
    private const string enableChatCmd = "{\"IsChat\":true}";
    /// <summary>
    /// 用户关闭麦克风指令
    /// </summary>
    private const string disableChatCmd = "{\"IsChat\":false}";
    /// <summary>
    /// 用户移出房间指令（房主可踢出其他成员）
    /// </summary>
    private const string outRoomCmd = "{\"IsOut\":true}";

    /// <summary>
    /// 服务器返回的通道关闭消息
    /// </summary>
    public const string quitRoomMsg = "主动退出了直播间";
    public const string kickedMsg = "被迫移除了直播间";
    public const string remoteLoginMsg = "强迫关闭现有连接";
    public const string heartCheckMsg = "心跳检测关闭连接";
    public const string roomDisMsg = "直播间已经被解散";

    public const string joinRoomFailedMsg = "通信地址获取失败。";
    public const string joinRoomNonExistAccountMsg = "该用户未注册。";
    public const string joinRoomWrongPwdMsg = "房间号或密码有误。";

    #endregion

    public const string TYPE = "type";
    public const string PAYLOAD = "payload";
    public const string CODE = "code";
    public const string MESSAGE = "message";
    public const string PING = "ping";
    public const string PONG = "{\"type\":\"pong\"}";
    public const string ERROR = "error";
    public const string DATA = "data";
    public const string AUDIO = "audio";
    public const string VIDEO = "video";
    public const string LABEL = "label";

    #region RTI
    public const string VERSION = "versionCode";
    public const string COMMAND = "command";
    /// <summary>
    /// 交互消息，由客户端发送
    /// </summary>
    public const string OPERATION = "operation";
    /// <summary>
    /// 交互动作消息，由服务端发送，主要转发客户端的交互消息
    /// </summary>
    public const string RTI_ACTION = "rti_action";
    #endregion

    #region RTM
    /// <summary>
    /// 主动关闭消息，由服务端发送
    /// </summary>
    public const string BYE = "Bye!";
    /// <summary>
    /// 房间关闭消息，由服务端发送
    /// </summary>
    public const string ROOM_CLOSE = "room_close";
    /// <summary>
    /// 踢出成员消息，由客户端发送（只能是房主）
    /// </summary>
    public const string EVICT = "evict";
    /// <summary>
    /// 更新房间成员信息消息
    /// </summary>
    public const string PROFILE = "profile";
    /// <summary>
    /// 成员禁言消息 仅房主
    /// </summary>
    public const string SILENT = "silent";
    /// <summary>
    /// 成员取消禁言消息 仅房主
    /// </summary>
    public const string SILENT_OFF = "silent-off";
    /// <summary>
    /// 成员列表消息
    /// </summary>
    public const string MEMBER_LIST = "member_list";
    /// <summary>
    /// 成员进入房间消息
    /// </summary>
    public const string MEMBER_IN = "member_in";
    /// <summary>
    /// 成员离开房间消息
    /// </summary>
    public const string MEMBER_OUT = "member_out";
    /// <summary>
    /// 全员禁言成功消息
    /// </summary>
    public const string SILENT_ALL = "silent-all";
    /// <summary>
    /// 全员取消禁言成功消息
    /// </summary>
    public const string SILENT_OFF_ALL = "silent-off-all";
    #endregion

    #region RTC
    /// <summary>
    /// 消息，由客户端发送
    /// </summary>
    public const string RTC_OPERATION = "rtc_operation";
    #endregion
}
