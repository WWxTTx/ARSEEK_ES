using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 可同步交互状态\检测单双击\带有音效的Button
/// </summary>
public class Button_LinkModeExtend : Button, ILinkMode
{
    /// <summary>
    /// 禁用交互时点击事件
    /// </summary>
    public ButtonClickedEvent onDisableClick = new ButtonClickedEvent();

    public bool CanControl => interactable;

    public Graphic[] targetGraphics;


    protected override void Start()
    {
        //第二瞬间刷新UI状态
        DoStateTransition(currentSelectionState, true);
    }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);

        foreach (UnityEngine.Transform item in transform)
            if (item.TryGetComponent(out ILinkMode component))
                if (component.CanControl)
                    component.SetState((int)state, instant);

        if (targetGraphics != null)
        {
            foreach (Graphic item in targetGraphics)
                if (item.TryGetComponent(out ILinkMode component))
                    if (component.CanControl)
                        component.SetState((int)state, instant);
        }
    }

    public void SetState(int state, bool instant)
    {
        base.DoStateTransition((SelectionState)state, instant);
    }

    /// <summary>
    /// 添加交互状态联动Graphic
    /// </summary>
    /// <param name="target"></param>

    public void AddTarget(Graphic target)
    {
        targetGraphics = new List<Graphic>() { target }.ToArray();
    }
    public void AddTargets(List<Graphic> targets)
    {
        targetGraphics = targets.ToArray();
    }

    #region 仅单击
    //private void Press()
    //{
    //    if (IsActive() && IsInteractable())
    //    {
    //        onClick.Invoke();
    //    }
    //    else
    //    {
    //        onDisableClick.Invoke();
    //    }
    //}

    //public override void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
    //{
    //    if (eventData.button == UnityEngine.EventSystems.PointerEventData.InputButton.Left)
    //    {
    //        Press();
    //    }
    //}
    #endregion

    #region 单双击
    /// <summary>
    /// 双击事件
    /// </summary>
    public ButtonClickedEvent onDoubleClick = new ButtonClickedEvent();

    private bool _ignoreDoubleClick = true;
    public bool IgnoreDoubleClick
    {
        get { return _ignoreDoubleClick; }
        set
        {
            _ignoreDoubleClick = value;
            if (_ignoreDoubleClick)
                DoubleClickInterval = 0f;
        }
    }

    private float _doubleClickInterval = 0.35f;
    public float DoubleClickInterval { get { return _doubleClickInterval; } set { _doubleClickInterval = value; } }

    float lastTimeClick;
    bool isPointerDown = false;
    bool single = false;
    bool first = true;

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (!IgnoreDoubleClick && eventData.button == PointerEventData.InputButton.Left)
            isPointerDown = true;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (!IgnoreDoubleClick && isPointerDown)
        {
            //if (IgnoreDoubleClick)
            //{
            //    single = true;
            //}
            //else
            //{
                single = !single;

                if (!first) return;
                first = false;
            //}
            Invoke(nameof(Press), DoubleClickInterval);
        }
    }

    public override void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (IgnoreDoubleClick && eventData.button == UnityEngine.EventSystems.PointerEventData.InputButton.Left)
        {
            Press();
        }
    }

    private void Press()
    {
        if (IsActive() && IsInteractable())
        {
            SoundManager.Instance.PlayEffect(SoundManager.ButtonClick);
            if (IgnoreDoubleClick || single)
            {
                onClick?.Invoke();
            }
            else
            {
                onDoubleClick?.Invoke();
            }
            single = false;
            first = true;
            isPointerDown = false;
        }
        else
        {
            onDisableClick.Invoke();
        }
    }
    #endregion

    #region 右键
    ///// <summary>
    ///// 右键单击事件
    ///// </summary>
    //public ButtonClickedEvent onRightClick = new ButtonClickedEvent();

    //private void Press()
    //{
    //    if (IsActive() && IsInteractable())
    //    {
    //        onClick?.Invoke();
    //    }
    //    else
    //    {
    //        onDisableClick.Invoke();
    //    }
    //}

    //public override void OnPointerClick(PointerEventData eventData)
    //{
    //    if (eventData.button == PointerEventData.InputButton.Right)
    //    {
    //        onRightClick?.Invoke();
    //    }
    //    else if (eventData.button == PointerEventData.InputButton.Left)
    //    {
    //        Press();
    //    }
    //}
    #endregion
}