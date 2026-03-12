using UnityEngine;

namespace UnityFramework.Runtime
{
    /// <summary>
    /// 日志数据类
    /// </summary>
    public class LogData
    {
        /// <summary>
        /// 日志id
        /// </summary>
        public int index;
        /// <summary>
        /// 日志打印时间
        /// </summary>
        public string time;
        /// <summary>
        /// 日志类型
        /// </summary>
        public LogType logType;
        /// <summary>
        /// 日志内容
        /// </summary>
        public string condition;
        /// <summary>
        /// 日志堆栈
        /// </summary>
        public string stacktrace;
        /// <summary>
        /// 日志个数
        /// </summary>
        public int count = 1;
    }
}