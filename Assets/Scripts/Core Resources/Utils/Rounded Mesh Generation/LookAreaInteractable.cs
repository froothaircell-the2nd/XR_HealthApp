using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CoreResources.Utils
{
    public class LookAreaInteractable : MonoBehaviour
    {
        [SerializeField, ColorUsage(true, true)]
        private Color _highlightColor;

        public Vector3 direction; // Should be normalized
        public float minDistance = 0.5f;
        public float maxDistance = 2f;

        private Vector3 _center = Vector3.zero;
        private Renderer _renderer;
        private Color _defaultColor;
        private bool _isInteractable = false;

        public void InitializeInteractable()
        {
            direction = transform.right.normalized;
            _renderer = GetComponent<Renderer>();
            _defaultColor = _renderer.material.GetColor("_EmissionColor");
            _center = Vector3.zero; // or assign a central point if different
        }

        public void EnableInteraction()
        {
            _isInteractable = true;
        }

        public void DisableInteraction()
        {
            _isInteractable = false;
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
                float projection = Vector3.Dot(local, direction);
                float clampedDistance = Mathf.Clamp(projection, minDistance, maxDistance);
                transform.localPosition = _center + direction * clampedDistance;
            }
        }

        private void Update()
        {
            
        }

        
    }
}