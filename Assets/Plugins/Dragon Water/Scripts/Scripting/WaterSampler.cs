using DragonWater.Utils;
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace DragonWater.Scripting
{
    public class WaterSampler : IDisposable
    {
        static List<WaterSampler> _ActiveSamplers = new();
        public static IReadOnlyList<WaterSampler> ActiveSamplers => _ActiveSamplers;

        [Serializable]
        public enum SurfaceDetectionMode
        {
            Default,
            AutoCull,
            Custom
        }

        [Serializable]
        public enum CutoutDetectionMode
        {
            AutoCull,
            DontCutout,
            Custom
        }
        public struct HitResult
        {
            public Vector3 sampledPoint;

            public WaterSurface surface;
            public Vector3 hitPoint;
            public Vector3 hitNormal;

            public bool HasHit => surface != null;
            public bool IsUnderwater => sampledPoint.y < hitPoint.y;
            public float Depth => hitPoint.y - sampledPoint.y;
            public float Height => sampledPoint.y - hitPoint.y;
            public float WaterLevel => hitPoint.y;
        }

        private class ScheduleData : IDisposable
        {
            public int count;
            public NativeArray<Vector3> points;
            public Bounds bounds;
            public float time;
            public int precision;
            public bool ignoreCutout;

            public List<WaterSurface> surfaces = new();
            public List<Collider> colliders = new();
            public Dictionary<WaveProfile, WaveProfile.Sampler> waveSamplers = new();

            public ScheduleData(int maxSize = 1)
            {
                points = new(maxSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }
            public void Dispose()
            {
                points.Dispose();
                foreach (var kv in waveSamplers)
                    kv.Value.Dispose();
            }

            public void Resize(int newMaxSize)
            {
                points.Dispose();
                points = new(newMaxSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            public void Schedule()
            {
                var slice = points.Slice(0, count);

                WaveProfile.Sampler sampler;
                for (int i=0; i<surfaces.Count;i++)
                {
                    if (surfaces[i].waveProfile == null) continue;

                    if (!waveSamplers.TryGetValue(surfaces[i].waveProfile, out sampler))
                    {
                        sampler = surfaces[i].waveProfile.CreateSampler(points.Length);
                        waveSamplers.Add(surfaces[i].waveProfile, sampler);
                    }

                    if (!sampler.IsRunning)
                    {
                        sampler.Schedule(slice, time, precision);
                    }
                }
            }

            public void Complete()
            {
                foreach (var kv in waveSamplers)
                    if (kv.Value.IsRunning)
                        kv.Value.Complete();
            }

            public void FillResults(HitResult[] array)
            {
                Vector3 point;
                WaterSurface surface;
                bool insideCollider;
                WaveProfile.Sampler sampler;

                for (int i = 0; i < count; i++)
                {
                    point = points[i];
                    surface = null;
                    insideCollider = false;

                    if (!ignoreCutout)
                    {
                        for (int j = 0; j < colliders.Count; j++)
                        {
                            if (colliders[j] == null) continue; // safety check
                            if (colliders[j].ClosestPoint(point) == point)
                            {
                                insideCollider = true;
                                break;
                            }
                        }
                    }
                    for (int j = 0; j < surfaces.Count; j++)
                    {
                        if (surfaces[j] == null) continue; // safety check
                        if (!surfaces[j].ContainsPoint(point))
                            continue;

                        surface = surfaces[j];
                        if (surface.cutoutWaterVolume && !ignoreCutout)
                        {
                            if (insideCollider == !surface.cutoutReverse)
                            {
                                surface = null;
                                continue;
                            }
                        }

                        break;
                    }

                    array[i].sampledPoint = point;
                    array[i].surface = surface;

                    if (surface != null)
                    {
                        if (surface.waveProfile == null)
                        {
                            array[i].hitPoint = new Vector3(point.x, surface.transform.position.y, point.y);
                            array[i].hitNormal = Vector3.up;
                        }
                        else
                        {
                            sampler = waveSamplers[surface.waveProfile];
                            array[i].hitPoint = sampler.ResultPositions[i] + new Vector3(0, surface.transform.position.y, 0);
                            array[i].hitNormal = sampler.ResultNormals[i];
                        }
                    }
                    else
                    {
                        array[i].hitPoint = point;
                        array[i].hitNormal = Vector3.up;
                    }
                }
            }
        }

        public int Precision = 0;
        public SurfaceDetectionMode SurfaceDetection = SurfaceDetectionMode.Default;
        public CutoutDetectionMode CutoutDetection = CutoutDetectionMode.AutoCull;
        public List<WaterSurface> SurfaceDetectionCustom = null;
        public List<WaterCutoutVolume> CutoutDetectionCustom = null;
        public Bounds CullingBox = default;

        public int MaxSize { get; private set; } = 0;
        public bool IsRunning { get; private set; } = false;
        public HitResult[] Results { get; private set; }
        public int ResultCount { get; private set; }

        Vector3[] _points;
        ScheduleData _data;
        bool _disposed;
        bool _surfaceCached = false;
        bool _cutoutCached = false;

        public WaterSampler(int maxSize = 1)
        {
            Results = new HitResult[maxSize];
            _points = new Vector3[maxSize];
            _data = new(maxSize);
            MaxSize = maxSize;
            _ActiveSamplers.Add(this);
        }
        ~WaterSampler() { Dispose(); }
        public void Dispose()
        {
            if (_disposed) return;
            if (IsRunning) _data.Complete();
            _data.Dispose();
            _ActiveSamplers.Remove(this);
            _disposed = true;
        }

        public void Resize(int newMaxSize)
        {
            if (IsRunning) _data.Complete();

            var newResults = new HitResult[newMaxSize];
            var newPoints = new Vector3[newMaxSize];

            Array.Copy(Results, newResults, Mathf.Min(MaxSize, newMaxSize));
            Array.Copy(_points, newPoints, Mathf.Min(MaxSize, newMaxSize));

            _data.Resize(newMaxSize);

            Results = newResults;
            _points = newPoints;
            MaxSize = newMaxSize;
        }

        public void SetPoint(int index, Vector3 point)
        {
            _points[index] = point;
        }
        public void SetPoints(IReadOnlyList<Vector3> points)
        {
            for (int i = 0; i < points.Count; i++)
                this._points[i++] = points[0];
        }
        public void SetPoints(ArraySegment<Vector3> points)
        {
            Array.Copy(points.Array, points.Offset, this._points, 0, points.Count);
        }

        public void Schedule() => Schedule(MaxSize);
        public void Schedule(int pointsCount)
        {
            if (_disposed) throw new Exception("Disposed");
            if (IsRunning) throw new Exception("Complete running operation first.");

            _data.count = pointsCount;
            NativeArray<Vector3>.Copy(_points, _data.points, pointsCount);

            _data.bounds = default;
            _data.time = DragonWaterManager.Instance.Time;
            _data.precision = Precision > 0 ? Precision : Constants.DefaultSamplerPrecision;

            if (SurfaceDetection == SurfaceDetectionMode.Custom)
            {
                if (SurfaceDetectionCustom == null)
                    throw new Exception("SurfaceDetection detection is Custom, but list is not set");

                _data.surfaces.Clear();
                _data.surfaces.AddRange(SurfaceDetectionCustom);
            }
            else if (SurfaceDetection == SurfaceDetectionMode.Default && DragonWaterManager.Instance.DefaultSurface != null)
            {
                _data.surfaces.Clear();
                _data.surfaces.Add(DragonWaterManager.Instance.DefaultSurface);
            }
            else
            {
                if (!_surfaceCached)
                {
                    _data.surfaces.Clear();
                    EnsureBounds(pointsCount);
                    DragonWaterManager.Instance.CullWaterSurfaces(ref _data.bounds, ref _data.surfaces);
                }
            }

            var hasCutout = false;
            for (int i = 0; i < _data.surfaces.Count; i++)
            {
                if (_data.surfaces[i].cutoutWaterVolume)
                {
                    hasCutout = true;
                    break;
                }
            }

            if (hasCutout)
            {
                if (CutoutDetection == CutoutDetectionMode.Custom)
                {
                    if (CutoutDetectionCustom == null)
                        throw new Exception("CutoutDetection detection is Custom, but list is not set");

                    _data.colliders.Clear();
                    for (int i = 0; i < CutoutDetectionCustom.Count; i++)
                    {
                        var collider = CutoutDetectionCustom[i]._collider;
                        if (collider) _data.colliders.Add(collider);
                    }
                    _data.ignoreCutout = false;
                }
                else if (CutoutDetection == CutoutDetectionMode.AutoCull)
                {
                    if (!_cutoutCached)
                    {
                        _data.colliders.Clear();
                        EnsureBounds(pointsCount);
                        DragonWaterManager.Instance.CullCutoutColliders(ref _data.bounds, ref _data.colliders);
                        _data.ignoreCutout = false;
                    }
                }
                else
                {
                    _data.colliders.Clear();
                    _data.ignoreCutout = true;
                }
            }

            _data.Schedule();
            ResultCount = 0;
            IsRunning = true;
        }
        private void EnsureBounds(int pointsCount)
        {
            if (_data.bounds != default) return;
            if (CullingBox == default)
            {
                _data.bounds = _points.MakeBounds(pointsCount);
            }
            else
            {
                _data.bounds = CullingBox;
            }
        }

        public void Complete()
        {
            if (_disposed) throw new Exception("Disposed");
            if (!IsRunning) throw new Exception("Schedule operation first.");

            _data.Complete();
            _data.FillResults(Results);

            ResultCount = _data.count;
            IsRunning = false;
        }

        public void CacheAutoCull() => CacheAutoCull(MaxSize);
        public void CacheAutoCull(int points)
        {
            if (SurfaceDetection == SurfaceDetectionMode.AutoCull)
            {
                _data.surfaces.Clear();
                EnsureBounds(points);
                DragonWaterManager.Instance.CullWaterSurfaces(ref _data.bounds, ref _data.surfaces);
                _surfaceCached = true;
            }
            if (CutoutDetection == CutoutDetectionMode.AutoCull)
            {
                _data.colliders.Clear();
                EnsureBounds(points);
                DragonWaterManager.Instance.CullCutoutColliders(ref _data.bounds, ref _data.colliders);
                _cutoutCached = true;
            }
        }
        public void ClearAutoCullCache()
        {
            _surfaceCached = false;
            _cutoutCached = false;
        }

        public void ClearState()
        {
            if (_disposed) throw new Exception("Disposed");
            if (IsRunning) _data.Complete();
            ResultCount = 0;
            ClearAutoCullCache();
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
}
