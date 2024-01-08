using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using Plugins.GeometricVision.TargetingSystem.Code;
using UnityEditor;
using UnityEngine;

namespace Plugins.GeometricVision.TargetingSystem.Editor.UI
{
    [CustomPropertyDrawer(typeof(ActionsTemplateTriggerActionElement))]
    public class ActionsTemplateActionElementDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var intend = EditorGUI.indentLevel;
            float offsetX = -70;
            float positionX = position.x + offsetX;
            EditorGUI.indentLevel = 0;
            var offset = position.height / 2 - 6;
            label.text = "" ;
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            //Names for each element properties
            string enabledFieldName = "enabled";
            string startDelayFieldName = "startDelay";
            string durationFieldName = "duration";
            string prefabFieldName = "prefab";
            string spawnAtSourceFieldName = "spawnAtSource";
            string spawnAtTargetFieldName = "spawnAtTarget";
            string unParentFromSourceFieldName = "unParent";
            string spawnAsEntityFieldName = "spawnAsEntity";
            string entityFilterFieldName = "entityFilter";
            string gameObjectTagFieldName = "gameObjectTag";
            
            //Width for each element properties
            float startActionEnabledRectTextWidth = enabledFieldName.Length * 8;
            float startDelayTextWidth = startDelayFieldName.Length * 8;
            float durationTextWidth = durationFieldName.Length * 8;
            float prefabTextWidth = prefabFieldName.Length * 8;
            float spawnAtSourceTextWidth = spawnAtSourceFieldName.Length * 8;
            float spawnAtTargetTextWidth = spawnAtTargetFieldName.Length * 8;
            float unParentFromSourceTextWidth = unParentFromSourceFieldName.Length * 8;
            float spawnAsEntityTextWidth = spawnAsEntityFieldName.Length * 8;
            float entityFilterFieldTextWidth = entityFilterFieldName.Length * 8;
            float gameObjectTagFieldTextWidth = gameObjectTagFieldName.Length * 8;
            float floatFieldWidth = 45;
            float boolFieldWidth = 35;
            float prefabFieldWidth = 95;
            float totalWidth = 0;


            var labelRect = new Rect(positionX + 70, position.y - offset, startActionEnabledRectTextWidth, position.height);
            var startActionEnabledRect = new Rect(positionX + 125, position.y - offset, boolFieldWidth, position.height);


            totalWidth = labelRect.position.x + labelRect.width + startActionEnabledRect.width +10;
            
            var startDelayLabel = new Rect(totalWidth, position.y - offset, startDelayTextWidth, position.height);
            var startDelayRect = new Rect(totalWidth + startDelayLabel.width, position.y, floatFieldWidth,
                position.height);

            totalWidth = startDelayLabel.x + startDelayLabel.width + startDelayRect.width +10;
            
            var labelDuration = new Rect(totalWidth, position.y - offset, durationTextWidth, position.height);
            var durationRect = new Rect(totalWidth + labelDuration.width , position.y, floatFieldWidth , position.height);

            totalWidth = labelDuration.x + labelDuration.width + durationRect.width +10;
            
            var actionPrefabRect = new Rect(totalWidth, position.y - offset, prefabTextWidth, position.height);
            var prefabRect = new Rect(totalWidth + actionPrefabRect.width , position.y, prefabFieldWidth,
                position.height);

            totalWidth = prefabRect.x + prefabRect.width +10;
            
            var spawnAtSourceRect = new Rect(totalWidth, position.y - offset, spawnAtSourceTextWidth, position.height);
            var spawnAtSourceBoolRect = new Rect(totalWidth + spawnAtSourceRect.width , position.y, boolFieldWidth,
                position.height);

            totalWidth = spawnAtSourceBoolRect.x + spawnAtSourceBoolRect.width +10;
            
            var spawnAtTargetRect = new Rect(totalWidth, position.y - offset, spawnAtTargetTextWidth, position.height);
            var spawnAtTargetBoolRect = new Rect(totalWidth + spawnAtTargetRect.width, position.y,boolFieldWidth ,
                position.height);

            totalWidth = spawnAtTargetBoolRect.x + spawnAtTargetBoolRect.width +10;
            
            var unParentFromSourceRect = new Rect(totalWidth, position.y - offset, unParentFromSourceTextWidth +8, position.height);
            var unParentFromSourceBoolRect = new Rect(totalWidth + unParentFromSourceRect.width -10, position.y,boolFieldWidth ,
                position.height);

