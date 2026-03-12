using UnityEngine;

[System.Serializable]
public class RotaryKnobModel : TriggerModel
{
    /// <summary>
    /// 目标物体
    /// </summary>
    public Transform targetObject;
    /// <summary>
    /// 开始点
    /// </summary>
    public Transform startPoint;
    /// <summary>
    /// 结束点
    /// </summary>
    public Transform endPoint;
    /// <summary>
    /// 角度上限
    /// </summary>
    public Vector3 startAngle;
    /// <summary>
    /// 角度下限
    /// </summary>
    public Vector3 endAngle;
    /// <summary>
    /// 档位数量
    /// </summary>
    public int gear;

#if UNITY_EDITOR
    public RotaryKnobModel()
    {
        triggerType = TriggerType.RotaryKnob;
    }
    public override ModelTypeBase Draw()
    {
        targetObject = UnityEditor.EditorGUILayout.ObjectField("向量终点", targetObject, typeof(Transform), true) as Transform;
        startPoint = UnityEditor.EditorGUILayout.ObjectField("向量起点", startPoint, typeof(Transform), true) as Transform;
        endPoint = UnityEditor.EditorGUILayout.ObjectField("向量终点", endPoint, typeof(Transform), true) as Transform;
        startAngle = UnityEditor.EditorGUILayout.Vector3Field("最小角度", startAngle);
        endAngle = UnityEditor.EditorGUILayout.Vector3Field("最大角度", endAngle);
        gear = UnityEditor.EditorGUILayout.IntField("档位", gear);

        return this;
    }
#endif
}