using Newtonsoft.Json;
using StrikerLink.Unity.Runtime.HapticEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace StrikerLink.Unity.Editor.AssetPipeline
{

    [ScriptedImporter(1, "hapt", AllowCaching = false)]
    public class HapticImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            Debug.Log("[DEBUG] Found V1 Haptics File!");

            string key = Path.GetFileNameWithoutExtension(ctx.assetPath);

            Debug.Log("Importing Effects Library: " + key);

            HapticLibraryAsset library = ScriptableObject.CreateInstance<HapticLibraryAsset>();

            library.libraryKey = key;

            string json = File.ReadAllText(ctx.assetPath);

            BasicHapticLibraryData data = JsonConvert.DeserializeObject<BasicHapticLibraryData>(json);

            library.json = json;
            library.effectCount = data.Effects != null ? data.Effects.Count : 0;
            library.paletteCount = data.SamplesPalette != null ? data.SamplesPalette.Count : 0;

            ctx.AddObjectToAsset("Library - " + key, library);

            ctx.SetMainObject(library);

            List<string> addedIds = new List<string>();

            foreach(BasicEffectData effect in data.Effects)
            {
                if(addedIds.Contains(effect.EffectId))
                {
                    Debug.LogWarning("[HAPTICS] Tried to add duplicate effect with key '" + effect.EffectId + "' as sub-asset of haptic library '" + library.libraryKey + "'");
                }

                HapticEffectAsset effectAsset = ScriptableObject.CreateInstance<HapticEffectAsset>();
                effectAsset.name = effect.EffectId;
                effectAsset.effectName = effect.EffectId;
                effectAsset.libraryId = key;

                ctx.AddObjectToAsset(effect.EffectId, effectAsset);

                addedIds.Add(effect.EffectId);
            }
        }
    }
}