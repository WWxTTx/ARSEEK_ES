using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using RenderHeads.Media.AVProMovieCapture;
using UnityFramework.Runtime;

public class MicEncoderWithAudioFilter : MonoBehaviour
{
#if !UNITY_WEBGL || UNITY_EDITOR
    //----------------------------------------------
    AudioSource AudioMic;
    private Queue<byte> AudioBytes = new Queue<byte>();

    private Queue<Queue<byte>> CachedAudio;
    Queue<byte> tempCache = new Queue<byte>();

    public MicDeviceMode DeviceMode = MicDeviceMode.Default;
    public string TargetDeviceName = "MacBook Pro Microphone";
    string CurrentDeviceName = null;

    [TextArea]
    public string DetectedDevices;

    //[Header("[Capture In-Game Sound]")]
    public bool StreamGameSound = true;
    public int OutputSampleRate = 11025;
    public int OutputChannels = 1;
    private object _asyncLockAudio = new object();
    private object _asyncLockCachedAudio = new object();

    private int CurrentAudioTimeSample = 0;
    private int LastAudioTimeSample = 0;
    //----------------------------------------------

    [Range(1, 60)]
    public int StreamFPS = 20;
    private float interval = 0.05f;

    public bool UseHalf = true;
    public bool GZipMode = false;

    public UnityEventByteArray OnDataByteReadyEvent = new UnityEventByteArray();
    public UnityEventString OnDataStringReadyEvent = new UnityEventString();

    //[Header("Pair Encoder & Decoder")]
    public int label = 2001;
    private int dataID = 0;
    private int maxID = 1024;
    private int chunkSize = 8096; //32768;
    private float next = 0f;
    private bool stop = false;

    public int dataLength;
    public int cachedLength;

    private int emptyPacket = 32;

    private int emptyFrameCount = 0; // TTS连续空帧计数，超时自动停止SenderCOR
    private bool ttsActive = false; // TTS是否有数据正在发送中（与麦克风完全独立）
    private int ttsBytesPending = 0; // TTS待发送字节计数，喂入+，消费-，归零时触发自动停止

    public float threshold;
    public float clipPeek;

    private float lastPeakTime;
    public float silentTime;

    private bool useCache;
    public float cacheTime;

    public int cacheCount;

    // Use this for initialization

    private bool micStartSuccess = false;
    public bool MicStartSuccess => micStartSuccess;

    private OnAudioFilterReadForwarder audioFilterReadForwarder;

    public UnityEvent<OnAudioFilterReadForwarder> OnMicInitSuccess = new UnityEvent<OnAudioFilterReadForwarder>();

    /// <summary>
    /// 初始化麦克风
    /// </summary>
    public void InitMic()
    {
        if (micStartSuccess)
            return;

#if UNITY_ANDROID
        this.GetPermission(PermissionManager.Request.麦克风, arg =>
        {
            switch (arg)
            {
                case PermissionManager.Result.已授权:
                    StartCoroutine(_InitMic());
                    break;
                case PermissionManager.Result.未授权:
                case PermissionManager.Result.未授权且不再询问:
                    break;
            }
        });
#else
        StartCoroutine(_InitMic());
#endif
    }

    IEnumerator _InitMic()
    {
        if (AudioMic == null) AudioMic = GetComponent<AudioSource>();
        if (AudioMic == null) AudioMic = gameObject.AddComponent<AudioSource>();

        //Check Target Device
        DetectedDevices = "";
        string[] MicNames = Microphone.devices;

        if (MicNames == null || MicNames.Length == 0)
        {
            FormMsgManager.Instance.SendMsg(new MsgBase((ushort)MediaChannelEvent.MicError));
            yield break;
        }

        foreach (string _name in MicNames) DetectedDevices += _name + "\n";
        if (DeviceMode == MicDeviceMode.TargetDevice)
        {
            bool IsCorrectName = false;
            for (int i = 0; i < MicNames.Length; i++)
            {
                if (MicNames[i] == TargetDeviceName)
                {
                    IsCorrectName = true;
                    break;
                }
            }
            if (!IsCorrectName) TargetDeviceName = null;
        }

        CurrentDeviceName = DeviceMode == MicDeviceMode.Default ? null : TargetDeviceName;

        AudioMic.clip = Microphone.Start(CurrentDeviceName, true, 1, OutputSampleRate);
        if(AudioMic.clip == null)
        {
            FormMsgManager.Instance.SendMsg(new MsgBase((ushort)MediaChannelEvent.MicError));
            yield break;
        }
        AudioMic.loop = true;
        while (!(Microphone.GetPosition(CurrentDeviceName) > 0))
        {
            yield return null;
        }

        micStartSuccess = true;
        Debug.Log("Start Mic(pos): " + Microphone.GetPosition(CurrentDeviceName));

        AudioMic.Play();

        audioFilterReadForwarder = gameObject.AddComponent<OnAudioFilterReadForwarder>();
        audioFilterReadForwarder.Streaming = false;
        audioFilterReadForwarder._MuteBehaviour = OnAudioFilterReadForwarder.MuteBehaviour.After;

        OnMicInitSuccess?.Invoke(audioFilterReadForwarder);

        OutputChannels = AudioMic.clip.channels;
    }

