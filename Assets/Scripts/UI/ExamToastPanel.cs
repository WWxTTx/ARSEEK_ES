using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityFramework.Runtime;

public class ExamToastPanel : ToastPanel
{

    public class PanelData : ToastPanelInfo
    {
        /// <summary>
        /// 按钮提示文字
        /// </summary>
        public string buttonText;
        /// <summary>
        /// 按钮点击事件
        /// </summary>
        public UnityAction buttonOnClick;
        /// <summary>
        /// 倒计时结束事件
        /// </summary>
        public UnityAction timeOutEvent;
        /// <summary>
        /// 时间刷新文字
        /// 参数 当前剩余事件
        /// 回调 当前显示文字
        /// </summary>
        public System.Func<float, string> timeUpdate;
        public PanelData() { }
    }

    public override bool Repeatable => false;
    private PanelData data;
    private Text buttonText;
    private float totalTime;

    public override void Open(UIData uiData = null)
    {
        data = uiData as PanelData;
        totalTime = data.ShowTime;
        buttonText = this.GetComponentByChildName<Text>("ButtonText");
        base.Open(uiData);
        StartCoroutine(UpdateText());
    }

    private IEnumerator UpdateText()
    {
        float currentTime = totalTime + delayTime;
        while (currentTime > 0)
        {
            currentTime -= Time.fixedDeltaTime;
            toastText.text = data.timeUpdate?.Invoke(currentTime);
            yield return new WaitForFixedUpdate();
        }
        data.timeOutEvent.Invoke();
        UIManager.Instance.CloseModuleUI<ExamToastPanel>(ParentPanel, data);
    }

    protected override void SetTextSyncBackground(Text targetText, string text)
    {
        toastText.text = data.Message;
        buttonText.text = data.buttonText;
        buttonText.GetComponent<Button>().onClick.AddListener(() =>
        {
            data.buttonOnClick.Invoke();
            UIManager.Instance.CloseModuleUI<ExamToastPanel>(ParentPanel, data);
        });
    }

    public override void JoinAnim(UnityAction callback)
    {
        buttonText.DOFade(1, JoinAnimePlayTime);
        base.JoinAnim(callback);
    }

    public override void ExitAnim(UnityAction callback)
    {
        buttonText.DOFade(0, JoinAnimePlayTime);
        base.ExitAnim(callback);
    }
}
