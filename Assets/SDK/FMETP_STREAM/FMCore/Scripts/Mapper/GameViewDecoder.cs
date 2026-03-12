using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class GameViewDecoder : MonoBehaviour
{
    public bool FastMode = false;
    public bool AsyncMode = false;

    [Range(0f, 10f)]
    public float DecoderDelay = 0f;
    private float DecoderDelay_old = 0f;

    public Texture ReceivedTexture { get { return (ColorReductionLevel > 0 ? (Texture)ReceivedRenderTexture : (Texture)ReceivedTexture2D); } }
    public Texture2D ReceivedTexture2D;
    public RenderTexture ReceivedRenderTexture;
    public int ColorReductionLevel = 0;
    public bool FrameOperationChunk = false;

    public GameObject TestQuad;
    public RawImage TestImg;

    public UnityEventTexture OnReceivedTexture;

    public UnityEventString OnReceivedFrameOperation;

    [Tooltip("Mono return texture format R8, otherwise it's RGB24 by default")]
    public bool Mono = false;
    public FilterMode DecodedFilterMode = FilterMode.Bilinear;
    public TextureWrapMode DecodedWrapMode = TextureWrapMode.Clamp;

    [HideInInspector] public Material MatColorAdjustment;
    void Reset() { MatColorAdjustment = new Material(Shader.Find("Hidden/FMETPColorAdjustment")); }

    public TextureFormat textureFormat = TextureFormat.RGB24;

    private int receivedWidth;
    private int receivedHeight;

    // Use this for initialization
    void Start()
    {
        if(MatColorAdjustment) MatColorAdjustment = new Material(Shader.Find("Hidden/FMETPColorAdjustment"));
        Application.runInBackground = true;
    }

    private bool ReadyToGetFrame = true;

    //[Header("Pair Encoder & Decoder")]
    public int label = 1001;
    private int dataID = 0;
    //int maxID = 1024;
    private int dataLength = 0;
    private int receivedLength = 0;

    private byte[] dataByte;
    public bool GZipMode = false;

    /// <summary>
    /// 解析收到的画面数据
    /// </summary>
    /// <param name="_byteData"></param>
    public void Action_ProcessImageData(byte[] _byteData)
    {
        // 如果当前组件未启用，直接返回
        if (!enabled) return;

        // 如果数据长度不足（小于15字节），直接返回（基础元数据需要14+1字节）
        // 注释说明：基础元数据包含：dataID(4) + dataLength(4) + offset(4) + GZipMode(1) + ColorReductionLevel(1) + FrameOperationChunk(1) = 15字节
        if (_byteData.Length <= 14) return;

        // 解析数据包ID（前4字节）
        int _dataID = BitConverter.ToInt32(_byteData, 0);

        // 如果是新的数据包（ID变化），重置接收长度计数器
        if (_dataID != dataID) receivedLength = 0;
        dataID = _dataID; // 更新当前数据包ID

        // 解析总数据长度（4-7字节）
        dataLength = BitConverter.ToInt32(_byteData, 4);
        // 解析当前数据块在完整数据中的偏移量（8-11字节）
        int _offset = BitConverter.ToInt32(_byteData, 8);

        // 解析压缩标志（第13字节：索引12），1表示GZip压缩
        GZipMode = _byteData[12] == 1;
        // 解析颜色缩减等级（第14字节：索引13）
        ColorReductionLevel = (int)_byteData[13];
        // 解析帧操作标志（第15字节：索引14），1表示这是控制指令而非图像数据
        FrameOperationChunk = _byteData[14] == 1;

        // 处理控制指令（帧操作）
        if (FrameOperationChunk && _byteData.Length > 15)
        {
            // 提取操作指令数据（跳过前15字节元数据）
            byte[] opByteData = new byte[_byteData.Length - 15];
            Buffer.BlockCopy(_byteData, 15, opByteData, 0, opByteData.Length);

            // 将字节数据转为UTF-8字符串
            string msg = System.Text.Encoding.UTF8.GetString(opByteData);
            if (!string.IsNullOrEmpty(msg))
                // 触发帧操作事件
                OnReceivedFrameOperation?.Invoke(msg);
            return; // 结束处理（非图像数据）
        }

        // 如果是新数据包的第一个分块，初始化数据缓冲区
        if (receivedLength == 0)
            dataByte = new byte[dataLength];

        // 计算当前分块的有效数据长度（总长 - 15字节头部）
        int chunkDataLength = _byteData.Length - 15;
        // 更新累计接收长度
        receivedLength += chunkDataLength;

        // 将有效数据复制到缓冲区指定位置
        Buffer.BlockCopy(_byteData, 15, dataByte, _offset, chunkDataLength);

        // 检查是否满足帧处理条件
        if (ReadyToGetFrame)
        {
            // 当接收完成时（累计长度 = 总长度）
            if (receivedLength == dataLength)
            {
                // 如果解码延迟参数变更，停止所有协程并更新
                if (DecoderDelay_old != DecoderDelay)
                {
                    StopAllCoroutines();
                    DecoderDelay_old = DecoderDelay;
                }

                // 确保组件激活状态下启动数据处理协程
                if (this.isActiveAndEnabled)
                    StartCoroutine(ProcessImageData(dataByte));
            }
        }
    }


    IEnumerator ProcessImageData(byte[] _byteData)
    {
        yield return new WaitForSeconds(DecoderDelay);
        ReadyToGetFrame = false;

        if (GZipMode)
        {
            try { _byteData = _byteData.FMUnzipBytes(); }
            catch(Exception e)
            {
                Debug.LogException(e);
                ReadyToGetFrame = true;
                yield break;
            }
        }

        //check is Mono
        if (ReceivedTexture2D != null)
        {
            if (ReceivedTexture2D.format != (Mono ? TextureFormat.R8 : textureFormat))
            {
                Destroy(ReceivedTexture2D);
                ReceivedTexture2D = null;
            }
        }
        if (ReceivedTexture2D == null) ReceivedTexture2D = new Texture2D(0, 0, Mono ? TextureFormat.R8 : textureFormat, false);
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
                bool AsyncDecoding = true;
                byte[] RawTextureData = new byte[1];
                int _width = 0;
                int _height = 0;

                Loom.RunAsync(() =>
                {
                    try { _byteData.FMJPGToRawTextureData(ref RawTextureData, ref _width, ref _height, Mono ? TextureFormat.R8 : textureFormat); }
                    catch { }
                    AsyncDecoding = false;

                });
                while (AsyncDecoding) yield return null;

                if (RawTextureData.Length <= 8)
                {
                    ReadyToGetFrame = true;
                    yield break;
                }

                try
                {
                    //check resolution
                    ReceivedTexture2D.FMMatchResolution(ref ReceivedTexture2D, _width, _height);
                    ReceivedTexture2D.LoadRawTextureData(RawTextureData);
                    ReceivedTexture2D.Apply();
                }
                catch
                {
                    Destroy(ReceivedTexture2D);
                    GC.Collect();

                    ReadyToGetFrame = true;
                    yield break;
                }
            }
            else
            {
                //no spare thread, run in main thread
                try { ReceivedTexture2D.FMLoadJPG(ref ReceivedTexture2D, _byteData); }
                catch
                {
                    Destroy(ReceivedTexture2D);
                    GC.Collect();

                    ReadyToGetFrame = true;
                    yield break;
                }
            }
        }
        else { ReceivedTexture2D.LoadImage(_byteData); }
