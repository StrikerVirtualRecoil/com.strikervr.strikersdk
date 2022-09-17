using StrikerLink.Shared.Client;
using StrikerLink.Shared.Devices;
using StrikerLink.Shared.Devices.DeviceFeatures;
using StrikerLink.Shared.Devices.Types;
using StrikerLink.Unity.Runtime.HapticEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = StrikerLink.Shared.Utils.Logger;

namespace StrikerLink.Unity.Runtime.Core
{
    public class StrikerDevice : MonoBehaviour
    {
        [Range(0, 15)]
        public int deviceIndex = 0;
        public Transform deviceOffsetRoot;
        DeviceBase runtimeDevice;
        public bool debugInputsInEditor = false;

        [Header("Runtime State")]
        public string GUID;
        public EDeviceType type;
        public bool isConnected;
        public bool isReady;
        public float batteryLevel = 0f;
        public DeviceBase.BatteryChargerStatus chargeStatus = DeviceBase.BatteryChargerStatus.Unknown;

        [Header("Input Settings"), Range(0f, 1f)]
        public float triggerPullThreshold = 0.8f;

        #region Button State Tracking

        Dictionary<DeviceButton, bool> previousButtonStates = new Dictionary<DeviceButton, bool>();
        DeviceButton[] cachedButtonEnums;

        bool previousTriggerState = false;

        Dictionary<DeviceSensor, bool> previousSensorStates = new Dictionary<DeviceSensor, bool>();
        DeviceSensor[] cachedSensorEnums;

        Dictionary<DeviceRawSensor, bool> previousRawSensorStates = new Dictionary<DeviceRawSensor, bool>();
        DeviceRawSensor[] cachedRawSensorEnums;

        Vector3 leftTouchpadValue;
        Vector3 rightTouchpadValue;

        #endregion

        private void Awake()
        {
            cachedButtonEnums = System.Enum.GetValues(typeof(DeviceButton)).Cast<DeviceButton>().ToArray();

            cachedSensorEnums = System.Enum.GetValues(typeof(DeviceSensor)).Cast<DeviceSensor>().ToArray();
            cachedRawSensorEnums = System.Enum.GetValues(typeof(DeviceRawSensor)).Cast<DeviceRawSensor>().ToArray();

            if (StrikerController.Controller == null)
            {
                Logger.Warning("No StrikerController present in scene, disabling " + gameObject.name);
            }
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if(StrikerController.IsConnected)
            {

                if (runtimeDevice == null)
                {
                    runtimeDevice = StrikerController.Controller.GetClient().GetDevice(deviceIndex);
                }

                UpdateDevice();
            }
        }

        private void LateUpdate()
        {
            UpdatePreviousButtonStates();
            UpdatePreviousSensorStates();
            UpdatePreviousRawSensorStates();
        }

        void UpdateDevice()
        {
            if (runtimeDevice == null)
            {
                isConnected = false;
                isReady = false;
                return;
            }

            GUID = runtimeDevice.GUID;
            type = runtimeDevice.Type;

            isConnected = runtimeDevice.Connected;
            isReady = runtimeDevice.IsReady;

            batteryLevel = runtimeDevice.BatteryLevel;
            chargeStatus = runtimeDevice.ChargerStatus;

            if (runtimeDevice.DeviceOffset != null && deviceOffsetRoot != null)
            {
                deviceOffsetRoot.localPosition = new Vector3(runtimeDevice.DeviceOffset.PosX, runtimeDevice.DeviceOffset.PosY, runtimeDevice.DeviceOffset.PosZ);
                deviceOffsetRoot.localEulerAngles = new Vector3(runtimeDevice.DeviceOffset.RotX, runtimeDevice.DeviceOffset.RotY, runtimeDevice.DeviceOffset.RotZ);
            } else
            {
                deviceOffsetRoot.localPosition = Vector3.zero;
                deviceOffsetRoot.localRotation = Quaternion.identity;
            }

            if(debugInputsInEditor && Application.isEditor)
                DebugInputs();
        }

        void DebugInputs()
        {
            foreach(DeviceButton btn in cachedButtonEnums)
            {
                if (GetButtonDown(btn))
                    Debug.Log("[DEBUG] " + btn.ToString() + " pressed");
                else if (GetButtonUp(btn))
                    Debug.Log("[DEBUG] " + btn.ToString() + " released");
            }

            foreach(DeviceSensor sensor in cachedSensorEnums)
            {
                if (GetSensorDown(sensor))
                    Debug.Log("[DEBUG] " + sensor.ToString() + " touched");
                else if (GetSensorUp(sensor))
                    Debug.Log("[DEBUG] " + sensor.ToString() + " untouched");
            }

            foreach (DeviceRawSensor sensor in cachedRawSensorEnums)
            {
                if (GetRawSensorDown(sensor))
                    Debug.Log("[DEBUG] " + sensor.ToString() + " touched");
                else if (GetRawSensorUp(sensor))
                    Debug.Log("[DEBUG] " + sensor.ToString() + " untouched");
            }

            if (GetTriggerDown())
            {
                Debug.Log("[DEBUG] Trigger Down");
            } else if(GetTriggerUp())
            {
                Debug.Log("[DEBUG] Trigger Up");
            }
        }

