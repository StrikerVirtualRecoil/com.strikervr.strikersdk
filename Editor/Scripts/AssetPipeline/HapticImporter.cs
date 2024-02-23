using Newtonsoft.Json;
using StrikerLink.Unity.Runtime.HapticEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using UnityEngine;

namespace StrikerLink.Unity.Editor.AssetPipeline
{

    // Mark this class as a scripted importer for assets with the "hapt" extension.
    [ScriptedImporter(1, "hapt", AllowCaching = false)]
    public class HapticImporter : ScriptedImporter
    {
        // This method is invoked when an asset of type "hapt" is imported into Unity.
        public override void OnImportAsset(AssetImportContext ctx)
        {
            // Log a debug message indicating the discovery of a V1 Haptics File.
            Debug.Log("[DEBUG] Found V1 Haptics File!");

            // Extract the filename without extension to be used as a key.
            string key = Path.GetFileNameWithoutExtension(ctx.assetPath);
            Debug.Log("Importing Effects Library: " + key);

            // Create an instance of the HapticLibraryAsset.
            HapticLibraryAsset library = ScriptableObject.CreateInstance<HapticLibraryAsset>();

            // Assign the extracted key to the library's libraryKey field.
            library.libraryKey = key;

            // Read the contents of the asset file into a string.
            string json = File.ReadAllText(ctx.assetPath);

            // Deserialize the JSON string into a BasicHapticLibraryData object.
            BasicHapticLibraryData data = JsonConvert.DeserializeObject<BasicHapticLibraryData>(json);

            // Assign the JSON string and the counts of effects and palette samples to the library object.
            library.json = json;
            library.effectCount = data.Effects != null ? data.Effects.Count : 0;
            library.paletteCount = data.SamplesPalette != null ? data.SamplesPalette.Count : 0;

            // Add the library object to the assets being imported.
            ctx.AddObjectToAsset("Library - " + key, library);

            // Set the library object as the main asset.
            ctx.SetMainObject(library);

            // Create a list to keep track of the IDs that have been added.
            List<string> addedIds = new List<string>();

            // Iterate over each effect in the data's Effects list.
            foreach (BasicEffectData effect in data.Effects)
            {
                // Check if the effect's ID has already been added.
                if (addedIds.Contains(effect.EffectId))
                {
                    Debug.LogWarning("[HAPTICS] Tried to add duplicate effect with key '" + effect.EffectId + "' as sub-asset of haptic library '" + library.libraryKey + "'");
                    continue;  // Skip the rest of this iteration.
                }

                // Create a new HapticEffectAsset for the current effect.
                HapticEffectAsset effectAsset = ScriptableObject.CreateInstance<HapticEffectAsset>();
                effectAsset.name = effect.EffectId;
                effectAsset.effectName = effect.EffectId;
                effectAsset.libraryId = key;

                // Add the effect asset to the assets being imported.
                ctx.AddObjectToAsset(effect.EffectId, effectAsset);

                // Add the effect's ID to the list of added IDs.
                addedIds.Add(effect.EffectId);
            }
        }
    }
}