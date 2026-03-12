using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityFramework.Runtime
{
    /// <summary>
    /// 显示日志界面
    /// </summary>
    public class UILogModule : UIModuleBase
    {
        /// <summary>
        /// 日志容器
        /// </summary>
        public Transform logContent;
        /// <summary>
        /// 日志预制体
        /// </summary>
        public GameObject logPrefab;
        /// <summary>
        /// 正常日志图标
        /// </summary>
        public Sprite icon_info;
        /// <summary>
        /// 警告日志图标
        /// </summary>
        public Sprite icon_warning;
        /// <summary>
        /// 错误日志图标
        /// </summary>
        public Sprite icon_error;
        /// <summary>
        /// 日志深色背景色
        /// </summary>
        public Color32 bg_back = new Color32(227, 232, 235, 255);
        /// <summary>
        /// 日志浅色背景色
        /// </summary>
        public Color32 bg_thumb = new Color32(191, 195, 197, 255);

        /// <summary>
        /// 判断日志背景颜色，间隔颜色方便查看
        /// </summary>
        private bool isDual = false;
        /// <summary>
        /// 显示日志对象集合
        /// </summary>
        private List<GameObject> logs = new List<GameObject>();

        /// <summary>
        /// 日志详情显示
        /// </summary>
        public Text logDetails;

        /// <summary>
        /// 清除日志按钮
        /// </summary>
        public Button clear;
        /// <summary>
        /// 复制日志按钮
        /// </summary>
        public Button copy;
        /// <summary>
        /// 折叠日志开关
        /// </summary>
        public Toggle collapse;
        /// <summary>
        /// 正常日志显示/隐藏开关
        /// </summary>
        public Toggle showLog;
        /// <summary>
        /// 警告日志显示/隐藏开关
        /// </summary>
        public Toggle showWarning;
        /// <summary>
        /// 错误日志显示/隐藏开关
        /// </summary>
        public Toggle showError;

        /// <summary>
        /// 正常日志总数
        /// </summary>
        public Text numLog;
        /// <summary>
        /// 警告日志总数
        /// </summary>
        public Text numWarning;
        /// <summary>
        /// 错误日志总数
        /// </summary>
        public Text numError;
        /// <summary>
        /// 日志显示滑动视窗
        /// </summary>
        public ScrollRect sc;
        /// <summary>
        /// 搜索输入框
        /// </summary>
        public InputField find;

        /// <summary>
        /// 日志详情
        /// </summary>
        private string logDetailsStr;
        /// <summary>
        /// 日志详情最大显示字符数
        /// </summary>
        private int logDetails_Max = 2048;

        public override void Open(UIData uiData = null)
        {
            AddMsg(new ushort[] {
            (ushort)DebugEvent.RefreshLog,
            (ushort)DebugEvent.AddLog
        });

            clear.onClick.AddListener(() =>
            {
                for (int i = 0; i < logs.Count; i++)
                {
                    Destroy(logs[i]);
                }
                logs.Clear();
                LogManager.Instance.ClearAllLogs();
            });
            copy.onClick.AddListener(() =>
            {
                GUIUtility.systemCopyBuffer = logDetailsStr;
            });
            collapse.onValueChanged.AddListener((isOn) =>
            {
                LogManager.Instance.isCollapse = isOn;
                LogManager.Instance.CalculateCurrentLog();
            });
            showLog.onValueChanged.AddListener((isOn) =>
            {
                LogManager.Instance.isShowLog = isOn;
                LogManager.Instance.CalculateCurrentLog();
            });
            showWarning.onValueChanged.AddListener((isOn) =>
            {
                LogManager.Instance.isShowWarning = isOn;
                LogManager.Instance.CalculateCurrentLog();
            });
            showError.onValueChanged.AddListener((isOn) =>
            {
                LogManager.Instance.isShowError = isOn;
                LogManager.Instance.CalculateCurrentLog();
            });
            find.onValueChanged.AddListener((text) =>
            {
                OnClick("", "");//清空查看详情日志
                LogManager.Instance.filterText = text;
                LogManager.Instance.CalculateCurrentLog();
            });
        }

        public override void Show(UIData uiData = null)
        {
            base.Show(uiData);

            collapse.SetIsOnWithoutNotify(LogManager.Instance.isCollapse);

            RefreshLogs(LogManager.Instance.currentLog);
            RefreshLogsNum();
            StartCoroutine(RefreshLogsScrollbar());
        }
        /// <summary>
        /// 刷新日志
        /// </summary>
        /// <param name="currentLog">日志集合</param>
        private void RefreshLogs(List<LogData> currentLog)
        {
            isDual = false;
            for (int i = 0; i < logs.Count; i++)
            {
                logs[i].SetActive(false);
            }

            LogData data = null;
            for (int i = 0; i < currentLog.Count; i++)
            {
                data = currentLog[i];
                if (i < logs.Count)
                    AddLog(data, LogManager.Instance.isCollapse, logs[i]);
                else
                    AddLog(data, LogManager.Instance.isCollapse);
            }
        }
        /// <summary>
        /// 添加日志
        /// </summary>
        /// <param name="data">日志数据</param>
        /// <param name="isShowNum">是否显示数量</param>
        /// <param name="go">日志对象</param>
        private void AddLog(LogData data, bool isShowNum = false, GameObject go = null)
        {
            if (data == null)
                return;

            if (go == null)
            {
                go = Instantiate(logPrefab, logContent);
                logs.Add(go);
            }

            go.SetActive(true);
            LogItem item = go.GetComponent<LogItem>();

            //判断log背景颜色，间隔颜色方便查看
            Color32 color;
            if (isDual)
            {
                isDual = false;
                color = bg_back;
            }
            else
            {
                isDual = true;
                color = bg_thumb;
            }

            Sprite icon = null;
            switch (data.logType)
            {
                case LogType.Error:
                    icon = icon_error;
                    break;
                case LogType.Assert:
                    icon = icon_error;
                    break;
                case LogType.Warning:
                    icon = icon_warning;
                    break;
                case LogType.Log:
                    icon = icon_info;
                    break;
                case LogType.Exception:
                    icon = icon_error;
                    break;
                default:
                    break;
            }

            item.Show(color, icon, data.time, data.condition, data.count, isShowNum,
                () => OnClick(data.condition, data.stacktrace));
        }
        /// <summary>
        /// 日志点击查看详情事件
        /// </summary>
        /// <param name="condition">打印信息</param>
        /// <param name="stacktrace">堆栈踪迹</param>
        private void OnClick(string condition, string stacktrace)
        {
            logDetailsStr = condition + "\n" + stacktrace;
            if (logDetailsStr.Length > logDetails_Max)
                logDetails.text = logDetailsStr.Substring(0, logDetails_Max) + "...";
            else
                logDetails.text = logDetailsStr;
        }
        /// <summary>
        /// 刷新日志数量显示
        /// </summary>
        private void RefreshLogsNum()
        {
            if (LogManager.Instance.isCollapse)
                LogManager.Instance.GetNumOfCollapsedLogs(RefreshLogsNum);
            else
                LogManager.Instance.GetNumOfLogs(RefreshLogsNum);
        }
        /// <summary>
        /// 刷新日志数量显示
        /// </summary>
        /// <param name="_numLog">正常日志数</param>
        /// <param name="_numWarning">警告日志数</param>
        /// <param name="_numError">错误日志数</param>
        private void RefreshLogsNum(int _numLog, int _numWarning, int _numError)
        {
            if (_numLog > 999)
                numLog.text = "999+";
            else
                numLog.text = _numLog.ToString();

            if (_numWarning > 999)
                numWarning.text = "999+";
            else
                numWarning.text = _numWarning.ToString();

            if (_numError > 999)
                numError.text = "999+";
            else
                numError.text = _numError.ToString();
        }
        /// <summary>
        /// 默认显示最后的日志
        /// </summary>
        /// <returns></returns>
        IEnumerator RefreshLogsScrollbar()
        {
            bool isRefresh = false;
            if (sc.verticalNormalizedPosition <= 0.001f)
                isRefresh = true;

            yield return null;

            if (isRefresh)
                sc.verticalNormalizedPosition = 0f;
        }
        public override void ProcessEvent(MsgBase msg)
        {
            base.ProcessEvent(msg);
            if (!gameObject.activeSelf) { return; }
            switch (msg.msgId)
            {
                case (ushort)DebugEvent.RefreshLog://刷新日志
                    RefreshLogs(LogManager.Instance.currentLog);

                    RefreshLogsNum();
                    StartCoroutine(RefreshLogsScrollbar());
                    break;
                case (ushort)DebugEvent.AddLog://添加单个日志
                    AddLog(((MsgDbug)msg).arg, LogManager.Instance.isCollapse);

                    RefreshLogsNum();
                    StartCoroutine(RefreshLogsScrollbar());
                    break;
                default:
                    break;
            }
        }
    }
}