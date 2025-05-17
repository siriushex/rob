using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace DragonWater.Utils
{
    internal static class MeshUtility
    {
        static Mesh _spherePrimitive;
        static Mesh _boxPrimitive;
        static Mesh _rayPlane;

        public static Mesh GetSpherePrimitive()
        {
            if (_spherePrimitive != null)
                return _spherePrimitive;

            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _spherePrimitive = gameObject.GetComponent<MeshFilter>().sharedMesh;
            UnityEx.SafeDestroy(gameObject);

            return _spherePrimitive;
        }
        public static Mesh GetBoxPrimitive()
        {
            if (_boxPrimitive != null)
                return _boxPrimitive;

            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _boxPrimitive = gameObject.GetComponent<MeshFilter>().sharedMesh;
            UnityEx.SafeDestroy(gameObject);

            return _boxPrimitive;
        }
        public static Mesh GetRayPlane()
        {
            if (_rayPlane != null)
                return _rayPlane;

            _rayPlane = new Mesh();
            _rayPlane.vertices = new Vector3[]
            {
                new Vector3(-0.5f, 0.0f, 0.0f),
                new Vector3(0.5f, 0.0f, 0.0f),
                new Vector3(-0.5f, -1.0f, 0.0f),
                new Vector3(0.5f, -1.0f, 0.0f),
            };
            _rayPlane.uv = new Vector2[]
            {
                new Vector2(0.0f, 1.0f),
                new Vector2(1.0f, 1.0f),
                new Vector2(0.0f, 0.0f),
                new Vector2(1.0f, 0.0f),
            };
            _rayPlane.triangles = new int[]
            {
                0, 2, 1,
                1, 2, 3
            };
            _rayPlane.UploadMeshData(true);

            return _rayPlane;
        }

        public static Mesh CreateRectangle(float width, float height, int subdivisions, Mesh sourceMesh = null)
        {
            var max = Mathf.Max(width, height);
            var min = Mathf.Min(width, height);

            var subMax = subdivisions;
            var subMin = Mathf.RoundToInt((min/max) * subMax);

            var xSegs = (width > height ? subMax : subMin) + 1;
            var ySegs = (width > height ? subMin : subMax) + 1;

            var xLen = width / xSegs;
            var yLen = height / ySegs;

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();

            for (int x = 0; x < xSegs + 1; x++)
            {
                var bx = (x * (ySegs + 1));
                var bxp = ((x - 1) * (ySegs + 1));

                for (int y = 0; y < ySegs + 1; y++)
                {
                    vertices.Add(new Vector3(
                        (width * -0.5f) + x * xLen,
                        0,
                        (height * -0.5f) + y * yLen
                        ));
                    normals.Add(Vector3.up);

                    if (x > 0 && y > 0)
                    {
                        triangles.Add(bxp + y);
                        triangles.Add(bx + y);
                        triangles.Add(bxp + y - 1);

                        triangles.Add(bxp + y - 1);
                        triangles.Add(bx + y);
                        triangles.Add(bx + y - 1);
                    }
                }
            }

            if (sourceMesh == null)
                sourceMesh = new();

            sourceMesh.Clear(false);

            sourceMesh.vertices = vertices.ToArray();
            sourceMesh.normals = normals.ToArray();
            sourceMesh.triangles = triangles.ToArray();

            sourceMesh.bounds = new Bounds(
               Vector3.zero,
               new Vector3(width * 1.05f, 50, height * 1.05f)
               );

            sourceMesh.RecalculateTangents();
            sourceMesh.UploadMeshData(false);

            return sourceMesh;
        }
        public static Mesh CreateCircle(float radius, int segments, int rings, Mesh sourceMesh = null)
        {
            rings = rings + 1; // actual amount of rings;
            var rsRatio = radius / segments;

            var segs = new List<int>() { segments };
            while (segments % 2 == 0 && segs.Count < rings && segments > 8)
            {
                segments /= 2;
                segs.Add(segments);
            }
            segs.Reverse();

            var vertices = new List<Vector3>() { Vector3.zero };
            var normals = new List<Vector3>() { Vector3.up };
            var triangles = new List<int>();

            var prevSegs = segs[0];
            var currentSegs = segs[0];
            for (int i = 0; i < rings; i++)
            {
                var r = radius * ((i + 1.0f) / rings);

                var nearestSegs = segs.OrderBy(s => Mathf.Abs((s * rsRatio) - r)).First();
                if (nearestSegs > currentSegs && i > 0)
                    currentSegs = segs[segs.IndexOf(currentSegs) + 1];

                var bi = vertices.Count;
                var bip = vertices.Count - (prevSegs + 1);

                for (int j = 0; j < currentSegs + 1; j++)
                {
                    var a = Mathf.PI * 2.0f * ((float)j / currentSegs);

                    vertices.Add(new Vector3(
                        Mathf.Sin(a) * r,
                        0,
                        Mathf.Cos(a) * r
                        ));
                    normals.Add(Vector3.up);

                    if (j > 0)
                    {
                        if (i == 0)
                        {
                            triangles.Add(bi + j);
                            triangles.Add(0);
                            triangles.Add(bi + j - 1);
                        }
                        else if (prevSegs == currentSegs)
                        {
                            triangles.Add(bi + j);
                            triangles.Add(bip + j);
                            triangles.Add(bip + j - 1);

                            triangles.Add(bi + j);
                            triangles.Add(bip + j - 1);
                            triangles.Add(bi + j - 1);
                        }
                        else if (j % 2 == 0)
                        {
                            var jh = j / 2;

                            triangles.Add(bip + jh - 1);
                            triangles.Add(bi + j - 2);
                            triangles.Add(bi + j - 1);

                            triangles.Add(bip + jh - 1);
                            triangles.Add(bi + j - 1);
                            triangles.Add(bip + jh);

                            triangles.Add(bip + jh);
                            triangles.Add(bi + j - 1);
                            triangles.Add(bi + j);
                        }
                    }
                }

                prevSegs = currentSegs;
            }


            sourceMesh.Clear(false);

            sourceMesh.vertices = vertices.ToArray();
            sourceMesh.normals = normals.ToArray();
            sourceMesh.triangles = triangles.ToArray();

            sourceMesh.bounds = new Bounds(
               Vector3.zero,
               new Vector3(radius * 1.05f, 50, radius * 1.05f)
               );

            sourceMesh.RecalculateTangents();
            sourceMesh.UploadMeshData(false);

            return sourceMesh;
        }
    }
}
