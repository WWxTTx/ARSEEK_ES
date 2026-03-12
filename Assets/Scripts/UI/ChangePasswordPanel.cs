using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using UnityFramework.Runtime;

/// <summary>
/// –ﬁ∏ƒ√‹¬Î 
/// æ…√‹¬Î+–¬√‹¬Î
/// </summary>
public class ChangePasswordPanel : UIPanelBase
{
    public class PanelData : UIData
    {
        public System.Action<string, string> callBack;
    }

    CanvasGroup Mask;
    CanvasGroup BackGround;
    CanvasGroup Content;

    private Button_LinkMode NextBtn;
    private Button_LinkMode EnterBtn;

    private GameObject Old;
    private GameObject New;
    private GameObject Error;

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        Mask = transform.GetComponentByChildName<CanvasGroup>("Mask");
        BackGround = transform.GetComponentByChildName<CanvasGroup>("BackGround");
        Content = transform.GetComponentByChildName<CanvasGroup>("Content");
        Mask.alpha = 0;
        BackGround.alpha = 0;
        Content.alpha = 0;

        NextBtn = this.GetComponentByChildName<Button_LinkMode>("Next");
        EnterBtn = this.GetComponentByChildName<Button_LinkMode>("Enter");
        Old = this.FindChildByName("Old").gameObject;
        New = this.FindChildByName("New").gameObject;
        Error = this.FindChildByName("Error").gameObject;

        var data = uiData as PanelData;
        {
            this.GetComponentByChildName<Button>("Cancel").onClick.AddListener(() =>
            {
                UIManager.Instance.CloseUI<ChangePasswordPanel>();
            });

            var oldInputField = this.GetComponentByChildName<InputField>("OldPassword");
            {
                oldInputField.onValueChanged.AddListener(content =>
                {
                    Error.SetActive(false);
                    NextBtn.interactable = oldInputField.text.Length >= 6;
                });
            }

            var newInputField = this.GetComponentByChildName<InputField>("NewPassword");
            {
                newInputField.onValueChanged.AddListener(content =>
                {
                    EnterBtn.interactable = newInputField.text.Length >= 6;
                });
            }

            NextBtn.onClick.AddListener(() =>
            {
                if (!oldInputField.text.Equals(PlayerPrefs.GetString(GlobalInfo.passwordCacheKey)))
                {
                    Error.SetActive(true);
                }
                else
                {
                    Error.SetActive(false);
                    Old.SetActive(false);
                    New.SetActive(true);
                }
            });

            EnterBtn.onClick.AddListener(() =>
            {
                data.callBack?.Invoke(oldInputField.text, newInputField.text);
                UIManager.Instance.CloseUI<ChangePasswordPanel>();
            });
        }
    }

    #region ∂Ø–ß
    public override void JoinAnim(UnityAction callback)
    {
        SoundManager.Instance.PlayEffect("Popup");
        Mask.alpha = 1f;
        BackGround.transform.localScale = Vector3.one * 0.001f;
        JoinSequence.Append(BackGround.transform.DOScale(Vector3.one, JoinAnimePlayTime));
        JoinSequence.Join(DOTween.To(() => BackGround.alpha, (value) => BackGround.alpha = value, 1f, JoinAnimePlayTime));
        JoinSequence.Join(DOTween.To(() => Content.alpha, (value) => Content.alpha = value, 1f, JoinAnimePlayTime));
        JoinSequence.AppendCallback(() => Content.blocksRaycasts = true);
        base.JoinAnim(callback);
    }

    public override void ExitAnim(UnityAction callback)
    {
        Content.blocksRaycasts = false;
        Content.alpha = 0f;
        BackGround.transform.localScale = Vector3.one;
        ExitSequence.Append(BackGround.transform.DOScale(Vector3.one * 0.001f, ExitAnimePlayTime));
        ExitSequence.Join(DOTween.To(() => BackGround.alpha, (value) => BackGround.alpha = value, 0f, ExitAnimePlayTime));
        ExitSequence.Join(DOTween.To(() => Mask.alpha, (value) => Mask.alpha = value, 0f, ExitAnimePlayTime));
        base.ExitAnim(callback);
    }
    #endregion
}