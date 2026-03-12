using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// 悬浮透明度变化
/// </summary>
public class UIFade : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    /// <summary>
    /// 目标物体
    /// </summary>
    private Graphic targetGraphic;
    /// <summary>
    /// 鼠标悬停时变化目标值
    /// </summary>
    private float enterValue;
    /// <summary>
    /// 鼠标移出时变化目标值
    /// </summary>
    private float exitValue;
    /// <summary>
    /// 动画时长
    /// </summary>
    private float animTime;

    public delegate bool OnPointerCondition();

    public OnPointerCondition OnPointerEnterCondition;

    public OnPointerCondition OnPointerExitCondition;

    public void Init(Graphic targetGraphic, float enterValue, float exitValue, float animTime = 0.3f)
    {
        this.targetGraphic = targetGraphic;
        this.enterValue = enterValue;
        this.exitValue = exitValue;
        this.animTime = animTime;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (OnPointerEnterCondition != null && OnPointerEnterCondition.Invoke())
            return;
        targetGraphic.DOFade(enterValue, animTime);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (OnPointerExitCondition != null && OnPointerExitCondition.Invoke())
            return;
        targetGraphic.DOFade(exitValue, animTime);
    }
}