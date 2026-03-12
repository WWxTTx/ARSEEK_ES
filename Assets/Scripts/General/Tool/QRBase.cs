using UnityEngine;
using UnityEngine.UI;
using ZXing;
using System.IO;
using UnityFramework.Runtime;
public static class QRBase
{
    private static WebCamTexture webCamTexture;
    private static BarcodeReader barcodeReader;
    private static Result result;

    /// <summary>
    /// 打开相机纹理采集
    /// </summary>
    /// <param name="cameraImage"></param>
    /// <param name="callBack">识别后的回调</param>
    /// <param name="autoClose">识别后是否自动关闭 默认是</param>
    /// <returns></returns>
    public static bool Start(RawImage cameraImage)
    {
        if (cameraImage == null)
            return false;

        // 获取相机名称
        string deviceName = string.Empty;
#if UNITY_STANDALONE || UNITY_EDITOR
        deviceName = "T2 Webcam";
#endif
        Rect rect = cameraImage.rectTransform.rect;
        if (Start(deviceName, (int)rect.width + 1, (int)rect.height + 1))
        {
            AspectRatioFitter aspectRatioFitter = cameraImage.GetComponent<AspectRatioFitter>();
            if (aspectRatioFitter == null)
                aspectRatioFitter = cameraImage.gameObject.AddComponent<AspectRatioFitter>();

            //// 判断是否是竖屏，竖屏时由于旋转的关系，需要将width和height调换
            //if (webCamTexture.videoRotationAngle % 180 != 0)
            //    aspectRatioFitter.aspectRatio = webCamTexture.height / webCamTexture.width;
            //else
            //    aspectRatioFitter.aspectRatio = webCamTexture.width / webCamTexture.height;

            //#if UNITY_STANDALONE || UNITY_EDITOR
            //            aspectRatioFitter.aspectRatio = webCamTexture.width / webCamTexture.height;
            //#else
            //            aspectRatioFitter.aspectRatio = webCamTexture.height / webCamTexture.width;
            //#endif

            aspectRatioFitter.aspectRatio = (float)webCamTexture.width / webCamTexture.height;
            aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

            cameraImage.texture = webCamTexture;
            return true;
        }
        else
            return false;
    }
    /// <summary>
    /// 打开相机纹理采集
    /// </summary>
    /// <param name="width">采集画面宽</param>
    /// <param name="height">采集画面高</param>
    /// <returns>采集相机画面对象</returns>
    public static WebCamTexture Start(string deviceName = "", int width = 0, int height = 0)
    {
        WebCamDevice[] devices = WebCamTexture.devices;//获取相机数组
        if (devices.Length <= 0)
        {
            Log.Warning("未找到相机设备！");
            return null;
        }

        if (string.IsNullOrEmpty(deviceName))
            deviceName = devices[0].name;//默认使用第一个相机
        else if (string.IsNullOrEmpty(devices.Find(d => d.name == deviceName).name))//查找不到指定相机
        {
            Log.Warning("未找到相机设备-" + deviceName);
            return null;
        }

        if (width == 0 && height == 0)
            webCamTexture = new WebCamTexture(deviceName);
        else
            webCamTexture = new WebCamTexture(deviceName, width, height);

        webCamTexture.Play();//打开摄像头
        barcodeReader = new BarcodeReader();
        return webCamTexture;
    }
    /// <summary>
    /// 关闭相机纹理采集
    /// </summary>
    /// <param name="texture">相机纹理采集对象</param>
    public static void Stop(WebCamTexture texture = null)
    {
        if (texture != null)
            texture.Stop();
        else if (webCamTexture)
        {
            webCamTexture.Stop();
            webCamTexture = null;
            barcodeReader = null;
            result = null;
        }
    }
    /// <summary>
    /// 暂停相机纹理采集
    /// </summary>
    /// <param name="texture">相机纹理采集对象</param>
    public static void Pause(WebCamTexture texture = null)
    {
        if (texture != null && texture.isPlaying)
            texture.Pause();
        else if (webCamTexture && webCamTexture.isPlaying)
            webCamTexture.Pause();
    }
    /// <summary>
    /// 恢复相机纹理采集
    /// </summary>
    /// <param name="texture">相机纹理采集对象</param>
    public static void Replay(WebCamTexture texture = null)
    {
        if (texture != null && !texture.isPlaying)
            texture.Play();
        else if (webCamTexture && !webCamTexture.isPlaying)
            webCamTexture.Play();
    }
    /// <summary>
    /// 解析二维码
    /// </summary>
    /// <param name="texture">需解析图片</param>
    /// <returns>解析出的字符串</returns>
    public static string DecodeQRCode(WebCamTexture texture = null)
    {
        if (texture == null)
            texture = webCamTexture;

        if (texture == null)
        {
            Log.Error("texture is null！");
            return null;
        }
        //Log.Error("height=" + webCamTexture.height + "; width=" + webCamTexture.width);
        return DecodeQRCode(texture.GetPixels32(), texture.width, texture.height);
    }
    /// <summary>
    /// 解析二维码
    /// </summary>
    /// <param name="texture">需解析图片</param>
    /// <returns>解析出的字符串</returns>
    public static string DecodeQRCode(Texture2D texture)
    {
        if (texture == null)
        {
            Log.Error("texture is null！");
            return null;
        }
        return DecodeQRCode(texture.GetPixels32(), texture.width, texture.height);
    }
    /// <summary>
    /// 解析二维码
    /// </summary>
    /// <param name="pixelData">图片像素数组</param>
    /// <param name="width">图片宽</param>
    /// <param name="height">图片高</param>
    /// <returns>解析出的字符串</returns>
    public static string DecodeQRCode(Color32[] pixelData, int width, int height)
    {

        if (barcodeReader == null)
            barcodeReader = new BarcodeReader();

        result = barcodeReader.Decode(pixelData, width, height);
        if (result != null)
            return result.Text;
        else
            return null;
    }
    /// <summary>
    /// 截图并保存在对应目录
    /// </summary>
    /// <param name="savePath"></param>
    public static void GetScreenShot(string savePath)
    {
        if (webCamTexture == null)
        {
            Log.Error("未开启相机！");
            return;
        }
        savePath += ".png";

        Texture2D texture2D = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture renderTexture = RenderTexture.GetTemporary(webCamTexture.width, webCamTexture.height, 32);
        Graphics.Blit(webCamTexture, renderTexture);

        RenderTexture.active = renderTexture;

        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();
        byte[] bytes = texture2D.EncodeToPNG();
        string localPath = savePath.Substring(0, savePath.LastIndexOf('/'));

        if (!Directory.Exists(localPath)) 
            Directory.CreateDirectory(localPath);

        FileStream file = File.Open(savePath, FileMode.Create);
        BinaryWriter writer = new BinaryWriter(file);
        writer.Write(bytes);
        file.Close();
        Texture2D.DestroyImmediate(texture2D);
        RenderTexture.active = currentRT;
        RenderTexture.ReleaseTemporary(renderTexture);
        Log.Debug($"拍照完成！保存在:{savePath}");
    }
}
