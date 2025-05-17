using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DragonWater.URP
{
    public class ReCopyColorPass : ScriptableRenderPass
    {
        int _sampleOffsetShaderHandle = Shader.PropertyToID("_SampleOffset");
        ProfilingSampler _profilingSampler = new("Dragon Water - ReCopyColor");

        Material _blitMaterial;
        Material _samplingMaterial;
        RenderTargetHandle _opaqueColor;

        public ReCopyColorPass()
        {
            _blitMaterial = CoreUtils.CreateEngineMaterial("Hidden/Universal Render Pipeline/Blit");
            _samplingMaterial = CoreUtils.CreateEngineMaterial("Hidden/Universal Render Pipeline/Sampling");
            _opaqueColor.Init("_CameraOpaqueTexture");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, _profilingSampler))
            {
                var downsamplingMethod = UniversalRenderPipeline.asset.opaqueDownsampling;
                switch (downsamplingMethod)
                {
                    case Downsampling.None:
                        cmd.Blit(colorAttachment, _opaqueColor.Identifier(), _blitMaterial);
                        break;
                    case Downsampling._2xBilinear:
                        cmd.Blit(colorAttachment, _opaqueColor.Identifier(), _blitMaterial);
                        break;
                    case Downsampling._4xBox:
                        _samplingMaterial.SetFloat(_sampleOffsetShaderHandle, 2);
                        cmd.Blit(colorAttachment, _opaqueColor.Identifier(), _blitMaterial);
                        break;
                    case Downsampling._4xBilinear:
                        cmd.Blit(colorAttachment, _opaqueColor.Identifier(), _blitMaterial);
                        break;
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
