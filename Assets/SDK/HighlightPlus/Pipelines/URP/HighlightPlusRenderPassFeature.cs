using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HighlightPlus {

    public class HighlightPlusRenderPassFeature : ScriptableRendererFeature {
        class HighlightPass : ScriptableRenderPass {

            // 添加新的深度比较器（基于相机空间深度）
            class DepthComparer : IComparer<HighlightEffect>
            {
                public Camera camera;

                public int Compare(HighlightEffect e1, HighlightEffect e2)
                {
                    if (e1 == null || e2 == null || camera.name != "ModelCamera") return 0;

                    // 获取相机空间深度（z值越小表示越近）
                    float depth1 = GetEffectDepth(e1);
                    float depth2 = GetEffectDepth(e2);

                    // 近的物体应该后渲染（返回1表示e1应该在e2之后）
                    //Debug.Log(e1.gameObject.name + "与" + e2.gameObject.name + "的深度差为  " + (depth1 - depth2));
                    return depth1.CompareTo(depth2);
                }

                // 获取物体在视线方向上的最小深度（相机空间Z值）
                float GetEffectDepth(HighlightEffect effect)
                {
                    if (effect == null) 
                        return float.MaxValue;
                    Renderer renderer = effect.GetComponent<Renderer>();
                    if (renderer == null) 
                        return float.MaxValue;

                    Bounds bounds = renderer.bounds;
                    Vector3[] corners = GetBoundsCorners(bounds);
                    float minViewDepth = float.MaxValue;

                    // 遍历包围盒的8个顶点
                    foreach (Vector3 corner in corners)
                    {
                        // 将顶点转换到相机空间
                        Vector3 viewPos = camera.worldToCameraMatrix.MultiplyPoint3x4(corner);
                        // 相机空间中，viewPos.z 表示沿视线方向的深度（正值=前方，负值=后方）
                        // 取最小值（最靠近相机的点）
                        if (viewPos.z < minViewDepth) minViewDepth = viewPos.z;
                    }
                    return -minViewDepth;
                }

                // 获取包围盒的8个顶点
                Vector3[] GetBoundsCorners(Bounds bounds)
                {
                    Vector3[] corners = new Vector3[8];
                    Vector3 min = bounds.min;
                    Vector3 max = bounds.max;
                    corners[0] = new Vector3(min.x, min.y, min.z);
                    corners[1] = new Vector3(min.x, min.y, max.z);
                    corners[2] = new Vector3(min.x, max.y, min.z);
                    corners[3] = new Vector3(min.x, max.y, max.z);
                    corners[4] = new Vector3(max.x, min.y, min.z);
                    corners[5] = new Vector3(max.x, min.y, max.z);
                    corners[6] = new Vector3(max.x, max.y, min.z);
                    corners[7] = new Vector3(max.x, max.y, max.z);
                    return corners;
                }
            }

            public bool usesCameraOverlay = false;

            ScriptableRenderer renderer;
            RenderTextureDescriptor cameraTextureDescriptor;
            DepthComparer depthComparer;
            bool isVREnabled;

            public void Setup(RenderPassEvent renderPassEvent, ScriptableRenderer renderer) {
                this.renderPassEvent = renderPassEvent;
                this.renderer = renderer;
                if (depthComparer == null)
                {
                    depthComparer = new DepthComparer();
                }
                isVREnabled = UnityEngine.XR.XRSettings.enabled && Application.isPlaying;
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
                this.cameraTextureDescriptor = cameraTextureDescriptor;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
                Camera cam = renderingData.cameraData.camera;
                depthComparer.camera = cam; // 每帧更新相机引用

                int camLayer = 1 << cam.gameObject.layer;

                RenderTargetIdentifier cameraColorTarget = renderer.cameraColorTarget;
                RenderTargetIdentifier cameraDepthTarget = renderer.cameraDepthTarget;
                if (!usesCameraOverlay && (cameraTextureDescriptor.msaaSamples > 1 || cam.cameraType == CameraType.SceneView)) {
                    cameraDepthTarget = cameraColorTarget;
                }
                if (Time.frameCount % 10 == 0 || !Application.isPlaying) {
                    HighlightEffect.instances.Sort(depthComparer);
                }

                int count = HighlightEffect.instances.Count;
                for (int k = 0; k < count; k++) {
                    HighlightEffect effect = HighlightEffect.instances[k];
                    if (effect.isActiveAndEnabled) {
                        if ((effect.camerasLayerMask & camLayer) == 0) continue;
                        CommandBuffer cb = effect.GetCommandBuffer(cam, cameraColorTarget, cameraDepthTarget, FullScreenBlit);
                        if (cb != null) {
                            context.ExecuteCommandBuffer(cb);
                        }
                    }
                }
            }

            static Matrix4x4 matrix4x4identity = Matrix4x4.identity;
            void FullScreenBlit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material, int passIndex) {
                destination = new RenderTargetIdentifier(destination, 0, CubemapFace.Unknown, -1);
                cmd.SetRenderTarget(destination);
                cmd.SetGlobalTexture(ShaderParams.MainTex, source);
                cmd.SetGlobalFloat(ShaderParams.AspectRatio, isVREnabled ? 0.5f : 1);
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, matrix4x4identity, material, 0, passIndex);
            }

            public override void FrameCleanup(CommandBuffer cmd) {
            }
        }

        HighlightPass renderPass;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public static bool installed;


        void OnDisable() {
            installed = false;
        }

        public override void Create() {
            renderPass = new HighlightPass();
        }

        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
#if UNITY_2019_4_OR_NEWER
            if (renderingData.cameraData.renderType == CameraRenderType.Base) {
                Camera cam = renderingData.cameraData.camera;
                renderPass.usesCameraOverlay = cam.GetUniversalAdditionalCameraData().cameraStack.Count > 0;
            }
#endif
            renderPass.Setup(renderPassEvent, renderer);
            renderer.EnqueuePass(renderPass);
            installed = true;
        }
    }

}
