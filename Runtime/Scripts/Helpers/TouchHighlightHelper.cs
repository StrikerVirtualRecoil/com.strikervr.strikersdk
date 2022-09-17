using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StrikerLink.Unity.Runtime.Helpers
{
    public class TouchHighlightHelper : MonoBehaviour
    {
        public MeshRenderer touchRenderer;

        [ColorUsage(false, true)]
        public Color touchColor = Color.cyan;

        [Range(0f, 1f)]
        public float touchPosition;
        [Range(0f, 1f)]
        public float touchPressure;

        MaterialPropertyBlock propertyBlock;

        private void Awake()
        {
            if (touchRenderer == null)
                touchRenderer = GetComponent<MeshRenderer>();

            propertyBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (touchRenderer == null)
            {
                Debug.Log("[STRIKER] Touch Renderer is not set for TouchHighlightHelper on " + gameObject.name + ": Disabling component to avoid log spam");
                enabled = false;
                return;
            }

            propertyBlock.SetFloat("_TouchPosition", touchPosition);
            propertyBlock.SetFloat("_TouchPressure", touchPressure);
            propertyBlock.SetColor("_Emissive", touchColor);

            touchRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}