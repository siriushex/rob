using DragonWater.Scripting;
using DragonWater.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace DragonWater
{
    [AddComponentMenu("Dragon Water/Water Surface")]
    [ExecuteAlways]
    public class WaterSurface : MonoBehaviour
    {
        [Serializable]
        public enum GeometryMeshType
        {
            Rectangle,
            Circle,
            InfiniteOcean,
            CustomMesh
        }

        [Serializable]
        public enum OceanAttachTarget
        {
            MainCamera,
            CustomObject
        }

        [Serializable]
        public enum CutoutCullingMode
        {
            None,
            Front,
            Perfect
        }

        [Serializable]
        public struct RectangleMeshConfig
        {
            public float width;
            public float height;
            public int subdivisions;

            public void SafeClamp()
            {
                width = Mathf.Clamp(width, 0, 10000);
                height = Mathf.Clamp(height, 0, 10000);
                subdivisions = Mathf.Clamp(subdivisions, 0, 1024);
            }

            public static readonly RectangleMeshConfig Default = new()
            {
                width = 20,
                height = 20,
                subdivisions = 10
            };
        }
        [Serializable]
        public struct CircleMeshConfig
        {
            public float radius;
            public int segments;
            public int rings;

            public void SafeClamp()
            {
                radius = Mathf.Clamp(radius, 0, 5000);
                segments = Mathf.Clamp(segments, 3, 1024);
                rings = Mathf.Clamp(rings, 0, 1024);
            }

            public static readonly CircleMeshConfig Default = new()
            {
                radius = 20,
                segments = 64,
                rings = 12
            };
        }

        [SerializeField] internal GeometryMeshType geometryType;
        [Tooltip("Rectangle mesh parameters.\nEnable wireframe mode to visualise it.")]
        [SerializeField] internal RectangleMeshConfig geometryRectangleConfig = RectangleMeshConfig.Default;
        [Tooltip("Circle mesh parameters.\nEnable wireframe mode to visualise it.")]
        [SerializeField] internal CircleMeshConfig geometryCircleConfig = CircleMeshConfig.Default;
        [Tooltip("You can edit presets in Dragon Water Manager.\nEnable wireframe mode to visualise it.")]
        [SerializeField] internal string geometryInfiniteOceanPreset = "";
        [Tooltip("Infinite Ocean geometry should be attached to camera or custom object?")]
        [SerializeField] internal OceanAttachTarget geometryInfiniteOceanAttachTo = OceanAttachTarget.MainCamera;
        [SerializeField] internal Transform geometryInfiniteOceanAttachCustomTarget = null;
        [Tooltip("Custom mesh asset. It should be flat mesh.\nEnable wireframe mode to visualise it.")]
        [SerializeField] internal Mesh geometryCustomMesh = null;

        [Tooltip("This is wave configuration.\nTry to use as less profiles as possible - their calculation result is shared between all surfaces using it")]
        [SerializeField] internal WaveProfile waveProfile = null;
        [Tooltip("This is water surface material configuration.")]
        [SerializeField] internal MaterialProfile materialProfile = null;
        [Tooltip("It tells how to render underwater effect.")]
        [SerializeField] internal UnderwaterProfile underwaterProfile = null;
        [Tooltip("If checked, wave world origin will be synchronized with this water surface.")]
        [SerializeField] internal bool synchronizeWorldOrigin = false;

        [Tooltip("If enabled, certain areas of water can be culled out by Cutout Volumes.\nIt cutouts both - rendering and physics processing.\nDisable if you do not use it.")]
        [SerializeField] internal bool cutoutWaterVolume = false;
        [Tooltip("Reverse the cutout - now only water inside cutout volume will be visible.")]
        [SerializeField] internal bool cutoutReverse = false;
        [Tooltip("How to cutout - see documentation for details.\nNOTE: It only affects visuals, not physics.")]
        [SerializeField] internal CutoutCullingMode cutoutCulling = CutoutCullingMode.Front;

        [Tooltip("If enabled, ripple effect will be calculated for this surface.\nYou must specify projector also.\nDisable if you do not use it.")]
        [SerializeField] internal bool useRipple = false;
        [Tooltip("Assign existing projector on your scene.")]
        [SerializeField] internal WaterRippleProjector rippleProjector;


        [Tooltip("Above this height from surface level, system will stop detecting this water surface below.")]
        [SerializeField] internal float maxHeight = 100.0f;
        [Tooltip("Maximum depth of water - below this value system will stop detecting being underwater.")]
        [SerializeField] internal float maxDepth = 100.0f;

        [Tooltip("Extra global multiplier for buoyancy force for Floating Body.")]
        [SerializeField] internal float physicsBuoyancyFactor = 1.0f;
        [Tooltip("Adds extra submerge drag value for Floating Body.")]
        [SerializeField] internal float physicsExtraDrag = 0.0f;
        [Tooltip("Adds extra submerge angular drag value for Floating Body.")]
        [SerializeField] internal float physicsExtraAngularDrag = 0.0f;

        [Tooltip("If set to true, this will be default surface for easier accessibility via API and automatic optimization for Water Sampler.\nYou do not need to have a default surface, but you want, you can have only one.")]
        [SerializeField] internal bool isDefaultSurface = false;


        public GeometryMeshType GeometryType
        {
            get => geometryType;
            set
            {
                if (geometryType != value)
                {
                    geometryType = value;
                    InvalidateMesh();
                }
            }
        }
        public RectangleMeshConfig GeometryRectangleConfig
        {
            get => geometryRectangleConfig;
            set
            {
                if (geometryRectangleConfig.width != value.width ||
                    geometryRectangleConfig.height != value.height ||
                    geometryRectangleConfig.subdivisions != value.subdivisions)
                {
                    geometryRectangleConfig = value;
                    if (geometryType == GeometryMeshType.Rectangle) InvalidateMesh();
                }
            }
        }
        public CircleMeshConfig GeometryCircleConfig
        {
            get => geometryCircleConfig;
            set
            {
                if (geometryCircleConfig.radius != value.radius ||
                    geometryCircleConfig.segments != value.segments ||
                    geometryCircleConfig.rings != value.rings)
                {
                    geometryCircleConfig = value;
                    if (geometryType == GeometryMeshType.Circle) InvalidateMesh();
                }
            }
        }
        public string GeometryInfiniteOceanPreset
        {
            get => geometryInfiniteOceanPreset;
            set
            {
                if (geometryInfiniteOceanPreset != value)
                {
                    geometryInfiniteOceanPreset = value;
                    if (geometryType == GeometryMeshType.InfiniteOcean) InvalidateMesh();
                }
            }
        }
        public OceanAttachTarget GeometryInfiniteOceanAttachTo
        {
            get => geometryInfiniteOceanAttachTo;
            set
            {
                if (geometryInfiniteOceanAttachTo != value)
                {
                    geometryInfiniteOceanAttachTo = value;
                }
            }
        }
        public Transform GeometryInfiniteOceanAttachCustomTarget
        {
            get => geometryInfiniteOceanAttachCustomTarget;
            set
            {
                if (geometryInfiniteOceanAttachCustomTarget != value)
                {
                    geometryInfiniteOceanAttachCustomTarget = value;
                }
            }
        }
        public Mesh GeometryCustomMesh
        {
            get => geometryCustomMesh;
            set
            {
                if (geometryCustomMesh != value)
                {
                    geometryCustomMesh = value;
                    if (geometryType == GeometryMeshType.CustomMesh) InvalidateMesh();
                }
            }
        }

        public WaveProfile WaveProfile
        {
            get => waveProfile;
            set
            {
                waveProfile = value;
            }
        }
        public MaterialProfile MaterialProfile
        {
            get => materialProfile;
            set
            {
                materialProfile = value;
                if (materialProfile != null && Material != null)
                    materialProfile.SetMaterialDirty(Material);
            }
        }
        public UnderwaterProfile UnderwaterProfile
        {
            get => underwaterProfile;
            set
            {
                underwaterProfile = value;
            }
        }

        public bool SynchronizeWorldOrigin { get => synchronizeWorldOrigin; set => synchronizeWorldOrigin = value; }
        public bool CutoutWaterVolume { get => cutoutWaterVolume; set => cutoutWaterVolume = value; }
        public bool CutoutReverse { get => cutoutReverse; set => cutoutReverse = value; }
        public CutoutCullingMode CutoutCulling { get => cutoutCulling; set => cutoutCulling = value; }
        public bool UseRipple { get => useRipple; set => useRipple = value; }
        public WaterRippleProjector RippleProjector { get => rippleProjector; set => rippleProjector = value; }
        public float MaxHeight { get => maxHeight; set => maxHeight = value; }
        public float MaxDepth { get => maxDepth; set => maxDepth = value; }

        public float PhysicBuoyancyFactor { get => physicsBuoyancyFactor; set => physicsBuoyancyFactor = value; }
        public float PhysicsExtraDrag { get => physicsExtraDrag; set => physicsExtraDrag = value; }
        public float PhysicsExtraAngularDrag { get => physicsExtraAngularDrag; set => physicsExtraAngularDrag = value; }

        public bool IsDefaultSurface
        {
            get => isDefaultSurface;
            set
            {
                if (isDefaultSurface != value)
                {
                    isDefaultSurface = value;
                    if (enabled) DragonWaterManager.Instance.UpdateDefaultSurface();
                }
            }
        }



        public Material Material { get; private set; }
        public Bounds Bounds { get; private set; }


        List<MeshRenderer> _renderers = new();
        List<Mesh> _meshCache = new();
        bool _validMesh = false;
        float _gridSnapUnits = -1;
        WaterSampler _samplerInstant;

        private void Awake()
        {
            Material = new Material(Shader.Find(Constants.Shader.WaterShaderName));

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
#endif
                _samplerInstant = new WaterSampler(1);
                _samplerInstant.SurfaceDetection = WaterSampler.SurfaceDetectionMode.Custom;
                _samplerInstant.SurfaceDetectionCustom = new() { this };
#if UNITY_EDITOR
            }
#endif
        }

        private void Start()
        {
        }
        private void OnEnable()
        {
            DragonWaterManager.Instance.RegisterSurface(this);
            if (MaterialProfile && Material)
                MaterialProfile.SetMaterialDirty(Material);

            InvalidateMesh();
        }
        private void OnDisable()
        {
            DragonWaterManager.InstanceUnsafe?.UnregisterSurface(this);
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                _samplerInstant?.Dispose();
            }
#endif
        }
        private void OnDestroy()
        {
            UnityEx.SafeDestroy(Material);
            _renderers.ForEach(r => UnityEx.SafeDestroyGameObject(r));
            _meshCache.ForEach(m => UnityEx.SafeDestroy(m));
            _samplerInstant?.Dispose();
        }


        public void InvalidateMesh()
        {
            _validMesh = false;
            _gridSnapUnits = -1;
        }
        public bool ContainsPoint(Vector3 point)
        {
            if (!Bounds.Contains(point))
                return false;

            if (geometryType == GeometryMeshType.Rectangle)
            {
                var local = transform.InverseTransformPoint(point);

                if (Mathf.Abs(local.x) > (geometryRectangleConfig.width * 0.5f)) return false;
                if (Mathf.Abs(local.z) > (geometryRectangleConfig.height * 0.5f)) return false;
            }
            else if (geometryType == GeometryMeshType.Circle)
            {
                var offset = point - transform.position;

                if ((new Vector2(offset.x, offset.y).magnitude) > geometryCircleConfig.radius) return false;
            }

            // TODO:
            // Find efficient method to check with custom meshes
            // Maybe use PolygonCollider2D somehow?
            //
            // for now, just return true since it is already in bounds

            return true;
        }


        public WaterSampler.HitResult SampleWater(Vector3 position)
        {
            return SampleWater(position, cutoutWaterVolume);
        }
        public WaterSampler.HitResult SampleWater(Vector3 position, bool considerCutouts)
        {
            _samplerInstant.CutoutDetection = considerCutouts ? WaterSampler.CutoutDetectionMode.AutoCull : WaterSampler.CutoutDetectionMode.DontCutout;
            _samplerInstant.SetPoint(0, position);
            _samplerInstant.Schedule();
            _samplerInstant.Complete();
            return _samplerInstant.Results[0];
        }


        internal void UpdateWater(Camera camera)
        {
            if (waveProfile != null)
            {
                if (synchronizeWorldOrigin)
                {
                    waveProfile.WorldOriginPosition = new Vector2(
                        transform.position.x,
                        transform.position.z
                        );
                    waveProfile.WorldOriginRotation = Vector2.SignedAngle(
                        new(transform.forward.x, transform.forward.z),
                        Vector2.up
                        ) * Mathf.Deg2Rad;
                }

                DragonWaterManager.Instance.RequestWaveProcess(waveProfile);
                waveProfile.ConfigureMaterial(Material, camera);
            }
            else
            {
                WaveProfile.CleanupMaterial(Material);
            }

            if (materialProfile != null)
            {
#if UNITY_EDITOR
                materialProfile.ConfigureMaterial(Material, !Application.isPlaying);
#else
                materialProfile.ConfigureMaterial(Material);
#endif
            }

            if (cutoutWaterVolume)
            {
                Material.SetAlphaClip(true);
                Material.SetFloat(Constants.Shader.Property.CutoutWaterVolumeMode, (int)cutoutCulling);
                Material.SetFloat(Constants.Shader.Property.CutoutWaterReverse, cutoutReverse ? 1 : 0);
            }
            else
            {
                Material.SetAlphaClip(false);
            }

            if (useRipple && rippleProjector != null && waveProfile != null)
            {
                var rippler = rippleProjector.CreateRippler(waveProfile);
                rippler.Enqueue();
                rippleProjector.ConfigureMaterial(Material, rippler);
            }
            else
            {
                WaterRippleProjector.CleanupMaterial(Material);
            }


            if (!_validMesh || _gridSnapUnits == -1)
            {
                UpdateMesh();
            }

            if (geometryType == GeometryMeshType.InfiniteOcean)
            {
                var attachTransform = (geometryInfiniteOceanAttachTo == OceanAttachTarget.CustomObject && geometryInfiniteOceanAttachCustomTarget != null)
                    ? geometryInfiniteOceanAttachCustomTarget : camera.transform;

                var position = attachTransform.transform.position;
                position.y = transform.position.y;
                if (_gridSnapUnits > 0)
                {
                    position.x -= Mathf.Sign(position.x) * Mathf.Repeat(Mathf.Abs(position.x), _gridSnapUnits);
                    position.z -= Mathf.Sign(position.z) * Mathf.Repeat(Mathf.Abs(position.z), _gridSnapUnits);
                }

                for (int i = 0; i < _renderers.Count; i++)
                {
                    if (_renderers[i].gameObject.activeSelf)
                        _renderers[i].transform.position = position;
                }

                Bounds = new Bounds(
                    new Vector3(0, position.y + (MaxHeight - MaxDepth) * 0.5f, 0),
                    new Vector3(999999, MaxHeight + MaxDepth, 999999)
                );
            }
            else
            {
                for (int i = 0; i < _renderers.Count; i++)
                {
                    if (_renderers[i].gameObject.activeSelf)
                    {
                        var bounds = _renderers[i].bounds;
                        bounds.extents = new Vector3(
                            bounds.extents.x,
                            0.1f,
                            bounds.extents.z
                            );
                        bounds.max += Vector3.up * MaxHeight;
                        bounds.min += Vector3.down * MaxDepth;
                        Bounds = bounds;
                        break;
                    }
                }
            }
        }

        private void UpdateMesh()
        {
            _meshCache.RemoveAll(m => m == null);
            while (_meshCache.Count < 2)
            {
                _meshCache.Add(new()); // keep 2 meshes in cache
            }

            _renderers.ForEach(r => r.gameObject.SetActive(false));

            if (geometryType == GeometryMeshType.InfiniteOcean)
            {
                var preset = DragonWaterManager.Instance.Config.GetOceanQualityPreset(geometryInfiniteOceanPreset);
                if (preset == null)
                {
                    DragonWaterDebug.LogError("Cannot find ocean mesh preset: " + geometryInfiniteOceanPreset, gameObject);
                    return;
                }

                var q1 = CreateRenderer("#Q1");
                var q2 = CreateRenderer("#Q2");
                var q3 = CreateRenderer("#Q3");
                var q4 = CreateRenderer("#Q4");

                q1.transform.localEulerAngles = Vector3.up * 0.0f;
                q2.transform.localEulerAngles = Vector3.up * 90.0f;
                q3.transform.localEulerAngles = Vector3.up * 180.0f;
                q4.transform.localEulerAngles = Vector3.up * 270.0f;

                q1.gameObject.SetActive(true);
                q2.gameObject.SetActive(true);
                q3.gameObject.SetActive(true);
                q4.gameObject.SetActive(true);

                q1.GetComponent<MeshFilter>().sharedMesh = _meshCache[0];
                q2.GetComponent<MeshFilter>().sharedMesh = _meshCache[1];
                q3.GetComponent<MeshFilter>().sharedMesh = _meshCache[0];
                q4.GetComponent<MeshFilter>().sharedMesh = _meshCache[1];

                var meshCreatorA = new OceanMeshCreator(false);
                var meshCreatorB = new OceanMeshCreator(true);

                meshCreatorA.ApplyPreset(preset);
                meshCreatorB.ApplyPreset(preset);

                meshCreatorA.BuildMesh(_meshCache[0]);
                meshCreatorB.BuildMesh(_meshCache[1]);

                _gridSnapUnits = preset.gridSnapUnits;
            }
            else
            {
                var renderer = CreateRenderer("#Renderer");
                renderer.transform.localPosition = Vector3.zero;
                renderer.transform.localRotation = Quaternion.identity;
                renderer.gameObject.SetActive(true);

                var filter = renderer.GetComponent<MeshFilter>();

                switch (geometryType)
                {
                    case GeometryMeshType.Rectangle:
                        {
                            geometryRectangleConfig.SafeClamp();
                            filter.sharedMesh = MeshUtility.CreateRectangle(
                                geometryRectangleConfig.width,
                                geometryRectangleConfig.height,
                                geometryRectangleConfig.subdivisions,
                                _meshCache[0]);
                        }
                        break;
                    case GeometryMeshType.Circle:
                        {
                            geometryCircleConfig.SafeClamp();
                            filter.sharedMesh = MeshUtility.CreateCircle(
                                geometryCircleConfig.radius,
                                geometryCircleConfig.segments,
                                geometryCircleConfig.rings,
                                _meshCache[0]);
                        }
                        break;
                    case GeometryMeshType.CustomMesh:
                        {
                            filter.sharedMesh = geometryCustomMesh;
                        }
                        break;
                }

                _gridSnapUnits = 1.0f;
            }

            _validMesh = true;
        }
        private MeshRenderer CreateRenderer(string name)
        {
            var child = transform.Find(name);
            if (child == null)
            {
                child = new GameObject(name).transform;
                child.parent = transform;
                child.localPosition = Vector3.zero;
                child.localRotation = Quaternion.identity;
                child.localScale = Vector3.one;
                child.gameObject.AddComponent<MeshFilter>();
                child.gameObject.AddComponent<MeshRenderer>();
            }

            child.gameObject.layer = DragonWaterManager.Instance.Config.WaterRendererLayer;
            child.gameObject.hideFlags = HideFlags.HideAndDontSave;
            //child.gameObject.hideFlags = HideFlags.None;

            var renderer = child.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = Material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            _renderers.Add(renderer);

            return renderer;
        }



#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                UnityEditor.SceneView.RepaintAll();
            }

            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(Bounds.center, Bounds.size);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.clear;

            Gizmos.matrix = transform.localToWorldMatrix;

            if (geometryType == GeometryMeshType.InfiniteOcean)
            {
                Gizmos.DrawCube(Vector3.zero, new Vector3(5000, 0.1f, 5000));
            }
            else
            {
                var r = _renderers.Find(r => r.gameObject.activeSelf);
                if (r != null)
                {
                    var bounds = r.localBounds;
                    Gizmos.DrawCube(bounds.center, new Vector3(bounds.extents.x * 2.0f, 0.1f, bounds.extents.y * 2.0f));
                }
            }
        }
#endif

#if UNITY_EDITOR
        // in editor mode, force update activeness in manager in case of script reload
        private void Update()
        {
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (enabled && !DragonWaterManager.Instance.Surfaces.Contains(this))
                {
                    enabled = false;
                    enabled = true;
                }
            }
        }
#endif
    }
}
