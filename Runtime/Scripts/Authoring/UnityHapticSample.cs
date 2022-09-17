using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StrikerLink.Unity.Authoring
{
    [CreateAssetMenu(fileName = "NewHapticSample", menuName = "StrikerVR/Haptic Sample")]
    public class UnityHapticSample : ScriptableObject
    {
        public List<UnityPrimitiveData> primitiveSequence = new List<UnityPrimitiveData>();

        [System.Serializable]
        public class SerializableHapticSample
        {
            [JsonProperty("sample_id")]
            public string id;

            [JsonProperty("sequence")]
            public List<UnityPrimitiveData> primitiveSequence;
        }

        [System.Serializable]
        public class UnityPrimitiveData
        {
            [JsonProperty("command")]
            public string command;

            [JsonProperty("intensity")]
            public float intensity;

            [JsonProperty("frequency")]
            public long frequency;

            [JsonProperty("duration_base")]
            public int duration;
        }

        public SerializableHapticSample GetSerializableSample()
        {
            return new SerializableHapticSample()
            {
                id = name,
                primitiveSequence = primitiveSequence
            };
        }
    }
}