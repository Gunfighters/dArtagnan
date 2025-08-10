using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.AlertMessage
{
    public class AlertMessageView : MonoBehaviour
    {
        public TextMeshProUGUI messageText;
        public List<Image> decoImage;

        private void Awake()
        {
            AlertMessagePresenter.Initialize(this);
        }
    }
}