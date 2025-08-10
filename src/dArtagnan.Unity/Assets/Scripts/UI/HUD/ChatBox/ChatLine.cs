using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace UI.HUD.ChatBox
{
    public class ChatLine : MonoBehaviour
    {
        private TextMeshProUGUI _content;
        private bool _disappearing;

        private void Awake()
        {
            _content = GetComponent<TextMeshProUGUI>();
        }

        public void SetLine(string line)
        {
            _content.text = line;
        }

        public async UniTask FadeOut(float duration)
        {
            if (_disappearing) return;
            _disappearing = true;
            while (_content.color.a > 0)
            {
                var textColor = _content.color;
                textColor.a -= Time.deltaTime / duration;
                _content.color = textColor;
                await UniTask.WaitForEndOfFrame();
            }

            Destroy(gameObject);
        }
    }
}