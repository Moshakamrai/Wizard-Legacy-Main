using System.Collections.Generic;
using Plugins.GeometricVision.TargetingSystem.BaseCode.GameObjects.ImplementationsGameObjects;
using Plugins.GeometricVision.TargetingSystem.BaseCode.Interfaces;
using Plugins.GeometricVision.TargetingSystem.BaseCode.TargetingComponents;
using UnityEngine;

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.UtilitiesAndPlugins
{
    public static class TargetingSystemComparers
    {

        //Usage: this.targets.Sort<TargetingSystemDataModels.Target, DistanceComparer>(new DistanceComparer());
        public struct DistanceComparerToViewDirection : IComparer<Target>
        {
            public int Compare(Target x, Target y)
            {
                if (x.distanceFromTargetToCastOrigin == 0f && y.distanceFromTargetToCastOrigin != 0f)
                {
                    return -1;
                }

                if (x.distanceFromTargetToCastOrigin != 0f && y.distanceFromTargetToCastOrigin == 0f)
                {
                    return 1;
                }
                if (x.distanceFromTargetToCastOrigin == 0f && y.distanceFromTargetToCastOrigin == 0f)
                {
                    return 0;
                }
                //First check if the target distances are close to each other, thus creating a distance competition.
                //In this case also use distance to camera to give the actual closest target.
                if (Mathf.Abs(x.distanceFromTargetToProjectedPoint - y.distanceFromTargetToProjectedPoint) < 0.005f)
                {
                    
                    if (x.distanceFromTargetToCastOrigin < y.distanceFromTargetToCastOrigin)
                    {
                        return -1;
                    }
                    else if (x.distanceFromTargetToCastOrigin > y.distanceFromTargetToCastOrigin)
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else if (x.distanceFromTargetToProjectedPoint < y.distanceFromTargetToProjectedPoint)
                {
                    return -1;
                }
                else if (x.distanceFromTargetToProjectedPoint > y.distanceFromTargetToProjectedPoint)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        //Usage: this.targets.Sort<TargetingSystemDataModels.Target, DistanceComparer>(new DistanceComparer());
        public struct DistanceComparerToCamera : IComparer<Target>
        {
            public int Compare(Target x, Target y)
            {
                if (x.distanceFromTargetToCastOrigin == 0f && y.distanceFromTargetToCastOrigin != 0f)
                {
                    return 1;
                }

                if (x.distanceFromTargetToCastOrigin != 0f && y.distanceFromTargetToCastOrigin == 0f)
                {
                    return -1;
                }
                
                if (x.distanceFromTargetToCastOrigin < y.distanceFromTargetToCastOrigin)
                {
                    return -1;
                }

                else if (x.distanceFromTargetToCastOrigin > y.distanceFromTargetToCastOrigin)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }
        
        public struct VisibilityProcessorComparer : IComparer<ITargetVisibilityProcessor>
        {

            public int Compare(ITargetVisibilityProcessor x, ITargetVisibilityProcessor y)
            {
                if(x is GameObjectTargetVisibilityProcessor) {
                    return -1;
                }
                else if(y is GameObjectTargetVisibilityProcessor) {
                    return 1;
                }

                else
                {
                    return 1;
                }
            }
        }        

#if TARGETING_SYSTEM_GEOMETRY_BASED_TARGETING

        public struct TargetCreatorComparer : IComparer<ITargetCreator>
        {
            public int Compare(ITargetCreator creator1, ITargetCreator creator2)
            {
       
                if(creator1 is GameObjectTargetCreator) {
                    return -1;
                }
                else if(creator2 is GameObjectTargetCreator)
                {
                    return 1;
                }
                else
                {
                    return 1;
                }
            }
        }
#else

        public struct TargetCreatorComparer : IComparer<ITargetCreator>
        {
            public int Compare(ITargetCreator creator1, ITargetCreator creator2)
            {

                return -1;
            }
        }   
#endif
    }
}
