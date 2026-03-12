using System.Collections.Generic;
using static UnityFramework.Runtime.ServiceRequestData;

/// <summary>
/// RTM房间通道相关接口
/// </summary>
public interface IRoomAgentHelper
{
    /// <summary>
    /// 将成员踢除房间
    /// </summary>
    /// <param name="id">传入本人id离开房间；房主可传入其他用户id将其踢除房间</param>
    void KickOutUser(int id);

    /// <summary>
    /// 控制全员禁言 （仅房主）
    /// </summary>
    /// <param name="allowTalk"></param>
    void SilentAllMember(bool allowTalk);

    /// <summary>
    /// 控制用户语音开关 （仅房主）
    /// </summary>
    /// <param name="id"></param>
    void SwitchUserTalk(int id);

    /// <summary>
    /// 控制用户麦克风开关
    /// </summary>
    /// <param name="id"></param>
    void SwitchUserChat(int id);

    /// <summary>
    /// 设置用户操作权限
    /// </summary>
    /// <param name="id"></param>
    /// <param name="give"></param>
    void SetUserControl(int id, bool give);

    /// <summary>
    /// 设置用户主画面
    /// </summary>
    /// <param name="id"></param>
    /// <param name="give"></param>
    void SetUserMainView(int id, bool give);

    /// <summary>
    /// 设置用户颜色
    /// </summary>
    /// <param name="id"></param>
    void SetUserColor(int id);

    /// <summary>
    /// 用户是否在线
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    bool IsUserOnline(int id);

    /// <summary>
    /// 用户是否开麦
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    bool IsUserChat(int id);

    /// <summary>
    /// 获取用户昵称
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    string GetUserName(int id);

    /// <summary>
    /// 获取用户设备类型
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    string GetUserDevice(int id);

    /// <summary>
    /// 判断当前端是否为房间列表中第一个在线用户
    /// </summary>
    /// <returns></returns>
    bool IsFirstActiveUser();

    /// <summary>
    /// 获取房间成员列表
    /// </summary>
    /// <returns></returns>
    List<Member> GetRoomMemberList();

    /// <summary>
    /// 获取房间成员人数
    /// </summary>
    /// <returns></returns>
    int GetRoomMemberCount();

    /// <summary>
    /// 获取随机房间成员列表
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    List<Member> GetRandomMemberList(int count);
}