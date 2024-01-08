using System.Collections;
using Plugins.GeometricVision.TargetingSystem.BaseCode.DataModels;
using Plugins.GeometricVision.TargetingSystem.BaseCode.MainClasses;
using Unity.Mathematics;
using UnityEngine;

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.Audio
{
    public class AudioPoolManager
    {
        private AudioPoolItem[] audioSourcePool = new AudioPoolItem[0];
        private Transform audioPoolParent = null;
        internal TargetingSystemsRunner runner = null;
        private bool warningMessageShown = false;
        public AudioPoolItem[] AudioSourcePool
        {
            get { return this.audioSourcePool; }
            set { this.audioSourcePool = value; }
        }
  
        /// <summary>
        /// Recreates audio pool by flushing it and then rebuilding it
        /// </summary>
        /// <param name="audioPoolSize"></param>
        internal void ResizeAudioPool(int audioPoolSize, MonoBehaviour owner)
        {
            this.FlushAudioPool(owner, false);
            if (audioPoolSize == 0)
            {
                return;
            }

            if (this.audioPoolParent == null)
            {
                this.audioPoolParent = (new GameObject {name = TargetingSystemSettings.AudioPoolParentName}).transform;
            }
            this.audioPoolParent.gameObject.layer = TargetingSystemSettings.IgnoreRayCastLayer;
            this.audioSourcePool = new AudioPoolItem[audioPoolSize];
            this.audioPoolParent.transform.position = TargetingSystemSettings.DefaultTransformLocation;
            var poolItemFirst = new GameObject {name = TargetingSystemSettings.AudioPoolItemName};

            for (int i = 0; i < audioPoolSize; i++)
            {
                var poolObject = Object.Instantiate(poolItemFirst, this.audioPoolParent.transform);
                poolObject.transform.localPosition = Vector3.zero;
                poolObject.name =  TargetingSystemSettings.AudioPoolItemName;
                poolObject.AddComponent<AudioSource>();
                poolObject.name = poolObject.name + "_" + i;
                poolObject.layer = TargetingSystemSettings.IgnoreRayCastLayer;
                var poolItem = new AudioPoolItem
                {
                    AudioSource = poolObject.GetComponent<AudioSource>(),
                    audioSourceTransform = poolObject.transform
                    
                };
                poolItem.AudioSource.spatialize = true;
                poolItem.AudioSource.spatialBlend = 1f;
                this.audioSourcePool[i] = poolItem;
            }
            Object.Destroy(poolItemFirst);
        }
        
        public void FlushAudioPool(MonoBehaviour owner, bool stopCoroutines)
        {
            if (this.audioSourcePool == null)
            {
                return;
            }

            if (stopCoroutines)
            {
                owner.StopAllCoroutines(); 
            }
            
            
            if (this.audioPoolParent != null)
            {
                Object.Destroy(this.audioPoolParent.gameObject);
            }

            this.audioSourcePool = null;
        }
        /// <summary>
        /// Reserve pool item for playing audio from the pool of game objects containing audiosource
        /// </summary>
        /// <param name="parentTargetingSystemTransform">Usually the targeting system calling to spawn targeting effects</param>
        /// <param name="positionToSpawn"></param>
        /// <param name="actionsElement"></param>
        /// <param name="owner"></param>
        public void PlayAudioAt(Transform parentTargetingSystemTransform, float3 positionToSpawn, ActionsTemplateTriggerActionElement actionsElement, MonoBehaviour owner)
        {
            var audioPoolObject = this.TryGetAudioItemFromPool(this.audioSourcePool);

            if (audioPoolObject == null || this.audioSourcePool.Length == 0)
            {
                #if UNITY_EDITOR
                if (this.runner.suppressEditorWarnings == false && this.warningMessageShown == false)
                {
                    this.warningMessageShown = true;
                    Debug.Log("Audio pool size exceeded ("+ this.audioSourcePool.Length+") while trying to play audio. This means that you either have too small pool size or too many audio triggers triggered from targeting effects system. " +
                              "\nPool size can be increased from the TargetingSystemsRunner gameObject. " +
                              "\nThis message is harmless and can be turned off from targeting system runner -> suppress editor warnings");
                    
                }
                #endif
                return;
            }
            
            audioPoolObject = ConfigureAudioPoolObjectForPlaying(audioPoolObject, parentTargetingSystemTransform, positionToSpawn, actionsElement);

            // Finally run coroutine that waits until delay, if any is exhausted 
            owner.StartCoroutine(this.PlayAudioAfter(audioPoolObject));

            AudioPoolItem ConfigureAudioPoolObjectForPlaying(AudioPoolItem audioPoolObjectIn, Transform parentTargetingSystemTransform1, Vector3 positionIn,
                ActionsTemplateTriggerActionElement triggerActionElement)
            {
                audioPoolObjectIn.AudioSource.clip = triggerActionElement.AudioClipToUse;

                if (triggerActionElement.UnParent == false)
                {
                    audioPoolObjectIn.audioSourceTransform.parent = parentTargetingSystemTransform1;
                }
                else
                {
                    audioPoolObjectIn.audioSourceTransform.parent = this.audioPoolParent;
                }

                audioPoolObjectIn.AudioSource.spatialBlend = triggerActionElement.AudioSpatialBlend;
                audioPoolObjectIn.AudioSource.volume = triggerActionElement.AudioVolume;
                audioPoolObjectIn.audioSourceTransform.position = positionIn;
                return audioPoolObjectIn;
            }
        }

        
        public void PlayAudioAt(Transform parentTargetingSystemTransform, float3 positionToSpawn, AudioSource audioSource, bool unparentFromSource, MonoBehaviour owner)
        {
            var audioPoolObject = this.TryGetAudioItemFromPool(this.audioSourcePool);

            if (audioPoolObject == null)
            {
                if (this.runner.suppressEditorWarnings == false && this.warningMessageShown == false )
                {
                    this.warningMessageShown = true;
                    Debug.Log("Audio pool size exceeded. This means that you either have too small pool size or too many audio triggers triggered from targeting effects system. Pool size can be increased from the TargetingSystemsRunner gameobject");
                }
                    
                return;
            }
            
            audioPoolObject = ConfigureAudioPoolObjectForPlaying(audioPoolObject, parentTargetingSystemTransform, positionToSpawn);

            // Finally run coroutine that waits until delay, if any is exhausted 
            owner.StartCoroutine(this.PlayAudioAfter(audioPoolObject));

            AudioPoolItem ConfigureAudioPoolObjectForPlaying(AudioPoolItem audioPoolObjectIn, Transform parentTargetingSystemTransform1, float3 float3)
            {
                audioPoolObjectIn.AudioSource.clip = audioSource.clip;

                if (unparentFromSource == false && parentTargetingSystemTransform1 != null)
                {
                    audioPoolObjectIn.audioSourceTransform.parent = parentTargetingSystemTransform1;
                }
                else
                {
                    audioPoolObjectIn.audioSourceTransform.parent = this.audioPoolParent;
                }
                audioPoolObjectIn.AudioSource.spatialBlend = audioSource.spatialBlend;
                audioPoolObjectIn.AudioSource.volume = audioSource.volume;
                audioPoolObjectIn.AudioSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
                audioPoolObjectIn.audioSourceTransform.position = float3;
                
                return audioPoolObject;
            }
        }   

        private AudioPoolItem TryGetAudioItemFromPool(AudioPoolItem[] audioSourcePoolIn)
        {
            
            for (int i = 0; i < audioSourcePoolIn.Length; i++)
            {
                if (audioSourcePoolIn[i].IsInUse == false)
                {
                    audioSourcePoolIn[i].IsInUse = true;
                    audioSourcePoolIn[i].Index = i;
                    return audioSourcePoolIn[i];
                }
            }

            return null;
        }

        private IEnumerator PlayAudioAfter(AudioPoolItem audioPoolObject)
        {
            audioPoolObject.DelayAmountLeft -= Time.deltaTime;
            while (audioPoolObject.DelayAmountLeft > 0)
            {
                yield return null;
            }
            audioPoolObject.AudioSource.Play();
            while (audioPoolObject.AudioSource.isPlaying == true) 
            {
                yield return null;
            }
            yield return null;
            audioPoolObject.IsInUse = false;
            audioPoolObject.AudioSource.clip = null;
            var transform = audioPoolObject.AudioSource.transform;
            transform.parent = this.audioPoolParent;
            transform.localPosition = Vector3.zero;
        }
 
    }
}
