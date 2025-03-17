using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using CoreResources.Singleton;
using System.Linq;
using UnityEngine.UIElements;
using Wave.Essence;
using Wave.Native;
using System.Text;
using UnityEngine.EventSystems;
using Wave.Essence.Hand;
using Wave.Essence.Hand.StaticGesture;

namespace CoreResources.Managers.InputManagement
{
    public enum ControllerLayout
    {
        VR = 0,
        VR_Continuous = 1,
        VR_NonContinuous = 2,
    }

    public enum ControllerComponent
    {
        HMD = 0,
        Eyes = 1,
        RightController = 2,
        LeftController = 3,
    }

    enum TrackingState
    {
        /// <summary>
        /// Position and rotation are not valid.
        /// </summary>
        None,

        /// <summary>
        /// Position is valid.
        /// See <c>InputTrackingState.Position</c>.
        /// </summary>
        Position = 1 << 0,

        /// <summary>
        /// Rotation is valid.
        /// See <c>InputTrackingState.Rotation</c>.
        /// </summary>
        Rotation = 1 << 1,
    }

    public enum GestureType { Default = 0, Custom = 1 }


    [RequireComponent(typeof(PlayerInput))] // PlayerInput is important for manual control switching
    public class InputManager : MonoSingleton<InputManager>
    {
        #region Private Properties
        private PlayerInput _playerInput;
        private EventSystem _eventSystem;

        #region VR Keyboard
        private IMEManager _imeManagerInstance = null;
        private IMEManager.IMEParameter _currentIMEParameter = null;
        private IMEManager.IMEParameter _currentIMENumericParameter = null;
        private int _currentKeyboardState;
        private const int MODE_FLAG_FIX_MOTION = 0x02;
        private const int MODE_FLAG_AUTO_FIT_CAMERA = 0x04;
        private const int CONTROLLER_BUTTON_DEFAULT = 16; //System:0 , Menu:1 , Grip:2, Touchpad:16, Trigger:17

        private Action<string> _onInputComplete;
        private Action<string> _onInputClick;

        private string _currentInput = "";
        #endregion

        private List<InputDevice> m_inputDevices;

        private readonly Dictionary<string, ControllerLayout> ControllerLayoutDict = new Dictionary<string, ControllerLayout>()
        {
            {"Generic XR Controller", ControllerLayout.VR },
            {"Continuous Move", ControllerLayout.VR_Continuous },
            {"Noncontinuous Move", ControllerLayout.VR_NonContinuous }
        };
        #endregion

        #region Public Properties
        public EventSystem EventSystem => _eventSystem;
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

        public bool IsIMEManagerInitialized => _imeManagerInstance != null && _imeManagerInstance.isInitialized();
        public int CurrentKeyboardState => _currentKeyboardState; // Will be less than 2 for inactive and 3 for active
        public bool IsKeyboardOpen => CurrentKeyboardState > 1;
        #endregion

        #region Overrides
        public override void InitSingleton()
        {
            base.InitSingleton();

            InputActions = new BaseInputActions();
            
            _playerInput = GetComponent<PlayerInput>();
            if (!_playerInput)
                throw new MissingReferenceException($"Player Input component doesn't exist!");

            _eventSystem = GetComponentInChildren<EventSystem>();
            if (!_eventSystem)
                throw new MissingReferenceException($"Event System Component Doesnt exist!");

            InitIMEManager();

            m_inputDevices = new List<InputDevice>(InputSystem.devices);

            InputActions.XRIHead.Enable();
            InputActions.XRILeftHand.Enable();
            InputActions.XRILeftHandInteraction.Enable();
            InputActions.XRILeftHandLocomotion.Enable();
            InputActions.XRIRightHand.Enable();
            InputActions.XRIRightHandInteraction.Enable();
            InputActions.XRIRightHandLocomotion.Enable();
            InputActions.XRIUI.Enable();
            InputActions.TouchscreenGestures.Enable();

            // _playerInput.neverAutoSwitchControlSchemes = true;

            InputSystem.onDeviceChange += OnInputChanged;
            _playerInput.onControlsChanged += OnControlSchemeSwitched;

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
            m_inputDevices?.Clear();
            m_inputDevices = null;
        }

        #region Unity Events
        private void Update()
        {
            //GetDevicePosition(out var right, ControllerComponent.RightController);
            //GetDevicePosition(out var left, ControllerComponent.LeftController);

            //Debug.Log($"Left Position: {left} \nRight Position: {right}");
        }

