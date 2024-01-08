using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using Plugins.GeometricVision.TargetingSystem.BaseCode.Factory;
using Plugins.GeometricVision.TargetingSystem.BaseCode.GameObjects.ImplementationsGameObjects;
using Plugins.GeometricVision.TargetingSystem.BaseCode.Interfaces;
using Plugins.GeometricVision.TargetingSystem.BaseCode.MainClasses;
using Plugins.GeometricVision.TargetingSystem.BaseCode.TargetingComponents;
using Plugins.GeometricVision.TargetingSystem.BaseCode.TargetingJobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Plugins.GeometricVision.TargetingSystem.BaseCode.UtilitiesAndPlugins.MathUtilities;
using RaycastHit = UnityEngine.RaycastHit;

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.UtilitiesAndPlugins
{
    public static class TargetingSystemUtilities
    {
        /// <summary>
        /// Moves transform. GameObject version of the move target
        /// </summary>
        /// <param name="targetTransform"></param>
        /// <param name="newPosition"></param>
        /// <param name="movementSpeed"></param>
        /// <param name="distanceToStop"></param>
        public static IEnumerator MoveTarget(Transform targetTransform, Vector3 newPosition, float movementSpeed,
            float distanceToStop)
        {
            if (targetTransform == null)
            {
                yield break;
            }

            var distanceBetweenStartToEnd = Vector3.Distance(targetTransform.position, newPosition);

            while (distanceBetweenStartToEnd > distanceToStop)
            {
                distanceBetweenStartToEnd = MoveTransformWithSpeed(targetTransform, newPosition, movementSpeed,
                    distanceBetweenStartToEnd);
                yield return null;
            }
        }

        private static float MoveTransformWithSpeed(Transform transform, Vector3 newPositionIn, float movementSpeedIn,
            float distanceBetweenStartToEnd)
        {
            if (transform == null)
            {
                return 0;
            }

            Vector3 position = transform.position;
            distanceBetweenStartToEnd = Vector3.Distance(position, newPositionIn);
            position = Vector3.MoveTowards(position, newPositionIn, movementSpeedIn);

            transform.position = position;
            return distanceBetweenStartToEnd;
        }

        public static bool TargetHasNotChanged(Target newTarget,
            Target currentTarget)
        {
            return newTarget.geoInfoHashCode == currentTarget.geoInfoHashCode;
        }

        public static Target GetTargetValuesFromAnotherSystem(Target targetToBeUpdated,
            GV_TargetingSystem targetingSystem)
        {
            if (targetToBeUpdated.isEntity == TargetingSystemDataModels.Boolean.True)
            {

            }
            else
            {
                targetToBeUpdated = targetingSystem.GetClosestTargets(false)
                    .FirstOrDefault(targetIn => targetIn.geoInfoHashCode == targetToBeUpdated.geoInfoHashCode);
            }

            var targetWithSquaredDistances = SquareTargetDistances(targetToBeUpdated);

            return targetWithSquaredDistances;
        }


        /// <summary>
        ///   <para>Calculate a targetPosition between the points specified by current and target, moving no farther than the distance specified by maxDistanceDelta.</para>
        /// </summary>
        /// <param name="current">The targetPosition to move from.</param>
        /// <param name="target">The targetPosition to move towards.</param>
        /// <param name="maxDistanceDelta">Distance to move current per call.</param>
        /// <returns>
        ///   <para>The new targetPosition.</para>
        /// </returns>
        [BurstCompile]
        public static float3 MoveTowards(
            float3 current,
            float3 target,
            float maxDistanceDelta)
        {
            float num1 = target.x - current.x;
            float num2 = target.y - current.y;
            float num3 = target.z - current.z;
            float num4 = num1 * num1 + num2 * num2 + num3 * num3;
            if (num4 == 0.0f || maxDistanceDelta >= 0.0 && num4 <= maxDistanceDelta * maxDistanceDelta)
            {
                return target;
            }

            float num5 = Mathf.Sqrt(num4);
            return new float3(current.x + num1 / num5 * maxDistanceDelta, current.y + num2 / num5 * maxDistanceDelta,
                current.z + num3 / num5 * maxDistanceDelta);
        }

        /// <summary>
        ///   <para>Calculate a targetPosition between the points specified by current and target, moving no farther than the distance specified by maxDistanceDelta.</para>
        /// </summary>
        /// <param name="current">The targetPosition to move from.</param>
        /// <param name="target">The targetPosition to move towards.</param>
        /// <param name="maxDistanceDelta">Distance to move current per call.</param>
        /// <returns>
        ///   <para>The new targetPosition.</para>
        /// </returns>
        [BurstCompile]
        public static float4 MoveTowards(
            float4 current,
            float4 targetMaxDistanceDelta,
            float4 resultHolder
        )
        {
            resultHolder.x = targetMaxDistanceDelta.x - current.x;
            resultHolder.y = targetMaxDistanceDelta.y - current.y;
            resultHolder.z = targetMaxDistanceDelta.z - current.z;
            resultHolder.w = resultHolder.x * resultHolder.x + resultHolder.y * resultHolder.y +
                             resultHolder.z * resultHolder.z;
            if (resultHolder.w == 0.0f || targetMaxDistanceDelta.w >= 0.0 &&
                resultHolder.w <= targetMaxDistanceDelta.w * targetMaxDistanceDelta.w)
            {
                return targetMaxDistanceDelta;
            }

            current.w = Mathf.Sqrt(resultHolder.w);
            return new float4(current.x + resultHolder.x / current.w * targetMaxDistanceDelta.w,
                current.y + resultHolder.y / current.w * targetMaxDistanceDelta.w,
                current.z + resultHolder.z / current.w * targetMaxDistanceDelta.w, 0);
        }

        public static JobHandle AddTargetsToContainer(NativeList<Target> closestTargetsContainerIn2,
            NativeSlice<Target> targetsToAdd, int offset)
        {
            if (targetsToAdd.Length > 0)
            {
                var job1 = new TargetingVisibilityJobs.CombineEntityAndGameObjectTargetsOptimized
                {
                    targetingContainerLengthTargetsToInsertLengthOffset = new int4(closestTargetsContainerIn2.Length,
                        targetsToAdd.Length, offset, 0),
                    targetingContainer = closestTargetsContainerIn2,
                    targetsToInsert = targetsToAdd,
                };
                var handle = job1.Schedule(targetsToAdd.Length, 2);
                return handle;
            }
            else
            {
                return new JobHandle();
            }
        }

        private static JobHandle AddTargetsToContainer(ref NativeList<Target> closestTargetsContainerIn2,
            NativeSlice<Target> targetsToAdd, int offset, JobHandle dependency)
        {
            if (targetsToAdd.Length > 0)
            {
                var job1 = new TargetingVisibilityJobs.CombineEntityAndGameObjectTargetsOptimized
                {
                    targetingContainerLengthTargetsToInsertLengthOffset = new int4(closestTargetsContainerIn2.Length,
                        targetsToAdd.Length, offset, 0),
                    targetingContainer = closestTargetsContainerIn2,
                    targetsToInsert = targetsToAdd,
                };

                dependency = job1.Schedule(targetsToAdd.Length, 2, dependency);
            }

            return dependency;
        }

        public static void AddSpaceToContainerIfNeeded(int entityContainerLength,
            NativeList<Target> closestTargetsContainer)
        {
            if (entityContainerLength > closestTargetsContainer.Length)
            {
                closestTargetsContainer.Resize(closestTargetsContainer.Length + entityContainerLength * 2,
                    NativeArrayOptions.UninitializedMemory);
            }
        }


        [BurstCompile]
        public struct ResizeNativeListJob : IJob
        {
            public NativeList<Target> closestTargetsContainer;
            public int4 amountOfTargets_WantedLength_ClosestTargetsContainerLength;

            public void Execute()
            {
                //multiply the actual length by two to give a treshold for the operation
                if (this.amountOfTargets_WantedLength_ClosestTargetsContainerLength.x >
                    this.amountOfTargets_WantedLength_ClosestTargetsContainerLength.z)
                {
                    this.closestTargetsContainer.Resize(
                        this.amountOfTargets_WantedLength_ClosestTargetsContainerLength.y,
                        NativeArrayOptions.UninitializedMemory);
                }
            }
        }

        [BurstCompile]
        public static bool IsCulledByGameObject(float4 originPosition, Vector3 endPosition, float4 distanceTarget,
            ref RaycastHit hit)
        {
            int layerMask = (1 << 8) | (1 << 2);

            // This would cast rays only against colliders in layer 8.
            // But instead we want to collide against everything except layer 8. The ~ operator does this, it inverts a bitmask.
            layerMask = ~layerMask;


            // Does the ray intersect any objects excluding the 2 layers 2 and 8?
            if (Physics.Raycast(originPosition.xyz, GetDirection(originPosition.xyz, endPosition), out hit,
                Mathf.Infinity, layerMask))
            {
                var hitInfoPosition = hit.point;

                if (SurfacePointIsInFrontOfTarget(hitInfoPosition))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return false;

            bool SurfacePointIsInFrontOfTarget(float3 hitPosition)
            {
                var distanceToHitPosition = DistanceToHitPoint(hitPosition);
                return (distanceToHitPosition < distanceTarget - 0.1f).x;

                float4 DistanceToHitPoint(float3 hitPositionIn)
                {
                    return Float3Distance(originPosition, hitPositionIn).x;
                }
            }
        }

        /// <summary>
        /// Used to check whit game object processing on to check if there is entity or game object obstructing the view
        /// </summary>
        /// <param name="cullingMode"></param>
        /// <param name="geometryVisionPosition"></param>
        /// <param name="rayCastInput"></param>
        /// <param name="geoInfoIn"></param>
        /// <param name="pWorldIn"></param>
        /// <param name="hitInfoIn"></param>
        /// <param name="hit"></param>
        /// <returns></returns>
        public static bool IsGameObjectTargetCulledByGameObjectOrEntity(int4 cullingMode, float4 geometryVisionPosition,
            TargetingSystemDataModels.GeoInfo geoInfoIn,
            RaycastHit hit)
        {
            if (cullingMode.x == 0)
            {
                return false;
            }

            var culledByGameObject = IsCulledByGameObject(geometryVisionPosition,
                geoInfoIn.transform.position,
                Float3Distance(geometryVisionPosition, geoInfoIn.transform.position).x, ref hit);

            geoInfoIn.isHitByRay = TargetingSystemDataModels.Boolean.False;
            if (hit.transform != null && hit.transform.GetHashCode() == geoInfoIn.transform.GetHashCode())
            {
                geoInfoIn.isHitByRay = TargetingSystemDataModels.Boolean.True;
            }

            return CulledOrNot(culledByGameObject);

            bool CulledOrNot(bool culledByGameObjectIn)
            {
                if (culledByGameObjectIn)
                {
                    if (culledByGameObjectIn && TransformsAreSame(hit.transform, geoInfoIn.transform))
                    {
                        return false;
                    }

                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Compares transforms has codes and returns true if same.
        /// </summary>
        /// <param name="hitTransform"></param>
        /// <param name="transform"></param>
        /// <returns></returns>
        private static bool TransformsAreSame(Transform hitTransform, Transform otherTransform)
        {
            return hitTransform.GetHashCode() == otherTransform.GetHashCode();
        }

        internal static bool TransformIsEffect(GameObject gameObjectToQuestion,
            string effectTag)
        {
            return gameObjectToQuestion.layer == TargetingSystemSettings.IgnoreRayCastLayer;
        }

        /// <summary>
        /// In case the user plays around with the settings on the inspector and changes thins this needs to be run.
        /// It checks that the targeting system implementations are correct.
        /// </summary>
        /// <param name="targetingInstructionsIn"></param>
        /// <param name="gameObjectProcessing"></param>
        /// <param name="entityBasedProcessing"></param>
        /// <param name="entityWorld"></param>
        public static List<TargetingInstruction> ValidateTargetingInstructions(
            List<TargetingInstruction> targetingInstructionsIn, bool gameObjectProcessing
            , bool entityBasedProcessing)
        {

            foreach (var targetingInstruction in targetingInstructionsIn)
            {
                if (gameObjectProcessing == true)
                {
                    if ((int) targetingInstruction.GeometryType == 2)
                    {
                        targetingInstruction.GeometryType = GeometryType.Objects;
                    }

                    targetingInstruction.TargetProcessorForGameObjects =
                        AssignTargetingProcessorToTargetingInstruction(targetingInstruction, new GameObjectTargetProcessor());
                }
                
                targetingInstruction.NeedsUpdate = true;
            }

            return targetingInstructionsIn;
        }

        private static ITargetProcessor AssignTargetingProcessorToTargetingInstruction(
            TargetingInstruction targetingInstruction, ITargetProcessor newObjectTargeting)
        {
            ITargetProcessor targetProcessorToReturn = null;

            if (targetingInstruction.GeometryType == GeometryType.Objects)
            {
                targetProcessorToReturn = newObjectTargeting;
            }
            
            return targetProcessorToReturn;
        }

        //Collects game object actions from instructions. 
        internal static List<ActionsTemplateTriggerActionElement>
            CollectGameObjectTargetingActionsFromTargetingInstructions(
                List<TargetingInstruction> targetingInstructionsIn,
                List<ActionsTemplateTriggerActionElement> collectedTriggerActionElements,
                GV_TargetingSystem targetingSystem)
        {
            collectedTriggerActionElements.Clear();
            foreach (var targetingInstruction in targetingInstructionsIn)
            {
                CollectActions(targetingInstruction.TargetingActions, targetingInstruction);
            }

            return collectedTriggerActionElements;


            //
            //Locals
            //
            void CollectActions(TargetingActionsTemplateObject actions, TargetingInstruction targetingInstructionIn)
            {
                for (var index = 0; index < actions.TriggerActionElements.Count; index++)
                {
                    ActionsTemplateTriggerActionElement action =
                        new ActionsTemplateTriggerActionElement(actions.TriggerActionElements[index],
                            targetingInstructionIn);

                    if (action.SpawnAtTarget == false && action.SpawnAtSource == false && action.Enabled == true)
                    {
                        action.Enabled = false;
                        continue;
                    }

                    collectedTriggerActionElements.Add(action);
                }
            }
        }

        //Counts game object actions from instructions. 
        internal static int CountTargetingActionsFromTargetingInstructions(
            List<TargetingInstruction> targetingInstructionsIn)
        {
            int totalElementsCount = 0;
            foreach (var targetingInstruction in targetingInstructionsIn)
            {
                var actions = targetingInstruction.TargetingActions;

                totalElementsCount += actions.TriggerActionElements.Count();
            }

            return totalElementsCount;
        }

        internal static Target CheckTargetAgainstPickingRadius(Target targetIn, bool enabled,
            float indicatorVisibilityDistance)
        {
            if (enabled == false)
            {
                return targetIn;
            }
            else if (indicatorVisibilityDistance > targetIn.distanceFromTargetToProjectedPoint)
            {
                return targetIn;
            }

            return new Target();
        }

        internal static Target GetAndUpdateTarget(Target currentTarget, NativeSlice<Target> closestTargetSlice,
            int index,
            ref bool targetFetchedForCurrentFrame)
        {
            if (targetFetchedForCurrentFrame == false)
            {
                currentTarget = SquareTargetDistances(closestTargetSlice[index]);
                targetFetchedForCurrentFrame = true;
                return currentTarget;
            }

            return currentTarget;
        }

        /// <summary>
        /// Fetch the data structure for gameObject based on the hash code usually got from the target object.
        /// Target object cannot contain transforms or gameObjects, because its not safe.
        /// </summary>
        /// <param name="geoInfoHashCode">Hash code that you can get from closest target</param>
        /// <returns>geoInfo object that contains reference to gameObject related data</returns>
        public static TargetingSystemDataModels.GeoInfo GetGeoInfoBasedOnHashCode(int geoInfoHashCode,
            List<TargetingSystemDataModels.GeoInfo> GeoInfos)
        {
            var geoInfo = GeoInfos.FirstOrDefault(geoInfoElement =>
                geoInfoElement.GetHashCode().x == geoInfoHashCode);
            return geoInfo;
        }

        /// <summary>
        /// Loads png image from file path and returns it.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static Texture LoadPNG(string filePath)
        {
            Texture2D texture2D = null;
            byte[] fileData;

            if (File.Exists(filePath))
            {
                fileData = File.ReadAllBytes(filePath);
                texture2D = new Texture2D(2, 2);
                texture2D.LoadImage(fileData);
            }

            return texture2D;
        }

        public static Type GetCurrentEntityFilterType(UnityEngine.Object entityFilterObject)
        {
            if (entityFilterObject)
            {
                var typeToReturn = SearchTypeFromAssemblies();

                Type SearchTypeFromAssemblies()
                {
                    var typeToReturn1 = TryGetTypeFromCallingScriptsAssembly();
                    typeToReturn1 = TryGetTypeFromCSharpAssembly(typeToReturn1);
                    typeToReturn1 = TryGetTypeFromTargetingSystemsAssembly(typeToReturn1);
                    typeToReturn1 = TryGetFromAssembliesThatIsNotBlackListed(typeToReturn1);
                    return typeToReturn1;
                }

                return typeToReturn;
            }
            else
            {
                return null;
            }

            Type TryGetTypeFromCallingScriptsAssembly()
            {
                //Try from the calling scripts domain
                var assemblyName = Assembly.GetCallingAssembly().FullName;
                var type = Type.GetType(string.Concat(GetNameSpace(entityFilterObject.ToString()), ".",
                    entityFilterObject.name, ", " + assemblyName));
                return type;
            }

            Type TryGetTypeFromCSharpAssembly(Type type)
            {
                if (type == null)
                {
                    type = Type.GetType(string.Concat(GetNameSpace(entityFilterObject.ToString()), ".",
                        entityFilterObject.name,
                        ", " + "Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
                }

                return type;
            }

            Type TryGetTypeFromTargetingSystemsAssembly(Type type)
            {
                if (type != null)
                {
                    return type;
                }

                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var assembly in assemblies)
                {
                    if (assembly.FullName.Contains("GV_TargetingSystem"))
                    {
                        type = SearchForTypeInAssembly(assembly);
                    }
                }

                return type;
            }

            Type TryGetFromAssembliesThatIsNotBlackListed(Type type)
            {
                if (type != null)
                {
                    return type;
                }

                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var assembly in assemblies)
                {
                    // To avoid waiting for long skip time consuming and obvious assemblies
                    if (IsNotBlackListed())
                    {
                        type = SearchForTypeInAssembly(assembly);
                    }

                    bool IsNotBlackListed()
                    {
                        return assembly.FullName.StartsWith("Unity.") == false &&
                               assembly.FullName.StartsWith("UnityEditor,") == false &&
                               assembly.FullName.StartsWith("UIEditorDrawers,") == false &&
                               assembly.FullName.StartsWith("UnityEngine") == false &&
                               assembly.FullName.StartsWith("mscorlib") == false &&
                               assembly.FullName.StartsWith("Mono.") == false &&
                               assembly.FullName.StartsWith("Microsoft.") == false &&
                               assembly.FullName.StartsWith("netstandard") == false &&
                               assembly.FullName.StartsWith("Bee.BeeDriver") == false &&
                               assembly.FullName.StartsWith("System.") == false &&
                               assembly.FullName.StartsWith("System,") == false &&
                               assembly.FullName.StartsWith("StompyRobot.") == false &&
                               assembly.FullName.StartsWith("ExCSS.Unity") == false &&
                               assembly.FullName.StartsWith("Autodesk") == false &&
                               assembly.FullName.StartsWith("FBX") == false &&
                               assembly.FullName.StartsWith("UniRx") == false;
                    }
                }

                return type;
            }

            Type SearchForTypeInAssembly(Assembly assembly)
            {
                Type type = null;
                Type[] types = assembly.GetTypes();
                foreach (var typeInAssembly in types)
                {
                    if (typeInAssembly.FullName != null &&
                        typeInAssembly.FullName.Contains(entityFilterObject.name))
                    {
                        type = typeInAssembly;
                        break;
                    }
                }

                return type;
            }
        }


        /// <summary>
        /// Get namespace for getting a type with class name
        /// </summary>
        /// <param name="text"></param>
        /// <returns>Trimmed namespace</returns>
        public static string GetNameSpace(string text)
        {
            string[] lines = text.Replace("\r", "").Split('\n');
            string toReturn = "";
            int elementFollowingNamespaceDeclaration = 1;
            foreach (var line in lines)
            {
                if (line.Contains("namespace"))
                {
                    toReturn = line.Split(' ')[elementFollowingNamespaceDeclaration].Trim();
                }
            }

            return toReturn;
        }

        public static void ApplySystemToTargetingInstructions(GV_TargetingSystem targetingSystem,
            List<TargetingInstruction> targetingInstructions)
        {
            foreach (var targetingInstruction in targetingInstructions)
            {
                if (targetingInstruction.TargetingActions)
                {
                    targetingInstruction.TargetingActions.TargetingSystem = targetingSystem;
                }
            }
        }

        public static Target GetTargetingDataForTargetNoSqrt(Target target, float4 rayLocationIn,
            float4 rayDirectionWSIn)
        {
            float3 point = target.position;
            point = PointToGivenSpace(rayLocationIn, point);
            float4 rayDirectionEndPoint = PointToGivenSpace(rayLocationIn, rayDirectionWSIn);
            target.projectedTargetPosition = Project(point, rayDirectionEndPoint) + rayLocationIn;
            target.position = PointFromRaySpaceToObjectSpaceF3(point, rayLocationIn).xyz;
            target.distanceFromTargetToProjectedPoint =
                Float4DistanceNoSqrt(target.position, target.projectedTargetPosition).x;
            target.distanceFromTargetToCastOrigin = Float4DistanceNoSqrt(rayLocationIn, target.position).x;
            target.distanceFromProjectedPointToCastOrigin =
                Float4DistanceNoSqrt(rayLocationIn.xyz, target.projectedTargetPosition).x;
            return target;
        }

        [BurstCompile]
        public static Target GetTargetingDataForTarget(Target target, float4 rayLocationIn, float4 rayDirectionWSIn)
        {
            float3 point = target.position;
            point = PointToGivenSpace(rayLocationIn, point);
            float4 rayDirectionEndPoint = PointToGivenSpace(rayLocationIn, rayDirectionWSIn);
            target.projectedTargetPosition = (Project(point, rayDirectionEndPoint) + rayLocationIn);
            target.position = PointFromRaySpaceToObjectSpaceF3(point, rayLocationIn).xyz;
            target.distanceFromTargetToProjectedPoint =
                Float4Distance(target.position, target.projectedTargetPosition).x;
            target.distanceFromTargetToCastOrigin = Float3Distance(rayLocationIn, target.position).x;
            target.distanceFromProjectedPointToCastOrigin =
                Float4Distance(rayLocationIn.xyz, target.projectedTargetPosition).x;
            return target;
        }


        /// <summary>
        /// Heavily optimized version of the above code
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
        public static TargetingSystemDataModels.Boolean IsInsideFrustum2(
            [ReadOnly] float4 point,
            [NoAlias] float4 planeNormal0,
            [NoAlias] float4 planeNormal1,
            [NoAlias] float4 planeNormal2,
            [NoAlias] float4 planeNormal3,
            [NoAlias] float4 planeNormal4,
            [NoAlias] float4 planeNormal5,
            [ReadOnly] float4 zero)
        {
            if ((planeNormal0.x * point.x) + (planeNormal0.y * point.y) + (planeNormal0.z * point.z) +
                planeNormal0.w < zero.x ||
                (planeNormal1.x * point.x) + (planeNormal1.y * point.y) + (planeNormal1.z * point.z) +
                planeNormal1.w < zero.x ||
                (planeNormal2.x * point.x) + (planeNormal2.y * point.y) + (planeNormal2.z * point.z) +
                planeNormal2.w < zero.x ||
                (planeNormal3.x * point.x) + (planeNormal3.y * point.y) + (planeNormal3.z * point.z) +
                planeNormal3.w < zero.x ||
                (planeNormal4.x * point.x) + (planeNormal4.y * point.y) + (planeNormal4.z * point.z) +
                planeNormal4.w < zero.x ||
                (planeNormal5.x * point.x) + (planeNormal5.y * point.y) + (planeNormal5.z * point.z) +
                planeNormal5.w < zero.x)
                return TargetingSystemDataModels.Boolean.False;
            else
                return TargetingSystemDataModels.Boolean.True;
        }

        /// <summary>
        /// Squares the distances in target object. Returns the same target with new distances.
        /// </summary>
        /// <param name="target">Target to have their un squared distances squared</param>
        /// <returns></returns>
        internal static Target SquareTargetDistances(Target target)
        {
            target.distanceFromTargetToCastOrigin = Mathf.Sqrt(target.distanceFromTargetToCastOrigin);
            target.distanceFromTargetToProjectedPoint = Mathf.Sqrt(target.distanceFromTargetToProjectedPoint);
            target.distanceFromProjectedPointToCastOrigin = Mathf.Sqrt(target.distanceFromProjectedPointToCastOrigin);
            return target;
        }

        /// <summary>
        /// Original version copied from net and optimized with the help of the Unity performance framework.
        /// This is a faster AABB cull than brute force that also gives additional info on intersections.
        /// Calling Bounds.Min/Max is actually quite expensive so as an optimization you can precalculate these.
        /// http://www.lighthouse3d.com/tutorials/view-frustum-culling/geometric-approach-testing-boxes-ii/
        /// </summary>
        /// <param name="planeNormalAndDistance0">xyz normal, w = distance</param>
        /// <param name="planeNormalAndDistance1"></param>
        /// <param name="planeNormalAndDistance2"></param>
        /// <param name="planeNormalAndDistance3"></param>
        /// <param name="planeNormalAndDistance4"></param>
        /// <param name="planeNormalAndDistance5"></param>
        /// <param name="boundsMin"></param>
        /// <param name="boundsMax"></param>
        /// <param name="vMin">Holds result of bounds check so use float3.zero as parameter</param>
        /// <returns></returns>
        [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TargetingSystemDataModels.Boolean TestPlanesAABBInternalFastest(
            [NoAlias] float4 planeNormalAndDistance0,
            [NoAlias] float4 planeNormalAndDistance1,
            [NoAlias] float4 planeNormalAndDistance2,
            [NoAlias] float4 planeNormalAndDistance3,
            [NoAlias] float4 planeNormalAndDistance4,
            [NoAlias] float4 planeNormalAndDistance5,
            float4 boundsMin,
            float4 boundsMax,
            float3 vMin,
            TargetingSystemDataModels.Boolean boolIn = TargetingSystemDataModels.Boolean.True)
        {
            return CheckUnVisible(planeNormalAndDistance0) == TargetingSystemDataModels.Boolean.False
                ? TargetingSystemDataModels.Boolean.False
                : CheckUnVisible(planeNormalAndDistance1) == TargetingSystemDataModels.Boolean.False
                    ? TargetingSystemDataModels.Boolean.False
                    : CheckUnVisible(planeNormalAndDistance2) == TargetingSystemDataModels.Boolean.False
                        ? TargetingSystemDataModels.Boolean.False
                        : CheckUnVisible(planeNormalAndDistance3) == TargetingSystemDataModels.Boolean.False
                            ? TargetingSystemDataModels.Boolean.False
                            : CheckUnVisible(planeNormalAndDistance4) == TargetingSystemDataModels.Boolean.False
                                ? TargetingSystemDataModels.Boolean.False
                                : CheckUnVisible(planeNormalAndDistance5) == TargetingSystemDataModels.Boolean.False
                                    ? TargetingSystemDataModels.Boolean.False
                                    : TargetingSystemDataModels.Boolean.True;

            //normal = xyz, distance = w
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
            TargetingSystemDataModels.Boolean CheckUnVisible(float4 normalAndDistance)
            {
                // X axis
                vMin.x = normalAndDistance.x < 0 ? boundsMin.x : boundsMax.x;

                // Y axis
                vMin.y = normalAndDistance.y < 0 ? boundsMin.y : boundsMax.y;

                // Z axis
                vMin.z = normalAndDistance.z < 0 ? boundsMin.z : boundsMax.z;

                var dot1 = normalAndDistance.x * vMin.x + normalAndDistance.y * vMin.y + normalAndDistance.z * vMin.z;
                return dot1 + normalAndDistance.w < 0
                    ? TargetingSystemDataModels.Boolean.False
                    : TargetingSystemDataModels.Boolean.True;
            }
        }

        /// <summary>
        /// recursively count all the transforms in the scene.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="numberOfObjects"></param>
        /// <returns></returns>
        internal static int CountObjectsInTransformHierarchy(Transform root, int numberOfObjects)
        {
            int childCount = root.childCount;

            if (childCount == 0)
            {
                return numberOfObjects;
            }

            for (var index = 0; index < childCount; index++)
            {
                if (root.gameObject.layer == TargetingSystemSettings.IgnoreRayCastLayer)
                {
                    continue;
                }

                Transform transform = root.GetChild(index);
                numberOfObjects = CountObjectsInTransformHierarchy(transform, numberOfObjects + 1);
            }

            return numberOfObjects;
        }


        /// <summary>
        /// Gets all the transforms from list of root objects
        /// </summary>
        /// <param name="rootObjects"></param>
        /// <param name="targetTransforms"></param>
        /// <returns></returns>
        public static HashSet<Transform> GetUntaggedTransformsFromRootObjects(List<GameObject> rootObjects,
            HashSet<Transform> targetTransforms)
        {
            int numberOfObjects = 0;

            for (var index = 0; index < rootObjects.Count; index++)
            {
                var root = rootObjects[index];
                if (root.transform.CompareTag("Untagged") &&
                    root.gameObject.layer != TargetingSystemSettings.IgnoreRayCastLayer
                    && root.gameObject.activeSelf)
                {
                    targetTransforms.Add(root.transform);
                    GetUntaggedObjectsInTransformHierarchy(root.transform, ref targetTransforms, numberOfObjects + 1);
                }
            }

            return targetTransforms;
        }


        private static int GetUntaggedObjectsInTransformHierarchy(Transform transformRootToIterate,
            ref HashSet<Transform> targetList, int numberOfObjects)
        {
            int childCount = transformRootToIterate.childCount;
            for (var index = 0; index < childCount; index++)
            {
                var child = transformRootToIterate.GetChild(index);
                if (child.CompareTag("Untagged") && transformRootToIterate.GetChild(index).gameObject.layer !=
                    TargetingSystemSettings.IgnoreRayCastLayer)
                {
                    targetList.Add(transformRootToIterate.GetChild(index));
                    GetUntaggedObjectsInTransformHierarchy(transformRootToIterate.GetChild(index), ref targetList,
                        numberOfObjects + 1);
                }
            }

            return numberOfObjects;
        }

        internal static HashSet<Transform> UpdateTransformSceneChanges(string targetTag,
            HashSet<Transform> transformsIn, List<GameObject> rootObjects)
        {
#if TARGETINGS_SYSTEM_GAMEOBJECT_TARGETCREATOR_DEBUG
            Debug.Log("__<Update scene transforms>__");

#endif
            if (targetTag.Length == 0)
            {
                targetTag = "Untagged";
            }

            if (targetTag == "Untagged")
            {
                //GameObject find with tag cannot find objects tagged with "Untagged"
                transformsIn = FindUntaggedTransforms(transformsIn);
            }
            else
            {
                transformsIn = FindTaggedTransforms(targetTag, transformsIn);
            }
            return transformsIn;


            HashSet<Transform> FindUntaggedTransforms(HashSet<Transform> transformsIn)
            {
                var foundTransforms =
                    GetUntaggedTransformsFromRootObjects(rootObjects, transformsIn);

                foreach (var transformWithTag in foundTransforms)
                {
                    if (transformWithTag.CompareTag("Untagged") &&
                        transformWithTag.gameObject.layer != TargetingSystemSettings.IgnoreRayCastLayer
                    )
                    {
                        transformsIn.Add(transformWithTag);
                    }
                }

                return transformsIn;
            }

            HashSet<Transform> FindTaggedTransforms(string tagIn, HashSet<Transform> transformsIn)
            {
                foreach (var gameObjectIn in GameObject.FindGameObjectsWithTag(tagIn))
                {
                    if (gameObjectIn.layer != TargetingSystemSettings.IgnoreRayCastLayer)
                    {
                        transformsIn.Add(gameObjectIn.transform);
                    }
                }

                return transformsIn;
            }
        }

        internal static bool TargetingInstructionTagExistsOnCollection(string targetTag,
            Dictionary<string, HashSet<Transform>> transformsGroupedByTags)
        {
            return transformsGroupedByTags
                .FirstOrDefault(valuePair => valuePair.Key == targetTag).Key != null;
        }


        internal static Dictionary<string, HashSet<Transform>> AddTransformsToTagDictionary(
            string targetTag, Dictionary<string, HashSet<Transform>> dictionary, HashSet<Transform> allTransforms)
        {
            if (TargetingInstructionTagExistsOnCollection(targetTag, dictionary))
            {
                if (allTransforms == null)
                {
                    return dictionary;
                }

                foreach (var transform1 in allTransforms)
                {
                    dictionary[targetTag].Add(transform1);
                }
            }
            else
            {
                if (allTransforms != null)
                {
                    dictionary.Add(targetTag, new HashSet<Transform>(allTransforms));
                }
            }

            return dictionary;
        }

        public static bool CheckIfUpdateIsNeededFromInstructions(HashSet<GV_TargetingSystem> targetingSystems,
            GeometryType geometryType)
        {
            bool updateIsNeeded = false;
            foreach (var targetingSystem in targetingSystems)
            {
                if (targetingSystem.GameObjectBasedProcessing.Value == false)
                {
                    continue;
                }

                foreach (var targetingInstruction in targetingSystem.TargetingInstructionsWithRefresh)
                {
                    if (targetingInstruction.IsTargetingEnabled == false ||
                        geometryType != targetingInstruction.GeometryType)
                    {
                        continue;
                    }

                    if (targetingInstruction.NeedsUpdate == true)
                    {
                        targetingInstruction.NeedsUpdate = false;
                        updateIsNeeded = true;
                    }
                }
            }

            return updateIsNeeded;
        }

        public static void UpdatedInstructions(HashSet<GV_TargetingSystem> targetingSystems, GeometryType geometryType)
        {
            foreach (var targetingSystem in targetingSystems)
            {
                if (targetingSystem.GameObjectBasedProcessing.Value == false)
                {
                    continue;
                }

                foreach (var targetingInstruction in targetingSystem.TargetingInstructionsWithRefresh)
                {
                    if (targetingInstruction.IsTargetingEnabled == false ||
                        geometryType != targetingInstruction.GeometryType)
                    {
                        continue;
                    }

                    if (targetingInstruction.NeedsUpdate == true)
                    {
                        targetingInstruction.NeedsUpdate = false;
                    }
                }
            }
        }


        public static HashSet<string> CollectTagsWithGeometryTypeFromSystems(
            HashSet<GV_TargetingSystem> targetingSystems,
            HashSet<string> tagsIn, bool getTAgsForAllTargetedTypes, GeometryType geometryType)
        {
            tagsIn.Clear();

            foreach (var targetingSystem in targetingSystems)
            {
                if (targetingSystem.GameObjectBasedProcessing.Value == false)
                {
                    continue;
                }

                CollectTagsFromTargetingSystemInstructions(tagsIn, geometryType, targetingSystem);
            }

            return tagsIn;

            void CollectTagsFromTargetingSystemInstructions(HashSet<string> hashSetTagsIn, GeometryType geometryType1,
                GV_TargetingSystem targetingSystem)
            {
                foreach (var targetingInstruction in targetingSystem.TargetingInstructions)
                {
                    if (getTAgsForAllTargetedTypes)
                    {
                        hashSetTagsIn.Add(targetingInstruction.TargetTag);
                        continue;
                    }

                    if (targetingInstruction.IsTargetingEnabled == false ||
                        (targetingInstruction.GeometryType != geometryType1))
                    {
                        continue;
                    }

                    hashSetTagsIn.Add(targetingInstruction.TargetTag);
                }
            }
        }

        /// <summary>
        ///Check the give factory for null and in case its null recreates the factory and returns it
        /// </summary>
        /// <param name="factoryIn"></param>
        /// <returns></returns>
        public static TargetingSystemFactory TryReCreateFactory(TargetingSystemFactory factoryIn)
        {
            if (factoryIn == null)
            {
                factoryIn = new TargetingSystemFactory();
            }

            return factoryIn;
        }
    }
}