using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;
using UnityEngine.EventSystems;


//使用方法
//var audio = (ShowAudioModule)UIManager.Instance.OpenModuleUI<ShowAudioModule>(this, ShowModulePoint);
//audio.ShowAudioHandler("http://niuyeshuzi-carvideos.oss-cn-hangzhou.aliyuncs.com/CarAudio/DT0.mp3", "音频标题");

/// <summary>
/// 音频播放模块
/// </summary>
public class ShowAudioModule : UIModuleBase
{
    private Canvas canvas;
    private CanvasGroup CanvasGroup;

    private GameObject Background;
    private GameObject ShowAudio;
    private RectTransform PlayerContent;
    private Toggle AudioCtrlToggle;
    private Image ShowAudioPlay;
    private Slider AudioTimeSlider;
    private CanvasGroup SliderCanvasGroup;
    private Text AudioTime;
    private bool oldPlayingState = false;
    private bool SliderValueSetting = false;

    private AudioClip audioClip;
    private AudioSource audioSource;

    private int currentHour;
    private int currentMinute;
    private int currentSecond;

    private int clipHour;
    private int clipMinute;
    private int clipSecond;

    private int id;
    /// <summary>
    /// 是否显示关闭按钮
    /// </summary>
    private bool showCloseBtn;

    public ShowLinkModuleData AudioModuleData { get; private set; }

    private bool interactable;

