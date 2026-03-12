using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityFramework.Runtime
{
    public class UIInformationModule : UIModuleBase
    {
        public GameObject prefabItem;
        public Transform content;
        private List<string> datas = new List<string>();
        private List<GameObject> items = new List<GameObject>();

        public override void Show(UIData uiData = null)
        {
            base.Show(uiData);
            Refresh();
        }
        /// <summary>
        /// 获取设备信息
        /// </summary>
        /// <returns>设备信息列表</returns>
        public static List<string> GetDevInfo()
        {
            List<string> datas = new List<string>();
            //设备
            datas.Add(string.Format("<color=#1500FF>{0}</color>", "---------------设备信息---------------"));
            datas.Add("设备ID   " + SystemInfo.deviceUniqueIdentifier);//设备唯一标识符
            datas.Add("设备名称 " + SystemInfo.deviceName);//设备名称
            datas.Add("设备类型 " + SystemInfo.deviceType);//设备类型
            datas.Add("设备型号 " + SystemInfo.deviceModel);//设备型号
                                                        //处理器
            datas.Add("CPU名称  " + SystemInfo.processorType);//处理器名称
                                                            //内存
            datas.Add("系统内存  " + SystemInfo.systemMemorySize + " MB");//系统内存
                                                                      //操作系统
            datas.Add("操作系统  " + SystemInfo.operatingSystem);//操作系统
                                                             //显卡
            datas.Add("GPU名称  " + SystemInfo.graphicsDeviceName);//显卡名称
            datas.Add("GPU驱动  " + SystemInfo.graphicsDeviceVersion);//显卡驱动版本
            datas.Add("GPU内存  " + SystemInfo.graphicsMemorySize + " MB");//显存
            datas.Add("电池状态 " + SystemInfo.batteryStatus.ToString());//电池状态

            datas.Add(string.Format("<color=#1500FF>{0}</color>", "---------------渲染信息---------------"));
            datas.Add("渲染管线 " + Shader.globalRenderPipeline);//渲染管线
            datas.Add("HDR支持  " + SystemInfo.hdrDisplaySupportFlags);//HDR显示支持
            datas.Add("阴影支持 " + SystemInfo.supportsShadows);//是否支持内置阴影
            datas.Add("最大立方体贴图 " + SystemInfo.maxCubemapSize);//最大立方体贴图纹理大小
            datas.Add("最大纹理       " + SystemInfo.maxTextureSize);//最大纹理大小

            datas.Add(string.Format("<color=#1500FF>{0}</color>", "---------------支持功能---------------"));
            datas.Add("支持音频      " + SystemInfo.supportsAudio);//支持音频
            datas.Add("支持振动      " + SystemInfo.supportsVibration);//支持振动
            datas.Add("支持运动矢量  " + SystemInfo.supportsMotionVectors);//支持运动矢量
            datas.Add("支持陀螺仪    " + SystemInfo.supportsGyroscope);//支持陀螺仪
            datas.Add("支持光线追踪  " + SystemInfo.supportsRayTracing);//支持光线追踪
            datas.Add("支持加速度计  " + SystemInfo.supportsAccelerometer);//支持加速度计
            datas.Add("支持定位服务  " + SystemInfo.supportsLocationService);//支持定位服务

            datas.Add(string.Format("<color=#1500FF>{0}</color>", "-----------纹理、着色器支持-----------"));
            datas.Add("NPOT Support                    " + SystemInfo.npotSupport);//（非二次幂）纹理支持
            datas.Add("Supports 2D Array Textures      " + SystemInfo.supports2DArrayTextures.ToString());//是否支持 2D 阵列纹理
            datas.Add("Supports 3D Render Textures     " + SystemInfo.supports3DRenderTextures.ToString());//是否支持 3D（体积）渲染纹理
            datas.Add("Supports 3D Textures            " + SystemInfo.supports3DTextures.ToString());//是否支持 3D（体积）纹理
            datas.Add("Supports Sparse Textures        " + SystemInfo.supportsSparseTextures.ToString());//是否支持稀疏纹理
            datas.Add("Supports Cubemap Array Textures " + SystemInfo.supportsCubemapArrayTextures.ToString());//是否支持 Cubemap Array 纹理
            datas.Add("Supports Multi-sampled Textures " + SystemInfo.supportsMultisampledTextures.ToString());//是否支持多重采样纹理
            datas.Add("Supports Compute Shader         " + SystemInfo.supportsComputeShaders.ToString());//是否支持计算着色器
            datas.Add("Supports Geometry Shaders       " + SystemInfo.supportsGeometryShaders.ToString());//是否支持几何着色器
            datas.Add("Supports Tessellation Shaders   " + SystemInfo.supportsTessellationShaders.ToString());//是否支持曲面细分着色器

            datas.Add(string.Format("<color=#1500FF>{0}</color>", "-------------应用程序信息-------------"));
            datas.Add("Genuine                  " + Application.genuine);//应用程序在构建后是否被更改
            datas.Add("Genuine Check Available  " + Application.genuineCheckAvailable);//应用程序完整性可以被确认

            return datas;
        }
        void Refresh()
        {
            datas = GetDevInfo();

            GameObject go = null;
            for (int i = 0; i < datas.Count; i++)
            {
                if (i >= items.Count)
                {
                    go = Instantiate(prefabItem, content);
                    go.SetActive(true);
                    items.Add(go);
                }
                else
                    go = items[i];

               go.GetComponentInChildren<Text>().text = datas[i];
            }
        }
    }
}
