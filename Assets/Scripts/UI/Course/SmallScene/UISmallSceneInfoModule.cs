using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityFramework.Runtime;
using static UnityEngine.GraphicsBuffer;

/// <summary>
/// 录制提示文字
/// </summary>
public class UISmallSceneInfoModule : UIModuleBase
{
    public class UIInfoData : UIData
    {
        public int stepIndex;
        public string stepId;
        public int index;
    }

    private CanvasGroup canvasGroup;

    private RectTransform content;
    private Text text;


    private string no_breaking_space = "\u00A0";

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        canvasGroup = transform.AutoComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        content = transform.GetComponentByChildName<RectTransform>("Content");
        text = transform.GetComponentInChildren<Text>();

        #region 显示配置
        if (SpeechManager.Instance.InfoBackground != null)
            content.GetComponent<Image>().sprite = SpeechManager.Instance.InfoBackground;

        text.font = SpeechManager.Instance.InfoFont;
        text.fontSize = SpeechManager.Instance.InfoFontSize;
        text.color = SpeechManager.Instance.InfoFontColor;
        #endregion

        SpeechManager.Instance.SetTipUI((speechData) =>
        {
            if (speechData != null)
            {
                text.text = speechData.text.Replace(" ", no_breaking_space);
                LayoutRebuilder.ForceRebuildLayoutImmediate(text.rectTransform);
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            }
            canvasGroup.DOFade(1f, SpeechManager.Instance.InfoFadeInTime);
        }, () =>
        {
            canvasGroup.DOFade(0f, SpeechManager.Instance.InfoFadeOutTime);
        });
    }
}