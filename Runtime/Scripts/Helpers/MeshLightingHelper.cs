using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StrikerLink.Unity.Runtime.Helpers
{
    public class MeshLightingHelper : MonoBehaviour
    {
        [Header("Renderers")]
        public List<SkinnedMeshRenderer> blasterMeshes;

        [Header("Colors")]
        [ColorUsage(false, true)]
        public Color ringColor = Color.cyan;
        [ColorUsage(false, true)]
        public Color stripColor = Color.cyan;

        MaterialPropertyBlock blasterBlock;

        private void Awake()
        {
            blasterBlock = new MaterialPropertyBlock();
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            UpdateGunMeshes();
        }

        void UpdateGunMeshes()
        {
            foreach(SkinnedMeshRenderer blaster in blasterMeshes)
            {
                blaster.GetPropertyBlock(blasterBlock);
                blasterBlock.SetColor("_LEDStripColor", stripColor);
                blasterBlock.SetColor("_LEDRingColor", ringColor);
                blaster.SetPropertyBlock(blasterBlock);
            }
        }
    }
}