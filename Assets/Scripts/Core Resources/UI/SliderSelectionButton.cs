using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using CoreResources.Managers.InputManagement;

namespace CoreResources.UI
{
    [RequireComponent(typeof(Selectable))]
    public class SliderSelectionButton : UIButton_PersistentPress, ISelectHandler, IDeselectHandler
    {
        [SerializeField]
        private Slider _slider;

        public void OnSelect(BaseEventData eventData)
        {
            if (InputManager.InputActions != null)
                InputManager.InputActions.XRIUI.Submit.performed += OnClick;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (InputManager.InputActions != null)
                InputManager.InputActions.XRIUI.Submit.performed -= OnClick;
        }

        private void OnClick(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed && InputManager.InputActions != null)
            {
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
        }

    }
}
