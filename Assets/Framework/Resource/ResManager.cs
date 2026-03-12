using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using static UnityFramework.Runtime.RequestData;

namespace UnityFramework.Runtime
{
    /// <summary>
    /// 资源管理类
    /// </summary>
    public class ResManager : Singleton<ResManager>
    {
        public string resourcesRootPath => Application.persistentDataPath;
        public string resourcesCacheRootPath => $"{Application.persistentDataPath}/Cache";
        public string abPath => $"{resourcesCacheRootPath}/AssetBundle";
        public string imageTargetPath => $"{resourcesCacheRootPath}/ImageTargets";
        public string imagePath => $"{resourcesCacheRootPath}/Image";
        public string knowledgePointPath => $"{resourcesCacheRootPath}/Knowledgepoint";
        public string courseExercisePath => $"{resourcesCacheRootPath}/Exercise";
        public string examRecordingPath => $"{resourcesCacheRootPath}/Record";

        //public string abDownLoadPath = "https://ind.oss.arseek.cn/";
        //public string OSSDownLoadPath = "https://web3d-arseek.oss-cn-chengdu.aliyuncs.com";

        //public string OSSDownLoadPath = "https://oss.arseek.cn/";
        public string OSSDownLoadPath => $"{ApiData.STSObject}?objectName=";

        private string defaultCoverImage = "default_arimreform_course_icon";

        /// <summary>
        /// 检查百科有没有更新 这个是封装过的 可直接调用
        /// </summary>
        /// <param name="encyclopediaList">百科列表</param>
        /// <param name="callBack"></param>
        public void CheckUpdate(List<CourseABPackage> abList, UnityAction<int> callBack)
        {
            if (abList == null || abList.Count == 0)
            {
                Log.Warning($"课程没有百科资源！");
                callBack.Invoke(0);
                return;
            }

            int state = 0;
            int needWait = 0;

            foreach (CourseABPackage ab in abList)
            {
                state = Mathf.Max(state, CheckState(ab.encyclopediaId.ToString(), ab.filePath, DtataType.abs));
            }

            if (needWait == 0)
            {
                callBack.Invoke(state);
            }
        }

        /// <summary>
        /// 检查资源状态
        /// </summary>
        /// <param name="saveName">保存名称，不含后缀</param>
        /// <param name="downLoadPath">资源下载路径</param>
        /// <param name="dtataType">资源类型</param>
        /// <returns>0已下载 1未下载 2需要更新 3继续下载</returns>
        public int CheckState(string saveName, string downLoadPath, DtataType dtataType)
        {
            if (string.IsNullOrEmpty(saveName)/* || string.IsNullOrEmpty(downLoadPath)*/)
            {
                Log.Warning("加载参数错误：saveName = " + saveName + "；downLoadPath = " + downLoadPath);
                return 0;
            }

            if (string.IsNullOrEmpty(downLoadPath))
            {
                Log.Warning("加载参数错误：saveName = " + saveName + "；downLoadPath = " + downLoadPath);
                return 1;
            }

            UnparsedData(saveName, downLoadPath, dtataType, out string savePath, out string name, out string rename);

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(rename) || string.IsNullOrEmpty(savePath))
                return 0;

            string version = ConfigXML.GetData(ConfigType.Cache, dtataType, name);//获取版本号
            if (!string.IsNullOrEmpty(version))//已下载
            {
                //判断是否有更新
                if (version.Equals(rename))//不更新
                    return 0;
                else//更新
                    return 2;
            }
            else//未下载或未下载完
            {
                if (File.Exists($"{savePath.Substring(0, savePath.LastIndexOf('.'))}{DownloadFileHandler.postfix}"))
                {
                    return 3;
                }
                else
                {
                    return 1;
                }
            }
        }

