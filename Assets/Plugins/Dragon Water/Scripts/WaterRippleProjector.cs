using DragonWater.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DragonWater
{
    [AddComponentMenu("Dragon Water/Ripple Projector")]
    [ExecuteAlways]
    public class WaterRippleProjector : MonoBehaviour
    {
        [Serializable]
        public enum ProjectorType
        {
            Local,
            Infinite
        }

        [Serializable]
        public enum AttachTarget
        {
            MainCamera,
            CustomObject
        }

        [Serializable]
        public enum PrecisionType
        {
            HeightOffset,
            Simple,
            Flat
        }

        public class WaveRippler
        {
            public WaveProfile Profile { get; internal set; }
            public RenderTexture SimulationTexture { get; internal set; }

            // this is texture for extra blur layer which, in the end, is not really needed
            // not good enough results in comparison to performance impact
            // maybe it can be enabled in future, as an optional quality feature
            //public RenderTexture RippleTexture { get; internal set; }

            public bool IsEnqueued { get; internal set; }


            public void Enqueue()
            {
                IsEnqueued = true;
            }
            public void Dequeue()
            {
                IsEnqueued = false;
            }

            internal void CheckTextures(int width, int height)
            {
                if (SimulationTexture != null && SimulationTexture.IsCreated())
                {
                    if (SimulationTexture.width != width || SimulationTexture.height != height)
                    {
                        SimulationTexture.Release();
                        SimulationTexture = null;
                    }
                }
                /*
                if (RippleTexture != null && RippleTexture.IsCreated())
                {
                    if (RippleTexture.width != width || RippleTexture.height != height)
                    {
                        RippleTexture.Release();
                        RippleTexture = null;
                    }
                }
                */

                if (SimulationTexture == null)
                {
                    SimulationTexture = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat);
                    SimulationTexture.enableRandomWrite = true;
                    SimulationTexture.Create();
                }
                /*
                if (RippleTexture == null)
                {
                    RippleTexture = new RenderTexture(width, height, 0, RenderTextureFormat.R8);
                    RippleTexture.enableRandomWrite = true;
                    RippleTexture.Create();
                }
                */
            }
            internal void Release()
            {
                if (SimulationTexture != null && SimulationTexture.IsCreated())
                {
                    SimulationTexture.Release();
                }
                /*
                if (RippleTexture != null && RippleTexture.IsCreated())
                {
                    RippleTexture.Release();
                }
                */
            }

            internal void BlitOffsetTexture(RenderTexture rt, Vector2Int offset)
            {
                var uv = new Vector2(
                    (float)offset.x / rt.width,
                    -(float)offset.y / rt.height
                    );

                Graphics.Blit(SimulationTexture, rt, Vector2.one, uv);
                Graphics.Blit(rt, SimulationTexture);

                /*
                Graphics.Blit(RippleTexture, rt, Vector2.one, uv);
                Graphics.Blit(rt, RippleTexture);
                */
            }

            internal void DispatchRippleCompute(ComputeShader shader, int kernel, PrecisionType precision)
            {
                if (precision == PrecisionType.HeightOffset)
                {
                    Profile.RequestHeightOffsetTextureProcessing();
                }

                Profile.ConfigureRippler(shader, kernel, precision == PrecisionType.Flat);

                shader.SetTexture(kernel, Constants.Shader.Property.ComputeResultSimulation, SimulationTexture);
                //shader.SetTexture(kernel, Constants.Shader.Property.ComputeResultRipple, RippleTexture);

                shader.Dispatch(kernel, SimulationTexture.width / 8, SimulationTexture.height / 8, 1);
            }
        }

        [SerializeField] internal int textureSize = 512;
        [SerializeField] internal ProjectorType type = ProjectorType.Local;
        [SerializeField] internal Vector2 size = new Vector2(128,128);
        [SerializeField] internal float distance = 128;
        [SerializeField] internal AttachTarget attachTo = AttachTarget.MainCamera;
        [SerializeField] internal Transform attachCustomTarget = null;
        [SerializeField] internal float upperClip = 10.0f;
        [SerializeField] internal float lowerClip = 10.0f;
        [SerializeField] internal PrecisionType precision = PrecisionType.HeightOffset;

        Camera _camera;
        RenderTexture _renderTexture;
        List<WaveRippler> _ripplers = new();
        Vector2Int _infiniteOffset = Vector2Int.zero;


        #region properties
        public int TextureSize
        {
            get { return textureSize; }
            set
            {
                textureSize = value;
                UpdateProjector();
            }
        }
        public ProjectorType Type
        {
            get { return type; }
            set
            {
                if (type == value) return;
#if UNITY_EDITOR
                //EditorSaveParams();
#endif
                type = value;
                UpdateProjector();
            }
        }
        public Vector3 Size
        {
            get { return size; }
            set
            {
                size = value;
                if (type == ProjectorType.Local) { UpdateProjector(); }
            }
        }
        public float Distance
        {
            get { return distance; }
            set
            {
                distance = value;
                if (type == ProjectorType.Infinite) { UpdateProjector(); }
            }
        }
        public AttachTarget AttachTo
        {
            get { return attachTo; }
            set
            {
                attachTo = value;
                if (type == ProjectorType.Infinite) { UpdateProjector(); }
            }
        }
        public Transform AttachCustomTarget
        {
            get { return attachCustomTarget; }
            set
            {
                attachCustomTarget = value;
                if (type == ProjectorType.Infinite) { UpdateProjector(); }
            }
        }
        public float LowerClip
        {
            get { return lowerClip; }
            set
            {
                lowerClip = value;
                UpdateProjector();
            }
        }
        public float UpperClip
        {
            get { return upperClip; }
            set
            {
                upperClip = value;
                UpdateProjector();
            }
        }
        public PrecisionType Precision
        {
            get { return precision; }
            set
            {
                precision = value;
            }
        }
        #endregion

        public RenderTexture ProjectionTexture => GetRenderTexture();
        public IReadOnlyList<WaveRippler> Ripplers => _ripplers;


        private void Awake()
        {
            UpdateProjector();
        }
        private void OnEnable()
        {
            if (_camera) _camera.enabled = true;
            UpdateProjector();
            DragonWaterManager.Instance.RegisterRippleProjector(this);
        }
        private void OnDisable()
        {
            if (_camera) _camera.enabled = false;
            DragonWaterManager.InstanceUnsafe?.UnregisterRippleProjector(this);

            DequeueAllRipplers();
        }
        private void OnDestroy()
        {
            if (_renderTexture != null && _renderTexture.IsCreated())
            {
                _renderTexture.Release();
            }

            _ripplers.ForEach(r => r.Release());
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (DragonWaterManager.InstanceUnsafe != null)
                UpdateProjector();
        }
#endif

#if UNITY_EDITOR
        // in editor mode, force update activeness in manager in case of script reload
        private void Update()
        {
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (enabled && !DragonWaterManager.Instance.RippleProjectors.Contains(this))
                {
                    enabled = false;
                    enabled = true;
                }
            }
        }
