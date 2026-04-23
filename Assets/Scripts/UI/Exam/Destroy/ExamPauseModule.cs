using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using UnityFramework.Runtime;

/// <summary>e
/// 考核暂停
/// 小组考核，房主异常离线时，避免成员继续操作无法记录成绩
/// </summary>
public class ExamPauseModule : UIModuleBase
{
    private RectTransform BackGround;

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        //防止多重弹窗关不掉
        UIManager.Instance.CloseUI<PopupPanel>();

        BackGround = transform.GetComponentByChildName<RectTransform>("BackGround");

        this.GetComponentByChildName<Button>("Quit").onClick.AddListener(() =>
        {
            (uiData as ModuleData)?.Callback?.Invoke();
        });

        Cursor.lockState = CursorLockMode.None;
        ShortcutManager.Instance.enabled = false;
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {       
        ShortcutManager.Instance.enabled = true;
        base.Close(uiData, callback);
    }

    public class ModuleData : UIData
    {
        public UnityAction Callback;
    }

    #region 动效
    public override void JoinAnim(UnityAction callback)
    {
        BackGround.transform.localScale = Vector3.one * 0.001f;
        CanvasGroup canvasGroup = BackGround.GetComponent<CanvasGroup>();
        JoinSequence.Join(BackGround.transform.DOScale(Vector3.one, JoinAnimePlayTime));
        JoinSequence.Join(DOTween.To(() => 0f, (value) => canvasGroup.alpha = value, 1f, JoinAnimePlayTime));
        base.JoinAnim(callback);
    }

    public override void ExitAnim(UnityAction callback)
    {
        ExitSequence.Join(BackGround.transform.DOScale(Vector3.one * 0.001f, ExitAnimePlayTime));
        CanvasGroup canvasGroup = BackGround.GetComponent<CanvasGroup>();
        ExitSequence.Join(DOTween.To(() => 1f, (value) => canvasGroup.alpha = value, 0f, ExitAnimePlayTime));
        base.ExitAnim(callback);
    }
    #endregion
}