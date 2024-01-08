using System;
using System.Collections;
using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using Plugins.GeometricVision.TargetingSystem.BaseCode.MainClasses;
using Plugins.GeometricVision.TargetingSystem.BaseCode.TargetingComponents;

using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;
using Observable = Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.UnityEngineBridge.Observable;
using ObservableExtensions = Plugins.GeometricVision.TargetingSystem.Code.UtilitiesAndPlugins.UniRx.Scripts.ObservableExtensions;
#if ENABLE_TARGETING_SYSTEM_ENTITY_SUPPORT
using Plugins.GeometricVision.TargetingSystem.Entities.Components.Core;
#endif

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.UtilitiesAndPlugins
{
    public static class TimedSpawnDespawn
    {
        /// <summary>
        /// Coroutine that can spawn and destroys spawned assetToSpawn based on delay and duration.
        /// </summary>
        /// <param name="actionsElement"></param>
        /// <param name="parentTargetingSystemTransform">if not null parentTargetingSystemTransform spawned assetToSpawn to this</param>
        /// <param name="positionToSpawn">World space position to use for spawning location</param>
        /// <param name="target"></param>
        /// <param name="runner"></param>
        /// <returns></returns>
        internal static IEnumerator TimedSpawnDeSpawnQueueEntityAndGameObjectService(
            ActionsTemplateTriggerActionElement actionsElement,
            Transform parentTargetingSystemTransform, float3 positionToSpawn, Target target,
            TargetingSystemsRunner runner)
        {
            GameObject spawnedObject = null;

            if (actionsElement.AudioClipToUse)
            {
                runner.AudioPoolManager.PlayAudioAt(parentTargetingSystemTransform, positionToSpawn, actionsElement,
                    runner);
            }


            SpawnGameObject(actionsElement, parentTargetingSystemTransform, positionToSpawn,
                    spawnedObject);
            

            yield return null;
        }

        private static GameObject SpawnGameObject(ActionsTemplateTriggerActionElement actionsElement,
            Transform parent, float3 positionToSpawn, GameObject spawnedObject)
        {
            ObservableExtensions.Subscribe(Spawn(actionsElement.Prefab, actionsElement.StartDelay, parent), instantiatedGameObject =>
                {
                    instantiatedGameObject.name = actionsElement.Prefab.name;
                    actionsElement.Name = instantiatedGameObject.name;
                    spawnedObject = instantiatedGameObject;

                    HandleParenting(parent, actionsElement, spawnedObject);
                    spawnedObject.transform.position = positionToSpawn;
                    spawnedObject.name = "Targeting system trigger action object";

                    if (actionsElement.GameObjectTag.Length > 0)
                    {
                        spawnedObject.tag = actionsElement.GameObjectTag;

                    }
                    //Put spawned effect object to layer that is ignored by the target creator. 
                    spawnedObject.layer = TargetingSystemSettings.IgnoreRayCastLayer;
                    ObservableExtensions.Subscribe(Observable.FromCoroutine(x => TimedDeSpawnService(spawnedObject, actionsElement)));
                        
                });
            return spawnedObject;


        }
        private static void HandleParenting(Transform parent, ActionsTemplateTriggerActionElement actionsElement, GameObject spawnedObject)
        {
            if (actionsElement.UnParent || parent == null)
            {
                return;
            }
            spawnedObject.transform.parent = parent;
            spawnedObject.transform.localPosition = Vector3.zero;
            spawnedObject.transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Coroutine that Spawns and returns a gameObject
        /// </summary>
        /// <param name="asset">Asset to spawn</param>
        /// <param name="delay">Amount to delay spawn in seconds</param>
        /// <param name="parentTransform">Copy settings from this transform to the just spawned one</param>
        /// <returns></returns>
        public static IObservable<GameObject> Spawn(GameObject asset, float delay, Transform parentTransform)
        {
            // convert coroutine to IObservable
            return Observable.FromCoroutine<GameObject>((observer, cancellationToken) =>
                TimedSpawnService(asset, observer, delay));

            IEnumerator TimedSpawnService(GameObject assetIn, IObserver<GameObject> observer, float delayIn)
            {
                while (delayIn > 0)
                {
                    var countedTimeScale = Time.deltaTime;
                    delayIn -= countedTimeScale;
                    yield return null;
                }

                if (assetIn != null)
                {
                    observer.OnNext(Object.Instantiate(assetIn, parentTransform.position, parentTransform.rotation));
                    observer.OnCompleted();
                }
            }
        }

        private static IEnumerator TimedDeSpawnService(GameObject spawnedObject, ActionsTemplateTriggerActionElement actionsElement)
        {
            var duration = actionsElement.Duration;
            while (duration > 0)
            {
                var countedTimeScale = Time.deltaTime;
                duration -= countedTimeScale;
                yield return null;
            }

            if (spawnedObject != null)
            {
                Object.Destroy(spawnedObject);
            }
        }
    }
}