using DragonWater.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace DragonWater
{
    [AddComponentMenu("Dragon Water/Ripple Caster")]
    [ExecuteAlways]
    public class WaterRippleCaster : MonoBehaviour
    {
        [SerializeField] internal Mesh mesh = null;

        MeshRenderer _renderer;


        #region properties
        public Mesh Mesh
        {
            get { return mesh; }
            set
            {
                mesh = value;
                UpdateCaster();
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
        }
        private void OnDisable()
        {
            if (_renderer) _renderer.enabled = false;
        }

        private void Reset()
        {
            UpdateCaster();
        }


        private void EnsureDynamicConfig()
        {
            GetRenderer().SetShaderMaterialAll(DragonWaterManager.Instance.RippleCasterMaterial);

            _renderer.gameObject.layer = DragonWaterManager.Instance.Config.RippleLayer;
        }

        internal void UpdateCaster()
        {
            var renderer = GetRenderer();
            var filter = renderer.GetComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            renderer.transform.localPosition = Vector3.zero;
            renderer.transform.localScale = Vector3.one;
        }


#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            UpdateCaster();
            EnsureDynamicConfig();

            var rendeer = GetRenderer();
            var mesh = rendeer.GetComponent<MeshFilter>().sharedMesh;
            if (mesh != null)
            {
                Gizmos.color = new Color(1.0f, 0.0f, 0.5f, 0.3f);
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

#if UNITY_EDITOR
        [UnityEditor.MenuItem("CONTEXT/MeshRenderer/Create Ripple Caster")]
        static void CreateFromContextMenu(UnityEditor.MenuCommand command)
        {
            var renderer = (MeshRenderer)command.context;
            var filter = renderer.GetComponent<MeshFilter>();
            var index = renderer.transform.GetSiblingIndex();

            var newObject = new GameObject(renderer.gameObject.name + " - Ripple Caster");
            newObject.transform.parent = renderer.transform.parent;
            newObject.transform.localPosition = renderer.transform.localPosition;
            newObject.transform.localRotation = renderer.transform.localRotation;
            newObject.transform.localScale = renderer.transform.localScale;
            newObject.transform.SetSiblingIndex(index + 1);

            var caster = newObject.AddComponent<WaterRippleCaster>();
            caster.Mesh = filter.sharedMesh;

            UnityEditor.Selection.activeGameObject = newObject;
        }
#endif
    }
}