    public void StartCapture(bool startMic = true)
    {
        if (stop)
        {
            stop = false;
            emptyFrameCount = 0;
            if (audioFilterReadForwarder != null)
                audioFilterReadForwarder.Streaming = true;

            ReAllocateCache();

            StartCoroutine(SenderCOR());
            if (micStartSuccess && startMic)
                StartCoroutine(CaptureMic());
        }
    }

    public void StopCapture()
    {
        stop = true;
        ttsActive = false;
        ttsBytesPending = 0;
        if (audioFilterReadForwarder)
            audioFilterReadForwarder.Streaming = false;

        if (CachedAudio != null)
            CachedAudio.Clear();

        StopCoroutine(SenderCOR());
        StopCoroutine(CaptureMic());
    }

    void ReAllocateCache()
    {
        useCache = cacheTime > 0;
        if (useCache)
        {
            lock (_asyncLockCachedAudio)
            {
                cacheCount = Mathf.RoundToInt(StreamFPS * cacheTime);
                CachedAudio = new Queue<Queue<byte>>(cacheCount);
            }
        }
    }

    IEnumerator CaptureMic()
    {
        while (!stop)
        {
            AddMicData();
            yield return null;
        }
        yield return null;
    }

    private Int16 FloatToInt16(float inputFloat)
    {
        inputFloat *= 32767;
        if (inputFloat < -32768) inputFloat = -32768;
        if (inputFloat > 32767) inputFloat = 32767;
        return Convert.ToInt16(inputFloat);
    }

    void AddMicData()
    {
        tempCache.Clear();
        LastAudioTimeSample = CurrentAudioTimeSample;
        //CurrentAudioTimeSample = AudioMic.timeSamples;
        CurrentAudioTimeSample = Microphone.GetPosition(CurrentDeviceName);

        if (CurrentAudioTimeSample != LastAudioTimeSample)
        {
            float[] samples = new float[AudioMic.clip.samples];
            AudioMic.clip.GetData(samples, 0);

            //采集样本峰值低于threshold且持续时间达到silentTime(s)时不再进行传输
            if (threshold > 0 && silentTime > 0)
            {
                clipPeek = GetClipPeak(samples);
                if (clipPeek < threshold)
                {
                    if (Time.realtimeSinceStartup - lastPeakTime > silentTime)
                    {
                        if (useCache)
                        {
                            if (CachedAudio.Count > cacheCount)
                                CachedAudio.Dequeue();
                            AddCacheMicData(samples);
                        }
                        return;
                    }
                }
                else
                {
                    lastPeakTime = Time.realtimeSinceStartup;
                }
            }


            if (CurrentAudioTimeSample > LastAudioTimeSample)
            {
                lock (_asyncLockAudio)
                {
                    for (int i = LastAudioTimeSample; i < CurrentAudioTimeSample; i++)
                    {
                        byte[] byteData = BitConverter.GetBytes(FloatToInt16(samples[i]));
                        foreach (byte _byte in byteData) AudioBytes.Enqueue(_byte);
                    }
                }
            }
            else if (CurrentAudioTimeSample < LastAudioTimeSample)
            {
                lock (_asyncLockAudio)
                {
                    for (int i = LastAudioTimeSample; i < samples.Length; i++)
                    {
                        byte[] byteData = BitConverter.GetBytes(FloatToInt16(samples[i]));
                        foreach (byte _byte in byteData) AudioBytes.Enqueue(_byte);
                    }
                    for (int i = 0; i < CurrentAudioTimeSample; i++)
                    {
                        byte[] byteData = BitConverter.GetBytes(FloatToInt16(samples[i]));
                        foreach (byte _byte in byteData) AudioBytes.Enqueue(_byte);
                    }
                }
            }

            FormMsgManager.Instance.SendMsg(new MsgInt((ushort)MediaChannelEvent.MicOnAir, GlobalInfo.account.id));
        }
    }


