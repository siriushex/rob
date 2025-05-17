using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DragonWater.URP
{
    public class ReCopyDepthPass : ScriptableRenderPass
    {
        int _scaleBiasRt = Shader.PropertyToID("_ScaleBiasRt");
        ProfilingSampler _profilingSampler = new("Dragon Water - ReCopyDepth");

        Material _copyDepthMaterial;
        RenderTargetHandle _depthTexture;

        public ReCopyDepthPass()
        {
            _copyDepthMaterial = CoreUtils.CreateEngineMaterial("Hidden/Universal Render Pipeline/CopyDepth");
            _depthTexture.Init("_CameraDepthTexture");
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cameraTextureDescriptor.colorFormat = RenderTextureFormat.Depth;
#if UNITY_SWITCH || UNITY_ANDROID
            cameraTextureDescriptor.depthBufferBits = 24;
#else
            cameraTextureDescriptor.depthBufferBits = 32;
#endif
            cameraTextureDescriptor.msaaSamples = 1;
            if (!_depthTexture.HasInternalRenderTargetId())
                cmd.GetTemporaryRT(_depthTexture.id, cameraTextureDescriptor, FilterMode.Point);

            ConfigureTarget(_depthTexture.Identifier());
            ConfigureClear(ClearFlag.None, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, _profilingSampler))
            {

                var descriptor = renderingData.cameraData.cameraTargetDescriptor;
                var cameraSamples = descriptor.msaaSamples;

                // When auto resolve is supported or multisampled texture is not supported, set camera samples to 1
                if (SystemInfo.supportsMultisampledTextures == 0)
                    cameraSamples = 1;

                var cameraData = renderingData.cameraData;

                switch (cameraSamples)
                {
                    case 8:
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                        cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                        break;

                    case 4:
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                        break;

                    case 2:
                        cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                        break;

                    // MSAA disabled, auto resolve supported or ms textures not supported
                    default:
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                        break;
                }


                cmd.SetGlobalTexture("_CameraDepthAttachment", renderingData.cameraData.renderer.cameraDepthTarget);

                var yflip = cameraData.IsCameraProjectionMatrixFlipped();
                var flipSign = yflip ? -1.0f : 1.0f;
                Vector4 scaleBiasRt = (flipSign < 0.0f)
                    ? new Vector4(flipSign, 1.0f, -1.0f, 1.0f)
                    : new Vector4(flipSign, 0.0f, 1.0f, 1.0f);
                cmd.SetGlobalVector(_scaleBiasRt, scaleBiasRt);
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, _copyDepthMaterial);

                cmd.SetGlobalTexture("_CameraDepthTexture", _depthTexture.Identifier());
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
