using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;
using UnityFramework.Runtime;

public class PopupButtonData
{
    /// <summary>
    /// 区分按钮类型 确认或取消
    /// </summary>
    public bool isConfirm;
    /// <summary>
    /// 事件
    /// </summary>
    public UnityAction action;
    /// <summary>
    /// 按钮能否点击
    /// </summary>
    public bool interactable;
    /// <summary>
    /// 按钮数据
    /// </summary>
    /// <param name="action">按钮触发事件</param>
    /// <param name="isConfirm">区分按钮类型 确认或取消</param>
    /// <param name="interactable">按钮能否点击</param>
    public PopupButtonData(UnityAction action, bool isConfirm = false, bool interactable = true)
    {
        this.isConfirm = isConfirm;
        this.action = action;
        this.interactable = interactable;
    }
}

public class UIPopupData : UIData
{
    /// <summary>
    /// 标题
    /// </summary>
    public string title;
    /// <summary>
    /// 图片
    /// </summary>
    public Texture texture;
    /// <summary>
    /// 内容文本
    /// </summary>
    public string info;

    /// <summary>
    /// 关闭事件
    /// </summary>
    public UnityAction onClose;
    /// <summary>
    /// 是否显示关闭按键
    /// </summary>
    public bool showCloseBtn;

    /// <summary>
    /// 按钮-事件 字典
    /// </summary>
    public Dictionary<string, PopupButtonData> btns;

    public UIPopupData(string title, string info, Dictionary<string, PopupButtonData> btns, UnityAction onClose = null, bool showCloseBtn = true)
    {
        this.title = title;
        this.info = info;
        this.onClose = onClose;
        this.showCloseBtn = showCloseBtn;

        if (btns != null)
            this.btns = btns;
    }

    public UIPopupData(string title, string info, Texture texture, Dictionary<string, PopupButtonData> btns, UnityAction onClose = null, bool showCloseBtn = true)
    {
        this.title = title;
        this.texture = texture;
        this.info = info;
        this.onClose = onClose;
        this.showCloseBtn = showCloseBtn;

        if (btns != null)
            this.btns = btns;
    }
}

public class PopupPanel : UIPanelBase
{
    public override bool Repeatable { get { return true; } }
    /// <summary>
    /// 标题
    /// </summary>
    private Text title;
    /// <summary>
    /// 图片
    /// </summary>
    private GameObject ImageMask;
    private RawImage ShowImage;
    /// <summary>
    /// 内容
    /// </summary>
    private Text textInfo;
    /// <summary>
    /// 关闭按钮
    /// </summary>
    private Button CloseButton;

    Button_LinkMode Btn1;
    Button_LinkMode Btn2;
    CanvasGroup Mask;
    CanvasGroup BackGround;
    CanvasGroup Content;

    private List<GameObject> btns;

    private UIPopupData uiPopupData;

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);
        title = transform.GetComponentByChildName<Text>("Title");
        CloseButton = transform.GetComponentByChildName<Button>("CloseBtn");
        textInfo = transform.GetComponentByChildName<Text>("Text");
        ImageMask = transform.FindChildByName("ImageMask").gameObject;
        ShowImage = ImageMask.GetComponentInChildren<RawImage>();

        Btn1 = transform.GetComponentByChildName<Button_LinkMode>("Btn1");
        Btn2 = transform.GetComponentByChildName<Button_LinkMode>("Btn2");
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
            uiPopupData = uiData as UIPopupData;
            title.text = uiPopupData.title;
            textInfo.text = uiPopupData.info;

            if (uiPopupData.texture)
            {
                ShowImage.texture = uiPopupData.texture;
                ImageMask.SetActive(true);
            }

            CloseButton.onClick.AddListener(() =>
            {
                if (!GlobalInfo.MultiplePopup)
                    Cursor.lockState = GlobalInfo.CursorLockMode;

                uiPopupData.onClose?.Invoke();
                UIManager.Instance.CloseUI<PopupPanel>(uiData);
            });
            CloseButton.gameObject.SetActive(uiPopupData.showCloseBtn);

            for (int i = 0; i < btns.Count; i++)
            {
                Destroy(btns[i]);
            }
            if (uiPopupData.btns != null)
            {
                //让确定排在最后
                foreach (var item in Check(ref uiPopupData.btns))
                {
                    Button btnClone;
                    if (item.Value.isConfirm)
                        btnClone = Instantiate(Btn2, Btn2.transform.parent);
                    else
                        btnClone = Instantiate(Btn1, Btn1.transform.parent);
                    btns.Add(btnClone.gameObject);

                    btnClone.gameObject.SetActive(true);
                    btnClone.GetComponentInChildren<Text>().text = item.Key;
                    LayoutRebuilder.ForceRebuildLayoutImmediate(btnClone.GetComponent<RectTransform>());

                    btnClone.onClick.AddListener(() =>
                    {
                        if(!GlobalInfo.MultiplePopup)
                            Cursor.lockState = GlobalInfo.CursorLockMode;

                        item.Value.action?.Invoke();
                        UIManager.Instance.CloseUI<PopupPanel>(uiPopupData);
                    });
                    btnClone.interactable = item.Value.interactable;
                }
            }
        }

        Cursor.lockState = CursorLockMode.None;
        //弹窗时禁用快捷键
        ShortcutManager.Instance.enabled = false;
        //左右两个按钮大小按照最大的来
        if (btns.Count > 1)
        {
            RectTransform temp;
            var max = Vector2.zero;

            foreach (var btn in btns)
            {
                if (btn == null)
                    continue;
                temp = btn.GetComponent<RectTransform>();
                LayoutRebuilder.ForceRebuildLayoutImmediate(temp);

                if (max.x < temp.sizeDelta.x)
                {
                    max = temp.sizeDelta;
                }

                temp.GetComponent<ContentSizeFitter>().enabled = false;
                btn.GetComponent<RectTransform>().sizeDelta = max;
            }
        }

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

    //避免和设置快捷键冲突
    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Escape))
    //    {
    //        Cursor.lockState = GlobalInfo.CursorLockMode;
    //        UIManager.Instance.CloseUI<PopupPanel>(uiPopupData);
    //    }
    //}

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        //弹窗时禁用快捷键
        ShortcutManager.Instance.enabled = true;
        GlobalInfo.ShowPopup = false;
        base.Close(uiData, callback);
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
            UIPopupData data = uiData as UIPopupData;
            //return textInfo.text.Equals(data.info);
            return textInfo.text.Contains(data.info);
        }
        return false;
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