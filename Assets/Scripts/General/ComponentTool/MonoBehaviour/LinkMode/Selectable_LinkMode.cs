using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 过渡状态联动的可选择控件
/// </summary>
public class Selectable_LinkMode : Selectable, ILinkMode
{
    public bool CanControl => interactable;

    /// <summary>
    /// 子物体联动组件列表
    /// </summary>
    private List<ILinkMode> linkModeComponents = new List<ILinkMode>();

    /// <summary>
    /// 记录控件初始颜色
    /// </summary>
    private Color saveColor;

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

    public void SetState(int state, bool instant)
    {
        DoStateTransition((SelectionState)state, instant);
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
    /// 设置控件颜色
    /// </summary>
    /// <param name="color"></param>

    public void SetColor(Color? color = null)
    {
        if (saveColor == default)
        {
            saveColor = targetGraphic?.color ?? Color.white;
        }

        targetGraphic.color = color ?? saveColor;
    }
}
