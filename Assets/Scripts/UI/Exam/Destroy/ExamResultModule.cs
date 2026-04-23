using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityFramework.Runtime;

public class ExamResultModule : UIModuleBase
{
    private RectTransform BackGround;

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        //防止多重弹窗关不掉
        UIManager.Instance.CloseUI<PopupPanel>();

        BackGround = transform.GetComponentByChildName<RectTransform>("BackGround");

        var data = uiData as PanelData;
        {
            this.GetComponentByChildName<Text>("CourseName").text = data.courseName;
            this.GetComponentByChildName<Text>("Score").text = $"{data.score}分";
            this.GetComponentByChildName<Text>("Step").text = $"{data.correct}/{data.wrong + data.correct}";
            this.GetComponentByChildName<Text>("Time").text = data.time.ToString(@"hh\:mm\:ss");
            this.GetComponentByChildName<Text>("Title").text = data.title;

            this.GetComponentByChildName<Button>("Button").onClick.AddListener(() =>
            {
                UIManager.Instance.CloseModuleUI<ExamResultModule>(ParentPanel);
                data.callBack?.Invoke();
            });

            this.GetComponentByChildName<Button>("Button").GetComponentInChildren<Text>().text = data.buttonName;
        }

        Cursor.lockState = CursorLockMode.None;
        ShortcutManager.Instance.enabled = false;
    }
    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        ShortcutManager.Instance.enabled = true;
        base.Close(uiData, callback);
    }

    public class PanelData : UIData
    {
        public string title;
        public string courseName;
        public int score;
        public int correct;
        public int wrong;
        public string buttonName;
        public System.TimeSpan time;
        public System.Action callBack;
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