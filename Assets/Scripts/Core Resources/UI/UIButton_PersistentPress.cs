using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CoreResources.UI
{
    public class UIButton_PersistentPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public Action OnPressDown;
        public Action OnRelease;

        protected bool _isPressed;

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if(!_isPressed)
            {
                OnPressDown?.Invoke();
                _isPressed = true;
            }
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {

        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            if(_isPressed)
            {
                OnRelease?.Invoke();
                _isPressed = false;
            }
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            if(_isPressed)
            {
                OnRelease?.Invoke();
                _isPressed = false;
            }
        }
    }

}
