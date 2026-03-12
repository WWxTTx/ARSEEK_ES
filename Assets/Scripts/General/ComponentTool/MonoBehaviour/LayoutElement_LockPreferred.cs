/// <summary>
/// 锁定最小size为期望值 不会被挤压
/// </summary>
public class LayoutElement_LockPreferred : UnityEngine.EventSystems.UIBehaviour, UnityEngine.UI.ILayoutElement
{
    public bool LockPreferredWidth;
    public bool LockPreferredHeight;
    private UnityEngine.RectTransform rectTransform
    {
        get
        {
            if(_rectTransform==null)
            {
                _rectTransform = transform as UnityEngine.RectTransform;
            }

            return _rectTransform;
        }
    }
    private UnityEngine.RectTransform _rectTransform;

    public float minWidth => preferredWidth;
    public float minHeight => preferredHeight;
    public float preferredWidth
    {
        get
        {
            if (LockPreferredWidth)
                return GetPreferredValue(0);
            else
                return -1;
        }
    }
    public float preferredHeight
    {
        get
        {
            if (LockPreferredHeight)
                return GetPreferredValue(1);
            else
                return -1;
        }
    }
    public float flexibleWidth => -1;
    public float flexibleHeight => -1;
    public int layoutPriority => 1;

    /// <summary>
    /// 获取最佳值(宽高) image和text也携带ILayoutElement 这个东西的本质就是与layout互动用的
    /// </summary>
    /// <param name="axis"></param>
    /// <returns></returns>
    private float GetPreferredValue(int axis)
    {
        float preferredValue = 0;
        if (axis == 0)
        {
            foreach (var iLayoutElement in GetComponents<UnityEngine.UI.ILayoutElement>())
            {
                if (iLayoutElement.Equals(this))
                    continue;

                if (iLayoutElement.preferredWidth > preferredValue)
                {
                    preferredValue = iLayoutElement.preferredWidth;
                }
            }
        }
        else
        {
            foreach (var iLayoutElement in GetComponents<UnityEngine.UI.ILayoutElement>())
            {
                if (iLayoutElement.Equals(this))
                    continue;

                if (iLayoutElement.preferredHeight > preferredValue)
                {
                    preferredValue = iLayoutElement.preferredHeight;
                }
            }
        }
        return preferredValue;
    }
    protected void SetDirty()
    {
        if (IsActive())
        {
            UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }
    }

    public void CalculateLayoutInputHorizontal() { }
    public void CalculateLayoutInputVertical() { }

    protected override void OnEnable()
    {
        base.OnEnable();
        SetDirty();
    }
    protected override void OnTransformParentChanged()
    {
        SetDirty();
    }
    protected override void OnDisable()
    {
        SetDirty();
        base.OnDisable();
    }
    protected override void OnDidApplyAnimationProperties()
    {
        SetDirty();
    }
    protected override void OnBeforeTransformParentChanged()
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