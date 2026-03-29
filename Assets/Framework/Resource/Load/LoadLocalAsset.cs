using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace UnityFramework.Runtime
{
    /// <summary>
    /// 资源加载
    /// </summary>
    public class LoadLocalAsset : Singleton<LoadLocalAsset>
    {
        /// <summary>
        /// 记录正在使用的ab资源，防止回调中再次加载同一ab包
        /// </summary>
        private Dictionary<string, AssetBundle> abs = new Dictionary<string, AssetBundle>();
        /// <summary>
        /// 记录正在加载中的ab包路径，防止同一时间重复加载
        /// </summary>
        private HashSet<string> loadingPaths = new HashSet<string>();
        /// <summary>
        /// 记录正在使用的图片资源，防止再次加载
        /// </summary>
        private Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

        /// <summary>
        /// 加载AB包
        /// </summary>
        /// <param name="path">ab包磁盘路径</param>
        /// <returns>加载ab包</returns>
        public AssetBundle LoadAB(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Log.Error("ab包加载路径为null");
                return null;
            }

            if (abs.ContainsKey(path) && abs[path] != null)
            {
                Log.Warning("ab包已加载:" + path);
                return abs[path];
            }

            AssetBundle bundle = AssetBundle.LoadFromFile(path);
            if (bundle == null)
            {
                Log.Error("加载ab包失败:" + path);
                if (abs.ContainsKey(path))
                    abs.Remove(path);

                return null;
            }

            if (abs.ContainsKey(path))
                abs[path] = bundle;
            else
                abs.Add(path, bundle);

            return bundle;
        }
        /// <summary>
        /// 加载AB包中的资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">ab包磁盘路径</param>
        /// <param name="resName">资源名称(带有文件扩展名的相对路径)</param>
        /// <returns>加载资源</returns>
        public T LoadAB<T>(string path, string resName) where T : Object
        {
            Log.Debug("加载路径为{0}的ab包中名称为{1}的{2}类型资源!", path, resName, typeof(T).ToString());
            if (string.IsNullOrEmpty(resName))
            {
                Log.Error($"资源路径或名称错误：path:{path}; resName:{resName}");
                return null;
            }

            AssetBundle bundle = LoadAB(path);
            if (bundle == null)
                return null;

            T asset = bundle.LoadAsset<T>(resName);
            if (asset == null)
            {
                Log.Error("路径为{0}的ab包中未找到{1}名称的{2}类型资源！", path, resName, typeof(T).ToString());
                return null;
            }

            return asset;
        }
        /// <summary>
        /// 加载AB包中所有T类型的资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">ab包磁盘路径</param>
        /// <returns>加载T类型资源集合</returns>
        public T[] LoadABAll<T>(string path) where T : Object
        {
            Log.Debug("加载路径为{0}的ab包中的所有{1}类型资源", path, typeof(T).ToString());

            AssetBundle bundle = LoadAB(path);
            if (bundle == null)
                return null;

            T[] assets = bundle.LoadAllAssets<T>();
            if (assets == null || assets.Length == 0)
            {
                Log.Error("路径为{0}的ab包中未找到{1}类型资源", path, typeof(T).ToString());
                return null;
            }

            return assets;
        }
        /// <summary>
        /// 加载AB包
        /// </summary>
        /// <param name="path">ab包磁盘路径</param>
        /// <param name="callback">资源加载回调,T0:加载进度(0-1)--加载失败(-1),T1:加载ab包</param>
        /// <returns></returns>
        private IEnumerator ILoadABAsync(string path, UnityAction<float, AssetBundle> callback)
        {
            if (abs.ContainsKey(path) && abs[path] != null)
            {
                Log.Warning("ab包已加载:" + path);
                callback?.Invoke(1, abs[path]);
                yield break;
            }

            // 检查是否正在加载中，防止重复加载
            if (loadingPaths.Contains(path))
            {
                Log.Warning("ab包正在加载中，跳过重复请求:" + path);
                callback?.Invoke(-1f, null);
                yield break;
            }

            loadingPaths.Add(path);

            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(path);
            while (!request.isDone)
            {
                yield return new WaitForSeconds(0.5f);
                callback?.Invoke(request.progress, null);
            }

            loadingPaths.Remove(path);

            if (request.isDone)
            {
                AssetBundle bundle = request.assetBundle;
                if (bundle == null)
                {
                    Log.Error("加载ab包失败:" + path);
                    if (abs.ContainsKey(path))
                        abs.Remove(path);

                    callback?.Invoke(-1f, null);
                }
                else
                {
                    if (abs.ContainsKey(path))
                        abs[path] = bundle;
                    else
                        abs.Add(path, bundle);

                    callback?.Invoke(1f, bundle);
                }
            }
        }
        /// <summary>
        /// 加载AB包
        /// </summary>
        /// <param name="path">ab包磁盘路径</param>
        /// <param name="callback">资源加载回调,T0:加载进度(0-1)--加载失败(-1),T1:加载ab包</param>
        public void LoadABAsync(string path, UnityAction<float, AssetBundle> callback)
        {
            if (string.IsNullOrEmpty(path))
            {
                Log.Error("异步加载ab包路径为空！");
                callback?.Invoke(-1, null);
            }
            else
                StartCoroutine(ILoadABAsync(path, callback));
        }
        /// <summary>
        /// 异步加载AB包中的资源协程
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">ab包磁盘路径</param>
        /// <param name="resName">资源名称(带有文件扩展名的相对路径)</param>
        /// <param name="callback">资源加载回调,T0:加载进度(0-1)--加载失败(-1),T1:已加载资源</param>
        /// <returns></returns>
        private IEnumerator ILoadABAsync<T>(string path, string resName, UnityAction<float, T> callback) where T : Object
        {
            AssetBundle bundle = null;
            yield return StartCoroutine(ILoadABAsync(path, (p, b) =>
            {
                if (p > 0 && p < 1)
                    callback?.Invoke(p * 0.5f, null);
                else if (p < 0)
                {
                    callback?.Invoke(-1, null);
                }
                else if (p >= 1)
                {
                    bundle = b;
                }
            }));

            AssetBundleRequest request = bundle.LoadAssetAsync<T>(resName);
            while (!request.isDone)
            {
                yield return new WaitForSeconds(0.5f);
                callback?.Invoke((request.progress + 1) * 0.5f, null);
            }

            if (request.isDone)
            {
                Object asset = request.asset;
                if (asset == null)
                {
                    Log.Error("路径为{0}的ab包中未找到{1}名称的{2}类型资源！", path, resName, typeof(T).ToString());
                    callback?.Invoke(-1f, null);
                }
                else
                    callback?.Invoke(1f, asset as T);
            }
        }
        /// <summary>
        /// 异步加载AB包中的资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">ab包磁盘路径</param>
        /// <param name="resName">资源名称(带有文件扩展名的相对路径)</param>
        /// <param name="callback">资源加载回调,T0:加载进度(0-1)--加载失败(-1),T1:已加载资源</param>
        public void LoadABAsync<T>(string path, string resName, UnityAction<float, T> callback) where T : Object
        {
            Log.Debug("异步加载路径为{0}的ab包中名称为{1}的{2}类型资源", path, resName, typeof(T).ToString());
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(resName))
            {
                Log.Error("ab包加载路径或加载资源名称为空！");
                callback?.Invoke(-1, null);
            }
            else
                StartCoroutine(ILoadABAsync<T>(path, resName, callback));
        }
        /// <summary>
        /// 异步加载AB包中所有T类型资源协程
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">ab包磁盘路径</param>
        /// <param name="callback">资源加载回调,T0:加载进度(0-1)--加载失败(-1),T1:加载T类型资源集合</param>
        /// <returns></returns>
        private IEnumerator ILoadABAllAsync<T>(string path, UnityAction<float, T[]> callback) where T : Object
        {
            AssetBundle bundle = null;
            yield return StartCoroutine(ILoadABAsync(path, (p, b) =>
            {
                if (p > 0 && p < 1)
                    callback?.Invoke(p * 0.5f, null);
                else if (p < 0)
                {
                    callback?.Invoke(-1, null);
                }
                else if (p >= 1)
                {
                    bundle = b;
                }
            }));

            AssetBundleRequest request = null;
            try
            {
                request = bundle.LoadAllAssetsAsync<T>();
            }
            catch
            {
                Debug.Log("加载失败，可能是重复调用");
            }
            
            if (request != null)
            {
                while (!request.isDone)
                {
                    yield return null;
                    callback?.Invoke(Mathf.Min((request.progress + 1) * 0.5f, 0.99f), null);
                }

                var allAssets = request.allAssets;
                {
                    if (allAssets == null || allAssets.Length == 0)
                    {
                        Log.Error("路径为{0}的ab包中未找到{1}类型资源", path, typeof(T).ToString());
                        callback?.Invoke(-1f, null);
                    }
                    else
                    {
                        T[] assets = new T[allAssets.Length];
                        for (int i = 0; i < allAssets.Length; i++)
                        {
                            assets[i] = allAssets[i] as T;
                        }
                        callback?.Invoke(1f, assets);
                    }
                }
            }
        }
        /// <summary>
        /// 异步加载AB包中所有T类型资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">ab包磁盘路径</param>
        /// <param name="callback">资源加载回调,T0:加载进度(0-1)--加载失败(-1),T1:加载T类型资源集合</param>
        public void LoadABAllAsync<T>(string path, UnityAction<float, T[]> callback) where T : Object
        {
            Log.Debug("异步加载路径为{0}的ab包中的所有{1}类型资源", path, typeof(T).ToString());

            if (string.IsNullOrEmpty(path))
            {
                Log.Error("异步加载ab包路径为空！");
                callback?.Invoke(-1, null);
            }
            else
                StartCoroutine(ILoadABAllAsync<T>(path, callback));
        }
        /// <summary>
        /// 卸载ab包
        /// </summary>
        /// <param name="path">ab包路径</param>
        /// <param name="unloadAllLoadedObjects">是否卸载ab包所有实例</param>
        public void UnloadAB(string path, bool unloadAllLoadedObjects)
        {
            if (string.IsNullOrEmpty(path))
            {
                Log.Warning("ab包路径为null");
                return;
            }

            if (!abs.ContainsKey(path))
            {
                Log.Warning("ab包未加载:" + path);
                return;
            }

            if (unloadAllLoadedObjects)
            {
                abs[path].Unload(true);
                abs.Remove(path);
                Resources.UnloadUnusedAssets();
                System.GC.Collect();
            }
            else
                abs[path].Unload(false);
        }
        /// <summary>
        /// 卸载所有ab包
        /// </summary>
        /// <param name="unloadAllLoadedObjects">是否卸载ab包所有实例，默认为true</param>
        public void UnloadABAll(bool unloadAllLoadedObjects = true)
        {
            foreach (var item in abs)
            {
                if (item.Value != null)
                {
                    if (unloadAllLoadedObjects)
                        item.Value.Unload(true);
                    else
                        item.Value.Unload(false);
                }
                else
                    Log.Warning("ab包未加载:" + item.Key);
            }

            if (unloadAllLoadedObjects)
            {
                abs.Clear();
                Resources.UnloadUnusedAssets();
                System.GC.Collect();
            }
        }

        /// <summary>
        /// 加载图片，回调返回null，表示加载失败
        /// </summary>
        public Texture2D LoadTexture(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Log.Error("图片加载路径为null");
                return null;
            }

            var bytes = FileTool.FileRead(path);
            if (bytes != null)
            {
                Texture2D texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);

                texture.LoadImage(bytes);
                texture.Compress(true);
                return texture;
            }
            else
            {
                Log.Error("图片加载失败:" + path);
                return null;
            }
        }
        public void LoadTextureAsync(string path, UnityAction<float, Texture2D> call)
        {
            Log.Debug("加载路径为{0}的图片", path);
            if (string.IsNullOrEmpty(path))
            {
                Log.Error("图片加载路径为null");
                call?.Invoke(-1, null);
            }
            else
                StartCoroutine(ILoadTexture(path, call));
        }
        /// <summary>
        /// 加载图片，回调返回null，表示加载失败
        /// </summary>
        private IEnumerator ILoadTexture(string path, UnityAction<float, Texture2D> call)
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                case RuntimePlatform.IPhonePlayer:
                    path = "file://" + path;
                    break;
            }

            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(path))
            {
                request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.ConnectionError)
                {
                    Log.Error($"路径为{path}的图片加载出错：{request.error}");
                    call?.Invoke(-1, null);
                    yield break;
                }
                while (!request.isDone)
                {
                    yield return new WaitForSeconds(0.5f);
                    call?.Invoke(request.downloadProgress, null);
                }

                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                if (texture)
                {
                    texture.Compress(true);
                    call?.Invoke(2, texture);
                }
                else
                {
                    Log.Error("路径为{0}的图片加载失败！", path);
                    call?.Invoke(-1, null);
                }
            }
        }
        /// <summary>
        /// 加载图片，回调返回null，表示加载失败
        /// </summary>
        public Texture2D LoadTextureCache(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Log.Error("图片加载路径为null");
                return null;
            }

            if (textures.ContainsKey(path) && textures[path] != null)
            {
                Log.Warning("图片已加载:" + path);
                return textures[path];
            }

            var bytes = FileTool.FileRead(path);
            if (bytes != null)
            {
                Texture2D texture = new Texture2D(Screen.width, Screen.height);
                texture.LoadImage(bytes);
                texture.Compress(true);

                if (textures.ContainsKey(path))
                    textures[path] = texture;
                else
                    textures.Add(path, texture);

                return texture;
            }
            else
            {
                Log.Error("图片加载失败:" + path);
                if (textures.ContainsKey(path))
                    textures.Remove(path);

                return null;
            }
        }
        public void LoadTextureAsyncCache(string path, UnityAction<float, Texture2D> call)
        {
            Log.Debug("加载路径为{0}的图片", path);
            if (string.IsNullOrEmpty(path))
            {
                Log.Error("图片加载路径为null");
                call?.Invoke(-1, null);
            }
            else
            {
                if (textures.ContainsKey(path) && textures[path] != null)
                {
                    Log.Warning("图片已加载:" + path);
                    call?.Invoke(1, textures[path]);
                }
                else
                    StartCoroutine(ILoadTextureCache(path, call));
            }
        }
        /// <summary>
        /// 加载图片，回调返回null，表示加载失败
        /// </summary>
        private IEnumerator ILoadTextureCache(string path, UnityAction<float, Texture2D> call)
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                case RuntimePlatform.IPhonePlayer:
                    path = "file://" + path;
                    break;
            }

            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(path))
            {
                request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.ConnectionError)
                {
                    Log.Error("图片加载出错：" + request.error);
                    if (textures.ContainsKey(path))
                        textures.Remove(path);

                    call?.Invoke(-1, null);
                    yield break;
                }
                while (!request.isDone)
                {
                    yield return new WaitForSeconds(0.5f);
                    call?.Invoke(request.downloadProgress, null);
                }
                if (request.isDone)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    if (texture)
                    {
                        if (textures.ContainsKey(path))
                            textures[path] = texture;
                        else
                            textures.Add(path, texture);

                        call?.Invoke(1, texture);
                    }
                    else
                    {
                        Log.Error("路径为{0}的图片加载失败！", path);
                        if (textures.ContainsKey(path))
                            textures.Remove(path);

                        call?.Invoke(-1, null);
                    }
                }
            }
        }

        /// <summary>
        /// 卸载图片缓存
        /// </summary>
        /// <param name="path">图片本地路径</param>
        public void UnloadTexture(string path)
        {
            textures.Remove(path);
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        /// <summary>
        /// 卸载所有已加载图片
        /// </summary>
        public void UnloadTextureAll()
        {
            textures.Clear();
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        /// <summary>
        ///  加载音频
        /// </summary>
        /// <param name="path"></param>
        /// <param name="call"></param>
        public void LoadAudio(string path, UnityAction<AudioClip> call, AudioType audioType = AudioType.WAV)
        {
            StartCoroutine(ILoadAudio(path, call, audioType));
        }
        /// <summary>
        /// 协程加载音频
        /// </summary>
        /// <param name="localPath">本地路径</param>
        /// <param name="callBack">回调</param>
        private IEnumerator ILoadAudio(string localPath, UnityAction<AudioClip> call, AudioType audioType)
        {
            localPath = ValidateUri(localPath);

            using (var uwr = UnityWebRequestMultimedia.GetAudioClip(localPath, audioType))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result == UnityWebRequest.Result.ConnectionError)
                    Log.Error(localPath + "没有获取到文件！\n" + uwr.error);
                else
                    call.Invoke(DownloadHandlerAudioClip.GetContent(uwr));
            }
        }
        private string ValidateUri(string rawPath)
        {
            // 1. 如果已经是完整URL（http/https），直接返回
            if (rawPath.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase) ||
                rawPath.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
            {
                // 对于网络URL，直接返回原始路径（不需要添加file://）
                return rawPath;
            }

            // 2. 处理本地文件路径（StreamingAssets）
            string streamingPath = System.IO.Path.Combine(Application.streamingAssetsPath, rawPath);

            // 3. 根据不同平台处理本地路径
#if UNITY_ANDROID && !UNITY_EDITOR
    // Android平台特殊处理
    // 注意：Android平台直接使用路径，不需要file://前缀
    return streamingPath;
#else
            // 其他平台（iOS、Windows、编辑器等）
            // 转义路径中的特殊字符
            string escapedPath = System.Uri.EscapeUriString(streamingPath).Replace("\\", "/");

            // 添加file://协议
            return "file://" + escapedPath;
#endif
        }


        /// <summary>
        /// 加载缩略图
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="callBack"></param>
        /// <returns></returns>
        public IEnumerator LoadTextures_Thumbnail(List<string> paths, UnityAction<Dictionary<string, Texture2D>> callBack)
        {
            Dictionary<string, Texture2D> texture2ds = new Dictionary<string, Texture2D>();

            int height = 0;
#if UNITY_STANDALONE_WIN
            height = 256;
#elif UNITY_ANDROID
            height = 128;
#endif

            foreach (string path in paths)
            {
                using (UnityWebRequest unityWebRequest = UnityWebRequestTexture.GetTexture($"file://{path}"))
                {
                    yield return unityWebRequest.SendWebRequest();

                    if (unityWebRequest.result == UnityWebRequest.Result.Success)
                    {
                        Texture2D oldTexture = DownloadHandlerTexture.GetContent(unityWebRequest);
                        int width = (int)((float)oldTexture.width / oldTexture.height * height);

                        Texture2D newTexture = new Texture2D(width, height, oldTexture.format, false);
                        Graphics.ConvertTexture(oldTexture, newTexture);
                        Destroy(oldTexture);

                        texture2ds.Add(path, newTexture);
                    }
                    else
                    {
                        Debug.LogError($"路径为:{path} 的图片获取失败! 原因为:{unityWebRequest.error}");
                        texture2ds.Add(path, null);
                    }
                }
            }

            callBack.Invoke(texture2ds);
        }

        protected override void InstanceDestroy()
        {
            foreach (var item in abs)
            {
                if (item.Value != null)
                {
                    item.Value.Unload(true);
                }
            }

            abs.Clear();

            textures.Clear();
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }
    }
}