#endif


        internal WaveRippler CreateRippler(WaveProfile profile)
        {
            var rt = ProjectionTexture;

            WaveRippler rippler = null;
            for (int i = 0; i < _ripplers.Count; i++)
            {
                if (_ripplers[i].Profile == profile)
                {
                    rippler = _ripplers[i];
                    break;
                }
            }

            if (rippler == null)
            {
                rippler = new()
                {
                    Profile = profile
                };
                _ripplers.Add(rippler);
            }

            rippler.CheckTextures(rt.width, rt.height);
            return rippler;
        }

        internal void ConfigureMaterial(Material material, WaveRippler rippler)
        {
            material.EnableKeyword(Constants.Shader.Keyword.UseRipple);
            //material.SetTexture(Constants.Shader.Property.RippleTexture, rippler.RippleTexture);
            material.SetTexture(Constants.Shader.Property.RippleTexture, rippler.SimulationTexture);

            var position = transform.position;
            if (type == ProjectorType.Infinite)
            {
                material.SetVector(Constants.Shader.Property.RippleProjection,
                    new Vector4(position.x - distance, position.z - distance, distance * 2.0f, distance * 2.0f)
                );
            }
            else if (type == ProjectorType.Local)
            {
                material.SetVector(Constants.Shader.Property.RippleProjection,
                    new Vector4(position.x - size.x * 0.5f, position.z - size.y * 0.5f, size.x, size.y)
                );
            }

        }
        internal static void CleanupMaterial(Material material)
        {
            material.DisableKeyword(Constants.Shader.Keyword.UseRipple);
        }

        internal void UpdateProjector()
        {
            UpdateCameraParams();
        }
        internal void DequeueAllRipplers()
        {
            _ripplers.ForEach(r => r.Dequeue());
        }

        internal void UpdateProjector(Camera mainCamera)
        {
            if (type == ProjectorType.Infinite)
            {
                var attachTransform = (attachTo == AttachTarget.CustomObject && attachCustomTarget != null)
                    ? attachCustomTarget : mainCamera.transform;

                var snap = (distance * 2.0f) / textureSize;

                var x = attachTransform.position.x;
                var y = attachTransform.position.z;

                var ox = Mathf.Sign(x) * Mathf.Repeat(Mathf.Abs(x), snap);
                var oy = Mathf.Sign(y) * Mathf.Repeat(Mathf.Abs(y), snap);

                var snappedX = x - ox;
                var snappedY = y - oy;

                var oldX = transform.position.x;
                var height = transform.position.y;
                var oldY = transform.position.z;

                transform.position = new Vector3(snappedX, height, snappedY);

                var difference = new Vector2Int(
                    Mathf.RoundToInt((snappedX - oldX) / snap),
                    Mathf.RoundToInt((snappedY - oldY) / snap)
                    );
                _infiniteOffset += difference;
            }
        }
        
        internal void DispatchRippleCompute(ComputeShader shader, int kernelMain)
        {
            if (_renderTexture == null || !_renderTexture.IsCreated()) return;

            var anyQueued = false;
            for (int i = 0; i < _ripplers.Count; i++)
            {
                if (_ripplers[i].IsEnqueued)
                {
                    anyQueued = true;
                    break;
                }
            }
            if (!anyQueued)
                return;

            shader.SetTexture(kernelMain, Constants.Shader.Property.ComputeRippleProjectionTex, _renderTexture);

            var position = transform.position;
            shader.SetVector(Constants.Shader.Property.ComputeProjectorOffset, new Vector4(
                position.x, position.z, 0, 0
                ));
            shader.SetFloat(Constants.Shader.Property.ComputeProjectorY, position.y);

            shader.SetVector(Constants.Shader.Property.ComputeRippleTextureSize, new Vector4(
                _renderTexture.width, _renderTexture.height, 0, 0
                ));
            if (type == ProjectorType.Infinite)
            {
                shader.SetVector(Constants.Shader.Property.ComputeRippleProjectionSize, new Vector4(distance * 2.0f, distance * 2.0f, 0, 0));
            }
            else if (type == ProjectorType.Local)
            {
                shader.SetVector(Constants.Shader.Property.ComputeRippleProjectionSize, new Vector4(size.x, size.y, 0, 0));
            }

            var rt = GetRenderTexture();
            var blit = type == ProjectorType.Infinite && _infiniteOffset.sqrMagnitude > 0
                ? RenderTexture.GetTemporary(rt.width, rt.height, 0, RenderTextureFormat.RFloat)
                : null;

            for (int i = 0; i < _ripplers.Count; i++)
            {
                if (_ripplers[i].IsEnqueued)
                {
                    if (blit != null)
                        _ripplers[i].BlitOffsetTexture(blit, _infiniteOffset);

                    _ripplers[i].DispatchRippleCompute(shader, kernelMain, precision);
                }
            }

            if (blit != null)
            {
                RenderTexture.ReleaseTemporary(blit);
            }

            _infiniteOffset = Vector2Int.zero;
        }

        private Camera GetCamera()
        {
            if (_camera != null)
            {
                return _camera;
            }

            var child = transform.Find("#RippleCamera");
            if (child == null)
            {
                child = new GameObject("#RippleCamera").transform;
                child.parent = transform;
                child.localPosition = Vector3.zero;
                child.localRotation = Quaternion.Euler(-90, 0, 0);
                child.localScale = Vector3.one;
                child.gameObject.AddComponent<Camera>();
            }

            child.gameObject.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            //child.gameObject.hideFlags = HideFlags.None;

            _camera = child.GetComponent<Camera>();
            UpdateCameraParams();

            return _camera;
        }
        private RenderTexture GetRenderTexture()
        {
            var width = 0;
            var height = 0;
            var ratio = 1.0f;

            if (type == ProjectorType.Infinite)
            {
                width = textureSize;
                height = textureSize;
            }
            else if (type == ProjectorType.Local)
            {
                if (size.magnitude >= 1.0f)
                    ratio = size.x / size.y;

                if (ratio > 1.0f)
                {
                    width = textureSize;
                    height = Mathf.RoundToInt((textureSize / ratio) * 0.125f) * 8;
                    height = Mathf.Max(height, 8);
                }
                else
                {
                    width = Mathf.RoundToInt((textureSize * ratio) * 0.125f) * 8;
                    height = textureSize;
                    width = Mathf.Max(width, 8);
                }
            }

            if (_renderTexture != null &&
                _renderTexture.IsCreated())
            {
                if (_renderTexture.width != width || _renderTexture.height != height)
                {
                    _renderTexture.Release();
                    _renderTexture = null;
                }
                else
                {
                    return _renderTexture;
                }
            }

            var rtFormat = SystemInfo.SupportsRandomWriteOnRenderTextureFormat(RenderTextureFormat.RHalf)
                ? RenderTextureFormat.RHalf
                : RenderTextureFormat.RFloat;
            _renderTexture = new RenderTexture(width, height, 0, rtFormat);
            var msaaSupport = SystemInfo.GetRenderTextureSupportedMSAASampleCount(_renderTexture.descriptor);
            if (msaaSupport >= 4)
                _renderTexture.antiAliasing = 4;
            else if (msaaSupport >= 2)
                _renderTexture.antiAliasing = 2;
            _renderTexture.Create();

            GetCamera().targetTexture = _renderTexture;

            return _renderTexture;
        }
        private void UpdateCameraParams()
        {
            if (_camera == null) GetCamera();

            _camera.orthographic = true;
            _camera.targetTexture = GetRenderTexture();
            _camera.aspect = type == ProjectorType.Infinite ? 1.0f : size.x / size.y;
            _camera.farClipPlane = upperClip;
            _camera.nearClipPlane = -lowerClip;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(float.MaxValue, 0, 0, 0);
            _camera.allowMSAA = true;
            _camera.useOcclusionCulling = false;

            if (type == ProjectorType.Infinite)
            {
                _camera.orthographicSize = distance;
            }
            else if (type == ProjectorType.Local)
            {
                _camera.orthographicSize = size.y * 0.5f;
            }

            _camera.cullingMask = DragonWaterManager.Instance.Config.RippleMask;

            var data = _camera.GetUniversalAdditionalCameraData();
            data.renderShadows = false;
            data.requiresColorTexture = false;
            data.requiresDepthTexture = false;
            data.renderPostProcessing = false;
            data.SetRenderer(DragonWaterManager.Instance.Config.RippleURPRendererIndex);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // force zero rotation
            transform.rotation = Quaternion.identity;

            Gizmos.matrix = transform.localToWorldMatrix;

            var cube = Vector3.zero;
            if (type == ProjectorType.Infinite)
            {
                cube = new Vector3(distance, 0.1f, distance) * 2.0f;
            }
            else if (type == ProjectorType.Local)
            {
                cube = new Vector3(size.x, 0.2f, size.y);
            }

            Gizmos.color = new Color(0.5f, 0.0f, 1.0f, 0.4f);
            Gizmos.DrawCube(Vector3.zero, cube);

            cube.y = (lowerClip + upperClip);

            Gizmos.color = new Color(0.0f, 1.0f, 0.5f, 0.1f);
            Gizmos.DrawCube(
                Vector3.zero + Vector3.up * (upperClip - lowerClip) * 0.5f,
                cube
                );

            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(
                Vector3.zero + Vector3.up * (upperClip - lowerClip) * 0.5f,
                cube
                );
        }
#endif
    }
}
