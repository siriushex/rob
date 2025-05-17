#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DragonWater.Editor
{
    internal static class EditorUtilityEx
    {
        public static Rect GetLine(this Rect rect, float lineHight, int line)
        {
            return new Rect(rect.position + new Vector2(0.0f, lineHight * line), new Vector2(rect.size.x, lineHight));
        }
        public static Rect GetLine(this Rect rect, int line)
        {
            return new Rect(rect.position + new Vector2(0.0f, EditorGUIUtility.singleLineHeight * line), new Vector2(rect.size.x, EditorGUIUtility.singleLineHeight));
        }

        public static void DrawSeparatorLine(Color color, int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }
    }
}
#endif