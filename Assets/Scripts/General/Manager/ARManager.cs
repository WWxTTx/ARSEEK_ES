using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace UnityFramework.Runtime
{
    /// <summary>
    /// AR管理类
    /// </summary>
    public class ARManager : Singleton<ARManager>
    {
        /// <summary>
        /// 主摄像机
        /// </summary>
        private Transform cameraMain;
        /// <summary>
        /// ARFoundation控制器
        /// </summary>
        public GameObject ARCamera;
        public Transform ARSessionOrigin;
        private ARCameraManager ARCameraManager;
        private ARCameraBackground ARCameraBackground;
        private ARPoseDriver ARPoseDriver;
        private bool ARSupported = false;

        /// <summary>
        /// 3D模型背景
        /// </summary>
        private GameObject BackGroundCanvas;
        /// <summary>
        /// 相机初始位置
        /// </summary>
        private Transform cameraParent;
        /// <summary>
        /// 相机初始位置
        /// </summary>
        private Vector3 cameraPosition;
        /// <summary>
        /// 相机初始角度
        /// </summary>
        private Vector3 cameraAngle;

        protected override void InstanceAwake()
        {
            cameraMain = Camera.main.transform;

            ARSessionOrigin = ComponentExtend.FindChildByName(ARCamera.transform, "AR Session Origin");
            ARCameraManager = cameraMain.GetComponent<ARCameraManager>();
            ARCameraBackground = cameraMain.GetComponent<ARCameraBackground>();
            ARPoseDriver = cameraMain.GetComponent<ARPoseDriver>();

            BackGroundCanvas = cameraMain.Find("BackGroundCanvas").gameObject;

            ARCameraManager.enabled = false;
            ARCameraBackground.enabled = false;
            ARPoseDriver.enabled = false;
            ARCamera.SetActive(false);

            cameraParent = cameraMain.parent;
            cameraPosition = cameraMain.position;
            cameraAngle = cameraMain.eulerAngles;
        }

        IEnumerator Start()
        {
            yield return ARSession.CheckAvailability();

            //当前设备不支持AR功能
            if (ARSession.state == ARSessionState.Unsupported)
            {
                Log.Warning("当前设备不支持AR功能");
            }
            else
            {
                //设备支持 AR，但需要安装
                if (ARSession.state == ARSessionState.NeedsInstall)
                {
                    Log.Info("设备支持 AR，但需要安装");
                    yield return ARSession.Install();
                }
                
                if (ARSession.state == ARSessionState.Ready)
                {
                    Log.Debug("设备支持 AR");
                    ARSupported = true;
                }
            }
        }

        /// <summary>
        /// 判断摄像头设备是否可使用
        /// </summary>
        public bool HasCamera()
        {
            WebCamDevice[] devices = WebCamTexture.devices;//获取相机数组
            if (devices.Length <= 0)
            {
                Log.Warning("未找到相机设备！");
                Dictionary<string, PopupButtonData> notSupport = new Dictionary<string, PopupButtonData>();
                notSupport.Add("确认", new PopupButtonData(null, true));
                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("错误", "未找到相机设备！", notSupport));
                return false;
            }

#if UNITY_STANDALONE || UNITY_EDITOR
            // 获取相机名称
            string deviceName = "T2 Webcam";
            if (string.IsNullOrEmpty(devices.Find(d => d.name == deviceName).name))//查找不到指定相机
            {
                Log.Warning("未找到相机设备-" + deviceName);
                Dictionary<string, PopupButtonData> notSupport = new Dictionary<string, PopupButtonData>();
                notSupport.Add("确认", new PopupButtonData(null, true));
                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("错误", "未找到相机设备-" + deviceName, notSupport));
                return false;
            }
#endif
            return true;
        }

        public bool OpenARSession()
        {
            if (!HasCamera())
                return false;

            if (!ARSupported)
            {
                Dictionary<string, PopupButtonData> notSupport = new Dictionary<string, PopupButtonData>();
                notSupport.Add("确认", new PopupButtonData(null, true));
                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("错误", string.Format("当前设备不支持{0}！", Application.platform == RuntimePlatform.IPhonePlayer ? "ARKit" : "ARCore"), notSupport));
                return false;
            }

            if (ARCamera)
            {
                cameraPosition = cameraMain.position;
                cameraAngle = cameraMain.eulerAngles;

                ARCamera.SetActive(true);
                ARCameraManager.enabled = true;
                ARCameraBackground.enabled = true;
                ARPoseDriver.enabled = true;
                cameraMain.SetParent(ARSessionOrigin);
                cameraMain.localPosition = Vector3.zero;
                cameraMain.localEulerAngles = Vector3.zero;
                BackGroundCanvas.SetActive(false);
            }
            return true;
        }

        public void CloseARSession()
        {
            Log.Debug("关闭ar，打开3d相机");
            if (ARCamera)
            {
                ARCameraManager.enabled = false;
                ARCameraBackground.enabled = false;
                ARPoseDriver.enabled = false;
                ARCamera.SetActive(false);
                BackGroundCanvas.SetActive(true);
                cameraMain.SetParent(cameraParent);
                cameraMain.SetAsFirstSibling();
                cameraMain.position = cameraPosition;
                cameraMain.eulerAngles = cameraAngle;
            }
        }

        public void ControlBackgroundCanvas(bool show)
        {
            BackGroundCanvas.SetActive(show);
        }

        #region 通过ARFoundation获取相机画面
        /// <summary>
        /// 获取ar相机画面
        /// </summary>
        public unsafe Texture2D GetARTexture2D()
        {
            if (!ARCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
                return null;

            var conversionParams = new XRCpuImage.ConversionParams
            {
                // Get the entire image.
                inputRect = new RectInt(0, 0, image.width, image.height),

                // Downsample by 2.
                outputDimensions = new Vector2Int(image.width / 2, image.height / 2),

                // Choose RGBA format.
                outputFormat = TextureFormat.RGBA32,

                // Flip across the vertical axis (mirror image).
                transformation = XRCpuImage.Transformation.MirrorY
            };

            // See how many bytes you need to store the final image.
            int size = image.GetConvertedDataSize(conversionParams);

            // Allocate a buffer to store the image.
            var buffer = new NativeArray<byte>(size, Allocator.Temp);

            // Extract the image data
            image.Convert(conversionParams, new IntPtr(buffer.GetUnsafePtr()), buffer.Length);
            
            // The image was converted to RGBA32 format and written into the provided buffer
            // so you can dispose of the XRCpuImage. You must do this or it will leak resources.
            image.Dispose();

            // At this point, you can process the image, pass it to a computer vision algorithm, etc.
            // In this example, you apply it to a texture to visualize it.

            // You've got the data; let's put it into a texture so you can visualize it.
            Texture2D m_Texture = new Texture2D(
                conversionParams.outputDimensions.x,
                conversionParams.outputDimensions.y,
                conversionParams.outputFormat,
                false);

            m_Texture.LoadRawTextureData(buffer);
            m_Texture.Apply();

            // Done with your temporary data, so you can dispose it.
            buffer.Dispose();

            return m_Texture;
        }
        #endregion
    }
}