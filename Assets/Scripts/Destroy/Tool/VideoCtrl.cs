using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityFramework.Runtime;
using Cysharp.Threading.Tasks;

public class VideoCtrl : MonoBase
{
    private Toggle ShowVideoToggle;
    private Image ShowVideoplay;
    private Slider ShowVideoSlider;
    private Text NowM;
    private Text NowS;
    private Text MaxM;
    private Text MaxS;

    private VideoPlayer videoPlayer;
    /// <summary>
    /// 播放状态
    /// </summary>
    private bool oldPlayingState = false;
    private bool isPlay = false;
    private bool SliderValueSetting = false;

    //进度同步
    private float updateInterval = 3f;
    private DateTime lastUpdateTime;
    private DateTime now;

    protected override void InitComponents()
    {
        ShowVideoToggle = ComponentExtend.GetComponentByChildName<Toggle>(transform, "ShowVideoToggle");
        ShowVideoplay = ComponentExtend.GetComponentByChildName<Image>(transform, "ShowVideoplay");
        ShowVideoSlider = ComponentExtend.GetComponentByChildName<Slider>(transform, "ShowVideoSlider");
        NowM = ComponentExtend.GetComponentByChildName<Text>(transform, "NowM");
        NowS = ComponentExtend.GetComponentByChildName<Text>(transform, "NowS");
        MaxM = ComponentExtend.GetComponentByChildName<Text>(transform, "MaxM");
        MaxS = ComponentExtend.GetComponentByChildName<Text>(transform, "MaxS");

        AddMsg(new ushort[]{
            (ushort)HyperLinkEvent.VideoCtrl,
            (ushort)HyperLinkEvent.VideoValue,
        });

        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.started += (source) => ShowVideoToggle.isOn = true;
        videoPlayer.sendFrameReadyEvents = true;
        videoPlayer.frameReady += (source, index) => {
            if (!SliderValueSetting && videoPlayer.isPlaying)
            {
                ShowVideoSlider.value = videoPlayer.frame * 1.0f / videoPlayer.frameCount;
                NowM.text = ((int)videoPlayer.time / 60).ToString("d2");
                NowS.text = ((int)videoPlayer.time % 60).ToString("d2");

                if (GlobalInfo.IsLiveMode() && NetworkManager.Instance.IsFirstActiveUser())
                {
                    now = DateTime.Now;
                    if ((now - lastUpdateTime).TotalSeconds > updateInterval)
                    {
                        lastUpdateTime = now;
                        NetworkManager.Instance.SendFrameMsg(new MsgBool((ushort)HyperLinkEvent.VideoCtrl, true));
                        NetworkManager.Instance.SendFrameMsg(new MsgFloat((ushort)HyperLinkEvent.VideoValue, (float)(ShowVideoSlider.value)));
                    }
                }
            }

            if (ShowVideoSlider.value > 0.99)
            {
                videoPlayer.Stop();
                videoPlayer.Prepare();
                ShowVideoToggle.isOn = false;
                ShowVideoSlider.value = 0;
                NowM.text = "00";
                NowS.text = "00";
            }
        };

        ShowVideoToggle.onValueChanged.AddListener((arg) =>
        {
            lastUpdateTime = DateTime.Now;
            ToolManager.SendBroadcastMsg(new MsgFloat((ushort)HyperLinkEvent.VideoValue, (float)(ShowVideoSlider.value)), true);
            ToolManager.SendBroadcastMsg(new MsgBool((ushort)HyperLinkEvent.VideoCtrl, arg), true);
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
        eventTrigger.AddEvent(EventTriggerType.Drag, (arg) =>
        {
            ToolManager.SendBroadcastMsg(new MsgFloat((ushort)HyperLinkEvent.VideoValue, (float)(ShowVideoSlider.value)), true);
        });
        eventTrigger.AddEvent(EventTriggerType.PointerUp, (arg) =>
        {
            SliderValueSetting = false;
            lastUpdateTime = DateTime.Now;
            ToolManager.SendBroadcastMsg(new MsgFloat((ushort)HyperLinkEvent.VideoValue, (float)(ShowVideoSlider.value)), true);
            ToolManager.SendBroadcastMsg(new MsgBool((ushort)HyperLinkEvent.VideoCtrl, oldPlayingState), true);         
        });

        Init(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private void OnEnable()
    {
        Init(this.GetCancellationTokenOnDestroy()).Forget();
    }

    async UniTaskVoid Init(System.Threading.CancellationToken ct)
    {
        if (videoPlayer == null)
            return;

        while (videoPlayer.frameCount == 0)
        {
            await UniTask.Yield(ct);
        }
        int i = (int)(videoPlayer.frameCount / videoPlayer.frameRate);
        int m = i / 60;
        int s = i % 60;
        NowM.text = "00";
        NowS.text = "00";
        MaxM.text = m.ToString("d2");
        MaxS.text = s.ToString("d2");
    }


    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)HyperLinkEvent.VideoCtrl:
                bool isVideoPlay = ((MsgBrodcastOperate)msg).GetData<MsgBool>().arg1;
                ShowVideoToggle.SetIsOnWithoutNotify(isVideoPlay);
                isPlay = isVideoPlay;
                if (isVideoPlay)
                {
                    videoPlayer.Play();
                    ShowVideoplay.enabled = false;
                }
                else
                {
                    videoPlayer.Pause();
                    ShowVideoplay.enabled = true;
                }
                break;
            case (ushort)HyperLinkEvent.VideoValue:
                if (videoPlayer.frameCount == 0)
                    return;

                float sliderValue =((MsgBrodcastOperate)msg).GetData<MsgFloat>().arg;
                long frame = long.Parse((sliderValue * videoPlayer.frameCount).ToString("0."));
                videoPlayer.frame = frame;
                //if videoPlayer.frameCount == 0, NaN
                ShowVideoSlider.value = frame * 1.0f / videoPlayer.frameCount;
                int time = (int)(frame / videoPlayer.frameRate);
                NowM.text = (time / 60).ToString("d2");
                NowS.text = (time % 60).ToString("d2");
                break;
        }
    }

    public void ResetVideo()
    {
        if (ShowVideoToggle != null)
        {
            ShowVideoToggle.isOn = false;
        }
        if (ShowVideoSlider != null)
        {
            ShowVideoSlider.value = 0;
        }
    }
}