        private void LateUpdate()
        {
            if (_currentInput.Length > 0 && _onInputClick == null)
            {
                _onInputClick?.Invoke(_currentInput);
                _currentInput = "";
            }
        }
        #endregion
        #endregion

        #region XR
        #region IME Keyboard
        private void InitIMEManager()
        {
            _imeManagerInstance = IMEManager.instance;
            
            int id = 0;
            int type = MODE_FLAG_AUTO_FIT_CAMERA;
            int mode = 4;

            string exist = "";
            int cursor = 0;
            int selectStart = 0;
            int selectEnd = 0;
            double[] pos = new double[] { 0, 0, -1 };
            double[] rot = new double[] { 1.0, 0.0, 0.0, 0.0 };
            int width = 800;
            int height = 800;
            int shadow = 100;
            string locale = "en_US";
            string localeForNumeric = "numeric";
            string title = "IME_Keyboard";
            int extraInt = 0;
            string extraString = "";
            int buttonId = (1 << (int)WVR_InputId.WVR_InputId_Alias1_Thumbstick)
            | (1 << (int)WVR_InputId.WVR_InputId_Alias1_Touchpad)
            | (1 << (int)WVR_InputId.WVR_InputId_Alias1_Trigger)
            | (1 << (int)WVR_InputId.WVR_InputId_Alias1_Bumper);

            _currentIMEParameter = new IMEManager.IMEParameter(id, type, mode, exist, cursor, selectStart, selectEnd, pos,
            rot, width, height, shadow, locale, title, extraInt, extraString, buttonId);

            _currentIMENumericParameter = new IMEManager.IMEParameter(id, type, mode, exist, cursor, selectStart, selectEnd, pos,
                             rot, width, height, shadow, localeForNumeric, title, extraInt, extraString, buttonId);

            _currentKeyboardState = _imeManagerInstance.getKeyboardState();
        }

        public void ToggleKeyboard(bool status, bool isNum = false, bool showEditorPanel = true, string presetText = "", Action<string> inputComplete = null, Action<string> inputClick = null)
        {
            if (status)
            {
                ShowKeyboard(isNum, showEditorPanel, presetText, inputComplete, inputClick);
                return;
            }

            HideKeyboard();
        }

        private void ShowKeyboard(bool isNum = false, bool showEditorPanel = true, string presetText = "", Action<string> inputComplete = null, Action<string> inputClick = null)
        {
            if (IsIMEManagerInitialized && !IsKeyboardOpen)
            {
                var imeParams = isNum ? _currentIMENumericParameter : _currentIMEParameter;
                _onInputComplete += inputComplete;
                _onInputClick += inputClick;
                _currentInput = "";
                imeParams.exist = presetText;
                imeParams.cursor = presetText.Length;
                _imeManagerInstance?.showKeyboard(imeParams, showEditorPanel, InputDoneCallback, InputClickCallback);
                _currentKeyboardState = _imeManagerInstance.getKeyboardState();
            }
        }

        private void HideKeyboard()
        {
            if (IsIMEManagerInitialized && IsKeyboardOpen)
            {
                _imeManagerInstance.hideKeyboard();
                _onInputComplete = null;
                _onInputClick = null;
                _currentKeyboardState = _imeManagerInstance.getKeyboardState();
            }
        }

        private void InputDoneCallback(IMEManager.InputResult results) //Pass this callback as a parameter when calling showKeyboard()
        {
            //Action to do when input is completed
            Debug.Log("Input Done - VR");
            
            _currentInput = results.InputContent;
            _onInputComplete?.Invoke(results.InputContent);
            _currentKeyboardState = _imeManagerInstance.getKeyboardState();
        }

        private void InputClickCallback(IMEManager.InputResult results) //Pass this callback as a parameter when calling showKeyboard()
        {
            //Action to do when input is completed
            Debug.Log("Input Click - VR");

            _currentInput = results.InputContent;
            if (results.KeyCode == IMEManager.InputResult.Key.BACKSPACE)
            {
                // Can use this for active input processing
            }
            if (results.KeyCode == IMEManager.InputResult.Key.ENTER)
            {
                Debug.Log("on clicked enter key");
                HideKeyboard();
            }
            if (results.KeyCode == IMEManager.InputResult.Key.CLOSE)
            {
                Debug.Log("on clicked close key");
                HideKeyboard();
            }
        }
        #endregion

