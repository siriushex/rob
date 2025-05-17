#if UNITY_EDITOR
using DragonWater.Scripting;
using DragonWater.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DragonWater.Editor
{
    [CustomEditor(typeof(FloatingBody))]
    internal class FloatingBodyDrawer : WaterBehaviourDrawer
    {
        const int EDIT_NONE = -2;
        const int EDIT_COM = -1;

        bool _editMode = false;
        int _editIndex = EDIT_NONE;
        Vector3 _raycastPosition = Vector3.zero;
        float _raycastRadius = 10.0f;
        int _raycastCount = 4;
        float _raycastOffset = 0.0f;

       private void OnEnable()
{
    _editMode = false;
    _editIndex = EDIT_NONE;

    var body = (FloatingBody)target;

    // Попробуем автоматически установить Plane Pos (т.е. _raycastPosition)
    var renderer = body.GetComponent<Renderer>();
    if (renderer != null)
    {
        Vector3 worldCenter = renderer.bounds.center;
        _raycastPosition = body.transform.InverseTransformPoint(worldCenter); // локальные координаты
    }
    else
    {
        // Если рендера нет — хотя бы центр объекта
        _raycastPosition = Vector3.zero;
    }
}

        protected override void DrawInspectorDefault()
        {
            var body = (FloatingBody)target;

            DrawFoldoutSection("fb_geometry", "Geometry Settings", () =>
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                DrawTitle("Center of Mass");
                if (_editIndex == EDIT_COM) PushGUITint(1.0f, 1.0f, 0.5f);
                else PushGUITint(1, 1, 1);
                GUI.enabled = _editMode;
                PropertyField(serializedObject.FindProperty(nameof(FloatingBody.centerOfMass)), null);
                GUI.enabled = true;
                PopGUITint();

                EditorGUILayout.Space();
                DrawTitle("Contact Points");
                for (int i = 0; i < body.contactPoints.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    if (_editIndex == i) PushGUITint(1.0f, 1.0f, 0.5f);
                    else PushGUITint(1, 1, 1);

                    EditorGUILayout.LabelField($"{i}:", EditorStyles.boldLabel, GUILayout.Width(30));

                    GUI.enabled = _editMode;
                    body.contactPoints[i] = EditorGUILayout.Vector3Field(GUIContent.none, body.contactPoints[i]);

                    PushGUITint(1, 0.75f, 0.75f);
                    if (GUILayout.Button("X", GUILayout.Width(30)))
                    {
                        var list = new List<Vector3>(body.contactPoints);
                        list.RemoveAt(i);
                        body.contactPoints = list.ToArray();
                        if (_editIndex == i) _editIndex = EDIT_NONE;
                        else if (_editIndex > i) _editIndex--;
                        break;
                    }
                    PopGUITint();
                    GUI.enabled = true;

                    PopGUITint();
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                PushGUITint(0.75f, 1.0f, 0.75f);
                GUI.enabled = _editMode;
               if (GUILayout.Button("+"))
{
    var list = new List<Vector3>(body.contactPoints);

    // Если первая — делаем просто по центру
    if (list.Count == 0)
    {
        list.Add(_raycastPosition);
    }
    else
    {
        // Добавим точку по окружности
        int total = list.Count + 1;
        float angle = (Mathf.PI * 2f) / total;
        float offset = angle * _raycastOffset;

        // Считаем позицию по окружности
        float a = offset + angle * list.Count;
        Vector3 localPoint = _raycastPosition + new Vector3(
            Mathf.Sin(a) * _raycastRadius,
            0,
            Mathf.Cos(a) * _raycastRadius
        );

        list.Add(localPoint);
    }

    body.contactPoints = list.ToArray();
}
                GUI.enabled = true;
                PopGUITint();
                EditorGUILayout.EndHorizontal();

                GUI.enabled = true;
                EditorGUILayout.Space();
                DrawTitle("Editor");
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                var toggleEdit = GUILayout.Toggle(_editMode, "Edit Mode", EditorStyles.miniButton);
                if (toggleEdit != _editMode)
                {
                    _editMode = toggleEdit;
                    _editIndex = EDIT_NONE;
                    Tools.current = toggleEdit ? Tool.Custom : Tool.Move;
                }
                if (_editMode)
                {
                    EditorGUILayout.Space();
                    DrawTitle("Simple Disc Placer");
                    EditorGUILayout.HelpBox("This is just a simple tool for fast prototyping.\nAfter all you should manually adjust all contact points and ensure they are evenly placed.", MessageType.Info);
                    _raycastPosition = EditorGUILayout.Vector3Field("Plane Pos", _raycastPosition);
                    _raycastRadius = EditorGUILayout.FloatField("Radius", _raycastRadius);
                    _raycastCount = EditorGUILayout.IntField("Samples", _raycastCount);
                    _raycastCount = Mathf.Clamp(_raycastCount, 1, 32);
                    _raycastOffset = EditorGUILayout.Slider("Angle Offset", _raycastOffset, 0, 1);
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Add samples at height"))
                    {
                        AddSamplesAtHeight();
                    }
                    if (GUILayout.Button("Clear samples at height"))
                    {
                        ClearSamplesAtHeight();
                    }
                    EditorGUILayout.Space();
                    DrawTitle("Clear");
                    if (GUILayout.Button("Clear all"))
                    {
                        body.contactPoints = new Vector3[0];
                    }
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndVertical();
            });

            EditorGUILayout.Space();
            DrawTitle("Force");
            PropertyField(serializedObject.FindProperty(nameof(FloatingBody.buoyancyForce)));
            EditorGUI.indentLevel++;
            PropertyField(serializedObject.FindProperty(nameof(FloatingBody.ignoreMass)));
            EditorGUI.indentLevel--;

            PropertyField(serializedObject.FindProperty(nameof(FloatingBody.normalAlignemnt)));
            if (body.normalAlignemnt > 0.01f)
            {
                EditorGUI.indentLevel++;
                PropertyField(serializedObject.FindProperty(nameof(FloatingBody.instability)));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            DrawTitle("Submerge Drag");
            PropertyField(serializedObject.FindProperty(nameof(FloatingBody.submergeDrag)), "Drag");
            PropertyField(serializedObject.FindProperty(nameof(FloatingBody.submergeAngularDrag)), "Angular Drag");
        }

        protected override void OnChanges()
        {
            var body = (FloatingBody)target;
            body.CenterOfMass = body.CenterOfMass; // force update CoM in rigidbody
        }

        protected override void DrawInspectorDebug()
        {
            var body = (FloatingBody)target;
            EditorGUILayout.FloatField("Current Submerge Level", body.CurrentSubmergeLevel);
        }

        private void OnSceneGUI()
        {
            var body = (FloatingBody)target;

            if (_editMode)
            {
                var center = body.transform.position + _raycastPosition;

                Handles.color = new Color(1.0f, 1.0f, 0.5f, 0.1f);
                Handles.DrawSolidDisc(center, Vector3.up, _raycastRadius);

                var targets = GetRaycastTargets();
                Handles.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                foreach (var point in targets)
                {
                    Handles.DrawLine(center, point);
                }
            }

            var matrixTo = body.transform.localToWorldMatrix;
            var matrixFrom = body.transform.worldToLocalMatrix;

            for (int i = 0; i < body.contactPoints.Length; i++)
            {
                var point = matrixTo.MultiplyPoint3x4(body.contactPoints[i]);
                point = DrawHandle(point, 0.5f, i, new Color(1.0f, 1.0f, 0.0f));
                body.contactPoints[i] = matrixFrom.MultiplyPoint3x4(point);
            }

            var com = matrixTo.MultiplyPoint3x4(body.centerOfMass);
            com = DrawHandle(com, 1.0f, EDIT_COM, new Color(1.0f, 0.5f, 0.0f));
            body.centerOfMass = matrixFrom.MultiplyPoint3x4(com);
        }

        private Vector3 DrawHandle(Vector3 position, float radius, int index, Color color)
        {
            if (!_editMode)
                color.a = 0.3f;
            else if (_editIndex == index)
                color.a = 0.5f;

            Handles.color = color;
            var click = Handles.Button(position, Quaternion.identity, radius, radius, Handles.SphereHandleCap);
            if (click && _editMode)
            {
                _editIndex = index;
                Repaint();
            }

            if (_editIndex == index)
            {
                position = Handles.PositionHandle(position, ((FloatingBody)target).transform.rotation);
            }

            return position;
        }

        private void AddSamplesAtHeight()
        {
            var body = (FloatingBody)target;
            var center = body.transform.position + _raycastPosition;
            var matrixFrom = body.transform.worldToLocalMatrix;

            var list = new List<Vector3>(body.contactPoints);
            var countBefore = list.Count;

            var colliders = body.gameObject
                .GetComponentsInChildren<Collider>()
                .Where(collider => !collider.isTrigger)
                .ToList();
            if (colliders.Count == 0)
            {
                DragonWaterDebug.LogWarning("No colliders attached to this body. No contact can be added.");
            }

            var targets = GetRaycastTargets();
            foreach (var point in targets)
            {
                var hits = new List<Vector3>();
                colliders.ForEach(collider =>
                {
                    var ray = new Ray(point, (center - point).normalized);
                    if (collider.Raycast(ray, out var hit, _raycastRadius))
                    {
                        hits.Add(hit.point);
                    }
                });

                if (hits.Count > 0)
                {
                    var closest = hits.OrderBy(hit => Vector3.Distance(hit, point)).First();
                    list.Add(matrixFrom.MultiplyPoint3x4(closest));
                }
            }

            body.contactPoints = list.ToArray();

            var countAfter = list.Count;
            if (countAfter == countBefore)
            {
                DragonWaterDebug.LogWarning("No contact points added. Did you set up plane properly?");
            }
        }
        private void ClearSamplesAtHeight()
        {
            var body = (FloatingBody)target;
            var center = body.transform.position + _raycastPosition;
            var matrixTo = body.transform.localToWorldMatrix;

            for (int i=0; i<body.contactPoints.Length; i++)
            {
                var position = matrixTo.MultiplyPoint3x4(body.contactPoints[i]);

                if (Mathf.Abs(position.y - center.y) < 0.01f)
                {
                    var list = new List<Vector3>(body.contactPoints);
                    list.RemoveAt(i);
                    body.contactPoints = list.ToArray();
                    i--;
                    continue;
                }
            }
        }

        private List<Vector3> GetRaycastTargets()
        {
            var center = ((FloatingBody)target).transform.position + _raycastPosition;
            var targets = new List<Vector3>(_raycastCount);

            var angle = (Mathf.PI * 2.0f) / _raycastCount;
            var offset = angle * _raycastOffset;

            for (int i = 0; i < _raycastCount; i++)
            {
                targets.Add(center + new Vector3(
                    Mathf.Sin(offset + angle * i) * _raycastRadius,
                    0,
                    Mathf.Cos(offset + angle * i) * _raycastRadius
                    ));
            }

            return targets;
        }
    }
}
#endif