using System;
using Plugins.GeometricVision.TargetingSystem.BaseCode.MainClasses;

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.Interfaces
{
    public interface ITargetCreator: IComparable<ITargetCreator>
    {
        void SetRunner(TargetingSystemsRunner runner);
        /// <summary>
        /// Counts all the scene objects in the current active scene. Not including objects from other scenes.
        /// </summary>
        /// <returns></returns>
        int CountSceneObjects();
        
        /// <summary>
        /// Checks if there are new game objects or entities on the scene and then updates the situation.
        /// </summary>
        void CheckSceneChanges();
                
        /// <summary>
        /// Turn flag on the creator to update next time its called on the on update
        /// </summary>
        void ScheduleUpdate();
    }
}
