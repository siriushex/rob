using DragonWater.Utils;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Rendering;

namespace DragonWater
{
    [ExecuteAlways]
    public class DragonUnderwaterRenderer : MonoBehaviour
    {
        public const int RayBlockSize = 120;
        public const int RayBlockCapacity = 96;

        public bool ShowSuperfarPlane { get; set; } = false;
        public bool ShowRays { get; set; } = false;

        public float RayVisibilityDistance { get; set; } = 120;
        public float RayLength { get; set; } = 50.0f;
        public Vector2 RayWidth { get; set; } = new Vector2(3.0f, 10.0f);
        public float RayDensityFactor { get; set; } = 1.0f;



        public bool ShowCaustics { get; set; } = false; // screen-space caustics
        public float CausticsWaterLevelOffset { get; set; } = 0.0f;

        public Material RayMaterial { get; private set; } = null;
        public Material CausticsMaterial { get; private set; } = null;
        public Volume OverrideVolume { get; private set; } = null;

        private class RayBlock
        {
            public Vector2Int index;
            public bool dirty;
            public MeshRenderer[] renderers;
            public TransformAccessArray transforms;

            public int ActiveCount { get; private set; } = -1;
            public void SetActive(int count)
            {
                if (ActiveCount == count)
                    return;

                for (int i = 0; i < renderers.Length; i++)
                    renderers[i].enabled = i < count;
                ActiveCount = count;
            }
        }
        private struct RayBlockJob : IJobParallelForTransform
        {
            [ReadOnly] public NativeArray<Vector3> randoms;

            public Vector3 cameraPosition;
            public Vector3 lightDirection;
            public float waterLevel;
            public Vector2Int index;
            public float size;
            public float rayLength;
            public Vector2 rayWidth;
            public int activeCount;

            public void Execute(int i, TransformAccess transform)
            {
                if (i >= activeCount)
                    return;

                var position = new Vector3(
                    index.x * size + randoms[i].x * size,
                    0,
                    index.y * size + randoms[i].y * size
                    );

                var cameraLevelPosition = position + lightDirection * (cameraPosition.y / lightDirection.y);
                var billboardDirection = (cameraLevelPosition - cameraPosition).normalized;

                var lightForwardFlat = new Vector3(lightDirection.x, 0.0f, lightDirection.z).normalized;
                var lightTopAngle = Vector3.Angle(lightDirection, Vector3.down);
                var lightAngle = Vector3.SignedAngle(lightForwardFlat, Vector3.back, Vector3.up);

                var rotation =
                     Quaternion.LookRotation(lightForwardFlat)
                     * Quaternion.AngleAxis(lightTopAngle, Vector3.left)
                     * Quaternion.LookRotation(billboardDirection)
                     * Quaternion.AngleAxis(lightAngle, Vector3.up)
                    ;

                var scale = new Vector3(Mathf.Lerp(rayWidth.x, rayWidth.y, randoms[i].z), rayLength, 1.0f);

                transform.SetLocalPositionAndRotation(
                    position - (lightDirection * (waterLevel + 5.0f)), // make it axtra above water surface
                    rotation
                    );
                transform.localScale = scale + Vector3.up * 5.0f;
            }
        }


        GameObject _superfarPlane;
        List<RayBlock> _activeBlocks = new();
        List<RayBlock> _inactiveBlocks = new();

        NativeArray<Vector3> _rayRandoms;
        NativeArray<JobHandle> _rayJobs;

