using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;
using UnityFramework.Runtime;

public class VideoPreviewData
{
    public int id;
    public string url;
    public Texture2D texture;
    public uint width;
    public uint height;
    public string length;

    public VideoPreviewData(int id, string url, Texture2D texture, uint width, uint height, string length)
    {
        this.id = id;
        this.url = url;
        this.texture = texture;
        this.width = width;
        this.height = height;
        this.length = length;
    }
}

public class GetFirstVideoImage : MonoBehaviour
{
    VideoPlayer vp;
    Texture2D videoFrameTexture;

    Dictionary<int, VideoPreviewData> save = new Dictionary<int, VideoPreviewData>();

    private string lastLoadedUrl;

    private Coroutine loadPreviewCo;

    private Coroutine loadPreviewsCo;


    private void InitComponent()
    {
        if (vp == null)
        {
            vp = GetComponent<VideoPlayer>();
            vp.audioOutputMode = VideoAudioOutputMode.None;
            vp.playOnAwake = false;
            vp.waitForFirstFrame = true;
            vp.sendFrameReadyEvents = true;
        }
    }

    /// <summary>
    /// 加载单一视频预览图
    /// </summary>
    /// <param name="url"></param>
    /// <param name="callBack"></param>
    public void LoadVideoPreview(string url, UnityAction<Texture2D> callBack)
    {
        if (url.Equals(lastLoadedUrl) && videoFrameTexture != null)
        {
            callBack?.Invoke(videoFrameTexture);
            return;
        }

        videoFrameTexture = new Texture2D(2, 2);
        InitComponent();
        loadPreviewCo = StartCoroutine(_loadPreview(url, callBack));
    }

    private IEnumerator _loadPreview(string url, UnityAction<Texture2D> callBack)
    {
        vp.frame = 0;
        vp.url = url.Replace("https", "http");
        vp.frameReady += OnNewFrame;
        vp.Play();
        videoFrameTexture = null;
        vp.Prepare();
        vp.errorReceived += (vp, msg) =>
        {
            Log.Error(msg);
            if (loadPreviewCo != null)
            {
                StopCoroutine(loadPreviewCo);
                vp.Pause();
            }
        };
        //等待缓冲
        while (!vp.isPrepared)
            yield return 0;
        while (videoFrameTexture == null)
            yield return 0;

        videoFrameTexture = ScaleTexture(videoFrameTexture, 332, 305);
        vp.Pause();

        lastLoadedUrl = url;
        callBack.Invoke(videoFrameTexture);
    }

    /// <summary>
    /// 加载多个视频封面, 全部加载完成后再执行回调
    /// </summary>
    /// <param name="infos"></param>
    /// <param name="callBacke"></param>
    public void LoadVideoPreviews(Dictionary<int, string> urls, UnityAction<Dictionary<int, VideoPreviewData>> callBack)
    {
        if (loadPreviewsCo != null)
            StopCoroutine(loadPreviewsCo);
        InitComponent();
        loadPreviewsCo = StartCoroutine(_loadAllTogether(urls, callBack));
    }

    public IEnumerator _loadAllTogether(Dictionary<int, string> urls, UnityAction<Dictionary<int, VideoPreviewData>> callBack)
    {
        List<int> index = new List<int>(urls.Keys);
        bool errorReceived = false;
        for (int i = 0; i < index.Count; i++)
        {
            errorReceived = false;
            vp.frame = 0;
            vp.url = urls[index[i]].Replace("https", "http");
            vp.frameReady -= OnNewFrame;
            vp.frameReady += OnNewFrame;
            vp.Play();
            videoFrameTexture = null;
            vp.Prepare();
            vp.errorReceived -= OnError;
            vp.errorReceived += OnError;

            //等待缓冲
            while (!vp.isPrepared && !errorReceived)
                yield return 0;
            while (videoFrameTexture == null && !errorReceived)
                yield return 0;

            if (errorReceived)
                continue;

            ScaleTexture(videoFrameTexture, vp.width, vp.height, 332, 305, vp.length, index[i], vp.url);
            vp.Pause();
        }
        callBack.Invoke(save);
        //save.Clear();
    }


    /// <summary>
    /// 加载多个视频封面, 单个加载完成后执行回调
    /// </summary>
    /// <param name="urls"></param>
    /// <param name="callBack"></param>
    public void LoadVideoPreviews2(Dictionary<int, string> urls, UnityAction<VideoPreviewData> callBack)
    {
        if (loadPreviewsCo != null)
            StopCoroutine(loadPreviewsCo);
        InitComponent();
        loadPreviewsCo = StartCoroutine(_loadSeperate(urls, callBack));
    }

