using System;
using Plugins.GeometricVision.TargetingSystem.BaseCode.Interfaces;
using Plugins.GeometricVision.TargetingSystem.BaseCode.UtilitiesAndPlugins;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels
{
    /// <summary>
    /// Contains user defined targeting instructions for the GV_TargetingSystem object
    /// </summary>
    [Serializable]
    public class TargetingInstruction: ISerializationCallbackReceiver
    {
        [SerializeField, Tooltip("Choose what geometry to target or use. Default is Objects")]
        private GeometryType geometryType = GeometryType.Objects;

        [SerializeField] private bool isTargetingEnabled = false;

        [SerializeField, Tooltip("Choose what tag from unity tags settings to use")]
        private string targetTag;
        

        //GV_TargetingSystem plugin needs to be able to target both GameObjects and Entities at the same time
        private ITargetProcessor targetProcessorForGameObjects = null; //TODO:consider: remove these for 2.0
        private ITargetProcessor targetProcessorEntities = null; //TODO:same

        [SerializeField]
        internal Object entityQueryFilter;
        [SerializeField] private string entityQueryFilterName;
        [SerializeField] private string entityQueryFilterNameSpace;
        [SerializeField] private Type entityFilterComponentType;
        [FormerlySerializedAs("targetingTargetingActions")] [SerializeField] private TargetingActionsTemplateObject targetingActions;
        private bool needsUpdate;
        
        /// <summary>
        /// Constructor for the GV_TargetingSystem targeting instructions object
        /// </summary>
        /// <param name="geoType"></param>
        /// <param name="tagName"></param>
        /// <param name="targetingSystem">Item1 entity targeting system, Item2 GameObject targeting system<</param>
        /// <param name="targetingEnabled"></param>
        /// <param name="entityQueryFilter"></param>
        public TargetingInstruction(GeometryType geoType, string tagName,
            ITargetProcessor targetingSystem, bool targetingEnabled)
        {
            this.GeometryType = geoType;

            if (this.TargetTag == null && tagName == null)
            {
                this.TargetTag = "Untagged";
            }
            else
            {
                this.TargetTag = tagName;
            }

            this.entityFilterComponentType = TargetingSystemUtilities.GetCurrentEntityFilterType(this.entityQueryFilter);

            this.isTargetingEnabled = targetingEnabled;

            this.AssignTargetProcessor(targetingSystem);
            if (this.targetingActions == null)
            {
                this.TargetingActions = ScriptableObject.CreateInstance<TargetingActionsTemplateObject>();
            }

            this.needsUpdate = true;
        }

        /// <summary>
        /// Constructor overload for the GV_TargetingSystem targeting instruction object.
        /// Accepts factory settings as parameter.
        /// Easier to pass multiple parameters.
        /// </summary>
        /// <param name="geoType"></param>
        /// <param name="targetProcessor"></param>
        /// <param name="settings"></param>
        public TargetingInstruction(GeometryType geoType, ITargetProcessor targetProcessor,
            TargetingSystemDataModels.FactorySettings settings)
        {
            this.GeometryType = geoType;
            if (this.TargetTag == null && settings.defaultTag == null)
            {
                this.TargetTag = "";
            }
            else
            {
                this.TargetTag = settings.defaultTag;
            }

            this.entityQueryFilter = settings.entityComponentQueryFilter;
            this.entityFilterComponentType = this.GetCurrentEntityFilterType();
            this.isTargetingEnabled = settings.defaultTargeting;
            this.AssignTargetProcessor(targetProcessor);
            if (settings.TargetingActionsTemplateObject != null)
            {
                this.TargetingActions = settings.TargetingActionsTemplateObject;
            }
            else
            {
                this.TargetingActions = ScriptableObject.CreateInstance<TargetingActionsTemplateObject>();
            }
        }

        void AssignTargetProcessor(ITargetProcessor targetProcessorSystem)
        {
            if (targetProcessorSystem != null && targetProcessorSystem.IsForEntities())
            {
                this.TargetProcessorEntities = targetProcessorSystem;
            }
            else if(targetProcessorSystem != null)
            {
                this.TargetProcessorForGameObjects = targetProcessorSystem;
            }
        }

        public ITargetProcessor TargetProcessorForGameObjects
        {
            get { return this.targetProcessorForGameObjects; }
            set { this.targetProcessorForGameObjects = value; }
        }

        public ITargetProcessor TargetProcessorEntities
        {
            get { return this.targetProcessorEntities; }
            set { this.targetProcessorEntities = value; }
        }

        public string TargetTag
        {
            get
            {
                return this.targetTag;
            }
            set
            {
                if (value != null)
                {
                    this.targetTag = value;
                }
                else
                {
                    this.targetTag = "";
                }

                this.needsUpdate = true;
            }
        }

        public GeometryType GeometryType
        {
            get { return this.geometryType; }
            set { this.geometryType = value; }
        }

        /// <summary>
        /// Use the targeting system, if Target.Value set to true
        /// </summary>
        public bool IsTargetingEnabled
        {
            get { return this.isTargetingEnabled; }
            set { this.isTargetingEnabled = value; }
        }

        public TargetingActionsTemplateObject TargetingActions
        {
            get
            {
                return this.targetingActions; 
            }
            set
            {
                this.needsUpdate = true;
                this.targetingActions = value;
            }
        }

        public Type EntityFilterComponentType
        {
            get { return this.entityFilterComponentType; }
        }

        public bool NeedsUpdate
        {
            get { return this.needsUpdate; }
            set { this.needsUpdate = value; }
        }

        public bool NeedsQueryUpdate { get; set; }

        public void SetCurrentEntityFilterType(Object entityFilterObject)
        {
            if (entityFilterObject)
            {
                this.entityQueryFilter = entityFilterObject;
                this.entityFilterComponentType = TargetingSystemUtilities.GetCurrentEntityFilterType(entityFilterObject);
                if (this.entityFilterComponentType != null)
                {
                    this.entityQueryFilterNameSpace = this.entityFilterComponentType.Namespace;
                    this.entityQueryFilterName = this.entityFilterComponentType.Name;
                }
            }
            else
            {
                this.entityFilterComponentType =null;
                this.entityQueryFilterNameSpace = "";
                this.entityQueryFilterName = "";
            }

            this.needsUpdate = true;
            this.NeedsQueryUpdate = true;
        }
        
        public void SetCurrentEntityFilterType(Type entityFilterType)
        {
            this.entityFilterComponentType = entityFilterType;
            if (this.entityFilterComponentType != null)
            {
                this.entityQueryFilterNameSpace = this.entityFilterComponentType.Namespace;
                this.entityQueryFilterName = this.entityFilterComponentType.Name;
                this.entityQueryFilter = null;
            }
            this.NeedsQueryUpdate = true;
        }

        public Type GetCurrentEntityFilterType()
        {
            return TargetingSystemUtilities.GetCurrentEntityFilterType(this.entityQueryFilter);
        }

        public void OnBeforeSerialize()
        {
            if (this.entityQueryFilter)
            {
                var nameSpace = TargetingSystemUtilities.GetNameSpace(this.entityQueryFilter.ToString());
                this.entityQueryFilterNameSpace = nameSpace;
                this.entityQueryFilterName = this.entityQueryFilter.name;
                if (this.TargetingActions == null )
                {
                    return;
                }
                for (int i = 0; i< this.TargetingActions.TriggerActionElements.Count; i++)
                {
                    if (this.TargetingActions.TriggerActionElements[i].EntityFilter == null)
                    {
                        continue;
                    }

                    this.TargetingActions.TriggerActionElements[i].EntityQueryFilterNameSpace = TargetingSystemUtilities.GetNameSpace(this.TargetingActions.TriggerActionElements[i].EntityFilter.ToString());
                    this.TargetingActions.TriggerActionElements[i].EntityQueryFilterName = this.TargetingActions.TriggerActionElements[i].EntityFilter.name;
    
                }

                this.entityFilterComponentType = Type.GetType(string.Concat(this.entityQueryFilterNameSpace, ".", this.entityQueryFilterName));
                if (this.targetingActions == null || this.targetingActions.TriggerActionElements == null)
                {
                    return;
                }
                for (int i = 0; i< this.TargetingActions.TriggerActionElements.Count; i++)
                {
                    this.TargetingActions.TriggerActionElements[i].EntityFilterComponentType = Type.GetType(string.Concat(this.TargetingActions.TriggerActionElements[i].EntityQueryFilterNameSpace, ".", this.TargetingActions.TriggerActionElements[i].EntityQueryFilterName));
                }

                this.entityFilterComponentType = Type.GetType(string.Concat(this.entityQueryFilterNameSpace, ".", this.entityQueryFilterName));

            }
            
        }

        public void OnAfterDeserialize()
        {
            this.entityFilterComponentType = Type.GetType(string.Concat(this.entityQueryFilterNameSpace, ".", this.entityQueryFilterName));
     
            if (this.targetingActions == null)
            {
                return;
            }
            foreach (var t in this.TargetingActions.TriggerActionElements)
            {
                t.EntityFilterComponentType = Type.GetType(string.Concat(t.EntityQueryFilterNameSpace, ".", t.EntityQueryFilterName));
            }
            
        }
    }
}