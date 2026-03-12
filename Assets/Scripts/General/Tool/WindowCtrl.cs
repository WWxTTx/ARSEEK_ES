using System;
using System.Runtime.InteropServices;

public static class WindowCtrl
{
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    /// <summary>
    /// 还原窗口
    /// </summary>
    public static void RestoreWindow()
    {
        ShowWindow(GetForegroundWindow(), 1);
    }

    /// <summary>
    /// 最小化窗口
    /// </summary>
    public static void MinimizeWindow()
    { //最小化 
        ShowWindow(GetForegroundWindow(), 2);
    }

    /// <summary>
    /// 最大化窗口
    /// </summary>
    public static void MaximizeWindow()
    {
        ShowWindow(GetForegroundWindow(), 3);
    }
}
