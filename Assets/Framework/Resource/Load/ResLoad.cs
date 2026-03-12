using UnityEngine;
using System.Collections;

namespace UnityFramework.Runtime
{
    /// <summary>
    /// 在Unity的Resources.Load的基础之上，增加了“缓存”的处理，主要用于Resources下的预制体
    /// </summary>
    public class ResLoad : Singleton<ResLoad>
    {
        /// <summary>
        /// 缓存的已加载资源
        /// </summary>
        private Hashtable ht = new Hashtable();

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源路径</param>
        /// <param name="isCache">是否缓存资源</param>
        /// <returns></returns>
        public T Load<T>(string path, bool isCache = true) where T : Object
        {
            Log.Debug("资源路径:" + path);
            //已缓存
            if (ht.Contains(path))
                return ht[path] as T;

            T TResource = Resources.Load<T>(path);
            if (TResource == null)
                Log.Error($"资源找不到，请检查! {path}");
            else if (isCache)
                ht.Add(path, TResource);

            return TResource;
        }

        /// <summary>
        /// 加载对象资源，并实例化
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="isCatch">是否缓存资源</param>
        /// <returns></returns>
        public GameObject LoadAndInst(string path, Transform parent = null, bool isCatch = true)
        {
            GameObject goObj = Load<GameObject>(path, isCatch);
            if (goObj == null) return null;
            
            GameObject goObjClone = null;
            if (parent)
                goObjClone = Instantiate(goObj, parent);
            else
                goObjClone = Instantiate(goObj);

            if (goObjClone == null)
                Log.Error($"资源实例化不成功，请检查! {path}");

            return goObjClone;
        }
    }
}