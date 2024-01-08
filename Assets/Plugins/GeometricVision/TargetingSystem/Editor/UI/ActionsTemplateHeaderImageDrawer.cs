using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using Plugins.GeometricVision.TargetingSystem.BaseCode.UtilitiesAndPlugins;
using Plugins.GeometricVision.TargetingSystem.Code;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins;
using UnityEditor;
using UnityEngine;

namespace Plugins.GeometricVision.TargetingSystem.Editor.UI
{

    [CustomEditor(typeof(TargetingActionsTemplateObject))]
    internal class ActionsTemplateHeaderImageDrawer : UnityEditor.Editor
    {
        private Texture headerTexture;
        private float textureBottomSpaceExtension = 50f;
        private float ratioMultiplier;
        
        public override void OnInspectorGUI()
        {
            if (this.headerTexture == null)
            {
                this.headerTexture = TargetingSystemUtilities.LoadPNG(Application.dataPath+ TargetingSystemSettings.HeaderImagePath);
            }
            
            DrawTexture();
            this.DrawDefaultInspector ();

            void DrawTexture()
            {
                GUILayout.Label("Geometric vision actions template");
                this.ratioMultiplier = (float) this.headerTexture.height / (float) this.headerTexture.width;
                EditorGUI.DrawPreviewTexture(
                    new Rect(25, 60, EditorGUIUtility.currentViewWidth, EditorGUIUtility.currentViewWidth * this.ratioMultiplier), this.headerTexture);
                GUILayout.Space(EditorGUIUtility.currentViewWidth * this.ratioMultiplier + this.textureBottomSpaceExtension);
            }
        }
    }
}