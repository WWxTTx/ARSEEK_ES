using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// 悬浮纵向移动动效
/// </summary>
public class UIMoveVertical : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    /// <summary>
    /// 目标物体
    /// </summary>
    private RectTransform targetTransform;
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

    private string enterFlag;
    private string exitFlag;

    private Sequence enter;
    private Sequence exit;

    public void Init(RectTransform targetTransform, float enterValue, float exitValue, float animTime = 0.3f)
    {
        this.targetTransform = targetTransform;
        this.enterValue = enterValue;
        this.exitValue = exitValue;
        this.animTime = animTime;

        enterFlag = $"{this.GetInstanceID()}_Enter";
        exitFlag = $"{this.GetInstanceID()}_Exit";

        enter = DOTween.Sequence();
        enter.Join(this.targetTransform.DOAnchorPosY(enterValue, animTime));
        enter.SetId(enterFlag).SetAutoKill(false).Pause();

        exit = DOTween.Sequence();
        exit.Join(this.targetTransform.DOAnchorPosY(exitValue, animTime));
        exit.SetId(exitFlag).SetAutoKill(false).Pause();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        DOTween.Pause(exitFlag);
        enter.Restart();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        DOTween.Pause(enterFlag);
        exit.Restart();
    }
}