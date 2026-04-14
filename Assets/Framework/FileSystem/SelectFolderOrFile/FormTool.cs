using System;
using System.IO;
using System.Windows.Forms;
using UnityEngine;
using UnityFramework.Runtime;

/// <summary>
/// 选择文件夹或文件弹窗
/// 注意：System.Windows.Forms.dll
/// 使用目前的，弹窗样式更好看
/// 使用Unity安装目录（Unity\Editor\Data\MonoBleedingEdge\lib\mono\unityjit）下的，弹窗样式更复古
/// </summary>
public class FormTool
{
    /// <summary>
    /// 选择文件夹
    /// </summary>
    /// <param name="title">弹窗标题</param>
    /// <param name="defaultPath">弹窗默认打开路径</param>
    /// <param name="callback">完成选择后触发回调</param>
    public static void OpenFolderDialog(string title, string defaultPath, Action<string> callback)
    {
        Log.Debug("title:" + title + "; defaultPath:" + defaultPath);
        FolderBrowserDialog dialog = new FolderBrowserDialog();
        dialog.Description = title;
        dialog.SelectedPath = defaultPath.Replace("/", "\\");  //默认打开路径

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            defaultPath = dialog.SelectedPath.Trim();
            Log.Debug("folderPath:" + defaultPath);
            callback?.Invoke(defaultPath);
        }
    }
    /// <summary>
    /// 选择文件
    /// </summary>
    /// <param name="title">弹窗标题</param>
    /// <param name="defaultPath">弹窗默认打开路径</param>
    /// <param name="callback">完成选择后触发回调</param>
    public static void OpenFileDialog(string title, string defaultPath, Action<string> callback, FileType fileType = FileType.All)
    {
        Log.Debug("title:" + title + "; defaultPath:" + defaultPath);
        OpenFileDialog dialog = new OpenFileDialog();
        dialog.Title = title;
        dialog.InitialDirectory = defaultPath.Replace("/", "\\").Replace(":", ":\\");//默认打开路径
        dialog.Filter = GetFilter(fileType);

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            defaultPath = dialog.FileName;
            Log.Debug("FilePath:" + defaultPath);
            callback?.Invoke(defaultPath);
        }
    }
    /// <summary>
    /// 选择多文件
    /// </summary>
    /// <param name="title">弹窗标题</param>
    /// <param name="defaultPath">弹窗默认打开路径</param>
    /// <param name="callback">完成选择后触发回调</param>
    public static void OpenFileDialog(string title, string defaultPath, Action<string[]> callback, FileType fileType = FileType.All)
    {
        Log.Debug("title:" + title + "; defaultPath:" + defaultPath);
        OpenFileDialog dialog = new OpenFileDialog();
        dialog.Title = title;
        dialog.InitialDirectory = defaultPath.Replace("/", "\\").Replace(":", ":\\");//默认打开路径
        dialog.Filter = GetFilter(fileType);
        dialog.Multiselect = true;

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            defaultPath = Path.GetDirectoryName(dialog.FileNames[0]);
            Log.Debug("FilePath:" + defaultPath);
            for (int i = 0; i < dialog.FileNames.Length; i++)
            {
                Log.Debug("SelectFilePath:" + dialog.FileNames[i]);
            }
            callback?.Invoke(dialog.FileNames);
        }
    }
    /// <summary>
    /// 获取文件筛选条件
    /// </summary>
    /// <param name="fileType">文件类型</param>
    /// <returns>文件筛选条件字符串</returns>
    private static string GetFilter(FileType fileType)
    {
        string str = "";
        switch (fileType)
        {
            case FileType.All:
                str = "All files (*.*)|*.*";
                break;
            case FileType.Text:
                str = "文本文件 (*.xml,*.json,*.txt)|*.xml;*.json;*.txt";
                break;
            case FileType.Texture:
                str = "图片文件 (*.png,*.jpg,*.jpeg)|*.png;*.jpg;*.jpeg";
                break;
            case FileType.Audio:
                str = "音频文件 (*.mp3,*.flac,*.wav)|*.mp3;*.flac;*.wav";
                break;
            case FileType.Video:
                str = "视频文件 (*.mp4,*.wmv,*.avi)|*.mp4;*.wmv;*.avi";
                break;
            case FileType.Media:
                str = "音视频文件 (*.mp3,*.wav,*.mp4,*.avi)|*.mp3;*.wav;*.mp4;*.avi";
                break;
            case FileType.PPT:
                str = "PowerPoint文件 (*.ppt,*.pptx)|*.ppt;*.pptx";
                break;
            case FileType.DOC:
                str = "文档文件 (*.doc,*.docx,*.pdf,*.xls,*.xlsx)|*.doc;*.docx;*.pdf;*.xls;*.xlsx";
                break;
            case FileType.CourseWare:
                str = "课件资料文件|*.ppt;*.pptx;*.doc;*.docx;*.pdf;*.xls;*.xlsx;*.png;*.jpg;*.jpeg;*.mp3;*.wav;*.mp4;*.avi";
                break;
            default:
                str = "All files (*.*)|*.*";
                break;
        }
        return str;
    }

    public static void OpenFolder(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            System.Diagnostics.Process.Start(path);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message + "  " + path);
        }
    }

    public enum FileType
    {
        All,
        Text,
        Texture,
        Audio,
        Video,
        PPT,
        DOC,
        Media,
        CourseWare
    }
}
