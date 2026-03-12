/// <summary>
/// RTV视频通道相关接口
/// </summary>
public interface IVideoAgentHelper
{
    /// <summary>
    /// 开启关闭本地视频
    /// </summary>
    /// <param name="enabled"></param>
    void EnableLocalVideo(bool enabled);

    ///// <summary>
    ///// 发送视频帧数据
    ///// </summary>
    ///// <param name="msg"></param>
    //void SendVideoFrame(byte[] frameData);

    /// <summary>
    /// 添加用户视频解码器
    /// </summary>
    /// <param name="label"></param>
    /// <param name="gameViewDecoder"></param>
    void AddUserVideo(string label, GameViewDecoder gameViewDecoder);

    /// <summary>
    /// 移除用户视频
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="destroy"></param>
    void RemoveUserVideo(int userId, bool destroy);

    /// <summary>
    /// 移除全部用户视频
    /// </summary>  
    /// <param name="destroy"></param>
    void ClearUserVideo(bool destroy);

    /// <summary>
    /// 更新视频帧消息包 
    /// 用于同步音、视频百科的播放状态
    /// </summary>
    /// <param name="url"></param>
    /// <param name="isPlay"></param>
    /// <param name="type"></param>
    /// <param name="progressValue"></param>
    void UpdateVideoPacket(string url, bool isPlay, int type, float progressValue = 0);

    /// <summary>
    /// 获取视频帧消息包
    /// 在GameViewEncoder发送视频帧时调用，随视频帧一同发送
    /// </summary>
    /// <returns></returns>
    string GetVideoPacket();

    /// <summary>
    /// 清除视频帧消息包
    /// 在关闭音、视频百科时调用
    /// </summary>
    /// <returns></returns>
    void ClearVideoPacket();
}