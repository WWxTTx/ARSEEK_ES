using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Vuplex.WebView;
using DG.Tweening;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;
using UnityEngine.Networking;

public class ShowLinkModuleData : UIData
{
    public int id;
    public string title;
    public string url;
    /// <summary>
    /// 文件类型
    /// </summary>
    public string docType;
    /// <summary>
    /// 模块关闭回调
    /// </summary>
    public Action closedAction;
    /// <summary>
    /// 是否显示关闭图标
    /// </summary>
    public bool showClose;

    public ShowLinkModuleData() { }

    public ShowLinkModuleData(int id, string title, string url, string docType, Action closedAction = null, bool showClose = false)
    {
        this.id = id;
        this.title = title;
        this.url = url;
        this.docType = docType;
        this.closedAction = closedAction;
        this.showClose = showClose;
    }

    public ShowLinkModuleData(Encyclopedia encyclopedia, string docType)
    {
        this.id = encyclopedia.id;
        this.title = encyclopedia.name;
        this.url = (encyclopedia as EncyclopediaLink).data;
        this.docType = docType;
    }

    public ShowLinkModuleData(int id, string title, string docType, bool showClose = false)
    {
        this.id = id;
        this.title = title;
        this.docType = docType;
        this.showClose = showClose;
    }

    public ShowLinkModuleData(MsgHyperlink hyperlink, Action closedAction)
    {
        this.id = hyperlink.id;
        this.title = hyperlink.title;
        this.url = hyperlink.url;
        this.docType = hyperlink.docType;
        this.closedAction = closedAction;
        this.showClose = hyperlink.showClose;
    }

    public ShowLinkModuleData(MsgHyperlinkClose hyperlinkClose)
    {
        this.id = hyperlinkClose.id;
        this.title = hyperlinkClose.title;
        this.docType = hyperlinkClose.docType;
        this.showClose = false;
    }
}

/// <summary>
/// 查看超链接模块
/// </summary>
public class ShowLinkModule : UIModuleBase
{
    private Canvas canvas;
    private CanvasGroup CanvasGroup;
    private RectTransform Background;
    private Button CloseBtn;
    private CanvasWebViewPrefab CanvasWebViewPrefab;

    private int id;

    public ShowLinkModuleData LinkModuleData { get; private set; }

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

        CanvasGroup = GetComponent<CanvasGroup>();
        CanvasGroup.interactable = !GlobalInfo.isLive || GlobalInfo.IsOperator();
        Background = this.GetComponentByChildName<RectTransform>("BackGround");
        CloseBtn = this.GetComponentByChildName<Button>("Close");
        CanvasWebViewPrefab = this.GetComponentByChildName<CanvasWebViewPrefab>("CanvasWebViewPrefab");
#if UNITY_ANDROID || UNITY_IOS
        //CanvasWebViewPrefab.Resolution = Screen.width / 2340f;
#endif
        CanvasWebViewPrefab.ClickingEnabled = !GlobalInfo.isLive || GlobalInfo.IsOperator();
        CanvasWebViewPrefab.ScrollingEnabled = !GlobalInfo.isLive || GlobalInfo.IsOperator();

        LinkModuleData = uiData as ShowLinkModuleData;

        ShowFileHandler(LinkModuleData);

        GetComponent<CanvasGroup>().interactable = !GlobalInfo.isLive || GlobalInfo.IsOperator();
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
    /// 加载文档
    /// </summary>
    /// <param name="linkModuleData"></param>
    private async void ShowFileHandler(ShowLinkModuleData linkModuleData)
    {
        id = linkModuleData.id;

        if (linkModuleData.showClose)
        {
            CloseBtn.onClick.AddListener(() =>
            {
                if (linkModuleData.closedAction != null)
                    linkModuleData.closedAction.Invoke();
                else
                    UIManager.Instance.CloseModuleUI<ShowLinkModule>();
            });
            CloseBtn.gameObject.SetActive(true);
        }
        else
        {
#if UNITY_ANDROID || UNITY_IOS
            Background.offsetMax = new Vector2(Background.offsetMax.x, -96);
#endif
        }

        CanvasWebViewPrefab.ScrollingEnabled = true;
        CanvasWebViewPrefab.ClickingEnabled = true;
#if UNITY_ANDROID || UNITY_IOS
        CanvasWebViewPrefab.DragMode = DragMode.DragToScroll;
#else
        CanvasWebViewPrefab.DragMode = DragMode.DragWithinPage;
#endif

        await CanvasWebViewPrefab.WaitUntilInitialized();

        //string url = "https://view.officeapps.live.com/op/view.aspx?src=" + UnityWebRequest.EscapeURL(linkModuleData.url);

        //string furl = UnityWebRequest.EscapeURL($"{ResManager.Instance.OSSDownLoadPath}{linkModuleData.url}");
        //CanvasWebViewPrefab.WebView.LoadUrl($"https://ow365.cn/?i=32258&ssl=1&n=5&furl={furl}&p=1");

        CanvasWebViewPrefab.WebView.LoadUrl($"{ApiData.STSObjectView}?objectName=/{linkModuleData.url}");
        CanvasWebViewPrefab.WebView.PageLoadFailed -= PageLoadFailed;
        CanvasWebViewPrefab.WebView.PageLoadFailed += PageLoadFailed;

        //CanvasWebViewPrefab.WebView.ConsoleMessageLogged -= OnConsoleLogged;
        //CanvasWebViewPrefab.WebView.ConsoleMessageLogged += OnConsoleLogged;

#if UNITY_ANDROID || UNITY_IOS
        ////隐藏滑动条
        //CanvasWebViewPrefab.WebView.PageLoadScripts.Add(@"
        //    var styleElement = document.createElement('style');
        //    styleElement.innerText = `
        //      *::-webkit-scrollbar {
        //        display: none;
        //      }
        //      * {
        //        scrollbar-width: none;
        //        -ms-overflow-style: none;
        //      }
        //    `;
        //    document.head.appendChild(styleElement);
        //");
#endif
    }

    void PageLoadFailed(object sender, EventArgs e)
    {
        CanvasWebViewPrefab.gameObject.SetActive(false);
        UIManager.Instance.OpenModuleUI<LocalTipModule_Button>(ParentPanel, Background,
            new LocalTipModule_Button.ModuleData("加载失败", "刷新", () =>//"网络连接断开，请检查网络设置"
            {
                CanvasWebViewPrefab.gameObject.SetActive(true);
                CanvasWebViewPrefab.WebView.Reload();
            }, 1));
    }

    void OnConsoleLogged(object sender, ConsoleMessageEventArgs eventArgs)
    {
        switch (eventArgs.Level)
        {
            case ConsoleMessageLevel.Error:
                CanvasWebViewPrefab.Resolution -= 0.2f;
                Debug.LogError($"CanvasWebViewPrefab Resolution {CanvasWebViewPrefab.Resolution}");
                break;
            case ConsoleMessageLevel.Warning:
                break;
            default:
                break;
        }
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        Resources.UnloadUnusedAssets();
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
            ShowLinkModuleData data = uiData as ShowLinkModuleData;
            return id == data.id;
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