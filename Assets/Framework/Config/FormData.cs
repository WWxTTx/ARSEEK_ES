/***
 *           1： 系统常量
 *           2： 全局性方法。
 *           3： 系统枚举类型
 *           4： 委托定义 
 *   
 */

using UnityEngine;

namespace UnityFramework.Runtime
{
    public static class FormData
    {

        /// <summary>
        /// 框架内UI 面板预制体路径
        /// </summary>
        public static string SystemCanvasPath
        {
            get
            {
#if UNITY_STANDALONE
                return "Prefabs/PC/FrameCanvas";
#elif UNITY_ANDROID || UNITY_IOS
                return "Prefabs/Android/FrameCanvas";
#endif
                /*|| UNITY_IOS */
            }
        }
        /// <summary>
        /// 框架内UI 面板预制体路径
        /// </summary>
        public static string SystemUIPrefabsPath
        {
            get
            {
#if UNITY_STANDALONE 
                return "Prefabs/PC/UI/Panels/";
#elif UNITY_ANDROID || UNITY_IOS
                return "Prefabs/Android/UI/Panels/";
#endif
                /*|| UNITY_IOS */
            }
        }
        /// <summary>
        /// 框架内UI 模块预制体路径
        /// </summary>
        public static string ModulePrefabsPath
        {
            get
            {
#if UNITY_STANDALONE
                return "Prefabs/PC/UI/Modules/";
#elif UNITY_ANDROID || UNITY_IOS
                return "Prefabs/Android/UI/Modules/";
#endif
                /*|| UNITY_IOS */
            }
        }

        /// <summary>
        /// 框架内UI   资源下载模块
        /// </summary>
        public static string ResourcesFullScreenModulePath = ModulePrefabsPath + "Resources/FullScreen/";
        public static string ResourcesPopupModulePath = ModulePrefabsPath + "Resources/Popup/";
    }
}