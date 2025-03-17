using CoreResources.Managers.InputManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShowKeyboard : MonoBehaviour
{
    private bool status = false;

    [SerializeField]
    private TMP_InputField _inputField;

    private void OnEnable()
    {
        if (_inputField != null)
        {
            // To ensure that the keyboard is turned on on select
            // and not the internal soft keyboard click activation
            _inputField.keyboardType = (TouchScreenKeyboardType)(-1); 
            _inputField.onSelect.AddListener(DisplayKeyboard);
        }
    }

    private void OnDisable()
    {
        if (_inputField != null)
        {
            _inputField.onSelect.RemoveAllListeners();
        }
    }

    public void ToggleNumKeyboard()
    {
        status = !status;
        InputManager.Instance?.ToggleKeyboard(status, true);
    }

    public void DisplayKeyboard()
    {
        _inputField.Select();
    }

    public void DisplayKeyboard(string currText)
    {
        InputManager.Instance?.ToggleKeyboard(true, false, true, currText, OnInputComplete, OnInputClick);
    }

    private void OnInputComplete(string text)
    {
        _inputField.text = text;

        if (_inputField != null)
        {
            InputManager.Instance.EventSystem.SetSelectedGameObject(null);
        }
    }

    private void OnInputClick(string text)
    {
        _inputField.text = text;
    }
}
