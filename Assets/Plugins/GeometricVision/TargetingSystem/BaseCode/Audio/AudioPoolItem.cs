using UnityEngine;

namespace Plugins.GeometricVision.TargetingSystem.BaseCode.Audio
{
    public class AudioPoolItem
    {
        public int Index;
        public AudioSource AudioSource;
        internal Transform audioSourceTransform;
        public float DelayAmountLeft { get; set; }
        public bool IsInUse;
    }
}
