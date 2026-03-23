using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using DG.Tweening;
using UnityFramework.Runtime;

/// <summary>
/// 显示视频模块
/// </summary>
public class ShowVideoModule : UIModuleBase
{
    private Canvas canvas;
    private CanvasGroup CanvasGroup;
    private GameObject Background;

    private RectTransform PlayerContent;
    private Toggle ShowVideoToggle;
    private Image ShowVideoplay;
    private Slider ShowVideoSlider;
    private CanvasGroup SliderCanvasGroup;

    private VideoPlayer ShowVideo;
    private RawImage ShowVideoImage;
    private RenderTexture renderTexture;
    private AspectRatioFitter RatioFitter;
    private ulong frameCount;
    private double videoDuration;
    /// <summary>
    /// 播放状态
    /// </summary>
    private bool firstStart = true;
    private bool oldPlayingState = false;
    private bool isPlay = false;
    private bool SliderValueSetting = false;
    private bool videoSeekPlay = false;
    private double seekDesiredTime;

    private Text NowText;
    private Text MaxText;

    //进度同步
    private float updateInterval = 2f;
    private DateTime lastUpdateTime;
    private DateTime now;

    private int id;
    /// <summary>
    /// 是否显示关闭按钮
    /// </summary>
    private bool showCloseBtn;

    public ShowLinkModuleData VideoModuleData { get; private set; }

    private bool interactable;

    public override bool Repeatable { get { return true; } }

    private void OnEnable()
    {
        firstStart = true;
    }

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        AddMsg(new ushort[]
        {
#if UNITY_ANDROID || UNITY_IOS
            (ushort)ARModuleEvent.Tracking,
#endif
            (ushort)CoursePanelEvent.Option,
            (ushort)HyperLinkEvent.VideoCtrl,
            (ushort)HyperLinkEvent.VideoValue,
            (ushort)HyperLinkEvent.VideoSync
        });

        VideoModuleData = (ShowLinkModuleData)uiData;
        id = VideoModuleData.id;
        showCloseBtn = VideoModuleData.showClose;

        InitVariables();
        ShowVideoHandler(ResManager.Instance.OSSDownLoadPath + VideoModuleData.url, VideoModuleData.title);

