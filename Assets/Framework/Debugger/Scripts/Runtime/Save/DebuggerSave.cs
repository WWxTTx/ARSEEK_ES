using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityFramework.Runtime
{
    /// <summary>
    /// 调试信息保存类
    /// </summary>
    public class DebuggerSave : MonoBase
    {
        /// <summary>
        /// 是否启用上传功能
        /// </summary>
        public static bool UploadEnabled;

        /// <summary>
        /// 日志文件夹路径
        /// </summary>
        private string dir { get { return Application.persistentDataPath; } }
        /// <summary>
        /// 日志文件前缀
        /// </summary>
        private string fileNamePrefix { get { return "Log"; } }
        /// <summary>
        /// 已保存日志文件
        /// </summary>
        private List<string> logFiles = new List<string>();
        /// <summary>
        /// 日志最大存储数量
        /// </summary>
        private int logNumMax = 10;
        /// <summary>
        /// 当前日志路径
        /// </summary>
        private string filePath;
        /// <summary>
        /// 缓存日志集合
        /// </summary>
        private List<LogData> threadedLogs = new List<LogData>();
        /// <summary>
        /// 错误日志数
        /// </summary>
        private int errorNum;
        /// <summary>
        /// 日志提交间隔
        /// </summary>
        private float interval = 60f;
        /// <summary>
        /// 日志提交间隔计时器
        /// </summary>
        private float timer;

        void Awake()
        {
            AddMsg(new ushort[] {
            (ushort)DebugEvent.SaveLog
            });
            timer = interval;
            //查找历史日志
            string[] files = Directory.GetFiles(dir);
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].Contains(fileNamePrefix))
                {
                    logFiles.Add(files[i]);
                }
            }

            //删除多余历史日志
            while (logFiles.Count >= logNumMax)
            {
                FileTool.FileDelete(logFiles[0]);
                logFiles.RemoveAt(0);
            }

            //创建新日志
            string time = ((int)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds).ToString();
            filePath = dir + "/" + fileNamePrefix + time;

            string str = DateTime.Now.ToLocalTime().ToString() + "\n";
            List<string> datas = UIInformationModule.GetDevInfo();
            for (int i = 0; i < datas.Count; i++)
            {
                str += datas[i] + "\n";
            }
            FileTool.FileWrite(filePath, System.Text.Encoding.Default.GetBytes(str));
        }
        /// <summary>
        /// 缓存日志
        /// </summary>
        /// <param name="condition">打印信息</param>
        /// <param name="stacktrace">堆栈踪迹</param>
        /// <param name="type">日志类型</param>
        void CaptureLogThread(LogData log)
        {
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
                    SaveLogs(threadedLogs);
                    threadedLogs.Clear();
                }
            }

            if (errorNum > 0)
            {
                timer += Time.deltaTime;
                if (timer >= interval)
                {
                    timer = 0;
                    errorNum = 0;
                    if (ApiData.state == 1 || UploadEnabled)
                        RequestManager.Instance.PostLog(filePath);
                }

            }
        }

        /// <summary>
        /// 保存日志集合
        /// </summary>
        /// <param name="threadedLogs">保存日志集合</param>
        private void SaveLogs(List<LogData> threadedLogs)
        {
            string str = "\n";
            for (int i = 0; i < threadedLogs.Count; i++)
            {
                LogData data = threadedLogs[i];
                str += "[" + data.time + "]";
                switch (data.logType)
                {
                    case LogType.Error:
                        errorNum += 1;
                        str += "error:";
                        break;
                    case LogType.Assert:
                        errorNum += 1;
                        str += "error:";
                        break;
                    case LogType.Warning:
                        str += "warning:";
                        break;
                    case LogType.Log:
                        str += "info:";
                        break;
                    case LogType.Exception:
                        errorNum += 1;
                        str += "error:";
                        break;
                    default:
                        break;
                }
                str += data.condition + "\n" + data.stacktrace + "\n";
            }

            byte[] byteArray = System.Text.Encoding.Default.GetBytes(str);
            FileTool.FileAdd(filePath, byteArray);
        }

        public override void ProcessEvent(MsgBase msg)
        {
            base.ProcessEvent(msg);
            switch (msg.msgId)
            {
                case (ushort)DebugEvent.SaveLog:
                    CaptureLogThread(((MsgDbug)msg).arg);
                    break;
                default:
                    break;
            }
        }
    }
}