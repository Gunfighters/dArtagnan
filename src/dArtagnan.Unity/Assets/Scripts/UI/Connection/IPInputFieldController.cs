using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IPInputFieldController : MonoBehaviour
{
    private void Awake()
    {
        var btn = GetComponentInChildren<Button>();
        var text = GetComponentInChildren<TMP_InputField>();
        btn.onClick.AddListener(() =>
        {
            LocalEventChannel.InvokeOnEndpointSelected(text.text, 7777);
        });
    }
}