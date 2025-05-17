using DragonWater.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace DragonWater
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct GerstnerWave
    {
        public const int SIZEOF = 6 * 4;

        public Vector2 direction;

        public float number;
        public float steepness;

        public float amplitude;
        public float speed;

        public float height => amplitude * 2.0f;
        public float length => NumberToLength(number);
        public float frequency => speed / length;


        public static float LengthToNumber(float waveLength)
        {
            return (Mathf.PI * 2) / waveLength;
        }
        public static float NumberToLength(float waveNumber)
        {
            return (Mathf.PI * 2) / waveNumber;
        }
        public static float GetSpeed(float waveNumber, float gravity)
        {
            return Mathf.Sqrt(gravity / waveNumber);
        }
        public static float GetAmplitude(float waveLength, float steepness, float heightRatio)
        {
            return waveLength * heightRatio * steepness * 0.5f;
        }
        public static float GetLength(float frequency, float gravity)
        {
            return gravity / (2.0f * Mathf.PI * frequency * frequency);
        }

        public static GerstnerWave CreateDefault()
        {
            return new()
            {
                direction = Vector2.up,
                number = 1.0f,
                steepness = 0.5f,
                amplitude = 0.5f,
                speed = 1.0f
            };
        }
    }

    [CreateAssetMenu(menuName = "Dragon Water/Wave Profile")]
    public class WaveProfile : ScriptableObject
    {
        const int MAX_WAVE_BUFFER_SIZE = 64;
        const float PROJECTION_SIZE_SNAP = 16.0f; // highest value of density in infinite ocean mesh

        public class Sampler : IDisposable
        {
            static List<Sampler> _ActiveSamplers = new();
            public static IReadOnlyList<Sampler> ActiveSamplers => _ActiveSamplers;


            public readonly WaveProfile profile;
            internal struct Job : IJobParallelFor
            {
                [ReadOnly] public NativeSlice<GerstnerWave> waves;
                [ReadOnly] public NativeSlice<LocalWaveAreaEntry> areas;
                [ReadOnly] public NativeSlice<Vector3> points;

                [ReadOnly] public Vector3 baseInfluences;
                [ReadOnly] public bool includeLocalAreas;
                [ReadOnly] public float time;
                [ReadOnly] public int precision;

                [ReadOnly] public Vector2 originPosition;
                [ReadOnly] public float originRotation;

                public NativeArray<Vector3> resultPositions;
                public NativeArray<Vector3> resultNormals;

                public void Execute(int index)
                {
                    var world = points[index];
                    var precision = this.precision;

                    var offset = Vector3.zero;
                    var normal = Vector3.zero;

                    var targetPosition = new Vector2(world.x, world.z);
                    var samplePosition = targetPosition;

                    var influences = GetLocalInfluences(targetPosition);
                    while (precision-- > 0)
                    {
                        var transformedPosition = samplePosition - originPosition;
                        if (originRotation != 0)
                            transformedPosition = RotateVec2(transformedPosition, originRotation);

                        CalculateWave(transformedPosition, out offset, out normal, influences);

                        if (precision > 0)
                        {
                            var flatOffset = new Vector2(offset.x, offset.z);
                            var difference = (samplePosition + flatOffset) - targetPosition;
                            samplePosition -= difference;
                        }
                    }

                    resultPositions[index] = new Vector3(samplePosition.x + offset.x, offset.y, samplePosition.y + offset.z);
                    resultNormals[index] = (normal + Vector3.up).normalized;
                }

                private void CalculateWave(Vector2 position, out Vector3 offset, out Vector3 normal, Vector3 influences)
                {
                    var time = this.time;

                    offset = Vector3.zero;
                    normal = Vector3.zero;

                    for (int i = 0; i < waves.Length; i++)
                    {
                        var wave = waves[i];

                        var d = Vector2.Dot(wave.direction, position);
                        var f = wave.number * (d - wave.speed * time);

                        var C = Mathf.Cos(f);
                        var S = Mathf.Sin(f);

                        var width = wave.steepness * wave.amplitude * C * influences.x * influences.y;
                        var height = wave.amplitude * S * influences.x;

                        offset += new Vector3(
                            wave.direction.x * width,
                            height,
                            wave.direction.y * width
                            );

                        normal -= new Vector3(
                            wave.direction.x * width * wave.number,
                            height * wave.number * wave.steepness * influences.y,
                            wave.direction.y * width * wave.number
                            );
                    }
                }

                private Vector3 GetLocalInfluences(Vector2 position)
                {
                    var influences = baseInfluences;

                    if (!includeLocalAreas)
                        return influences;

                    for (int i = 0; i < areas.Length; i++)
                    {
                        var area = areas[i];

                        var radius = area.radius.x;
                        var innerRadius = area.radius.x * area.radius.y;

                        var d = Vector2.Distance(position, area.position);
                        var f = 1.0f;
                        if (d < innerRadius)
                        {
                            f = 0.0f;
                        }
                        else if (d < radius)
                        {
                            f = Mathf.InverseLerp(innerRadius, radius, d);
                        }

                        influences = Vector3.Scale(influences, Vector3.Lerp(area.influences, Vector3.one, f));
                    }

                    return influences;
                }

                private Vector2 RotateVec2(Vector2 position, float rot)
                {
                    float s = Mathf.Sin(rot);
                    float c = Mathf.Cos(rot);
                    return new(
                        (position.x * c) - (position.y * s),
                        (position.y * c) + (position.x * s)
                    );
                }
            }


            public bool IsRunning { get; private set; }
            public NativeArray<Vector3> ResultPositions { get; private set; }
            public NativeArray<Vector3> ResultNormals { get; private set; }
            public int ResultCount { get; private set; }

            Job _job;
            JobHandle _handle;
            int _scheduleCount;
            bool _disposed;

            internal Sampler(WaveProfile profile, int maxSize = 1)
            {
                this.profile = profile;

                ResultPositions = new(maxSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                ResultNormals = new(maxSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

                _job = new()
                {
                    resultPositions = ResultPositions,
                    resultNormals = ResultNormals,
                };

                _ActiveSamplers.Add(this);
            }
            ~Sampler() { Dispose(); }

            public void Dispose()
            {
                if (_disposed) return;
                if (IsRunning) Complete();
                ResultPositions.Dispose();
                ResultNormals.Dispose();
                _ActiveSamplers.Remove(this);
                _disposed = true;
            }

            public void Schedule(NativeSlice<Vector3> points, float time, int precision)
            {
                if (_disposed) throw new Exception("Disposed");
                if (IsRunning) throw new Exception("Complete running operation first.");

                profile.GetSamplerData(out _job.waves, out _job.areas);

                _job.baseInfluences = new Vector3(
                    profile.globalAmplitudeMultiplier,
                    profile.globalSteepnessMultiplier,
                    profile.globalHillnessMultiplier
                    );
                _job.includeLocalAreas = profile.includeLocalAreas;

                _job.originPosition = profile.WorldOriginPosition;
                _job.originRotation = profile.WorldOriginRotation;

                _job.points = points;
                _job.time = time;
                _job.precision = precision;

                _scheduleCount = points.Length;
                _handle = _job.Schedule(_scheduleCount, 1);

                ResultCount = 0;
                IsRunning = true;
            }

            public void Complete()
            {
                if (_disposed) throw new Exception("Disposed");
                if (!IsRunning) throw new Exception("Schedule operation first.");

                _handle.Complete();

                ResultCount = _scheduleCount;
                IsRunning = false;
            }


            public static void CompleteAll()
            {
                for (int i = 0; i < _ActiveSamplers.Count; i++)
                {
                    if (_ActiveSamplers[i].IsRunning)
                        _ActiveSamplers[i].Complete();
                }
            }
        }

        [SerializeField] internal GerstnerWave[] waves = new GerstnerWave[0];
        [SerializeField] internal bool autoWaveSpeed = true;
        [SerializeField] internal float autoWaveSpeedGravity = 9.81f;

        [SerializeField] internal bool autoWaveHeight = true;
        [SerializeField] internal float autoWaveHeightRatio = 0.143f;


        [SerializeField] internal int textureSize = 512;
        [Tooltip("Projection size in world units.\nHalf of this value is radius of visibility distance.\nThe lower value, the more detailed waves.")]
        public float projectionSize = 512;


        [Tooltip("How much height-offset from water leves influences the hillness.")]
        [Range(0.0f, 0.5f)] public float hillnessOffsetFactor = 0.1f;
        [Tooltip("How much normal vector of wave influences the hillness.")]
        [Range(0.125f, 8.0f)] public float hillnessNormalPower = 1.0f;


        [Tooltip("Works like local wave area multiplier, but globally for this entire wave profile")]
        [SerializeField] public float globalAmplitudeMultiplier = 1.0f;
        [Tooltip("Works like local wave area multiplier, but globally for this entire wave profile")]
        [SerializeField] public float globalSteepnessMultiplier = 1.0f;
        [Tooltip("Works like local wave area multiplier, but globally for this entire wave profile")]
        [SerializeField] public float globalHillnessMultiplier = 1.0f;
        [Tooltip("Will waves from this profile be affected by local areas?")]
        [SerializeField] public bool includeLocalAreas = true;


        public float rippleMaxDepth = 3.0f;
        public float rippleTime = 0.5f;
        public float rippleRestoreTime = 15.0f;
        public float rippleBlurStep = 0.5f;
        [Range(0.0f,1.0f)] public float rippleBlurAttenuation = 0.3f;

        #region getters / setters
        public int WavesCount => waves.Length;
        public int TextureSize
        {
            get => textureSize;
            set
            {
                if (textureSize == value) return;
                if (!Mathf.IsPowerOfTwo(value)) throw new Exception("Must be power of two");
                textureSize = value;
                Uninitalize();
            }
        }
        #endregion

        public Vector2 WorldOriginPosition { get; set; } = Vector2.zero;
        public float WorldOriginRotation { get; set; } = 0.0f; // in radians

        public bool Initialized { get; private set; } = false;
        public RenderTexture TextureOffset { get; private set; } = null;
        public RenderTexture TextureNormal { get; private set; } = null;
        public RenderTexture TextureHeightOffset { get; private set; } = null;
        public bool IsProcessingHeighOffsetTexture => (Time.unscaledTime - _lastHeightOffsetRequest) < 1.0f;

        ComputeBuffer _waveBuffer;
        NativeArray<GerstnerWave> _nativeWaves;
        bool _waveBufferDirty = false;
        float _lastHeightOffsetRequest = 0.0f;


        public GerstnerWave GetWave(int index)
        {
            return waves[index];
        }
        public void SetWave(int index, GerstnerWave wave)
        {
            waves[index] = wave;
            _waveBufferDirty = true;
        }
        public void RemoveWave(int index)
        {
            var tmp = new List<GerstnerWave>(waves);
            tmp.RemoveAt(index);
            waves = tmp.ToArray();
            _waveBufferDirty = true;
        }
        public void AddWave(GerstnerWave wave)
        {
            var tmp = new List<GerstnerWave>(waves);
            tmp.Add(wave);
            waves = tmp.ToArray();
            _waveBufferDirty = true;
        }

        public void RequestHeightOffsetTextureProcessing()
        {
            _lastHeightOffsetRequest = Time.unscaledTime;
        }

        internal void Initialize()
        {
            if (Initialized)
            {
                var valid = true;

                if (TextureOffset == null)
                    valid = false;
                else if (TextureOffset.enableRandomWrite == false)
                    valid = false;

                if (TextureNormal == null)
                    valid = false;
                else if (TextureNormal.enableRandomWrite == false)
                    valid = false;

                if (TextureHeightOffset == null)
                    valid = false;
                else if (TextureHeightOffset.enableRandomWrite == false)
                    valid = false;

                if (!_waveBuffer?.IsValid() ?? true)
                    valid = false;

                if (!_nativeWaves.IsCreated)
                    valid = false;

                if (valid) return;
                Uninitalize();
            }

            TextureOffset = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBHalf);
            TextureOffset.enableRandomWrite = true;
            TextureOffset.Create();
            
            TextureNormal = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBHalf);
            TextureNormal.enableRandomWrite = true;
            TextureNormal.Create();

            var thoFormat = SystemInfo.SupportsRandomWriteOnRenderTextureFormat(RenderTextureFormat.RHalf)
               ? RenderTextureFormat.RHalf
               : RenderTextureFormat.RFloat;
            TextureHeightOffset = new RenderTexture(textureSize, textureSize, 0, thoFormat);
            TextureHeightOffset.enableRandomWrite = true;
            TextureHeightOffset.Create();

            _waveBuffer = new ComputeBuffer(MAX_WAVE_BUFFER_SIZE, GerstnerWave.SIZEOF, ComputeBufferType.Structured);
            _nativeWaves = new NativeArray<GerstnerWave>(MAX_WAVE_BUFFER_SIZE, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            Initialized = true;
            UpdateWaveBuffer();
        }
        internal void Uninitalize()
        {
            if (!Initialized) return;

            Sampler.CompleteAll();

            if (TextureOffset != null) UnityEx.SafeDestroy(TextureOffset);
            if (TextureNormal != null) UnityEx.SafeDestroy(TextureNormal);
            if (TextureHeightOffset != null) UnityEx.SafeDestroy(TextureHeightOffset);
            if (_waveBuffer != null) _waveBuffer.Release();
            if (_nativeWaves.IsCreated) _nativeWaves.Dispose();

            TextureOffset = null;
            TextureNormal = null;
            _waveBuffer = null;
            _nativeWaves = default;

            Initialized = false;
        }

        internal void UpdateWaveBuffer()
        {
            if (!Initialized) return;
            _waveBuffer.SetData(waves);
            NativeArray<GerstnerWave>.Copy(waves, _nativeWaves, WavesCount);
            _waveBufferDirty = false;
        }

        internal void DispatchWaveCompute(ComputeShader shader, int kernel, Camera camera)
        {
            if (_waveBufferDirty)
                UpdateWaveBuffer();

            if (IsProcessingHeighOffsetTexture)
            {
                shader.EnableKeyword(Constants.Shader.Keyword.ComputeCalculateHeightOffset);
                shader.SetTexture(kernel, Constants.Shader.Property.ComputeResultHeightOffset, TextureHeightOffset);
            }
            else
            {
                shader.DisableKeyword(Constants.Shader.Keyword.ComputeCalculateHeightOffset);
            }

            shader.SetTexture(kernel, Constants.Shader.Property.ComputeResultOffset, TextureOffset);
            shader.SetTexture(kernel, Constants.Shader.Property.ComputeResultNormal, TextureNormal);
            shader.SetBuffer(kernel, Constants.Shader.Property.ComputeWaves, _waveBuffer);
            shader.SetInt(Constants.Shader.Property.ComputeWaveCount, WavesCount);
            shader.SetInt(Constants.Shader.Property.ComputeTextureSize, textureSize);
            shader.SetVector(Constants.Shader.Property.ComputeCameraOffset, GetCameraOffset(camera.transform.position));
            shader.SetVector(Constants.Shader.Property.ComputeWorldOriginPosition, WorldOriginPosition);
            shader.SetFloat(Constants.Shader.Property.ComputeWorldOriginRotation, WorldOriginRotation);
            shader.SetFloat(Constants.Shader.Property.ComputeProjectionSize, GetSnappedProjectionSize());
            shader.SetVector(Constants.Shader.Property.ComputeBaseInfluences, new Vector3(globalAmplitudeMultiplier, globalSteepnessMultiplier, globalHillnessMultiplier));
            shader.SetInt(Constants.Shader.Property.ComputeUseLocalArea, includeLocalAreas ? 1 : 0);
            shader.SetFloat(Constants.Shader.Property.ComputeHillnessOffsetFactor, hillnessOffsetFactor);
            shader.SetFloat(Constants.Shader.Property.ComputeHillnessNormalPower, hillnessNormalPower);
            
            shader.Dispatch(kernel, textureSize/8, textureSize/8, 1);
        }


        internal void ConfigureRippler(ComputeShader shader, int kernel, bool isFlat)
        {
            if (isFlat)
            {
                shader.SetKeywordEnum(Constants.Shader.Keyword.ComputePrecision, 2);
            }
            else
            {
                if (IsProcessingHeighOffsetTexture)
                {
                    shader.SetKeywordEnum(Constants.Shader.Keyword.ComputePrecision, 0);
                    shader.SetTexture(kernel, Constants.Shader.Property.ComputeWaveHeightOffsetTex, TextureHeightOffset);
                }
                else
                {
                    shader.SetKeywordEnum(Constants.Shader.Keyword.ComputePrecision, 1);
                    shader.SetTexture(kernel, Constants.Shader.Property.ComputeWaveOffsetTex, TextureOffset);
                }
            }
            shader.SetFloat(Constants.Shader.Property.ComputeWaveProjectionSize, GetSnappedProjectionSize());
            shader.SetFloat(Constants.Shader.Property.CumputeMaxDepth, rippleMaxDepth);
            shader.SetFloat(Constants.Shader.Property.CumputeRippleTime, rippleTime);
            shader.SetFloat(Constants.Shader.Property.CumputeRestoreTime, rippleRestoreTime);
            shader.SetFloat(Constants.Shader.Property.CumputeBlurStep, rippleBlurStep);
            shader.SetFloat(Constants.Shader.Property.CumputeBlurAttenuation, rippleBlurAttenuation);
        }


        internal void ConfigureMaterial(Material material, Camera camera)
        {
            //material.EnableKeyword(Constants.Shader.Keyword.UseWaveTexture);

            var realProjectionSize = GetSnappedProjectionSize();

            var worldOffset = new Vector4(
                WorldOriginPosition.x,
                WorldOriginPosition.y,
                WorldOriginRotation,
                0
                );

            var offset = GetCameraOffset(camera.transform.position);
            material.SetVector(Constants.Shader.Property.WorldOriginOffset, worldOffset);
            material.SetTexture(Constants.Shader.Property.WaveTextureOffset, TextureOffset);
            material.SetTexture(Constants.Shader.Property.WaveTextureNormal, TextureNormal);
            material.SetVector(Constants.Shader.Property.WaveTextureProjection,
                new Vector4(offset.x - realProjectionSize / 2, offset.y - realProjectionSize / 2, realProjectionSize, realProjectionSize)
                );
        }
        internal static void CleanupMaterial(Material material)
        {
            //material.DisableKeyword(Constants.Shader.Keyword.UseWaveTexture);
        }


        private Vector2 GetCameraOffset(Vector3 position)
        {
            var units = Mathf.Max(GetSnappedProjectionSize() / textureSize);
            return new Vector2(
                position.x - Mathf.Sign(position.x) * Mathf.Repeat(Mathf.Abs(position.x), units),
                position.z - Mathf.Sign(position.z) * Mathf.Repeat(Mathf.Abs(position.z), units)
                );
        }
        private float GetSnappedProjectionSize()
        {
            return Mathf.Ceil(projectionSize / PROJECTION_SIZE_SNAP) * PROJECTION_SIZE_SNAP;
        }


        private void OnEnable()
        {
            Uninitalize();
        }
        private void OnDisable()
        {
            Uninitalize();
        }
        private void OnDestroy()
        {
            Uninitalize();
        }


        public Sampler CreateSampler(int maxSize = 1)
        {
            var sampler = new Sampler(this, maxSize);
            return sampler;
        }
        internal void GetSamplerData(out NativeSlice<GerstnerWave> waves, out NativeSlice<LocalWaveAreaEntry> areas)
        {
            if (!Initialized) Initialize();
            waves = _nativeWaves.Slice(0, WavesCount);
            DragonWaterManager.Instance.GetSamplerData(out areas);
        }
    }
}