        void UpdatePreviousButtonStates()
        {
            previousTriggerState = GetTrigger();

            foreach (DeviceButton btn in cachedButtonEnums)
            {
                previousButtonStates[btn] = GetButton(btn);
            }
        }

        void UpdatePreviousSensorStates()
        {
            foreach(DeviceSensor sensor in cachedSensorEnums)
            {
                previousSensorStates[sensor] = GetSensor(sensor);
            }
        }

        void UpdatePreviousRawSensorStates()
        {
            foreach (DeviceRawSensor sensor in cachedRawSensorEnums)
            {
                previousRawSensorStates[sensor] = GetRawSensor(sensor);
            }
        }

        public DeviceBase GetRuntimeDevice()
        {
            return runtimeDevice;
        }

        public bool GetTrigger()
        {
            return GetAxis(DeviceAxis.TriggerAxis) >= triggerPullThreshold;
        }

        public bool GetTriggerDown()
        {
            return GetTrigger() && !previousTriggerState;
        }

        public bool GetTriggerUp()
        {
            return !GetTrigger() && previousTriggerState;
        }


        #region Button Fetchers
        public bool GetButton(Shared.Devices.DeviceFeatures.DeviceButton button)
        {
            if (runtimeDevice != null)
                return runtimeDevice.GetButton(button);
            else
                return false;
        }

        public bool GetButtonDown(DeviceButton btn)
        {
            return GetButton(btn) && (!previousButtonStates.ContainsKey(btn) || !previousButtonStates[btn]);
        }

        public bool GetButtonUp(DeviceButton btn)
        {
            return !GetButton(btn) && previousButtonStates.ContainsKey(btn) && previousButtonStates[btn];
        }
        #endregion

        #region Axis Fetchers
        public float GetAxis(DeviceAxis axis)
        {
            if (runtimeDevice != null)
                return runtimeDevice.GetAxis(axis);
            else
                return 0f;
        }

        public Vector3 GetTouchpad(DeviceTouchpad touchpad)
        {
            if (runtimeDevice != null)
            {
                if (touchpad == DeviceTouchpad.TouchpadLeft)
                {
                    runtimeDevice.GetTouchpadAxis(touchpad, out leftTouchpadValue.x, out leftTouchpadValue.y, out leftTouchpadValue.z);
                    return leftTouchpadValue;
                } else if(touchpad == DeviceTouchpad.TouchpadRight)
                {
                    runtimeDevice.GetTouchpadAxis(touchpad, out rightTouchpadValue.x, out rightTouchpadValue.y, out rightTouchpadValue.z);
                    return rightTouchpadValue;
                } else
                {
                    return Vector3.zero;
                }
            } else
            {
                leftTouchpadValue = Vector3.zero;
                rightTouchpadValue = Vector3.zero;

                return Vector3.zero;
            }
        }
        #endregion

        public byte GetMask(DeviceMask mask)
        {
            if (runtimeDevice != null)
                return runtimeDevice.GetMask(mask);
            else
                return byte.MinValue;
        }

        #region Raw Sensor Fetchers
        public bool GetRawSensor(DeviceRawSensor sensor)
        {
            if (runtimeDevice != null)
                return runtimeDevice.GetRawSensor(sensor);
            else
                return false;
        }

        public bool GetRawSensorDown(DeviceRawSensor sensor)
        {
            return GetRawSensor(sensor) && (!previousRawSensorStates.ContainsKey(sensor) || !previousRawSensorStates[sensor]);
        }

        public bool GetRawSensorUp(DeviceRawSensor sensor)
        {
            return !GetRawSensor(sensor) && previousRawSensorStates.ContainsKey(sensor) && previousRawSensorStates[sensor];
        }
        #endregion

        #region Sensor Fetchers
        public bool GetSensor(DeviceSensor sensor)
        {
            if (runtimeDevice != null)
                return runtimeDevice.GetSensor(sensor);
            else
                return false;
        }

        public bool GetSensorDown(DeviceSensor sensor)
        {
            return GetSensor(sensor) && (!previousSensorStates.ContainsKey(sensor) || !previousSensorStates[sensor]);
        }

        public bool GetSensorUp(DeviceSensor sensor)
        {
            return !GetSensor(sensor) && previousSensorStates.ContainsKey(sensor) && previousSensorStates[sensor];
        }
        #endregion

        public bool GetState(DeviceState state)
        {
            if (runtimeDevice != null)
                return runtimeDevice.GetState(state);
            else
                return false;
        }

        public void FireHaptic(HapticEffectAsset effect)
        {
            FireHaptic(effect, 1f, 1f, 1f);
        }

        public void StopHaptics()
        {
            StrikerController.Controller.GetClient().StopAllHaptics((ushort)deviceIndex);
        }

        public void FireHaptic(HapticEffectAsset effect, float intensityModifier, float durationModifier, float frequencyModifier)
        {
            effect.Fire(deviceIndex, intensityModifier, durationModifier, frequencyModifier);
        }

        public void FireHaptic(string libraryId, string effectId, float intensityModifier = 1f, float durationModifier = 1f, float frequencyModifier = 1f)
        {
            StrikerController.Controller.GetClient().FireHaptic((ushort)deviceIndex, StrikerController.Controller.libraryPrefix + libraryId, effectId, intensityModifier, durationModifier, frequencyModifier);
        }
    }
}