        /// <summary>
        /// 根据传入数据解析出加载资源所需数据
        /// </summary>
        /// <param name="saveName">保存名称，不含后缀</param>
        /// <param name="downLoadPath">资源下载路径</param>
        /// <param name="dtataType">资源类型</param>
        /// <param name="savePath">资源保存路径</param>
        /// <param name="name">资源保存名称，包含后缀</param>
        /// <param name="version">资源版本号</param>
        public void UnparsedData(string saveName, string downLoadPath, DtataType dtataType, out string savePath, out string name, out string version)
        {
            if (string.IsNullOrEmpty(saveName) || string.IsNullOrEmpty(downLoadPath))
            {
                Log.Warning("参数错误：saveName = " + saveName + "; downLoadPath = " + downLoadPath);
                savePath = name = version = "";
                return;
            }

            try
            {
                version = downLoadPath.Split('/')[downLoadPath.Split('/').Length - 1];//这里拿别名当做版本号
                string postfix = version.Substring(version.LastIndexOf('.'));
                name = saveName + postfix;

                switch (dtataType)
                {
                    case DtataType.arImages:
                        savePath = imageTargetPath + "/" + name;
                        break;
                    case DtataType.images:
                        savePath = imagePath + "/" + name;
                        break;
                    case DtataType.abs:
                        savePath = abPath + "/" + name;
                        break;
                    case DtataType.knowledgePoints:
                        savePath = knowledgePointPath + "/" + name;
                        break;
                    default:
                        savePath = "";
                        Log.Error("类型参数错误");
                        break;
                }
            }
            catch (System.Exception e)
            {
                savePath = name = version = "";
                Log.Warning($"{downLoadPath} 数据解析失败：" + e);
            }
        }

        /// <summary>
        /// 下载资源，并保存版本号
        /// </summary>
        /// <param name="saveName">资源保存名称</param>
        /// <param name="version">资源版本号</param>
        /// <param name="downLoadPath">资源下载路径</param>
        /// <param name="savePath">资源保存路径</param>
        /// <param name="dtataType">资源类型</param>
        /// <param name="isShowLoading">是否显示加载界面</param>
        /// <param name="call">回调方法，返回下载进度</param>
        /// <param name="priority">是否优先下载</param>
        public void DownLoadFile(string saveName, string version, string downLoadPath, string savePath, DtataType dtataType, bool isShowLoading = false, UnityAction<float> call = null, bool priority = false)
        {
            if (string.IsNullOrEmpty(saveName) || string.IsNullOrEmpty(version) || string.IsNullOrEmpty(downLoadPath) || string.IsNullOrEmpty(savePath))
            {
                Log.Warning("加载参数错误：saveName = " + saveName + "；version = " + version + "；downLoadPath = " + downLoadPath + "；savePath = " + savePath);
                call?.Invoke(-1);
                return;
            }

            if (isShowLoading)
                UIManager.Instance.OpenUI<LoadingPanel>(UILevel.Loading);

            if (!File.Exists(savePath))
            {
                Log.Debug("下载资源中：" + downLoadPath);
                DownLoad.Instance.DownLoadFile(downLoadPath, savePath, (f) =>
                {
                    if (f >= 0 && f < 1)//下载中
                    {
                        //Log.Debug("下载资源中" + (f * 100).ToString("00") + "%");
                        call?.Invoke(f);
                    }
                    else //成功或失败
                    {
                        if (f >= 1)//下载成功
                        {
                            Log.Debug("下载成功：" + downLoadPath);
                            string oldVersion = ConfigXML.GetData(ConfigType.Cache, dtataType, saveName);//获取版本号
                            if (string.IsNullOrEmpty(oldVersion))
                                ConfigXML.AddData(ConfigType.Cache, dtataType, saveName, version);//记录版本号
                            else
                            {
                                Log.Warning("未下载资源，但配置文件中获取到资源历史版本记录！");
                                ConfigXML.UpdateData(ConfigType.Cache, dtataType, saveName, version);//更新版本号
                            }

                            call?.Invoke(1);
                        }
                        else
                        {
                            Log.Error("下载失败：" + downLoadPath);
                            call?.Invoke(-1);
                        }
                        if (isShowLoading)
                            UIManager.Instance.CloseUI<LoadingPanel>();
                    }
                }, priority);
            }
            else
            {
                string oldVersion = ConfigXML.GetData(ConfigType.Cache, dtataType, saveName);//获取版本号
                if (string.IsNullOrEmpty(oldVersion))
                {
                    Log.Warning("已有资源，但配置文件中未获取到资源历史版本记录！" + downLoadPath);
                    ConfigXML.AddData(ConfigType.Cache, dtataType, saveName, version);//记录版本号
                    call?.Invoke(1);
                    if (isShowLoading)
                        UIManager.Instance.CloseUI<LoadingPanel>();
                }
                else
                {
                    //判断是否有更新
                    if (oldVersion.Equals(version))//不更新
                    {
                        call?.Invoke(1);
                        if (isShowLoading)
                            UIManager.Instance.CloseUI<LoadingPanel>();
                    }
                    else//更新
                    {
                        Log.Debug("更新资源中：" + downLoadPath);
                        DownLoad.Instance.DownLoadFile(downLoadPath, savePath, (f) =>
                        {
                            if (f >= 0 && f < 1)//下载中
                            {
                                //Log.Debug("下载资源中" + (f * 100).ToString("00") + "%");
                                call?.Invoke(f);
                            }
                            else//成功或失败
                            {
                                if (f >= 1)//下载成功
                                {
                                    Log.Debug("更新成功：" + downLoadPath);
                                    ConfigXML.UpdateData(ConfigType.Cache, dtataType, saveName, version);//更新版本号
                                    call?.Invoke(1);
                                }
                                else
                                {
                                    Log.Debug("更新失败：" + downLoadPath);
                                    call?.Invoke(-1);
                                }
                                if (isShowLoading)
                                    UIManager.Instance.CloseUI<LoadingPanel>();
                            }
                        }, priority);
                    }
                }
            }
        }

