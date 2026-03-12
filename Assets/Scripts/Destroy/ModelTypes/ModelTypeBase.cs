[System.Serializable]
public class ModelTypeBase
{
#if UNITY_EDITOR
    public virtual ModelTypeBase DrawBase()
    {
        return this;
    }
#endif
}

[System.Serializable]
public enum TriggerType
{
    /// <summary>
    /// µã»÷
    /// </summary>
    Click,
    /// <summary>
    /// ´¥Åö
    /// </summary>
    Touch,
    /// <summary>
    /// ÍÏ×§
    /// </summary>
    DragRotate,
    /// <summary>
    /// ÐýÅ¥
    /// </summary>
    RotaryKnob,
    /// <summary>
    /// ÇøÓò
    /// </summary>
    Area,
    /// <summary>
    /// ¸úËæÐý×ª
    /// </summary>
    FollowRotate,
}