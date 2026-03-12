using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace FFmpeg
{
    public class FFmpegWrapper : MonoBehaviour
    {
#if UNITY_IOS && !UNITY_EDITOR

        [System.Security.SuppressUnmanagedCodeSecurity()]
        //void* execute(char** argv, int argc, void (* callback)(const char*))
        [DllImport("__Internal")]
        static extern void execute(string[] argv, int argc, IOSCallback callback);

        delegate void IOSCallback(string msg);
		[AOT.MonoPInvokeCallback(typeof(IOSCallback))]
		static void IOSCallbacFunc(string message)
		{
			callbackMSGs.Enqueue(message);
		}

#elif UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaObject pluginClass;
#elif UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_EDITOR
        static void StandaloneCallback(string message)
        {
            callbackMSGs.Enqueue(message);
        }
#endif
        static Queue<string> callbackMSGs = new Queue<string>();

        //------------------------------

        void Start()
        {
#if UNITY_IOS && !UNITY_EDITOR
			//IOS implementation doesn't need initialization
#elif UNITY_ANDROID && !UNITY_EDITOR
            using (AndroidJavaClass configClass = new AndroidJavaClass("com.arthenica.ffmpegkit.FFmpegKitConfig"))
            {
                AndroidJavaObject paramVal = new AndroidJavaClass("com.arthenica.ffmpegkit.Signal").GetStatic<AndroidJavaObject>("SIGXCPU");
                configClass.CallStatic("ignoreSignal", new object[] { paramVal });
            }
            pluginClass = new AndroidJavaClass("com.arthenica.ffmpegkit.FFmpegKit");
#elif UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_EDITOR
            StandaloneProxy.Begin(StandaloneCallback);
#else
            Debug.LogWarning("FFmpeg is not implemented for " + Application.platform);
#endif
		}

        internal void Execute(string[] cmd)
        {
#if UNITY_IOS && !UNITY_EDITOR
            execute(cmd, cmd.Length, IOSCallbacFunc);
#elif UNITY_ANDROID && !UNITY_EDITOR
            callbackMSGs.Enqueue(FFmpegParser.COMMAND_CODE + FFmpegParser.START_CODE + $" Started.");
            AndroidJavaObject session = pluginClass.CallStatic<AndroidJavaObject>("execute", new object[] { string.Join(" ", cmd) });
            AndroidJavaObject returnCode = session.Call<AndroidJavaObject>("getReturnCode", new object[] { });
            int rc = returnCode.Call<int>("getValue", new object[] { });
            Debug.LogWarning($"FFMpeg FINISH with RETURN CODE:{rc}");
            callbackMSGs.Enqueue(FFmpegParser.COMMAND_CODE + FFmpegParser.FINISH_CODE + $" RETURN CODE:{rc}");
#elif UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_EDITOR
            StandaloneProxy.Execute(string.Join(" ", cmd));
#else
            Debug.LogWarning("FFmpeg is not implemented for " + Application.platform);
#endif
        }

        void Update()
        {
            if (callbackMSGs.Count > 0)
            {
                FFmpegParser.Handle(callbackMSGs.Dequeue());
            }
        }
    }
}