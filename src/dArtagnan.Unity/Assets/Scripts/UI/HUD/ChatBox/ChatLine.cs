using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD.ChatBox
{
    public class ChatLine : MonoBehaviour
    {
        public TextMeshProUGUI _content;
        public Image background;
        private bool _disappearing;

        public void SetLine(string line)
        {
            _content.text = line;
        }

        public void SetSystemMessage(bool system)
        {
            background.color = system ? Color.black : Color.white;
            _content.color = system ? Color.yellow : Color.black;
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
                var bgColor = background.color;
                bgColor.a -= Time.deltaTime / duration;
                background.color = bgColor;
                await UniTask.WaitForEndOfFrame();
            }

            Destroy(gameObject);
        }
    }
}