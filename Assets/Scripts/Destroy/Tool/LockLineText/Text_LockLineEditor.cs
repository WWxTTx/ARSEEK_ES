#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
[CustomEditor(typeof(Text_LockLine))]
public class Text_LockLineEditor : TextEditor
{
    private SerializedProperty lineValue;
    protected override void OnEnable()
    {
        base.OnEnable();
        lineValue = serializedObject.FindProperty("Line");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(lineValue);
        serializedObject.ApplyModifiedProperties();
        base.OnInspectorGUI();
    }
}
#endif
