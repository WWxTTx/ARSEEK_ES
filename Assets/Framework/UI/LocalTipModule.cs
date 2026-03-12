using UnityEngine;
using UnityFramework.Runtime;
using DG.Tweening;

/// <summary>
/// 模块界面内部提示
/// </summary>
public class LocalTipModule : UIModuleBase
{
    private Sequence sequence;

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);
    }

    public override void Show(UIData uiData = null)
    {
        base.Show(uiData);

        if(uiData != null)
        {
            ModuleData moduleData = uiData as ModuleData;

            GetComponent<RectTransform>().sizeDelta = moduleData.size * Vector2.one;

            GetComponentInChildren<UnityEngine.UI.Text>().text = moduleData.content;

            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            {
                GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                canvasGroup.alpha = 0;

                if (sequence != null)
                    sequence.Kill();

                sequence = DOTween.Sequence();
                {
                    sequence.Append(DOTween.To(() => canvasGroup.alpha, value => canvasGroup.alpha = value, 1, 0.25f));
                    sequence.AppendInterval(1.5f);
                    sequence.Append(DOTween.To(() => canvasGroup.alpha, value => canvasGroup.alpha = value, 0, 0.25f).OnComplete(() =>
                    {
                        UIManager.Instance.CloseModuleUI<LocalTipModule>(ParentPanel);
                    }));
                }
            }
        }   
    }

    public class ModuleData : UIData
    {
        public int size { get; set; }
        public string content { get; set; }
#if UNITY_STANDALONE
        public ModuleData(string content, int size = 180)
#else
        public ModuleData(string content, int size = 360)
#endif
        {
            this.content = content;
            this.size = size;
        }
    }
}
