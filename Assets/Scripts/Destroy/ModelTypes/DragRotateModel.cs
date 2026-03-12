using UnityEngine;

[System.Serializable]
public class DragRotateModel : TriggerModel
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
    /// 最终角度
    /// </summary>
    public float targetAngle;

#if UNITY_EDITOR
    public DragRotateModel()
    {
        triggerType = TriggerType.DragRotate;
    }
    public override ModelTypeBase Draw( )
    {
        targetObject = UnityEditor.EditorGUILayout.ObjectField("向量终点", targetObject, typeof(Transform), true) as Transform;
        startPoint = UnityEditor.EditorGUILayout.ObjectField("向量起点", startPoint, typeof(Transform), true) as Transform;
        endPoint = UnityEditor.EditorGUILayout.ObjectField("向量终点", endPoint, typeof(Transform), true) as Transform;
        startAngle = UnityEditor.EditorGUILayout.Vector3Field("最小角度", startAngle);
        endAngle = UnityEditor.EditorGUILayout.Vector3Field("最大角度", endAngle);
        targetAngle = UnityEditor.EditorGUILayout.FloatField("目标角度百分比(0-1)", targetAngle);

        return this;
    }
#endif
}