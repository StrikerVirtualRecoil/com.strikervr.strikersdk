using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Declare the namespace for better organization.
namespace StrikerLink.Unity.Editor.Authoring
{
    // A static class containing constant values used for haptic editing.
    public static class HapticEditingConstants
    {
        // Inner class to represent a mapping between an ID and its corresponding display text.
        internal class ValueMapping
        {
            // Properties for ID and display text.
            public string Id { get; private set; }
            public string Text { get; private set; }

            // Constructor to initialize the ID and display text.
            public ValueMapping(string id, string text)
            {
                this.Id = id;
                this.Text = text;
            }

            // Static method to get the index of a given ID within the provided list of ValueMappings.
            public static int GetIndexForId(string id, List<ValueMapping> map)
            {
                // Iterate over the map to find the index of the ValueMapping with the given Id.
                for (int i = 0; i < map.Count; i++)
                {
                    if (map[i].Id == id)
                        return i; // Return the index if found.
                }
                return 0;  // Default index if ID not found.
            }

            // Static method to get the ID associated with a given index in the provided list of ValueMappings.
            public static string GetIdForIndex(int index, List<ValueMapping> map)
            {
                // Return the Id of the ValueMapping at the given index if it exists, otherwise return null.
                return map.Count > index ? map[index].Id : null;
            }

            // Static method to get the display text for a given ID in the provided list of ValueMappings.
            public static string GetTextForId(string id, List<ValueMapping> map)
            {
                // Find all the ValueMappings in the map that have the given Id.
                ValueMapping[] res = map.Where(x => x.Id.ToLower() == id.ToLower()).ToArray();

                // Return the text of the first matching ID if found, else return a default value.
                if (res.Length > 0)
                    return res[0].Text;
                else
                    return "???";
            }
        }

        // List to map device IDs to their respective display names.
        internal static List<ValueMapping> DeviceIdMap = new List<ValueMapping>()
        {
            new ValueMapping("hammerTop", "Thunder (Top)"),
            new ValueMapping("fosterFront", "Cricket (Front)"),
            new ValueMapping("fosterBack", "Cricket (Back)"),
            new ValueMapping("fosters", "Cricket (Both)"),
        };

        // List to map command IDs to their respective display names.
        internal static List<ValueMapping> CommandMap = new List<ValueMapping>()
        {
            new ValueMapping("tick", "Tick"),
            new ValueMapping("pulse", "Pulse"),
            new ValueMapping("vibrate", "Vibrate"),
            new ValueMapping("pause", "Pause"),
        };
    }
}
