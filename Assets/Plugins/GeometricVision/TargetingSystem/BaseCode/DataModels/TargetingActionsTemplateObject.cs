using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.BaseCode.MainClasses;
using UnityEngine;

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels
{
    [CreateAssetMenu(fileName = "Actions", menuName = "ScriptableObjects/GeometricVision/ActionsForTargeting",
        order = 1)]
    public class TargetingActionsTemplateObject : ScriptableObject
    {
        [SerializeField, Tooltip("User given parameters as set of targeting instructions")]
        private List<ActionsTemplateTriggerActionElement> triggerActionElements = new List<ActionsTemplateTriggerActionElement>();
        private GV_TargetingSystem targetingSystem;
        private bool initDone = false;
        
        public List<ActionsTemplateTriggerActionElement> TriggerActionElements
        {
            get { return this.triggerActionElements; }
            private set { this.triggerActionElements = value; }
        }

        public GV_TargetingSystem TargetingSystem
        {
            get { return this.targetingSystem; }
            set { this.targetingSystem = value; }
        }
        
        private void Awake()
        {
            if (this.TriggerActionElements == null)
            {
                this.TriggerActionElements = new List<ActionsTemplateTriggerActionElement>();
            }
        }

        private void OnEnable()
        {
            this.initDone = false;
        }

        public void InitActionElementsForTargetingSystem(GV_TargetingSystem gvTargetingSystem)
        {
            if (this.initDone)
            {
                ApplyEffectDataFromTriggerActionElements(gvTargetingSystem);
                return;
            }

            this.initDone = true;
            ApplyEffectDataFromTriggerActionElements(gvTargetingSystem);

            void ApplyEffectDataFromTriggerActionElements(GV_TargetingSystem gvTargetingSystem1)
            {
                foreach (var triggerActionElement in this.TriggerActionElements)
                {
                    if (triggerActionElement.Prefab != null)
                    {
                        triggerActionElement.Enabled = true;
                    }
                    
                    //Remove audio source since we use pooled one initiated from the TargetingSystemsRunner
                    if (triggerActionElement.AudioClipToUse != null)
                    {
                        triggerActionElement.Prefab.GetComponent<AudioSource>().enabled = false;
                    }
                }
            }
        }
        
        void OnValidate()
        {
            if (this.targetingSystem == null)
            {
                return;
            }

            foreach (var triggerActionElement in this.TriggerActionElements)
            {
                triggerActionElement.StartDelay = Mathf.Clamp(triggerActionElement.StartDelay, 0, float.MaxValue);
                triggerActionElement.Duration = Mathf.Clamp(triggerActionElement.Duration, 0, float.MaxValue);

                triggerActionElement.AudioClipToUse = this.TryExtractAndAssignAudioClipFromSource(triggerActionElement);

                triggerActionElement.Enabled = triggerActionElement.Prefab;
                if (triggerActionElement.Prefab == null)
                {
                    Debug.Log("You need to have prefab assigned to enable this effect. " + this.name);
                }
            }
        }

        private AudioClip TryExtractAndAssignAudioClipFromSource(
            ActionsTemplateTriggerActionElement triggerActionElement)
        {
            var audioSourceFromPrefab = triggerActionElement.Prefab.GetComponent<AudioSource>();
            if (audioSourceFromPrefab != null && audioSourceFromPrefab.clip != null)
            {
                triggerActionElement.AudioVolume = audioSourceFromPrefab.volume;
                triggerActionElement.AudioSpatialBlend = audioSourceFromPrefab.spatialBlend;
                return audioSourceFromPrefab.clip;
            }

            return null;
        }
    }
}