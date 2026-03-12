using UnityEngine;
using UnityEngine.UI;

public class ExercisesModule_AutonHeight : UnityEngine.EventSystems.UIBehaviour, ILayoutElement
{
    public LayoutGroup heightTarget;

    public float minWidth => 0;
    public float minHeight => 0;
    public float preferredWidth => -1;
    public float preferredHeight => heightTarget?.preferredHeight ?? 0;
    public float flexibleWidth => 1;
    public float flexibleHeight => -1;
    public int layoutPriority => 0;

    private RectTransform rectTransform
    {
        get
        {
            if (_rectTransform == null)
            {
                _rectTransform = transform as RectTransform;
            }

            return _rectTransform;
        }
    }
    private RectTransform _rectTransform;

    public void CalculateLayoutInputHorizontal()
    {
    }
    public void CalculateLayoutInputVertical()
    {
    }
    protected void SetDirty()
    {
        if (isActiveAndEnabled)
        {
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }
    }
    #region ´¥·¢
    protected override void OnEnable()
    {
        base.OnEnable();
        SetDirty();
    }

    protected override void OnDidApplyAnimationProperties()
    {
        SetDirty();
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        SetDirty();
    }

    protected virtual void OnTransformChildrenChanged()
    {
        SetDirty();
    }
#if UNITY_EDITOR
    protected override void OnValidate()
    {
        SetDirty();
    }
#endif
    #endregion
}