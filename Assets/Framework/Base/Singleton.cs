using UnityEngine;

namespace UnityFramework.Runtime
{
    /// <summary>
    /// 单例基类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        private static T instance = null;

        private static bool isAppQuit;

        public static T Instance
        {
            get
            {
                if (isAppQuit)
                    return null;

                if (instance == null)
                {
                    instance = FindObjectOfType<T>();
                    if (instance == null)
                    {
                        var gameObject = new GameObject();
                        DontDestroyOnLoad(gameObject);
                        gameObject.AddComponent<T>();
                        Log.Debug("实例化单例对象-" + gameObject.name);
                    }
                }
                return instance;
            }
        }

        protected void Awake()
        {
            //确保唯一性
            if (instance == null)
            {
                gameObject.name = typeof(T).Name;
                instance = GetComponent<T>();
                InstanceAwake();
                Log.Debug("初始化单例对象-" + gameObject.name);
            }
            else
            {
                Log.Warning("销毁多余单例对象-" + gameObject.name);
                Destroy(gameObject);
            }
        }


        protected void OnApplicationQuit()
        {
            isAppQuit = true;
            if (instance != null)
            {
                instance.InstanceDestroy();
                instance = null;
                Destroy(gameObject);
            }
        }
        /// <summary>
        /// Awake中自动调用
        /// </summary>
        protected virtual void InstanceAwake() { }
        /// <summary>
        /// app退出时自动调用
        /// </summary>
        protected virtual void InstanceDestroy() { }

    }
}