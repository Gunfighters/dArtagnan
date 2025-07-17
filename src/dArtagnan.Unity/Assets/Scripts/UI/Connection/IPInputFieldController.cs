using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IPInputFieldController : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    private void Awake()
    {
        var btn = GetComponentInChildren<Button>();
        var text = GetComponentInChildren<TMP_InputField>();
        btn.onClick.AddListener(() =>
        {
            networkManager.Connect(text.text, 7777);
            CanvasManager.Instance.Hide(GameScreen.Connection);
        });
    }
}