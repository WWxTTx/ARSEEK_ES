using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

/// <summary>
/// 重绘Toggle_LinkMode组件
/// </summary>
[CustomEditor(typeof(Toggle_LinkMode), true)]
public class Toggle_LinkMode_ReDraw : ToggleEditor
{
    private SerializedObject self;
    private SerializedProperty selectColor;

    protected override void OnEnable()
    {
        base.OnEnable();
        self = new SerializedObject(target);
        selectColor = self.FindProperty("SelectColor");
    }

    public override void OnInspectorGUI()
    {
        self.Update();

        EditorGUILayout.PropertyField(selectColor, new GUIContent("isOn 联动控件颜色"));

        self.ApplyModifiedProperties();
        base.OnInspectorGUI();
    }
}