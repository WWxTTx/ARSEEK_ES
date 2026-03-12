using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// 悬浮缩放动效
/// </summary>
public class UIScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    /// <summary>
    /// 目标物体
    /// </summary>
    private Transform targetTransform;
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

    public void Init(Transform targetTransform, float enterValue, float exitValue, float animTime = 0.3f)
    {
        this.targetTransform = targetTransform;
        this.enterValue = enterValue;
        this.exitValue = exitValue;
        this.animTime = animTime;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetTransform.DOScale(enterValue * Vector3.one, animTime);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetTransform.DOScale(exitValue * Vector3.one, animTime);
    }
}