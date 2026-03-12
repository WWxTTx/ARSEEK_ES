using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 过渡状态联动、带有音效的Button
/// 可注册禁用状态点击事件
/// </summary>
public class Button_LinkMode : Button, ILinkMode
{
    public bool CanControl => interactable;

    /// <summary>
    /// 子物体联动组件列表
    /// </summary>
    private List<ILinkMode> linkModeComponents = new List<ILinkMode>();

    /// <summary>
    /// 禁用时点击事件
    /// </summary>
    public ButtonClickedEvent OnDisableClick = new ButtonClickedEvent();

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

    /// <summary>
    /// 添加过渡状态联动的Graphic
    /// </summary>
    /// <param name="target"></param>

    public void AddTarget(Graphic target)
    {
        if (target.TryGetComponent(out ILinkMode component))
        {
            if(!linkModeComponents.Contains(component))
                linkModeComponents.Add(component);
        }
    }

    public void AddTargets(List<Graphic> targets)
    {
        foreach(Graphic graphic in targets)
        {
            AddTarget(graphic);
        }
    }

    private void Press()
    {
        if (IsActive() && IsInteractable())
        {
            SoundManager.Instance.PlayEffect(SoundManager.ButtonClick);
            onClick.Invoke();
        }
        else
        {
            OnDisableClick.Invoke();
        }
    }

    public override void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (eventData.button == UnityEngine.EventSystems.PointerEventData.InputButton.Left)
        {
            Press();
        }
    }

#if UNITY_EDITOR
    [MenuItem("CONTEXT/Button/Convert to Button_LinkMode")]
    static void Convert()
    {
        bool interactable;
        Selectable.Transition transition;
        Graphic targetGraphic;
        ColorBlock colorBlock = ColorBlock.defaultColorBlock;
        SpriteState spriteState;
        AnimationTriggers animationTriggers = null;
        Navigation navigation;

        Button button = Selection.activeGameObject.GetComponent<Button>();
        interactable = button.interactable;
        transition = button.transition;
        targetGraphic = button.targetGraphic;
        switch (transition)
        {
            case Selectable.Transition.ColorTint:
                colorBlock = button.colors;
                break;
            case Selectable.Transition.SpriteSwap:
                spriteState = button.spriteState;
                break;
            case Selectable.Transition.Animation:
                animationTriggers = button.animationTriggers;
                break;
        }
        navigation = button.navigation;

        DestroyImmediate(button, true);

        Button_LinkMode button_LinkMode = Selection.activeGameObject.AddComponent<Button_LinkMode>();
        button_LinkMode.interactable = interactable;
        button_LinkMode.transition = transition;
        button_LinkMode.targetGraphic = targetGraphic;
        switch (transition)
        {
            case Selectable.Transition.ColorTint:
                button_LinkMode.colors = colorBlock;
                break;
            case Selectable.Transition.SpriteSwap:
                button_LinkMode.spriteState = spriteState;
                break;
            case Selectable.Transition.Animation:
                button_LinkMode.animationTriggers = animationTriggers;
                break;
        }
        button_LinkMode.navigation = navigation;
    }
#endif
}