using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityFramework.Runtime;

[RequireComponent(typeof(AudioSource))]
public class AudioDelayControlDecoder : MonoBehaviour
{
    readonly int sizeofFloat = Marshal.SizeOf(default(float));

    public int SourceChannels = 1;
    public int SourceSampleRate = 48000;
    public int SourceStreamFPS = 20;
    public float SourceFrameDuration = 50f;
    public int DeviceSampleRate = 48000;

    private AudioSource audioSource;
    protected AudioSource Audio
    {
        get
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
            return audioSource;
        }
    }
    private AudioClip audioClip;

    private int samplerate = 44100;
    private int channel = 2;
    private int streamfps = 20;
    private float frameDurationMs = 50;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;
    public float Volume
    {
        get { return volume; }
        set
        {
            volume = Mathf.Clamp(value, 0f, 1f);
            Audio.volume = volume;
        }
    }

    private bool ReadyToGetFrame = true;
    public int label = 2001;
    [HideInInspector]
    public int userId;
    private int dataID = 0;
    private int dataLength = 0;
    private int receivedLength = 0;

    private byte[] dataByte;
    public bool GZipMode = false;

    Queue<float[]> frameQueue = new Queue<float[]>();
    private object _asyncLock = new object();

    protected int frameSamples { get { return (int)(this.SourceSampleRate * (long)frameDurationMs / 1000); } }
    private int frameSize;
    protected int bufferSamples;

    AudioUtil.PlayDelayConfig playDelayConfig;
    private int targetDelaySamples;
    // correct if higher: gradually move to target via input frames resampling
    private int upperTargetDelaySamples;
    // set delay to this value if delay is higher 
    private int maxDelaySamples;

    /// <summary>
    /// 当前播放位置
    /// </summary>
    private long outPos { get { return Audio.timeSamples; } }
    private long outPosPrev;
    /// <summary>
    /// 循环次数
    /// </summary>
    private int playLoopCount;
    /// <summary>
    /// 是否需要对音频进行加速处理
    /// </summary>
    bool catchingUp = false;

    /// <summary>
    /// 音频加速器
    /// </summary>
    AudioUtil.TempoUp<float> tempoUp;

    const int TEMPO_UP_SKIP_GROUP = 6;
    float[] zeroFrame;
    float[] resampledFrame;

    private long writeSamplePos;
    private long playSamplePos;
    private long clearSamplePos;

    private bool flushed = true;

    void Start()
    {
        Application.runInBackground = true;
        DeviceSampleRate = AudioSettings.GetConfiguration().sampleRate;

        Audio.volume = volume;
    }

    public void Action_ProcessData(byte[] _byteData)
    {
        if (!enabled) return;
        if (_byteData.Length <= 18) return;

        int _label = BitConverter.ToInt32(_byteData, 0);
        if (_label != label) return;

        int _dataID = BitConverter.ToInt32(_byteData, 4);

        if (_dataID != dataID) receivedLength = 0;
        dataID = _dataID;
        dataLength = BitConverter.ToInt32(_byteData, 8);
        int _offset = BitConverter.ToInt32(_byteData, 12);

        GZipMode = _byteData[16] == 1;

        if (receivedLength == 0) dataByte = new byte[dataLength];
        receivedLength += _byteData.Length - 18;
        Buffer.BlockCopy(_byteData, 18, dataByte, _offset, _byteData.Length - 18);

        if (ReadyToGetFrame)
        {
            if (receivedLength == dataLength)
            {
                if (this.isActiveAndEnabled) StartCoroutine(ProcessAudioData(dataByte));
            }
        }
    }

    IEnumerator ProcessAudioData(byte[] receivedAudioBytes)
    {
        if (Application.platform != RuntimePlatform.WebGLPlayer)
        {
            ReadyToGetFrame = false;
            if (GZipMode) receivedAudioBytes = receivedAudioBytes.FMUnzipBytes();

            if (receivedAudioBytes.Length >= 12 + 1024)
            {
                byte[] _sampleRateByte = new byte[4];
                byte[] _channelsByte = new byte[4];
                byte[] _streamFpsByte = new byte[4];
                byte[] _audioByte = new byte[1];
                lock (_asyncLock)
                {
                    _audioByte = new byte[receivedAudioBytes.Length - 12];
                    Buffer.BlockCopy(receivedAudioBytes, 0, _sampleRateByte, 0, _sampleRateByte.Length);
                    Buffer.BlockCopy(receivedAudioBytes, 4, _channelsByte, 0, _channelsByte.Length);
                    Buffer.BlockCopy(receivedAudioBytes, 8, _streamFpsByte, 0, _streamFpsByte.Length);
                    Buffer.BlockCopy(receivedAudioBytes, 12, _audioByte, 0, _audioByte.Length);
                }

                SourceSampleRate = BitConverter.ToInt32(_sampleRateByte, 0);
                SourceChannels = BitConverter.ToInt32(_channelsByte, 0);
                SourceStreamFPS = BitConverter.ToInt32(_streamFpsByte, 0);

                float[] ABuffer = ToFloatArray(_audioByte);

                CreateAudio();

                Push(ABuffer);
            }
            ReadyToGetFrame = true;
        }
        yield return null;
    }

    private void CreateAudio()
    {
        if (samplerate != SourceSampleRate || channel != SourceChannels || streamfps != SourceStreamFPS)
        {
            samplerate = SourceSampleRate;
            channel = SourceChannels;
            streamfps = SourceStreamFPS;

            frameDurationMs = 1000f / streamfps;

            targetDelaySamples = playDelayConfig.Low * SourceSampleRate / 1000 + frameSamples;
            upperTargetDelaySamples = playDelayConfig.High * SourceSampleRate / 1000 + frameSamples;
            if (upperTargetDelaySamples < targetDelaySamples + 10 * frameSamples)
            {
                upperTargetDelaySamples = targetDelaySamples + 10 * frameSamples;
            }

            maxDelaySamples = playDelayConfig.Max * SourceSampleRate / 1000;
            if (maxDelaySamples < upperTargetDelaySamples)
            {
                maxDelaySamples = upperTargetDelaySamples;
            }

            bufferSamples = 3 * maxDelaySamples;
            frameSize = frameSamples * channel;

            writeSamplePos = targetDelaySamples;

            zeroFrame = new float[frameSize];
            resampledFrame = new float[frameSize];

            tempoUp = new AudioUtil.TempoUp<float>();

            Audio.loop = true;
            audioClip = AudioClip.Create("AudioOut", bufferSamples, SourceChannels, SourceSampleRate, false);

            Audio.clip = audioClip;

            Audio.Play();
        }
    }

    public void Push(float[] frame)
    {
        if (frame.Length == 0)
            return;

        lock (frameQueue)
        {
            frameQueue.Enqueue(frame);

            FormMsgManager.Instance.SendMsg(new MsgInt((ushort)MediaChannelEvent.MicOnAir, userId));
        }
    }

    protected void Update()
    {
        long currentOutPos = outPos;
        if (currentOutPos < outPosPrev)
        {
            playLoopCount++;
        }
        outPosPrev = currentOutPos;

        playSamplePos = playLoopCount * bufferSamples + currentOutPos;

        lock (frameQueue)
        {
            while (frameQueue.Count > 0)
            {
                var frame = frameQueue.Dequeue();

                if (processFrame(frame, playSamplePos))
                {
                    break;
                }
            }
        }

        var clearMin = playSamplePos - bufferSamples;
        if (clearSamplePos < clearMin)
        {
            clearSamplePos = clearMin;
        }
        // clear played back buffer segment
        for (; clearSamplePos + frameSamples < playSamplePos; clearSamplePos += frameSamples)
        {
            int o = (int)(clearSamplePos % bufferSamples);
            if (o < 0) o += bufferSamples;
            AudioWrite(zeroFrame, o);
        }
    }

    /// <summary>
    /// 处理音频帧，写入AudioClip
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="playSamplePos">当前总计播放点位</param>
    /// <returns></returns>
    bool processFrame(float[] frame, long playSamplePos)
    {
        int lagSamples = (int)(writeSamplePos - playSamplePos);
        if (!flushed)
        {
            if (lagSamples > maxDelaySamples)
            {
                writeSamplePos = playSamplePos + maxDelaySamples;
                lagSamples = maxDelaySamples;
            }
            else if (lagSamples < 0)
            {
                writeSamplePos = playSamplePos + targetDelaySamples;
                lagSamples = targetDelaySamples;
            }
        }

        // flush signalled
        if (frame == null) 
        {
            flushed = true;
            if (catchingUp)
                catchingUp = false;

            return true;
        }
        else
        {
            if (flushed)
            {
                writeSamplePos = playSamplePos + targetDelaySamples;
                lagSamples = targetDelaySamples;
                flushed = false;
            }
        }

        // starting catching up
        if (lagSamples > upperTargetDelaySamples && !catchingUp)
        {
            tempoUp.Begin(channel, playDelayConfig.SpeedUpPerc, TEMPO_UP_SKIP_GROUP);
            catchingUp = true;
        }

        // finishing catching up
        // first frame after switching from catching up requires special processing to flush TempoUp (the end of skipping wave removed if required)
        bool frameIsWritten = false;
        if (lagSamples <= targetDelaySamples && catchingUp)
        {
            int skipSamples = tempoUp.End(frame);
            int resampledLenSamples = frame.Length / channel - skipSamples;
            Buffer.BlockCopy(frame, skipSamples * channel * sizeofFloat, resampledFrame, 0, resampledLenSamples * channel * sizeofFloat);
            writeResampled(resampledFrame, resampledLenSamples);
            frameIsWritten = true;
            catchingUp = false;
        }

        if (frameIsWritten)
            return false;

        if (catchingUp)
        {
            int resampledLenSamples = tempoUp.Process(frame, resampledFrame);
            writeResampled(resampledFrame, resampledLenSamples);
        }
        else
        {
            AudioWrite(frame, (int)(writeSamplePos % bufferSamples));
            writeSamplePos += frame.Length / channel;
        }

        return false;
    }

    int writeResampled(float[] f, int resampledLenSamples)
    {
        // zero not used part of the buffer because SetData applies entire frame
        // if this frame is the last, grabage may be played back
        int tailSize = (f.Length - resampledLenSamples * channel) * sizeofFloat;
        if (tailSize > 0)
        {
            Buffer.BlockCopy(this.zeroFrame, 0, f, resampledLenSamples * channel * sizeofFloat, tailSize);
        }

        AudioWrite(f, (int)(writeSamplePos % bufferSamples));
        writeSamplePos += resampledLenSamples;
        return resampledLenSamples;
    }

    private void AudioWrite(float[] frame, int offset)
    {
        audioClip.SetData(frame, offset);
    }

    private float[] ToFloatArray(byte[] byteArray)
    {
        int len = byteArray.Length / 2;
        float[] floatArray = new float[len];
        for (int i = 0; i < byteArray.Length; i += 2)
        {
            floatArray[i / 2] = ((float)BitConverter.ToInt16(byteArray, i)) / 32767f;
        }
        return floatArray;
    }

    private void OnDestroy()
    {
        Audio.Stop();
        if (Audio != null)
        {
            Audio.clip = null;
            audioClip = null;
        }
    }
}