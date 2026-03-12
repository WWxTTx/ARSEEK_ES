using System.Collections;
using UnityEngine;
using System;

using UnityEngine.Rendering;
using System.Collections.Generic;

public enum GameViewShotCaptureMode { RenderCam, FullScreen}


public class GameViewShotEncoder : MonoBehaviour
{
    public GameViewShotCaptureMode CaptureMode = GameViewShotCaptureMode.RenderCam;
    private GameViewShotCaptureMode _CaptureMode = GameViewShotCaptureMode.RenderCam;
    public GameViewResize Resize = GameViewResize.Quarter;

    public Camera MainCam;
    public Camera RenderCam;

    public Vector2 Resolution = new Vector2(512, 512);
    private Vector2 renderResolution = new Vector2(512, 512);
    public Vector2 RenderResolution
    {
        get { return renderResolution; }
    }
    public bool MatchScreenAspect = true;

    public bool FastMode = false;
    public bool AsyncMode = false;

    public bool GZipMode = false;

    [Range(10, 100)]
    public int Quality = 40;
    public FMChromaSubsamplingOption ChromaSubsampling = FMChromaSubsamplingOption.Subsampling420;

    public bool ignoreSimilarTexture = true;
    private int lastRawDataByte = 0;
    [Tooltip("Compare previous image data size(byte)")]
    public int similarByteSizeThreshold = 8;

    private bool NeedUpdateTexture = false;
    private bool EncodingTexture = false;

    //experimental feature: check if your GPU supports AsyncReadback
    private bool supportsAsyncGPUReadback = false;
    public bool EnableAsyncGPUReadback = true;
    public bool SupportsAsyncGPUReadback { get { return supportsAsyncGPUReadback; } }

    private int streamWidth;
    private int streamHeight;

    public Texture2D CapturedTexture;
    public Texture GetStreamTexture
    {
        get
        {
            if (supportsAsyncGPUReadback && EnableAsyncGPUReadback) return rt;
            return CapturedTexture;
        }
    }
    private RenderTextureDescriptor sourceDescriptor;
    private RenderTexture rt;
    private RenderTexture rt_cube;
    private RenderTexture rt_reserved;
    private bool reservedExistingRenderTexture = false;

    public GameViewCubemapSample CubemapResolution = GameViewCubemapSample.Medium;

    private Texture2D Screenshot;
    private ColorSpace ColorSpace;

    public UnityEventByteArray OnDataByteReadyEvent = new UnityEventByteArray();

    public UnityEventByteQueue OnDataChunksReadyEvent = new UnityEventByteQueue();
    public Queue<byte[]> DataChunks = new Queue<byte[]>();

    //[Header("Pair Encoder & Decoder")]
    public int label = 1001;
    private int dataID = 0;
    private int maxID = 1024;
    private int chunkSize = 8096; //32768
    private float next = 0f;
    private bool stop = false;
    private byte[] dataByte;

    public int dataLength;

    private Coroutine sendCOR;

    public void StartCapture()
    {
        stop = false;
        RequestTextureUpdate();

        NeedUpdateTexture = false;
        EncodingTexture = false;
    }

    public void StopCapture()
    {
        stop = true;
        StopAllCoroutines();
        sendCOR = null;
        DataChunks.Clear();
        lastRawDataByte = 0;
    }

    void CaptureModeUpdate()
    {
        if (_CaptureMode != CaptureMode)
        {
            _CaptureMode = CaptureMode;
            if (rt != null) Destroy(rt);
            if (CapturedTexture != null) Destroy(CapturedTexture);
        }
    }

    private void Start()
    {
        Application.runInBackground = true;
        ColorSpace = QualitySettings.activeColorSpace;

        sourceDescriptor = (UnityEngine.XR.XRSettings.enabled) ? UnityEngine.XR.XRSettings.eyeTextureDesc : new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.ARGB32);
        sourceDescriptor.depthBufferBits = 16;

#if UNITY_2018_2_OR_NEWER
        try { supportsAsyncGPUReadback = SystemInfo.supportsAsyncGPUReadback; }
        catch { supportsAsyncGPUReadback = false; }
#else
        supportsAsyncGPUReadback = false;
#endif
        if (RenderCam != null)
        {
            if (RenderCam.targetTexture != null)
            {
                rt_reserved = RenderCam.targetTexture;
                reservedExistingRenderTexture = true;
            }
        }

        CaptureModeUpdate();
        CheckResolution();

