using System;
using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using Plugins.GeometricVision.TargetingSystem.BaseCode.MainClasses;
using Plugins.GeometricVision.TargetingSystem.BaseCode.TargetingComponents;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.Interfaces
{
    /// <summary>
    /// Made to handle targeting logic.
    /// Usage: For new targeting behavior implement this interface and add it to the targeting systems list on the
    /// GeometryTargetingSystemsContainer component from GeometricVision component.
    /// </summary>
    public interface ITargetProcessor: IComparable<ITargetProcessor>
    {
        /// <summary>
        /// Gets targeting data
        /// </summary>
        /// <param name="rayLocation"></param>
        /// <param name="rayDirection"></param>
        /// <param name="gvTargetingSystem"></param>
        /// <param name="targetingInstruction"></param>
        /// <returns></returns>
        NativeArray<Target> GetTargetsAsNativeArray(Vector3 rayLocation, Vector3 rayDirection,GV_TargetingSystem gvTargetingSystem, TargetingInstruction targetingInstruction);
       
        /// <summary>
        /// Gets targeting data from given point and direction.
        /// </summary>
        /// <param name="rayLocation">Point where to get data from</param>
        /// <param name="rayDirection"></param>
        /// <param name="gvTargetingSystem"></param>
        /// <param name="targetingInstruction">Instruction from targeting component to descibe what kind of targeting data to fetch</param>
        /// <returns></returns>
        void GetTargetsAsNativeSlice(Vector3 rayLocation, Vector3 rayDirection,GV_TargetingSystem gvTargetingSystem);
        
        /// <summary>
        /// Gets targeting data JobHandle for give targeting system in cases where the jobs needs to be completed before accessing containers.
        /// </summary>
        /// <remarks>System needs to implement Unity's job system. Otherwise not implemented exception will occur</remarks>
        /// <param name="gvTargetingSystem">Given targeting system</param>
        /// <returns>targeting JobHandle</returns>
        JobHandle GetTargetsJobHandle(GV_TargetingSystem gvTargetingSystem);

        /// <summary>
        /// Gets targeting data
        /// </summary>
        /// <param name="rayLocation"></param>
        /// <param name="rayDirection"></param>
        /// <param name="gvTargetingSystem"></param>
        /// <param name="targetingInstruction"></param>
        /// <returns></returns>
        NativeList<Target> GetTargets(Vector3 rayLocation, Vector3 rayDirection, GV_TargetingSystem gvTargetingSystem,
            TargetingInstruction targetingInstruction);
      //  GeometryType TargetedType { get; }
        
        /// <summary>
        /// For checking if is entity based /-system
        /// </summary>
        /// <returns>Boolean telling if this targeting system uses entity based systems</returns>
        bool IsForEntities();

    }
}
