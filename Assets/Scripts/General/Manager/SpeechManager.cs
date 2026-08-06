using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;
using Text = UnityEngine.UI.Text;

public class SpeechManager : Singleton<SpeechManager>
{
    public AudioSource audioSource;

    private string currentStepId;
    private TipType currentTipType;

    /// <summary>
    /// 每次 DoSpeech 递增，回调时校验，防止异步加载覆盖新播放
    /// </summary>
    private int playId;

    /// <summary>
    /// 当前播放的提示语音剩余时间（仅当播放的是StepName/StepComplete时有效，Tips返回0）
    /// </summary>
    public float PromptVoiceRemainingTime
    {
        get
        {
            if (!SpeechMode || audioSource == null || audioSource.clip == null || !audioSource.isPlaying)
                return 0;
            if (currentTipType == TipType.Tips)
                return 0;
            return audioSource.clip.length - audioSource.time + 1;
        }
    }

    private void Update()
    {
        if (audioSource.isPlaying)
            audioSource.volume = PlayerPrefs.GetFloat(GlobalInfo.volumeCacheKey, 1f);
    }

    private void Start()
    {
        if (!PlayerPrefs.HasKey(GlobalInfo.courseVoice))
        {
            PlayerPrefs.SetInt(GlobalInfo.courseVoice, 1);
            GlobalInfo.UpdateSpeechMode();
            Log.Debug("语音模式" + SpeechMode);
        }
    }

    /// <summary>
    /// 字幕文本组件
    /// </summary>
    public Text subTitleText;
    public GameObject subTitleBackground;
    public Dictionary<string, Dictionary<TipType, List<SpeechData>>> StepSpeechData;

    /// <summary>
    /// 音频是否正在播放
    /// </summary>
    public bool IsAudioPlaying { get { return audioSource.isPlaying; } }

    public bool SpeechMode;
    public static int EncyclopediaId;
    public Sprite InfoBackground;
    public Font InfoFont;
    public Color InfoFontColor;
    public int InfoFontSize = 28;
    public KeyCode ShowInfoKey = KeyCode.Space;

    /// <summary>
    /// 单行最大显示字数
    /// </summary>
    private int CharPerLine = 36;
    /// <summary>
    /// 语速：单个字符秒数
    /// </summary>
    private float SecPerChar = 0.22f;//对应网页-1 其他值未测量
    /// <summary>
    /// pausePunctuations停顿时长
    /// </summary>
    private float PauseTime = 0.25f;
    /// <summary>
    /// 用于断句的标点符号
    /// </summary>
    private readonly List<char> punctuations = new List<char>() { '，', '。', '、', '；', ','};
    /// <summary>
    /// 语音生成时会带有停顿的符号
    /// </summary>
    private readonly List<char> pausePunctuations = new List<char>() { '，', '、', '。' };
    /// <summary>
    /// 特殊延时标记 当前网页端生成语音时未使用该功能
    /// </summary>
    private Dictionary<string, float> specialSymbols = new Dictionary<string, float>();

    public void LoadData()
    {
        GlobalInfo.UpdateSpeechMode();
        // 如果语音模式开启且不在考核模式，加载语音数据
        if (SpeechMode && GlobalInfo.currentWiki != null)
        {
            if (EncyclopediaId != GlobalInfo.currentWiki.id)
            {
                StepSpeechData = null;
                RequestManager.Instance.GetSpeechList(GlobalInfo.currentWiki.id, (data) =>
                {
                    SaveData(data);
                }, errorMsg =>
                {
                    Debug.LogError("获取百科语音失败");
                });
            }
        }
    }

    public void SaveData(List<SpeechData> pediaSpeechData)
    {
        StepSpeechData = new Dictionary<string, Dictionary<TipType, List<SpeechData>>>();

        var stepData = pediaSpeechData.GroupBy(data => data.stepId);

        EncyclopediaId = GlobalInfo.currentWiki.id;

        foreach (var step in stepData)
        {
            //stepId = int.Parse(step.Key);
            StepSpeechData.Add(step.Key, new Dictionary<TipType, List<SpeechData>>());
            foreach (var data in step)
            {
                TipType tipType = data.Type();
                if (StepSpeechData[step.Key].ContainsKey(tipType))
                    StepSpeechData[step.Key][tipType].Add(data);
                else
                    StepSpeechData[step.Key].Add(tipType, new List<SpeechData>() { data });
            }
        }
    }

    UnityAction<SpeechData> onDataFetched;
    UnityAction onComplete;
    [System.Obsolete("Use RegisterTipDisplay instead")]
    public void SetTipUI(UnityAction<SpeechData> onDataFetched, UnityAction onComplete)
    {
        this.onDataFetched = onDataFetched;
        this.onComplete = onComplete;
    }