        /// <summary>
        /// 移动端下载apk
        /// </summary>
        /// <param name="downLoadPath">下载路径</param>
        /// <param name="call">回调函数，返回下载进度(-1:下载失败  0-1:下载中  1:下载完成)</param>
        public void DownLoadAPK(string downLoadPath, UnityAction<float> call)
        {
            string[] strs = downLoadPath.Split('/');
            string savePath = resourcesCacheRootPath + "/" + strs[strs.Length - 1];
            DownLoad.Instance.DownLoadFile(downLoadPath, savePath, call);
        }

        /// <summary>
        /// 加载封面图
        /// </summary>
        /// <param name="courseID">课程ID</param>
        /// <param name="downLoadPath">资源下载路径</param>
        /// <param name="isShowLoading">是否显示加载界面</param>
        /// <param name="call">回调函数，返回创建的图片</param>
        public void LoadCoverImage(string courseID, string downLoadPath, bool isShowLoading, UnityAction<Texture2D> call)
        {
            if (downLoadPath.Contains(defaultCoverImage))
                courseID = defaultCoverImage;

            UnparsedData(courseID, downLoadPath, DtataType.images, out string savePath, out string name, out string version);
            DownLoadFile(name, version, downLoadPath, savePath, DtataType.images, isShowLoading, (f) =>
            {
                if (f >= 1)//成功
                {
                    //用IO部分 图片会有D3D的问题 应该是mipmap错误导致的  目前直接用unity自己的加载解决
                    LoadLocalAsset.Instance.LoadTextureAsync(savePath, (index, textrue) =>
                    {
                        if (index == 2)
                        {
                            call(textrue);
                        }
                        else if (index == -1)
                        {
                            call(null);
                        }
                    });

                    //call(LoadLocalAsset.Instance.LoadTexture(savePath));
                }
                else if (f <= -1)//失败
                {
                    call(null);
                }
            });
        }
     
