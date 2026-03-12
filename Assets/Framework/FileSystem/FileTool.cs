using System;
using System.IO;
using UnityEngine;
using UnityFramework.Runtime;

/// <summary>
/// 文件读写，删除等操作工具类
/// </summary>
public class FileTool
{
    /// <summary>
    /// 读取文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns></returns>
    public static byte[] FileRead(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Log.Debug("文件不存在：" + filePath);
            return null;
        }
        else
        {
            var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            fs.Seek(0, SeekOrigin.Begin);
            var binary = new byte[fs.Length];
            fs.Read(binary, 0, binary.Length);
            fs.Close();
            return binary;
        }
    }
    /// <summary>
    /// 写入文件（没有则新建，已有则覆盖）
    /// </summary>
    /// <param name="Path_Name"></param>
    /// <param name="dates"></param>
    public static void FileWrite(string Path_Name, byte[] dates)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path_Name));
        FileStream fs = new FileStream(Path_Name, FileMode.Create);
        fs.Write(dates, 0, dates.Length);
        fs.Flush();
        fs.Close();
    }
    /// <summary>
    /// 追加文件内容（没有则新建，已有则在已存在文件中新增内容）
    /// </summary>
    /// <param name="Path_Name"></param>
    /// <param name="dates"></param>
    public static void FileAdd(string Path_Name, byte[] dates)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path_Name));
        FileStream fs;
        if (!File.Exists(Path_Name))
            fs = File.Create(Path_Name);
        else
            fs = File.OpenWrite(Path_Name);

        long ength = fs.Length;
        fs.Seek(ength, SeekOrigin.Current);//断点续传核心，设置本地文件流的当前位置
        fs.Write(dates, 0, dates.Length);
        fs.Flush();
        fs.Close();
    }

    /// <summary>
    /// 拷贝文件
    /// </summary>
    /// <param name="OrignFile">原始文件</param>
    /// <param name="NewFile">新文件路径</param>
    public static void FileCopy(string OrignFile, string NewFile)
    {
        if (File.Exists(OrignFile))
        {
            Log.Debug($"复制指定文件{OrignFile} 到 {NewFile}");
            Directory.CreateDirectory(Path.GetDirectoryName(NewFile));
            File.Copy(OrignFile, NewFile, true);
        }
    }
    /// <summary>
    /// 移动文件
    /// </summary>
    /// <param name="OrignFile">原始路径</param>
    /// <param name="NewFile">新路径</param>
    public static void FileMove(string OrignFile, string NewFile)
    {
        if (File.Exists(OrignFile))
        {
            string root = Path.GetPathRoot(NewFile);
            string dir = Path.GetDirectoryName(NewFile);
            if (!root.Equals(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.Move(OrignFile, NewFile);
        }
    }
    /// <summary>
    /// 删除指定文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    public static void FileDelete(string filePath)
    {
        Log.Debug("删除指定文件:" + filePath);
        if (!File.Exists(filePath))
            Log.Debug("文件不存在：" + filePath);
        else
        {
            File.Delete(filePath);
            Log.Debug("文件删除成功!");
        }
    }

    /// <summary>
    /// 递归删除文件夹目录（文件夹、子文件夹、文件）
    /// </summary>
    /// <param name="dir">文件夹目录</param>
    public static void FolderDelete(string dir)
    {
        if (Directory.Exists(dir)) //如果存在这个文件夹删除之
        {
            foreach (string d in Directory.GetFileSystemEntries(dir))
            {
                if (File.Exists(d))
                    File.Delete(d); //直接删除其中的文件  
                else
                    FolderDelete(d); //递归删除子文件夹
            }
            Directory.Delete(dir, true); //删除已空文件夹                
        }
        else
            Log.Debug("目录不存在：" + dir);
    }

    /// <summary>
    /// 清理缓存 为了应对部分文件删除不掉的占用问题 单独写出来
    /// </summary>
    public static void CleanCache()
    {
        var path = $"{UnityFramework.Runtime.ResManager.Instance.resourcesRootPath}/Cache";

        if (!Directory.Exists(path))
        {
            return;
        }

        //foreach (string filePath in Directory.GetFileSystemEntries(path))
        //{
        //    if (filePath.EndsWith("Vuplex.WebView"))
        //        continue;

        //    if (File.Exists(filePath))
        //    {
        //        if (!filePath.EndsWith("log"))
        //            File.Delete(filePath); //直接删除其中的文件  
        //    }
        //    else Directory.Delete(filePath, true);
        //}

        foreach (string filePath in Directory.GetFileSystemEntries(path))
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
            else
                Directory.Delete(filePath, true);
        }
    }

    public static void DeleteDirectory(string target_dir)
    {
        string[] files = Directory.GetFiles(target_dir);
        string[] dirs = Directory.GetDirectories(target_dir);

        foreach (string file in files)
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        foreach (string dir in dirs)
        {
            DeleteDirectory(dir);
        }

        Directory.Delete(target_dir, false);
    }

    /// <summary>
    /// 获取文件夹大小
    /// </summary>
    /// <param name="dir"></param>
    /// <returns>byte</returns>
    public static long GetDirectorySize(string dir)
    {
        if (!Directory.Exists(dir))
            return 0;

        long len = 0;
        DirectoryInfo di = new DirectoryInfo(dir);
        //获取di目录中所有文件的大小
        foreach (FileInfo item in di.GetFiles())
            len += item.Length;
        //获取dir目录中所有的文件夹,并保存到一个数组中,以进行递归
        DirectoryInfo[] dis = di.GetDirectories();
        for (int i = 0; i < dis.Length; i++)
            //递归dis.Length个文件夹,得到每个dis[i]下面所有文件的大小
            len += GetDirectorySize(dis[i].FullName);
        return len;
    }

    /// <summary>
    /// 获取文件长度
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static long GetFileLength(string path)
    {
        if (!File.Exists(path))
            return 0;

        FileStream fs = File.OpenWrite(path);
        long length = fs.Length;
        fs.Close();
        return length;
    }
}
