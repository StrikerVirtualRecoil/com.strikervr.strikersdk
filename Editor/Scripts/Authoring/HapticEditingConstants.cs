using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StrikerLink.Unity.Editor.Authoring
{
    public static class HapticEditingConstants
    {
        internal class ValueMapping
        {
            public string Id { get; private set; }
            public string Text { get; private set; }

            public ValueMapping(string id, string text)
            {
                this.Id = id;
                this.Text = text;
            }

            public static int GetIndexForId(string id, List<ValueMapping> map)
            {
                for(int i = 0; i < map.Count; i++)
                {
                    if (map[i].Id == id)
                        return i;
                }

                return 0;
            }

            public static string GetIdForIndex(int index, List<ValueMapping> map)
            {
                return map.Count > index ? map[index].Id : null;
            }

            public static string GetTextForId(string id, List<ValueMapping> map)
            {
                ValueMapping[] res = map.Where(x => x.Id.ToLower() == id.ToLower()).ToArray();

                if (res.Length > 0)
                    return res[0].Text;
                else
                    return "???";
            }
        }

        internal static List<ValueMapping> DeviceIdMap = new List<ValueMapping>()
        {
            new ValueMapping("hammerTop", "Thunder (Top)"),
            new ValueMapping("fosterFront", "Cricket (Front)"),
            new ValueMapping("fosterBack", "Cricket (Back)"),
            new ValueMapping("fosters", "Cricket (Both)"),
        };

        internal static List<ValueMapping> CommandMap = new List<ValueMapping>()
        {
            new ValueMapping("tick", "Tick"),
            new ValueMapping("pulse", "Pulse"),
            new ValueMapping("vibrate", "Vibrate"),
            new ValueMapping("pause", "Pause"),
        };
    }
}