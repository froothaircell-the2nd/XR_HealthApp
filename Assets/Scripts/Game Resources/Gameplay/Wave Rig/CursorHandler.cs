using CoreResources.Managers.InputManagement;
using CoreResources.Singleton;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameResources.Gameplay.VRController
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
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private RectTransform _cursorImageRect; // UI element on the world-space canvas
        [SerializeField] private RectTransform _cursorCanvasRect;
        [SerializeField] private Image _cursorImage;

        [Space(5)]

        [Header("Mutable Properties")]
        [SerializeField] private float _canvasDistance = 2.0f; // z distance from camera to canvas
        [SerializeField] private LayerMask _targetLayer;
        [SerializeField] private float _paddingMultiplier = 1.1f; // to slightly pad the cursor around the object
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
            _defaultCursorSize = _cursorImageRect.sizeDelta; // get the default cursor size for resetting later
            ResetCursorDimensions();

            InputManager.OnInstatntiationComplete += OnInputManagerInitialized; // single-use binding to assign events after initialization
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

        #region Public Methods
        public void EnableCursorInteraction(bool enableCursorSelection = true, bool enableCursorModification = false)
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

        public void DisableCursorInteraction()
        {
            _interactionEnabled = false;
            _selectionEnabled = false;

            if (_cursorRefreshCoroutine != null)
            {
                StopCoroutine(_cursorRefreshCoroutine);
                _cursorRefreshCoroutine = null;
            }
        }
        #endregion

        #region Private Methods
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

                var pos = _mainCamera.transform.position;
                var rot = _mainCamera.transform.forward;

                var raycastHitValid = Physics.SphereCast(pos, _spherecastRadius, rot, out RaycastHit hit, _maxRaycastRange, _targetLayer);
                
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
                    ResetCursorDimensions();
                    CursorMode = CursorMode.Interacting;
                    continue;
                }

                ResetCursorDimensions();
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
            
            // Get bounds of the object (assumes Renderer is on root)
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null) return;

            float x = Vector3.Distance(camPos, targetPos); // distance to object
            float y = _canvasDistance; // canvas fixed distance

            float objectHeight = renderer.bounds.size.y; // height in world units

            // Projected height of object at canvas distance using similar triangles
            float projectedWorldHeightAtCanvas = objectHeight * (y / x) * _paddingMultiplier;

            // Convert world height to local canvas units using lossyScale
            float localScaleY = _cursorImageRect.lossyScale.y;
            float sizeDeltaY = projectedWorldHeightAtCanvas / localScaleY;

            // Set sizeDelta (assumes square cursor)
            _cursorImageRect.sizeDelta = new Vector2(sizeDeltaY, sizeDeltaY);

            // Position the cursor at canvas distance along the camera ray
            Vector3 dir = (hitTransform.position - camPos).normalized;
            Vector3 newCursorPos = camPos + dir * y;

            _cursorImageRect.position = newCursorPos;
            _cursorImageRect.rotation = Quaternion.LookRotation(dir);
        }

        private void ResetCursorDimensions()
        {
            _cursorImageRect.sizeDelta = _defaultCursorSize;
            _cursorImageRect.localPosition = new Vector3(0, 0, _canvasDistance);
            _cursorImageRect.localRotation = Quaternion.identity;
        }

        #endregion
        
        #region Event Listeners
        private void OnInputManagerInitialized(InputManager inputManager)
        {
            InputManager.InputActions.XRILeftHandInteraction.UIPress.performed += OnSelectPerformed;
            InputManager.InputActions.XRILeftHandInteraction.UIPress.canceled += OnSelectCancelled;

            if (InputManager.IsInstantiated)
            {
                InputManager.OnInstatntiationComplete -= OnInputManagerInitialized;
            }
        }

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