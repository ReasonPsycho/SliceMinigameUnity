using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class GlobalSliceRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public Material sliceMaterial;
    }

    public Settings settings = new Settings();
    private GlobalSliceRenderPass renderPass;

    public override void Create()
    {
        renderPass = new GlobalSliceRenderPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.sliceMaterial == null)
        {
            Debug.LogWarning("GlobalSliceRenderFeature: Slice material is null!");
            return;
        }
        
        renderer.EnqueuePass(renderPass);
    }
    
    protected override void Dispose(bool disposing)
    {
        renderPass?.Dispose();
    }

    class GlobalSliceRenderPass : ScriptableRenderPass
    {
        private Settings settings;
        private RTHandle tempHandle;
        private Material sliceMaterial;
        private const string profilerTag = "Global Slice Effect";

        public GlobalSliceRenderPass(Settings settings)
        {
            this.settings = settings;
            this.sliceMaterial = settings.sliceMaterial;
            renderPassEvent = settings.renderPassEvent;
        }

        // New RenderGraph path (Unity 6+)
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (sliceMaterial == null)
                return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            // Skip rendering for preview cameras
            if (cameraData.cameraType == CameraType.Preview)
                return;

            var source = resourceData.activeColorTexture;
            var descriptor = cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;

            TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, descriptor, "_TempSliceTexture", false);

            // Apply slice effect with proper blit
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(profilerTag, out var passData))
            {
                passData.sliceMaterial = sliceMaterial;
                passData.source = source;
                
                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }

            // Copy back to source
            using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("Copy Back", out var passData))
            {
                passData.source = destination;
                
                builder.UseTexture(destination, AccessFlags.Read);
                builder.SetRenderAttachment(source, 0, AccessFlags.Write);
                builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) => ExecuteCopyPass(data, context));
            }
        }

        private static void ExecutePass(PassData data, RasterGraphContext context)
        {
            // Use Blitter for proper texture sampling
            Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.sliceMaterial, 0);
        }

        private static void ExecuteCopyPass(CopyPassData data, RasterGraphContext context)
        {
            // Copy back
            Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 0, false);
        }

        // Legacy path (for compatibility mode)
#pragma warning disable CS0618
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            
            RenderingUtils.ReAllocateHandleIfNeeded(ref tempHandle, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_TempSliceTexture");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (sliceMaterial == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

            var source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            
            // Blit with slice effect
            Blitter.BlitCameraTexture(cmd, source, tempHandle, sliceMaterial, 0);
            Blitter.BlitCameraTexture(cmd, tempHandle, source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
#pragma warning restore CS0618

        public void Dispose()
        {
            tempHandle?.Release();
        }

        private class PassData
        {
            public Material sliceMaterial;
            public TextureHandle source;
        }

        private class CopyPassData
        {
            public TextureHandle source;
        }
    }
}