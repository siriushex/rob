using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonWater.Scripting
{
    public abstract class WaterBehaviour : MonoBehaviour
    {
        static int FrameDividerOffsetAccumulator = 0;

        [Serializable]
        public class WaterSamplerConfig
        {
            [Tooltip("Amount of iterations used to sample water height")]
            [Range(1, 6)] public int precision = 3;
            [Tooltip("Water will be sampled every N frame. If you have many objects with e.g. N=2, then they will automatically split load between odd and even frames.")]
            [Range(1, 6)] public int frameDivider = 1;

            [Tooltip("How water surfaces to sample will be acquired")]
            public WaterSampler.SurfaceDetectionMode surfaceDetection = WaterSampler.SurfaceDetectionMode.Default;
            public List<WaterSurface> surfaces = new();

            [Tooltip("How cutout volumes used to exclude from surface sampler will be acquired")]
            public WaterSampler.CutoutDetectionMode cutoutDetection = WaterSampler.CutoutDetectionMode.AutoCull;
            public List<WaterCutoutVolume> cutouts = new();

            [Tooltip("If enabled, bounding box used to detect nearby volumes/surfaces will be calcualted automaticaly from points.")]
            public bool autoCullingBox = true;
            public Vector3 cullingBoxSize = new Vector3(50,30,50);
            [Min(0)] public float cullingRefreshTime = 0.2f;
        }

        [SerializeField] internal WaterSamplerConfig samplerConfig;

        public WaterSamplerConfig SamplerConfig => samplerConfig;
        public int FrameDividerOffset { get; private set; } = 0;

        protected bool isSchedulingFrane { get; private set; } = false;
        protected Vector3[] samplingPoints { get; private set; } = new Vector3[0];

        WaterSampler _sampler = null;
        int _samplingCount = 0;
        float _nextCacheTime = 0f;
        protected virtual void Awake()
        {
            _sampler = new(0);
            FrameDividerOffset = FrameDividerOffsetAccumulator++;
        }
        protected virtual void OnEnable()
        {
            DragonWaterManager.Instance.onFixedUpdate += OnFixedUpdate;
            _sampler.ClearState();
            _nextCacheTime = Time.time - UnityEngine.Random.Range(0, samplerConfig.cullingRefreshTime);
        }
        protected virtual void OnDisable()
        {
            var manager = DragonWaterManager.InstanceUnsafe;
            if (manager) manager.onFixedUpdate -= OnFixedUpdate;
        }
        protected virtual void OnDestroy()
        {
            _sampler?.Dispose();
        }


        protected virtual void OnFixedUpdate()
        {
            if (_sampler != null && _sampler.IsRunning)
                _sampler.Complete();

            isSchedulingFrane = (DragonWaterManager.Instance.fixedUpdateCount + FrameDividerOffset) % samplerConfig.frameDivider == 0;

            InnerFixedUpdate();

            if (isSchedulingFrane && _samplingCount > 0)
            {
                if (_sampler.MaxSize < _samplingCount)
                {
                    _sampler.Resize(_samplingCount);
                }

                _sampler.Precision = samplerConfig.precision;
                _sampler.SurfaceDetection = samplerConfig.surfaceDetection;
                if (samplerConfig.surfaceDetection == WaterSampler.SurfaceDetectionMode.Custom)
                    _sampler.SurfaceDetectionCustom = samplerConfig.surfaces;
                _sampler.CutoutDetection = samplerConfig.cutoutDetection;
                if (samplerConfig.cutoutDetection == WaterSampler.CutoutDetectionMode.Custom)
                    _sampler.CutoutDetectionCustom = samplerConfig.cutouts;

                if (Time.time > _nextCacheTime)
                {
                    if (samplerConfig.autoCullingBox)
                        _sampler.CullingBox = default;
                    else
                        _sampler.CullingBox = new Bounds(transform.position, samplerConfig.cullingBoxSize);

                    _sampler.CacheAutoCull();
                    _nextCacheTime += samplerConfig.cullingRefreshTime;
                }

                _sampler.SetPoints(new(samplingPoints, 0, _samplingCount));
                _sampler.Schedule(_samplingCount);
            }
        }

        public void InitializeWaterBehaviour()
        {
            if (samplerConfig == null)
                samplerConfig = new WaterSamplerConfig(); // ← важное добавление

            if (_sampler == null)
            {
                _sampler = new WaterSampler(0);
                FrameDividerOffset = FrameDividerOffsetAccumulator++;
            }
        }

        protected void PrepareSamplingPointsSize(int size)
        {
            if (samplingPoints.Length < size)
            {
                var newPoints = new Vector3[size];
                Array.Copy(samplingPoints, newPoints, samplingPoints.Length);
                samplingPoints = newPoints;
            }
            _samplingCount = size;
        }
        protected void SetSamplingPoint(int index, Vector3 point)
        {
            samplingPoints[index] = point;
        }
        protected void SetSamplingPoints(Vector3[] points)
        {
            samplingPoints = new Vector3[points.Length];
            _samplingCount = points.Length;
        }

        protected int GetResultCount()
        {
            return _sampler.ResultCount;
        }
        protected WaterSampler.HitResult GetResult(int index)
        {
            return _sampler.Results[index];
        }
        protected void GetResults(out ArraySegment<WaterSampler.HitResult> results)
        {
            results = new(_sampler.Results, 0, _sampler.ResultCount);
        }


        protected virtual void InnerFixedUpdate() { }


#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (samplerConfig.surfaceDetection == WaterSampler.SurfaceDetectionMode.AutoCull
                || samplerConfig.cutoutDetection == WaterSampler.CutoutDetectionMode.AutoCull)
            {
                if (!samplerConfig.autoCullingBox)
                {
                    Gizmos.color = new Color(1.0f, 0.0f, 1.0f, 0.2f);
                    Gizmos.DrawCube(transform.position, samplerConfig.cullingBoxSize);
                }
            }
        }
#endif
    }
}
