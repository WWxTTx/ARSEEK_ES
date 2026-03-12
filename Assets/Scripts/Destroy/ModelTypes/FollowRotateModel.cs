using UnityEngine;

[System.Serializable]
public class FollowRotateModel : TriggerModel
{
    /// <summary>
    /// 目标物体
    /// </summary>
    public Transform targetObject;
    /// <summary>
    /// 额外文本用于显示角度
    /// </summary>
    public Transform content;
    /// <summary>
    /// 以哪一个轴旋转
    /// </summary>
    public DragConvertAngle.Axis axis;
    /// <summary>
    /// 是否限制角度
    /// </summary>
    public bool clampAngle = false;
    /// <summary>
    /// 最小角度
    /// </summary>
    public float minAngle;
    /// <summary>
    /// 最大角度
    /// </summary>
    public float maxAngle;
    /// <summary>
    /// 目标角度
    /// </summary>
    public float targetAngle;
    /// <summary>
    /// 跟随动画时长
    /// </summary>
    public float animeTime = 0.5f;

#if UNITY_EDITOR
    public FollowRotateModel()
    {
        triggerType = TriggerType.FollowRotate;
    }
    public override ModelTypeBase Draw()
    {
        targetObject = UnityEditor.EditorGUILayout.ObjectField("目标物体", targetObject, typeof(Transform), true) as Transform;

        content = UnityEditor.EditorGUILayout.ObjectField("3DText(用于显示角度)", content, typeof(Transform), true) as Transform;

        axis = (DragConvertAngle.Axis)UnityEditor.EditorGUILayout.EnumPopup("以哪一个轴旋转", axis);

        clampAngle = UnityEditor.EditorGUILayout.Toggle("是否限制角度", clampAngle);

        if (clampAngle)
        {
            minAngle = UnityEditor.EditorGUILayout.FloatField("最小角度", minAngle);
            maxAngle = UnityEditor.EditorGUILayout.FloatField("最大角度", maxAngle);
        }

        targetAngle = UnityEditor.EditorGUILayout.FloatField("目标角度", targetAngle);

        animeTime = UnityEditor.EditorGUILayout.FloatField("跟随动画时长", animeTime);

        return this;
    }
#endif
}