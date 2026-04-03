using Framework.Debugger;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityFramework.Runtime
{
    /// <summary>
    /// 调试信息管理类
    /// </summary>
    public class LogManager : Singleton<LogManager>
    {
        /// <summary>
        /// 是否可以显示日志UI
        /// </summary>
        public bool show = false;
        /// <summary>
        /// 显示日志UI所需转圈数
        /// </summary>
        public int numOfCircleToShow = 5;

        /// <summary>
        /// 缓存日志集合
        /// </summary>
        private List<LogData> threadedLogs = new List<LogData>();
        /// <summary>
        /// 所有未折叠的日志
        /// </summary>
        private List<LogData> logs = new List<LogData>();
        /// <summary>
        /// 所有折叠的日志
        /// </summary>
        private List<LogData> collapsedLogs = new List<LogData>();
        /// <summary>
        /// 当前显示日志
        /// </summary>
        [HideInInspector]
        public List<LogData> currentLog = new List<LogData>();
        /// <summary>
        /// 用于检查新的日志是否已经存在或已经有新的日志
        /// </summary>
        MultiKeyDictionary<string, string, LogData> logsDic = new MultiKeyDictionary<string, string, LogData>();

        /// <summary>
        /// 是否折叠重复日志
        /// </summary>
        [HideInInspector]
        public bool isCollapse = false;
        /// <summary>
        /// 是否显示正常日志
        /// </summary>
        [HideInInspector]
        public bool isShowLog = true;
        /// <summary>
        /// 是否显示警告日志
        /// </summary>
        [HideInInspector]
        public bool isShowWarning = true;
        /// <summary>
        /// 是否显示错误日志
        /// </summary>
        [HideInInspector]
        public bool isShowError = true;

        /// <summary>
        /// 正常日志总数
        /// </summary>
        private int numOfLogs = 0;
        /// <summary>
        /// 警告日志总数
        /// </summary>
        private int numOfLogsWarning = 0;
        /// <summary>
        /// 错误日志总数
        /// </summary>
        private int numOfLogsError = 0;
        /// <summary>
        /// 正常日志去掉重复后总数
        /// </summary>
        private int numOfCollapsedLogs = 0;
        /// <summary>
        /// 警告日志去掉重复后总数
        /// </summary>
        private int numOfCollapsedLogsWarning = 0;
        /// <summary>
        /// 错误日志去掉重复后总数
        /// </summary>
        private int numOfCollapsedLogsError = 0;

        /// <summary>
        /// 搜索文本
        /// </summary>
        [HideInInspector]
        public string filterText = "";

        protected override void InstanceAwake()
        {
            FrameworkLog.SetLogHelper(new DefaultLogHelper());
            Application.logMessageReceivedThreaded += CaptureLogThread;
            gameObject.AddComponent<DebuggerSave>();
        }
        /// <summary>
        /// 缓存日志
        /// </summary>
        /// <param name="condition">打印信息</param>
        /// <param name="stacktrace">堆栈踪迹</param>
        /// <param name="type">日志类型</param>
        void CaptureLogThread(string condition, string stacktrace, LogType type)
        {
            LogData log = new LogData() 
            {
                condition = condition, 
                stacktrace = stacktrace, 
                logType = type 
            };
            lock (threadedLogs)
            {
                threadedLogs.Add(log);
            }
        }
        void Update()
        {
            if (threadedLogs.Count > 0)
            {
                lock (threadedLogs)
                {
                    for (int i = 0; i < threadedLogs.Count; i++)
                    {
                        LogData l = threadedLogs[i];
                        AddLog(l.condition, l.stacktrace, l.logType);
                    }
                    threadedLogs.Clear();
                }
            }

            if (show && isGestureDone())
            {
                UIManager.Instance.OpenUI<UIDebugger>(UILevel.Top, null, UIDebugger.SystemUIDebuggerPath);
            }
        }

        List<Vector2> gestureDetector = new List<Vector2>();
        Vector2 gestureSum = Vector2.zero;
        float gestureLength = 0;
        int gestureCount = 0;
        bool isGestureDone()
        {
            if (Application.platform == RuntimePlatform.Android ||
                Application.platform == RuntimePlatform.IPhonePlayer)
            {
                if (Input.touches.Length != 1)
                {
                    gestureDetector.Clear();
                    gestureCount = 0;
                }
                else
                {
                    if (Input.touches[0].phase == TouchPhase.Canceled || Input.touches[0].phase == TouchPhase.Ended)
                        gestureDetector.Clear();
                    else if (Input.touches[0].phase == TouchPhase.Moved)
                    {
                        Vector2 p = Input.touches[0].position;
                        if (gestureDetector.Count == 0 || (p - gestureDetector[gestureDetector.Count - 1]).magnitude > 10)
                            gestureDetector.Add(p);
                    }
                }
            }
            else
            {
                if (Input.GetMouseButtonUp(0))
                {
                    gestureDetector.Clear();
                    gestureCount = 0;
                }
                else
                {
                    if (Input.GetMouseButton(0))
                    {
                        Vector2 p = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                        if (gestureDetector.Count == 0 || (p - gestureDetector[gestureDetector.Count - 1]).magnitude > 10)
                            gestureDetector.Add(p);
                    }
                }
            }

            if (gestureDetector.Count < 10)
                return false;

            gestureSum = Vector2.zero;
            gestureLength = 0;
            Vector2 prevDelta = Vector2.zero;
            for (int i = 0; i < gestureDetector.Count - 2; i++)
            {

                Vector2 delta = gestureDetector[i + 1] - gestureDetector[i];
                float deltaLength = delta.magnitude;
                gestureSum += delta;
                gestureLength += deltaLength;

                float dot = Vector2.Dot(delta, prevDelta);
                if (dot < 0f)
                {
                    gestureDetector.Clear();
                    gestureCount = 0;
                    return false;
                }

                prevDelta = delta;
            }

            int gestureBase = (Screen.width + Screen.height) / 4;

            if (gestureLength > gestureBase && gestureSum.magnitude < gestureBase / 2)
            {
                gestureDetector.Clear();
                gestureCount++;
                if (gestureCount >= numOfCircleToShow)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 添加日志
        /// </summary>
        /// <param name="condition">打印信息</param>
        /// <param name="stacktrace">堆栈踪迹</param>
        /// <param name="type">日志类型</param>
        void AddLog(string condition, string stacktrace, LogType type)
        {
            LogData log = new LogData()
            {
                time = DateTime.Now.ToLocalTime().ToString("HH:mm:ss"),
                logType = type,
                condition = condition,
                stacktrace = stacktrace,
                index = logs.Count
            };

            FormMsgManager.Instance.SendMsg(new MsgDbug((ushort)DebugEvent.SaveLog, log));

            //判断日志是否重复
            bool isNew = false;
            if (logsDic.ContainsKey(condition, stacktrace))
            {
                isNew = false;
                logsDic[condition][stacktrace].count++;
            }
            else
            {
                isNew = true;
                collapsedLogs.Add(log);
                logsDic[condition][stacktrace] = log;

                if (type == LogType.Log)
                    numOfCollapsedLogs++;
                else if (type == LogType.Warning)
                    numOfCollapsedLogsWarning++;
                else
                    numOfCollapsedLogsError++;
            }

            if (type == LogType.Log)
                numOfLogs++;
            else if (type == LogType.Warning)
                numOfLogsWarning++;
            else
                numOfLogsError++;

            logs.Add(log);

            //判断日志是否添加到显示日志中
            if (!isCollapse || isNew)
            {
                bool skip = false;
                if (log.logType == LogType.Log && !isShowLog)
                    skip = true;
                if (log.logType == LogType.Warning && !isShowWarning)
                    skip = true;
                if (log.logType == LogType.Error && !isShowError)
                    skip = true;
                if (log.logType == LogType.Assert && !isShowError)
                    skip = true;
                if (log.logType == LogType.Exception && !isShowError)
                    skip = true;

                if (!skip)
                {
                    if (string.IsNullOrEmpty(filterText) || log.condition.ToLower().Contains(filterText.ToLower()))
                    {
                        currentLog.Add(log);
                        FormMsgManager.Instance.SendMsg(new MsgDbug((ushort)DebugEvent.AddLog, log));
                        return;
                    }
                }
            }

            FormMsgManager.Instance.SendMsg(new MsgDbug((ushort)DebugEvent.RefreshLog, log));
        }
        /// <summary>
        /// 计算当前显示日志
        /// </summary>
        public void CalculateCurrentLog()
        {
            bool filter = !string.IsNullOrEmpty(filterText);
            string _filterText = "";
            if (filter)
                _filterText = filterText.ToLower();
            currentLog.Clear();
            if (isCollapse)
            {
                for (int i = 0; i < collapsedLogs.Count; i++)
                {
                    LogData log = collapsedLogs[i];
                    if (log.logType == LogType.Log && !isShowLog)
                        continue;
                    if (log.logType == LogType.Warning && !isShowWarning)
                        continue;
                    if (log.logType == LogType.Error && !isShowError)
                        continue;
                    if (log.logType == LogType.Assert && !isShowError)
                        continue;
                    if (log.logType == LogType.Exception && !isShowError)
                        continue;

                    if (filter)
                    {
                        if (log.condition.ToLower().Contains(_filterText))
                            currentLog.Add(log);
                    }
                    else
                        currentLog.Add(log);
                }
            }
            else
            {
                for (int i = 0; i < logs.Count; i++)
                {
                    LogData log = logs[i];
                    if (log.logType == LogType.Log && !isShowLog)
                        continue;
                    if (log.logType == LogType.Warning && !isShowWarning)
                        continue;
                    if (log.logType == LogType.Error && !isShowError)
                        continue;
                    if (log.logType == LogType.Assert && !isShowError)
                        continue;
                    if (log.logType == LogType.Exception && !isShowError)
                        continue;

                    if (filter)
                    {
                        if (log.condition.ToLower().Contains(_filterText))
                            currentLog.Add(log);
                    }
                    else
                        currentLog.Add(log);
                }
            }

            FormMsgManager.Instance.SendMsg(new MsgBase((ushort)DebugEvent.RefreshLog));
        }
        /// <summary>
        /// 清除所有日志
        /// </summary>
        public void ClearAllLogs()
        {
            logs.Clear();
            collapsedLogs.Clear();
            currentLog.Clear();
            logsDic.Clear();
            numOfLogs = 0;
            numOfLogsWarning = 0;
            numOfLogsError = 0;
            numOfCollapsedLogs = 0;
            numOfCollapsedLogsWarning = 0;
            numOfCollapsedLogsError = 0;
            GC.Collect();
        }
        /// <summary>
        /// 获取各日志数量
        /// </summary>
        /// <param name="action"></param>
        public void GetNumOfLogs(Action<int, int, int> action)
        {
            action?.Invoke(numOfLogs, numOfLogsWarning, numOfLogsError);
        }
        /// <summary>
        /// 获取各日志去掉重复后数量
        /// </summary>
        /// <param name="action"></param>
        public void GetNumOfCollapsedLogs(Action<int, int, int> action)
        {
            action?.Invoke(numOfCollapsedLogs, numOfCollapsedLogsWarning, numOfCollapsedLogsError);
        }
    }

}