        public void GetComponentPosition(out Vector3 position, ControllerComponent component)
        {
            position = Vector3.zero;

            switch (component)
            {
                case ControllerComponent.HMD:
                    position = InputActions.XRIHead.Position.ReadValue<Vector3>();
                    break;
                case ControllerComponent.Eyes:
                    position = InputActions.XRIHead.EyeGazePosition.ReadValue<Vector3>();
                    break;
                case ControllerComponent.LeftController:
                    position = InputActions.XRILeftHand.Position.ReadValue<Vector3>();
                    break;
                case ControllerComponent.RightController:
                    position = InputActions.XRIRightHand.Position.ReadValue<Vector3>();
                    break;
            }
        }

        public void GetComponentRotation(out Quaternion rotation, ControllerComponent component)
        {
            rotation = Quaternion.identity;

            switch (component)
            {
                case ControllerComponent.HMD:
                    rotation = InputActions.XRIHead.Rotation.ReadValue<Quaternion>();
                    break;
                case ControllerComponent.Eyes:
                    rotation = InputActions.XRIHead.EyeGazeRotation.ReadValue<Quaternion>();
                    break;
                case ControllerComponent.LeftController:
                    rotation = InputActions.XRILeftHand.Rotation.ReadValue<Quaternion>();
                    break;
                case ControllerComponent.RightController:
                    rotation = InputActions.XRIRightHand.Rotation.ReadValue<Quaternion>();
                    break;
            }
        }

        public void GetDeviceTrackingState(out bool validPos, out bool validRot, ControllerComponent component)
        {
            validPos = false;
            validRot = false;

            int res = 0;
            
            switch (component)
            {
                case ControllerComponent.HMD:
                    res = InputActions.XRIHead.TrackingState.ReadValue<int>();
                    break;
                case ControllerComponent.Eyes:
                    res = InputActions.XRIHead.EyeGazeTrackingState.ReadValue<int>();
                    break;
                case ControllerComponent.LeftController:
                    res = InputActions.XRILeftHand.TrackingState.ReadValue<int>();
                    break;
                case ControllerComponent.RightController:
                    res = InputActions.XRIRightHand.TrackingState.ReadValue<int>();
                    break;
            }

            validPos = (res & (int)TrackingState.Position) == (int)TrackingState.Position;
            validRot = (res & (int)TrackingState.Rotation) == (int)TrackingState.Rotation;
        }

        public Tuple<string, string, string> GetHandGestures(GestureType gesture)
        {
            string gestureType = gesture.ToString();
            string leftStr = "";
            string rightStr = "";

            if (gesture == GestureType.Default && HandManager.Instance != null)
            {
                var left = HandManager.Instance.GetHandGesture(true);
                var right = HandManager.Instance.GetHandGesture(false);
                leftStr = left.ToString();
                rightStr = right.ToString();
                // gesture += " Left: " + left + ", Right: " + right;
            }
            if (gesture == GestureType.Custom && CustomGestureProvider.Current != null)
            {
                var left = CustomGestureProvider.Current.GetCustomGesture(true);
                var right = CustomGestureProvider.Current.GetCustomGesture(false);
                leftStr = left;
                rightStr = right;
                // gesture += " Left: " + left + ", Right: " + right;
            }

            return new Tuple<string, string, string>(gestureType, leftStr, rightStr);
        }
        #endregion

        #region Input Utils
        private void OnInputChanged(InputDevice device, InputDeviceChange change)
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                    Debug.Log("New device added: " + device);
                    m_inputDevices.Add(device);
                    CheckForInputs();
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
                    m_inputDevices.Remove(device);
                    if (m_inputDevices.Count == 0)
                        throw new MissingReferenceException("No Input Devices Found! Please connect a new input device");
                    CheckForInputs();
                    break;
            }

        }

        public void OnControlSchemeSwitched(PlayerInput currInput)
        {
            Debug.Log($"Player Input Changed to {currInput.currentControlScheme}");
            
            onControlSchemeChange?.Invoke();     
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
            if (m_inputDevices.Count > 0)
            {
                // Any preemptive processing can happen here
            }
            else
                throw new MissingReferenceException("Input device list is empty! Please connect a device");
        }

        internal void ToggleKeyboard(bool status, bool v, Action<string> onInputComplete)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}