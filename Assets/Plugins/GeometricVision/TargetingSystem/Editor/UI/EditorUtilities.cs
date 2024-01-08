using UnityEditor;
using UnityEngine;

namespace Plugins.GeometricVision.TargetingSystem.Editor.UI
{
    internal static class EditorUtilities
    {
        private static string previousTag;
        private static string currentTag;
        private static string renderTag;        

        internal static void DrawTagField(SerializedProperty property, Rect tagTextRect, Rect tagRect, string fieldName)
        {
            GUIContent label2 = new GUIContent("");
            EditorGUI.LabelField(tagTextRect, "Tag:");
            currentTag = EditorGUI.TagField(tagRect, label2, property.FindPropertyRelative(fieldName).stringValue);

            if (previousTag != currentTag)
            {
                renderTag = currentTag;
            }

            previousTag = EditorGUI.TagField(tagRect, label2, renderTag);

            property.FindPropertyRelative(fieldName).stringValue = renderTag;
          
        }

        internal static void DrawEntityFilterField(SerializedProperty property, Rect labelRect, Rect entityFilterRect, string fieldName)
        {
            EditorGUI.LabelField(labelRect, "Entity filter:");
            var prop = property.FindPropertyRelative(fieldName);
            EditorGUI.PropertyField(entityFilterRect, prop, GUIContent.none);
        }

        public static void DrawGeometryTypeField(SerializedProperty property, Rect geometryTypeTextRect, Rect geometryTypeRect, string fieldName)
        {
            EditorGUI.LabelField(geometryTypeTextRect, "Geometry type:");
            var prop = property.FindPropertyRelative(fieldName);
            EditorGUI.PropertyField(geometryTypeRect, prop, GUIContent.none);
        }
    }

}
