[System.Serializable]
public class ClickModel : TriggerModel
{
#if UNITY_EDITOR
    public ClickModel()
    {
        triggerType = TriggerType.Click;
    }
#endif
}