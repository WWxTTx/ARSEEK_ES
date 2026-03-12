using UnityEditor;

namespace UnityFramework.Editor
{
    /// <summary>
    /// 日志脚本宏定义。
    /// </summary>
    public static class LogScriptingDefineSymbols
    {
        private const string ScriptingDefineSymbol = "DEFINE_SYMBOL";

        private const string EnableLogScriptingDefineSymbol = "ENABLE_LOG";
        private const string EnableDebugAndAboveLogScriptingDefineSymbol = "ENABLE_DEBUG_AND_ABOVE_LOG";
        private const string EnableInfoAndAboveLogScriptingDefineSymbol = "ENABLE_INFO_AND_ABOVE_LOG";
        private const string EnableWarningAndAboveLogScriptingDefineSymbol = "ENABLE_WARNING_AND_ABOVE_LOG";
        private const string EnableErrorAndAboveLogScriptingDefineSymbol = "ENABLE_ERROR_AND_ABOVE_LOG";
        private const string EnableFatalAndAboveLogScriptingDefineSymbol = "ENABLE_FATAL_AND_ABOVE_LOG";
        private const string EnableDebugLogScriptingDefineSymbol = "ENABLE_DEBUG_LOG";
        private const string EnableInfoLogScriptingDefineSymbol = "ENABLE_INFO_LOG";
        private const string EnableWarningLogScriptingDefineSymbol = "ENABLE_WARNING_LOG";
        private const string EnableErrorLogScriptingDefineSymbol = "ENABLE_ERROR_LOG";
        private const string EnableFatalLogScriptingDefineSymbol = "ENABLE_FATAL_LOG";

        private static readonly string[] AboveLogScriptingDefineSymbols = new string[]
        {
            EnableDebugAndAboveLogScriptingDefineSymbol,
            EnableInfoAndAboveLogScriptingDefineSymbol,
            EnableWarningAndAboveLogScriptingDefineSymbol,
            EnableErrorAndAboveLogScriptingDefineSymbol,
            EnableFatalAndAboveLogScriptingDefineSymbol
        };

        private static readonly string[] SpecifyLogScriptingDefineSymbols = new string[]
        {
            EnableDebugLogScriptingDefineSymbol,
            EnableInfoLogScriptingDefineSymbol,
            EnableWarningLogScriptingDefineSymbol,
            EnableErrorLogScriptingDefineSymbol,
            EnableFatalLogScriptingDefineSymbol
        };

        private const string DisableAllLogsPath = "Framework/Log Scripting Define Symbols/Disable All Logs";
        private const string EnableAllLogsPath = "Framework/Log Scripting Define Symbols/Enable All Logs";
        private const string EnableDebugAndAboveLogsPath = "Framework/Log Scripting Define Symbols/Enable Debug And Above Logs";
        private const string EnableInfoAndAboveLogsPath = "Framework/Log Scripting Define Symbols/Enable Info And Above Logs";
        private const string EnableWarningAndAboveLogsPath = "Framework/Log Scripting Define Symbols/Enable Warning And Above Logs";
        private const string EnableErrorAndAboveLogsPath = "Framework/Log Scripting Define Symbols/Enable Error And Above Logs";
        private const string EnableFatalAndAboveLogsPath = "Framework/Log Scripting Define Symbols/Enable Fatal And Above Logs";
        public static int DefineSymbolLevel
        {
            get { return EditorPrefs.GetInt(ScriptingDefineSymbol, 0); }
            set { EditorPrefs.SetInt(ScriptingDefineSymbol, value); }
        }

        /// <summary>
        /// 禁用所有日志脚本宏定义。
        /// </summary>
        [MenuItem(DisableAllLogsPath, false, 30)]
        public static void DisableAllLogs()
        {
            if (DefineSymbolLevel == 0)
                return;

            ScriptingDefineSymbols.RemoveScriptingDefineSymbol(EnableLogScriptingDefineSymbol);

            foreach (string specifyLogScriptingDefineSymbol in SpecifyLogScriptingDefineSymbols)
            {
                ScriptingDefineSymbols.RemoveScriptingDefineSymbol(specifyLogScriptingDefineSymbol);
            }

            foreach (string aboveLogScriptingDefineSymbol in AboveLogScriptingDefineSymbols)
            {
                ScriptingDefineSymbols.RemoveScriptingDefineSymbol(aboveLogScriptingDefineSymbol);
            }
            DefineSymbolLevel = 0;
        }
        [MenuItem(DisableAllLogsPath, true)]
        public static bool DisableAllLogsShow()
        {
            Menu.SetChecked(DisableAllLogsPath, DefineSymbolLevel == 0);
            return true;
        }

        /// <summary>
        /// 开启所有日志脚本宏定义。
        /// </summary>
        [MenuItem(EnableAllLogsPath, false, 31)]
        public static void EnableAllLogs()
        {
            if (DefineSymbolLevel == 1)
                return;

            DisableAllLogs();
            ScriptingDefineSymbols.AddScriptingDefineSymbol(EnableLogScriptingDefineSymbol);
            DefineSymbolLevel = 1;
        }
        [MenuItem(EnableAllLogsPath, true)]
        public static bool EnableAllLogsShow()
        {
            Menu.SetChecked(EnableAllLogsPath, DefineSymbolLevel == 1);
            return true;
        }

