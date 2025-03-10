using System;
using UnityEngine;
using UnityEngine.EventSystems;
using CoreResources.Managers.InputManagement;

namespace CoreResources.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class DraggableElement : UIButton_PersistentPress
    {
        public Action OnElementSnapped;
        public RectTransform targetRectTransform;

        private Vector3 _pointerOffset;
        private RectTransform _rectTransform;

        private void OnEnable()
        {
            if (!_rectTransform)
            {
                _rectTransform = GetComponent<RectTransform>();
            }
        }

        private void OnDisable()
        {
            OnElementSnapped = null;
        }

        private void Update()
        {
            if (_isPressed)
            {
                DragObject();
            }
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!_isPressed)
            {
                OnPressDown?.Invoke();
                _isPressed = true;
                _pointerOffset = _rectTransform.position - (Vector3) InputManager.InputActions?.XRIUI.Point.ReadValue<Vector2>();
            }
        }

        public void DragObject()
        {
            _rectTransform.position = (Vector3)InputManager.InputActions?.XRIUI.Point.ReadValue<Vector2>() + _pointerOffset;
        }

        public void SnapToLocation(Vector3 location, bool invokeEvent = false)
        {
            _rectTransform.position = location;
            
            if (invokeEvent)
                OnElementSnapped?.Invoke();
        }
    }
}
