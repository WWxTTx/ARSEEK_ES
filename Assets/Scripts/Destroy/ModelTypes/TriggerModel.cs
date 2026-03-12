[System.Serializable]
public class TriggerModel : ModelTypeBase
{
    /// <summary>
    /// 交互方式
    /// </summary>
    public TriggerType triggerType = TriggerType.Click;
    /// <summary>
    /// 高亮节点
    /// </summary>
    public UnityEngine.Transform highlightNode;

#if UNITY_EDITOR
    public TriggerModel(TriggerType triggerType = TriggerType.Click)
    {
        this.triggerType = triggerType;
    }

    public override ModelTypeBase DrawBase()
    {
        highlightNode = UnityEditor.EditorGUILayout.ObjectField("高亮节点", highlightNode, typeof(UnityEngine.Transform), true) as UnityEngine.Transform;

        UnityEditor.EditorGUI.BeginChangeCheck();

        triggerType = (TriggerType)UnityEditor.EditorGUILayout.EnumPopup("触发方式", (TriggerType_ReDraw)triggerType);

        if (UnityEditor.EditorGUI.EndChangeCheck())
        {
            return System.Activator.CreateInstance(System.Type.GetType($"{System.Enum.GetName(typeof(TriggerType), triggerType)}Model")) as ModelTypeBase;
        }
        else
        {
            return Draw();
        }
    }

    public virtual ModelTypeBase Draw()
    {
        return this;
    }

    private enum TriggerType_ReDraw
    {
        点击 = TriggerType.Click,
        触碰 = TriggerType.Touch,
        拖拽 = TriggerType.DragRotate,
        旋钮 = TriggerType.RotaryKnob,
        区域 = TriggerType.Area,
        跟随旋转 = TriggerType.FollowRotate,
    }
#endif
}