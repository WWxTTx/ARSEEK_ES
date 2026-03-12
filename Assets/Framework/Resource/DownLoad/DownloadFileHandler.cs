using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityFramework.Runtime;

/// <summary>
/// 文件下载，断点续传，重名替换
/// </summary>
public class DownloadFileHandler : DownloadHandlerScript
{
    /// <summary>
    /// 临时文件后缀名
    /// </summary>
    public static string postfix = ".temp";
    /// <summary>
    /// 临时文件保存路径(路径+名+临时后缀)
    /// </summary>
    private string tempPath;
    /// <summary>
    /// 文件保存路径（路径+名+后缀）
    /// </summary>
    private string savePath;
    /// <summary>
    /// 已下载长度
    /// </summary>
    public int nowLength { get; private set; }
    /// <summary>
    /// 文件总长度
    /// </summary>
    public int sumLength { get; private set; }
    /// <summary>
    /// 下载进度
    /// </summary>
    public float DownloadProgress
    {
        get
        {
            if (sumLength == 0)
                return 0f;
            else
                return (float)nowLength / sumLength;
        }
    }

    /// <summary>
    /// 是否下载完成
    /// </summary>
    public new bool isDone;
    /// <summary>
    /// 是否下载
    /// </summary>
    private bool isdown;

    /// <summary>
    /// 实例方法
    /// </summary>
    /// <param name="savePath">文件保存路径</param>
    public DownloadFileHandler(string savePath) : base(new byte[1024 * 200])//限制下载的长度
    {
        Log.Debug("文件保存路径:" + savePath );
        if (string.IsNullOrEmpty(savePath))
        {
            isdown = false;
            Log.Error("文件保存路径为null");
            return;
        }

        //创建保存地址文件夹
        string saveDir = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(saveDir))
            Directory.CreateDirectory(saveDir);

        isdown = true;
        this.savePath = savePath;
        tempPath = savePath.Substring(0, savePath.LastIndexOf('.')) + postfix;//修改后缀为临时文件后缀
        //nowLength = (int)FileTool.GetFileLength(tempPath);

        FileTool.FileDelete(tempPath);
        nowLength = 0;
    }

    /// <summary>
    /// 实例方法 该实例方法只限获取下载文件长度
    /// </summary>
    public DownloadFileHandler()
    {
        isdown = false;
        nowLength = 0;
    }

    /// <summary>
    /// 重写基类 获取到下载文件的长度
    /// </summary>
    /// <param name="contentLength"></param>
    protected override void ReceiveContentLengthHeader(ulong contentLength)
    {
        //这里真坑  断点下载 下次获取的是未下载的长度 需要加上本地已经下载的长度 才是整个文件的总长度
        sumLength = (int)contentLength + nowLength;
    }

    /// <summary>
    ///  重写基类 正在下载 
    /// </summary>
    /// <param name="data">下载的数据 （不止有现在下载的还有前面下载的数据）</param>
    /// <param name="dataLength">当前下载的数据长度</param>
    /// <returns></returns>
    protected override bool ReceiveData(byte[] data, int dataLength)
    {
        if (!isdown)
            return false;

        nowLength += dataLength;
        WriteFile(tempPath, data, dataLength);

        return true;
    }

    /// <summary>
    /// 写入文件
    /// </summary>
    /// <param name="Path_Name"></param>
    /// <param name="dates"></param>
    /// <param name="insertPosition"></param>
    private void WriteFile(string Path_Name, byte[] dates, int length)
    {
        FileStream fs;
        if (!File.Exists(Path_Name))
            fs = File.Create(Path_Name);
        else
            fs = File.OpenWrite(Path_Name);

        long ength = fs.Length;
        fs.Seek(ength, SeekOrigin.Current);//断点续传核心，设置本地文件流的当前位置
        fs.Write(dates, 0, length);
        fs.Flush();
        fs.Close();
    }

    /// <summary>
    /// 下载完成回调
    /// </summary>
    protected override void CompleteContent()
    {
        FileTool.FileDelete(savePath);
        FileTool.FileMove(tempPath, savePath);
        isDone = true;
    }

    public void ClearTemp()
    {
        FileTool.FileDelete(tempPath);
    }
}