    void AddCacheMicData(float[] samples)
    {
        Queue<byte> tempCache = new Queue<byte>();

        if (CurrentAudioTimeSample > LastAudioTimeSample)
        {
            lock (_asyncLockCachedAudio)
            {
                for (int i = LastAudioTimeSample; i < CurrentAudioTimeSample; i++)
                {
                    byte[] byteData = BitConverter.GetBytes(FloatToInt16(samples[i]));
                    foreach (byte _byte in byteData) tempCache.Enqueue(_byte);
                }
            }
        }
        else if (CurrentAudioTimeSample < LastAudioTimeSample)
        {
            lock (_asyncLockCachedAudio)
            {
                for (int i = LastAudioTimeSample; i < samples.Length; i++)
                {
                    byte[] byteData = BitConverter.GetBytes(FloatToInt16(samples[i]));
                    foreach (byte _byte in byteData) tempCache.Enqueue(_byte);
                }
                for (int i = 0; i < CurrentAudioTimeSample; i++)
                {
                    byte[] byteData = BitConverter.GetBytes(FloatToInt16(samples[i]));
                    foreach (byte _byte in byteData) tempCache.Enqueue(_byte);
                }
            }
        }
        CachedAudio.Enqueue(tempCache);
    }

    private float GetClipPeak(float[] samples)
    {
        clipPeek = 0;
        if (CurrentAudioTimeSample > LastAudioTimeSample)
        {
            for (int i = LastAudioTimeSample; i < CurrentAudioTimeSample; i++)
            {
                clipPeek = Mathf.Max(Math.Abs(samples[i]), clipPeek);
            }
        }
        if (CurrentAudioTimeSample < LastAudioTimeSample)
        {
            for (int i = LastAudioTimeSample; i < samples.Length; i++)
            {
                clipPeek = Mathf.Max(Math.Abs(samples[i]), clipPeek);
            }
            for (int i = 0; i < CurrentAudioTimeSample; i++)
            {
                clipPeek = Mathf.Max(Math.Abs(samples[i]), clipPeek);
            }
        }
        return clipPeek;
    }

    IEnumerator SenderCOR()
    {
        Debug.Log("[TTS语音] 发送");
        while (!stop)
        {
            if (Time.realtimeSinceStartup > next)
            {
                interval = 1f / StreamFPS;
                next = Time.realtimeSinceStartup + interval;
                EncodeBytes();
            }
            yield return null;
        }

        Debug.Log("[TTS语音] 停止");
    }

