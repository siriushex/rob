using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DragonWater.URP
{
    public class WaterCutoutRenderPass : ScriptableRenderPass
    {
        [System.Serializable]
        public class Settings
        {
            [Tooltip("Scale of screen resolution used for drawing water cutout mask.\nIn most cases value in range 0.25-0.5 is enough.")]
            [Range(0.1f, 1.0f)]
            public float maskResolutionScale = 0.5f;
        }

        public Settings settings { get; private set; } = new();


        ShaderTagId _shaderTagID = new(Constants.Shader.Tag.WaterCutout);
        ProfilingSampler _profilingSampler = new("Dragon Water - Cutout");

        int _tmpId = Constants.Shader.Property.WaterCutoutMask;
        RenderTargetIdentifier _rt;


        public void SetSettings(Settings settings)
        {
            this.settings = settings;
        }


        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor ctr)
        {
            var width = ctr.width * settings.maskResolutionScale;
            var height = ctr.height * settings.maskResolutionScale;

            cmd.GetTemporaryRT(_tmpId, (int)width, (int)height, 0, FilterMode.Point, RenderTextureFormat.RGFloat);
            _rt = new RenderTargetIdentifier(_tmpId);
            ConfigureTarget(_rt);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var drawingSettings = CreateDrawingSettings(_shaderTagID, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
            var filteringSettings = new FilteringSettings(null, DragonWaterManager.Instance.Config.CutoutMask);
            
            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                // clear texture
                cmd.SetRenderTarget(_rt);
                cmd.ClearRenderTarget(false, true, new Color(float.MaxValue, float.MaxValue, 0, 0));

                // Ensure we flush our command-buffer before we render...
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