    private CanvasGroup tipCanvasGroup;
    private Text tipText;
    public void RegisterTipDisplay(CanvasGroup canvasGroup, Text text)
    {
        tipCanvasGroup = canvasGroup;
        tipText = text;
    }



    public async UniTaskVoid RePlayStart(string ID, int index, TipType tipType, CancellationToken ct)
    {
        try
        {
            await UniTask.Delay(800, cancellationToken: ct);
            await UniTask.WaitUntil(() => !IsAudioPlaying, cancellationToken: ct);
            await UniTask.Delay(200, cancellationToken: ct);
            lasttype = tipType;
            PlayImmediate(ID, index, tipType);
        }
        finally
        {
            if (nextCts?.Token == ct)
                nextCts = null;
        }
    }

    void OnDestroy()
    {
        waitStepCts?.Cancel();
        waitStepCts?.Dispose();
        waitStepCts = null;
        Cancell();
    }

    private CancellationTokenSource waitStepCts;

    /// <summary>
    /// 有限等待语音准备
    /// </summary>
    /// <param name="stepId"></param>
    /// <param name="index"></param>
    /// <param name="tipType"></param>
    /// <returns></returns>
    public async UniTaskVoid WaitStepSpeechData(string stepId, int index, TipType tipType)
    {
        waitStepCts?.Cancel();
        waitStepCts?.Dispose();
        waitStepCts = new CancellationTokenSource();

        var linked = CancellationTokenSource.CreateLinkedTokenSource(
            waitStepCts.Token,
            this.GetCancellationTokenOnDestroy()
        );

        try
        {
            await UniTask.WaitUntil(
                () => StepSpeechData != null,
                cancellationToken: linked.Token
            ).Timeout(TimeSpan.FromSeconds(5));
        }
        catch (Exception)
        {
            return;
        }
        finally
        {
            linked.Dispose();
        }

        PlayImmediate(stepId, index, tipType);
    }

    /// <summary>
    /// 语音播放的前置条件检查
    /// </summary>
    /// <param name="stepIndex">stepIndex</param>
    /// <param name="index">步骤内index</param>
    /// <param name="isAuto">是否自动触发</param>
    TipType lasttype = TipType.StepName;
    CancellationTokenSource nextCts;

    /// <summary>
    /// StepComplete 播放时间戳，用于判断下一步 StepName 是否应排队等待
    /// 超过窗口期（手动跳转步骤）则立即打断
    /// </summary>
    private float stepCompleteTime;
    private const float StepCompleteWindow = 3f;

    public SpeechData GetSpeechData(string stepId, int index, TipType tipType)
    {
        if (GlobalInfo.currentWiki == null) return null;
        stepId = "BK" + GlobalInfo.currentWiki.id + stepId.Substring(6, stepId.Length - 6);

        if (StepSpeechData!= null && StepSpeechData.ContainsKey(stepId))
        {
            if (StepSpeechData[stepId].TryGetValue(tipType, out List<SpeechData> data))
            {
                if (index >= 0 && index < data.Count)
                    return data[index];
            }
        }
        return null;
    }

    /// <summary>
    /// 播放语音
    /// </summary>
    /// <param name="speechData"></param>
    /// <param name="tipType"></param>
    private void DoSpeech(SpeechData speechData, TipType tipType)
    {
        lasttype = tipType;
        if (tipType == TipType.StepComplete)
            stepCompleteTime = Time.realtimeSinceStartup;
        int currentPlayId = ++playId;
        if (tipType == TipType.Tips)
        {
            onDataFetched?.Invoke(speechData);
            if (tipText != null)
            {
                tipText.text = speechData.text.Replace(" ", " ");
                LayoutRebuilder.ForceRebuildLayoutImmediate(tipText.rectTransform);
                tipCanvasGroup?.DOFade(1f, 0);
            }
        }

        LoadLocalAsset.Instance.LoadAudio(speechData.audioUrl, audioClip =>
        {
            if (currentPlayId != playId)
                return;

            audioSource.clip = audioClip;
            audioSource.volume = PlayerPrefs.GetFloat(GlobalInfo.volumeCacheKey, 1f);
            audioSource.Play();

            // 直播模式下将TTS音频数字直送麦克风编码器，使观众端也能听到提示语音
            if (GlobalInfo.IsLiveMode() && GlobalInfo.IsOperator())
            {
                byte[] ttsBytes = ConvertClipToPCM(audioClip);
                if (ttsBytes != null && ttsBytes.Length > 0)
                    NetworkManager.Instance.FeedTtsAudio(ttsBytes);
                else
                    Debug.LogWarning("[TTS语音] ConvertClipToPCM返回空数据");
            }

            _cts = new CancellationTokenSource();
            MultipleLineAsync(speechData.text, _cts.Token).Forget();

        }, AudioType.MPEG);
    }

