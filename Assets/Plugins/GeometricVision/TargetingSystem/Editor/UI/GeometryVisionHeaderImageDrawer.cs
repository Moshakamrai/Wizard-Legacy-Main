using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using Plugins.GeometricVision.TargetingSystem.BaseCode.MainClasses;
using Plugins.GeometricVision.TargetingSystem.BaseCode.UtilitiesAndPlugins;
using Plugins.GeometricVision.TargetingSystem.Code;
using Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins;
using UnityEditor;
using UnityEngine;

namespace Plugins.GeometricVision.TargetingSystem.Editor.UI
{
    [CustomEditor(typeof(GV_TargetingSystem)), CanEditMultipleObjects]
    internal class TargetingSystemHeaderImageDrawer : UnityEditor.Editor
    {
        private Texture headerTexture;
        private float textureBottomSpaceExtension = 5f;
        private float textureImageRatio;

        public override void OnInspectorGUI()
        {
            if (this.headerTexture == null)
            {
                this.headerTexture =
                    TargetingSystemUtilities.LoadPNG(Application.dataPath + TargetingSystemSettings.HeaderImagePath);
            }

            DrawTexture();
            this.DrawDefaultInspector();

            if (GUILayout.Button("Create a new actions template for targeting."))
            {
                var newActionsTemplate = CreateInstance<TargetingActionsTemplateObject>();
                newActionsTemplate.name = newActionsTemplate.name;

                AssetDatabase.CreateAsset(newActionsTemplate, TargetingSystemSettings.NewActionsAssetForTargetingPath);
                AssetDatabase.SaveAssets();

                EditorUtility.FocusProjectWindow();

                Selection.activeObject = newActionsTemplate;
            }

            var go = Selection.activeGameObject;


            if (go != null && go.GetComponent<GV_TargetingSystem>() != null)
            {

                var camera = go.GetComponent<Camera>();
                if (camera != null)
                {
                    go.GetComponent<Camera>().hideFlags = HideFlags.HideInInspector;
                }
                
            }

            void DrawTexture()
            {
                this.textureImageRatio = (float) this.headerTexture.height / (float) this.headerTexture.width;
                EditorGUI.DrawPreviewTexture(
                    new Rect(0, 0, EditorGUIUtility.currentViewWidth,
                        EditorGUIUtility.currentViewWidth * this.textureImageRatio), this.headerTexture);
                GUILayout.Space(EditorGUIUtility.currentViewWidth * this.textureImageRatio + this.textureBottomSpaceExtension);
            }
        }
    }
}