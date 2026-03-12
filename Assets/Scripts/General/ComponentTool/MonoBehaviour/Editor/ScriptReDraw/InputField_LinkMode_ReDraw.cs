using UnityEditor;
using UnityEngine.UI;
using UnityEditor.UI;

/// <summary>
/// 重绘InputField_LinkMode组件
/// </summary>
[CustomEditor(typeof(InputField_LinkMode), true)]
public class InputField_LinkMode_ReDraw : InputFieldEditor
{
    private InputField_LinkMode self;
    protected override void OnEnable()
    {
        base.OnEnable();
        self = target as InputField_LinkMode;
    }

    public override void OnInspectorGUI()
    {
        if (self.contentType == InputField.ContentType.Password || self.contentType == InputField.ContentType.Custom)
        {
            self.MaskChar = EditorGUILayout.TextField("自定义掩码", self.MaskChar);
            if (self.MaskChar.Length > 1)
                self.MaskChar = self.MaskChar.Substring(0, 1);
        }
        base.OnInspectorGUI();
    }
}

