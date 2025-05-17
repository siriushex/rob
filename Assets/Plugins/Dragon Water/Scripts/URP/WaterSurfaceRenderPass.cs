using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DragonWater.URP
{
    public class WaterSurfaceRenderPass : ScriptableRenderPass
    {
        ShaderTagId _shaderTagID = new(Constants.Shader.Tag.WaterSurface);
        ProfilingSampler _profilingSampler = new("Dragon Water - Surface");

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var drawingSettings = CreateDrawingSettings(_shaderTagID, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
            var filteringSettings = new FilteringSettings(null, DragonWaterManager.Instance.Config.WaterRendererMask);

            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                // Render the objects...
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
