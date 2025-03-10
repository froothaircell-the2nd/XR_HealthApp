using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using CoreResources.Managers.InputManagement;


namespace CoreResources.UI
{
    public class InputSelectionButton : UIButton_PersistentPress, ISelectHandler, IDeselectHandler
    {
        private bool _inputsSet = false;

        [SerializeField]
        private TMP_InputField _inputField;

        private void OnDisable()
        {
            if (_inputsSet)
            {
                _inputsSet = false;
                if (InputManager.InputActions != null)
                {
                    InputManager.InputActions.XRIUI.Submit.performed -= OnClick;
                }
            }
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (InputManager.InputActions != null && !_inputsSet)
            {
                _inputsSet = true;
                InputManager.InputActions.XRIUI.Submit.performed += OnClick;
            }
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (InputManager.InputActions != null && _inputsSet)
            {
                _inputsSet = false;
                InputManager.InputActions.XRIUI.Submit.performed -= OnClick;
            }
        }

        private void OnClick(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed && InputManager.InputActions != null)
            {
                // GameConstants.SetUISelectionEvent?.Invoke(_inputField.gameObject, false);
                InputManager.InputActions.XRIUI.Submit.performed += OnCancel;
                InputManager.InputActions.XRIUI.Cancel.performed += OnCancel;
            }
        }

        private void OnCancel(InputAction.CallbackContext context)
        {
            if (InputManager.InputActions != null)
            {
                InputManager.InputActions.XRIUI.Submit.performed -= OnCancel;
                InputManager.InputActions.XRIUI.Cancel.performed -= OnCancel;
            }
            // GameConstants.SetUISelectionEvent?.Invoke(gameObject, false);
        }
    }

}
