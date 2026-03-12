using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;
using UnityFramework.Runtime;

public class ToastPanelInfo : UIData
{
    public string Message;
    public float ShowTime;

    public string StepId;
    public int OpIndex = -1;

    public ToastPanelInfo() { }
    public ToastPanelInfo(string panelName, float showTime = 1.5f)
    {
        Message = panelName;
        ShowTime = showTime;
    }
}

public class ToastPanel : UIModuleBase
{
    private static int showNum;

    public static int ShowNum { get { return showNum; } }

    public override bool Repeatable { get { return true; } }

    protected Image background;

    /// <summary>
    /// 显示文本
    /// </summary>
    protected Text toastText;

    protected override bool UIMask => false;

    /// <summary>
    /// 显示时长
    /// </summary>
    protected float toastShowTime = 1.5f;

    protected float delayTime;

    protected UIData toastData;

    private string toastMsg;

    /// <summary>
    /// 初始化动画序列
    /// </summary>
    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        showNum += 1;

        background = transform.GetComponentByChildName<Image>("ToastTextBackground");
        toastText = transform.GetComponentByChildName<Text>("ToastText");

        background.SetAlpha(0);
        toastText.SetAlpha(0);

        toastData = uiData;
        ToastPanelInfo data = toastData as ToastPanelInfo;
        SetTextSyncBackground(toastText, data.Message);
        toastMsg = data.Message;

        toastShowTime = data.ShowTime;
        delayTime = (showNum - 1) * (toastShowTime + JoinAnimePlayTime);
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        showNum -= 1;
        base.Close(uiData, callback);
    }

    protected virtual void SetTextSyncBackground(Text targetText, string text)
    {
        RectTransform textParentRectTf = background.GetComponent<RectTransform>();
        if (textParentRectTf == null) return;
        var xOffset = textParentRectTf.sizeDelta.x - targetText.rectTransform.sizeDelta.x;
        var yOffset = textParentRectTf.sizeDelta.y - targetText.rectTransform.sizeDelta.y;

        targetText.text = text;

#if UNITY_ANDROID || UNIYY_IOS
        Vector2 size = new Vector2(Mathf.Clamp(targetText.preferredWidth, 252, 540), targetText.preferredHeight);
        targetText.rectTransform.sizeDelta = size;
        textParentRectTf.sizeDelta = new Vector2(Mathf.Clamp(size.x + xOffset, 440, 718), Mathf.Clamp(targetText.preferredHeight + 94, 100, int.MaxValue));
#else
        Vector2 size = new Vector2(Mathf.Clamp(targetText.preferredWidth, 112, 449), targetText.preferredHeight);//targetText.rectTransform.sizeDelta.y
        targetText.rectTransform.sizeDelta = size;
        textParentRectTf.sizeDelta = new Vector2(Mathf.Clamp(size.x + xOffset, 280, 540), Mathf.Clamp(targetText.preferredHeight + 80, 94, int.MaxValue));//size.y + yOffset
#endif
    }

    /// <summary>
    /// 是否重复
    /// </summary>
    public override bool CheckIsDuplicated(UIData uiData = null)
    {
        if (uiData != null)
        {
            ToastPanelInfo data = uiData as ToastPanelInfo;
            return toastText.text.Equals(data.Message) || toastMsg.Equals(data.Message);
        }
        return false;
    }

    #region 动效
    protected override float joinAnimePlayTime => 0.3f;
    protected override float exitAnimePlayTime => 0.2f;

    public override void JoinAnim(UnityAction callback)
    {
        var openS = DOTween.Sequence();
        openS.AppendInterval(delayTime);
        openS.AppendCallback(() => SoundManager.Instance.PlayEffect("Toast"));
#if UNITY_ANDROID || UNITY_IOS
        openS.Append(background.rectTransform.DOAnchorPosY(-112f, JoinAnimePlayTime));
#else
        openS.Append(background.rectTransform.DOAnchorPosY(-68f, JoinAnimePlayTime));
#endif
        openS.Join(background.DOFade(1, JoinAnimePlayTime));
        openS.Join(toastText.DOFade(1, JoinAnimePlayTime));
        openS.AppendInterval(toastShowTime);
        openS.AppendCallback(() =>
        {
            UIManager.Instance.CloseModuleUI<ToastPanel>(ParentPanel, toastData);
        });
        JoinSequence.Append(openS);
        base.JoinAnim(callback);
    }

    public override void ExitAnim(UnityAction callback)
    {
        ExitSequence.Append(background.rectTransform.DOAnchorPosY(background.rectTransform.sizeDelta.y, ExitAnimePlayTime));
        ExitSequence.Join(background.DOFade(0, ExitAnimePlayTime));
        ExitSequence.Join(toastText.DOFade(0, ExitAnimePlayTime));
        base.ExitAnim(callback);
    }
#endregion
}