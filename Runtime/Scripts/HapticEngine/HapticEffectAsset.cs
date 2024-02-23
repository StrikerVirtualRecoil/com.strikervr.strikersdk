using StrikerLink.Unity.Runtime.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StrikerLink.Unity.Runtime.HapticEngine
{
    public class HapticEffectAsset : ScriptableObject
    {
        public string libraryId;
        public string effectName;
        
        //public float intensityModifier = 1f;
        //public float durationModifier = 1f;
        //public float frequencyModifier = 1f;


        public void Fire(int deviceIndex, float intensityModifier = 1f, float durationModifier = 1f, float frequencyModifier = 1f)
        {
            StrikerController.Controller.GetClient().FireHaptic((ushort)deviceIndex, StrikerController.Controller.libraryPrefix + libraryId, effectName, intensityModifier, durationModifier, frequencyModifier);
        }
    }
}