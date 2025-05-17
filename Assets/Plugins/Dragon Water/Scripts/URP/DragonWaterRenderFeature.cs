using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DragonWater.URP
{
    public class DragonWaterRenderFeature : ScriptableRendererFeature
    {
        [Tooltip("Will copy depth texture once again so transparent object will have access to it with water surface included.\nNOTE: It works only with deferred renderer of forward with forced priming!")]
        public bool recopyDepthTexture = false;
        [Tooltip("Will copy opaque texture once again so transparent object will have access to it with water surface included.")]
        public bool recopyOpaqueTexture = false;
        public WaterSurfaceCameraSnapPass.Settings surfaceCameraSnap = new();
        public WaterCutoutRenderPass.Settings waterCutout = new();


        UnderwaterCausticsPass _underwaterCausticsPass;

        WaterSurfaceCameraSnapPass _waterSurfaceSnapPass;
        WaterCutoutRenderPass _waterCutoutPass;
        WaterSurfaceRenderPass _waterSurfacePass;

        ReCopyDepthPass _reCopyDepthPass;
        ReCopyColorPass _reCopyColorPass;

        public override void Create()
        {
            _underwaterCausticsPass = new();
            _underwaterCausticsPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox + 1;

            _waterSurfaceSnapPass = new();
            _waterSurfaceSnapPass.renderPassEvent = RenderPassEvent.BeforeRendering;
            _waterSurfaceSnapPass.SetSettings(surfaceCameraSnap);

            _waterCutoutPass = new();
            _waterCutoutPass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            _waterCutoutPass.SetSettings(waterCutout);

            _waterSurfacePass = new();
            _waterSurfacePass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox + 2;

            _reCopyDepthPass = new();
            _reCopyDepthPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox + 3;

            _reCopyColorPass = new();
            _reCopyColorPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox + 3;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var underwaterRenderer = DragonWaterManager.Instance.UnderwaterRenderer;

            if (underwaterRenderer.ShowCaustics)
            {
                renderer.EnqueuePass(_underwaterCausticsPass);
            }

            renderer.EnqueuePass(_waterSurfaceSnapPass);

            for (int i = 0; i < DragonWaterManager.Instance.Surfaces.Count; i++)
            {
                if (DragonWaterManager.Instance.Surfaces[i].CutoutWaterVolume)
                {
                    renderer.EnqueuePass(_waterCutoutPass);
                    break;
                }
            }

            renderer.EnqueuePass(_waterSurfacePass);


            if (recopyDepthTexture)
            {
                renderer.EnqueuePass(_reCopyDepthPass);
            }

            if (recopyOpaqueTexture)
            {
                renderer.EnqueuePass(_reCopyColorPass);
            }
        }

        public static bool CheckCurrentInstallation(out ScriptableRendererData rendererData)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            rendererData = pipeline.GetType().GetProperty("scriptableRendererData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(pipeline)
                as ScriptableRendererData;
            var features = rendererData.rendererFeatures;
            if (features.Any(f => f is DragonWaterRenderFeature))
                return true;
            else
                return false;
        }
    }
}