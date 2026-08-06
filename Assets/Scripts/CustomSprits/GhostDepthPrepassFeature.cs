using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GhostDepthPrepassFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        public LayerMask layerMask = -1;
        public string shaderTagId = "GhostDepthPrime";
    }

    public Settings settings = new Settings();
    GhostDepthPrepass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new GhostDepthPrepass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }

    class GhostDepthPrepass : ScriptableRenderPass
    {
        private Settings settings;
        private FilteringSettings m_FilteringSettings;
        private ShaderTagId m_ShaderTagId;

        public GhostDepthPrepass(Settings settings)
        {
            this.settings = settings;
            renderPassEvent = settings.renderPassEvent;
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.transparent, settings.layerMask);
            m_ShaderTagId = new ShaderTagId(settings.shaderTagId);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Ghost Depth Prepass");

            var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
            var drawSettings = CreateDrawingSettings(m_ShaderTagId, ref renderingData, sortFlags);
            drawSettings.perObjectData = PerObjectData.None;

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_FilteringSettings);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
