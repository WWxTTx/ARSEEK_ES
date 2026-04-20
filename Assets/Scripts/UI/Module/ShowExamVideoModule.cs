using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using DG.Tweening;
using UnityFramework.Runtime;
using UnityEditor;
using System.Windows.Forms;
using Button = UnityEngine.UI.Button;
using System.Threading;

/// <summary>
/// 显示视频模块
/// </summary>
public class ShowExamVideoModule : UIModuleBase
{
    #region 组件引用
    //视频播放组件
    private VideoPlayer ShowVideo;

    //小窗/全屏的播放暂停开关
    private Toggle playState_toggle;
    private Toggle fullPlayState_toggle;

    //小窗/全屏的进度条
    private Slider progressBar_slider;
    private Slider fullProgressBar_slider;

    //小窗/全屏的总时长（最大时间）
    private Text totalDuration_text;
    private Text fullTotalDuration_text;

    //全屏状态下，底部控制栏（播放、时间等）的父组件
    private RectTransform fullUnder_rectTransform;
    //小窗容器
    private RectTransform videoContent_rectTransform;
    #endregion

    //视频播放使用的RenderTexture
    private RenderTexture videoRenderTexture;

    //用于控制小窗双击全屏/全屏双击退出
    private float buttonTimer;
    private float fullButtonTimer;
    //小窗双击间隔会触发全屏/小屏
    private float doubleClickGap = 0.3f;

    //判断是否播放结束
    private bool isend = false;

    //控制小窗容器拖拽
    private bool isDrag = false;
    private Vector2 dragOffset;

    //控制全屏状态下计时器时间到了就隐藏底部栏
    private float exitProgressTimer;
    private float exitProgressGap = 3f;

    private double rawVideoLength = 0;
    private CancellationTokenSource videoCts;

    public ShowExamModuleData VideoModuleData { get; private set; }

