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

        private Transform _centerTransform;
        private Vector3 _direction; // Should be normalized
        private Vector3 _planarReferenceVector; // Used to find plane
        private Vector3 _defaultLocalPosition = default;
        private Vector3 _defaultScale = default;
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

        public void InitializeInteractable(Transform centerTrans)
        {
            CalibrateInteractable();
            
            _renderer = GetComponent<Renderer>();
            _defaultColor = _renderer.material.GetColor("_EmissionColor");
            _centerTransform = centerTrans; // or assign a central point if different
            _collider = GetComponent<Collider>();
        }

        public void CalibrateInteractable()
        {
            _direction = transform.right.normalized;
            _planarReferenceVector = -1 * transform.forward.normalized;
            _defaultLocalPosition = transform.localPosition;
            _defaultScale = transform.localScale;
        }

        public void ResetInteractable()
        {
            transform.localPosition = _defaultLocalPosition;
            transform.localScale = _defaultScale;
            SetHighlight(false);
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
                Color target = status ? _highlightColor : _defaultColor;
                _renderer.material.SetColor("_EmissionColor", target);
            }
        }

        /// <summary>
        /// Update the position of the given interactable 
        /// by using a reference line to calculate the new 
        /// position. This reference line is drawn from a 
        /// reference position and orientation
        /// </summary>
        /// <param name="referencePosition">
        /// The position that the reference line should 
        /// start or intersect from
        /// </param>
        /// <param name="referenceForward">
        /// The normalized forward vector that determines 
        /// the line direction
        /// </param>
        public void UpdatePositionByReferenceLine(Vector3 referencePosition, Vector3 referenceForward)
        {
            if (!_isInteractable || _centerTransform == null)
                return;

            Vector3 center = _centerTransform.position;

            // Step 1: Create the movement plane from the center using the planar reference
            Plane movementPlane = new Plane(_planarReferenceVector, center);

            // Step 2: Find the intersection point of the reference line with the movement plane
            float enter;
            Ray referenceRay = new Ray(referencePosition, referenceForward);

            if (!movementPlane.Raycast(referenceRay, out enter))
            {
                Debug.LogError("Reference line does not intersect the movement plane.");
                return;
            }

            Vector3 intersectionPoint = referenceRay.GetPoint(enter); // This is the point on the plane

            // Step 3: Project from center to intersection point onto _direction
            Vector3 local = intersectionPoint - center;
            float projection = Vector3.Dot(local, _direction.normalized);

            // Step 4: Clamp the projection to min/max range
            float clampedProjection = Mathf.Clamp(projection, minDistance, maxDistance);

            // Step 5: Set new position
            Vector3 newPosition = center + _direction.normalized * clampedProjection;
            transform.position = newPosition;
        }
    }
}