#else
        ReceivedTexture2D.LoadImage(_byteData);
#endif
        if (ReceivedTexture2D.width <= 8)
        {
            //throw new Exception("texture is smaller than 8 x 8, wrong data");
            Debug.LogError("texture is smaller than 8 x 8, wrong data");
            ReadyToGetFrame = true;
            yield break;
        }

        if (ReceivedTexture2D.filterMode != DecodedFilterMode) ReceivedTexture2D.filterMode = DecodedFilterMode;
        if (ReceivedTexture2D.wrapMode != DecodedWrapMode) ReceivedTexture2D.wrapMode = DecodedWrapMode;

        if(receivedWidth != ReceivedTexture2D.width || receivedLength != ReceivedTexture2D.height)
        {
            receivedWidth = ReceivedTexture2D.width;
            receivedHeight = ReceivedTexture2D.height;
            if (TestImg != null && TestImg.TryGetComponent(out AspectRatioFitter aspectRatioFitter))
            {
                aspectRatioFitter.aspectRatio = (float)receivedWidth / receivedHeight;
            }
        }

        if(ColorReductionLevel > 0)
        {
            //check is Mono
            if (ReceivedRenderTexture != null)
            {
                if (ReceivedRenderTexture.format != (Mono ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32))
                {
                    Destroy(ReceivedRenderTexture);
                    ReceivedRenderTexture = null;
                }
            }
            if (ReceivedRenderTexture == null) ReceivedRenderTexture = new RenderTexture(ReceivedTexture2D.width, ReceivedTexture2D.height, 0, Mono ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32);
            if (ReceivedRenderTexture.filterMode != DecodedFilterMode) ReceivedRenderTexture.filterMode = DecodedFilterMode;
            if (ReceivedRenderTexture.wrapMode != DecodedWrapMode) ReceivedRenderTexture.wrapMode = DecodedWrapMode;

            float brightness = Mathf.Pow(2, ColorReductionLevel);
            MatColorAdjustment.SetFloat("_Brightness", brightness);
            Graphics.Blit(ReceivedTexture2D, ReceivedRenderTexture, MatColorAdjustment);
        }


        if (TestQuad != null) TestQuad.GetComponent<Renderer>().material.mainTexture = ReceivedTexture;
        if (TestImg != null) TestImg.texture = ReceivedTexture;
        OnReceivedTexture.Invoke(ReceivedTexture);

        ReadyToGetFrame = true;
        yield return null;
    }

    private void OnDisable() 
    { 
        StopAllCoroutines();
        ReadyToGetFrame = true;
    }

    //Motion JPEG: frame buffer
    private byte[] frameBuffer = new byte[300000];
    private const byte picMarker = 0xFF;
    private const byte picStart = 0xD8;
    private const byte picEnd = 0xD9;

    private int frameIdx = 0;
    private bool inPicture = false;
    private byte previous = (byte)0;
    private byte current = (byte)0;

    private int idx = 0;
    private int streamLength = 0;

    public void Action_ProcessMJPEGData(byte[] _byteData) { parseStreamBuffer(_byteData); }
    void parseStreamBuffer(byte[] streamBuffer)
    {
        idx = 0;
        streamLength = streamBuffer.Length;

        while (idx < streamLength)
        {
            if (inPicture) { parsePicture(streamBuffer); }
            else { searchPicture(streamBuffer); }
        }
    }

    //look for a jpeg frame(begin with FF D8)
    void searchPicture(byte[] streamBuffer)
    {
        do
        {
            previous = current;
            current = streamBuffer[idx++];

            // JPEG picture start ?
            if (previous == picMarker && current == picStart)
            {
                frameIdx = 2;
                frameBuffer[0] = picMarker;
                frameBuffer[1] = picStart;
                inPicture = true;
                return;
            }
        } while (idx < streamLength);
    }


    //fill the frame buffer, until FFD9 is reach.
    void parsePicture(byte[] streamBuffer)
    {
        do
        {
            previous = current;
            current = streamBuffer[idx++];

            frameBuffer[frameIdx++] = current;

            // JPEG picture end ?
            if (previous == picMarker && current == picEnd)
            {
                // Using a memorystream thissway prevent arrays copy and allocations
                using (MemoryStream s = new MemoryStream(frameBuffer, 0, frameIdx))
                {
                    if (ReadyToGetFrame)
                    {
                        if (DecoderDelay_old != DecoderDelay)
                        {
                            StopAllCoroutines();
                            DecoderDelay_old = DecoderDelay;
                        }
                        StartCoroutine(ProcessImageData(s.ToArray()));
                    }
                }

                inPicture = false;
                return;
            }
        } while (idx < streamLength);
    }
}
