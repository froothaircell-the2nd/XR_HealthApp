using CoreResources.Managers.InputManagement;
using CoreResources.Singleton;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameResources.Gameplay.WaveRig
{
    public enum CursorRefreshMode
    {
        PerUpdate = 0,
        PerFixedUpdate = 1,
        FixedTime = 2,
        Conditional = 3,
    }

    public enum CursorMode
    {
        Default = 0,
        Interacting = 1,
        Selected = 2,
    }

    public class CursorHandler : DestroyableMonoSingleton<CursorHandler>
    {
        #region Serialized Properties
        [Header("References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private RectTransform cursorUI; // UI element on the world-space canvas
        [SerializeField] private Image _cursorImage;

        [Space(5)]

        [Header("Mutable Properties")]
        [SerializeField] private float canvasDistance = 2.0f; // y distance from camera to canvas
        [SerializeField] private LayerMask targetLayer;
        [SerializeField] private float paddingMultiplier = 1.1f; // to slightly pad the cursor around the object
        [SerializeField] private float _maxRaycastRange = 80f, 
            _spherecastRadius = 1f, 
            _raycastRefreshPeriod = 0.3f;

        [Space(5)]

        [Header("Cursor Sprites")]
        [SerializeField, Tooltip("Sprite for the default pointer, when nothing is in the selection range")] 
        private Sprite _defaultSprite;
        [SerializeField, Tooltip("Sprite for the pointer when an object is in selection range")] 
        private Sprite _interactableSprite;
        [SerializeField, Tooltip("Sprite for the pointer when an object is selected")] 
        private Sprite _selectedSprite;
        #endregion

        #region Private Properties
        private bool _interactionEnabled = false,
            _selectionEnabled = false,
            _selectionSpriteModificationEnabled = false;
        private Vector2 _defaultCursorSize;

        private CursorRefreshMode _refreshMode = CursorRefreshMode.FixedTime;
        private CursorMode _cursorMode = CursorMode.Default;
        private Coroutine _cursorRefreshCoroutine;
        private Transform _cachedTargetTransform;
        private Collider _cachedTargetCollider;
        #endregion

        #region Events
        public Action<Transform, Collider> OnValidInteraction;
        public Action<Transform, Collider> OnValidSelection;
        public Action OnValidCancellation;
        #endregion

        #region Public Properties
        public bool InteractableEnabled => _interactionEnabled;
        public bool SelectionEnabled => _selectionEnabled;

        public CursorMode CursorMode
        {
            get
            {
                return _cursorMode;
            }
            private set
            {
                if (_cursorMode != value)
                {
                    _cursorMode = value;

                    switch (_cursorMode)
                    {
                        case CursorMode.Default:
                        default:
                            _cursorImage.sprite = _defaultSprite;
                            break;
                        case CursorMode.Interacting:
                            _cursorImage.sprite = _interactableSprite;

                            break;
                        case CursorMode.Selected:
                            if (_selectionSpriteModificationEnabled)
                                _cursorImage.sprite = _selectedSprite;
                            break;
                    }
                }
            }
        }
        #endregion

        #region Overrides
        public override void OnInit()
        {
            _defaultCursorSize = cursorUI.sizeDelta; // get the default cursor size for resetting later

            InputManager.InputActions.XRILeftHandInteraction.UIPress.performed += OnSelectPerformed;
            InputManager.InputActions.XRILeftHandInteraction.UIPress.canceled += OnSelectCancelled;
        }

        public override void OnDeInit()
        {
            if (InputManager.IsInstantiated)
            {
                InputManager.InputActions.XRILeftHandInteraction.UIPress.performed -= OnSelectPerformed;
                InputManager.InputActions.XRILeftHandInteraction.UIPress.canceled -= OnSelectCancelled;
            }

            DisableCursorInteraction();
        }
        #endregion

        #region Private Methods
        private void EnableCursorInteraction(bool enableCursorSelection = true, bool enableCursorModification = false)
        {
            _interactionEnabled = true;
            _selectionEnabled = enableCursorSelection;
            _selectionSpriteModificationEnabled = enableCursorSelection;

            if (_cursorRefreshCoroutine != null)
            {
                StopCoroutine(_cursorRefreshCoroutine);
                _cursorRefreshCoroutine = null;
            }

            _cursorRefreshCoroutine = StartCoroutine(CursorModeRefreshCoroutine());
        }

        private void DisableCursorInteraction()
        {
            _interactionEnabled = false;
            _selectionEnabled = false;

            if (_cursorRefreshCoroutine != null)
            {
                StopCoroutine(_cursorRefreshCoroutine);
                _cursorRefreshCoroutine = null;
            }
        }

        private IEnumerator CursorModeRefreshCoroutine()
        {
            while (_interactionEnabled || _selectionEnabled)
            {
                // Execute the corresponding yield instruction
                switch (_refreshMode)
                {
                    case CursorRefreshMode.PerUpdate:
                    default:
                        yield return new WaitForEndOfFrame();
                        break;
                    case CursorRefreshMode.PerFixedUpdate:
                        yield return new WaitForFixedUpdate();
                        break;
                    case CursorRefreshMode.FixedTime:
                        yield return new WaitForSecondsRealtime(_raycastRefreshPeriod);
                        break;
                }

                var pos = mainCamera.transform.position;
                var rot = mainCamera.transform.forward;

                var raycastHitValid = Physics.SphereCast(pos, _spherecastRadius, rot, out RaycastHit hit, _maxRaycastRange, targetLayer);

                // Check if the raycast its an interactable object
                if (raycastHitValid || CursorMode == CursorMode.Selected)
                {
                    // In selection state
                    if (CursorMode == CursorMode.Selected)
                    {
                        if (raycastHitValid)
                        {
                            if (_cachedTargetTransform == null && _cachedTargetCollider == null)
                            {
                                _cachedTargetTransform = hit.transform; // cache for future use (but only if the original cache is clean
                                _cachedTargetCollider = hit.collider;
                            }

                            OnValidSelection?.Invoke(_cachedTargetTransform, _cachedTargetCollider);

                            ResizeCursor(hit, pos, rot);
                            continue;
                        }

                        OnValidSelection?.Invoke(_cachedTargetTransform, _cachedTargetCollider);
                        ResizeCursor(_cachedTargetTransform, pos, rot); // run with the cached transform instead
                        continue;
                    }

                    // In interaction state
                    ResetCursorSize();
                    CursorMode = CursorMode.Interacting;
                    continue;
                }

                ResetCursorSize();
                CursorMode = CursorMode.Default;
            }

            if (_cursorRefreshCoroutine != null)
            {
                StopCoroutine(_cursorRefreshCoroutine);
                _cursorRefreshCoroutine = null;
            }
        }

        private void ResizeCursor(RaycastHit hit, Vector3 camPos, Vector3 camRot)
        {
            ResizeCursor(hit.transform, camPos, camRot);
        }

        private void ResizeCursor(Transform hitTransform, Vector3 camPos, Vector3 camRot)
        {
            if (!_selectionSpriteModificationEnabled)
                return;

            if (hitTransform == null)
            {
                Debug.LogError("hitTransform not found");
                return;
            }

            Transform target = hitTransform;
            var targetPos = target.position;

            float x = Vector3.Distance(camPos, targetPos); // distance to object
            float y = canvasDistance; // canvas fixed distance

            // Get bounds of the object (assumes Renderer is on root)
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null) return;

            float objectHeight = renderer.bounds.size.y; // height in world units

            // Calculate the screen-space projected height at distance x
            Vector3 topPoint = targetPos + Vector3.up * (objectHeight / 2);
            Vector3 bottomPoint = targetPos - Vector3.up * (objectHeight / 2);

            Vector3 topScreen = mainCamera.WorldToScreenPoint(topPoint);
            Vector3 bottomScreen = mainCamera.WorldToScreenPoint(bottomPoint);

            float projectedScreenHeight = Mathf.Abs(topScreen.y - bottomScreen.y);

            // Projected height at canvas distance y using linear perspective scale
            float scaledHeight = projectedScreenHeight * (y / x);

            // Convert screen height to world space on canvas (world space canvas uses local scale)
            float canvasHeight = mainCamera.pixelHeight;
            float worldUnitScale = cursorUI.sizeDelta.y / canvasHeight;
            float adjustedHeight = scaledHeight * worldUnitScale * paddingMultiplier;

            // Set the cursor size (assuming square, can use width separately)
            cursorUI.sizeDelta = new Vector2(adjustedHeight, adjustedHeight);

            // Fix position: interpolate between object and camera center, but clamp to y distance
            Vector3 dir = (targetPos - mainCamera.transform.position).normalized;
            Vector3 desiredCursorPos = mainCamera.transform.position + dir * y;

            cursorUI.position = desiredCursorPos;
            cursorUI.rotation = Quaternion.LookRotation(dir);
        }

        private void ResetCursorSize()
        {
            cursorUI.sizeDelta = _defaultCursorSize;
        }

        #endregion
        
        #region Event Listeners
        private void OnSelectPerformed(InputAction.CallbackContext obj)
        {
            if (CursorMode == CursorMode.Interacting && _selectionEnabled) // can only select when we get interactable objects in range
            {
                CursorMode = CursorMode.Selected;
            }
        }

        private void OnSelectCancelled(InputAction.CallbackContext obj)
        {
            if (CursorMode == CursorMode.Selected && _selectionEnabled)
            {
                CursorMode = CursorMode.Interacting;
                _cachedTargetTransform = null;
                _cachedTargetCollider = null;

                OnValidCancellation?.Invoke();
            }
        }
        #endregion
    }
}