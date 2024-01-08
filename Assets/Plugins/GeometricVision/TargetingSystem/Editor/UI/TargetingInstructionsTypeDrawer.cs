using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using Plugins.GeometricVision.TargetingSystem.BaseCode.GameObjects.ImplementationsGameObjects;
using Plugins.GeometricVision.TargetingSystem.BaseCode.MainClasses;
using Plugins.GeometricVision.TargetingSystem.Code;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace Plugins.GeometricVision.TargetingSystem.Editor.UI
{
 
    /// <summary>
    /// Draws the targeting instruction that is visible to the user.
    /// </summary>
    [CustomPropertyDrawer(typeof(TargetingInstruction))]
    public class TargetingInstructionsTypeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label.text = "Targeting instruction: " + property.displayName;
            label = EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            float charWidth = 5;
            var intend = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            float offsetX = -220;
            float positionX = position.x + offsetX;

#if ENABLE_TARGETING_SYSTEM_ENTITY_SUPPORT
            offsetX = -70;
            positionX = position.x + offsetX;
            var labelRect = new Rect(positionX +70, position.y , 70, position.height);
            var entityFilterRect = new Rect(positionX + 145, position.y , 70, position.height);
#endif

            positionX = positionX + 230;
            var geometryTypeTextRect = new Rect(positionX, position.y , 111, position.height);
            positionX += ("Geometry type: ".Length * charWidth) + 20;
            var geometryTypeRect = new Rect(positionX, position.y, 111, position.height);
            positionX += geometryTypeRect.width +20;
            var tagTextRect = new Rect(positionX, position.y , 111, position.height);
            positionX += ("Tag:".Length * charWidth)  +20;
            var tagRect = new Rect(positionX, position.y, 111, position.height);
            positionX += tagRect.width + 20;
            var triggerActionsLabel = new Rect(positionX, position.y , 140, position.height);
            positionX += ("Trigger actions: ".Length * charWidth) +20;
            var targetingActions = new Rect(positionX , position.y, 240, position.height);
            EditorUtilities.DrawGeometryTypeField(property, geometryTypeTextRect, geometryTypeRect, "geometryType");
#if ENABLE_TARGETING_SYSTEM_ENTITY_SUPPORT
            EditorUtilities.DrawEntityFilterField(property, labelRect, entityFilterRect, "entityQueryFilter");
#endif

            EditorUtilities.DrawTagField(property, tagTextRect, tagRect, "targetTag");
            DrawTriggerActions(property, triggerActionsLabel, targetingActions);


            EditorGUI.indentLevel = intend;
            EditorGUI.EndProperty();
        }


        private static void DrawTriggerActions(SerializedProperty property, Rect labelRectOnTargetFound, Rect targetingActions)
        {
            EditorGUI.LabelField(labelRectOnTargetFound, "Trigger actions:");
            EditorGUI.PropertyField(targetingActions, property.FindPropertyRelative("targetingActions"),
                GUIContent.none);
        }
    }
}