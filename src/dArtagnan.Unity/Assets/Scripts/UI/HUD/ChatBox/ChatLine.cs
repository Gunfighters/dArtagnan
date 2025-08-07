using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD.ChatBox
{
    public class ChatLine : MonoBehaviour
    {
        public TextMeshProUGUI content;
        private bool _disappearing;
        public Image ContainerImage { get; private set; }

        private void Awake()
        {
            ContainerImage = GetComponent<Image>();
        }

        public void SetLine(string line)
        {
            content.text = line;
        }

        public async UniTask FadeOut(float duration)
        {
            if (_disappearing) return;
            _disappearing = true;
            while (ContainerImage.color.a > 0)
            {
                var imageColor = ContainerImage.color;
                imageColor.a -= Time.deltaTime / duration;
                ContainerImage.color = imageColor;
                var textColor = content.color;
                textColor.a -= Time.deltaTime / duration;
                content.color = textColor;
                await UniTask.WaitForEndOfFrame();
            }

            Destroy(gameObject);
        }
    }
}