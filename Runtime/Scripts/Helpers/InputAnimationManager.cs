using StrikerLink.Shared.Devices.DeviceFeatures;
using StrikerLink.Shared.Devices.Types;
using StrikerLink.Unity.Runtime.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StrikerLink.Unity.Runtime.Helpers
{
    public class InputAnimationManager : MonoBehaviour
    {
        public Animator animationController;
        public StrikerDevice device;

        [System.Serializable]
        public class SensorAxisBinding
        {
            public DeviceAxis positionBinding = DeviceAxis.None;
            public string positionParameter;

            public DeviceAxis touchAmountBinding = DeviceAxis.None;
            public string touchAmountParameter;
        }

        [System.Serializable]
        public class ButtonBinding
        {
            public DeviceButton stateBinding = DeviceButton.None;
            public string stateParameter;

            public DeviceButton buttonUpBinding = DeviceButton.None;
            public string onUpTrigger;

            public DeviceButton buttonDownBinding = DeviceButton.None;
            public string onDownTrigger;

            public ParticleSystem onPressParticleSystem;
        }

        [System.Serializable]
        public class SensorTouchedBinding
        {
            public DeviceSensor touchedBinding = DeviceSensor.None;
            public string touchedParameter;

            public DeviceSensor sensorUpBinding = DeviceSensor.None;
            public string onUpTrigger;

            public DeviceSensor sensorDownBinding = DeviceSensor.None;
            public string onDownTrigger;
        }

        [System.Serializable]
        public class TouchpadBinding
        {
            public DeviceTouchpad touchpad = DeviceTouchpad.None;
            [Tooltip("Treat 0.5 as the center (0,0) point so that animations can be normalised to 0-1")]
            public bool useHalfAsCenter = true;
            public string xParameter;
            public string yParameter;

            [Tooltip("Unless you only care about the touch position, you generally want to multiply the x/y values by the pressure")]
            public bool multiplyValuesByPressure = true;
            public string pressureParameter;
        }

        [Header("Sensors")]
        public List<SensorAxisBinding> sensorBindings;
        public List<SensorTouchedBinding> touchBindings;

        [Header("Buttons")]
        public List<ButtonBinding> buttonBindings;

        [Header("Touchpads")]
        public List<TouchpadBinding> touchpadBindings;

        private void Update()
        {
            UpdateAnimator();
        }

        void UpdateAnimator()
        {
            if (device == null)
                return;

            foreach(SensorAxisBinding binding in sensorBindings)
            {
                if (binding.touchAmountBinding != DeviceAxis.None && !string.IsNullOrEmpty(binding.touchAmountParameter))
                    animationController.SetFloat(binding.touchAmountParameter, device.GetAxis(binding.touchAmountBinding));

                if (binding.positionBinding != DeviceAxis.None && !string.IsNullOrEmpty(binding.positionParameter))
                    animationController.SetFloat(binding.positionParameter, device.GetAxis(binding.positionBinding));
            }

            foreach(SensorTouchedBinding binding in touchBindings)
            {
                if (binding.touchedBinding != DeviceSensor.None && !string.IsNullOrEmpty(binding.touchedParameter))
                    animationController.SetBool(binding.touchedParameter, device.GetSensor(binding.touchedBinding));

                if (binding.sensorDownBinding != DeviceSensor.None && !string.IsNullOrEmpty(binding.onDownTrigger) && device.GetSensorDown(binding.sensorDownBinding))
                    animationController.SetTrigger(binding.onDownTrigger);

                if (binding.sensorUpBinding != DeviceSensor.None && !string.IsNullOrEmpty(binding.onUpTrigger) && device.GetSensorUp(binding.sensorUpBinding))
                    animationController.SetTrigger(binding.onUpTrigger);
            }

            foreach (ButtonBinding binding in buttonBindings)
            {
                if (binding.stateBinding != DeviceButton.None && !string.IsNullOrEmpty(binding.stateParameter))
                    animationController.SetBool(binding.stateParameter, device.GetButton(binding.stateBinding));

                if (binding.buttonDownBinding != DeviceButton.None && !string.IsNullOrEmpty(binding.onDownTrigger) && device.GetButtonDown(binding.buttonDownBinding))
                    animationController.SetTrigger(binding.onDownTrigger);

                if (binding.buttonUpBinding != DeviceButton.None && !string.IsNullOrEmpty(binding.onUpTrigger) && device.GetButtonUp(binding.buttonUpBinding))
                    animationController.SetTrigger(binding.onUpTrigger);

                if (binding.buttonDownBinding != DeviceButton.None && binding.onPressParticleSystem != null && device.GetButtonDown(binding.buttonDownBinding))
                    binding.onPressParticleSystem.Play();
            }

            foreach (TouchpadBinding binding in touchpadBindings)
            {
                if (binding.touchpad == DeviceTouchpad.None)
                    continue;

                Vector3 val = device.GetTouchpad(binding.touchpad).normalized;

                if(binding.useHalfAsCenter)
                    val = new Vector3((val.x + 1f) * 0.5f, (val.y + 1f) * 0.5f, val.z);

                if (binding.multiplyValuesByPressure)
                    val = new Vector3(val.x * val.z, val.y * val.z, val.z);

                if (!string.IsNullOrEmpty(binding.xParameter))
                    animationController.SetFloat(binding.xParameter, val.x);

                if (!string.IsNullOrEmpty(binding.yParameter))
                    animationController.SetFloat(binding.yParameter, val.y);

                if (!string.IsNullOrEmpty(binding.pressureParameter))
                    animationController.SetFloat(binding.pressureParameter, val.z);
            }
        }
    }
}