        /// <summary>
        /// 加载模型
        /// <param name="abName">道具名称</param>
        /// <param name="downLoadPath">资源下载路径</param>
        /// <param name="loadNavMesh">是否加载导航网格</param>
        /// <param name="isShowLoading">是否显示加载界面</param>
        /// <param name="call">回调函数，返回创建的图片</param>
        public void LoadModel(string abName, string downLoadPath, bool loadNavMesh, bool isShowLoading, UnityAction<GameObject> call)
        {
            UnparsedData(abName, downLoadPath, DtataType.abs, out string savePath, out string name, out string version);
            DownLoadFile(name, version, downLoadPath, savePath, DtataType.abs, isShowLoading, (f) =>
            {
                if (f >= 1)//成功
                {
                    LoadABPrefab(savePath, loadNavMesh, call);
                }
                else if (f <= -1)//失败
                {
                    call(null);
                }
            });
        }

        /// <summary>
        /// 加载模型 下载和加载都是异步的 带进度
        /// <param name="name">道具名称</param>
        /// <param name="downLoadPath">资源下载路径</param>
        /// <param name="isShowLoading">是否显示加载界面</param>
        /// <param name="call">回调函数，返回创建的图片</param>
        public void LoadModelAsync(string abName, string downLoadPath, bool loadNavMesh, bool isShowLoading, UnityAction<GameObject> callBack, UnityAction<float> progress = null)
        {
            if (isShowLoading)
            {
                UIManager.Instance.OpenUI<LoadingPanel>(UILevel.Loading);
                callBack += item => UIManager.Instance.CloseUI<LoadingPanel>();
            }

            UnparsedData(abName, downLoadPath, DtataType.abs, out string savePath, out string name, out string version);
            DownLoadFile(name, version, downLoadPath, savePath, DtataType.abs, false, value =>
            {
                if (value >= 1)//成功
                {
                    LoadLocalAsset.Instance.LoadABAllAsync<GameObject>(savePath, (loadProgress, items) =>
                    {
                        if (loadProgress >= 1)//成功
                        {
                            if (items != null && items.Length > 0)
                            {
                                if (loadNavMesh)
                                {
                                    NavMeshData[] meshData = LoadLocalAsset.Instance.LoadABAll<NavMeshData>(savePath);
                                    if (meshData != null && meshData.Length > 0)
                                    {
                                        NavMesh.AddNavMeshData(meshData[0]);
                                    }
                                }
                                callBack(items[0]);
                            }
                            else
                                callBack(null);

                            LoadLocalAsset.Instance.UnloadAB(savePath, false);
                        }
                        else if (loadProgress <= -1)//失败
                        {
                            callBack(null);
                        }
                        else
                        {
                            progress?.Invoke(0.75f + (loadProgress * 0.25f));
                        }
                    });
                }
                else if (value <= -1)//失败
                {
                    callBack(null);
                }
                else
                {
                    progress?.Invoke(value * 0.75f);
                }
            });
        }

