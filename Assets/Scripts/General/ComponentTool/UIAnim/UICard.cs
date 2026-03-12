using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// 列表卡片悬浮动效
/// 向上移动3px 加0.4alpha的投影 icon放大20% 用时0.3f
/// </summary>
public class UICard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    /// <summary>
    /// 动画时长
    /// </summary>
    private float animTime = 0.3f;
    /// <summary>
    /// 阴影颜色
    /// </summary>
    private Color color = new Color(0, 0, 0, 0.4f);
    /// <summary>
    /// 图片放大比例
    /// </summary>
    private float scaleRatio = 1.2f;
    /// <summary>
    /// 卡片纵向移动距离
    /// </summary>
    private float verticalOffset = 3f;

    private string enterFlag;
    private string exitFlag;

    private Sequence enter;
    private Sequence exit;

    private RectTransform rectTransform;
    private Shadow shadow;
    private Transform rawImage;

    private void Start()
    {
        enterFlag = $"{transform.GetInstanceID()}_Enter";
        exitFlag = $"{transform.GetInstanceID()}_Exit";

        rectTransform = transform as RectTransform;
        rawImage = transform.GetComponentInChildren<RawImage>()?.transform;

        //添加阴影
        shadow = transform.AutoComponent<Shadow>();
        shadow.effectColor = Color.clear;
        shadow.effectDistance = new Vector2(0, -4);
        shadow.useGraphicAlpha = false;
        shadow.enabled = true;

        RectTransform newParent = new GameObject(transform.name).AutoComponent<RectTransform>();
        //newParent.parent = transform.parent;
        newParent.SetParent(transform.parent, false);
        newParent.position = transform.position;
        newParent.rotation = transform.rotation;
        newParent.localScale = Vector3.one;
        newParent.AutoComponent<Image>().color = Color.clear;
        //transform.parent = newParent.transform;
        transform.SetParent(newParent, false);
        rectTransform.anchorMin = Vector2.one * 0.5f;
        rectTransform.anchorMax = Vector2.one * 0.5f;
        rectTransform.anchoredPosition = Vector2.zero;

        enter = DOTween.Sequence();
        enter.Join(DOTween.To(() => shadow.effectColor, value => shadow.effectColor = value, color, animTime));
        if(rawImage)
            enter.Join(rawImage.DOScale(scaleRatio * Vector3.one, animTime));
        enter.Join(rectTransform.DOAnchorPosY(verticalOffset, animTime));
        enter.SetId(enterFlag).SetAutoKill(false).Pause();

        exit = DOTween.Sequence();
        exit.Join(DOTween.To(() => shadow.effectColor, value => shadow.effectColor = value, Color.clear, animTime));
        if(rawImage)
            exit.Join(rawImage.DOScale(Vector3.one, animTime));
        exit.Join(rectTransform.DOAnchorPosY(0, animTime));
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