using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StrikerLink.Unity.Runtime.HapticEngine
{
    public class HapticLibraryAsset : ScriptableObject
    {
        public string libraryKey;
        public int effectCount = 0;
        public int paletteCount = 0;

        [HideInInspector]
        public string json;
    }
}