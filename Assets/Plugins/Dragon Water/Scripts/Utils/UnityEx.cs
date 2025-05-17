using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;

namespace DragonWater.Utils
{
    internal static class UnityEx
    {
        public static Vector2 GetWaterOffset(this Camera camera)
        {
            var position = camera.transform.position;
            //return new Vector2(position.x, position.y);
            return new Vector2(Mathf.Round(position.x), Mathf.Round(position.z));
        }

        public static Bounds MakeBounds(this Vector3[] points, int length = -1, float margin = 0.1f)
        {
            if (length == -1) length = points.Length;

            var bounds = new Bounds(points[0], Vector3.zero);
            for (int i = 1; i < length; i++)
                bounds.Encapsulate(points[i]);
            bounds.Expand(margin);

            return bounds;
        }
        public static Vector4 ToSphere(this Bounds bounds)
        {
            return new Vector4(
                bounds.center.x,
                bounds.center.y,
                bounds.center.z,
                bounds.extents.magnitude
                );
        }

        public static void SafeDestroy(Object obj)
        {
            if (obj != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Object.DestroyImmediate(obj);
                else
#endif
                    Object.Destroy(obj);
            }
        }

        public static void SafeDestroyGameObject(Component obj)
        {
            if (obj != null)
            {
                SafeDestroy(obj.gameObject);
            }
        }

        public static void SetKeywordEnum(this Material material, string[] values, int value)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (i == value)
                    material.EnableKeyword(values[i]);
                else
                    material.DisableKeyword(values[i]);
            }
        }
        public static void SetKeywordEnum(this ComputeShader computeShader, string[] values, int value)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (i == value)
                    computeShader.EnableKeyword(values[i]);
                else
                    computeShader.DisableKeyword(values[i]);
            }
        }
        public static void SetTransparent(this Material material, bool setTransprent)
        {
            if (setTransprent)
            {
                material.SetFloat(Constants.Shader.Property.Surface, 1);
                material.SetOverrideTag("RenderType", "Transparent");
                material.EnableKeyword(Constants.Shader.Keyword.SurfaceTypeTransparent);

                material.SetFloat(Constants.Shader.Property.SrcBlend, (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat(Constants.Shader.Property.DstBlend, (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }
            else
            {
                material.SetFloat(Constants.Shader.Property.Surface, 0);
                material.SetOverrideTag("RenderType", "Opaque");
                material.DisableKeyword(Constants.Shader.Keyword.SurfaceTypeTransparent);

                material.SetFloat(Constants.Shader.Property.SrcBlend, (float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat(Constants.Shader.Property.DstBlend, (float)UnityEngine.Rendering.BlendMode.Zero);
            }
        }
        public static void SetAlphaClip(this Material material, bool setClip)
        {
            if (setClip)
            {
                material.SetFloat(Constants.Shader.Property.AlphaClip, 1);
                material.EnableKeyword(Constants.Shader.Keyword.AlphaTestOn);
            }
            else
            {
                material.SetFloat(Constants.Shader.Property.AlphaClip, 0);
                material.DisableKeyword(Constants.Shader.Keyword.AlphaTestOn);
            }
        }
        public static void SetShaderMaterialAll(this MeshRenderer renderer, Material material)
        {
            var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            if (mesh == null)
                return;

            if (mesh.subMeshCount == 1)
            {
                renderer.sharedMaterial = material;
                return;
            }

            var shared = renderer.sharedMaterials;
            if (shared.Length != mesh.subMeshCount)
            {
                shared = new Material[mesh.subMeshCount];
            }

            for (int i = 0; i < shared.Length; i++)
                shared[i] = material;
            renderer.sharedMaterials = shared;
        }

        public static List<GameObject> GetAllChildren(this GameObject gameObject)
        {
            var list = new List<GameObject>();
            foreach (Transform child in gameObject.transform)
            {
                list.Add(child.gameObject);
            }
            return list;
        }
    }
}
