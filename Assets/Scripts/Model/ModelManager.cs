using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using static UnityFramework.Runtime.ServiceRequestData;

namespace UnityFramework.Runtime
{
    public class ModelManager : Singleton<ModelManager>
    {
        public Transform modelRoot;

        [HideInInspector]
        public GameObject modelGo;

        public Vector3 modelBoundsCenter;

        /// <summary>
        /// 模型初始居中距离
        /// </summary>
        public float centerDis;
        [HideInInspector]
        public float playTime = 1.5f;

        private Transform mainCamTrans;

        public Vector3 initCamEuler { get; private set; }
        public Vector3 initCamPos { get; private set; }

        private Vector3 resetCamEuler;
        private Vector3 resetCamPos;

        private Vector3 initSceneLightPos;
        private Vector3 initSceneLightEuler;

        protected override void InstanceAwake()
        {
            mainCamTrans = Camera.main.transform;

            centerDis = Vector3.Distance(Camera.main.transform.position, modelBoundsCenter);

            initCamPos = mainCamTrans.position;
            initCamEuler = mainCamTrans.eulerAngles;
            resetCamPos = initCamPos;
            resetCamEuler = initCamEuler;

            initSceneLightPos = sceneLight.transform.position;
            initSceneLightEuler = sceneLight.transform.eulerAngles;
        }

        /// <summary>
        /// 实例化模型
        /// </summary>
        public GameObject CreateModel(GameObject prefab)
        {
            if (prefab == null)
            {
                Log.Warning("实例化对象为null！");
                return null;
            }

            //模型根节点初始化
            modelRoot.localEulerAngles = Vector3.zero;
            modelRoot.localScale = Vector3.one;

            modelGo = Instantiate(prefab, modelRoot);//实例化模型

            //模型初始化transform
            modelGo.transform.localPosition = Vector3.zero;

            return modelGo;
        }
        /// <summary>
        /// 初始化模型功能
        /// </summary>
        public void InitScripts()
        {
            ClearModelUUID();
            ModelInfo[] modelInfos = modelGo.GetComponentsInChildren<ModelInfo>();
            foreach (ModelInfo modelInfo in modelInfos)
            {
                if (modelInfo.PropType == PropType.Operate/* && modelInfo.InteractMode == InteractMode.Click*/)
                {
                    modelInfo.gameObject.AddComponent<CollisionBoxMouseEvent>();
                    AddModelUUID(modelInfo);
                }
            }
            AdaptModelRestrict(modelGo);
        }
        /// <summary>
        /// 清理模型根节点和相机上的功能脚本
        /// </summary>
        public void DestroyScripts(bool isImmediate = false)
        {
            ModelRotate rotate = modelRoot.GetComponent<ModelRotate>();
            if (rotate)
            {
                if (isImmediate)
                    DestroyImmediate(rotate);
                else
                    Destroy(rotate);
            }
            ModelZoom zoom = modelRoot.GetComponent<ModelZoom>();
            if (zoom)
            {
                if (isImmediate)
                    DestroyImmediate(zoom);
                else
                    Destroy(zoom);
            }
            ModelMove move = modelRoot.GetComponent<ModelMove>();
            if (move)
            {
                if (isImmediate)
                    DestroyImmediate(move);
                else
                    Destroy(move);
            }

            CameraRotate camRotate = mainCamTrans.GetComponent<CameraRotate>();
            if (camRotate)
            {
                if (isImmediate)
                    DestroyImmediate(camRotate);
                else
                    Destroy(camRotate);
            }
            CameraZoom camZoom = mainCamTrans.GetComponent<CameraZoom>();
            if (camZoom)
            {
                if (isImmediate)
                    DestroyImmediate(camZoom);
                else
                    Destroy(camZoom);
            }
            CameraMove camMove = mainCamTrans.GetComponent<CameraMove>();
            if (camMove)
            {
                if (isImmediate)
                    DestroyImmediate(camMove);
                else
                    Destroy(camMove);
            }

            resetCamPos = initCamPos;
            resetCamEuler = initCamEuler;
            CameraControl = true;
        }
        /// <summary>
        /// 清理节点下所有模型
        /// </summary>
        public void DestroyModels(bool isImmediate = false)
        {
            if (modelRoot == null) return;

            if (isImmediate)
            {
                while (modelRoot.childCount > 0)
                {
                    DestroyImmediate(modelRoot.GetChild(0).gameObject);
                }
            }
            else
            {
                for (int i = 0; i < modelRoot.childCount; i++)
                {
                    Destroy(modelRoot.GetChild(i).gameObject);
                }
            }
        }
        /// <summary>
        /// 控制模型是否显隐
        /// </summary>
        /// <param name="isShow">true：显示，false：隐藏</param>
        public void ControlModels(bool isShow)
        {
            if (modelRoot == null) return;

            for (int i = 0; i < modelRoot.childCount; i++)
            {
                modelRoot.GetChild(i).gameObject.SetActive(isShow);
            }
        }

