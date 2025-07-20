using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderText : MonoBehaviour
{
    public string label;
    public TextMeshProUGUI text;

    private void Awake()
    {
        GetComponent<Slider>().onValueChanged.AddListener(UpdateNumber);
    }

    private void UpdateNumber(float value)
    {
        text.SetText($"{label}: {value}");
    }
}
