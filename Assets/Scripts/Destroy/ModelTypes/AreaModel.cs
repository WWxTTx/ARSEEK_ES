[System.Serializable]
public class AreaModel : TriggerModel
{
    /// <summary>
    /// 在区域内的时间
    /// </summary>
    public float holdTime = 1;
#if UNITY_EDITOR
    public AreaModel()
    {
        triggerType = TriggerType.Area;
    }
    public override ModelTypeBase Draw()
    {
        holdTime = UnityEditor.EditorGUILayout.FloatField("等待时间", holdTime);
        return this;
    }
#endif
}