        public void CalculateModelCenter(GameObject go)
        {
            if (go == null)
            {
                modelBoundsCenter = Vector3.zero;
                return;
            }
            modelBoundsCenter = go.transform.position;
        }

        /// <summary>
        /// 应用模型限制
        /// </summary>
        /// <param name="go"></param>
        public void AdaptModelRestrict(GameObject go)
        {
            if (GlobalInfo.hasRole)
                return;

            if (go == null)
                return;

            CalculateModelCenter(go);

            ModelRestrict restrict = go.GetComponent<ModelRestrict>();

            if (restrict)
            {
                //移动
                CameraMove cameraMove = mainCamTrans.AutoComponent<CameraMove>();
                cameraMove.moveType = restrict.moveType;
                if (restrict.moveType != CameraMoveType.None)
                {
                    RestrictCameraMove moveConstraint = restrict.restrictCameraMove;
                    cameraMove.SetRange(moveConstraint.moveAlongMouse, moveConstraint.maxMove_L, moveConstraint.maxMove_R, moveConstraint.maxMove_U, moveConstraint.maxMove_D);
                }

                //旋转
                CameraRotate cameraRotate = mainCamTrans.AutoComponent<CameraRotate>();
                cameraRotate.rotateType = restrict.rotateType;
                if (restrict.rotateType != CameraRotateType.None)
                {
                    RestrictCameraRotate rotConstraint = restrict.restrictCameraRotate;
                    cameraRotate.SetRange(rotConstraint.allowPitch, rotConstraint.minAngle_P, rotConstraint.maxAngle_P, rotConstraint.allowYaw, rotConstraint.minAngle_Y, rotConstraint.maxAngle_Y);
                }

                //缩放
                CameraZoom cameraZoom = mainCamTrans.AutoComponent<CameraZoom>();
                cameraZoom.zoomType = restrict.zoomType;
                if (restrict.zoomType != CameraZoomType.None)
                {
                    RestrictCameraZoom zoomConstraint = restrict.restrictCameraZoom;
                    cameraZoom.SetRange(zoomConstraint.minDistance, zoomConstraint.maxDistance);
                }
            }
            else
            {
                mainCamTrans.AutoComponent<CameraMove>().moveType = CameraMoveType.Pan;
                mainCamTrans.AutoComponent<CameraRotate>().rotateType = CameraRotateType.RotateAroundMouse;
                mainCamTrans.AutoComponent<CameraZoom>().zoomType = CameraZoomType.Mouse;
            }
        }

        /// <summary>
        /// 用于模拟操作百科
        /// </summary>
        /// <param name="moveType"></param>
        /// <param name="rotateType"></param>
        /// <param name="zoomType"></param>
        public void AdaptModelRestrict(CameraMoveType moveType = CameraMoveType.Pan, CameraRotateType rotateType = CameraRotateType.RotateAroundScreen, CameraZoomType zoomType = CameraZoomType.Mouse)
        {
            if (GlobalInfo.hasRole)
                return;

            CalculateModelCenter(null);

            CameraMove cameraMove = Camera.main.AutoComponent<CameraMove>();
            cameraMove.moveType = moveType;
            cameraMove.ResetRange();
            CameraRotate cameraRotate = Camera.main.AutoComponent<CameraRotate>();
            cameraRotate.rotateType = rotateType;
            cameraRotate.ResetRange();
            CameraZoom cameraZoom = Camera.main.AutoComponent<CameraZoom>();
            cameraZoom.zoomType = zoomType;
            cameraZoom.UpdateRange();
        }

