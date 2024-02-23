using Newtonsoft.Json;
using System;
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

            public void SetOverlay(bool overlayValue)
            {
                foreach (var primitive in primitiveSequence)
                {
                    primitive.overlay = overlayValue;
                }
            }

            public void SetOverdrive(bool overdriveValue)
            {
                foreach (var primitive in primitiveSequence)
                {
                    primitive.overDrive = overdriveValue;
                }
            }

            public void SetWaveform(HapticProject.UnityHapticSequence.WaveformType waveformValue)
            {
                int waveformIntValue = (int)waveformValue;
                foreach (var primitive in primitiveSequence)
                {
                    primitive.waveform = waveformIntValue;
                }
            }
        }

       

        [System.Serializable]
        public class UnityPrimitiveData
        {
            [JsonProperty("version")]
            public string version = "2.0";

            [JsonProperty("command")]
            public string command;

            [JsonProperty("frequency")]
            [JsonConverter(typeof(MultiplierConverter), 100, true)]
            public float frequency;

            [JsonProperty("frequency_time")]
            public int frequencyTime;

            [JsonProperty("duration_base")]
            public int duration;

            [JsonProperty("intensity")]
            //[JsonConverter(typeof(MultiplierConverter), 0.01f, false)]
            public float intensity;

            [JsonProperty("intensity_time")]
            public int intensityTime;

            [JsonProperty("waveform")]
            public int waveform;

            [JsonProperty("overlay")]
            public bool overlay;

            [JsonProperty("overDrive")]
            public bool overDrive;
        }

        public SerializableHapticSample GetSerializableSample()
        {
            return new SerializableHapticSample()
            {
                id = name,
                primitiveSequence = new List<UnityPrimitiveData>(primitiveSequence) // Create a new list from existing sequence
            };
        }
    }

    public class MultiplierConverter : JsonConverter
    {
        private readonly float _factor;
        private readonly bool _isInt;

        public MultiplierConverter(float factor, bool isInt)
        {
            _factor = factor;
            _isInt = isInt;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(float) || objectType == typeof(int);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Default behavior for reading
            return serializer.Deserialize(reader, objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (_isInt)
            {
                float originalValue = (float)value;
                int modifiedValue = (int)(originalValue * _factor);
                serializer.Serialize(writer, modifiedValue);
            }
            else
            {
                int originalValue = (int)value;
                float modifiedValue = originalValue * _factor;

                // Fix for float precision, rounding to two decimal places
                modifiedValue = (float)Math.Round(modifiedValue, 2);

                serializer.Serialize(writer, modifiedValue);
            }
        }
    }
}