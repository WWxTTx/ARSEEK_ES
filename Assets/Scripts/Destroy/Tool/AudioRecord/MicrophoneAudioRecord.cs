using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;
using UnityFramework.Runtime;

public class MicrophoneAudioRecord : MonoBehaviour
{
    /// <summary>
    /// 当前设备名
    /// </summary>
    [SerializeField]
    private string DeviceName = "";
    /// <summary>
    /// 存储路径
    /// </summary>
    [SerializeField]
    private string savePath;
    /// <summary>
    /// 是否正在录制 用于退出循环
    /// </summary>
    [SerializeField]
    private bool isRecording = false;
    /// <summary>
    /// 是否暂停
    /// </summary>
    [SerializeField]
    private bool isPause = false;
    private Thread saveThread;
    private CancellationTokenSource recordingCts;
    private AudioClip NewClip;
    private UnityAction EndEditor;
    private UnityAction<int> RecordingTime;

    /// <summary>
    /// 开始录音
    /// </summary>
    /// <param name="path">录音路径</param>
    /// <param name="timeCallBack">时间回调 用于显示录音时长</param>
    /// <param name="deviceNum">设备号 用于指定录音设备</param>
    /// <param name="deviceName">设备名 用于指定录音设备</param>
    public bool StartRecord(string path, UnityAction<int> timeCallBack = null, int deviceNum = 0, string deviceName = null)
    {
        if (!CheckDevices())
        {
            Log.Error("没有找到设备或者没有获得录音权限！");
            return false;
        }

        if (isRecording)
        {
            Log.Error("已经开始录音却多次触发，请检查相关代码！");
            return false;
        }

        if (saveThread?.ThreadState == ThreadState.Running)
        {
            Log.Error("正在存储中，却调用此方法，请检查相关代码！");
            return false;
        }

        if (deviceNum != 0) DeviceName = Microphone.devices[deviceNum];
        else if (DeviceName != null) DeviceName = deviceName;
        else DeviceName = Microphone.devices[0];

        savePath = path;
        isPause = false;
        isRecording = true;
        RecordingTime = timeCallBack;
        recordingCts?.Cancel();
        recordingCts = new CancellationTokenSource();
        Recording(recordingCts.Token).Forget();
        return true;
    }

    /// <summary>
    /// 暂停录音
    /// </summary>
    public void PauseRecord()
    {
        if (!isRecording)
        {
            Log.Error("未开始录音却触发暂停，请检查相关代码！");
            return;
        }

        if (saveThread?.ThreadState == ThreadState.Running)
        {
            Log.Error("正在存储中，却调用此方法，请检查相关代码！");
            return;
        }

        if (isPause)
        {
            Log.Error("已暂停录音却多次触发，请检查相关代码！");
            return;
        }

        isPause = true;
    }

    /// <summary>
    /// 继续录音
    /// </summary>
    public void RestartRecord()
    {
        if (!isRecording)
        {
            Log.Error("未开始录音却触发继续，请检查相关代码！");
            return;
        }

        if (saveThread?.ThreadState == ThreadState.Running)
        {
            Log.Error("正在存储中，却调用此方法，请检查相关代码！");
            return;
        }

        if (!isPause)
        {
            Log.Error("未暂停录音却触发继续，请检查相关代码！");
            return;
        }

        isPause = false;
    }

    /// <summary>
    /// 结束录音
    /// </summary>
    /// <param name="endEditor"></param>
    public void EndRecord(UnityAction endEditor = null)
    {
        isRecording = false;
        isPause = false;
        EndEditor = endEditor;
    }

    /// <summary>
    /// 线程存储录音 用于音频过大存储时过于卡顿的处理
    /// </summary>
    private void SaveFiles()
    {
        ClipToWav.Save(savePath, NewClip);
        EndEditor?.Invoke();
    }

    /// <summary>
    /// 检查设备
    /// </summary>
    /// <returns></returns>
    public bool CheckDevices()
    {
        return Microphone.devices.Length > 0;
    }

    /// <summary>
    /// 录制协程
    /// </summary>
    /// <returns></returns>
    async UniTaskVoid Recording(CancellationToken ct)
    {
        //避免多次记录
        bool save = false;
        //时间记录总秒数
        int second = 0;
        //采样频率
        int frequency = 16000;
        //每一轮录制的时间
        int loopTime = 60;
        //采样媒体长度
        int audioLength;
        //音频存储 用于获取每轮的音频
        float[] audioData;
        //音频存储 用于存储总音频
        List<float> audioDataList = new List<float>();
        AudioClip clip = Microphone.Start(DeviceName, true, loopTime, frequency);
        int index = (loopTime * frequency) - frequency / 2;

        while (isRecording)
        {
            audioLength = Microphone.GetPosition(DeviceName);
            RecordingTime?.Invoke(second + (audioLength / frequency));

            if (save)
            {
                if (audioLength > index)
                {
                    save = false;
                    Log.Debug("一轮录音循环结束");
                    audioData = new float[Microphone.GetPosition(DeviceName)];
                    clip.GetData(audioData, 0);
                    audioDataList.AddRange(audioData);
                    second += loopTime;
                }
            }
            else
            {
                if (audioLength < frequency)
                {
                    Log.Debug("新录音循环开始！");
                    save = true;
                }
            }

            if (isPause)
            {
                Log.Debug("录音暂停！进程已挂起！");
                audioData = new float[Microphone.GetPosition(DeviceName)];
                clip.GetData(audioData, 0);
                audioDataList.AddRange(audioData);
                Microphone.End(DeviceName);
                second += audioLength / frequency;
                save = true;

                while (isPause)
                    await UniTask.Yield(ct);

                clip = Microphone.Start(DeviceName, true, loopTime, frequency);
                Log.Debug("录音继续！进程已恢复！");
            }

            await UniTask.Yield(ct);
        }

        audioData = new float[Microphone.GetPosition(DeviceName)];
        clip.GetData(audioData, 0);
        audioDataList.AddRange(audioData);
        Microphone.End(DeviceName);

        NewClip = AudioClip.Create("RecordTempFile", audioDataList.Count, 1, frequency, false);
        NewClip.SetData(audioDataList.ToArray(), 0);
        Log.Debug("音频录制完成！");
        SaveFiles();
        //开启线程保存 防止假死
        //saveThread = new Thread(new ThreadStart(SaveFiles));
        //saveThread.Start();
    }

    private void OnDestroy()
    {
        recordingCts?.Cancel();
        recordingCts?.Dispose();
        if (saveThread != null)
            saveThread.Abort();
    }
}