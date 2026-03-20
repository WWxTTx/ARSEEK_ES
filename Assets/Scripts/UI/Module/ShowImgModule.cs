using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;
using UnityFramework.Runtime;

/// <summary>
/// 查看图片模块
/// </summary>
public class ShowImgModule : UIModuleBase
{
    private Canvas canvas;
    private CanvasGroup CanvasGroup;
    private GameObject Background;
    private GameObject ShowImg;
    private RawImage AutoShow;

    private int id;
    /// <summary>
    /// 是否显示关闭按钮
    /// </summary>
    private bool showCloseBtn;

    public ShowLinkModuleData ImgModuleData { get; private set; }

    public override bool Repeatable { get { return true; } }

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        AddMsg(new ushort[]
        {
            (ushort)CoursePanelEvent.Option,
#if UNITY_ANDROID || UNITY_IOS
            (ushort)ARModuleEvent.Tracking,
#endif
        });

        Background = transform.FindChildByName("Background").gameObject;
        ShowImg = transform.FindChildByName("ShowImg")?.gameObject;
        AutoShow = transform.GetComponentByChildName<RawImage>("AutoShow");

        CanvasGroup = GetComponent<CanvasGroup>();
        CanvasGroup.interactable = !GlobalInfo.IsLiveMode() || GlobalInfo.IsOperator();

        ImgModuleData = (ShowLinkModuleData)uiData;
        id = ImgModuleData.id;
        showCloseBtn = ImgModuleData.showClose;

        if (showCloseBtn)
        {
            var Close = this.GetComponentByChildName<Button>("Close");
            Close.onClick.AddListener(() =>
            {
                if (ImgModuleData.closedAction != null)
                    ImgModuleData.closedAction();
                else
                    UIManager.Instance.CloseModuleUI<ShowImgModule>();
            });

            Close.gameObject.SetActive(true);
            Background.SetActive(true);
        }

        LoadImage(ImgModuleData.id, ResManager.Instance.OSSDownLoadPath + ImgModuleData.url);
    }

    public override void Show(UIData uiData = null)
    {
        base.Show(uiData);

        if (GlobalInfo.InEditMode)
        {
            canvas = transform.AutoComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 2;
            transform.AutoComponent<GraphicRaycaster>();
        }
    }

    private void LoadImage(int pointId, string url)
    {
        ResManager.Instance.LoadKnowledgepointImage(pointId.ToString(), url, (arg1) =>
        {
            if (arg1 == null)
            {
                ShowImg.SetActive(false);
                UIManager.Instance.OpenModuleUI<LocalTipModule_Button>(ParentPanel, ShowImg.transform.parent,
                    new LocalTipModule_Button.ModuleData("图片加载失败", "刷新", () => LoadImage(pointId, url), 1));
                return;
            }

            if (ShowImg == null)
                return;

            ShowImg.SetActive(true);
            AutoShow.gameObject.SetActive(true);
            AutoShow.texture = arg1;
            AspectRatioFitter aspectRatioFitter = AutoShow.AutoComponent<AspectRatioFitter>();
            aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspectRatioFitter.aspectRatio = (float)arg1.width / arg1.height;
        });
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        Resources.UnloadUnusedAssets();
        base.Close(uiData, callback);
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);

        switch (msg.msgId)
        {
#if UNITY_ANDROID || UNITY_IOS
            case (ushort)ARModuleEvent.Tracking:
                bool tracking = ((MsgBool)msg).arg1;
                CanvasGroup.blocksRaycasts = !tracking;
                CanvasGroup.DOFade(tracking ? 0 : 1, 0.3f);
                break;
#endif
            case (ushort)CoursePanelEvent.Option:
                bool open = ((MsgBool)msg).arg1;
                if (open)
                    OnPopupOpen();
                else
                    OnPopupClose();
                break;
        }
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
            ShowLinkModuleData data = uiData as ShowLinkModuleData;
            return (showCloseBtn && showCloseBtn == data.showClose) || id == data.id;
        }
        return false;
    }

    private void OnPopupOpen()
    {
        if (canvas)
            canvas.overrideSorting = false;
    }
    private void OnPopupClose()
    {
        if (canvas)
            canvas.overrideSorting = GlobalInfo.InEditMode;
    }
    #region 动效
    protected override float joinAnimePlayTime => 0.3f;
    protected override float exitAnimePlayTime => 0.2f;

    public override void JoinAnim(UnityAction callback)
    {
        JoinSequence.Join(DOTween.To(() => 0f, (value) => CanvasGroup.alpha = (value), 1f, JoinAnimePlayTime));
        base.JoinAnim(callback);
    }

    public override void ExitAnim(UnityAction callback)
    {
        ExitSequence.Join(DOTween.To(() => 1f, (value) => CanvasGroup.alpha = (value), 0f, ExitAnimePlayTime));
        base.ExitAnim(callback);
    }
    #endregion
}