    private bool errorReceived;
    public IEnumerator _loadSeperate(Dictionary<int, string> urls, UnityAction<VideoPreviewData> callBack)
    {
        List<int> index = new List<int>(urls.Keys);
        for (int i = 0; i < index.Count; i++)
        {
            errorReceived = false;
            vp.frame = 0;
            vp.url = urls[index[i]].Replace("https", "http");
            vp.frameReady -= OnNewFrame;
            vp.frameReady += OnNewFrame;
            vp.Play();
            videoFrameTexture = null;
            vp.Prepare();
            vp.errorReceived -= OnError;
            vp.errorReceived += OnError;
            //等待缓冲
            while (!vp.isPrepared && !errorReceived)
                yield return 0;
            while (videoFrameTexture == null && !errorReceived)
                yield return 0;

            if (errorReceived)
                continue;

            ScaleTexture(videoFrameTexture, vp.width, vp.height, 332, 305, vp.length, index[i], vp.url);
            vp.Pause();

            callBack.Invoke(save[index[i]]);
        }
    }

    void OnNewFrame(VideoPlayer source, long frameIdx)
    {
        if (frameIdx >= 5)
        {
            videoFrameTexture = RenderTextureToTexture2D(source.texture as RenderTexture);
            vp.frameReady -= OnNewFrame;
        }
    }

    void OnError(VideoPlayer source, string msg)
    {
        errorReceived = true;
        source.Pause();
        Log.Error(msg);
    }

    Texture2D RenderTextureToTexture2D(RenderTexture renderTexture)
    {
        if (renderTexture == null)
        {
            Log.Debug("renderTexture is null！");
            return null;
        }
        Texture2D t = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
        RenderTexture.active = renderTexture;
        t.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        t.Apply();
        RenderTexture.active = null;
        return t;
    }
    Sprite Texture2DToSprite(Texture2D texture2D)
    {
        if (texture2D == null)
        {
            Log.Debug("texture2D is null！");
            return null;
        }
        return Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), Vector2.zero);
    }

    /// <summary>
    /// 生成缩略图
    /// </summary>
    /// <param name="source"></param>
    /// <param name="targetWidth"></param>
    /// <param name="targetHeight"></param>
    /// <returns></returns>
    Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.ARGB32, false);
        for (int i = 0; i < result.height; ++i)
        {
            for (int j = 0; j < result.width; ++j)
            {
                Color newColor = source.GetPixelBilinear((float)j / (float)result.width, (float)i / (float)result.height);
                result.SetPixel(j, i, newColor);
            }
        }
        result.Apply();
        return result;
    }

    /// <summary>
    /// 生成缩略图，包含视频时长等数据
    /// </summary>
    /// <param name="source"></param>
    /// <param name="targetWidth"></param>
    /// <param name="targetHeight"></param>
    /// <param name="length"></param>
    /// <param name="id"></param>
    void ScaleTexture(Texture2D source, uint sourceWidth, uint sourceHeight, int targetWidth, int targetHeight, double length, int id, string url)
    {
        if (!save.ContainsKey(id))
        {
            Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.ARGB32, false);

            for (int i = 0; i < result.height; ++i)
            {
                for (int j = 0; j < result.width; ++j)
                {
                    Color newColor = source.GetPixelBilinear((float)j / (float)result.width, (float)i / (float)result.height);
                    result.SetPixel(j, i, newColor);
                }
            }
            result.Apply();

            save.Add(id, new VideoPreviewData(id, url, result, sourceWidth, sourceHeight, ToTimeFormat(length)));
        }
        else if (!save[id].url.Equals(url))
        {
            Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.ARGB32, false);

            for (int i = 0; i < result.height; ++i)
            {
                for (int j = 0; j < result.width; ++j)
                {
                    Color newColor = source.GetPixelBilinear((float)j / (float)result.width, (float)i / (float)result.height);
                    result.SetPixel(j, i, newColor);
                }
            }
            result.Apply();

            save[id] = new VideoPreviewData(id, url, result, sourceWidth, sourceHeight, ToTimeFormat(length));
        }          
    }

    private string ToTimeFormat(double time)
    {
        int seconds = (int)time;
        int hours = seconds / 3600;
        int minutes = (seconds % 3600) / 60;
        seconds = (seconds % 3600) % 60;
        return string.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);
    }

    private void OnDestroy()
    {
        save.Clear();
    }
}