using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 可以指定大小的布局 
/// </summary>
public class LayoutElement_AppointSize : LayoutElement
{
    /// <summary>
    /// 从指定物体上获取期望高度
    /// </summary>
    public RectTransform HeightTarget;
    /// <summary>
    /// 从指定物体上获取期望宽度
    /// </summary>
    public RectTransform WidthTarget;
    /// <summary>
    /// 高度百分比
    /// </summary>
    public float FlexibleHeight = 1;
    /// <summary>
    /// 宽度百分比
    /// </summary>
    public float FlexibleWidth = 1;

    /// <summary>
    /// 自身
    /// </summary>
    private RectTransform rectTransform;
    /// <summary>
    /// 最后一次获取到的高度
    /// </summary>
    private float LastHeight;
    /// <summary>
    /// 最后一次获取到的宽度
    /// </summary>
    private float LastWidth;

    public override float minHeight
    {
        get
        {
            if (HeightTarget)
            {
                LastHeight = LayoutUtility.GetPreferredHeight(HeightTarget) * FlexibleHeight;
                return LastHeight;
            }
            else
            {
                LastHeight = base.minHeight;
                return LastHeight;
            }
        }
    }
    public override float minWidth
    {
        get
        {
            if (WidthTarget)
            {
                LastWidth = LayoutUtility.GetPreferredWidth(WidthTarget) * FlexibleWidth;
                return LastWidth;
            }
            else
            {
                LastWidth = base.minWidth;
                return LastWidth;
            }
        }
    }

    protected override void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        base.Awake();
    }

    [ContextMenu("手动刷新")]
    public void Refresh()
    {
        if (IsActive())
        {
            if (LastHeight == LayoutUtility.GetPreferredHeight(HeightTarget) && LastWidth == LayoutUtility.GetPreferredWidth(WidthTarget))
                return;

            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }
    }

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

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        SetDirty();
    }
#endif
}
