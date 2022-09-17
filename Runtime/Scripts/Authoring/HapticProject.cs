using Malee.List;
using Newtonsoft.Json;
using StrikerLink.Shared.Haptics.Engines.V1.Types;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StrikerLink.Unity.Authoring
{
    [CreateAssetMenu(fileName = "NewHapticLibrary", menuName = "StrikerVR/Haptic Library")]
    public class HapticProject : ScriptableObject
    {
        public string identifier = "my_haptic_library";
        public List<UnityHapticEffect> effects;



        [System.Serializable]
        public class UnityHapticEffect
        {
            [JsonProperty("effect_id")]
            public string id;

            [JsonProperty("tracks")]
            public List<UnityHapticTrack> tracks;
        }

        [System.Serializable]
        public class UnityHapticTrack
        {
            [JsonProperty("deviceId")]
            public string deviceId;

            [JsonProperty("sequence")]
            public List<UnityHapticSequence> sequence;
        }

        [System.Serializable]
        public class UnityHapticSequence
        {
            [JsonIgnore]
            public UnityHapticSample sampleObject;

            [JsonProperty("sample_id_to_play")]
            public string DerivedSampleId
            {
                get
                {
                    if (sampleObject != null)
                        return sampleObject.name;
                    else
                        return null;
                }
            }


            [JsonProperty("is_modify_intensity")]
            public bool modifyIntensity;

            [JsonIgnore]
            public AnimationCurve intensityCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

            [JsonProperty("factor_intensity")]
            public float[] intensityModifiers
            {
                get
                {
                    if(sampleObject == null || !modifyIntensity)
                        return new float[0];

                    List<float> modifiers = new List<float>();

                    for (int i = 0; i < sampleObject.primitiveSequence.Count; i++)
                    {
                        modifiers.Add(intensityCurve.Evaluate((float)i / (float)(sampleObject.primitiveSequence.Count - 1)));
                    }

                    return modifiers.ToArray();
                }
            }

            [JsonProperty("is_modify_frequency")]
            public bool modifyFrequency;

            [JsonIgnore]
            public AnimationCurve frequencyCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

            [JsonProperty("factor_frequency")]
            public float[] frequencyModifiers
            {
                get
                {
                    if (sampleObject == null || !modifyFrequency)
                        return new float[0];

                    List<float> modifiers = new List<float>();

                    for(int i = 0; i < sampleObject.primitiveSequence.Count; i++)
                    {
                        modifiers.Add(frequencyCurve.Evaluate((float)i / (float)(sampleObject.primitiveSequence.Count - 1)));
                    }

                    return modifiers.ToArray();
                }
            }

            [JsonProperty("is_modify_duration")]
            public bool modifyDuration;

            [JsonProperty("duration_target")]
            public float durationTarget;
        }

        [System.Serializable]
        internal class UnityHapticPayload
        {
            [JsonProperty("effects")]
            public List<UnityHapticEffect> Effects;

            [JsonProperty("samples_palette")]
            public List<UnityHapticSample.SerializableHapticSample> SamplesPalette;

        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(new UnityHapticPayload()
            {
                Effects = effects,
                SamplesPalette = effects.SelectMany(x => x.tracks).SelectMany(x => x.sequence).Select(x => x.sampleObject).Distinct().Select(x => x.GetSerializableSample()).ToList()
            }, Formatting.Indented);
        }
    }
}