        /// <summary>
        /// 加载模型
        /// <param name="abName">考核快照百科ID</param>
        /// <param name="downLoadPath">资源下载路径</param>
        /// <param name="isShowLoading">是否显示加载界面</param>
        /// <param name="callBack">回调函数</param>
        public void LoadSnapshotModelAsync(string abName, string downLoadPath, bool loadNavMesh, bool isShowLoading, UnityAction<GameObject> callBack, UnityAction<float> progress = null)
        {
            if (isShowLoading)
            {
                UIManager.Instance.OpenUI<LoadingPanel>(UILevel.Loading);
                callBack += item => UIManager.Instance.CloseUI<LoadingPanel>();
            }

            UnparsedData(abName, downLoadPath, DtataType.abs, out string savePath, out string name, out string version);

            string originFile = ConfigXML.GetKey(ConfigType.Cache, DtataType.abs, version);
            string originSavePath = string.Empty;
            if (!string.IsNullOrEmpty(originFile))
            {
                originSavePath = savePath.Replace(abName, Path.GetFileNameWithoutExtension(originFile));
            }
            //已下载相同别名的文件
            if (!string.IsNullOrEmpty(originSavePath) && File.Exists(originSavePath))
            {
                LoadLocalAsset.Instance.LoadABAllAsync<GameObject>(originSavePath, (loadProgress, items) =>
                {
                    if (loadProgress >= 1)//成功
                    {
                        if (items != null && items.Length > 0)
                        {
                            if (loadNavMesh)
                            {
                                NavMeshData[] meshData = LoadLocalAsset.Instance.LoadABAll<NavMeshData>(originSavePath);
                                if (meshData != null && meshData.Length > 0)
                                {
                                    NavMesh.AddNavMeshData(meshData[0]);
                                }
                            }
                            callBack(items[0]);
                        }
                        else
                            callBack(null);

                        LoadLocalAsset.Instance.UnloadAB(originSavePath, false);
                    }
                    else if (loadProgress <= -1)//失败
                    {
                        callBack(null);
                    }
                    else
                    {
                        progress?.Invoke(0.75f + (loadProgress * 0.25f));
                    }
                });
            }
            else
            {
                //考核资源预下载失败，重新加载
                LoadModelAsync(abName, downLoadPath, loadNavMesh, isShowLoading, callBack, progress);
            }
        }

        /// <summary>
        /// 加载ab包中默认预制体，回调返回null，表示加载失败
        /// </summary>
        /// <param name="path"></param>
        /// <param name="loadNavMesh">是否加载导航网格</param>
        /// <param name="call"></param>
        public void LoadABPrefab(string path, bool loadNavMesh, UnityAction<GameObject> call)
        {
            GameObject[] list = LoadLocalAsset.Instance.LoadABAll<GameObject>(path);
            if (list != null && list.Length > 0)
            {
                if (loadNavMesh)
                {
                    NavMeshData[] meshData = LoadLocalAsset.Instance.LoadABAll<NavMeshData>(path);
                    if (meshData != null && meshData.Length > 0)
                    {
                        NavMesh.AddNavMeshData(meshData[0]);
                    }
                }
                call(list[0]);
            }
            else
                call(null);

            LoadLocalAsset.Instance.UnloadAB(path, false);
        }

        /// <summary>
        /// 加载知识点图片
        /// </summary>
        /// <param name="id">知识点id</param>
        /// <param name="url">文件路径</param>
        /// <param name="call"></param>
        public void LoadKnowledgepointImage(string id, string url, UnityAction<Texture2D> call)
        {
            UnparsedData(id, url, DtataType.knowledgePoints, out string savePath, out string name, out string version);
            DownLoadFile(name, version, url, savePath, DtataType.knowledgePoints, false, (f) =>
            {
                if (f >= 1)
                {
                    LoadLocalAsset.Instance.LoadTextureAsync(savePath, (index, textrue) =>
                    {
                        if (index == 2)
                        {
                            call(textrue);
                        }
                        else if (index == -1)
                        {
                            call(null);
                        }
                    });
                }
                else if (f <= -1)
                {
                    call(null);
                }
            });
        }

        /// <summary>
        /// 加载习题图片
        /// </summary>
        /// <param name="downLoadPath">下载地址</param>
        /// <param name="call">回调</param>
        public void LoadExerciseImage(string downLoadPath, UnityAction<Texture2D> call)
        {
            string savePath = courseExercisePath + "/image/" + downLoadPath.Substring(downLoadPath.LastIndexOf("/") + 1);
            if (!File.Exists(savePath))
            {
                DownLoad.Instance.DownLoadFile(OSSDownLoadPath + downLoadPath, savePath, (f) =>
                {
                    if (f >= 1)
                        call(LoadLocalAsset.Instance.LoadTexture(savePath));
                });
            }
            else call(LoadLocalAsset.Instance.LoadTexture(savePath));
        }

        public void StopAllDownLoad() { DownLoad.Instance.StopAllDownLoad(); }
    }
}