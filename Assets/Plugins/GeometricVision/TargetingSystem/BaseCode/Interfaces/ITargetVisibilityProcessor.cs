using System;
using Plugins.GeometricVision.TargetingSystem.BaseCode.MainClasses;

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.Interfaces
{
    /// <summary>
    /// Responsible for seeing objects and geometry inside the targetProcessorSystem.
    /// It checks, if object is inside visibility zone and filters out unwanted objects and geometry.
    ///
    /// Usage: Automatic. Default implementation of this interface is added automatically after user adds GV_TargetingSystem component from the inspector UI.
    /// Implementation is also switched according to user decisions. See GeometricVision.cs
    /// </summary>
    /// <remarks>The entity version has some differences how it behaves. For example there can be multiple MonoBehaviours, but only 1 entity system at a time.
    /// That means you can store things in MonoBehaviour components for easier life but with entities you need to share things.</remarks>
    public interface ITargetVisibilityProcessor: IComparable<ITargetVisibilityProcessor>
    {
        int Id { get; set; }

        ///  <summary>
        ///  Updates targets source game object or entity visibilities.
        ///  Only to be used in case you need to get updated information for target
        ///  searches like GetTargets or Update targets from GV_TargetingSystem component.
        ///  Normally this is done automatically and only needs to be manually invoked if waiting for a frame is not an option.
        ///  </summary>
        ///  <param name="targetingSystemIn">geometry vision component to use for lookup</param>
        ///  <param name="forceUpdateIn">Bypass optimizations and do a full update</param>
        void UpdateVisibility(GV_TargetingSystem targetingSystemIn, bool forceUpdateIn);
        bool IsEntityBased();
    }
}