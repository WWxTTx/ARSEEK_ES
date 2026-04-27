using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityFramework.Runtime;
using static UnityFramework.Runtime.RequestData;
using Text = UnityEngine.UI.Text;

public class SpeechManager : Singleton<SpeechManager>
{
    public AudioSource audioSource;

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

    public bool LoadData()
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
                return true;
            }
        }
        return false;
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
    public void SetTipUI(UnityAction<SpeechData> onDataFetched, UnityAction onComplete)
    {
        this.onDataFetched = onDataFetched;
        this.onComplete = onComplete;
    }



    public async UniTaskVoid RePlayStart(string ID, int index, TipType tipType, CancellationToken ct)
    {
        await UniTask.Delay(800, cancellationToken: ct);
        await UniTask.WaitUntil(() => !IsAudioPlaying, cancellationToken: ct);
        await UniTask.Delay(200, cancellationToken: ct);
        lasttype = tipType;
        PlayImmediate(ID, index, tipType);
    }

    void OnDestroy()
    {
        Cancell();
    }

    private Func<bool> stepSpeechDataReadyPredicate;

    public async UniTaskVoid WaitStepSpeechData(string stepId, int index, TipType tipType)
    {
        stepSpeechDataReadyPredicate = () => StepSpeechData != null;
        await UniTask.WaitUntil(stepSpeechDataReadyPredicate, cancellationToken: this.GetCancellationTokenOnDestroy());
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

    public SpeechData GetSpeechData(string stepId, int index, TipType tipType)
    {
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
        if (tipType == TipType.Tips)
        {
            onDataFetched?.Invoke(speechData);
        }

        LoadLocalAsset.Instance.LoadAudio(speechData.audioUrl, audioClip =>
        {
            audioSource.clip = audioClip;
            audioSource.Play();

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
        finally // 确保无论成功、取消还是异常都会执行
        {
            onComplete?.Invoke();// 清除提示字幕
            SetSubTitle(string.Empty); // 清除字幕
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
        if (LoadData())
        {
            if (StepSpeechData == null)
            {
                WaitStepSpeechData(stepId, index, tipType).Forget();
                return;
            }
        }

        if (lasttype == TipType.StepComplete)
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

            // 直接播放，跳过所有 TipType 检查
            SpeechData speechData = GetSpeechData(stepId, index, tipType);
            if (speechData != null && speechData.audioUrl != null)
            {
                DoSpeech(speechData, tipType);
            }
        }

      
    }
}