        //RequestTextureUpdate();
    }

    private void Update()
    {
        Resolution.x = Mathf.RoundToInt(Resolution.x);
        Resolution.y = Mathf.RoundToInt(Resolution.y);
        if (Resolution.x <= 1) Resolution.x = 1;
        if (Resolution.y <= 1) Resolution.y = 1;
        renderResolution = Resolution;

        CaptureModeUpdate();

        switch (_CaptureMode)
        {
            case GameViewShotCaptureMode.RenderCam:
                if (MatchScreenAspect)
                {
                    if (Screen.width > Screen.height) renderResolution.y = renderResolution.x / (float)(Screen.width) * (float)(Screen.height);
                    if (Screen.width < Screen.height) renderResolution.x = renderResolution.y / (float)(Screen.height) * (float)(Screen.width);
                }
                break;
            case GameViewShotCaptureMode.FullScreen:
                renderResolution = new Vector2(Screen.width, Screen.height) / Mathf.Pow(2, (int)Resize);
                break;
        }

        if (_CaptureMode != GameViewShotCaptureMode.RenderCam)
        {
            if (RenderCam != null)
            {
                if (RenderCam.targetTexture != null) RenderCam.targetTexture = null;
            }
        }
    }

    void CheckResolution()
    {
        if (renderResolution.x <= 1) renderResolution.x = 1;
        if (renderResolution.y <= 1) renderResolution.y = 1;

        bool IsLinear = (ColorSpace == ColorSpace.Linear) && (CaptureMode == GameViewShotCaptureMode.FullScreen);

        sourceDescriptor.width = Mathf.RoundToInt(renderResolution.x);
        sourceDescriptor.height = Mathf.RoundToInt(renderResolution.y);
        sourceDescriptor.sRGB = !IsLinear;

        if (rt == null)
        {
            //may have unsupport graphic format bug on Unity2019/2018, fallback to not using descriptor
            try { rt = new RenderTexture(sourceDescriptor); }
            catch
            {
                DestroyImmediate(rt);
                rt = new RenderTexture(sourceDescriptor.width, sourceDescriptor.height, 16, RenderTextureFormat.ARGB32);
            }
            rt.Create();
        }
        else
        {
            if (rt.width != sourceDescriptor.width || rt.height != sourceDescriptor.height || rt.sRGB != IsLinear)
            {
                if (MainCam != null) { if (MainCam.targetTexture == rt) MainCam.targetTexture = null; }
                if (RenderCam != null) { if (RenderCam.targetTexture == rt) RenderCam.targetTexture = null; }
                DestroyImmediate(rt);
                //may have unsupport graphic format bug on Unity2019/2018, fallback to not using descriptor
                try { rt = new RenderTexture(sourceDescriptor); }
                catch
                {
                    DestroyImmediate(rt);
                    rt = new RenderTexture(sourceDescriptor.width, sourceDescriptor.height, 16, RenderTextureFormat.ARGB32);
                }
                rt.Create();
            }
        }

        if (CapturedTexture == null) { CapturedTexture = new Texture2D(sourceDescriptor.width, sourceDescriptor.height, TextureFormat.RGB24, false, IsLinear); }
        else
        {
            if (CapturedTexture.width != sourceDescriptor.width || CapturedTexture.height != sourceDescriptor.height)
            {
                DestroyImmediate(CapturedTexture);
                CapturedTexture = new Texture2D(sourceDescriptor.width, sourceDescriptor.height, TextureFormat.RGB24, false, IsLinear);
            }
        }
    }

    void ProcessCapturedTexture()
    {
        streamWidth = rt.width;
        streamHeight = rt.height;

        if (!FastMode) EnableAsyncGPUReadback = false;
        if (supportsAsyncGPUReadback && EnableAsyncGPUReadback) { StartCoroutine(ProcessCapturedTextureGPUReadbackCOR()); }
        else { StartCoroutine(ProcessCapturedTextureCOR()); }
    }

    IEnumerator ProcessCapturedTextureCOR()
    {
        //render texture to texture2d
        RenderTexture.active = rt;
        CapturedTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        CapturedTexture.Apply();
        RenderTexture.active = null;

        //encode to byte for streaming
        StartCoroutine(EncodeBytes());
        yield break;
    }


    IEnumerator ProcessCapturedTextureGPUReadbackCOR()
    {
#if UNITY_2018_2_OR_NEWER
        if (rt != null)
        {
            AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(rt, 0, TextureFormat.RGB24);
            while (!request.done) yield return null;
            if (!request.hasError) { StartCoroutine(EncodeBytes(request.GetData<byte>().ToArray())); }
            else { EncodingTexture = false; }
        }
        else { EncodingTexture = false; }
#endif
    }

    IEnumerator RenderTextureRefresh()
    {
        if (NeedUpdateTexture && !EncodingTexture)
        {
            NeedUpdateTexture = false;
            EncodingTexture = true;

            yield return new WaitForEndOfFrame();
            CheckResolution();

            if (_CaptureMode == GameViewShotCaptureMode.RenderCam)
            {
                if (RenderCam != null)
                {
                    if (reservedExistingRenderTexture)
                    {
                        RenderCam.targetTexture = rt_reserved;
                        Graphics.Blit(rt_reserved, rt);
                    }
                    else
                    {
                        RenderCam.targetTexture = rt;
                        RenderCam.Render();
                        RenderCam.targetTexture = null;
                    }

                    // RenderTexture to Texture2D
                    ProcessCapturedTexture();
                }
                else { EncodingTexture = false; }
            }

            if (_CaptureMode == GameViewShotCaptureMode.FullScreen)
            {
                if (Resize == GameViewResize.Full)
                {
                    // cleanup
                    if (CapturedTexture != null) Destroy(CapturedTexture);
                    CapturedTexture = ScreenCapture.CaptureScreenshotAsTexture();

                    StartCoroutine(EncodeBytes());
                }
                else
                {
                    // cleanup
                    if (Screenshot != null) Destroy(Screenshot);
                    Screenshot = ScreenCapture.CaptureScreenshotAsTexture();

                    Graphics.Blit(Screenshot, rt);

                    // RenderTexture to Texture2D
                    ProcessCapturedTexture();
                }
            }
        }
    }

    public void Action_UpdateTexture() { RequestTextureUpdate(); }

    void RequestTextureUpdate()
    {
        if (EncodingTexture) return;
        NeedUpdateTexture = true;
        StartCoroutine(RenderTextureRefresh());
    }

    IEnumerator EncodeBytes(byte[] RawTextureData = null)
    {
        if (CapturedTexture != null || RawTextureData != null)
        {
            //==================getting byte data==================
#if UNITY_IOS && !UNITY_EDITOR
            FastMode = true;
#endif

#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_EDITOR_WIN || UNITY_IOS || UNITY_ANDROID || WINDOWS_UWP || UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            if (FastMode)
            {
                //try AsyncMode, on supported platform
                if (AsyncMode && Loom.numThreads < Loom.maxThreads)
                {
                    //has spare thread
                    bool AsyncEncoding = true;
                    if (RawTextureData == null) RawTextureData = CapturedTexture.GetRawTextureData();

                    Loom.RunAsync(() =>
                    {
                        dataByte = RawTextureData.FMRawTextureDataToJPG(streamWidth, streamHeight, Quality, ChromaSubsampling);
                        AsyncEncoding = false;
                    });
                    while (AsyncEncoding) yield return null;
                }
                else
                {
                    //need yield return, in order to fix random error "coroutine->IsInList()"
                    yield return dataByte = RawTextureData == null ? CapturedTexture.FMEncodeToJPG(Quality, ChromaSubsampling) : RawTextureData.FMRawTextureDataToJPG(streamWidth, streamHeight, Quality, ChromaSubsampling);
                }
            }
            else { dataByte = RawTextureData == null ? CapturedTexture.EncodeToJPG(Quality) : RawTextureData.FMRawTextureDataToJPG(streamWidth, streamHeight, Quality, ChromaSubsampling); }
#else
            dataByte = RawTextureData == null ? CapturedTexture.EncodeToJPG(Quality) : RawTextureData.FMRawTextureDataToJPG(streamWidth, streamHeight, Quality, ChromaSubsampling);
#endif

            if (ignoreSimilarTexture)
            {
                float diff = Mathf.Abs(lastRawDataByte - dataByte.Length);
                if (diff < similarByteSizeThreshold)
                {
                    EncodingTexture = false;
                    yield break;
                }
            }
            lastRawDataByte = dataByte.Length;

            if (GZipMode) dataByte = dataByte.FMZipBytes();

            dataLength = dataByte.Length;
            //==================getting byte data==================
            int _length = dataByte.Length;
            int _offset = 0;

            byte[] _meta_label = BitConverter.GetBytes(label);
            byte[] _meta_id = BitConverter.GetBytes(dataID);
            byte[] _meta_length = BitConverter.GetBytes(_length);

            int chunks = Mathf.RoundToInt(dataByte.Length / chunkSize);
            for (int i = 0; i <= chunks; i++)
            {
                int SendByteLength = (i == chunks) ? (_length % chunkSize + 17) : (chunkSize + 17);
                byte[] _meta_offset = BitConverter.GetBytes(_offset);
                byte[] SendByte = new byte[SendByteLength];

                Buffer.BlockCopy(_meta_label, 0, SendByte, 0, 4);
                Buffer.BlockCopy(_meta_id, 0, SendByte, 4, 4);
                Buffer.BlockCopy(_meta_length, 0, SendByte, 8, 4);

                Buffer.BlockCopy(_meta_offset, 0, SendByte, 12, 4);
                SendByte[16] = (byte)(GZipMode ? 1 : 0);

                Buffer.BlockCopy(dataByte, _offset, SendByte, 17, SendByte.Length - 17);
                OnDataByteReadyEvent.Invoke(SendByte);

                DataChunks.Enqueue(SendByte);

                _offset += chunkSize;
            }

            OnDataChunksReadyEvent?.Invoke(RawTextureData, DataChunks);

            dataID++;
            if (dataID > maxID) dataID = 0;
        }

        EncodingTexture = false;
        yield break;
    }

    void OnDisable() { StopCapture(); }
    void OnApplicationQuit() { StopCapture(); }
    void OnDestroy() { StopCapture(); }
}