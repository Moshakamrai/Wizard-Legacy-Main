using System.Reflection;
using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using Plugins.GeometricVision.TargetingSystem.BaseCode.MainClasses;
using UnityEditor;
using UnityEngine;

namespace Plugins.GeometricVision.TargetingSystem.Editor.UI
{
    /// <summary>
    /// Draws the targeting instruction that is visible to the user.
    /// </summary>
    [CustomPropertyDrawer(typeof(GV_TargetingSystem))]
    public class CullOptionsDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var prop2 = property.FindPropertyRelative("cullingBehaviour");
            if (prop2 != null)
            {
                label.text = "Cull targets options: " + property.displayName;
                EditorGUI.BeginProperty(position, label, property);
                position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

                var intend = EditorGUI.indentLevel;
                float offsetX = -70;
                float positionX = position.x + offsetX;
                EditorGUI.indentLevel = 0;
                var offset = position.height / 2 - 6;
                var labelRect = new Rect(positionX + 70, position.y - offset, 70, position.height);
                var cullingBehaviourRect = new Rect(positionX + 145, position.y - offset, 70, position.height);

                EditorGUI.LabelField(labelRect, "Behaviour/type:");
                prop2.intValue = (int) (TargetingSystemDataModels.CullingBehaviour) EditorGUI.EnumPopup(cullingBehaviourRect,
                    (TargetingSystemDataModels.CullingBehaviour) prop2.intValue);
                
                EditorGUI.indentLevel = intend;
                EditorGUI.EndProperty();
            }
            else
            {
                MethodInfo defaultDraw = typeof(EditorGUI).GetMethod("DefaultPropertyField", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                defaultDraw.Invoke(null, new object[3] { position, property, label });
            }
        }
    }
}