            totalWidth = unParentFromSourceBoolRect.x + unParentFromSourceBoolRect.width +10;
            
            var spawnAsEntitiesTextRect = new Rect(totalWidth, position.y - offset, spawnAsEntityTextWidth, position.height);
            var spawnAsEntitiesBoolRect = new Rect(totalWidth + spawnAsEntitiesTextRect.width, position.y,boolFieldWidth ,
                position.height);
#if ENABLE_TARGETING_SYSTEM_ENTITY_SUPPORT   
            totalWidth = spawnAsEntitiesBoolRect.x + spawnAsEntitiesBoolRect.width +10;
            var gameObjectTagTextRect = new Rect(totalWidth, position.y - offset, "Tag: ".Length*8, position.height);
            var gameObjectTagEnumRect = new Rect(totalWidth + gameObjectTagTextRect.width, position.y,gameObjectTagFieldTextWidth ,
                position.height);
 #else
            var gameObjectTagTextRect = new Rect(totalWidth, position.y - offset, "Tag: ".Length*8, position.height);
            var gameObjectTagEnumRect = new Rect(totalWidth + gameObjectTagTextRect.width, position.y,gameObjectTagFieldTextWidth ,
                position.height);
 #endif
            totalWidth = spawnAsEntitiesBoolRect.x + spawnAsEntitiesBoolRect.width +10;
            
            var entityFilterTextRect = new Rect(totalWidth, position.y - offset, "Entity filter: ".Length*5, position.height);
            var entityFilterObjectRect = new Rect(totalWidth + entityFilterTextRect.width, position.y, entityFilterFieldTextWidth ,
                position.height);
            
            
            EditorGUI.LabelField(labelRect, "Enabled:");
            var prop = property.FindPropertyRelative(enabledFieldName);

            EditorGUI.PropertyField(startActionEnabledRect, prop, GUIContent.none);

            EditorGUI.LabelField(startDelayLabel, "startDelay:");
            property.FindPropertyRelative("startDelay").floatValue = EditorGUI.FloatField(startDelayRect,
                property.FindPropertyRelative("startDelay").floatValue);

            EditorGUI.LabelField(labelDuration, "duration:");
            property.FindPropertyRelative("duration").floatValue = EditorGUI.FloatField(durationRect,
                property.FindPropertyRelative("duration").floatValue);

            EditorGUI.LabelField(actionPrefabRect, "Prefab:");
            EditorGUI.PropertyField(prefabRect, property.FindPropertyRelative("prefab"), GUIContent.none);
            
            EditorGUI.LabelField(spawnAtSourceRect, "Spawn at source:");
            EditorGUI.PropertyField(spawnAtSourceBoolRect, property.FindPropertyRelative(spawnAtSourceFieldName), GUIContent.none);
            
            EditorGUI.LabelField(spawnAtTargetRect, "Spawn at target:");
            EditorGUI.PropertyField(spawnAtTargetBoolRect, property.FindPropertyRelative(spawnAtTargetFieldName), GUIContent.none);

            EditorGUI.LabelField(unParentFromSourceRect, "Unparent:");
            EditorGUI.PropertyField(unParentFromSourceBoolRect, property.FindPropertyRelative(unParentFromSourceFieldName), GUIContent.none);
#if ENABLE_TARGETING_SYSTEM_ENTITY_SUPPORT
            EditorGUI.LabelField(spawnAsEntitiesTextRect, "Spawn as entity:");
            EditorGUI.PropertyField(spawnAsEntitiesBoolRect, property.FindPropertyRelative(spawnAsEntityFieldName), GUIContent.none);
#else
            
#endif
            if (property.FindPropertyRelative(spawnAsEntityFieldName).boolValue == true)
            {
#if ENABLE_TARGETING_SYSTEM_ENTITY_SUPPORT
                EditorUtilities.DrawEntityFilterField(property, entityFilterTextRect, entityFilterObjectRect, entityFilterFieldName);
#endif
            }
            else
            {
                EditorUtilities.DrawTagField(property, gameObjectTagTextRect, gameObjectTagEnumRect, gameObjectTagFieldName);

            }

            EditorGUI.indentLevel = intend;
            EditorGUI.EndProperty();
        }
    }
}