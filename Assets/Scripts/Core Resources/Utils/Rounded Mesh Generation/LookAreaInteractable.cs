using DG.Tweening;
using GameResources.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CoreResources.Utils
{
    public class LookAreaInteractable : MonoBehaviour, ICursorInteractable
    {
        [SerializeField, ColorUsage(true, true)]
        private Color _highlightColor;
        [SerializeField]
        private float minDistance = 0.5f, maxDistance = 2f;

        private Vector3 _direction; // Should be normalized
        private Vector3 _center = Vector3.zero;
        private Vector3 _defaultScale = Vector3.zero;
        private float _resizeDuration = 0.65f;
        private Renderer _renderer;
        private Collider _collider;
        private Color _defaultColor;
        private bool _isInteractable = false;

        public bool IsInteractable
        {
            get => _isInteractable;
            private set => _isInteractable = value;
        }

        public void InitializeInteractable()
        {
            _direction = transform.right.normalized;
            _defaultScale = transform.localScale;
            _renderer = GetComponent<Renderer>();
            _defaultColor = _renderer.material.GetColor("_EmissionColor");
            _center = Vector3.zero; // or assign a central point if different
            _collider = GetComponent<Collider>();
        }

        public void EnableInteraction()
        {
            _isInteractable = true;
            transform.DOScale(1f, _resizeDuration);
            _collider.enabled = true;
        }

        public void DisableInteraction()
        {
            _isInteractable = false;
            transform.DOScale(0.15f, _resizeDuration);
            _collider.enabled = false;
        }

        public void SetHighlight(bool status)
        {
            if (_isInteractable)
            {
                Color target = status ? (_highlightColor) : _defaultColor;
                _renderer.material.SetColor("_EmissionColor", target);
            }
        }

        public void UpdatePosition(Vector3 position)
        {
            if (_isInteractable)
            {
                Vector3 local = position - _center;
                float projection = Vector3.Dot(local, _direction);
                float clampedDistance = Mathf.Clamp(projection, minDistance, maxDistance);
                transform.localPosition = _center + _direction * clampedDistance;
            }
        }
    }
}