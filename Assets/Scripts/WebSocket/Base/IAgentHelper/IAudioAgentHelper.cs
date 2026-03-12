using UnityEngine.Events;
using RenderHeads.Media.AVProMovieCapture;

/// <summary>
/// RTA音频通道相关接口
/// </summary>
public interface IAudioAgentHelper
{
    /// <summary>
    /// 请求本地麦克风设备并初始化
    /// 添加为音频源
    /// </summary>
    /// <param name="AudioFromMultipleSources"></param>
    void RequestMicrophone(CaptureAudioFromMultipleSources AudioFromMultipleSources, UnityAction callback);

    /// <summary>
    /// 释放麦克风
    /// </summary>
    void ReleaseMicrophone();

    /// <summary>
    /// 开启关闭本地麦克风
    /// </summary>
    /// <param name="enabled"></param>
    void EnableLocalMic(bool enabled);

    /// <summary>
    /// 移除用户语音
    /// </summary>
    /// <param name="userId"></param>
    void RemoveUserAudio(int userId);
}