    void EncodeBytes()
    {
        //==================getting byte data==================
        byte[] dataByte;
        List<byte> cacheDataByte = new List<byte>();

        byte[] _samplerateByte = BitConverter.GetBytes(OutputSampleRate);
        byte[] _channelsByte = BitConverter.GetBytes(OutputChannels);
        byte[] _streamFPSByte = BitConverter.GetBytes(StreamFPS);

        if (useCache && CachedAudio.Count > 0 && AudioBytes.Count > 0)
        {
            Queue<byte>[] bytes = CachedAudio.ToArray();
            foreach (Queue<byte> data in bytes)
            {
                cacheDataByte.AddRange(data.ToArray());
            }
            cachedLength = cacheDataByte.Count;
        }

        // 每次EncodeBytes只取一帧大小的数据，确保解码器流式缓冲不会溢出
        // 正常麦克风采集天然每50ms产生~1102字节；TTS一次性灌入大量数据时需拆分
        int maxFrameBytes = OutputSampleRate * OutputChannels * 2 / Mathf.Max(StreamFPS, 1);

        lock (_asyncLockAudio)
        {
            byte[] allAudio = AudioBytes.ToArray();
            int takeBytes = Mathf.Min(allAudio.Length, maxFrameBytes);

            dataByte = new byte[cacheDataByte.Count + takeBytes + _samplerateByte.Length + _channelsByte.Length + _streamFPSByte.Length];

            Buffer.BlockCopy(_samplerateByte, 0, dataByte, 0, _samplerateByte.Length);
            Buffer.BlockCopy(_channelsByte, 0, dataByte, 4, _channelsByte.Length);
            Buffer.BlockCopy(_streamFPSByte, 0, dataByte, 8, _streamFPSByte.Length);

            if (useCache && AudioBytes.Count > 0)
            {
                lock (_asyncLockCachedAudio)
                {
                    Buffer.BlockCopy(cacheDataByte.ToArray(), 0, dataByte, 12, cacheDataByte.Count);
                    CachedAudio.Clear();
                }
            }

            Buffer.BlockCopy(allAudio, 0, dataByte, 12 + cacheDataByte.Count, takeBytes);

            // 递减TTS待发送计数（AudioBytes中的字节先入先出，扣除已消费的部分）
            ttsBytesPending = Mathf.Max(0, ttsBytesPending - takeBytes);

            // 保留未消费的字节，供后续帧发送
            AudioBytes.Clear();
            for (int i = takeBytes; i < allAudio.Length; i++)
                AudioBytes.Enqueue(allAudio[i]);
        }


        if (GZipMode) dataByte = dataByte.FMZipBytes();
        //==================getting byte data==================

        dataLength = dataByte.Length;

        // TTS自动停止计时：只关心自己的数据是否发完，不看共享队列状态（与麦克风完全解耦）
        if (ttsActive && ttsBytesPending <= 0)
        {
            emptyFrameCount++;
            if (emptyFrameCount > StreamFPS * 2)
            {
                ttsActive = false;
                StopCapture();
            }
        }
        else
        {
            emptyFrameCount = 0;
        }

        if (dataLength < emptyPacket)
            return;

        int _length = dataByte.Length;
        int _offset = 0;

        byte[] _meta_label = BitConverter.GetBytes(label);
        byte[] _meta_id = BitConverter.GetBytes(dataID);
        byte[] _meta_length = BitConverter.GetBytes(_length);

        int chunks = Mathf.FloorToInt(dataByte.Length / chunkSize);
        for (int i = 0; i <= chunks; i++)
        {
            byte[] _meta_offset = BitConverter.GetBytes(_offset);
            int SendByteLength = (i == chunks) ? (_length % chunkSize + 18) : (chunkSize + 18);
            byte[] SendByte = new byte[SendByteLength];

            Buffer.BlockCopy(_meta_label, 0, SendByte, 0, 4);
            Buffer.BlockCopy(_meta_id, 0, SendByte, 4, 4);
            Buffer.BlockCopy(_meta_length, 0, SendByte, 8, 4);
            Buffer.BlockCopy(_meta_offset, 0, SendByte, 12, 4);
            SendByte[16] = (byte)(GZipMode ? 1 : 0);
            SendByte[17] = (byte)0;//not used, but just keep one empty byte for standard

            Buffer.BlockCopy(dataByte, _offset, SendByte, 18, SendByte.Length - 18);
            OnDataByteReadyEvent.Invoke(SendByte);

            //OnDataStringReadyEvent.Invoke(Encoding.Default.GetString(SendByte));
            _offset += chunkSize;
        }

        dataID++;
        if (dataID > maxID) dataID = 0;
    }

    /// <summary>
    /// 将外部音频数据(已转为11025Hz mono Int16)直接写入采集队列，与麦克风数据混合发送
    /// </summary>
    public void FeedExternalAudio(byte[] audioData)
    {
        lock (_asyncLockAudio)
        {
            foreach (byte b in audioData)
                AudioBytes.Enqueue(b);
            ttsActive = true;
            ttsBytesPending += audioData.Length;
        }
    }

    public void ReleaseMic()
    {
        StopCapture();
        if (AudioMic != null)
        {
            AudioMic.Stop();
            Destroy(AudioMic);
            AudioMic = null;
        }
        if (audioFilterReadForwarder != null)
        {
            Destroy(audioFilterReadForwarder);
            audioFilterReadForwarder = null;
        }
        Microphone.End(CurrentDeviceName);
        micStartSuccess = false;
    }

    private void OnDestroy()
    {
        ReleaseMic();
    }
#endif
    }