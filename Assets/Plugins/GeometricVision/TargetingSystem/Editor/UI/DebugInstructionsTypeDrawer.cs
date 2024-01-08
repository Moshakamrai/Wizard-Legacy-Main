using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using Plugins.GeometricVision.TargetingSystem.BaseCode.Debugging;
using UnityEditor;
using UnityEngine;

namespace Plugins.GeometricVision.TargetingSystem.Editor.UI
{
    
    /// <summary>
    /// Draws the targeting instruction that is visible to the user.
    /// </summary>
    [CustomPropertyDrawer(typeof(TargetingDebugInstruction))]
    public class DebuggingInstructionsTypeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label.text = "" + property.displayName;
            label = EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var intend = EditorGUI.indentLevel;
            float offsetX = 0;
            float positionX = position.x + offsetX;
            EditorGUI.indentLevel = 0;
            var offset = position.height / 2 - 6;
            
            DrawEnumField();

            EditorGUI.indentLevel = intend;
            EditorGUI.EndProperty();
            
            //
            //Locals
            //
#pragma warning disable CS8321
            void DrawBooleanField()
            {
                var labelRect = new Rect(positionX + 70, position.y - offset, 111, position.height);
                var visualizeEnabledTargetingRect = new Rect(positionX + 205, position.y - offset, 70, position.height);
                EditorGUI.LabelField(labelRect, "Enabled:");
                var prop = property.FindPropertyRelative("visualizeTargetingInEditorView");
                EditorGUI.PropertyField(visualizeEnabledTargetingRect, prop, GUIContent.none);
            }
            
            void DrawEnumField()
            {
                var visualizeTargetingRectRect = new Rect(positionX , position.y - offset, 111, position.height);
                var visualizeTargetingRectRect2 = new Rect(positionX + 120, position.y, 111, position.height);
                EditorGUI.LabelField(visualizeTargetingRectRect, "Visualization Mode:");
                var prop2 = property.FindPropertyRelative("visualizationMode");

                prop2.intValue = (int) (TargetingSystemDataModels.VisualizationMode) EditorGUI.EnumPopup(visualizeTargetingRectRect2,
                    (TargetingSystemDataModels.VisualizationMode) prop2.intValue);
            }
        }
    }
}