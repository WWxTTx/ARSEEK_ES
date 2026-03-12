using System;
using System.Collections;
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

/// <summary>
/// 播放视频模块
/// </summary>
public class ShowExamVideoModule : UIModuleBase
{
    #region 所需子物体
    //视频播放组件
    private VideoPlayer ShowVideo;

    //小屏和全屏的左下角播放开关
    private Toggle playState_toggle;
    private Toggle fullPlayState_toggle;

    //小屏和全屏的进度条
    private Slider progressBar_slider;
    private Slider fullProgressBar_slider;

    //小屏和全屏的播放时间（这是总时间）
    private Text totalDuration_text;
    private Text fullTotalDuration_text;

    //全屏状态下，包含进度条，播放开关，时间等的父物体
    private RectTransform fullUnder_rectTransform;
    //小屏界面
    private RectTransform videoContent_rectTransform;
    #endregion

    //视频播放使用的RenderTexture
    private RenderTexture videoRenderTexture;

    //用于控制小屏界面双击和全屏界面双击
    private float buttonTimer;
    private float fullButtonTimer;
    //小于双击间隔就会触发全屏或者缩小
    private float doubleClickGap = 0.3f;

    //判断是否播放完毕
    private bool isend = false;

    //控制小屏界面拖动
    private bool isDrag = false;
    private Vector2 dragOffset;

    //用于全屏状态下计时，时间到了就隐藏下方界面
    private float exitProgressTimer;
    private float exitProgressGap = 3f;

    private double rawVideoLength = 0;

    public ShowExamModuleData VideoModuleData { get; private set; }

    /// <summary>
    /// 初始化
    /// </summary>
    private void Init()
    {
        #region 查找子物体
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

        //设置布局
        transform.GetComponentByChildName<Text>("Title").text = VideoModuleData.title;

        #region 播放视频相关
        //设置屏幕Raw Image
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

        #region 设置各个按钮，开关的效果
        //（小界面）单机暂停，双击全屏
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
        //（全屏）单机暂停，双击退出全屏
        transform.GetComponentByChildName<Button>("VideoScreenButton_Full").onClick.AddListener(() =>
        {
            //单击
            playState_toggle.isOn = !playState_toggle.isOn;


            //双击
            if (Time.time - fullButtonTimer < doubleClickGap)//双击推出全屏
            {
                ExitFullScreen();
            }
            fullButtonTimer = Time.time;
        });

        //关闭视频界面按钮
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

        //暂停和播放开关
        playState_toggle.onValueChanged.AddListener((state) =>
        {
            if (state)
            {
                StopAllCoroutines();
                StartCoroutine(StartVideo());
            }
            else
            {
                StopAllCoroutines();
                StartCoroutine(StopVideo());
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
        //（全屏）暂停和播放开关
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
            fullPlayState_toggle.transform.GetComponentByChildName<Image>("Image").color = new Vector4(1, 1, 1,40f / 255f);
        });
        fullPlayState_toggle.gameObject.AddComponent<EventTrigger>().AddEvent(EventTriggerType.PointerExit, (arg) =>
        {
            fullPlayState_toggle.transform.GetComponentByChildName<Image>("Image").color = new Vector4(1, 1, 1, 1f / 255f);
        });
        #endregion

        #region 设置进度条
        //(小屏)进度条拖动控制
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
                StartCoroutine(StartVideo());
            }
        });
        //(大屏)进度条拖动控制
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
                StartCoroutine(StartVideo());
            }
        });
        #endregion

        //设置小界面可拖动
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

        //进度条
        if (progressBar_slider != null && ShowVideo.isPlaying)
        {
            //设置显示时间
            //Debug.Log(ShowVideo.length);
            totalDuration_text.text = "/" + ToTimeFormat(rawVideoLength);
            totalDuration_text.GetComponentByChildName<Text>("Now").text = ToTimeFormat(ShowVideo.time) ;

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

        //播放完暂停
        if (ShowVideo.length != 0 && progressBar_slider.value == 1 && playState_toggle.isOn && ShowVideo.isPlaying && !isend)
        {
            isend = true;
            playState_toggle.isOn = false;
            ShowVideo.Stop();
        }

        //拖动视频界面
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
    /// 当父物体隐藏时，销毁视频界面
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

    public void Destroy()
    {
        Close();
    }

    /// <summary>
    /// 延迟播放，为了配合双击放大
    /// </summary>
    /// <returns></returns>
    private IEnumerator StartVideo()
    {
        yield return new WaitForSeconds(doubleClickGap);
        ShowVideo.Play();

        transform.GetComponentByChildName<Image>("PauseImage").gameObject.SetActive(!playState_toggle.isOn);
        transform.GetComponentByChildName<Image>("PauseImage_Full").gameObject.SetActive(!playState_toggle.isOn);

        if (isend)
        {
            yield return new WaitForSeconds(0.1f);
            isend = false;
        }
    }

    /// <summary>
    /// 延迟暂停，为了双击放大和缩小时暂停按钮不显示
    /// </summary>
    /// <returns></returns>
    private IEnumerator StopVideo()
    {
        yield return new WaitForSeconds(doubleClickGap);
        transform.GetComponentByChildName<Image>("PauseImage").gameObject.SetActive(!playState_toggle.isOn);
        transform.GetComponentByChildName<Image>("PauseImage_Full").gameObject.SetActive(!playState_toggle.isOn);
    }

    /// <summary>
    /// 把double转换为00：00格式
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

    #region 动效
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
/// 用于显示考核界面的数据
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
    /// 用于输入全名url的情况，视频存储名应该是数字名（现用于阅卷界面视频展示）
    /// </summary>
    /// <param name="id"></param>
    /// <param name="title"></param>
    /// <param name="url"></param>
    /// <param name="docType">现在只支持MP4</param>
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