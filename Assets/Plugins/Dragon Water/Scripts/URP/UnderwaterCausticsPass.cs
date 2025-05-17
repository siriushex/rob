using DragonWater.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DragonWater.URP
{
    public class UnderwaterCausticsPass : ScriptableRenderPass
    {
        ShaderTagId _shaderTagID = new(Constants.Shader.Tag.UnderwaterCaustics);
        ProfilingSampler _profilingSampler = new("Dragon Underwater - Caustics");

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var underwaterRenderer = DragonWaterManager.Instance.UnderwaterRenderer;

            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                cmd.DrawMesh(
                    MeshUtility.GetBoxPrimitive(),
                    Matrix4x4.TRS(renderingData.cameraData.worldSpaceCameraPos, Quaternion.identity, Vector3.one * renderingData.cameraData.camera.farClipPlane * 0.25f),
                    underwaterRenderer.CausticsMaterial
                    );
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