        /// <summary>
        /// 能否操控相机
        /// </summary>
        [HideInInspector]
        public bool CameraControl = true;
        /// <summary>
        /// 相机是否正在执行dotween
        /// </summary>
        private bool cameraDotween = false;
        public bool CameraDotween
        {
            get { return cameraDotween; }
            set
            {
                cameraDotween = value;
            }
        }
        /// <summary>
        /// 重置相机机位
        /// </summary>
        /// <param name="resetRotation">是否重置旋转</param>
        /// <param name="useResetPos">是否使用resetCamPos作为重置坐标位置  首页、资源使用</param>
        /// <param name="callback"></param>
        public void ResetCameraPose(bool resetRotation = false, bool useResetPos = false, UnityAction callback = null)
        {
            if (GlobalInfo.isAR)
            {
                callback?.Invoke();
                return;
            }

            //中断正在执行的相机动画
            if (cameraDotween)
            {
                //callback?.Invoke();
                //return;
                DOTween.Kill("BehaveMoveCamera", true);
                DOTween.Kill("BehaveZoomCamera", true);
            }

            CameraControl = false;
            CameraRotate cameraRotate = mainCamTrans.GetComponent<CameraRotate>();
            if (cameraRotate && cameraRotate.rotateType != CameraRotateType.None)
            {
                cameraRotate.ResetRotate(resetCamPos, useResetPos, resetRotation ? resetCamEuler.x : -1, resetRotation ? resetCamEuler.y : -1, playTime * GlobalInfo.playTimeRatio, () =>
                {
                    cameraRotate.UpdateState();
                    callback?.Invoke();
                    CameraControl = true;
                });
            }
            else
            {
                mainCamTrans.DOMove(resetCamPos, playTime * GlobalInfo.playTimeRatio).OnComplete(() =>
                {
                    if (cameraRotate)
                        cameraRotate.UpdateState();
                    callback?.Invoke();
                    CameraControl = true;
                });
            }
        }
        /// <summary>
        /// 更新相机机位
        /// </summary>
        public void UpdateCameraPose()
        {
            if (GlobalInfo.isAR)
                return;

            if (mainCamTrans.TryGetComponent(out CameraRotate cameraRotate))
            {
                cameraRotate.UpdateState();
            }
            resetCamPos = mainCamTrans.position;
            resetCamEuler = mainCamTrans.eulerAngles;
        }
        /// <summary>
        /// 还原相机机位
        /// </summary>
        public void RevertCameraPose()
        {
            if (GlobalInfo.isAR)
                return;

            resetCamPos = initCamPos;
            resetCamEuler = initCamEuler;
            if (mainCamTrans.TryGetComponent(out CameraRotate cameraRotate))
            {
                cameraRotate.RevertState();
            }
        }
        private Color outlineColor = new Color(0.9686275f, 0.3176471f, 0.2588235f);
        public HighlightPlus.HighlightEffect ControlHighlightEffect(Component component, bool isShow, Color? color = null)
        {
            if (component == null)
                return null;

            if (isShow)
            {
                HighlightPlus.HighlightEffect highlighter = component.AutoComponent<HighlightPlus.HighlightEffect>();
                {
                    highlighter.highlighted = true;
                    highlighter.outlineColor = color ?? outlineColor;
                    highlighter.glow = 0;
                    highlighter.overlay = 0;
                    highlighter.seeThrough = HighlightPlus.SeeThroughMode.Never;
                    highlighter.GPUInstancing = false;

                    //透视高亮
                    highlighter.outlineVisibility = HighlightPlus.Visibility.AlwaysOnTop;

                    highlighter.enabled = false;
                    highlighter.enabled = true;

                    return highlighter;
                }
            }
            else
            {
                if (component.TryGetComponent(out HighlightPlus.HighlightEffect highlightEffect))
                {
                    DestroyImmediate(highlightEffect);
                }

                return null;
            }
        }

        #region 后处理相关
        /// <summary>
        /// 控制质量级别
        /// </summary>
        /// <param name="index">0:Very Low; 1:Low; 2:Medium; 3:High; 4:Very High; 5:Ultra</param>
        public void ControlQualityLevel(int index)
        {
            QualitySettings.SetQualityLevel(index, true);
        }
        /// <summary>
        /// 后处理控制
        /// </summary>
        public UnityEngine.Rendering.Volume processVolume
        {
            get
            {
                if (_processVolume == null)
                    _processVolume = ComponentExtend.GetComponentByChildName<UnityEngine.Rendering.Volume>(GameObject.Find("StartSetupBase").transform, "Global Volume");

                return _processVolume;
            }
        }
        private UnityEngine.Rendering.Volume _processVolume;
        public List<UnityEngine.Rendering.VolumeProfile> profiles;
        /// <summary>
        /// 控制后处理
        /// </summary>
        /// <param name="isOn">是否开启 默认关闭</param>
        /// <param name="index">启用第几个配置 默认第1个</param>
        public void ControlGlobalVolume(bool isOn = false, int index = 0)
        {
            if (!isOn || !Option_GeneralModule.volume)
            {
                processVolume.enabled = false;
                return;
            }

            if (profiles == null)
            {
                throw new System.Exception("未配置后处理!!");
            }

            if (profiles.Count < index + 1)
            {
                throw new System.Exception("后处理列表索引超界!!");
            }

            processVolume.profile = profiles[index];
            processVolume.enabled = true;
        }
        /// <summary>
        /// 控制相机后处理
        /// </summary>
        /// <param name="isOn">是否开启</param>
        public void ControlPostProcessing(bool isOn)
        {
            Camera.main.gameObject.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing = isOn/* && Setting_GeneralModule.volume*/;
        }
        #endregion