    private CancellationTokenSource _cts;
    private void Cancell()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    /// <summary>
    /// 将AudioClip转为11025Hz mono Int16 PCM字节数组，用于数字直送麦克风编码器
    /// </summary>
    private byte[] ConvertClipToPCM(AudioClip clip)
    {
        if (clip == null) return null;

        int dstSampleRate = 11025;
        float[] srcSamples = new float[clip.samples * clip.channels];
        clip.GetData(srcSamples, 0);

        int srcLength = clip.samples;
        int dstLength = Mathf.RoundToInt((float)srcLength * dstSampleRate / clip.frequency);
        byte[] bytes = new byte[dstLength * 2];
        float ratio = (float)clip.frequency / dstSampleRate;
        int srcChannels = clip.channels;

        for (int i = 0; i < dstLength; i++)
        {
            float srcIndex = i * ratio;
            int idx0 = (int)srcIndex;
            int idx1 = Mathf.Min(idx0 + 1, srcLength - 1);
            float frac = srcIndex - idx0;

            float s0 = 0f, s1 = 0f;
            for (int ch = 0; ch < srcChannels; ch++)
            {
                s0 += srcSamples[idx0 * srcChannels + ch];
                s1 += srcSamples[idx1 * srcChannels + ch];
            }
            s0 /= srcChannels;
            s1 /= srcChannels;

            short int16 = (short)Mathf.Clamp((s0 + (s1 - s0) * frac) * 32767f, -32768, 32767);
            bytes[i * 2] = (byte)(int16 & 0xff);
            bytes[i * 2 + 1] = (byte)((int16 >> 8) & 0xff);
        }

        return bytes;
    }


