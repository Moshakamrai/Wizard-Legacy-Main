#if ENABLE_TARGETING_SYSTEM_ENTITY_SUPPORT
using Unity.Entities;
#endif

using System;
using Plugins.GeometricVision.TargetingSystem.BaseCode.MainClasses;
using Plugins.GeometricVision.TargetingSystem.BaseCode.UtilitiesAndPlugins;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels
{
    [Serializable]
    public class ActionsTemplateTriggerActionElement
    {
        [SerializeField] private bool enabled = false;
#if ENABLE_TARGETING_SYSTEM_ENTITY_SUPPORT == FALSE
        [HideInInspector]
#endif
        [SerializeField] private bool spawnAsEntity = false;

        [SerializeField, Tooltip("Start delay for instantiation of Prefab")]
        private float startDelay = 0;

        [SerializeField, Tooltip("timeLeft/lifeTime for instantiated Prefab")]
        private float duration = 0;

        [SerializeField, Tooltip("Prefab containing animation or visualisation for spawned item")]
        private GameObject prefab;

        [SerializeField] private bool spawnAtSource = true;

        [SerializeField] private bool spawnAtTarget = false;

        [FormerlySerializedAs("unParentFromSource")] [SerializeField] private bool unParent = true;
        [SerializeField] private Object entityFilter = new Object();
        [SerializeField] private string gameObjectTag = TargetingSystemSettings.TriggerActionDefaultTag;

        private GV_TargetingSystem gvTargetingSystem = null;
        private AudioClip audioClip = null;
        private float audioVolume = 1;
        //How much 3d audio effect to blend in
        private float audioSpatialBlend =1;
        [SerializeField] private string entityQueryFilterNameSpace = "";
        [SerializeField] private string entityQueryFilterName = "";
        [SerializeField] private Type entityFilterComponentType;

        [SerializeField,HideInInspector] 
        private TargetingInstruction targetingInstruction;
        public ActionsTemplateTriggerActionElement(bool enabled, float startDelay, float duration, GameObject prefab,
            bool spawnAtSource, bool spawnAtTarget, bool unParent, GV_TargetingSystem gvTargetingSystem, AudioClip audioClip, float audioVolume, float audioSpatialBlend, string defaultTag, TargetingInstruction targetingInstruction)
        {
            this.enabled = enabled;
            this.startDelay = startDelay;
            this.duration = duration;
            this.prefab = prefab;
            this.spawnAtSource = spawnAtSource;
            this.spawnAtTarget = spawnAtTarget;
            this.unParent = unParent;
            this.gvTargetingSystem = gvTargetingSystem;
            this.audioClip = audioClip;
            this.audioVolume = audioVolume;
            this.audioSpatialBlend = audioSpatialBlend;
            this.TargetingInstruction = targetingInstruction;
            this.gameObjectTag = defaultTag;
        }
        
        public ActionsTemplateTriggerActionElement(bool enabled, float startDelay, float duration, GameObject prefab,
            bool spawnAtSource, bool spawnAtTarget, bool unParent, GV_TargetingSystem gvTargetingSystem, AudioClip audioClip, float audioVolume, float audioSpatialBlend, TargetingInstruction targetingInstruction, Type entityFilterComponentType, Object entityFilter)
        {
            this.enabled = enabled;
            this.startDelay = startDelay;
            this.duration = duration;
            this.prefab = prefab;
            this.spawnAtSource = spawnAtSource;
            this.spawnAtTarget = spawnAtTarget;
            this.unParent = unParent;
            this.gvTargetingSystem = gvTargetingSystem;
            this.audioClip = audioClip;
            this.audioVolume = audioVolume;
            this.audioSpatialBlend = audioSpatialBlend;
            this.TargetingInstruction = targetingInstruction;
            this.entityFilterComponentType = entityFilterComponentType;
            this.entityFilter = entityFilter;
        }
        
        public ActionsTemplateTriggerActionElement(ActionsTemplateTriggerActionElement elementParams,
            TargetingInstruction targetingInstructionIn)
        {
            this.enabled = elementParams.enabled;
            this.startDelay = elementParams.startDelay;
            this.duration = elementParams.duration;
            this.prefab = elementParams.prefab;
            this.spawnAtSource = elementParams.spawnAtSource;
            this.spawnAtTarget = elementParams.spawnAtTarget;
            this.unParent = elementParams.unParent;
            this.gvTargetingSystem = elementParams.gvTargetingSystem;
            this.audioClip = elementParams.audioClip;
            this.audioVolume = elementParams.audioVolume;
            this.audioSpatialBlend = elementParams.audioSpatialBlend;
            if (targetingInstructionIn != null && Application.isPlaying)
            {
                this.targetingInstruction = targetingInstructionIn;
            }
            this.gameObjectTag = elementParams.GameObjectTag;
            if (elementParams.entityFilterComponentType != null)
            {
                this.entityFilterComponentType = elementParams.entityFilterComponentType;
            }
            if (elementParams.entityFilter != null)
            {
                this.entityFilter = elementParams.entityFilter;
            }

            this.spawnAsEntity = elementParams.spawnAsEntity;
        }
        
        public ActionsTemplateTriggerActionElement(TargetingInstruction targetingInstructionIn)
        {
            if (targetingInstructionIn != null )
            {
                this.TargetingInstruction = targetingInstructionIn;
            }
        }
        
        public void OnBeforeSerialize()
        {
            if (this.entityFilter != null || this.entityFilter.name != "")
            {
                this.entityFilterComponentType = TargetingSystemUtilities.GetCurrentEntityFilterType(this.entityFilter);
                this.entityQueryFilterNameSpace  = this.entityFilterComponentType.Namespace;
                this.entityQueryFilterName = this.entityFilterComponentType.Name;
            }
        }

        public void OnAfterDeserialize()
        {
            if (this.entityFilter != null && this.entityFilter.name != "")
            {
                this.entityFilterComponentType =
                    Type.GetType(string.Concat(this.entityQueryFilterNameSpace, ".", this.entityQueryFilterName));
            }
        }
        
        public float StartDelay
        {
            get { return this.startDelay; }
            set { this.startDelay = value; }
        }

        public float Duration
        {
            get { return this.duration; }
            set { this.duration = value; }
        }

        public GameObject Prefab
        {
            get { return this.prefab; }
            set { this.prefab = value; }
        }

        public bool Enabled
        {
            get { return this.enabled; }
            set { this.enabled = value; }
        }

        public bool SpawnAtTarget
        {
            get { return this.spawnAtTarget; }
            set { this.spawnAtTarget = value; }
        }

        public bool SpawnAtSource
        {
            get { return this.spawnAtSource; }
            set { this.spawnAtSource = value; }
        }

        public string Name { get; set; }

        public bool UnParent
        {
            get { return this.unParent; }
            set { this.unParent = value; }
        }

        public bool SpawnAsEntity
        {
            get { return this.spawnAsEntity; }
            set { this.spawnAsEntity = value; }
        }

        public float AudioVolume
        {
            get { return this.audioVolume; }
            set { this.audioVolume = value; }
        }

        public float AudioSpatialBlend
        {
            get { return this.audioSpatialBlend; }
            set { this.audioSpatialBlend = value; }
        }

        public AudioClip AudioClipToUse
        {
            get { return this.audioClip; }
            set { this.audioClip = value; }
        }

        public Object EntityFilter
        {
            get { return this.entityFilter; }
            set { this.entityFilter = value; }
        }

        public string GameObjectTag
        {
            get { return this.gameObjectTag; }
            set { this.gameObjectTag = value; }
        }

        public TargetingInstruction TargetingInstruction
        {
            get
            {
                return this.targetingInstruction;
            }
            set
            {
                this.targetingInstruction = value;
            }
        }

        public Type EntityFilterComponentType
        {
            get { return this.entityFilterComponentType; }
            set { this.entityFilterComponentType = value; }
        }

        public string EntityQueryFilterName
        {
            get { return this.entityQueryFilterName; }
            set { this.entityQueryFilterName = value; }
        }

        public string EntityQueryFilterNameSpace
        {
            get { return this.entityQueryFilterNameSpace; }
            set { this.entityQueryFilterNameSpace = value; }
        }
    }
}