using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StrikerLink.Unity.Runtime.HapticEngine
{
    public class BasicEffectData
    {
        [JsonProperty("effect_id")]
        public string EffectId;

        [JsonProperty("play_mode")]
        public string PlayMode;
    }

    public class BasicHapticLibraryData
    {
        [JsonProperty("samples_palette")]
        public List<object> SamplesPalette;

        [JsonProperty("effects")]
        public List<BasicEffectData> Effects;
    }
}