    /// <summary>
    /// 播放语音、设置字幕
    /// 断句优先级	标点断句 > 按字符数硬截断
    /// 连续标点处理 自动合并相邻标点如 。" 视为整体
    /// 循环内外均有 ct.ThrowIfCancellationRequested() 确保取消
    /// </summary>
    private async UniTask MultipleLineAsync(string line, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            string temp = string.Empty;
            int nextCharIndex = 0;
            List<int> punctuationIndex = new List<int>();

            // 收集所有标点位置
            for (int i = 0; i < line.Length; i++)
            {
                if (punctuations.Contains(line[i]))
                    punctuationIndex.Add(i);
            }

            // 计算理想分段数量
            int totalSegments = Mathf.CeilToInt((float)line.Length / CharPerLine);
            int idealSegmentLength = Mathf.CeilToInt((float)line.Length / totalSegments);

            while (nextCharIndex < line.Length)
            {
                ct.ThrowIfCancellationRequested();

                int segmentEnd = nextCharIndex + idealSegmentLength;
                bool hasPunctuation = false;
                int bestPunctuation = -1;
                int minDistance = int.MaxValue;

                // 查找最佳断句标点
                foreach (int puncIndex in punctuationIndex)
                {
                    if (puncIndex < nextCharIndex) continue;
                    if (puncIndex > segmentEnd + idealSegmentLength / 2) break;

                    // 计算与理想分割点的距离
                    int distance = Mathf.Abs(puncIndex - segmentEnd);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        bestPunctuation = puncIndex;
                        hasPunctuation = true;
                    }
                }

                if (hasPunctuation)
                {
                    // 包含连续标点
                    int actualEnd = bestPunctuation + 1;
                    while (actualEnd < line.Length && punctuations.Contains(line[actualEnd]))
                    {
                        actualEnd++;
                    }

                    // 确保不会超出文本范围
                    int segmentLength = Mathf.Min(actualEnd - nextCharIndex, line.Length - nextCharIndex);
                    temp = line.Substring(nextCharIndex, segmentLength);
                    nextCharIndex = actualEnd;
                }
                else
                {
                    // 动态调整硬截断长度
                    int remaining = line.Length - nextCharIndex;
                    int segmentsLeft = Mathf.CeilToInt((float)remaining / idealSegmentLength);
                    int adjustedLength = Mathf.Min(
                        Mathf.CeilToInt((float)remaining / segmentsLeft),
                        CharPerLine * 3 / 2 // 最大不超过1.5倍理想长度
                    );

                    // 尝试在空格处分割（如果有）
                    int spaceIndex = line.IndexOf(' ', nextCharIndex + adjustedLength / 2);
                    if (spaceIndex > nextCharIndex && spaceIndex < nextCharIndex + adjustedLength)
                    {
                        adjustedLength = spaceIndex - nextCharIndex;
                    }

                    temp = line.Substring(nextCharIndex, adjustedLength);
                    nextCharIndex += adjustedLength;
                }

                // 显示字幕
                SetSubTitle(temp);

                int effectiveChars = 0;
                int pauseCount = 0;
                float specialTime = 0f;

                for (int i = 0; i < temp.Length; i++)
                {
                    char c = temp[i];
                    if (pausePunctuations.Contains(c))
                        pauseCount++;
                    if (!punctuations.Contains(c))
                        effectiveChars++;
                }

                //目前网页语音生成时没有对符号进行延时
                //foreach (var symbol in specialSymbols)
                //{
                //    int count = CountOccurrences(temp, symbol.Key);
                //    specialTime += count * symbol.Value;
                //}

                float waitTime = effectiveChars * SecPerChar + pauseCount * PauseTime + specialTime;
                await UniTask.Delay(
                    (int)(waitTime * 1000),
                    DelayType.Realtime,
                    cancellationToken: ct
                );
            }
        }
        finally
        {
            if (!ct.IsCancellationRequested)
            {
                onComplete?.Invoke();
                tipCanvasGroup?.DOFade(0f, 0);
                SetSubTitle(string.Empty);
            }
        }
    }


    // 辅助方法：计算字符串出现次数
    private int CountOccurrences(string source, string value)
    {
        int count = 0;
        int index = 0;

        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) != -1)
        {
            index += value.Length;
            count++;
        }

        return count;
    }

    private void SetSubTitle(string text)
    {
        if(subTitleText!= null)
            subTitleText.text = text;
        subTitleBackground.SetActive(!string.IsNullOrEmpty(text));
    }

    public void StopSpeech()
    {
        currentStepId = null;
        currentTipType = TipType.StepName;
        lasttype = TipType.StepName;
        nextCts?.Cancel();
        nextCts?.Dispose();
        nextCts = null;
        Cancell();
        audioSource.Stop();
        subTitleText.text = "";
        subTitleBackground.SetActive(false);
    }

    /// <summary>
    /// 立即播放语音（用于用户手动选择步骤）
    /// 清除等待中的播放，打断当前播放
    /// </summary>
    public void PlayImmediate(string stepId, int index, TipType tipType)
    {
        GlobalInfo.UpdateSpeechMode();
        if (!SpeechMode)
            return;

        // 等待 StepSpeechData 初始化
        if (StepSpeechData == null)
        {
            WaitStepSpeechData(stepId, index, tipType).Forget();
            return;
        }

        // 仅在自然步骤推进时排队：StepComplete 刚播完、新请求是 StepName、且在窗口期内
        bool isNaturalNextStep = tipType == TipType.StepName
            && lasttype == TipType.StepComplete
            && Time.realtimeSinceStartup - stepCompleteTime < StepCompleteWindow;

        if (isNaturalNextStep)
        {
            if (nextCts == null)
            {
                nextCts = new CancellationTokenSource();
                RePlayStart(stepId, index, tipType, nextCts.Token).Forget();
            }
            else
            {
                nextCts.Cancel();
                nextCts.Dispose();
                nextCts = new CancellationTokenSource();
                RePlayStart(stepId, index, tipType, nextCts.Token).Forget();
            }
        }
        else
        {
            // 停止当前播放（无论什么类型）
            StopSpeech();

            currentStepId = stepId;
            currentTipType = tipType;

            // 直接播放，跳过所有 TipType 检查
            SpeechData speechData = GetSpeechData(stepId, index, tipType);
            if (speechData != null && speechData.audioUrl != null)
            {
                DoSpeech(speechData, tipType);
            }
            else
            {
                string resolvedId = "BK" + GlobalInfo.currentWiki.id + stepId.Substring(6, stepId.Length - 6);
                int availableCount = 0;
                if (StepSpeechData != null && StepSpeechData.TryGetValue(resolvedId, out var typeDict))
                {
                    if (typeDict.TryGetValue(tipType, out var list))
                        availableCount = list.Count;
                }
                Log.Debug($"语音未播放 stepId={stepId} resolvedId={resolvedId} index={index} tipType={tipType} 可用数量={availableCount} speechData={(speechData == null ? "null" : "hasUrl=" + (speechData.audioUrl != null))}");
            }
        }

      
    }
}
