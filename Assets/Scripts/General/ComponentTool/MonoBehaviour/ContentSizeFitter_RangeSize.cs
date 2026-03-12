using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 限定范围内自适应
/// </summary>
public class ContentSizeFitter_RangeSize : UIBehaviour, ILayoutSelfController, ILayoutController
{
    public RectTransform HorizontalTarget;
    public bool HorizontalControl;
    [Range(0, 1)]
    public float HorizontalMax;
    [Range(0, 1)]
    public float HorizontalMin;
    public float HorizontalMaxValue;
    public float HorizontalMinValue;

    public RectTransform VerticalTarget;
    public bool VerticalControl;
    [Range(0, 1)]
    public float VerticalMax;
    [Range(0, 1)]
    public float VerticalMin;
    public float VerticalMaxValue;
    public float VerticalMinValue;
    private RectTransform rectTransform
    {
        get
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            return _rectTransform;
        }
    }
    private RectTransform _rectTransform;

    protected override void OnEnable()
    {
        base.OnEnable();
        SetDirty();
    }

    protected override void OnDisable()
    {
        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        base.OnDisable();
    }

    protected override void OnRectTransformDimensionsChange()
    {
        SetDirty();
    }

    public virtual void SetLayoutHorizontal()
    {
        if (HorizontalControl)
        {
            var max = HorizontalMaxValue > 0 ? HorizontalMaxValue : HorizontalMax * HorizontalTarget.rect.width;
            var min = HorizontalMinValue > 0 ? HorizontalMinValue : HorizontalMin * HorizontalTarget.rect.width;
            
            rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)0, Mathf.Clamp(LayoutUtility.GetPreferredSize(_rectTransform, 0), min, max));
        }
    }

    public virtual void SetLayoutVertical()
    {
        if (VerticalControl)
        {
            var max = VerticalMaxValue > 0 ? VerticalMaxValue : VerticalMax * VerticalTarget.rect.width;
            var min = VerticalMinValue > 0 ? VerticalMinValue : VerticalMin * VerticalTarget.rect.width;

            rectTransform.SetSizeWithCurrentAnchors((RectTransform.Axis)1, Mathf.Clamp(LayoutUtility.GetPreferredSize(_rectTransform, 1), min, max));
        }
    }

    protected void SetDirty()
    {
        if (IsActive())
        {
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        SetDirty();
    }
#endif
}
