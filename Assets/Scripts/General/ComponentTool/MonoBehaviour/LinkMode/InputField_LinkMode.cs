using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 过渡状态联动的InputField
/// </summary>
public class InputField_LinkMode : InputField, ILinkMode
{
    public bool CanControl => interactable;

    /// <summary>
    /// 子物体联动组件列表
    /// </summary>
    private List<ILinkMode> linkModeComponents = new List<ILinkMode>();

    /// <summary>
    /// 自定义掩码
    /// </summary>
    public string MaskChar;


    protected override void Awake()
    {
        base.Awake();

        foreach (Transform item in transform)
        {
            if (item.TryGetComponent(out ILinkMode component) && component.CanControl)
            {
                linkModeComponents.Add(component);
            }
        }

        if (!string.IsNullOrEmpty(MaskChar))
            this.asteriskChar = MaskChar.ToCharArray()[0];
    }

    public void SetState(int state, bool instant)
    {
        base.DoStateTransition((SelectionState)state, instant);
    }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);

        foreach (ILinkMode c in linkModeComponents)
        {
            c.SetState((int)state, instant);
        }
    }

    /// <summary>
    /// 刷新关联Text
    /// </summary>
    public void RefreshLabel()
    {
        this.UpdateLabel();
    }

#if UNITY_EDITOR
    [MenuItem("CONTEXT/InputField/Convert to InputField_LinkMode")]
    static void Convert()
    {
        bool interactable;
        Selectable.Transition transition;
        Graphic targetGraphic;
        ColorBlock colorBlock = ColorBlock.defaultColorBlock;
        SpriteState spriteState;
        AnimationTriggers animationTriggers = null;
        Navigation navigation;
        Text textCompomemt;
        string text;
        int characterLimit;
        ContentType contentType;
        LineType lineType;
        Graphic placeHolder;
        float caretBlinkRate;
        int caretWidth;
        bool customCaretColor;
        Color caretColor;
        Color selectionColor;
        bool hideMobileInput;
        bool readOnly;
        bool shouldActiveOnSelect;

        InputField inputField = Selection.activeGameObject.GetComponent<InputField>();
        interactable = inputField.interactable;
        transition = inputField.transition;
        targetGraphic = inputField.targetGraphic;
        switch (transition)
        {
            case Selectable.Transition.ColorTint:
                colorBlock = inputField.colors;
                break;
            case Selectable.Transition.SpriteSwap:
                spriteState = inputField.spriteState;
                break;
            case Selectable.Transition.Animation:
                animationTriggers = inputField.animationTriggers;
                break;
        }
        navigation = inputField.navigation;
        textCompomemt = inputField.textComponent;
        text = inputField.text;
        characterLimit = inputField.characterLimit;
        contentType = inputField.contentType;
        lineType = inputField.lineType;
        placeHolder = inputField.placeholder;
        caretBlinkRate = inputField.caretBlinkRate;
        caretWidth = inputField.caretWidth;
        customCaretColor = inputField.customCaretColor;
        caretColor = inputField.caretColor;
        selectionColor = inputField.selectionColor;
        hideMobileInput = inputField.shouldHideMobileInput;
        readOnly = inputField.readOnly;
        shouldActiveOnSelect = inputField.shouldActivateOnSelect;

        Object.DestroyImmediate(inputField, true);

        InputField_LinkMode inputField_LinkMode = Selection.activeGameObject.AddComponent<InputField_LinkMode>();
        inputField_LinkMode.interactable = interactable;
        inputField_LinkMode.targetGraphic = targetGraphic;
        inputField_LinkMode.transition = transition;
        switch (transition)
        {
            case Selectable.Transition.ColorTint:
                inputField_LinkMode.colors = colorBlock;
                break;
            case Selectable.Transition.SpriteSwap:
                inputField_LinkMode.spriteState = spriteState;
                break;
            case Selectable.Transition.Animation:
                inputField_LinkMode.animationTriggers = animationTriggers;
                break;
        }
        inputField_LinkMode.navigation = navigation;
        inputField_LinkMode.textComponent = textCompomemt;
        inputField_LinkMode.text = text;
        inputField_LinkMode.characterLimit = characterLimit;
        inputField_LinkMode.contentType = contentType;
        inputField_LinkMode.lineType = lineType;
        inputField_LinkMode.placeholder = placeHolder;
        inputField_LinkMode.caretBlinkRate = caretBlinkRate;
        inputField_LinkMode.caretWidth = caretWidth;
        inputField_LinkMode.customCaretColor = customCaretColor;
        inputField_LinkMode.caretColor = caretColor;
        inputField_LinkMode.selectionColor = selectionColor;
        inputField_LinkMode.shouldHideMobileInput = hideMobileInput;
        inputField_LinkMode.readOnly = readOnly;
        inputField_LinkMode.shouldActivateOnSelect = shouldActiveOnSelect;
    }
#endif
}