        NetworkManager.Instance.UpdateVideoPacket(VideoModuleData.url, true, 1);
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

        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        if (ShowVideo == null)
            yield break;
        ShowVideo.Prepare();
        yield return new WaitUntil(() => ShowVideo.isPrepared);
        PlayVideo(true);
    }

    private void InitVariables()
    {
        Background = transform.FindChildByName("Background").gameObject;
        PlayerContent = transform.GetComponentByChildName<RectTransform>("PlayerContent");
        ShowVideo = this.GetComponentByChildName<VideoPlayer>("ShowVideo");
        ShowVideoImage = ShowVideo.GetComponentInChildren<RawImage>();
        RatioFitter = ShowVideo.GetComponentInChildren<AspectRatioFitter>();
        ShowVideoToggle = this.GetComponentByChildName<Toggle>("ShowVideoToggle");
        ShowVideoplay = this.GetComponentByChildName<Image>("ShowVideoplay");
        ShowVideoSlider = this.GetComponentByChildName<Slider>("ShowVideoSlider");
        SliderCanvasGroup = ShowVideoSlider.GetComponent<CanvasGroup>();
        NowText = this.GetComponentByChildName<Text>("Now");
        MaxText = this.GetComponentByChildName<Text>("Max");

        CanvasGroup = GetComponent<CanvasGroup>();
        interactable = !GlobalInfo.IsLiveMode() || GlobalInfo.IsUserOperator();
        CanvasGroup.blocksRaycasts = interactable;
        if (GlobalInfo.IsLiveMode())
        {
            SliderCanvasGroup.blocksRaycasts = false;
            SliderCanvasGroup.alpha = interactable ? 0.2f : 0f;
        }

        renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
        renderTexture.wrapMode = TextureWrapMode.Clamp;
        renderTexture.filterMode = FilterMode.Bilinear;
        renderTexture.Create();

        ShowVideo.targetTexture = renderTexture;
        ShowVideoImage.texture = renderTexture;

        ShowVideoToggle.onValueChanged.AddListener((arg) =>
        {
            lastUpdateTime = DateTime.Now;
            ToolManager.SendBroadcastMsg(new MsgFloatBool((ushort)HyperLinkEvent.VideoValue, (float)(ShowVideoSlider.value), arg), true);
            ToolManager.SendBroadcastMsg(new MsgBool((ushort)HyperLinkEvent.VideoCtrl, arg), true);

            NetworkManager.Instance.UpdateVideoPacket(VideoModuleData.url, arg, 1);
        });
        ShowVideoToggle.isOn = false;
        ShowVideoSlider.value = 0;

        EventTrigger eventTrigger = ShowVideoSlider.gameObject.AddComponent<EventTrigger>();
        eventTrigger.AddEvent(EventTriggerType.PointerDown, (arg) =>
        {
            oldPlayingState = ShowVideoToggle.isOn;
            SliderValueSetting = true;
            ToolManager.SendBroadcastMsg(new MsgBool((ushort)HyperLinkEvent.VideoCtrl, false), true);
        });
        //eventTrigger.AddEvent(EventTriggerType.Drag, (arg) =>
        //{
        //    ToolManager.SendBroadcastMsg(new MsgFloat((ushort)HyperLinkEvent.VideoValue, (float)(ShowVideoSlider.value)), true);
        //});
        eventTrigger.AddEvent(EventTriggerType.PointerUp, (arg) =>
        {
            SliderValueSetting = false;
            lastUpdateTime = DateTime.Now;
            ToolManager.SendBroadcastMsg(new MsgFloatBool((ushort)HyperLinkEvent.VideoValue, (float)(ShowVideoSlider.value), oldPlayingState), true);
            ToolManager.SendBroadcastMsg(new MsgBool((ushort)HyperLinkEvent.VideoCtrl, oldPlayingState), true);
        });

        Button_LinkMode CloseCtrl = this.GetComponentByChildName<Button_LinkMode>("CloseCtrl");
        CloseCtrl.onClick.AddListener(() =>
        {
            ShowVideoToggle.isOn = !ShowVideoToggle.isOn;
        });

        if (showCloseBtn)
        {
            Button Close = this.GetComponentByChildName<Button>("Close");
            Close.onClick.AddListener(() =>
            {
                if (VideoModuleData.closedAction != null)
                    VideoModuleData.closedAction();
                else
                    UIManager.Instance.CloseModuleUI<ShowVideoModule>();
            });
            Close.gameObject.SetActive(true);
            Background.SetActive(true);
        }

        NowText.text = "00:00 ";

        ShowVideo.started += (source) =>
        {
            if (firstStart)
            {
                RatioFitter.aspectRatio = (float)source.width / source.height;
                frameCount = source.frameCount;
                videoDuration = source.length;
                MaxText.text = $"/ {ToTimeFormat(source.length)}";

                ShowVideoToggle.SetIsOnWithoutNotify(true);
                ShowVideoplay.enabled = false;
                isPlay = true;
                oldPlayingState = true;

                firstStart = false;
            }
        };
        ShowVideo.seekCompleted += (source) =>
        {
            PlayVideo(videoSeekPlay);
        };

        ShowVideo.errorReceived += (source, message) =>
        {
            ShowVideo.Stop();
            ClearOutRenderTexture(ShowVideo.targetTexture, Color.white);
            ShowVideo.gameObject.SetActive(false);

            UIManager.Instance.OpenModuleUI<LocalTipModule_Button>(ParentPanel, ShowVideo.transform.parent,
                new LocalTipModule_Button.ModuleData("视频加载失败", "刷新", () =>
                {
                    ShowVideo.gameObject.SetActive(true);
                    ShowVideo.Prepare();
                }, 2));
        };

        ShowVideo.sendFrameReadyEvents = true;
        ShowVideo.frameReady += (source, index) =>
        {
            if (!SliderValueSetting && ShowVideo.isPlaying)
            {
                ShowVideoSlider.value = (float)index / (float)source.frameCount;
                NowText.text = $"{ToTimeFormat(source.time)} ";

                //if (GlobalInfo.IsLiveMode() && NetworkManager.Instance.IsFirstActiveUser())
                if (GlobalInfo.IsLiveMode() && GlobalInfo.IsMainScreen())
                {
                    now = DateTime.Now;
                    if ((now - lastUpdateTime).TotalSeconds > updateInterval)
                    {
                        lastUpdateTime = now;
                        //NetworkManager.Instance.SendFrameMsg(new MsgFloatBool((ushort)HyperLinkEvent.VideoValue, (float)(ShowVideoSlider.value), true));
                        NetworkManager.Instance.UpdateVideoPacket(VideoModuleData.url, true, 1, (float)(ShowVideoSlider.value));
                    }
                }
            }

            if (ShowVideoSlider.value > 0.99f)
            {
                ShowVideo.Stop();
                //ShowVideo.Prepare();
                ShowVideoSlider.value = 0;
                ShowVideoToggle.isOn = false;
                NowText.text = "00:00 ";
            }
        };
    }

    /// <summary>
    /// 加载视频地址并播放
    /// </summary>
    /// <param name="fileUrl">地址</param>
    /// <param name="fileName">名称</param>
    public void ShowVideoHandler(string fileUrl, string fileName)
    {
        ShowVideo.url = fileUrl.Replace("https", "http");
        //ShowVideo.gameObject.OnlyShowOrHide(true);
        ShowVideo.Prepare();
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
            case (ushort)HyperLinkEvent.VideoCtrl:
                bool isVideoPlay = ((MsgBrodcastOperate)msg).GetData<MsgBool>().arg1;
                PlayVideo(isVideoPlay);
                break;
            case (ushort)HyperLinkEvent.VideoValue:
                if (frameCount == 0)
                    return;

                MsgFloatBool msgFloatBool = ((MsgBrodcastOperate)msg).GetData<MsgFloatBool>();
                float sliderValue = msgFloatBool.arg1;
                videoSeekPlay = msgFloatBool.arg2;
                seekDesiredTime = sliderValue * videoDuration;
                long frame = long.Parse((sliderValue * ShowVideo.frameCount).ToString("0."));

                ShowVideoSlider.value = sliderValue;
#if UNITY_IOS && !UNITY_EDITOR
                ShowVideoToggle.SetIsOnWithoutNotify(videoSeekPlay);
                ShowVideoplay.enabled = !videoSeekPlay;

                if (seekFrameCo != null)
                    StopCoroutine(seekFrameCo);
                seekFrameCo = StartCoroutine(SeekFrameRoutine(frame));
#else
                ShowVideo.frame = frame;
                NowText.text = $"{ToTimeFormat(seekDesiredTime)} ";
#endif
                break;
            case (ushort)HyperLinkEvent.VideoSync:
                if (frameCount == 0)
                    return;
                float syncValue = ((MsgFloat)msg).arg;
                seekDesiredTime = syncValue * videoDuration;
                long syncFrame = long.Parse((syncValue * ShowVideo.frameCount).ToString("0."));
                if (Mathf.Abs(ShowVideo.frame - syncFrame) < 60)
                    return;
                ShowVideoSlider.value = syncValue;
                ShowVideo.frame = syncFrame;
                NowText.text = $"{ToTimeFormat(seekDesiredTime)} ";
                break;
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
    /// 控制播放
    /// </summary>
    /// <param name="play"></param>
    private void PlayVideo(bool play)
    {
        ShowVideoToggle.SetIsOnWithoutNotify(play);
        isPlay = play;
        if (play)
        {
            ShowVideo.Play();
            ShowVideoplay.enabled = false;
        }
        else
        {
            ShowVideo.Pause();
            ShowVideoplay.enabled = true;
        }
    }

    private Coroutine seekFrameCo;
    /// <summary>
    /// 针对IOS通过帧数设置播放进度和播放的状态
    /// </summary>
    /// <param name="seekFrame"></param>
    /// <returns></returns>
    private IEnumerator SeekFrameRoutine(long seekFrame)
    {
        if (ShowVideo)
        {
            ShowVideo.Stop();
            yield return null;

            ShowVideo.Prepare();
            float waitTillTime = Time.time + 3f;
            while (!ShowVideo.isPrepared && (Time.time < waitTillTime))
            {
                yield return null;
            }

            //跳转帧数前需要一次播放暂停处理防止从头开始
            ShowVideo.Play();
            ShowVideo.Pause();
            yield return null;

            ShowVideo.frame = seekFrame;
            NowText.text = $"{ToTimeFormat(seekDesiredTime)} ";
        }
    }

    public void ResetVideo()
    {
        if(ShowVideoToggle != null)
        {
            ShowVideoToggle.isOn = false;
        }
        if (ShowVideoSlider != null)
        {
            ShowVideoSlider.value = 0;
        }
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        renderTexture.Release();
        ShowVideo.targetTexture = null;
        ShowVideoImage.texture = null;
        Destroy(renderTexture);
        renderTexture = null;
        Resources.UnloadUnusedAssets();

        NetworkManager.Instance.ClearVideoPacket();
        base.Close(uiData, callback);
    }

    public void ClearOutRenderTexture(RenderTexture renderTexture, Color color)
    {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = renderTexture;
        GL.Clear(true, true, color);
        RenderTexture.active = rt;
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

    private string ToTimeFormat(double time)
    {
        int seconds = (int)time;
        int minutes = seconds / 60;
        seconds %= 60;
        return string.Format("{0:D2}:{1:D2}", minutes, seconds);
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
        JoinSequence.Join(PlayerContent.DOAnchorPos3DY(48f, JoinAnimePlayTime));
        base.JoinAnim(callback);
    }

    public override void ExitAnim(UnityAction callback)
    {
        ExitSequence.Join(DOTween.To(() => 1f, (value) => CanvasGroup.alpha = (value), 0f, ExitAnimePlayTime));
        ExitSequence.Join(PlayerContent.DOAnchorPos3DY(-48f, ExitAnimePlayTime));
        base.ExitAnim(callback);
    }
    #endregion
}
