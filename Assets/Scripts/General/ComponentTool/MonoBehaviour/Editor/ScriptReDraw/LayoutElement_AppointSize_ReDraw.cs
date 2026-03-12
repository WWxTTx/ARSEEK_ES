using UnityEditor;
using UnityEditor.UI;

/// <summary>
/// 重绘自定义组件
/// </summary>
[CustomEditor(typeof(LayoutElement_AppointSize), true)]
public class LayoutElement_AppointSize_ReDraw : LayoutElementEditor
{
    private LayoutElement_AppointSize Self;

    protected override void OnEnable()
    {
        base.OnEnable();
        Self = (LayoutElement_AppointSize)target;
    }

    public override void OnInspectorGUI()
    {
        Self.HeightTarget = EditorGUILayout.ObjectField("高度跟随物体", Self.HeightTarget, typeof(UnityEngine.RectTransform), true) as UnityEngine.RectTransform;

        if (Self.HeightTarget != null)
        {
            Self.FlexibleHeight = EditorGUILayout.FloatField("所占高度百分比", Self.FlexibleHeight);
        }

        Self.WidthTarget = EditorGUILayout.ObjectField("宽度跟随物体", Self.WidthTarget, typeof(UnityEngine.RectTransform), true) as UnityEngine.RectTransform;

        if (Self.WidthTarget != null)
        {
            Self.FlexibleWidth = EditorGUILayout.FloatField("所占宽度百分比", Self.FlexibleWidth);
        }

        base.OnInspectorGUI();
    }
}