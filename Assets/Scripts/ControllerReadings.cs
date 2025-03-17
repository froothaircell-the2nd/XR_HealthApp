using CoreResources.Managers.InputManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ControllerReadings : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _leftConText;
    [SerializeField]
    private TMP_Text _rightConText;
    [SerializeField]
    private TMP_Text _hmdText;
    [SerializeField]
    private TMP_Text _gazeText;

    private bool _inputSystemInstantiated = false;

    private void Awake()
    {
        StartCoroutine(WaitForInputSystem());
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void LateUpdate()
    {
        if (_inputSystemInstantiated)
        {
            InputManager.Instance.GetComponentPosition(out var leftConPos, ControllerComponent.LeftController);
            InputManager.Instance.GetComponentPosition(out var rightConPos, ControllerComponent.RightController);
            InputManager.Instance.GetComponentPosition(out var hmdPos, ControllerComponent.HMD);
            InputManager.Instance.GetComponentPosition(out var eyePos, ControllerComponent.Eyes);
            InputManager.Instance.GetComponentRotation(out var leftConRot, ControllerComponent.LeftController);
            InputManager.Instance.GetComponentRotation(out var rightConRot, ControllerComponent.RightController);
            InputManager.Instance.GetComponentRotation(out var hmdRot, ControllerComponent.HMD);
            InputManager.Instance.GetComponentRotation(out var eyeRot, ControllerComponent.Eyes);
            InputManager.Instance.GetDeviceTrackingState(out var leftConValidPos, out var leftConValidRot, ControllerComponent.LeftController);
            InputManager.Instance.GetDeviceTrackingState(out var rightConValidPos, out var rightConValidRot, ControllerComponent.RightController);
            InputManager.Instance.GetDeviceTrackingState(out var hmdValidPos, out var hmdValidRot, ControllerComponent.HMD);
            InputManager.Instance.GetDeviceTrackingState(out var eyeValidPos, out var eyeValidRot, ControllerComponent.Eyes);

            _leftConText.text = $"Left Controller\r\nPosition: {leftConPos}\r\nRotation: {leftConRot.eulerAngles}\r\nCurrent State: \n\tValid Position: {leftConValidPos} \n\tValid Rotation: {leftConValidRot}";
            _rightConText.text = $"Right Controller\r\nPosition: {rightConPos}\r\nRotation: {rightConRot.eulerAngles}\r\nCurrent State: \n\tValid Position: {rightConValidPos} \n\tValid Rotation: {rightConValidRot}";
            _hmdText.text = $"HMD\r\nPosition: {hmdPos}\r\nRotation: {hmdRot.eulerAngles}\r\nCurrent State: \n\tValid Position: {hmdValidPos} \n\tValid Rotation: {hmdValidRot}";
            _gazeText.text = $"Eye Gaze\r\nPosition: {eyePos}\r\nRotation: {eyeRot.eulerAngles}\r\nCurrent State: \n\tValid Position: {eyeValidPos} \n\tValid Rotation: {eyeValidRot}";
        }
    }

    private IEnumerator WaitForInputSystem()
    {
        yield return new WaitUntil(() => InputManager.IsInstantiated);

        _inputSystemInstantiated = true;
    }
}