    //同步
    private float updateInterval = 2f;
    private DateTime lastUpdateTime;
    private DateTime now;

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);
        AddMsg(new ushort[]
        {
            (ushort)CoursePanelEvent.Option,
            (ushort)HyperLinkEvent.AudioCtrl,
            (ushort)HyperLinkEvent.AudioValue,
            (ushort)HyperLinkEvent.AudioSync
        });

        AudioModuleData = (ShowLinkModuleData)uiData;
        id = AudioModuleData.id;
        showCloseBtn = AudioModuleData.showClose;

        InitVariables();
    }

    private void InitVariables()
    {
        audioSource = GetComponent<AudioSource>();

        Background = transform.FindChildByName("Background").gameObject;
        ShowAudio = this.FindChildByName("ShowAudio").gameObject;
        PlayerContent = transform.GetComponentByChildName<RectTransform>("PlayerContent");
        AudioCtrlToggle = this.GetComponentByChildName<Toggle>("ShowAudioToggle");
        ShowAudioPlay = this.GetComponentByChildName<Image>("ShowAudioPlay");
        AudioTimeSlider = this.GetComponentByChildName<Slider>("ShowAudioSlider");
        SliderCanvasGroup = AudioTimeSlider.GetComponent<CanvasGroup>();
        AudioTime = this.GetComponentByChildName<Text>("AudioTime");

        CanvasGroup = GetComponent<CanvasGroup>();
        interactable = !GlobalInfo.IsLiveMode() || GlobalInfo.IsUserOperator();
        CanvasGroup.blocksRaycasts = interactable;
        if (GlobalInfo.IsLiveMode())
        {
            SliderCanvasGroup.blocksRaycasts = false;
            SliderCanvasGroup.alpha = interactable ? 0.2f : 0f;
        }

        Button_LinkMode CloseCtrl = this.GetComponentByChildName<Button_LinkMode>("CloseCtrl");
        CloseCtrl.onClick.AddListener(() =>
        {
            AudioCtrlToggle.isOn = !AudioCtrlToggle.isOn;
        });
        if (showCloseBtn)
        {
            Button Close = this.GetComponentByChildName<Button>("Close");
            Close.onClick.AddListener(() =>
            {
                if (AudioModuleData.closedAction != null)
                    AudioModuleData.closedAction();
                else
                    UIManager.Instance.CloseModuleUI<ShowAudioModule>();
            });
            Close.gameObject.SetActive(true);
            Background.SetActive(true);
        }
        AudioCtrlToggle.onValueChanged.AddListener((arg) =>
        {
            ToolManager.SendBroadcastMsg(new MsgBool((ushort)HyperLinkEvent.AudioCtrl, arg), true);

            NetworkManager.Instance.UpdateVideoPacket(AudioModuleData.url, arg, 0);
        });

        EventTrigger eventTrigger = AudioTimeSlider.gameObject.AddComponent<EventTrigger>();
        eventTrigger.AddEvent(EventTriggerType.PointerDown, (arg) =>
        {
            oldPlayingState = AudioCtrlToggle.isOn;
            SliderValueSetting = true;
            ToolManager.SendBroadcastMsg(new MsgBool((ushort)HyperLinkEvent.AudioCtrl, false), true);
        });
        eventTrigger.AddEvent(EventTriggerType.PointerUp, (arg) =>
        {
            SliderValueSetting = false;
            ToolManager.SendBroadcastMsg(new MsgFloat((ushort)HyperLinkEvent.AudioValue, AudioTimeSlider.value), true);
            ToolManager.SendBroadcastMsg(new MsgBool((ushort)HyperLinkEvent.AudioCtrl, oldPlayingState), true);
        });
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

        ShowAudioHandler(ResManager.Instance.OSSDownLoadPath + AudioModuleData.url, AudioModuleData.title);
        NetworkManager.Instance.UpdateVideoPacket(AudioModuleData.url, false, 0);
    }

    /// <summary>
    /// 根据音频地址播放
    /// </summary>
    /// <param name="url">地址</param>
    /// <param name="title">标题</param>
    public void ShowAudioHandler(string url, string title)
    {
        //UIManager.Instance.OpenUI<LoadingPanel>(UILevel.PopUp);
        StartCoroutine(GetIntroduceAudioClip(url, title));
    }

    IEnumerator GetIntroduceAudioClip(string path, string title)
    {
        yield return new WaitUntil(() => CanvasGroup.alpha == 1);

        string fileExtension = System.IO.Path.GetExtension(path).ToUpper().Substring(1);
        AudioType audioType = AudioType.MPEG;
        switch (fileExtension)
        {
            case FileExtension.OGG:
                audioType = AudioType.OGGVORBIS;
                break;
            case FileExtension.WAV:
                audioType = AudioType.WAV;
                break;
        }

        var uwr = UnityWebRequestMultimedia.GetAudioClip(path, audioType);
        yield return uwr.SendWebRequest();

        if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
        {
            ShowAudio.SetActive(false);
            UIManager.Instance.OpenModuleUI<LocalTipModule_Button>(ParentPanel, ShowAudio.transform.parent,
                new LocalTipModule_Button.ModuleData("音频加载失败", "刷新", () => ShowAudioHandler(path, title), 1));
        }
        else if (uwr.isDone)
        {
            try
            {
                //"加载音频资源"
                audioClip = DownloadHandlerAudioClip.GetContent(uwr);
                ShowAudioHandler(audioClip);
            }
            catch (Exception ex)
            {
                Log.Error($"音频加载失败, {ex.Message}");
                ShowAudio.SetActive(false);
                UIManager.Instance.OpenModuleUI<LocalTipModule_Button>(ParentPanel, ShowAudio.transform.parent,
                    new LocalTipModule_Button.ModuleData("音频加载失败", "刷新", () => ShowAudioHandler(path, title), 1));
            }
        }
        UIManager.Instance.CloseUI<LoadingPanel>();
    }

    /// <summary>
    /// 根据音频文件播放
    /// </summary>
    /// <param name="clip">AudioClip</param>
    /// <param name="title">标题</param>
    public void ShowAudioHandler(AudioClip clip)
    {
        ShowAudio.SetActive(true);

        if (clip != null)
        {
            audioClip = clip;
            audioSource.clip = audioClip;
            clipHour = (int)audioSource.clip.length / 3600;
            clipMinute = (int)(audioSource.clip.length - clipHour * 3600) / 60;
            clipSecond = (int)(audioSource.clip.length - clipHour * 3600 - clipMinute * 60);
            SetAudioTimeValueChange(0);
            ShowAudioTime();
        }
    }

    void Update()
    {
        if (audioSource != null && audioSource.isPlaying)
            ShowAudioTime();
    }

    private void ShowAudioTime()
    {
        currentHour = (int)audioSource.time / 3600;
        currentMinute = (int)(audioSource.time - currentHour * 3600) / 60;
        currentSecond = (int)(audioSource.time - currentHour * 3600 - currentMinute * 60);

        AudioTime.text = string.Format("{0:D2}:{1:D2}:{2:D2} / {3:D2}:{4:D2}:{5:D2}", currentHour, currentMinute, currentSecond, clipHour, clipMinute, clipSecond);

        AudioTimeSlider.value = audioSource.time / audioClip.length;

        if (GlobalInfo.IsLiveMode() && GlobalInfo.IsMainScreen())
        {
            now = DateTime.Now;
            if ((now - lastUpdateTime).TotalSeconds > updateInterval)
            {
                lastUpdateTime = now;
                NetworkManager.Instance.UpdateVideoPacket(AudioModuleData.url, audioSource.isPlaying, 0, AudioTimeSlider.value);
            }
        }

        if (AudioTimeSlider.value > 0.99f)
        {
            AudioCtrlToggle.isOn = false;
            AudioTimeSlider.value = 0;
            audioSource.time = 0;
            AudioTime.text = string.Format("00:00:00 / {0:D2}:{1:D2}:{2:D2}", clipHour, clipMinute, clipSecond);
        }
    }

    private void SetAudioTimeValueChange(float value)
    {
        AudioTimeSlider.value = value;
        audioSource.time = Mathf.Min(AudioTimeSlider.value * audioSource.clip.length, audioSource.clip.length - 0.01f);
    }


    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)HyperLinkEvent.AudioCtrl:
                bool state = ((MsgBrodcastOperate)msg).GetData<MsgBool>().arg1;
                AudioCtrlToggle.SetIsOnWithoutNotify(state);
                ShowAudioPlay.enabled = !state;
                if (state)
                {
                    if ((currentHour == clipHour && currentMinute == clipMinute && currentSecond == clipSecond) || audioSource.time == 0)
                    {
                        audioSource.time = 0;
                        audioSource.Play();
                    }
                    else
                        audioSource.UnPause();
                }
                else
                    audioSource.Pause();
                break;
            case (ushort)HyperLinkEvent.AudioValue:
                if (audioSource.clip == null)
                    return;
                float value = ((MsgBrodcastOperate)msg).GetData<MsgFloat>().arg;
                SetAudioTimeValueChange(value);
                break;
            case (ushort)HyperLinkEvent.AudioSync:
                if (audioSource.clip == null)
                    return;
                float syncValue = ((MsgFloat)msg).arg;
                if (Mathf.Abs(AudioTimeSlider.value - syncValue) * audioSource.clip.length < 2)
                    return;
                SetAudioTimeValueChange(syncValue);
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

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        Resources.UnloadUnusedAssets();
        NetworkManager.Instance.ClearVideoPacket();
        base.Close(uiData, callback);
    }

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
