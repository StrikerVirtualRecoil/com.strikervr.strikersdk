using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using CommonUsages = UnityEngine.XR.CommonUsages;

namespace StrikerLink.Unity.Runtime.Samples.Input
{
    public class TrackerInput : MonoBehaviour
    {
        List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();

        public bool isLeft = false;

        public bool IsTracking { get; set; }
        public bool HasTracker { get; set; }

        public enum SingleTrackerPickerBehaviour
        {
            UseAny,
            UseIfLeft,
            UseIfRight,
            UseNone,
        }

        public SingleTrackerPickerBehaviour singleTrackerBehaviour;
        public InputDeviceTrackerCharacteristicsSingleSelect steamLeftAssignment = InputDeviceTrackerCharacteristicsSingleSelect.TrackerCamera;
        public InputDeviceTrackerCharacteristicsSingleSelect steamRightAssignment = InputDeviceTrackerCharacteristicsSingleSelect.TrackerKeyboard;
        public bool useOpenXROnAndroid = false;

        bool isTrackedOutVar;
        Vector3 pos;
        Quaternion rot;

        // Start is called before the first frame update
        void Start()
        {
            if (!Application.isEditor && Application.platform != RuntimePlatform.WindowsPlayer && !useOpenXROnAndroid)
                Wave.OpenXR.InputDeviceTracker.ActivateTracker(true);
        }

        // Update is called once per frame
        void Update()
        {
            UnityEngine.XR.InputDevices.GetDevicesAtXRNode(XRNode.HardwareTracker, devices);

            HasTracker = false;
            IsTracking = false;

            if (devices == null || devices.Count < 1)
                return;

            UnityEngine.XR.InputDevice pickedDevice = devices[0]; // Just to ensure it's assigned somewhere

            if (devices.Count == 1)
            {
                pickedDevice = devices[0];

                if (singleTrackerBehaviour == SingleTrackerPickerBehaviour.UseAny)
                {
                    HasTracker = true;
                }
                else if (singleTrackerBehaviour == SingleTrackerPickerBehaviour.UseIfLeft)
                {
                    if (pickedDevice.characteristics.HasFlag(InputDeviceCharacteristics.Left) || pickedDevice.characteristics.HasFlag((InputDeviceCharacteristics)steamLeftAssignment))
                        HasTracker = true;
                    else
                        return;
                }
                else if (singleTrackerBehaviour == SingleTrackerPickerBehaviour.UseIfRight)
                {
                    if (pickedDevice.characteristics.HasFlag(InputDeviceCharacteristics.Right) || pickedDevice.characteristics.HasFlag((InputDeviceCharacteristics)steamRightAssignment))
                        HasTracker = true;
                    else
                        return;
                }
                else if (singleTrackerBehaviour == SingleTrackerPickerBehaviour.UseNone)
                {
                    HasTracker = false;
                    IsTracking = false;
                    return;
                }
            }
            else
            {
                try
                {
                    if (isLeft)
                        pickedDevice = devices.First(x => x.characteristics.HasFlag(InputDeviceCharacteristics.Left) || x.characteristics.HasFlag((InputDeviceCharacteristics)steamLeftAssignment));
                    else
                        pickedDevice = devices.First(x => x.characteristics.HasFlag(InputDeviceCharacteristics.Right) || x.characteristics.HasFlag((InputDeviceCharacteristics)steamRightAssignment));
                }
                catch
                {
                    IsTracking = false;
                    HasTracker = false;
                    return;
                }
            }

            // If we get this far, ensure these are set accordingly
            HasTracker = true;
            IsTracking = false;

            // No point grabbing data if it doesn't track (or fails to fetch this value)
            if (!pickedDevice.TryGetFeatureValue(CommonUsages.isTracked, out isTrackedOutVar) || !isTrackedOutVar)
                return;

            if (pickedDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out pos) && pickedDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out rot))
            {
                transform.localPosition = pos;
                transform.localRotation = rot;
                IsTracking = true;
            }
        }

        // Repeating this here without the Flags attribute for ease of inspector use
        public enum InputDeviceTrackerCharacteristicsSingleSelect : uint
        {
            TrackerLeftFoot = 0x1000u,
            TrackerRightFoot = 0x2000u,
            TrackerLeftShoulder = 0x4000u,
            TrackerRightShoulder = 0x8000u,
            TrackerLeftElbow = 0x10000u,
            TrackerRightElbow = 0x20000u,
            TrackerLeftKnee = 0x40000u,
            TrackerRightKnee = 0x80000u,
            TrackerWaist = 0x100000u,
            TrackerChest = 0x200000u,
            TrackerCamera = 0x400000u,
            TrackerKeyboard = 0x800000u
        }
    }
}