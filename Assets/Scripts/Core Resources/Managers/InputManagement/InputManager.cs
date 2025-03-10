using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using CoreResources.Singleton;
using System.Linq;

namespace CoreResources.Managers.InputManagement
{
    public enum ControllerLayout
    {
        VR = 0,
        VR_Continuous = 1,
        VR_NonContinuous = 2,
    }
    
    [RequireComponent(typeof(PlayerInput))] // PlayerInput is important for manual control switching
    public class InputManager : MonoSingleton<InputManager>
    {
        private PlayerInput _playerInput;
        private List<InputDevice> _inputDevices;

        private readonly Dictionary<string, ControllerLayout> ControllerLayoutDict = new Dictionary<string, ControllerLayout>()
        {
            {"Generic XR Controller", ControllerLayout.VR },
            {"Continuous Move", ControllerLayout.VR_Continuous },
            {"Noncontinuous Move", ControllerLayout.VR_NonContinuous }
        };

        public static Action onControlSchemeChange;
        
        public static BaseInputActions InputActions { get; private set; }
        
        public ControllerLayout CurrentControlScheme
        {
            get 
            {
                if (ControllerLayoutDict.TryGetValue(
                        _playerInput.currentControlScheme, 
                        out var controllerLayout))
                    return controllerLayout;
                else
                    throw new MissingReferenceException(
                        $"{_playerInput.currentControlScheme} does not have a corresponding control scheme accounted for!");
            }
        }

        private bool _keyboardListenersAdded = false;

        public static bool ShiftPressed { get; private set; }

        #region Overrides
        public override void InitSingleton()
        {
            base.InitSingleton();

            InputActions = new BaseInputActions();
            
            _playerInput = GetComponent<PlayerInput>();
            if (!_playerInput)
                throw new MissingReferenceException($"Player Input component doesn't exist!");

            _inputDevices = new List<InputDevice>(InputSystem.devices);

            InputActions.XRIHead.Enable();
            InputActions.XRILeftHand.Enable();
            InputActions.XRILeftHandInteraction.Enable();
            InputActions.XRILeftHandLocomotion.Enable();
            InputActions.XRIRightHand.Enable();
            InputActions.XRIRightHandInteraction.Enable();
            InputActions.XRIRightHandLocomotion.Enable();
            InputActions.XRIUI.Enable();
            InputActions.TouchscreenGestures.Enable();


            _playerInput.neverAutoSwitchControlSchemes = true;

            InputSystem.onDeviceChange += OnInputChanged;
            _playerInput.onControlsChanged += OnControlSchemeSwitched;

            PreventDualInputs();

            ShiftPressed = false;
            _keyboardListenersAdded = false;
            CheckForInputs();
        }

        public override void CleanSingleton()
        {
            InputSystem.onDeviceChange -= OnInputChanged;
            _playerInput.onControlsChanged -= OnControlSchemeSwitched;

            onControlSchemeChange = null;

            InputActions?.XRIHead.Disable();
            InputActions?.XRILeftHand.Disable();
            InputActions?.XRILeftHandInteraction.Disable();
            InputActions?.XRILeftHandLocomotion.Disable();
            InputActions?.XRIRightHand.Disable();
            InputActions?.XRIRightHandInteraction.Disable();
            InputActions?.XRIRightHandLocomotion.Disable();
            InputActions?.XRIUI.Disable();
            InputActions?.TouchscreenGestures.Disable();
            
            InputActions = null;
            _playerInput = null;
            _inputDevices?.Clear();
            _inputDevices = null;
        }

        private void Update()
        {
            
        }
        #endregion

        private void OnInputChanged(InputDevice device, InputDeviceChange change)
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                    Debug.Log("New device added: " + device);
                    _inputDevices.Add(device);
                    CheckForInputs();
                   // _playerInput.SwitchCurrentControlScheme(device);
                   
                    break;

                case InputDeviceChange.Disconnected:
                    Debug.Log($"{device} disconnected");
                    CheckForInputs();
                    break;

                case InputDeviceChange.Reconnected:
                    Debug.Log($"{device} reconnected");
                    CheckForInputs();
                    //_playerInput?.SwitchCurrentControlScheme(device);
                    break;

                case InputDeviceChange.Removed:
                    Debug.Log("Device removed: " + device);
                    _inputDevices.Remove(device);
                    if (_inputDevices.Count == 0)
                        throw new MissingReferenceException("No Input Devices Found! Please connect a new input device");
                    CheckForInputs();
                    break;
            }

        }

        public void OnControlSchemeSwitched(PlayerInput currInput)
        {
            Debug.Log($"Player Input Changed to {currInput.currentControlScheme}");
            
            onControlSchemeChange?.Invoke();
            PreventDualInputs();         
        }

        private void PreventDualInputs()
        {
            string currentLayout = ControllerLayoutDict.FirstOrDefault(item => item.Value.Equals(CurrentControlScheme)).Key;
            string bindingGroup = InputActions.controlSchemes.FirstOrDefault(x => x.name == currentLayout).bindingGroup;
            InputActions.bindingMask = InputBinding.MaskByGroup(bindingGroup);
        }

        /// <summary>
        /// Checks for inputs, giving a preference 
        /// to Controllers over Keyboard and Mouse
        /// </summary>
        private void CheckForInputs()
        {
            if (_inputDevices.Count > 0)
            {
                var keyboardIndex = _inputDevices.FindIndex(0, (x) => x is Keyboard);
                //if (keyboardIndex >= 0)
                //{
                //    _playerInput.SwitchCurrentControlScheme(
                //        _inputDevices[keyboardIndex],
                //        _inputDevices.Find(x => x is Mouse));
                //}
                //else
                //{
                //    _playerInput.SwitchCurrentControlScheme(
                //        _inputDevices[0]);
                //}
            }
            else
                throw new MissingReferenceException("Input device list is empty! Please connect a device");
        }
    }
}