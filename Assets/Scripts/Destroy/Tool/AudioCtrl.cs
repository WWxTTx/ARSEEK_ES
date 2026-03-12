using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 音频控制
/// </summary>
public class AudioCtrl : MonoBehaviour
{
    private AudioSource audioSource;
    private Slider slider;
    /// <summary>
    /// 进度回调
    /// </summary>
    private UnityAction<float> CallBackEvent;
    /// <summary>
    /// 是否正在播放
    /// </summary>
    private bool isPlay = false;
    /// <summary>
    /// 记录拖动前播放状态
    /// </summary>
    private bool playState = false;

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="targetAudioSource">播放器</param>
    /// <param name="targetFile">音源</param>
    /// <param name="targetSlider">进度条</param>
    /// <param name="callBack">回调 刷新时间用（Darg and pointerUp）</param>
    public void Init(AudioSource targetAudioSource, AudioClip targetFile, Slider targetSlider, UnityAction<float> callBack)
    {
        audioSource = targetAudioSource;
        audioSource.clip = targetFile;
        float Lenght = targetFile.length;

        slider = targetSlider;
        CallBackEvent = callBack;

        EventTrigger eventTrigger = slider.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerDown;
        entry.callback.AddListener((arg) =>
        {
            playState = isPlay;
            isPlay = false;
        });
        eventTrigger.triggers.Add(entry);

        entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerUp;
        entry.callback.AddListener((arg) =>
        {
            //长度超限会报错 需要限制
            var value = Mathf.Clamp(slider.value, 0, 0.99f);
            audioSource.time = value * Lenght;
            CallBackEvent.Invoke(value * Lenght);
            isPlay = playState;
        });
        eventTrigger.triggers.Add(entry);

        entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.Drag;
        entry.callback.AddListener(arg =>
        {
            slider.OnDrag((PointerEventData)arg);
            CallBackEvent.Invoke(slider.value * audioSource.clip.length);
        });
        eventTrigger.triggers.Add(entry);
    }

    /// <summary>
    /// 设置音频
    /// </summary>
    /// <param name="targetFile">音源</param>
    /// <param name="callBack">新回调 覆盖</param>
    public void SetClip(AudioClip targetFile, UnityAction<float> callBack)
    {
        isPlay = false;
        audioSource.clip = targetFile;
        audioSource.time = 0;
        slider.value = 0;
        audioSource.Stop();
        CallBackEvent = callBack;
    }

    /// <summary>
    /// 播放
    /// </summary>
    public void Play()
    {
        audioSource.Play();
        isPlay = true;
    }

    /// <summary>
    /// 暂停
    /// </summary>
    public void Pause()
    {
        audioSource.Pause();
        isPlay = false;
    }

    private void Update()
    {
        if (isPlay)
        {
            slider.value = audioSource.time / audioSource.clip.length;
            CallBackEvent.Invoke(audioSource.time);
            if (!audioSource.isPlaying)
            {
                audioSource.time = 0;
                audioSource.Play();
            }
        }

    }
}
