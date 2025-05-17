using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonWater.Utils
{
    internal class OceanMeshCreator
    {
        List<Vector3> _vertices = new();
        List<int> _indices = new();

        float _density = 0.0f;
        float _length = 0.0f;
        int _size = 0;
        bool _flipTraingulation = false;

        public float CurrentDensity => _density;
        public float CurrentLength => _length;
        public bool FlipTriangulation => _flipTraingulation;

        public int CountVertices => _vertices.Count;
        public int CountTriangles => _indices.Count / 3;

        public OceanMeshCreator(bool flipTraingulation)
        {
            _flipTraingulation = flipTraingulation;

            AddBorderVertices();
        }

        public void ApplyPreset(OceanMeshQualityPreset preset)
        {
            for (var i = 0; i < preset.densities.Count; i++)
            {
                SetDensity(preset.densities[i]);
                ExtendTo(preset.lengths[i]);
            }

            while (CurrentLength < preset.maxFarDistance)
                SetDensity(CurrentDensity * 2.0f);
        }

        public void SetDensity(float density)
        {
            if (density <= 0.0f)
                throw new Exception("Invalid density");

            var oldDensity = _density;

            if (oldDensity == 0.0f || _size == 0)
            {
                _density = density;
                return;
            }

            var factor = Mathf.RoundToInt(density / oldDensity);
            if (factor <= 0 || !Mathf.IsPowerOfTwo(factor))
                throw new Exception("New density factor is not power of two");

            var requiredBorders = (factor - (_size % factor)) % factor;
            for (var i = 0; i < requiredBorders; i++)
                AddBorder();

            _density = density;

            var cBefore = (_vertices.Count - _size) - 1;

            var oldSize = _size;
            _size = (_size / factor) + 1;

            _length += density;
            AddBorderVertices();

            var cAfter = (_vertices.Count - _size) - 1;


            var hFactor = factor / 2;

            var it = cAfter - _size;
            var ib = cBefore - oldSize;

            for (var i = 0; i < _size; i++)
            {
                if (i > 0)
                {
                    AddTriangle(it, it - 1, ib - hFactor);
                }

                for (var n = -hFactor; n <= hFactor; n++)
                {
                    if (n > 0)
                    {
                        if (i == _size - 1) continue;
                        AddTriangle(it, ib + (n - 1), ib + n);
                    }
                    else if (n < 0)
                    {
                        if (i == 0) continue;
                        AddTriangle(it, ib + n, ib + (n + 1));
                    }
                }
                it++; ib += factor;
            }

            ib -= factor;
            if (_flipTraingulation)
            {
                AddTriangle(it, ib, it + 1);
                AddTriangle(it - 1, ib, it);
            }
            else
            {
                AddTriangle(it + 1, it, it - 1);
                AddTriangle(it + 1, it - 1, ib);
            }
            it++;

            for (var i = 0; i < _size; i++)
            {
                if (i > 0)
                {
                   AddTriangle(it, it - 1, ib - hFactor);
                }
                for (var n = -hFactor; n <= hFactor; n++)
                {
                    if (n > 0)
                    {
                        if (i == _size - 1) continue;
                        AddTriangle(it, ib + (n - 1), ib + n);
                    }
                    else if (n < 0)
                    {
                        if (i == 0) continue;
                        AddTriangle(it, ib + n, ib + (n + 1));
                    }
                }
                it++; ib += factor;
            }
        }


        public void ExtendTo(float distance)
        {
            while (_length < distance)
            {
                AddBorder();
            }
        }
        public void ExtendBy(int borders)
        {
            for (var i = 0; i < borders; i++)
            {
                AddBorder();
            }
        }

        void AddBorder()
        {
            var cBefore = (_vertices.Count - _size) - 1;

            _length += _density;
            _size++;
            AddBorderVertices();

            var cAfter = (_vertices.Count - _size) - 1;

            var it = cAfter - _size;
            var ib = cBefore - (_size - 1);

            for (var i = 0; i < _size; i++)
            {
                if (_flipTraingulation)
                {
                    if (i > 0) AddTriangle(it, it - 1, ib - 1);
                    if (i < _size - 1) AddTriangle(ib, ib + 1, it + 1);
                }
                else
                {
                    if (i > 0) AddTriangle(it, it - 1, ib);
                    if (i < _size - 1) AddTriangle(ib, ib + 1, it);
                }
                it++; ib++;
            }

            ib--;
            if (_flipTraingulation)
            {
                AddTriangle(it, ib, it + 1);
                AddTriangle(it - 1, ib, it);
            }
            else
            {
                AddTriangle(it + 1, it, it - 1);
                AddTriangle(it + 1, it - 1, ib);
            }
            it++;

            for (var i = 0; i < _size; i++)
            {
                if (_flipTraingulation)
                {
                    if (i > 0) AddTriangle(ib, it, it - 1);
                    if (i < _size - 1) AddTriangle(it, ib, ib + 1);
                }
                else
                {
                    if (i > 0) AddTriangle(ib, it, ib - 1);
                    if (i < _size - 1) AddTriangle(it, ib, it + 1);
                }
                it++; ib++;
            }
        }


        void AddBorderVertices()
        {
            for (var i = 0; i < _size; i++) _vertices.Add(new(_length, 0, _length * i/_size));
            _vertices.Add(new Vector3(_length,0, _length));
            for (var i = 0; i < _size; i++) _vertices.Add(new(_length * (1.0f - (i+1.0f)/_size), 0, _length));
        }

        void AddTriangle(int a, int b, int c)
        {
            _indices.Add(a);
            _indices.Add(b);
            _indices.Add(c);
        }


        public void BuildMesh(Mesh mesh)
        {
            var vertices = _vertices.ToArray();
            var indices = _indices.ToArray();
            var normals = new Vector3[vertices.Length];

            for (var i = 0; i < vertices.Length; i++)
                normals[i] = Vector3.up;

            mesh.Clear(false);

            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = indices;

            mesh.bounds = new Bounds(
                new Vector3(CurrentLength / 2, 0, CurrentLength / 2),
                new Vector3(CurrentLength * 1.05f, 50, CurrentLength * 1.05f)
                );
            
            mesh.RecalculateTangents();
            mesh.UploadMeshData(false);
        }
    }
}
