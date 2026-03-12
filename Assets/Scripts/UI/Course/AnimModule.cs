using System;
using System.Linq;
//using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.UI;
using DG.Tweening;
using UnityFramework.Runtime;
using static AdaptiveListModule;

/// <summary>
/// 动画百科模块
/// </summary>
public class AnimModule : UIModuleBase
{
    private CanvasGroup canvasGroup;

    /// <summary>
    /// 播放开关
    /// </summary>
    private Button PlayBtn;
    private Button PauseBtn;
    /// <summary>
    /// 进度条
    /// </summary>
    private Slider ProgressSlider;
    /// <summary>
    /// 时长
    /// </summary>
    private Text CurrentTime;
    private Text Duration;

    /// <summary>
    /// 改变透明度按钮
    /// </summary>
    private Button AlphaBut;
    private Text AlphaValue;

    private int alphaValue = 0;

    //进度同步
    private float updateInterval = 1f;
    private DateTime lastUpdateTime;
    private DateTime now;

    private bool prevPlayState = false;
    private bool isPlay = false;
    private bool isDrag = false;

    private float progress;
    private TimeSpan progressTimeSpan;

    private float dragProgress;
    private TimeSpan dragProgressTimeSpan;

    /// <summary>
    /// 动画控制脚本
    /// </summary>
    private AnimController iController;

    public List<Material> materials;


    protected override float exitAnimePlayTime => 0.3f;

    private Transform Background;
    private Vector3 _backgroundPos;

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);
        AddMsg(new ushort[] {
#if UNITY_ANDROID || UNITY_IOS
            (ushort)ARModuleEvent.Tracking,
            (ushort)ARModuleEvent.Close,
#endif
            (ushort)IntegrationModuleEvent.AlphaValue,
            (ushort)IntegrationModuleEvent.AnimSelect,
            (ushort)IntegrationModuleEvent.AnimPlay,
            (ushort)IntegrationModuleEvent.AnimFinish,
            (ushort)IntegrationModuleEvent.AnimValue,
            (ushort)IntegrationModuleEvent.CombAll,
            (ushort)RoomChannelEvent.UpdateControl
        });

        Init();
        RegisterButton();

        TapRecognizer.Instance.RegistOnRightMouseDoubleClick(ResetState);
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        TapRecognizer.Instance.RegistOnLeftMouseEmptyDoubleClick(ResetPosition);
#else
        TapRecognizer.Instance.RegistOnRightMouseEmptyClick(ResetPosition);
