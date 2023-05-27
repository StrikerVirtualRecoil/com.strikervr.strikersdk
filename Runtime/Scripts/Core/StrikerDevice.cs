using StrikerLink.Shared.Client;
using StrikerLink.Shared.Connectivity.Packets;
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
    [DefaultExecutionOrder(-50)]
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

        [Header("LED Settings")]
        public bool applyLedsOnConnect;
        public Color connectedLedColor = Color.cyan;

        [System.Serializable]
        public class DeviceEvent : UnityEngine.Events.UnityEvent<StrikerDevice> { }


        public enum GestureStage
        {
            Started,
            Progressed,
            Failed,
            Completed
        }

        [System.Serializable]
        public class GestureEvent : UnityEngine.Events.UnityEvent<GestureStage, float> { }

        [System.Serializable]
        public class CustomGestureEvent : UnityEngine.Events.UnityEvent<string, GestureStage, float> { }

        [System.Serializable]
        public class StrikerDeviceEvents
        {
            public DeviceEvent OnDeviceConnected;
            public DeviceEvent OnDeviceDisconnected;
        }

        /// <summary>
        /// General Device Events
        /// </summary>
        public StrikerDeviceEvents DeviceEvents;

        [System.Serializable]
        public class StrikerTriggerEvents
        {
            public DeviceEvent OnTriggerUp;
            public DeviceEvent OnTriggerDown;
        }

        /// <summary>
        /// Trigger Events
        /// </summary>
        public StrikerTriggerEvents TriggerEvents;

        [System.Serializable]
        public class StrikerButtonEvents
        {
            public DeviceEvent OnSideLeftUp;
            public DeviceEvent OnSideLeftDown;
            public DeviceEvent OnSideRightUp;
            public DeviceEvent OnSideRightDown;

            public DeviceEvent OnTouchpadLeftUp;
            public DeviceEvent OnTouchpadLeftDown;
            public DeviceEvent OnTouchpadRightUp;
            public DeviceEvent OnTouchpadRightDown;

            public DeviceEvent OnFrontTopUp;
            public DeviceEvent OnFrontTopDown;
            public DeviceEvent OnFrontBottomUp;
            public DeviceEvent OnFrontBottomDown;
        }

        /// <summary>
        /// Button Events
        /// </summary>
        public StrikerButtonEvents ButtonEvents;

        [System.Serializable]
        public class StrikerSensorArrayEvents
        {
            public DeviceEvent OnSlideTouched;
            public DeviceEvent OnSlideUntouched;

            public DeviceEvent OnReloadTouched;
            public DeviceEvent OnReloadUntouched;

            public DeviceEvent OnForwardBarGripTouched;
            public DeviceEvent OnForwardBarGripUntouched;

            public DeviceEvent OnUnderTouchpadGripTouched;
            public DeviceEvent OnUnderTouchpadGripUntouched;

            public DeviceEvent OnFrontHandGripTouched;
            public DeviceEvent OnFrontHandGripUntouched;

            public DeviceEvent OnFrontHandGripFaceTouched;
            public DeviceEvent OnFrontHandGripFaceUntouched;

            public DeviceEvent OnTriggerGripTouched;
            public DeviceEvent OnTriggerGripUntouched;
        }

        /// <summary>
        /// Sensor Events
        /// </summary>
        public StrikerSensorArrayEvents SensorEvents;

        object gestureLock = new object();
        Queue<PK_GestureEvent> gestureQueue = new Queue<PK_GestureEvent>();

        [System.Serializable]
        public class StrikerGestureEvents
        {
            public GestureEvent OnForwardBarGripSwipeForward;
            public GestureEvent OnForwardBarGripSwipeBackward;

            public GestureEvent OnUnderTouchpadSwipeForward;
            public GestureEvent OnUnderTouchpadSwipeBackward;
            public GestureEvent OnUnderTouchpadPump;

            public GestureEvent OnSlideSwipeForward;
            public GestureEvent OnSlideSwipeBackward;

            public GestureEvent OnReloadSwipeForward;
            public GestureEvent OnReloadSwipeBackward;

            public CustomGestureEvent OnGesture;
        }

        /// <summary>
        /// Sensor Array Gesture Events (requires StrikerLink 0.6.0+)
        /// </summary>
        public StrikerGestureEvents GestureEvents;

        #region Button State Tracking

        Dictionary<DeviceButton, bool> previousButtonStates = new Dictionary<DeviceButton, bool>();
        DeviceButton[] cachedButtonEnums;

        bool previousTriggerState = false;

        Dictionary<DeviceSensor, bool> previousSensorStates = new Dictionary<DeviceSensor, bool>();
        DeviceSensor[] cachedSensorEnums;

        Dictionary<DeviceRawSensor, bool> previousRawSensorStates = new Dictionary<DeviceRawSensor, bool>();
        DeviceRawSensor[] cachedRawSensorEnums;

        DeviceAxis[] cachedAxisEnums;

        Dictionary<DeviceAxis, float> currentAxisStates = new Dictionary<DeviceAxis, float>();
        Dictionary<DeviceButton, bool> currentButtonStates = new Dictionary<DeviceButton, bool>();
        Dictionary<DeviceSensor, bool> currentSensorStates = new Dictionary<DeviceSensor, bool>();
        Dictionary<DeviceRawSensor, bool> currentRawSensorStates = new Dictionary<DeviceRawSensor, bool>();

        Vector3 leftTouchpadValue;
        Vector3 immediateLeftTouchpadValue;
        Vector3 rightTouchpadValue;
        Vector3 immediateRightTouchpadValue;

        #endregion

        private void Awake()
        {
            cachedButtonEnums = System.Enum.GetValues(typeof(DeviceButton)).Cast<DeviceButton>().ToArray();
            cachedAxisEnums = System.Enum.GetValues(typeof(DeviceAxis)).Cast<DeviceAxis>().ToArray();
            cachedSensorEnums = System.Enum.GetValues(typeof(DeviceSensor)).Cast<DeviceSensor>().ToArray();
            cachedRawSensorEnums = System.Enum.GetValues(typeof(DeviceRawSensor)).Cast<DeviceRawSensor>().ToArray();

            if (StrikerController.Controller == null)
            {
                Logger.Warning("No StrikerController present in scene, disabling " + gameObject.name);
            }
        }

        private void OnEnable()
        {
            if (StrikerController.Controller != null && StrikerController.Controller.GetClient() != null)
                StrikerController.Controller.GetClient().OnGestureEvent += OnGestureEvent;
        }

        private void OnDisable()
        {
            if(StrikerController.Controller != null && StrikerController.Controller.GetClient() != null)
                StrikerController.Controller.GetClient().OnGestureEvent -= OnGestureEvent;
        }

        void OnGestureEvent(ushort index, PK_GestureEvent eventPacket)
        {
            if(index == deviceIndex)
            {
                lock(gestureLock)
                {
                    gestureQueue.Enqueue(eventPacket);
                }
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

                UpdateStartOfFrameStates();

                UpdateDevice();
                HandleEvents();
            }
        }

        // Update Current Input States at the start of the frame
        void UpdateStartOfFrameStates()
        {
            leftTouchpadValue = GetTouchpad(DeviceTouchpad.TouchpadLeft, true);
            rightTouchpadValue = GetTouchpad(DeviceTouchpad.TouchpadRight, true);

            foreach(DeviceAxis axis in cachedAxisEnums)
            {
                currentAxisStates[axis] = GetAxis(axis, true);
            }

            foreach (DeviceButton btn in cachedButtonEnums)
            {
                currentButtonStates[btn] = GetButton(btn, true);
            }

            foreach (DeviceSensor sensor in cachedSensorEnums)
            {
                currentSensorStates[sensor] = GetSensor(sensor, true);
            }

            foreach (DeviceRawSensor sensor in cachedRawSensorEnums)
            {
                currentRawSensorStates[sensor] = GetRawSensor(sensor, true);
            }
        }

        private void LateUpdate()
        {
            UpdatePreviousButtonStates();
            UpdatePreviousSensorStates();
            UpdatePreviousRawSensorStates();
            FireInputEvents();
        }

        void HandleEvents()
        {
            lock(gestureQueue)
            {
                while(gestureQueue.Count > 0) {
                    PK_GestureEvent ev = gestureQueue.Dequeue();

                    switch(ev.GestureId)
                    {
                        case "ForwardBarGripBackward":
                            GestureEvents.OnForwardBarGripSwipeBackward.Invoke(GetGestureStageFromPacket(ev), GetGestureValueFromPacket(ev));
                            break;

                        case "ForwardBarGripForward":
                            GestureEvents.OnForwardBarGripSwipeForward.Invoke(GetGestureStageFromPacket(ev), GetGestureValueFromPacket(ev));
                            break;

                        case "ReloadForward":
                            GestureEvents.OnReloadSwipeForward.Invoke(GetGestureStageFromPacket(ev), GetGestureValueFromPacket(ev));
                            break;

                        case "ReloadBackward":
                            GestureEvents.OnReloadSwipeBackward.Invoke(GetGestureStageFromPacket(ev), GetGestureValueFromPacket(ev));
                            break;

                        case "SlideBackward":
                            GestureEvents.OnSlideSwipeBackward.Invoke(GetGestureStageFromPacket(ev), GetGestureValueFromPacket(ev));
                            break;

                        case "SlideForward":
                            GestureEvents.OnSlideSwipeForward.Invoke(GetGestureStageFromPacket(ev), GetGestureValueFromPacket(ev));
                            break;

                        case "UnderTouchpadPump":
                            GestureEvents.OnUnderTouchpadPump.Invoke(GetGestureStageFromPacket(ev), GetGestureValueFromPacket(ev));
                            break;

                        case "UnderTouchpadBackward":
                            GestureEvents.OnUnderTouchpadSwipeBackward.Invoke(GetGestureStageFromPacket(ev), GetGestureValueFromPacket(ev));
                            break;

                        case "UnderTouchpadForward":
                            GestureEvents.OnUnderTouchpadSwipeForward.Invoke(GetGestureStageFromPacket(ev), GetGestureValueFromPacket(ev));
                            break;
                    }

                    GestureEvents.OnGesture.Invoke(ev.GestureId, GetGestureStageFromPacket(ev), GetGestureValueFromPacket(ev));
                }
            }
        }

        float GetGestureValueFromPacket(PK_GestureEvent packet)
        {
            if (packet.EventType == PK_GestureEvent.GestureEventType.Progress)
                return packet.GestureProgress;
            else if (packet.EventType == PK_GestureEvent.GestureEventType.Complete)
                return packet.GestureSpeed;

            return 0f;
        }

        GestureStage GetGestureStageFromPacket(PK_GestureEvent packet)
        {
            switch (packet.EventType)
            {
                case PK_GestureEvent.GestureEventType.Start:
                    return GestureStage.Started;
                case PK_GestureEvent.GestureEventType.Progress:
                    return GestureStage.Progressed;
                case PK_GestureEvent.GestureEventType.Complete:
                    return GestureStage.Completed;
                case PK_GestureEvent.GestureEventType.Failed:
                    return GestureStage.Failed;
                default:
                    return GestureStage.Started;
            }
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

            System.Action shouldRunEvent = null;

            if (!isConnected && runtimeDevice.Connected)
            {
                shouldRunEvent = OnDeviceConnectedInternal;
            } else if(isConnected && !runtimeDevice.Connected)
            {
                shouldRunEvent = OnDeviceDisconnectedInternal;
            }

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

            if (shouldRunEvent != null)
                shouldRunEvent();
        }

        void OnDeviceConnectedInternal()
        {
            if (applyLedsOnConnect)
            {
                PlaySolidLedEffect(connectedLedColor, 0f, DeviceMavrik.LedGroup.TopLine);
                PlaySolidLedEffect(connectedLedColor, 0f, DeviceMavrik.LedGroup.FrontRings);
            }

            DeviceEvents.OnDeviceConnected.Invoke(this);
        }

        void OnDeviceDisconnectedInternal()
        {
            DeviceEvents.OnDeviceDisconnected.Invoke(this);
        }

        void DebugInputs()
        {
            foreach(DeviceButton btn in cachedButtonEnums)
            {
                if (GetButtonDown(btn))
                    Debug.Log("[DEBUG] " + btn.ToString() + " pressed");
                
                if (GetButtonUp(btn))
                    Debug.Log("[DEBUG] " + btn.ToString() + " released");
            }

            foreach(DeviceSensor sensor in cachedSensorEnums)
            {
                if (GetSensorDown(sensor))
                    Debug.Log("[DEBUG] " + sensor.ToString() + " touched");
                
                if (GetSensorUp(sensor))
                    Debug.Log("[DEBUG] " + sensor.ToString() + " untouched");
            }

            foreach (DeviceRawSensor sensor in cachedRawSensorEnums)
            {
                if (GetRawSensorDown(sensor))
                    Debug.Log("[DEBUG] " + sensor.ToString() + " touched");
                
                if (GetRawSensorUp(sensor))
                    Debug.Log("[DEBUG] " + sensor.ToString() + " untouched");
            }

            if (GetTriggerDown())
            {
                Debug.Log("[DEBUG] Trigger Down");
                //Debug.Log("[DEBUG] Trigger Down at " + GetAxis(DeviceAxis.TriggerAxis));
            }

            if(GetTriggerUp())
            {
                Debug.Log("[DEBUG] Trigger Up");
                //Debug.Log("[DEBUG] Trigger Up at " + GetAxis(DeviceAxis.TriggerAxis));
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

        void FireInputEvents()
        {
            // Trigger
            if (GetTriggerDown() && TriggerEvents.OnTriggerDown != null)
                TriggerEvents.OnTriggerDown.Invoke(this);

            if (GetTriggerUp() && TriggerEvents.OnTriggerUp != null)
                TriggerEvents.OnTriggerUp.Invoke(this);

            // Side Left
            if (GetButtonDown(DeviceButton.SideLeft) && ButtonEvents.OnSideLeftDown != null)
                ButtonEvents.OnSideLeftDown.Invoke(this);

            if (GetButtonUp(DeviceButton.SideLeft) && ButtonEvents.OnSideLeftUp != null)
                ButtonEvents.OnSideLeftUp.Invoke(this);

            // Side Right
            if (GetButtonDown(DeviceButton.SideRight) && ButtonEvents.OnSideRightDown != null)
                ButtonEvents.OnSideRightDown.Invoke(this);

            if (GetButtonUp(DeviceButton.SideRight) && ButtonEvents.OnSideRightUp != null)
                ButtonEvents.OnSideRightUp.Invoke(this);

            // Touchpad Left
            if (GetButtonDown(DeviceButton.TouchpadLeft) && ButtonEvents.OnTouchpadLeftDown != null)
                ButtonEvents.OnTouchpadLeftDown.Invoke(this);

            if (GetButtonUp(DeviceButton.TouchpadLeft) && ButtonEvents.OnTouchpadLeftUp != null)
                ButtonEvents.OnTouchpadLeftUp.Invoke(this);

            // Touchpad Right
            if (GetButtonDown(DeviceButton.TouchpadRight) && ButtonEvents.OnTouchpadRightDown != null)
                ButtonEvents.OnTouchpadRightDown.Invoke(this);

            if (GetButtonUp(DeviceButton.TouchpadRight) && ButtonEvents.OnTouchpadRightUp != null)
                ButtonEvents.OnTouchpadRightUp.Invoke(this);

            // Slide
            if (GetSensorDown(DeviceSensor.SlideTouched) && SensorEvents.OnSlideTouched != null)
                SensorEvents.OnSlideTouched.Invoke(this);

            if (GetSensorUp(DeviceSensor.SlideTouched) && SensorEvents.OnSlideUntouched != null)
                SensorEvents.OnForwardBarGripUntouched.Invoke(this);

            // Reload
            if (GetSensorDown(DeviceSensor.ReloadTouched) && SensorEvents.OnReloadTouched != null)
                SensorEvents.OnReloadTouched.Invoke(this);

            if (GetSensorUp(DeviceSensor.ReloadTouched) && SensorEvents.OnReloadUntouched != null)
                SensorEvents.OnForwardBarGripUntouched.Invoke(this);

            // Forward Bar Grip
            if (GetSensorDown(DeviceSensor.ForwardBarGripTouched) && SensorEvents.OnForwardBarGripTouched != null)
                SensorEvents.OnForwardBarGripTouched.Invoke(this);

            if (GetSensorUp(DeviceSensor.ForwardBarGripTouched) && SensorEvents.OnForwardBarGripUntouched != null)
                SensorEvents.OnForwardBarGripUntouched.Invoke(this);

            // Under Touchpad Grip
            if (GetSensorDown(DeviceSensor.UnderTouchpadGripTouched) && SensorEvents.OnUnderTouchpadGripTouched != null)
                SensorEvents.OnUnderTouchpadGripTouched.Invoke(this);

            if (GetSensorUp(DeviceSensor.UnderTouchpadGripTouched) && SensorEvents.OnUnderTouchpadGripUntouched != null)
                SensorEvents.OnForwardBarGripUntouched.Invoke(this);

            // Front Hand Grip
            if (GetSensorDown(DeviceSensor.FrontHandGripTouched) && SensorEvents.OnFrontHandGripTouched != null)
                SensorEvents.OnFrontHandGripTouched.Invoke(this);

            if (GetSensorUp(DeviceSensor.FrontHandGripTouched) && SensorEvents.OnFrontHandGripUntouched != null)
                SensorEvents.OnForwardBarGripUntouched.Invoke(this);

            // Front Hand Grip (Face)
            if (GetSensorDown(DeviceSensor.FrontHandGripFaceTouched) && SensorEvents.OnFrontHandGripFaceTouched != null)
                SensorEvents.OnFrontHandGripFaceTouched.Invoke(this);

            if (GetSensorUp(DeviceSensor.FrontHandGripFaceTouched) && SensorEvents.OnFrontHandGripFaceUntouched != null)
                SensorEvents.OnForwardBarGripUntouched.Invoke(this);

            // Trigger Grip
            if (GetSensorDown(DeviceSensor.TriggerGripTouched) && SensorEvents.OnTriggerGripTouched != null)
                SensorEvents.OnTriggerGripTouched.Invoke(this);

            if (GetSensorUp(DeviceSensor.TriggerGripTouched) && SensorEvents.OnTriggerGripUntouched != null)
                SensorEvents.OnForwardBarGripUntouched.Invoke(this);
        }

        #region Public API

        /// <summary>
        /// Fetches the RuntimeDevice for this StrikerDevice's devicd index
        /// </summary>
        /// <returns>The active runtime device instance for the component's device index, or null if one does not exist</returns>
        public DeviceBase GetRuntimeDevice()
        {
            return runtimeDevice;
        }

        /// <summary>
        /// Is the trigger currently pulled? Based on the triggerPullThreshold
        /// </summary>
        /// <param name="immediate">Fetch the current value from the device, rather than the value at the start of the current frame</param>
        /// <returns><b>true</b> if pulled beyond or equal to the triggerPullThreshold, <b>false</b> if not</returns>
        public bool GetTrigger(bool immediate = false)
        {
            return GetAxis(DeviceAxis.TriggerAxis, immediate) >= triggerPullThreshold;
        }

        /// <summary>
        /// Has the trigger been pulled this frame?
        /// </summary>
        /// <returns><b>true</b> if pulled this frame, <b>false</b> if not</returns>
        public bool GetTriggerDown()
        {
            return GetTrigger() && !previousTriggerState;
        }

        /// <summary>
        /// Has the trigger been release this frame?
        /// </summary>
        /// <returns><b>true</b> if pulled this frame, <b>false</b> if not</returns>
        public bool GetTriggerUp()
        {
            return !GetTrigger() && previousTriggerState;
        }


        #region Button Fetchers
        /// <summary>
        /// Is the corresponding button currently pressed?
        /// </summary>
        /// <param name="button">The button to check for</param>
        /// <param name="immediate">Fetch the current value from the device, rather than the value at the start of the current frame</param>
        /// <returns><b>true</b> if pressed, <b>false</b> if not</returns>
        public bool GetButton(Shared.Devices.DeviceFeatures.DeviceButton button, bool immediate = false)
        {
            if (!immediate)
            {
                if (currentButtonStates.ContainsKey(button))
                    return currentButtonStates[button];
                else
                    return false;
            }
            else
            {
                if (runtimeDevice != null)
                    return runtimeDevice.GetButton(button);
                else
                    return false;
            }
        }

        /// <summary>
        /// Was the corresponding button pressed this frame?
        /// </summary>
        /// <param name="button">The button to check for</param>
        /// <returns><b>true</b> if the button was pressed this frame, <b>false</b> if not</returns>
        public bool GetButtonDown(DeviceButton button)
        {
            return GetButton(button) && (!previousButtonStates.ContainsKey(button) || !previousButtonStates[button]);
        }

        /// <summary>
        /// Was the corresponding button released this frame?
        /// </summary>
        /// <param name="button">The button to check for</param>
        /// <returns><b>true</b> if the button was released this frame, <b>false</b> if not</returns>
        public bool GetButtonUp(DeviceButton button)
        {
            return !GetButton(button) && previousButtonStates.ContainsKey(button) && previousButtonStates[button];
        }
        #endregion

        #region Axis Fetchers
        /// <summary>
        /// Fetches the current value of the corresponding 1D axis
        /// </summary>
        /// <param name="axis">The enum value of the axis to fetch</param>
        /// <param name="immediate">Fetch the current value from the device, rather than the value at the start of the current frame</param>
        /// <returns><b>0-1</b> value of the axis</returns>
        public float GetAxis(DeviceAxis axis, bool immediate = false)
        {
            if (!immediate)
            {
                if (currentAxisStates.ContainsKey(axis))
                    return currentAxisStates[axis];
                else
                    return 0f;
            }
            else
            {
                if (runtimeDevice != null)
                    return runtimeDevice.GetAxis(axis);
                else
                    return 0f;
            }
        }

        /// <summary>
        /// Fetches the current value of the corresponding touchpad
        /// </summary>
        /// <param name="touchpad">The enum value of the touchpad to fetch</param>
        /// <param name="immediate">Fetch the current value from the device, rather than the value at the start of the current frame</param>
        /// <returns>A Vector3 of the touchpad values, where <b>x</b> and <b>y</b> represent touch position, and <b>z</b> represents how much of the touchpad is being touched (this can be used to extrapolate a rough measure of pressure)</returns>
        public Vector3 GetTouchpad(DeviceTouchpad touchpad, bool immediate = false)
        {
            if (!immediate)
            {
                if (touchpad == DeviceTouchpad.TouchpadLeft)
                    return leftTouchpadValue;
                else if (touchpad == DeviceTouchpad.TouchpadRight)
                    return rightTouchpadValue;
                else
                    return Vector3.zero;
            }
            else
            {
                if (runtimeDevice != null)
                {
                    if (touchpad == DeviceTouchpad.TouchpadLeft)
                    {
                        runtimeDevice.GetTouchpadAxis(touchpad, out leftTouchpadValue.x, out leftTouchpadValue.y, out leftTouchpadValue.z);
                        return leftTouchpadValue;
                    }
                    else if (touchpad == DeviceTouchpad.TouchpadRight)
                    {
                        runtimeDevice.GetTouchpadAxis(touchpad, out rightTouchpadValue.x, out rightTouchpadValue.y, out rightTouchpadValue.z);
                        return rightTouchpadValue;
                    }
                    else
                    {
                        return Vector3.zero;
                    }
                }
                else
                {
                    leftTouchpadValue = Vector3.zero;
                    rightTouchpadValue = Vector3.zero;

                    return Vector3.zero;
                }
            }
        }
        #endregion

        /// <summary>
        /// Fetches the 8-bit value of the corresponding device mask, this is currently only used for the CoverPins on the top of the blaster to identify what is attached to it.
        /// </summary>
        /// <param name="mask">The enum value of the mask to fetch</param>
        /// <returns>A single byte representing the value of each pin under the Mavrik's top cover</returns>
        public byte GetMask(DeviceMask mask)
        {
            if (runtimeDevice != null)
                return runtimeDevice.GetMask(mask);
            else
                return byte.MinValue;
        }

        #region Raw Sensor Fetchers
        /// <summary>
        /// Is the corresponding individual capacitive touch sensor on the device currently reading as touched?
        /// </summary>
        /// <param name="sensor">The sensor to fetch</param>
        /// <param name="immediate">Fetch the current value from the device, rather than the value at the start of the current frame</param>
        /// <returns><b>true</b> if touched, <b>false</b> if not</returns>
        public bool GetRawSensor(DeviceRawSensor sensor, bool immediate = false)
        {
            if (!immediate)
            {
                if (currentRawSensorStates.ContainsKey(sensor))
                    return currentRawSensorStates[sensor];
                else
                    return false;
            }
            else
            {
                if (runtimeDevice != null)
                    return runtimeDevice.GetRawSensor(sensor);
                else
                    return false;
            }
        }

        /// <summary>
        /// Was the corresponding individual capacitive touch sensor on the device touched this frame?
        /// </summary>
        /// <param name="sensor">The sensor to fetch</param>
        /// <returns><b>true</b> if touched, <b>false</b> if not</returns>
        public bool GetRawSensorDown(DeviceRawSensor sensor)
        {
            return GetRawSensor(sensor) && (!previousRawSensorStates.ContainsKey(sensor) || !previousRawSensorStates[sensor]);
        }

        /// <summary>
        /// Was the corresponding individual capacitive touch sensor on the device released this frame?
        /// </summary>
        /// <param name="sensor">The sensor to fetch</param>
        /// <returns><b>true</b> if touched, <b>false</b> if not</returns>
        public bool GetRawSensorUp(DeviceRawSensor sensor)
        {
            return !GetRawSensor(sensor) && previousRawSensorStates.ContainsKey(sensor) && previousRawSensorStates[sensor];
        }
        #endregion

        #region Sensor Fetchers
        /// <summary>
        /// Is any part of the corresponding touch sensor array on the device currently being touched?
        /// </summary>
        /// <param name="sensor">The sensor array to fetch</param>
        /// <param name="immediate">Fetch the current value from the device, rather than the value at the start of the current frame</param>
        /// <returns><b>true</b> if touched, <b>false</b> if not</returns>
        public bool GetSensor(DeviceSensor sensor, bool immediate = false)
        {
            if (!immediate)
            {
                if (currentSensorStates.ContainsKey(sensor))
                    return currentSensorStates[sensor];
                else
                    return false;
            }
            else
            {
                if (runtimeDevice != null)
                    return runtimeDevice.GetSensor(sensor);
                else
                    return false;
            }
        }

        /// <summary>
        /// Was any part of the corresponding touch sensor array on the device touched this frame?
        /// </summary>
        /// <param name="sensor">The sensor array to fetch</param>
        /// <returns><b>true</b> if touched, <b>false</b> if not</returns>
        public bool GetSensorDown(DeviceSensor sensor)
        {
            return GetSensor(sensor) && (!previousSensorStates.ContainsKey(sensor) || !previousSensorStates[sensor]);
        }

        /// <summary>
        /// Was any part of the corresponding touch sensor array on the device released this frame?
        /// </summary>
        /// <param name="sensor">The sensor array to fetch</param>
        /// <returns><b>true</b> if touched, <b>false</b> if not</returns>
        public bool GetSensorUp(DeviceSensor sensor)
        {
            return !GetSensor(sensor) && previousSensorStates.ContainsKey(sensor) && previousSensorStates[sensor];
        }
        #endregion

        /// <summary>
        /// Currently not in use.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool GetState(DeviceState state)
        {
            if (runtimeDevice != null)
                return runtimeDevice.GetState(state);
            else
                return false;
        }

        /// <summary>
        /// Fires the corresponding haptic effect
        /// </summary>
        /// <param name="effect">The haptic effect to fire</param>
        public void FireHaptic(HapticEffectAsset effect)
        {
            FireHaptic(effect, 1f, 1f, 1f);
        }

        /// <summary>
        /// Stops all haptics currently active on the device
        /// </summary>
        public void StopHaptics()
        {
            StrikerController.Controller.GetClient().StopAllHaptics((ushort)deviceIndex);
        }

        /// <summary>
        /// Fires the corresponding haptic effect with modifiers
        /// </summary>
        /// <param name="effect">The haptic effect to fire</param>
        /// <param name="intensityModifier">Modifies the intensity of the haptic, acting as a multiplier</param>
        /// <param name="durationModifier">Modifies the duration of the haptic, acting as a multiplier</param>
        /// <param name="frequencyModifier">Modifies the duration of the haptic, acting as a multiplier</param>
        public void FireHaptic(HapticEffectAsset effect, float intensityModifier, float durationModifier, float frequencyModifier)
        {
            effect.Fire(deviceIndex, intensityModifier, durationModifier, frequencyModifier);
        }

        /// <summary>
        /// Fires the corresponding haptic effect (referenced by library and effect ID) with modifiers. This is a lower-level method, you usually want to use <b>HapticFireAssets</b> to fire your haptics
        /// </summary>
        /// <param name="libraryId">The haptic library that contains the haptic effect to fire</param>
        /// <param name="effectId">The haptic effect to fire</param>
        /// <param name="intensityModifier">Modifies the intensity of the haptic, acting as a multiplier</param>
        /// <param name="durationModifier">Modifies the duration of the haptic, acting as a multiplier</param>
        /// <param name="frequencyModifier">Modifies the duration of the haptic, acting as a multiplier</param>
        public void FireHaptic(string libraryId, string effectId, float intensityModifier = 1f, float durationModifier = 1f, float frequencyModifier = 1f)
        {
            StrikerController.Controller.GetClient().FireHaptic((ushort)deviceIndex, StrikerController.Controller.libraryPrefix + libraryId, effectId, intensityModifier, durationModifier, frequencyModifier);
        }

        /// <summary>
        /// Play a solid LED effect on the corresponding LEDs
        /// </summary>
        /// <param name="primaryColor">The color to set the LEDs to</param>
        /// <param name="duration">How long should this effect last?</param>
        /// <param name="group">The LED array (top line or front ring) to apply this effect to</param>
        /// <param name="mask">The LEDs (1-6 or all) on the LED array to apply this effect to</param>
        public void PlaySolidLedEffect(Color primaryColor, float duration = 0f, DeviceMavrik.LedGroup group = DeviceMavrik.LedGroup.TopLine, DeviceMavrik.LedMask mask = DeviceMavrik.LedMask.All)
        {
            StrikerController.Controller.GetClient().SendBasicLedEffect((ushort)deviceIndex, DeviceBase.LedSequence.Solid, group, mask, new Shared.Haptics.Types.LedCommand.LedColor(primaryColor.r, primaryColor.g, primaryColor.b), new Shared.Haptics.Types.LedCommand.LedColor(), duration, 0);
        }

        /// <summary>
        /// Play a flashing LED effect on the corresponding LEDs
        /// </summary>
        /// <param name="primaryColor">The primary color to set the LEDs to</param>
        /// <param name="secondaryColor">The secondary color to switch between when flashing</param>
        /// <param name="duration">How long should this effect last?</param>
        /// <param name="count">How many times should this effect repeat?</param>
        /// <param name="group">The LED array (top line or front ring) to apply this effect to</param>
        /// <param name="mask">The LEDs (1-6 or all) on the LED array to apply this effect to</param>
        public void PlayFlashLedEffect(Color primaryColor, Color secondaryColor, float duration = 0.5f, int count = 1, DeviceMavrik.LedGroup group = DeviceMavrik.LedGroup.TopLine, DeviceMavrik.LedMask mask = DeviceMavrik.LedMask.All)
        {
            if (count < 1)
                count = 1;

            duration *= 1000f; // Send in ms

            StrikerController.Controller.GetClient().SendBasicLedEffect((ushort)deviceIndex, DeviceBase.LedSequence.Flash, group, mask, new Shared.Haptics.Types.LedCommand.LedColor(primaryColor.r, primaryColor.g, primaryColor.b), new Shared.Haptics.Types.LedCommand.LedColor(secondaryColor.r, secondaryColor.g, secondaryColor.b), duration, count);
        }

        /// <summary>
        /// Play a pulsing LED effect on the corresponding LEDs
        /// </summary>
        /// <param name="primaryColor">The primary color to set the LEDs to</param>
        /// <param name="secondaryColor">The secondary color to switch between when pulsing</param>
        /// <param name="duration">How long should this effect last?</param>
        /// <param name="count">How many times should this effect repeat?</param>
        /// <param name="group">The LED array (top line or front ring) to apply this effect to</param>
        /// <param name="mask">The LEDs (1-6 or all) on the LED array to apply this effect to</param>
        public void PlayPulseLedEffect(Color primaryColor, Color secondaryColor, float duration = 0.5f, int count = 1, DeviceMavrik.LedGroup group = DeviceMavrik.LedGroup.TopLine, DeviceMavrik.LedMask mask = DeviceMavrik.LedMask.All)
        {
            if (count < 1)
                count = 1;

            duration *= 1000f; // Send in ms

            StrikerController.Controller.GetClient().SendBasicLedEffect((ushort)deviceIndex, DeviceBase.LedSequence.Pulse, group, mask, new Shared.Haptics.Types.LedCommand.LedColor(primaryColor.r, primaryColor.g, primaryColor.b), new Shared.Haptics.Types.LedCommand.LedColor(secondaryColor.r, secondaryColor.g, secondaryColor.b), duration, count);
        }

        /// <summary>
        /// Play a "shooting star" LED effect across the corresponding LEDs
        /// </summary>
        /// <param name="primaryColor">The primary color for the "shooting star"</param>
        /// <param name="secondaryColor">The secondary color to use as a background for the effect</param>
        /// <param name="duration">How long should this effect take?</param>
        /// <param name="count">How many times should this effect repeat?</param>
        /// <param name="group">The LED array (top line or front ring) to apply this effect to</param>
        /// <param name="mask">The LEDs (1-6 or all) on the LED array to apply this effect to</param>
        public void PlayForwardLedEffect(Color primaryColor, Color secondaryColor, float duration = 0.5f, int count = 1, DeviceMavrik.LedGroup group = DeviceMavrik.LedGroup.TopLine, DeviceMavrik.LedMask mask = DeviceMavrik.LedMask.All)
        {
            if (count < 1)
                count = 1;

            duration *= 1000f; // Send in ms

            StrikerController.Controller.GetClient().SendBasicLedEffect((ushort)deviceIndex, DeviceBase.LedSequence.DotForward, group, mask, new Shared.Haptics.Types.LedCommand.LedColor(primaryColor.r, primaryColor.g, primaryColor.b), new Shared.Haptics.Types.LedCommand.LedColor(secondaryColor.r, secondaryColor.g, secondaryColor.b), duration, count);
        }
        #endregion
    }
}