using CoreResources.Managers.InputManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class KeyboardUI : MonoBehaviour
{
    [SerializeField]
    private bool _showKeyboardState = false;
    [SerializeField]
    private bool _showHandGesture = false;

    private bool _inputSystemInitialized = false;


    [SerializeField]
    private TMP_Text _keyboardStatusText;

    // Start is called before the first frame update
    void Awake()
    {
        StartCoroutine(WaitForInputSystem());
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    // Update is called once per frame
    void Update()
    {
        if (_inputSystemInitialized && _keyboardStatusText != null && _showKeyboardState)
        {
            _keyboardStatusText.SetText($"IME Keyboard Status: {InputManager.Instance.CurrentKeyboardState}");
        }
        else if (_inputSystemInitialized && _keyboardStatusText != null && _showHandGesture)
        {
            var resDef = InputManager.Instance.GetHandGestures(GestureType.Default);
            var resCus = InputManager.Instance.GetHandGestures(GestureType.Custom);
            _keyboardStatusText.SetText($"{resDef.Item1} Gesture: \n\tLeft: {resDef.Item2}\n\tRight: {resDef.Item3}\r\n{resCus.Item1} Gesture: \n\tLeft: {resCus.Item2}\n\tRight: {resCus.Item3}");
        }
    }

    private IEnumerator WaitForInputSystem()
    {
        yield return new WaitUntil(() => InputManager.IsInstantiated);

        _inputSystemInitialized = true;
    }
}
