using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StrikerLink.Unity.Runtime.HapticEngine
{
    public class RepeatingHaptic
    {
        public int DeviceIndex { get; set; }
        public HapticEffectAsset Asset { get; set; }

        public float Interval { get; set; }

        float lastFire = 0f;

        /// <summary>
        /// Create a repeating haptic helper instance
        /// </summary>
        /// <param name="asset">The haptic to play</param>
        /// <param name="interval">The interval (in seconds) between playing the haptic</param>
        /// <param name="deviceIndex">The Striker Device Index</param>
        public RepeatingHaptic(HapticEffectAsset asset, float interval, int deviceIndex = 0)
        {
            Asset = asset;

            if(interval < .025f)
            {
                throw new InvalidOperationException("The Millisecond Interval must be at least 25 to avoid connection saturation");
            }

            Interval = interval;
        }

        /// <summary>
        /// Updates the repeating haptic loop, counting down (and/or playing) the next firing of the haptic
        /// </summary>
        /// <param name="intensity">Modify the normalized intensity of the haptic</param>
        /// <param name="frequency">Modify the normalized frequency of the haptic</param>
        /// <param name="duration">Modify the normalized duration of the haptic</param>
        public void Update(float intensity = 1f, float frequency = 1f, float duration = 1f)
        {
            if(lastFire < Time.time - Interval)
            {
                DoFireInternal(intensity, frequency, duration);
                lastFire = Time.time;
            }
        }

        void DoFireInternal(float intensity = 1f, float frequency = 1f, float duration = 1f)
        {
            Asset.Fire(DeviceIndex, intensity, frequency, duration);
        }
    }
}