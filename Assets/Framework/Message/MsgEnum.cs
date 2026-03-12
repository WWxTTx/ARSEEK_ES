namespace UnityFramework.Runtime
{
    public enum DebugEvent
    {
        Min = 0,
        RefreshLog,
        AddLog,
        SaveLog,
        Max,
    }

    public enum LoadingPanelEvent
    {
        Min = DebugEvent.Max + 1,
        ProgressValue,
        Max,
    }

    /// <summary>
    /// 动画遮罩 用于播放动画时遮挡界面
    /// </summary>
    public enum UIAnimEvent
    {
        Min = LoadingPanelEvent.Max + 1,
        ShowAnimMask,
        HideAnimMask,
        Max
    }
}