        bool _initialized = false;
        private void EnsureInitialzed()
        {
            if (_initialized) return;

            gameObject.GetAllChildren().ForEach(UnityEx.SafeDestroy);

            _superfarPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _superfarPlane.transform.localScale = Vector3.one * 100000;
            _superfarPlane.transform.parent = transform;
            _superfarPlane.GetComponent<Collider>().enabled = false;

            _rayRandoms = new NativeArray<Vector3>(RayBlockCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < RayBlockCapacity; i++)
                _rayRandoms[i] = new Vector3(
                    UnityEngine.Random.Range(-0.5f, 0.5f),
                    UnityEngine.Random.Range(-0.5f, 0.5f),
                    UnityEngine.Random.Range(0.0f, 1.0f)
                    );

            _rayJobs = new NativeArray<JobHandle>(64, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            RayMaterial = new Material(Shader.Find(Constants.Shader.UnderwaterRayShaderName));
            CausticsMaterial = new Material(Shader.Find(Constants.Shader.UnderwaterCausticsShaderName));

            {
                var go = new GameObject();
                var volume = go.AddComponent<Volume>();
                go.layer = DragonWaterManager.Instance.Config.underwaterVolumeLayer;
                volume.priority = DragonWaterManager.Instance.Config.UnderwaterVolumePriority;

#if UNITY_EDITOR
                UnityEditor.SceneVisibilityManager.instance.DisablePicking(go, true);
#endif

                go.transform.parent = transform;
                OverrideVolume = volume;
                OverrideVolume.enabled = false;
            }

            _initialized = true;
        }
        private void Awake()
        {
            _activeBlocks = new();
            _inactiveBlocks = new();

            EnsureInitialzed();
        }
        private void OnDestroy()
        {
            for (int i = 0; i < _activeBlocks.Count; i++)
            {
                _rayJobs[i].Complete();
            }

            UnityEx.SafeDestroy(_superfarPlane);
            _rayRandoms.Dispose();
            _rayJobs.Dispose();
            UnityEx.SafeDestroy(RayMaterial);

            _activeBlocks.ForEach(b => { b.transforms.Dispose(); b.renderers.ToList().ForEach(r => UnityEx.SafeDestroyGameObject(r)); });
            _inactiveBlocks.ForEach(b => { b.transforms.Dispose(); b.renderers.ToList().ForEach(r => UnityEx.SafeDestroyGameObject(r)); });
        }


        internal void UpdateUnderwater(Camera camera)
        {
#if UNITY_EDITOR
            EnsureInitialzed();
#endif

            if (ShowSuperfarPlane)
            {
                _superfarPlane.SetActive(true);

                var distance = camera.farClipPlane * 0.9f;
#if UNITY_EDITOR
                if (camera.cameraType == CameraType.SceneView)
                {
                    distance = Mathf.Min(distance, 10000);
                }
#endif
                _superfarPlane.transform.position = camera.transform.position + camera.transform.forward * distance;
                _superfarPlane.transform.up = -camera.transform.forward; ;
            }
            else
            {
                _superfarPlane.SetActive(false);
            }

            UpdateRays(camera);
            UpdateCaustics(camera);

#if UNITY_EDITOR
            OverrideVolume.gameObject.layer = DragonWaterManager.Instance.Config.underwaterVolumeLayer;
            OverrideVolume.priority = DragonWaterManager.Instance.Config.UnderwaterVolumePriority;
#endif
        }


        List<Vector2Int> _visibleIndices = new();
        List<Vector2Int> _invisibleIndices = new();
        private void UpdateRays(Camera camera)
        {
            for (int i = 0; i < _activeBlocks.Count; i++)
            {
                _rayJobs[i].Complete();
            }

            if (!ShowRays)
            {
                for (int i = 0; i < _activeBlocks.Count; i++)
                {
                    PushBlock(_activeBlocks[i]);
                    i--;
                }

                return;
            }

            var lightDirection = Vector3.down;
            var mainLight = Light.GetLights(LightType.Directional, 0).FirstOrDefault();
            if (mainLight)
            {
                lightDirection = mainLight.transform.forward;
            }

            var transform = camera.transform;

            var position = new Vector2(transform.position.x, transform.position.z);
            var direction = new Vector2(transform.forward.x, transform.forward.z).normalized;
            var fov = camera.orthographic ? 120.0f : camera.fieldOfView * 1.0f;

            var baseIndex = new Vector2Int(
                Mathf.RoundToInt(position.x / RayBlockSize),
                Mathf.RoundToInt(position.y / RayBlockSize)
                );
            _visibleIndices.Clear();
            _invisibleIndices.Clear();
            CullIndices(baseIndex, ref position, ref direction, ref fov);

            var activeCount = Mathf.RoundToInt(RayBlockCapacity * RayDensityFactor);
            var waterLevel = DragonWaterManager.Instance.UnderwaterSurface.transform.position.y;
            DragonWaterManager.Instance.UnderwaterRenderer.RayMaterial.SetFloat(Constants.Shader.Property.WaterLevel, waterLevel);

            _activeBlocks.ForEach(b => b.dirty = true);
            for (int i = 0; i < _visibleIndices.Count; i++)
            {
                PopBlock(_visibleIndices[i], activeCount);
            }
            for (int i = 0; i < _activeBlocks.Count; i++)
            {
                if (_activeBlocks[i].dirty)
                {
                    PushBlock(_activeBlocks[i]);
                    i--;
                }
                else
                {
                    ScheduleBlock(_activeBlocks[i], i, transform.position, lightDirection, waterLevel);
                }
            }
            JobHandle.ScheduleBatchedJobs();
            //JobHandle.CombineDependencies(_rayJobs.Slice(0, _activeBlocks.Count)).Complete();
        }
        private void UpdateCaustics(Camera camera)
        {
            if (!ShowCaustics)
            {
                return;
            }

            var mainLight = Light.GetLights(LightType.Directional, 0).FirstOrDefault();
            if (mainLight)
            {
                CausticsMaterial.SetMatrix(Constants.Shader.Property.LightDirectionMatrix, mainLight.transform.localToWorldMatrix);
            }

            var waterLevel = DragonWaterManager.Instance.UnderwaterSurface.transform.position.y;
            CausticsMaterial.SetFloat(Constants.Shader.Property.WaterLevel, waterLevel + CausticsWaterLevelOffset);
        }

        private void CullIndices(Vector2Int index, ref Vector2 position, ref Vector2 direction, ref float fov)
        {
            if (_visibleIndices.Contains(index)) return;
            if (_invisibleIndices.Contains(index)) return;

            var center = (Vector2)index * RayBlockSize;
            var distance = Vector2.Distance(position, center);
            var actualMaxDistance = RayVisibilityDistance + RayBlockSize;
            //var angle = Vector2.Angle(direction, (center - position).normalized);

            if (_visibleIndices.Count > 0 &&
                distance > actualMaxDistance)
            {
                _invisibleIndices.Add(index);
                return;
            }

            _visibleIndices.Add(index);
            CullIndices(index + new Vector2Int(0, 1), ref position, ref direction, ref fov);
            CullIndices(index + new Vector2Int(0, -1), ref position, ref direction, ref fov);
            CullIndices(index + new Vector2Int(1, 0), ref position, ref direction, ref fov);
            CullIndices(index + new Vector2Int(-1, 0), ref position, ref direction, ref fov);
        }
        private RayBlock PopBlock(Vector2Int index, int activeCount)
        {
            var free = _activeBlocks.Find(b => b.dirty);
            if (free != null)
            {
                free.SetActive(activeCount);
                free.dirty = false;
                free.index = index;
                return free;
            }

            if (_inactiveBlocks.Count > 0)
            {
                free = _inactiveBlocks[0];
                _inactiveBlocks.RemoveAt(0);
                _activeBlocks.Add(free);
                free.SetActive(activeCount);
                free.dirty = false;
                free.index = index;
                return free;
            }

            free = new()
            {
                index = index,
                dirty = false,
                renderers = new MeshRenderer[RayBlockCapacity],
                transforms = new TransformAccessArray(RayBlockCapacity)
            };

            for (int i=0; i< RayBlockCapacity; i++)
            {
                var go = new GameObject();
                var filter = go.AddComponent<MeshFilter>();
                var renderer = go.AddComponent<MeshRenderer>();

#if UNITY_EDITOR
                UnityEditor.SceneVisibilityManager.instance.DisablePicking(go, true);
#endif

                go.transform.parent = transform;

                filter.sharedMesh = MeshUtility.GetRayPlane();
                renderer.sharedMaterial = RayMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                go.hideFlags = HideFlags.HideAndDontSave;
                free.renderers[i] = renderer;
                free.transforms.Add(go.transform);
            }

            free.SetActive(activeCount);
            _activeBlocks.Add(free);
            return free;
        }
        private void PushBlock(RayBlock block)
        {
            block.SetActive(0);
            _activeBlocks.Remove(block);
            _inactiveBlocks.Add(block);
        }
        private void ScheduleBlock(RayBlock block, int jobIndex, Vector3 cameraPosition, Vector3 lightDirection, float waterLevel)
        {
            var job = new RayBlockJob()
            {
                randoms = _rayRandoms,
                cameraPosition = cameraPosition,
                lightDirection = lightDirection,
                waterLevel = waterLevel,
                index = block.index,
                size = RayBlockSize,
                rayLength = RayLength,
                rayWidth = RayWidth,
                activeCount = block.ActiveCount
            };
            _rayJobs[jobIndex] = job.Schedule(block.transforms);
        }
    }
}
