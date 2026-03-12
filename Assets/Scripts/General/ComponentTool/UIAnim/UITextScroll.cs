using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 文字滚动显示组件
/// 当文字内容超出容器宽度时，自动实现滚动显示效果
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UITextScroll : MonoBehaviour
{
    [Header("滚动设置")]
    [Tooltip("最大可见字符数，超过此数量启用滚动")]
    public int maxVisibleCharacters = 3;

    [Tooltip("滚动速度（像素/秒）")]
    public float scrollSpeed = 30f;

    [Tooltip("滚动前停留时间（秒）")]
    public float delayBeforeScroll = 1f;

    [Tooltip("滚动后停留时间（秒）")]
    public float delayAfterScroll = 1f;

    private Text textComponent;
    private RectTransform rectTransform;
    private RectTransform parentRect;
    private float textWidth;
    private float containerWidth;
    private float scrollDistance;
    private Tweener scrollTweener;
    private Sequence scrollSequence;
    private bool isScrolling;

    private void Awake()
    {
        textComponent = GetComponent<Text>();
        rectTransform = GetComponent<RectTransform>();
        // 设置文本不换行，允许超出显示
        textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
        textComponent.alignment = TextAnchor.MiddleLeft;
    }

    private void OnEnable()
    {
        CheckAndStartScroll();
    }

    private void OnDisable()
    {
        StopScroll();
        if(textComponent.text.Length <= 3)
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxVisibleCharacters * textComponent.fontSize);
    }

    private void OnDestroy()
    {
        StopScroll();
    }

    /// <summary>
    /// 检查文字是否超出容器并启动滚动
    /// </summary>
    public void CheckAndStartScroll()
    {
        if (textComponent == null)
            return;

        // 如果字符数不超过最大可见字符数，不启用滚动
        if (textComponent.text.Length > maxVisibleCharacters)
        {
            textWidth = maxVisibleCharacters * textComponent.fontSize;
            containerWidth = textComponent.text.Length * textComponent.fontSize;

            // 设置文本框宽度为实际文字宽度，确保全部字符可见
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, containerWidth);

            scrollDistance = containerWidth - textWidth;
            StartScrollAnimation();
        }
        else
        {
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    /// <summary>
    /// 启动滚动动画
    /// </summary>
    private void StartScrollAnimation()
    {
        StopScroll();

        isScrolling = true;

        // 计算滚动时间
        float scrollDuration = scrollDistance / scrollSpeed;

        scrollSequence = DOTween.Sequence();
        scrollSequence.SetId(gameObject);
        scrollSequence.AppendInterval(delayBeforeScroll);
        scrollSequence.Append(rectTransform.DOAnchorPosX(-scrollDistance, scrollDuration).SetEase(Ease.Linear));
        scrollSequence.AppendInterval(delayAfterScroll);
        scrollSequence.Append(rectTransform.DOAnchorPosX(0, scrollDuration).SetEase(Ease.Linear));
        scrollSequence.SetLoops(-1, LoopType.Restart);
    }

    /// <summary>
    /// 停止滚动
    /// </summary>
    public void StopScroll()
    {
        if (!isScrolling)
            return;

        isScrolling = false;

        if (scrollTweener != null)
        {
            scrollTweener.Kill();
            scrollTweener = null;
        }

        if (scrollSequence != null)
        {
            scrollSequence.Kill();
            scrollSequence = null;
        }
    }

    /// <summary>
    /// 重置滚动位置
    /// </summary>
    public void ResetPosition()
    {
        StopScroll();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    /// <summary>
    /// 刷新滚动状态（文字内容改变后调用）
    /// </summary>
    public void Refresh()
    {
        StopScroll();
        ResetPosition();
        CheckAndStartScroll();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            Refresh();
        }
    }
#endif
}
