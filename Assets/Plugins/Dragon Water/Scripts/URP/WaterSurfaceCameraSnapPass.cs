using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DragonWater.URP
{
    public class WaterSurfaceCameraSnapPass : ScriptableRenderPass
    {
        [System.Serializable]
        public class Settings
        {
            [Tooltip("Multiplier of cameras near plane distance, used to snap to water surface.")]
            [Range(0.25f, 3.0f)]
            public float nearClipFactor = 1.5f;
        }

        public Settings settings { get; private set; } = new();


        FieldInfo _viewMatrixField;

        public WaterSurfaceCameraSnapPass()
        {
            _viewMatrixField = typeof(CameraData)
                .GetField("m_ViewMatrix", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public void SetSettings(Settings settings)
        {
            this.settings = settings;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;
            var camera = cameraData.camera;

            if (camera != DragonWaterManager.Instance.MainCamera)
                return;

            var hit = DragonWaterManager.Instance.CameraHitResult;
            if (!hit.HasHit)
                return;

            var clampDistance = camera.nearClipPlane * settings.nearClipFactor;
            var clampOffset = 0.0f;
            if (hit.IsUnderwater)
            {
                if (hit.Depth < clampDistance)
                    clampOffset = -(clampDistance - hit.Depth);
            }
            else
            {
                if (hit.Height < clampDistance)
                    clampOffset = clampDistance - hit.Height;
            }

            if (clampOffset == 0.0f)
                return;

            var viewMatrix = cameraData.GetViewMatrix();
            var cameraTranslation = viewMatrix.GetColumn(3);
            var cameraUp = viewMatrix.GetColumn(1);

            viewMatrix.SetColumn(3, cameraTranslation - cameraUp * clampOffset);
            cameraData.worldSpaceCameraPos += Vector3.up * clampOffset;

            var reference = __makeref(cameraData);
            _viewMatrixField.SetValueDirect(reference, viewMatrix);
        }
    }
}
