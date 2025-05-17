using DragonWater.Scripting;
using DragonWater.Utils;
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DragonWater
{
    [AddComponentMenu("Dragon Water/Cutout Volume")]
    [ExecuteAlways]
    public class WaterCutoutVolume : MonoBehaviour
    {
        [Serializable]
        public enum CutoutShape
        {
            Sphere,
            Box,
            CustomMesh
        }

        [SerializeField] internal CutoutShape shape = CutoutShape.Box;
        [SerializeField] internal Vector3 center = Vector3.zero;
        [SerializeField] internal Vector3 size = Vector3.one;
        [SerializeField] internal float radius = 1.0f;
        [SerializeField] internal Mesh mesh = null;

        [SerializeField] internal Collider _collider;
        [SerializeField] internal MeshRenderer _renderer;


        #region main properties
        public CutoutShape Shape
        {
            get { return shape; }
            set
            {
                if (shape == value) return;
#if UNITY_EDITOR
                EditorSaveParams();
#endif
                shape = value;
                UpdateVolume();
            }
        }
        public Vector3 Center
        {
            get { return center; }
            set
            {
                center = value;
                if (shape == CutoutShape.Sphere) { GetCollider<SphereCollider>().center = center; UpdateVolume(); }
                if (shape == CutoutShape.Box) { GetCollider<BoxCollider>().center = center; UpdateVolume(); }
            }
        }
        public Vector3 Size
        {
            get { return size; }
            set
            {
                size = value;
                if (shape == CutoutShape.Box) { GetCollider<BoxCollider>().size = size; UpdateVolume(); }
            }
        }
        public float Radius
        {
            get { return radius; }
            set
            {
                radius = value;
                if (shape == CutoutShape.Sphere) { GetCollider<SphereCollider>().radius = radius; UpdateVolume(); }
            }
        }
        public Mesh Mesh
        {
            get { return mesh; }
            set
            {
                mesh = value;
                if (shape == CutoutShape.CustomMesh) { GetCollider<MeshCollider>().sharedMesh = mesh; UpdateVolume(); }
            }
        }
        #endregion


        private void Awake()
        {
            EnsureDynamicConfig();
        }

        private void OnEnable()
        {
            if (_renderer) _renderer.enabled = true;
            if (_collider) _collider.enabled = true;
        }
        private void OnDisable()
        {
            if (_renderer) _renderer.enabled = false;
            if (_collider) _collider.enabled = false;
        }

        private void OnDestroy()
        {
            WaterSampler.CompleteAll();
        }

        private void Reset()
        {
            UpdateVolume();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // force
            if (shape == CutoutShape.CustomMesh && _collider != null && _collider is MeshCollider)
                (_collider as MeshCollider).convex = true;

            if (_collider != null)
                _collider.isTrigger = true;
        }
#endif

        private void EnsureDynamicConfig()
        {
            GetRenderer().SetShaderMaterialAll(DragonWaterManager.Instance.VolumeCutoutMaterial);

            gameObject.layer = DragonWaterManager.Instance.Config.CutoutLayer;
            _renderer.gameObject.layer = DragonWaterManager.Instance.Config.CutoutLayer;
        }

        internal void UpdateVolume()
        {
            var renderer = GetRenderer();
            var filter = renderer.GetComponent<MeshFilter>();
            switch (shape)
            {
                case CutoutShape.Sphere:
                    {
                        filter.sharedMesh = MeshUtility.GetSpherePrimitive();
                        renderer.transform.localPosition = GetCollider<SphereCollider>().center;
                        renderer.transform.localScale = GetCollider<SphereCollider>().radius * Vector3.one * 2.0f;
                    }
                    break;
                case CutoutShape.Box:
                    {
                        filter.sharedMesh = MeshUtility.GetBoxPrimitive();
                        renderer.transform.localPosition = GetCollider<BoxCollider>().center;
                        renderer.transform.localScale = GetCollider<BoxCollider>().size;
                    }
                    break;
                case CutoutShape.CustomMesh:
                    {
                        filter.sharedMesh = GetCollider<MeshCollider>().sharedMesh;
                        renderer.transform.localPosition = Vector3.zero;
                        renderer.transform.localScale = Vector3.one;
                    }
                    break;
            }
        }

#if UNITY_EDITOR
        internal void EditorSaveParams()
        {
            switch (shape)
            {
                case CutoutShape.Sphere:
                    {
                        center = GetCollider<SphereCollider>().center;
                        radius = GetCollider<SphereCollider>().radius;
                    }
                    break;
                case CutoutShape.Box:
                    {
                        center = GetCollider<BoxCollider>().center;
                        size = GetCollider<BoxCollider>().size;
                    }
                    break;
                case CutoutShape.CustomMesh:
                    {
                        mesh = GetCollider<MeshCollider>().sharedMesh;
                    }
                    break;
            }
        }
#endif


#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            EditorSaveParams();
            UpdateVolume();
            EnsureDynamicConfig();
            OnValidate();

            var rendeer = GetRenderer();
            var mesh = rendeer.GetComponent<MeshFilter>().sharedMesh;
            if (mesh != null)
            {
                Gizmos.color = new Color(1.0f, 0.0f, 1.0f, 0.2f);
                Gizmos.DrawMesh(
                    mesh,
                    rendeer.transform.position,
                    rendeer.transform.rotation,
                    rendeer.transform.lossyScale
                    );
            }
        }
#endif


        private MeshRenderer GetRenderer()
        {
            if (_renderer != null)
            {
#if UNITY_EDITOR
                _renderer.gameObject.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
#endif
                return _renderer;
            }

            var child = transform.Find("Renderer");
            if (child == null)
            {
                child = new GameObject("Renderer").transform;
                child.parent = transform;
                child.localPosition = Vector3.zero;
                child.localRotation = Quaternion.identity;
                child.localScale = Vector3.one;
                child.gameObject.AddComponent<MeshFilter>();
                child.gameObject.AddComponent<MeshRenderer>();
            }

            child.gameObject.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            //child.gameObject.hideFlags = HideFlags.None;

            _renderer = child.GetComponent<MeshRenderer>();
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            EnsureDynamicConfig();

            return _renderer;
        }

        private T GetCollider<T>() where T : Collider
        {
            if (_collider != null && _collider is T)
                return _collider as T;

            if (_collider != null)
            {
                WaterSampler.CompleteAll();
                UnityEx.SafeDestroy(_collider);
            }

            T collider = gameObject.GetComponent<T>();
            if (collider == null)
                collider = gameObject.AddComponent<T>();

            if (collider is SphereCollider)
            {
                (collider as SphereCollider).center = center;
                (collider as SphereCollider).radius = radius;
            }
            else if (collider is BoxCollider)
            {
                (collider as BoxCollider).center = center;
                (collider as BoxCollider).size = size;
            }
            else if (collider is MeshCollider)
            {
                (collider as MeshCollider).convex = true;
                (collider as MeshCollider).sharedMesh = mesh;
            }
            collider.isTrigger = true;

            _collider = collider;
            return collider;
        }


#if UNITY_EDITOR
        [UnityEditor.MenuItem("CONTEXT/MeshRenderer/Create Cutout Volume")]
        static void CreateFromContextMenu(UnityEditor.MenuCommand command)
        {
            var renderer = (MeshRenderer)command.context;
            var filter = renderer.GetComponent<MeshFilter>();
            var index = renderer.transform.GetSiblingIndex();

            var newObject = new GameObject(renderer.gameObject.name + " - Cutout Volume");
            newObject.transform.parent = renderer.transform.parent;
            newObject.transform.localPosition = renderer.transform.localPosition;
            newObject.transform.localRotation = renderer.transform.localRotation;
            newObject.transform.localScale = renderer.transform.localScale;
            newObject.transform.SetSiblingIndex(index + 1);

            var volume = newObject.AddComponent<WaterCutoutVolume>();
            volume.Shape = CutoutShape.CustomMesh;
            volume.Mesh = filter.sharedMesh;

            UnityEditor.Selection.activeGameObject = newObject;
        }
#endif
    }
}