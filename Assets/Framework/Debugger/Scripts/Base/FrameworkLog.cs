namespace Framework.Debugger
{
    /// <summary>
    /// 框架日志类。
    /// </summary>
    public static partial class FrameworkLog
    {
        private static ILogHelper s_LogHelper = null;

        /// <summary>
        /// 设置框架日志辅助器。
        /// </summary>
        /// <param name="logHelper">要设置的框架日志辅助器。</param>
        public static void SetLogHelper(ILogHelper logHelper)
        {
            s_LogHelper = logHelper;
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <param name="message">日志内容。</param>
        public static void Debug(object message)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Debug, message);
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <param name="message">日志内容。</param>
        public static void Debug(string message)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Debug, message);
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T">日志参数的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg">日志参数。</param>
        public static void Debug<T>(string format, T arg)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Debug, string.Format(format, arg));
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        public static void Debug<T1, T2>(string format, T1 arg1, T2 arg2)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Debug, string.Format(format, arg1, arg2));
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        public static void Debug<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Debug, string.Format(format, arg1, arg2, arg3));
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        public static void Debug<T1, T2, T3, T4>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Debug, string.Format(format, arg1, arg2, arg3, arg4));
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        public static void Debug<T1, T2, T3, T4, T5>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Debug, string.Format(format, arg1, arg2, arg3, arg4, arg5));
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        public static void Debug<T1, T2, T3, T4, T5, T6>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Debug, string.Format(format, arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        public static void Debug<T1, T2, T3, T4, T5, T6, T7>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Debug, string.Format(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        public static void Debug<T1, T2, T3, T4, T5, T6, T7, T8>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Debug, string.Format(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        public static void Debug<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Debug, string.Format(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9));
        }

        /// <summary>
        /// 打印调试级别日志，用于记录调试类日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        public static void Debug<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Debug, string.Format(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10));
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <param name="message">日志内容。</param>
        public static void Info(object message)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Info, message);
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <param name="message">日志内容。</param>
        public static void Info(string message)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Info, message);
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T">日志参数的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg">日志参数。</param>
        public static void Info<T>(string format, T arg)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Info, string.Format(format, arg));
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        public static void Info<T1, T2>(string format, T1 arg1, T2 arg2)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Info, string.Format(format, arg1, arg2));
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        public static void Info<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Info, string.Format(format, arg1, arg2, arg3));
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        public static void Info<T1, T2, T3, T4>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Info, string.Format(format, arg1, arg2, arg3, arg4));
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        public static void Info<T1, T2, T3, T4, T5>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Info, string.Format(format, arg1, arg2, arg3, arg4, arg5));
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        public static void Info<T1, T2, T3, T4, T5, T6>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Info, string.Format(format, arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        public static void Info<T1, T2, T3, T4, T5, T6, T7>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Info, string.Format(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        public static void Info<T1, T2, T3, T4, T5, T6, T7, T8>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Info, string.Format(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        public static void Info<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Info, string.Format(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9));
        }

        /// <summary>
        /// 打印信息级别日志，用于记录程序正常运行日志信息。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        public static void Info<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Info, string.Format(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10));
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致崩溃或异常时使用。
        /// </summary>
        /// <param name="message">日志内容。</param>
        public static void Warning(object message)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Warning, message);
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致崩溃或异常时使用。
        /// </summary>
        /// <param name="message">日志内容。</param>
        public static void Warning(string message)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Warning, message);
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T">日志参数的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg">日志参数。</param>
        public static void Warning<T>(string format, T arg)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Warning, string.Format(format, arg));
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        public static void Warning<T1, T2>(string format, T1 arg1, T2 arg2)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Warning, string.Format(format, arg1, arg2));
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        public static void Warning<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Warning, string.Format(format, arg1, arg2, arg3));
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        public static void Warning<T1, T2, T3, T4>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Warning, string.Format(format, arg1, arg2, arg3, arg4));
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        public static void Warning<T1, T2, T3, T4, T5>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Warning, string.Format(format, arg1, arg2, arg3, arg4, arg5));
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        public static void Warning<T1, T2, T3, T4, T5, T6>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Warning, string.Format(format, arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        public static void Warning<T1, T2, T3, T4, T5, T6, T7>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Warning, string.Format(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        public static void Warning<T1, T2, T3, T4, T5, T6, T7, T8>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Warning, string.Format(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        public static void Warning<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Warning, string.Format(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9));
        }

        /// <summary>
        /// 打印警告级别日志，建议在发生局部功能逻辑错误，但尚不会导致崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        public static void Warning<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Warning, string.Format(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10));
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致崩溃或异常时使用。
        /// </summary>
        /// <param name="message">日志内容。</param>
        public static void Error(object message)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Error, message);
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致崩溃或异常时使用。
        /// </summary>
        /// <param name="message">日志内容。</param>
        public static void Error(string message)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Error, message);
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T">日志参数的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg">日志参数。</param>
        public static void Error<T>(string format, T arg)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Error, string.Format(format, arg));
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        public static void Error<T1, T2>(string format, T1 arg1, T2 arg2)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Error, string.Format(format, arg1, arg2));
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        public static void Error<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Error, string.Format(format, arg1, arg2, arg3));
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        public static void Error<T1, T2, T3, T4>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Error, string.Format(format, arg1, arg2, arg3, arg4));
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        public static void Error<T1, T2, T3, T4, T5>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Error, string.Format(format, arg1, arg2, arg3, arg4, arg5));
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        public static void Error<T1, T2, T3, T4, T5, T6>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Error, string.Format(format, arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        public static void Error<T1, T2, T3, T4, T5, T6, T7>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Error, string.Format(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        public static void Error<T1, T2, T3, T4, T5, T6, T7, T8>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Error, string.Format(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        public static void Error<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Error, string.Format(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9));
        }

        /// <summary>
        /// 打印错误级别日志，建议在发生功能逻辑错误，但尚不会导致崩溃或异常时使用。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        public static void Error<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Error, string.Format(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10));
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致崩溃或异常时使用，此时应尝试重启进程或重建框架。
        /// </summary>
        /// <param name="message">日志内容。</param>
        public static void Fatal(object message)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Fatal, message);
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致崩溃或异常时使用，此时应尝试重启进程或重建框架。
        /// </summary>
        /// <param name="message">日志内容。</param>
        public static void Fatal(string message)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Fatal, message);
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致崩溃或异常时使用，此时应尝试重启进程或重建框架。
        /// </summary>
        /// <typeparam name="T">日志参数的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg">日志参数。</param>
        public static void Fatal<T>(string format, T arg)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Fatal, string.Format(format, arg));
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致崩溃或异常时使用，此时应尝试重启进程或重建框架。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        public static void Fatal<T1, T2>(string format, T1 arg1, T2 arg2)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Fatal, string.Format(format, arg1, arg2));
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致崩溃或异常时使用，此时应尝试重启进程或重建框架。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        public static void Fatal<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Fatal, string.Format(format, arg1, arg2, arg3));
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致崩溃或异常时使用，此时应尝试重启进程或重建框架。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        public static void Fatal<T1, T2, T3, T4>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Fatal, string.Format(format, arg1, arg2, arg3, arg4));
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致崩溃或异常时使用，此时应尝试重启进程或重建框架。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        public static void Fatal<T1, T2, T3, T4, T5>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Fatal, string.Format(format, arg1, arg2, arg3, arg4, arg5));
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致崩溃或异常时使用，此时应尝试重启进程或重建框架。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        public static void Fatal<T1, T2, T3, T4, T5, T6>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Fatal, string.Format(format, arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致崩溃或异常时使用，此时应尝试重启进程或重建框架。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        public static void Fatal<T1, T2, T3, T4, T5, T6, T7>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Fatal, string.Format(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致崩溃或异常时使用，此时应尝试重启进程或重建框架。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        public static void Fatal<T1, T2, T3, T4, T5, T6, T7, T8>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Fatal, string.Format(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致崩溃或异常时使用，此时应尝试重启进程或重建框架。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        public static void Fatal<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Fatal, string.Format(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9));
        }

        /// <summary>
        /// 打印严重错误级别日志，建议在发生严重错误，可能导致崩溃或异常时使用，此时应尝试重启进程或重建框架。
        /// </summary>
        /// <typeparam name="T1">日志参数 1 的类型。</typeparam>
        /// <typeparam name="T2">日志参数 2 的类型。</typeparam>
        /// <typeparam name="T3">日志参数 3 的类型。</typeparam>
        /// <typeparam name="T4">日志参数 4 的类型。</typeparam>
        /// <typeparam name="T5">日志参数 5 的类型。</typeparam>
        /// <typeparam name="T6">日志参数 6 的类型。</typeparam>
        /// <typeparam name="T7">日志参数 7 的类型。</typeparam>
        /// <typeparam name="T8">日志参数 8 的类型。</typeparam>
        /// <typeparam name="T9">日志参数 9 的类型。</typeparam>
        /// <typeparam name="T10">日志参数 10 的类型。</typeparam>
        /// <param name="format">日志格式。</param>
        /// <param name="arg1">日志参数 1。</param>
        /// <param name="arg2">日志参数 2。</param>
        /// <param name="arg3">日志参数 3。</param>
        /// <param name="arg4">日志参数 4。</param>
        /// <param name="arg5">日志参数 5。</param>
        /// <param name="arg6">日志参数 6。</param>
        /// <param name="arg7">日志参数 7。</param>
        /// <param name="arg8">日志参数 8。</param>
        /// <param name="arg9">日志参数 9。</param>
        /// <param name="arg10">日志参数 10。</param>
        public static void Fatal<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)
        {
            if (s_LogHelper == null)
            {
                return;
            }

            s_LogHelper.Log(FrameworkLogLevel.Fatal, string.Format(format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10));
        }
    }
}