#endif
    }

    private void Init()
    {
#if UNITY_ANDROID || UNITY_IOS
        canvasGroup = transform.GetComponent<CanvasGroup>();
        canvasGroup.interactable = !GlobalInfo.isARTracking;
#endif
        ProgressSlider = transform.GetComponentByChildName<Slider>("ProgressSlider");
        PlayBtn = transform.GetComponentByChildName<Button>("PlayBtn");
        PauseBtn = transform.GetComponentByChildName<Button>("PauseBtn");
        CurrentTime = transform.GetComponentByChildName<Text>("CurrentTime");
        Duration = transform.GetComponentByChildName<Text>("Duration");
        AlphaBut = transform.GetComponentByChildName<Button>("AlphaBut");
        AlphaValue = transform.GetComponentByChildName<Text>("AlphaValue");
        alphaValue = 0;

        Background = transform.FindChildByName("Background");
        _backgroundPos = Background.localPosition;
    }

    private void RegisterButton()
    {
        PlayBtn.onClick.AddListener(() => ToolManager.SendBroadcastMsg(new MsgBool((ushort)IntegrationModuleEvent.AnimPlay, true), true));
        PauseBtn.onClick.AddListener(() => ToolManager.SendBroadcastMsg(new MsgBool((ushort)IntegrationModuleEvent.AnimPlay, false), true));

        EventTrigger eventTrigger = ProgressSlider.AutoComponent<EventTrigger>();
        eventTrigger.AddEvent(EventTriggerType.PointerDown, (arg) =>
        {
            if (iController.Playable == null)
                return;
            isDrag = true;
            prevPlayState = PauseBtn.gameObject.activeSelf;
            ToolManager.SendBroadcastMsg(new MsgBool((ushort)IntegrationModuleEvent.AnimPlay, false), true);
        });
        eventTrigger.AddEvent(EventTriggerType.Drag, (arg) =>
        {
            if (iController.Playable == null)
                return;
            dragProgress = (float)iController.Playable.duration * ProgressSlider.value;
            dragProgressTimeSpan = TimeSpan.FromSeconds(dragProgress);
            CurrentTime.text = string.Format("{0:D2}:{1:D2}", dragProgressTimeSpan.Minutes, dragProgressTimeSpan.Seconds);
            iController.Playable.time = dragProgress;
            iController.Playable.Evaluate();
        });
        eventTrigger.AddEvent(EventTriggerType.PointerUp, (arg) =>
        {
            if (iController.Playable == null)
                return;
            isDrag = false;
            lastUpdateTime = DateTime.Now;
            ToolManager.SendBroadcastMsg(new MsgFloat((ushort)IntegrationModuleEvent.AnimValue, ProgressSlider.value), true);
            ToolManager.SendBroadcastMsg(new MsgBool((ushort)IntegrationModuleEvent.AnimPlay, prevPlayState), true);
        });
       
        AlphaBut.onClick.AddListener(() => ToolManager.SendBroadcastMsg(new MsgInt((ushort)IntegrationModuleEvent.AlphaValue, alphaValue), true));
    }

    /// <summary>
    /// 初始化模型集成控制脚本
    /// </summary>
    /// <param name="go"></param>
    public void ChangeBaike(GameObject go)
    {
        UIManager.Instance.OpenUI<LoadingPanel>(UILevel.Loading);

        iController = go.AutoComponent<AnimController>();
        if (iController)
        {
            iController.materials = materials;
            iController.InitAnimList();

            //默认居中显示
            ModelManager.Instance.RevertCameraPose();
            ModelManager.Instance.ResetCameraPose(true);

            //默认选中第一个动画
            ChangeAnim(string.Empty);

            //打开动画列表
            SendMsg(new MsgBase((ushort)CoursePanelEvent.AnimListBtn));
        }
    }

    private void UpdateBtnUI(GameObject go)
    {
        iController.SetAlpha(-1);

        if (go == null || iController == null)
        {
            ClearAnim();
            AlphaValue.text = "透明度";
            AlphaBut.interactable = false;
            return;
        }
        else
        {
            AlphaBut.interactable = iController.canAlpha;
            if(!iController.canAlpha)
                AlphaValue.text = "透明度";
        }
    }

    /// <summary>
    /// 设置动画长度
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="info"></param>
    private void SetDuration(PlayableDirector info)
    {
        if (info == null)
            return;

        double length = info.duration;
        TimeSpan timeSpan = TimeSpan.FromSeconds(length);
        Duration.text = string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
    }

    void Update()
    {
        if (iController != null && iController.Playable != null && isPlay && !isDrag)
        {
            progress = (float)(iController.Playable.time / (float)iController.Playable.duration);
            progressTimeSpan = TimeSpan.FromSeconds(iController.Playable.time);
            ProgressSlider.value = progress;
            CurrentTime.text = string.Format("{0:D2}:{1:D2}", progressTimeSpan.Minutes, progressTimeSpan.Seconds);

            if (GlobalInfo.IsMainScreen())
            {
                now = DateTime.Now;
                if ((now - lastUpdateTime).TotalSeconds > updateInterval)
                {
                    lastUpdateTime = now;
                    NetworkManager.Instance.SendFrameMsg(new MsgFloat((ushort)IntegrationModuleEvent.AnimValue, progress));
                }
            }

            if (progress > 0.99f)
            {
                ResetAnim();
                //确保状态同步
                if (GlobalInfo.IsMainScreen())
                    ToolManager.SendBroadcastMsg(new MsgBool((ushort)IntegrationModuleEvent.AnimFinish, false));

            }
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
                canvasGroup.blocksRaycasts = !tracking;
                canvasGroup.DOFade(tracking ? 0 : 1, 0.3f);
                break;
            case (ushort)ARModuleEvent.Close:
                ModelManager.Instance.AdaptModelRestrict(iController.gameObject);
                ModelManager.Instance.RevertCameraPose();
                ModelManager.Instance.ResetCameraPose(true, false, () =>
                {
                    NetworkManager.Instance.SendFrameMsg(new MsgBase((ushort)GazeEvent.SyncCamera));
                });
                break;
#endif              
            case (ushort)IntegrationModuleEvent.AnimSelect:
                string animProp = ((MsgString)msg).arg;
                ChangeAnim(animProp);
                break;
            case (ushort)IntegrationModuleEvent.AnimPlay:
                if (iController.Playable)
                {
                    bool isPlay = ((MsgBrodcastOperate)msg).GetData<MsgBool>().arg1;
                    PlayBtn.gameObject.SetActive(!isPlay);
                    PauseBtn.gameObject.SetActive(isPlay);
                    if (isPlay)
                        iController.Playable?.Play();
                    else
                        iController.Playable?.Pause();

                    this.isPlay = isPlay;
                    iController.IsAnimPlay = isPlay;
                }
                break;
            case (ushort)IntegrationModuleEvent.AnimValue:
                if (iController.Playable)
                {
                    float progress = ((MsgBrodcastOperate)msg).GetData<MsgFloat>().arg;
                    iController.Playable.time = progress * iController.Playable.duration;
                    iController.Playable.Evaluate();

                    progressTimeSpan = TimeSpan.FromSeconds(iController.Playable.time);
                    ProgressSlider.value = progress;
                    CurrentTime.text = string.Format("{0:D2}:{1:D2}", progressTimeSpan.Minutes, progressTimeSpan.Seconds);
                }
                break;
            case (ushort)IntegrationModuleEvent.AnimFinish:
                if (isPlay)
                    ResetAnim();
                break;
            case (ushort)IntegrationModuleEvent.AlphaValue:
                if (iController.canAlpha)
                {
                    int alphaValue = ((MsgBrodcastOperate)msg).GetData<MsgInt>().arg;
                    iController.SetAlpha(alphaValue);
                    ChangeAlphaValue(alphaValue);
                }
                break;
            case (ushort)IntegrationModuleEvent.CombAll:
                ClearLocalState(()=>
                {
                    ModelManager.Instance.AdaptModelRestrict(gameObject);
                    ModelManager.Instance.RevertCameraPose();
                    ModelManager.Instance.ResetCameraPose(true, false, () =>
                    {
                        FormMsgManager.Instance.SendMsg(new MsgBool((ushort)HierarchyEvent.Interactable, true));
                        NetworkManager.Instance.IsIMSync = true;
                    });
                });
                break;
            case (ushort)RoomChannelEvent.UpdateControl:
                MsgIntBool msgIntBool = (MsgIntBool)msg;
                if (msgIntBool.arg1 == GlobalInfo.account.id)
                {
                    ClearLocalState();
                }
                break;
        }
    }

    /// <summary>
    /// 清除本地UI状态  
    /// 退出动画、观察模式, 关闭知识点 同步主画面
    /// </summary>
    /// <param name="controlKnowledge"></param>
    /// <param name="callback"></param>
    private void ClearLocalState(UnityAction callback = null)
    {
        UpdateBtnUI(null);
        iController.currentAlphaIndex = -1;
        ClearAnim();
        callback?.Invoke();       
    }

    /// <summary>
    /// 修改透明度值
    /// </summary>
    /// <param name="value"></param>
    private void ChangeAlphaValue(int value)
    {
        switch (value)
        {
            case 0:
                AlphaValue.text = "透明度50%";
                alphaValue = 1;
                break;
            case 1:
                AlphaValue.text = "透明度90%";
                alphaValue = -1;
                break;
            case -1:
                AlphaValue.text = "透明度0%";
                alphaValue = 0;
                break;
        }
    }

    /// <summary>
    /// 切换选中动画
    /// </summary>
    private void ChangeAnim(string animProp)
    {
        AlphaBut.interactable = iController.canAlpha;
        if (iController.canAlpha)
        {
            alphaValue = iController.currentAlphaIndex == -1 ? 1 : iController.currentAlphaIndex;
            iController.SetAlpha(alphaValue);
            ChangeAlphaValue(alphaValue);
        }
        else
        {
            AlphaValue.text = "透明度";
        }

        ClearAnim();
        iController.SelectAnime(animProp);
        SetDuration(iController.Playable);
        ProgressSlider.value = 0f;

        SelectID = iController.CurrentPlayableProp;
    }

    /// <summary>
    /// 重置动画状态
    /// </summary>
    private void ResetAnim()
    {
        PlayBtn.gameObject.SetActive(true);
        PauseBtn.gameObject.SetActive(false);
        ProgressSlider.value = 0f;

        CurrentTime.text = "00:00";
#if UNITY_ANDROID || UNITY_IOS
        CurrentTime.gameObject.SetActive(false);
        Duration.gameObject.SetActive(false);
#endif
        if (iController.Playable)
        {
            iController.Playable.initialTime = 0;
            iController.Playable.time = 0;
            iController.Playable.Evaluate();
            iController.Playable.Pause();
        }
        isPlay = false;
    }

    /// <summary>
    /// 清除当前动画状态
    /// </summary>
    private void ClearAnim()
    {
        PlayBtn.gameObject.SetActive(true);
        PauseBtn.gameObject.SetActive(false);
        CurrentTime.text = "00:00";

        if (iController.Playable)
        {
            iController.Playable.Pause();
        }
        iController.DeselectAnime();
    }

    private void ResetState()
    {
        if (iController != null)
            return;
        ToolManager.SendBroadcastMsg(new MsgBase((ushort)IntegrationModuleEvent.CombAll), true);
    }

    private void ResetPosition()
    {
        ModelManager.Instance.RevertCameraPose();
        ModelManager.Instance.ResetCameraPose();
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        TapRecognizer.Instance?.UnRegistOnRightMouseDoubleClick(ResetState);
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        TapRecognizer.Instance?.UnRegistOnLeftMouseEmptyDoubleClick(ResetPosition);
#else
        TapRecognizer.Instance?.UnRegistOnRightMouseEmptyClick(ResetPosition);
#endif
        base.Close(uiData, callback);
    }

    /// <summary>
    /// 进场动画
    /// </summary>
    /// <param name="callback">回调</param>
    public override void JoinAnim(UnityAction callback)
    {
        var pos = new Vector3(_backgroundPos.x, _backgroundPos.y - 50f, _backgroundPos.z);
        Background.localScale = Vector3.one * 0.001f;
        JoinSequence.Append(Background.DOLocalMoveY(_backgroundPos.y, JoinAnimePlayTime).SetEase(Ease.Linear));
        JoinSequence.Join(Background.DOScale(Vector3.one, JoinAnimePlayTime).SetEase(Ease.Linear));
        base.JoinAnim(callback);
    }

    /// <summary>
    /// 退场动画
    /// </summary>
    /// <param name="callback">回调</param>
    public override void ExitAnim(UnityAction callback)
    {
        ExitSequence.Append(Background.DOLocalMoveY(_backgroundPos.y - 50f, ExitAnimePlayTime));
        ExitSequence.Join(Background.DOScale(Vector3.zero, ExitAnimePlayTime));
        base.ExitAnim(callback);
    }
}