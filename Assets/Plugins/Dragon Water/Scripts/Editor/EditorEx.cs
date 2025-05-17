#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DragonWater.Editor
{
    internal abstract class EditorEx : UnityEditor.Editor
    {
        internal static readonly int[] TEXTURE_SIZES = new[] { 64, 128, 256, 512, 1024, 2048, 4096 };
        internal static readonly string[] TEXTURE_SIZES_STR = TEXTURE_SIZES.Select(v => v.ToString()).ToArray();

        static Dictionary<string, bool> _foldouts = new();
        static Stack<Color> _guiColors = new();

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            PushGUITint(1,1,1);
            CheckURPRenderFeature();
            DrawInspector();
            PopGUITint();

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
                EditorUtility.SetDirty(target);
                OnChanges();
            }

            if (DragonWaterManager.Instance.Config.drawDebugInspectors)
            {
                GUI.enabled = false;
                EditorGUILayout.Space();
                DrawTitle("Debug");
                DrawInspectorDebug();
                GUI.enabled = true;
            }

            if (ConstantSceneUpdate() && !Application.isPlaying)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
                Repaint();
            }
        }

        private void CheckURPRenderFeature()
        {
            if (URP.DragonWaterRenderFeature.CheckCurrentInstallation(out var rendererData))
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.HelpBox("Your main URP pipeline renderer is missing Dragon Water Render Feature!", MessageType.Error);
            if (GUILayout.Button("Select Renderer Asset"))
                EditorGUIUtility.PingObject(rendererData);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
        }


        protected abstract void DrawInspector();
        protected virtual void DrawInspectorDebug() { }
        protected virtual void OnChanges() { }


        protected virtual bool ConstantSceneUpdate()
        {
            return false;
        }

        protected void DrawTitle(string title)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorUtilityEx.DrawSeparatorLine(Color.grey);
        }

        protected void DrawFoldoutSection(string key, string title, Action onDraw, bool drawSeparator = true)
        {
            if (!_foldouts.ContainsKey(key)) _foldouts[key] = false;

            _foldouts[key] = EditorGUILayout.BeginFoldoutHeaderGroup(_foldouts[key], title);
            EditorGUILayout.EndFoldoutHeaderGroup();
            if (_foldouts[key]) onDraw();
            if (drawSeparator) EditorUtilityEx.DrawSeparatorLine(Color.gray);
        }
        protected void DrawToggleSection(string key, string title, Action onDraw, bool drawSeparator = true)
        {
            if (!_foldouts.ContainsKey(key)) _foldouts[key] = false;

            _foldouts[key] = EditorGUILayout.ToggleLeft(title, _foldouts[key]);
            if (_foldouts[key]) onDraw();
            if (drawSeparator) EditorUtilityEx.DrawSeparatorLine(Color.gray);
        }
        protected bool IsFoldoutExpanded(string key)
        {
            if (!_foldouts.ContainsKey(key)) return false;
            return _foldouts.ContainsKey(key);
        }


        protected void PushGUITint(float r, float g, float b)
        {
            _guiColors.Push(GUI.color);
            GUI.color *= new Color(r,g,b);
        }
        protected void PopGUITint()
        {
            GUI.color = _guiColors.Pop();
        }
        protected void RepushGUITint(float r, float g, float b)
        {
            GUI.color = _guiColors.Peek() * new Color(r,g,b);
        }


        protected void PropertyField(SerializedProperty property)
        {
            EditorGUILayout.PropertyField(property);
        }
        protected void PropertyField(SerializedProperty property, string label)
        {
            EditorGUILayout.PropertyField(property, label == null ? GUIContent.none : new GUIContent(label, property.tooltip));
        }
    }
}
#endif