    /// <summary>
    /// 初始化
    /// </summary>
    private void Init()
    {
        #region 获取组件引用
        playState_toggle = transform.GetComponentByChildName<Toggle>("ShowVideoToggle");
        progressBar_slider = transform.GetComponentByChildName<Slider>("ShowVideoSlider");
        totalDuration_text = transform.GetComponentByChildName<Text>("Max");
        ShowVideo = transform.GetComponentByChildName<VideoPlayer>("ShowVideo");
        fullTotalDuration_text = transform.GetComponentByChildName<Text>("Max_Full");
        fullPlayState_toggle = transform.GetComponentByChildName<Toggle>("ShowVideoToggle_Full");
        fullProgressBar_slider = transform.GetComponentByChildName<Slider>("ShowVideoSlider_Full");
        fullUnder_rectTransform = transform.Find("FullScreen").Find("PlayerContent").GetComponent<RectTransform>();
        videoContent_rectTransform = transform.GetComponentByChildName<RectTransform>("Content");
        #endregion

        //设置标题
        transform.GetComponentByChildName<Text>("Title").text = VideoModuleData.title;

        #region 设置视频播放
        //创建屏幕Raw Image
        videoRenderTexture = new RenderTexture(1920, 1080, 1);
        ShowVideo.targetTexture = videoRenderTexture;
        transform.GetComponentByChildName<RawImage>("RawImage").texture = videoRenderTexture;
        transform.GetComponentByChildName<RawImage>("RawImage_Full").texture = videoRenderTexture;

        ShowVideo.url = ResManager.Instance.OSSDownLoadPath + VideoModuleData.url;
        ShowVideo.Play();

        //视频(地址)加载失败
        ShowVideo.errorReceived += (source, message) =>
        {
            ShowVideo.Stop();

            transform.Find("Content").gameObject.SetActive(false);
            transform.Find("FullScreen").gameObject.SetActive(false);
            transform.Find("ErrorImage").gameObject.SetActive(true);
        };
        #endregion

        #region 设置各个按钮的点击效果
        //(小窗口)点击播放暂停/双击全屏
        transform.GetComponentByChildName<Button>("VideoScreenButton").onClick.AddListener(() =>
        {
            //单击
            playState_toggle.isOn = !playState_toggle.isOn;

            //双击
            if (Time.time - buttonTimer < doubleClickGap)//双击全屏
            {
                ChangeFullScreen();
            }
            buttonTimer = Time.time;
        });
        //(全屏)点击播放暂停/双击退出全屏
        transform.GetComponentByChildName<Button>("VideoScreenButton_Full").onClick.AddListener(() =>
        {
            //单击
            playState_toggle.isOn = !playState_toggle.isOn;

            //双击
            if (Time.time - fullButtonTimer < doubleClickGap)//双击退出全屏
            {
                ExitFullScreen();
            }
            fullButtonTimer = Time.time;
        });

        //关闭视频窗口按钮
        transform.GetComponentByChildName<Button>("Close").onClick.AddListener(() =>
        {
            Close();
        });
        //全屏按钮
        transform.GetComponentByChildName<Button>("FullScreenButton").onClick.AddListener(() =>
        {
            ChangeFullScreen();
        });
        //退出全屏按钮
        transform.GetComponentByChildName<Button>("ExitFullScreenButton").onClick.AddListener(() =>
        {
            ExitFullScreen();
        });

        //播放暂停开关
        playState_toggle.onValueChanged.AddListener((state) =>
        {
            if (state)
            {
                videoCts?.Cancel();
                videoCts = new CancellationTokenSource();
                StartVideo(videoCts.Token).Forget();
            }
            else
            {
                videoCts?.Cancel();
                videoCts = new CancellationTokenSource();
                StopVideo(videoCts.Token).Forget();
                ShowVideo.Pause();
            }

            fullPlayState_toggle.isOn = state;
            if (state)
            {
                playState_toggle.transform.Find("ShowVideoplay").GetComponent<Image>().color = new Vector4(1, 1, 1, 1f / 255f);
            }
            else
            {
                playState_toggle.transform.Find("ShowVideoplay").GetComponent<Image>().color = new Vector4(1, 1, 1, 1);
            }

        });
        playState_toggle.gameObject.AddComponent<EventTrigger>().AddEvent(EventTriggerType.PointerEnter, (arg) =>
        {
            playState_toggle.transform.GetComponentByChildName<Image>("Image").color = new Vector4(1, 1, 1, 40f / 255f);
        });
        playState_toggle.gameObject.AddComponent<EventTrigger>().AddEvent(EventTriggerType.PointerExit, (arg) =>
        {
            playState_toggle.transform.GetComponentByChildName<Image>("Image").color = new Vector4(1, 1, 1, 1f / 255f);
        });
        //全屏播放暂停开关
        fullPlayState_toggle.onValueChanged.AddListener((state) =>
        {
            playState_toggle.isOn = state;

            if (state)
            {
                fullPlayState_toggle.transform.Find("ShowVideoplay").GetComponent<Image>().color = new Vector4(1, 1, 1, 1f / 255f);
            }
            else
            {
                fullPlayState_toggle.transform.Find("ShowVideoplay").GetComponent<Image>().color = new Vector4(1, 1, 1, 1);
            }
        });
        fullPlayState_toggle.gameObject.AddComponent<EventTrigger>().AddEvent(EventTriggerType.PointerEnter, (arg) =>
        {
            fullPlayState_toggle.transform.GetComponentByChildName<Image>("Image").color = new Vector4(1, 1, 1, 40f / 255f);
        });
        fullPlayState_toggle.gameObject.AddComponent<EventTrigger>().AddEvent(EventTriggerType.PointerExit, (arg) =>
        {
            fullPlayState_toggle.transform.GetComponentByChildName<Image>("Image").color = new Vector4(1, 1, 1, 1f / 255f);
        });
        #endregion

        #region 设置进度条
        //(小窗)进度条拖拽事件
        EventTrigger eventTrigger = progressBar_slider.gameObject.AddComponent<EventTrigger>();
        eventTrigger.AddEvent(EventTriggerType.PointerDown, (arg) =>
        {
            ShowVideo.Pause();
        });
        eventTrigger.AddEvent(EventTriggerType.PointerUp, (arg) =>
        {
            ShowVideo.time = ShowVideo.length * progressBar_slider.value;
            if (playState_toggle.isOn)
            {
                StartVideo(this.GetCancellationTokenOnDestroy()).Forget();
            }
        });
        //(全屏)进度条拖拽事件
        EventTrigger eventTrigger1 = fullProgressBar_slider.gameObject.AddComponent<EventTrigger>();
        eventTrigger1.AddEvent(EventTriggerType.PointerDown, (arg) =>
        {
            ShowVideo.Pause();
        });
        eventTrigger1.AddEvent(EventTriggerType.PointerUp, (arg) =>
        {
            ShowVideo.time = ShowVideo.length * fullProgressBar_slider.value;
            if (playState_toggle.isOn)
            {
                StartVideo(this.GetCancellationTokenOnDestroy()).Forget();
            }
        });
        #endregion

        //设置小窗口拖拽
        EventTrigger eventTrigger2 = transform.GetComponentByChildName<Image>("DragImage").gameObject.AddComponent<EventTrigger>();
        eventTrigger2.AddEvent(EventTriggerType.PointerDown, (arg) =>
        {
            isDrag = true;
            dragOffset = (Vector2)Input.mousePosition - (Vector2)videoContent_rectTransform.anchoredPosition;
        });
        eventTrigger2.AddEvent(EventTriggerType.PointerUp, (arg) =>
        {
            isDrag = false;
        });
    }

