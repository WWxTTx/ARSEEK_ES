using UnityEngine;
using UnityFramework.Runtime;

/// <summary>
/// 音效管理器
/// </summary>
public class SoundManager : Singleton<SoundManager>
{
    /// <summary>
    /// 按钮点击音效
    /// </summary>
    public static string ButtonClick = "ButtonClick";

    public float volume
    {
        get
        {
            return m_mainSound.volume;
        }
        set
        {
            m_mainSound.volume = value;
            m_effectSound.volume = value;
            PlayerPrefs.SetFloat(GlobalInfo.volumeCacheKey, value);
        }
    }

    private string ResourceDir = "Sounds";
    private AudioSource m_mainSound;
    private AudioSource m_effectSound;
    /// <summary>
    /// 是否正在播放音效 防止音效重叠
    /// </summary>
    private bool played;
    /// <summary>
    /// 正在播放音效的时长
    /// </summary>
    private float playedDuration;

    protected override void InstanceAwake()
    {
        var volume = PlayerPrefs.GetFloat(GlobalInfo.volumeCacheKey, 1);
        {
            m_mainSound = gameObject.AddComponent<AudioSource>();
            m_mainSound.loop = false;
            m_mainSound.volume = volume;

            m_effectSound = gameObject.AddComponent<AudioSource>();
            m_effectSound.loop = false;
            m_effectSound.volume = volume;
        }
    }

    /// <summary>
    /// Play main sound
    /// </summary>
    public void PlayAudio(AudioClip clip)
    {
        m_mainSound.clip = clip;
        m_mainSound.Play();
    }

    /// <summary>
    /// Stop playing main sound
    /// </summary>
    public void StopAudio()
    {
        m_mainSound.Stop();
        m_mainSound.clip = null;
    }

    /// <summary>
    /// Play sound effect OneShot
    /// </summary>
    /// <param name="audioName"></param>
    /// <param name="waitLastOne">是否等当前正在播放的音效完成后再播放</param>
    /// <param name="loop"></param>
    public void PlayEffect(string audioName, bool waitLastOne = false, bool loop = false)
    {
        string path;
        if (string.IsNullOrEmpty(ResourceDir))
            path = audioName;
        else
            path = ResourceDir + "/" + audioName;

        AudioClip clip = ResLoad.Instance.Load<AudioClip>(path);
        if (loop)
        {
            m_effectSound.clip = clip;
            m_effectSound.Play();
            m_effectSound.loop = loop;
        }
        else
        {
            if (!played)
            {
                played = true;
                playedDuration = clip.length;

                m_effectSound.PlayOneShot(clip);

                this.WaitTime(clip.length, () =>
                {
                    played = false;
                });
            }
            else if (waitLastOne)            
            {
                this.WaitTime(playedDuration, () => PlayEffect(audioName));
            }
        }
    }

    /// <summary>
    /// Stop playing sound effect
    /// </summary>
    public void StopEffect()
    {
        m_effectSound.Stop();
        m_effectSound.clip = null;
    }
}