using System;
using System.Runtime.InteropServices;

/// <summary>
/// 通过WinApi控制剪切板 避免剪切板占用问题
/// </summary>
public static class ClipboardControl
{
    /// <summary>
    /// 设置剪切板字符串
    /// </summary>
    /// <param name="text">文本</param>
    public static void SetText(string text)
    {
        if (!OpenClipboard(IntPtr.Zero))
        {
            SetText(text);
            return;
        }
        EmptyClipboard();
        SetClipboardData(13, Marshal.StringToHGlobalUni(text));
        CloseClipboard();
    }
    /// <summary>
    /// 获取剪切板字符串
    /// </summary>
    /// <returns></returns>
    public static string GetText()
    {

        string value = string.Empty;
        OpenClipboard(IntPtr.Zero);
        if (IsClipboardFormatAvailable(13))
        {
            IntPtr ptr = GetClipboardData(13);
            if (ptr != IntPtr.Zero)
            {
                value = Marshal.PtrToStringUni(ptr);
            }
        }
        CloseClipboard();
        return value;
    }

    [DllImport("User32")]
    public static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("User32")]
    public static extern bool CloseClipboard();

    [DllImport("User32")]
    public static extern bool EmptyClipboard();

    [DllImport("User32")]
    public static extern bool IsClipboardFormatAvailable(int format);

    [DllImport("User32")]
    public static extern IntPtr GetClipboardData(int uFormat);

    [DllImport("User32", CharSet = CharSet.Unicode)]
    public static extern IntPtr SetClipboardData(int uFormat, IntPtr hMem);
}