    /// <summary>
    /// 全屏
    /// </summary>
    private void ChangeFullScreen()
    {
        transform.GetComponentByChildName<RectTransform>("Content").gameObject.SetActive(false);
        transform.GetComponentByChildName<RectTransform>("FullScreen").gameObject.SetActive(true);

        exitProgressTimer = Time.time;
        fullUnder_rectTransform.DOKill();
        fullUnder_rectTransform.DOAnchorPos(new Vector2(0, 0), 0.4f);
    }

    /// <summary>
    /// 退出全屏
    /// </summary>
    private void ExitFullScreen()
    {
        transform.GetComponentByChildName<RectTransform>("Content").gameObject.SetActive(true);
        transform.GetComponentByChildName<RectTransform>("FullScreen").gameObject.SetActive(false);
    }

    private void Update()
    {
        if (rawVideoLength == 0)
        {
            rawVideoLength = ShowVideo.length;
        }

        //更新进度
        if (progressBar_slider != null && ShowVideo.isPlaying)
        {
            //更新显示时间
            //Log.Debug(ShowVideo.length);
            totalDuration_text.text = "/" + ToTimeFormat(rawVideoLength);
            totalDuration_text.GetComponentByChildName<Text>("Now").text = ToTimeFormat(ShowVideo.time);

            fullTotalDuration_text.text = "/" + ToTimeFormat(rawVideoLength);
            fullTotalDuration_text.GetComponentByChildName<Text>("Now_Full").text = ToTimeFormat(ShowVideo.time);

            if ((float)ShowVideo.length != 0)
            {
                progressBar_slider.value = (float)ShowVideo.time / (float)ShowVideo.length;
                fullProgressBar_slider.value = progressBar_slider.value;

                if (ShowVideo.length - ShowVideo.time < 0.05)
                {
                    progressBar_slider.value = 1f;
                    fullProgressBar_slider.value = 1f;
                    fullTotalDuration_text.GetComponentByChildName<Text>("Now_Full").text = ToTimeFormat(rawVideoLength);
                    totalDuration_text.GetComponentByChildName<Text>("Now").text = ToTimeFormat(rawVideoLength);
                }
            }
            else
            {
                progressBar_slider.value = 1;
                fullProgressBar_slider.value = 1f;
            }
        }

        //视频播放结束暂停
        if (ShowVideo.length != 0 && progressBar_slider.value == 1 && playState_toggle.isOn && ShowVideo.isPlaying && !isend)
        {
            isend = true;
            playState_toggle.isOn = false;
            ShowVideo.Stop();
        }

        //拖拽视频窗口
        if (isDrag)
        {
            videoContent_rectTransform.anchoredPosition = (Vector2)Input.mousePosition - dragOffset;
        }

        //全屏显示下，显示和关闭进度条
        if (fullUnder_rectTransform.gameObject.activeSelf)
        {
            if (Input.mousePosition.y > 100)
            {
                if (Time.time - exitProgressTimer > exitProgressGap)
                {
                    fullUnder_rectTransform.DOKill();
                    fullUnder_rectTransform.DOAnchorPos(new Vector2(0, -52), 0.4f);
                }
            }
            else
            {
                exitProgressTimer = Time.time;
                fullUnder_rectTransform.DOKill();
                fullUnder_rectTransform.DOAnchorPos(new Vector2(0, 0), 0.4f);
            }
        }
    }

