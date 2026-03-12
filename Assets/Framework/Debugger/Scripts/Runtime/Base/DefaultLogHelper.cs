using Framework.Debugger;
using System;
using UnityEngine;

namespace UnityFramework.Runtime
{
    /// <summary>
    /// 默认框架日志辅助器。
    /// </summary>
    public class DefaultLogHelper : FrameworkLog.ILogHelper
    {
        /// <summary>
        /// 记录日志。
        /// </summary>
        /// <param name="level">日志等级。</param>
        /// <param name="message">日志内容。</param>
        public void Log(FrameworkLogLevel level, object message)
        {
            switch (level)
            {
                case FrameworkLogLevel.Debug:
                    Debug.Log(string.Format("<color=#888888>{0}</color>", message.ToString()));
                    break;

                case FrameworkLogLevel.Info:
                    Debug.Log(message.ToString());
                    break;

                case FrameworkLogLevel.Warning:
                    Debug.LogWarning(message.ToString());
                    break;

                case FrameworkLogLevel.Error:
                    Debug.LogError(message.ToString());
                    break;
                case FrameworkLogLevel.Fatal:
                    Debug.LogError(string.Format("<color=#FF0000>{0}</color>", message.ToString()));
                    break;
                default:
                    throw new Exception(message.ToString());
            }
        }
    }
}

