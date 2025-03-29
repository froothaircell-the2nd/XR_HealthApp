using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class ClickCounter : MonoBehaviour
{
    TextMeshProUGUI m_Text = null;
    private void Awake() { m_Text = GetComponent<TextMeshProUGUI>(); }

    uint counter = 0;
    private void Update() { m_Text.text = "Clicked: " + counter; }
    public void AddCounter() { counter++; }
}