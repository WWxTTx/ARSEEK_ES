using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 过渡状态联动、带有音效的Toggle
/// </summary>
public class Toggle_LinkMode : Toggle, ILinkMode
{
    public bool CanControl => interactable;

    /// <summary>
    /// 子物体联动组件列表
    /// </summary>
    private List<ILinkMode> linkModeComponents = new List<ILinkMode>();

    /// <summary>
    /// Toggle处于on状态时联动控件颜色
    /// </summary>
    public Color SelectColor = Color.white;

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

        if (linkModeComponents?.Count > 0)
        {
            base.onValueChanged.AddListener(SetColorOnValueChanged);
        }
    }

    protected override void Start()
    {
        DoStateTransition(currentSelectionState, true);
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

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (interactable)
        {
            SoundManager.Instance.PlayEffect(SoundManager.ButtonClick);
        }
        base.OnPointerClick(eventData);
    }

    public new void SetIsOnWithoutNotify(bool value)
    {
        SetColorOnValueChanged(value);
        base.SetIsOnWithoutNotify(value);
    }

    /// <summary>
    /// 值变化时，设置子物体控件颜色
    /// </summary>
    /// <param name="isOn"></param>
    private void SetColorOnValueChanged(bool isOn)
    {
        foreach (var c in linkModeComponents)
        {
            if(c is Selectable_LinkMode)
            {
                if (isOn)
                    ((Selectable_LinkMode)c)?.SetColor(SelectColor);
                else
                    ((Selectable_LinkMode)c)?.SetColor();
            }
        }
    }

#if UNITY_EDITOR
    [MenuItem("CONTEXT/Toggle/Convert to Toggle_LinkMode")]
    static void Convert()
    {
        bool interactable;
        Selectable.Transition transition;
        Graphic targetGraphic;
        ColorBlock colorBlock = ColorBlock.defaultColorBlock;
        SpriteState spriteState;
        AnimationTriggers animationTriggers = null;
        Navigation navigation;
        bool isOn;
        Toggle.ToggleTransition toggleTransition;
        Graphic graphic;
        ToggleGroup toggleGroup;

        Toggle toggle = Selection.activeGameObject.GetComponent<Toggle>();
        interactable = toggle.interactable;
        transition = toggle.transition;
        targetGraphic = toggle.targetGraphic;
        switch (transition)
        {
            case Selectable.Transition.ColorTint:
                colorBlock = toggle.colors;
                break;
            case Selectable.Transition.SpriteSwap:
                spriteState = toggle.spriteState;
                break;
            case Selectable.Transition.Animation:
                animationTriggers = toggle.animationTriggers;
                break;
        }
        navigation = toggle.navigation;
        isOn = toggle.isOn;
        toggleTransition = toggle.toggleTransition;
        graphic = toggle.graphic;
        toggleGroup = toggle.group;

        Object.DestroyImmediate(toggle, true);

        Toggle_LinkMode toggle_LinkMode = Selection.activeGameObject.AddComponent<Toggle_LinkMode>();
        toggle_LinkMode.interactable = interactable;
        toggle_LinkMode.targetGraphic = targetGraphic;
        toggle_LinkMode.transition = transition;
        switch (transition)
        {
            case Selectable.Transition.ColorTint:
                toggle_LinkMode.colors = colorBlock;
                break;
            case Selectable.Transition.SpriteSwap:
                toggle_LinkMode.spriteState = spriteState;
                break;
            case Selectable.Transition.Animation:
                toggle_LinkMode.animationTriggers = animationTriggers;
                break;
        }
        toggle_LinkMode.navigation = navigation;
        toggle_LinkMode.isOn = isOn;
        toggle_LinkMode.toggleTransition = toggleTransition;
        toggle_LinkMode.graphic = graphic;
        toggle_LinkMode.group = toggleGroup;
    }
#endif
}