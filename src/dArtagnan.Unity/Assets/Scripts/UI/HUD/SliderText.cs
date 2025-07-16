using TMPro;
using UnityEngine;

public class SliderText : MonoBehaviour
{
    public string Label;
    private TextMeshProUGUI text;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    public void UpdateNumber(float value)
    {
        text.SetText($"{Label}: {value}");
    }
}
