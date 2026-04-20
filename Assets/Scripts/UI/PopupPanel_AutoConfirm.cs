using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;
using UnityFramework.Runtime;


public class UIAutoPopupData : UIPopupData
{
    /// <summary>
    /// 倒计时
    /// </summary>
    public int countDown;

    /// <summary>
    /// 自动选择确认或取消
    /// </summary>
    public bool autoConfirm;

    public UIAutoPopupData(string title, string info, Dictionary<string, PopupButtonData> buts, int countDown, bool autoConfirm, UnityAction onClose = null, bool showCloseBtn = true) : base(title, info, buts, onClose, showCloseBtn)
    {
        this.countDown = countDown;
        this.autoConfirm = autoConfirm;
    }
}

/// <summary>
/// 倒计时弹窗，自动关闭
/// </summary>

public class PopupPanel_AutoConfirm : UIPanelBase
{
    public override bool Repeatable { get { return true; } }

    /// <summary>
    /// 标题
    /// </summary>
    private Text title;
    /// <summary>
    /// 关闭按钮
    /// </summary>
    private Button CloseButton;

    /// <summary>
    /// 内容
    /// </summary>
    private Text textInfo;

    Button Btn1;
    Button Btn2;
    CanvasGroup Mask;
    CanvasGroup BackGround;
    CanvasGroup Content;

    private List<GameObject> btns;

    private UIAutoPopupData data;

    private string info;
    private int countDown;
    private Button autoBtn;
    private System.Threading.CancellationTokenSource countDownCts;

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        title = transform.GetComponentByChildName<Text>("Title");
        CloseButton = transform.GetComponentByChildName<Button>("CloseBtn");

        textInfo = transform.GetComponentByChildName<Text>("Text");

        Btn1 = transform.GetComponentByChildName<Button>("Btn1");
        Btn2 = transform.GetComponentByChildName<Button>("Btn2");
        Btn1.gameObject.SetActive(false);
        Btn2.gameObject.SetActive(false);

        btns = new List<GameObject>();

        Mask = transform.GetComponentByChildName<CanvasGroup>("Mask");
        BackGround = transform.GetComponentByChildName<CanvasGroup>("BackGround");
        Content = transform.GetComponentByChildName<CanvasGroup>("Content");
        Mask.alpha = 0;
        BackGround.alpha = 0;
        Content.alpha = 0;

        //在Open中调用，避免CheckIsDuplicated返回true的panel重复增加_showPopup的值
        GlobalInfo.ShowPopup = true;
    }

    /// <summary>
    /// 打开，界面显示时调用，之后会调用show
    /// </summary>
    public override void Show(UIData uiData = null)
    {
        if (uiData != null)
        {
            data = uiData as UIAutoPopupData;

            title.text = data.title;

            countDown = data.countDown;
            info = data.info;

            if(countDown > 0)
            {
                textInfo.text = $"{info}\n\n即将关闭: {countDown}";
            }
            else
            {
                textInfo.text = info;
            }

            CloseButton.onClick.AddListener(() =>
            {
                data.onClose?.Invoke();
                UIManager.Instance.CloseUI<PopupPanel_AutoConfirm>(uiData);
            });
            CloseButton.gameObject.SetActive(data.showCloseBtn);

            if (data.btns == null)
                return;

            for (int i = 0; i < btns.Count; i++)
            {
                Destroy(btns[i]);
            }
            autoBtn = null;

            //让确定排在最后
            foreach (var item in Check(ref data.btns))
            {
                Button btnClone;
                if (item.Value.isConfirm)
                {
                    btnClone = Instantiate(Btn2, Btn2.transform.parent);
                    if (countDown > 0 && data.autoConfirm)
                        autoBtn = btnClone;
                }
                else
                {
                    btnClone = Instantiate(Btn1, Btn1.transform.parent);
                    if (countDown > 0 && !data.autoConfirm)
                        autoBtn = btnClone;
                }

                btns.Add(btnClone.gameObject);

                btnClone.gameObject.SetActive(true);
                btnClone.GetComponentInChildren<Text>().text = item.Key;
                LayoutRebuilder.ForceRebuildLayoutImmediate(btnClone.GetComponent<RectTransform>());

                btnClone.onClick.AddListener(() =>
                {
                    item.Value.action?.Invoke();
                    UIManager.Instance.CloseUI<PopupPanel_AutoConfirm>(uiData);
                });
            }
        }

        Cursor.lockState = CursorLockMode.None;
        base.Show(uiData);
    }

    /// <summary>
    /// 控制确定按钮的位置
    /// </summary>
    /// <param name="btns"></param>
    private List<KeyValuePair<string, PopupButtonData>> Check(ref Dictionary<string, PopupButtonData> btns)
    {
        List<KeyValuePair<string, PopupButtonData>> list = new List<KeyValuePair<string, PopupButtonData>>();
        KeyValuePair<string, PopupButtonData> kv;
        bool isConfirm = false;
        foreach (var item in btns)
        {
            if (item.Value.isConfirm)
            {
                kv = item;
                isConfirm = true;
            }
            else
                list.Add(item);
        }

        if (isConfirm)
            list.Add(kv);

        return list;
    }
  
    /// <summary>
    /// 是否重复
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public override bool CheckIsDuplicated(UIData uiData = null)
    {
        if (uiData != null)
        {
            UIAutoPopupData data = uiData as UIAutoPopupData;
            return textInfo.text.StartsWith(data.info);
        }
        return false;
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        GlobalInfo.ShowPopup = false;
        base.Close(uiData, callback);
    }

    #region 动效

    public override void JoinAnim(UnityAction callback)
    {
        SoundManager.Instance.PlayEffect("Popup");
        Mask.alpha = 1f;
        BackGround.transform.localScale = Vector3.one * 0.001f;
        JoinSequence.Append(BackGround.transform.DOScale(Vector3.one, JoinAnimePlayTime));
        JoinSequence.Join(DOTween.To(() => BackGround.alpha, (value) => BackGround.alpha = value, 1f, JoinAnimePlayTime));
        JoinSequence.Join(DOTween.To(() => Content.alpha, (value) => Content.alpha = value, 1f, JoinAnimePlayTime));
        JoinSequence.AppendCallback(() =>
        {
            Content.blocksRaycasts = true;
            if (countDown > 0)
                countDownCts = new System.Threading.CancellationTokenSource();
                CountDown(countDownCts.Token).Forget();
        });
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
        ExitSequence.AppendCallback(() =>
        {
            countDownCts?.Cancel();
            countDownCts = null;
        });
        base.ExitAnim(callback);
    }

    private async UniTaskVoid CountDown(System.Threading.CancellationToken ct)
    {
        while (countDown > 0)
        {
            textInfo.text = $"{info}\n\n即将关闭: {countDown--}";
            await UniTask.Delay(System.TimeSpan.FromSeconds(1f), cancellationToken: ct);

            if(countDown == 0)
            {
                autoBtn?.onClick?.Invoke();
            }
        }
    }
    #endregion
}