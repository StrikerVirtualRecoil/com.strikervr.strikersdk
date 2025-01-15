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
            public UnityHapticSequence sequence;


        }

        [System.Serializable]
        public class UnityHapticSequence
        {
            //public bool buildNewSequence;

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
                    if (sampleObject == null || !modifyIntensity)
                        return new float[0];

                    if (sampleObject.primitiveSequence.Count > 1)
                    {
                        List<float> modifiers = new List<float>();

                        for (int i = 0; i < sampleObject.primitiveSequence.Count; i++)
                        {
                            modifiers.Add(intensityCurve.Evaluate((float)i / (float)(sampleObject.primitiveSequence.Count - 1)));
                        }

                        return modifiers.ToArray();

                    }
                    /*else if (sampleObject.primitiveSequence.Count == 1)
                    {
                        TriggerBuildSequence();
                        return new float[0];
                    }*/
                    else
                    {
                        return new float[0];
                    }
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

                    if (sampleObject.primitiveSequence.Count > 1)
                    {
                        List<float> modifiers = new List<float>();
                        for (int i = 0; i < sampleObject.primitiveSequence.Count; i++)
                        {
                            modifiers.Add(frequencyCurve.Evaluate((float)i / (float)(sampleObject.primitiveSequence.Count - 1)));
                        }

                        return modifiers.ToArray();
                    }
                    /*else if (sampleObject.primitiveSequence.Count == 1)
                    {
                        TriggerBuildSequence();
                        return new float[0];
                    }*/
                    else
                    {
                        return new float[0];
                    }
                }
            }

            [JsonProperty("is_modify_duration")]
            public bool modifyDuration;

            [JsonProperty("duration_target")]
            public float durationTarget;

            [JsonProperty("overlay")]
            public bool overlay;

            [JsonProperty("overdrive")]
            public bool overdrive;

            [JsonProperty("Waveform")]
            public WaveformType waveform;

            public enum WaveformType
            {
                Sine = 0,
                Square = 1,
                Saw = 2,
                Triangle = 3,
                Noise = 4
            }

            /*private void TriggerBuildSequence()
            {
                buildNewSequence = true;
            }*/

            // This function processes both intensity and frequency modifiers for a single vibrate primitive
            private List<UnityHapticSample.UnityPrimitiveData> BuildSequenceFromModifiers()
            {
                List<UnityHapticSample.UnityPrimitiveData> newSequence = new List<UnityHapticSample.UnityPrimitiveData>();

                // Get the single primitive and clear the sequence
                UnityHapticSample.UnityPrimitiveData originalPrimitive = sampleObject.primitiveSequence[0];
                

                // Get keyframes from the curves
                Keyframe[] intensityKeyframes = intensityCurve.keys;
                Keyframe[] frequencyKeyframes = frequencyCurve.keys;

                // Determine the maximum number of keyframes between both curves
                int maxKeyframes = Mathf.Max(intensityKeyframes.Length, frequencyKeyframes.Length);

                // Set up initial intensity and frequency values using either the first keyframe or the original primitive's values
                float prevIntensityValue = (intensityKeyframes.Length > 0) ? intensityKeyframes[0].value : originalPrimitive.intensity;
                float nextIntensityValue = prevIntensityValue;
                float prevFrequencyValue = (frequencyKeyframes.Length > 0) ? frequencyKeyframes[0].value : originalPrimitive.frequency;
                float nextFrequencyValue = prevFrequencyValue;

                // Initialize indices to track our position in the keyframes arrays
                int nextIntensityKeyframeIndex = 0;
                int nextFrequencyKeyframeIndex = 0;

                // Process each keyframe
                for (int i = 0; i < maxKeyframes; i++)
                {
                    // Determine the time value for this iteration, choosing the smallest if both exist, or whichever is present
                    float timeModifier = (i < intensityKeyframes.Length) ? intensityKeyframes[i].time : frequencyKeyframes[i].time;
                    if (i < intensityKeyframes.Length && i < frequencyKeyframes.Length)
                    {
                        timeModifier = Mathf.Min(intensityKeyframes[i].time, frequencyKeyframes[i].time);
                    }

                    // Check if there's a matching intensity keyframe for the current time
                    bool hasIntensityKeyframe = nextIntensityKeyframeIndex < intensityKeyframes.Length && Mathf.Approximately(intensityKeyframes[nextIntensityKeyframeIndex].time, timeModifier);
                    if (hasIntensityKeyframe)
                    {
                        prevIntensityValue = intensityKeyframes[nextIntensityKeyframeIndex].value;
                        nextIntensityKeyframeIndex++;
                        if (nextIntensityKeyframeIndex < intensityKeyframes.Length)
                        {
                            nextIntensityValue = intensityKeyframes[nextIntensityKeyframeIndex].value;
                        }
                    }
                    else if (intensityKeyframes.Length != 0)  // Interpolation for intensity when missing
                    {
                        if (nextIntensityKeyframeIndex < intensityKeyframes.Length)
                        {
                            float lerpFactor = (timeModifier - intensityKeyframes[nextIntensityKeyframeIndex - 1].time) / (intensityKeyframes[nextIntensityKeyframeIndex].time - intensityKeyframes[nextIntensityKeyframeIndex - 1].time);
                            prevIntensityValue = Mathf.Lerp(prevIntensityValue, nextIntensityValue, lerpFactor);
                        }
                    }

                    // Check if there's a matching frequency keyframe for the current time
                    bool hasFrequencyKeyframe = nextFrequencyKeyframeIndex < frequencyKeyframes.Length && Mathf.Approximately(frequencyKeyframes[nextFrequencyKeyframeIndex].time, timeModifier);
                    if (hasFrequencyKeyframe)
                    {
                        prevFrequencyValue = frequencyKeyframes[nextFrequencyKeyframeIndex].value;
                        nextFrequencyKeyframeIndex++;
                        if (nextFrequencyKeyframeIndex < frequencyKeyframes.Length)
                        {
                            nextFrequencyValue = frequencyKeyframes[nextFrequencyKeyframeIndex].value;
                        }
                    }
                    else if (frequencyKeyframes.Length != 0)  // Interpolation for frequency when missing
                    {
                        if (nextFrequencyKeyframeIndex < frequencyKeyframes.Length)
                        {
                            float lerpFactor = (timeModifier - frequencyKeyframes[nextFrequencyKeyframeIndex - 1].time) / (frequencyKeyframes[nextFrequencyKeyframeIndex].time - frequencyKeyframes[nextFrequencyKeyframeIndex - 1].time);
                            prevFrequencyValue = Mathf.Lerp(prevFrequencyValue, nextFrequencyValue, lerpFactor);
                        }
                    }

                    // Calculate times and durations based on the original primitive
                    int denormalizedFrequencyTime = Mathf.RoundToInt(timeModifier * originalPrimitive.duration);
                    int denormalizedIntensityTime = Mathf.RoundToInt(timeModifier * originalPrimitive.duration);
                    int nextKeyframeTime = (i < intensityKeyframes.Length - 1) ? Mathf.RoundToInt(intensityKeyframes[i + 1].time * originalPrimitive.duration) : originalPrimitive.duration;
                    int denormalizedDuration = nextKeyframeTime - denormalizedFrequencyTime;

                    // Create and add the new primitive based on the calculated values
                    UnityHapticSample.UnityPrimitiveData newPrimitive = new UnityHapticSample.UnityPrimitiveData
                    {
                        command = originalPrimitive.command,
                        frequency = (prevFrequencyValue * originalPrimitive.frequency),
                        frequencyTime = denormalizedFrequencyTime,
                        duration = denormalizedDuration,
                        intensity = Mathf.RoundToInt(prevIntensityValue * originalPrimitive.intensity),
                        intensityTime = denormalizedIntensityTime,
                        waveform = originalPrimitive.waveform,
                        overlay = originalPrimitive.overlay,
                        overDrive = originalPrimitive.overDrive
                    };

                    newSequence.Add(newPrimitive);
                }

                return newSequence;
            }
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
            // First, collect unique samples 
            List<UnityHapticSample.SerializableHapticSample> allSamples = new List<UnityHapticSample.SerializableHapticSample>();
            foreach (var effect in effects)
            {
                foreach (var track in effect.tracks)
                {
                    if (track.sequence.sampleObject != null)
                    {
                        var serializableSample = track.sequence.sampleObject.GetSerializableSample();

                        // Apply overlay, overdrive, waveform, etc. 
                        serializableSample.SetOverlay(track.sequence.overlay);
                        serializableSample.SetOverdrive(track.sequence.overdrive);
                        serializableSample.SetWaveform(track.sequence.waveform);

                        allSamples.Add(serializableSample);
                    }
                }
            }
            // Remove duplicates
            allSamples = allSamples.Distinct().ToList();

            // Build the "effects" portion of your JSON with an array for each track.sequence
            var effectsArray = effects.Select(effect => new
            {
                // The old JSON used "effect_id" instead of "id"
                effect_id = effect.id,

                // Each effect has an array of tracks
                tracks = effect.tracks.Select(track => new
                {
                    deviceId = track.deviceId,

                    // Even though we only have one sequence in C#,
                    // we wrap it in an array so the JSON shows [ { ... } ]
                    sequence = new[]
                    {
                new
                {
                    // Mapping your single sequence fields to old JSON property names:
                    is_modify_intensity = track.sequence.modifyIntensity,
                    is_modify_frequency = track.sequence.modifyFrequency,
                    is_modify_duration = track.sequence.modifyDuration,
                    duration_target = track.sequence.durationTarget,
                    overlay = track.sequence.overlay,
                    overdrive = track.sequence.overdrive,
                    Waveform = (int)track.sequence.waveform,

                    // If you store the sample ID in track.sequence.sampleObject.GetSerializableSample().sampleId
                    // or in track.sequence.sampleId, map it appropriately:
                    sample_id_to_play = track.sequence.DerivedSampleId,

                    // If you used to store intensity/frequency curves as arrays, keep them empty or adapt them
                    factor_intensity = new float[0],
                    factor_frequency = new float[0]
                }
            }
                }).ToList()
            }).ToList();

           

            // Combine "effects" and "samples_palette" into the final structure
            var finalJsonObject = new
            {
                effects = effectsArray,
                samples_palette = allSamples
            };

            // Convert to JSON string (using JSON.NET, as you do with JsonConvert)
            return JsonConvert.SerializeObject(finalJsonObject, Formatting.Indented);
        }

    }
}