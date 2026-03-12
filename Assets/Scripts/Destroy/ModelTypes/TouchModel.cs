[System.Serializable]
public class TouchModel : TriggerModel
{
#if UNITY_EDITOR
    public TouchModel()
    {
        triggerType = TriggerType.Click;
    }
#endif
}