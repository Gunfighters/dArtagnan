using TMPro;
using UnityEngine;

namespace UI.ShowdownLoading
{
    public class ShowdownLoadingFrame : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI accuracyText;
        [SerializeField] private TextMeshProUGUI nicknameText;
        public int ID { get; private set; }

        public void Initialize(int id, int accuracy, string nickname)
        {
            Debug.Log($"Initializing {id} to {accuracy} with {nickname}");
            ID = id;
            accuracyText.text = $"{accuracy}%";
            nicknameText.text = nickname;
        }
    }
}