        /// <summary>
        /// 开启调试及以上级别的日志脚本宏定义。
        /// </summary>
        [MenuItem(EnableDebugAndAboveLogsPath, false, 32)]
        public static void EnableDebugAndAboveLogs()
        {
            if (DefineSymbolLevel == 2)
                return;

            SetAboveLogScriptingDefineSymbol(EnableDebugAndAboveLogScriptingDefineSymbol);
            DefineSymbolLevel = 2;
        }
        [MenuItem(EnableDebugAndAboveLogsPath, true)]
        public static bool EnableDebugAndAboveLogsShow()
        {
            Menu.SetChecked(EnableDebugAndAboveLogsPath, DefineSymbolLevel == 2);
            return true;
        }

        /// <summary>
        /// 开启信息及以上级别的日志脚本宏定义。
        /// </summary>
        [MenuItem(EnableInfoAndAboveLogsPath, false, 33)]
        public static void EnableInfoAndAboveLogs()
        {
            if (DefineSymbolLevel == 3)
                return;

            SetAboveLogScriptingDefineSymbol(EnableInfoAndAboveLogScriptingDefineSymbol);
            DefineSymbolLevel = 3;
        }
        [MenuItem(EnableInfoAndAboveLogsPath, true)]
        public static bool EnableInfoAndAboveLogsPathShow()
        {
            Menu.SetChecked(EnableInfoAndAboveLogsPath, DefineSymbolLevel == 3);
            return true;
        }

        /// <summary>
        /// 开启警告及以上级别的日志脚本宏定义。
        /// </summary>
        [MenuItem(EnableWarningAndAboveLogsPath, false, 34)]
        public static void EnableWarningAndAboveLogs()
        {
            if (DefineSymbolLevel == 4)
                return;

            SetAboveLogScriptingDefineSymbol(EnableWarningAndAboveLogScriptingDefineSymbol);
            DefineSymbolLevel = 4;
        }
        [MenuItem(EnableWarningAndAboveLogsPath, true)]
        public static bool EnableWarningAndAboveLogsPathShow()
        {
            Menu.SetChecked(EnableWarningAndAboveLogsPath, DefineSymbolLevel == 4);
            return true;
        }

        /// <summary>
        /// 开启错误及以上级别的日志脚本宏定义。
        /// </summary>
        [MenuItem(EnableErrorAndAboveLogsPath, false, 35)]
        public static void EnableErrorAndAboveLogs()
        {
            if (DefineSymbolLevel == 5)
                return;

            SetAboveLogScriptingDefineSymbol(EnableErrorAndAboveLogScriptingDefineSymbol);
            DefineSymbolLevel = 5;
        }
        [MenuItem(EnableErrorAndAboveLogsPath, true)]
        public static bool EnableErrorAndAboveLogsPathShow()
        {
            Menu.SetChecked(EnableErrorAndAboveLogsPath, DefineSymbolLevel == 5);
            return true;
        }

        /// <summary>
        /// 开启严重错误及以上级别的日志脚本宏定义。
        /// </summary>
        [MenuItem(EnableFatalAndAboveLogsPath, false, 36)]
        public static void EnableFatalAndAboveLogs()
        {
            if (DefineSymbolLevel == 6)
                return;

            SetAboveLogScriptingDefineSymbol(EnableFatalAndAboveLogScriptingDefineSymbol);
            DefineSymbolLevel = 6;
        }
        [MenuItem(EnableFatalAndAboveLogsPath, true)]
        public static bool EnableFatalAndAboveLogsPathShow()
        {
            Menu.SetChecked(EnableFatalAndAboveLogsPath, DefineSymbolLevel == 6);
            return true;
        }

        /// <summary>
        /// 设置日志脚本宏定义。
        /// </summary>
        /// <param name="aboveLogScriptingDefineSymbol">要设置的日志脚本宏定义。</param>
        public static void SetAboveLogScriptingDefineSymbol(string aboveLogScriptingDefineSymbol)
        {
            if (string.IsNullOrEmpty(aboveLogScriptingDefineSymbol))
            {
                return;
            }

            foreach (string i in AboveLogScriptingDefineSymbols)
            {
                if (i == aboveLogScriptingDefineSymbol)
                {
                    DisableAllLogs();
                    ScriptingDefineSymbols.AddScriptingDefineSymbol(aboveLogScriptingDefineSymbol);
                    return;
                }
            }
        }

        /// <summary>
        /// 设置日志脚本宏定义。
        /// </summary>
        /// <param name="specifyLogScriptingDefineSymbols">要设置的日志脚本宏定义。</param>
        public static void SetSpecifyLogScriptingDefineSymbols(string[] specifyLogScriptingDefineSymbols)
        {
            if (specifyLogScriptingDefineSymbols == null || specifyLogScriptingDefineSymbols.Length <= 0)
            {
                return;
            }

            bool removed = false;
            foreach (string specifyLogScriptingDefineSymbol in specifyLogScriptingDefineSymbols)
            {
                if (string.IsNullOrEmpty(specifyLogScriptingDefineSymbol))
                {
                    continue;
                }

                foreach (string i in SpecifyLogScriptingDefineSymbols)
                {
                    if (i == specifyLogScriptingDefineSymbol)
                    {
                        if (!removed)
                        {
                            removed = true;
                            DisableAllLogs();
                        }

                        ScriptingDefineSymbols.AddScriptingDefineSymbol(specifyLogScriptingDefineSymbol);
                        break;
                    }
                }
            }
        }
    }
}
