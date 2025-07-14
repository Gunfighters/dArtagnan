using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AccuracyStateButton : MonoBehaviour
{
    private Button button;
    private TextMeshProUGUI buttonText;
    
    private readonly string[] stateTexts = { "down", "keep", "up" };
    private readonly int[] cyclePattern = { -1, 0, 1, 0 }; // 순환 패턴
    private int cycleIndex = 1; // 시작은 0(유지) 상태에서
    
    private Player LocalPlayer => GameManager.Instance.LocalPlayer;

    private void Awake()
    {
        button = GetComponent<Button>();
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Start()
    {
        button.onClick.AddListener(OnButtonClick);
        UpdateText();
    }

    private void OnButtonClick()
    {
        if (LocalPlayer == null) return;
        
        // 순환 패턴에 따라 다음 상태로 이동
        cycleIndex = (cycleIndex + 1) % cyclePattern.Length;
        int newState = cyclePattern[cycleIndex];
        
        LocalPlayer.SetAccuracyState(newState);
        NetworkManager.Instance.SendAccuracyState(newState);
        
        UpdateText();
    }
    
    private void UpdateText()
    {
        if (buttonText == null) return;
        
        if (LocalPlayer == null) 
        {
            buttonText.text = "waiting";
            return;
        }
        
        buttonText.text = stateTexts[LocalPlayer.AccuracyState + 1]; // -1,0,1 → 0,1,2
    }
} 