        #region 灯光相关
        public Light sceneLight;
        /// <summary>
        /// 控制灯光
        /// </summary>
        /// <param name="isOn"></param>
        /// <param name="lightShadows"></param>
        public void ControlSceneLight(bool isOn, LightShadows lightShadows = LightShadows.None)
        {
            sceneLight.enabled = isOn;
            if (isOn)
                sceneLight.shadows = lightShadows;
        }
        /// <summary>
        /// 设置忽略光照层
        /// </summary>
        /// <param name="go"></param>
        public void SetLightLayer(GameObject go)
        {
            int layer = LayerMask.NameToLayer("Ignore Light");

            SaveBaking[] saveBakings = go.GetComponentsInChildren<SaveBaking>(true);
            foreach (SaveBaking saveBaking in saveBakings)
            {
                saveBaking.BindLightmap();
                SetLayerRecursively(saveBaking.transform, layer);
            }
        }
        private void SetLayerRecursively(Transform transform, int layer)
        {
            transform.gameObject.layer = layer;
            foreach (Transform child in transform)
            {
                SetLayerRecursively(child, layer);
            }
        }
        /// <summary>
        /// 还原默认灯光位置
        /// </summary>
        public void ResetSceneLight()
        {
            if (sceneLight == null)
                return;
            sceneLight.transform.position = initSceneLightPos;
            sceneLight.transform.eulerAngles = initSceneLightEuler;
        }

        #endregion

        #region 相机裁切面
        public void ControlClipPlane(float near = 0.01f, float far = 1000f)
        {
            Camera.main.nearClipPlane = near;
            Camera.main.farClipPlane = far;
        }
        #endregion

        #region 模型节点相关

        private Dictionary<string, GameObject> UUIDModels = new Dictionary<string, GameObject>();

        private Dictionary<GameObject, string> ModelUUIDs = new Dictionary<GameObject, string>();

        public void AddModelUUID(ModelInfo modelInfo)
        {
            if (modelInfo == null)
                return;

            if (UUIDModels.ContainsKey(modelInfo.ID) || ModelUUIDs.ContainsKey(modelInfo.gameObject))
            {
                Log.Error($"存在重复UUID: {modelInfo.gameObject.name}");
                return;
            }
            UUIDModels.Add(modelInfo.ID, modelInfo.gameObject);
            ModelUUIDs.Add(modelInfo.gameObject, modelInfo.ID);
        }

        public GameObject GetModelByUUID(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
                return null;

            UUIDModels.TryGetValue(uuid, out GameObject go);
            return go;
        }

        public string GetUUIDByModel(GameObject go)
        {
            if (go == null)
                return string.Empty;

            ModelUUIDs.TryGetValue(go, out string id);
            return id;
        }

        public void ClearModelUUID()
        {
            UUIDModels.Clear();
            ModelUUIDs.Clear();
        }
        #endregion

        #region 同步组件相关
        private CameraSync cameraSync;
        /// <summary>
        /// 添加同步组件
        /// </summary>
        public void AddSyncComponent(GameObject gameObject)
        {
            //个人考核不要同步组件
            if(GlobalInfo.IsLiveMode() && (GlobalInfo.roomInfo.RoomType != 0 || GlobalInfo.roomInfo.ExamType != (int)ExamRoomType.Person))
                cameraSync = gameObject.AutoComponent<CameraSync>();
        }
        /// <summary>
        /// 获取同步组件高度 临时用于VR
        /// </summary>
        /// <returns></returns>
        public float GetCameraSyncHeight()
        {
            if (cameraSync)
                return cameraSync.gameObject.transform.position.y;
            return 0f;
        }
        /// <summary>
        /// 销毁同步组件
        /// </summary>
        public void DestroySyncComponent()
        {
            if (cameraSync)
            {
                DestroyImmediate(cameraSync);
                cameraSync = null;
            }
        }
        #endregion
    }
}