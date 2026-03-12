using System;
using UnityFramework.Runtime;

public static class DebugHelper
{
    private static string logFormat = "[{0}] [{1}] {2}";
    private static string dateTimeFormat = "HH:mm:ss.fff";
    public static void Debug(ChannelType channel, string message)
    {
        Log.Debug(logFormat, DateTime.Now.ToString(dateTimeFormat), channel, message);
    }

    public static void Info(ChannelType channel, string message)
    {
        Log.Info(logFormat, DateTime.Now.ToString(dateTimeFormat), channel, message);
    }

    public static void Warning(ChannelType channel, string message)
    {
        Log.Warning(logFormat, DateTime.Now.ToString(dateTimeFormat), channel, message);
    }

    public static void Error(ChannelType channel, string message)
    {
        Log.Error(logFormat, DateTime.Now.ToString(dateTimeFormat), channel, message);
    }
}