    /// <summary>
    /// 当组件被禁用时，停止视频播放
    /// </summary>
    private void OnDisable()
    {
        Close();
    }

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        VideoModuleData = (ShowExamModuleData)uiData;

        Init();
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        if (ShowVideo != null)
        {
            ShowVideo.Stop();
            ShowVideo.targetTexture = null;
        }

        if (videoRenderTexture != null)
        {
            videoRenderTexture.Release();
            Destroy(videoRenderTexture);
            videoRenderTexture = null;
        }

        base.Close(uiData, callback);
    }

    public void Destroy()
    {
        Close();
    }

    /// <summary>
    /// 延迟播放，为了避免双击放大
    /// </summary>
    /// <returns></returns>
    private async UniTaskVoid StartVideo(CancellationToken ct)
    {
        await UniTask.Delay((int)(doubleClickGap * 1000), cancellationToken: ct);
        ShowVideo.Play();

        transform.GetComponentByChildName<Image>("PauseImage").gameObject.SetActive(!playState_toggle.isOn);
        transform.GetComponentByChildName<Image>("PauseImage_Full").gameObject.SetActive(!playState_toggle.isOn);

        if (isend)
        {
            await UniTask.Delay(100, cancellationToken: ct);
            isend = false;
        }
    }

    /// <summary>
    /// 延迟暂停，为了避免双击放大缩小时暂停按钮显示
    /// </summary>
    /// <returns></returns>
    private async UniTaskVoid StopVideo(CancellationToken ct)
    {
        await UniTask.Delay((int)(doubleClickGap * 1000), cancellationToken: ct);
        transform.GetComponentByChildName<Image>("PauseImage").gameObject.SetActive(!playState_toggle.isOn);
        transform.GetComponentByChildName<Image>("PauseImage_Full").gameObject.SetActive(!playState_toggle.isOn);
    }

    /// <summary>
    /// 将double转换为00:00格式
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    private string ToTimeFormat(double time)
    {
        int seconds = (int)time;
        int minutes = seconds / 60;
        seconds %= 60;
        return string.Format("{0:D2}:{1:D2}", minutes, seconds);
    }

    #region 动画效果
    //protected override float joinAnimePlayTime => 0.3f;
    //protected override float exitAnimePlayTime => 0.2f;

    //public override void JoinAnim(UnityAction callback)
    //{
    //    JoinSequence.Join(DOTween.To(() => 0f, (value) => CanvasGroup.alpha = (value), 1f, JoinAnimePlayTime));
    //    JoinSequence.Join(PlayerContent.DOAnchorPos3DY(48f, JoinAnimePlayTime));
    //    base.JoinAnim(callback);
    //}

    //public override void ExitAnim(UnityAction callback)
    //{
    //    ExitSequence.Join(DOTween.To(() => 1f, (value) => CanvasGroup.alpha = (value), 0f, ExitAnimePlayTime));
    //    ExitSequence.Join(PlayerContent.DOAnchorPos3DY(-48f, ExitAnimePlayTime));
    //    base.ExitAnim(callback);
    //}
    #endregion
}

/// <summary>
/// 视频显示模块的数据类
/// </summary>
public class ShowExamModuleData : UIData
{
    /// <summary>
    /// 视频标题
    /// </summary>
    public string title;
    /// <summary>
    /// 视频地址
    /// </summary>
    public string url;
    /// <summary>
    /// 文件类型
    /// </summary>
    public string docType;

    /// <summary>
    /// 构造方法，传入完整url，系统会自动处理视频存储对应的路径，然后调用相应的视频展示
    /// </summary>
    /// <param name="id"></param>
    /// <param name="title"></param>
    /// <param name="url"></param>
    /// <param name="docType">目前只支持MP4</param>
    /// <param name="closedAction"></param>
    /// <param name="showClose"></param>
    public ShowExamModuleData(string title, string url, string docType)
    {
        string[] strs = url.Split('/');

        if (strs.Length > 1)
        {
            url = strs[0];
        }

        for (int i = 1; i < strs.Length - 1; i++)
        {
            url += "/" + strs[i];
        }

        this.title = title;
        this.url = url;
        this.docType = docType;
    }
}
