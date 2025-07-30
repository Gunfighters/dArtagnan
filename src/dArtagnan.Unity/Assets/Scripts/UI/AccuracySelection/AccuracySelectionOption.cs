using Assets.HeroEditor4D.Common.Scripts.Collections;
using dArtagnan.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.AccuracySelection
{
    public class AccuracySelectionOption : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerClickHandler
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private Image image;
        [SerializeField] private IconCollection iconCollection;
        [SerializeField] private Toggle toggle;
        private int _index;

        public void OnPointerClick(PointerEventData eventData)
        {
            PacketChannel.Raise(new WantToSelectAccuracyFromClient { AccuracyIndexDesired = _index });
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.localScale = Vector3.one * 1.1f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = Vector3.one;
        }

        public void Setup(int index, int accuracy)
        {
            _index = index;
            text.text = $"{accuracy}%";
            image.sprite = iconCollection.GunIconByAccuracy(accuracy);
            toggle.isOn = false;
            transform.localScale = Vector3.one;
        }

        public void Toggle(bool isLocalPlayerSelection)
        {
            toggle.graphic.color = isLocalPlayerSelection ? Color.white : Color.grey;
            toggle.isOn = true;
            if (!isLocalPlayerSelection)
                transform.localScale = Vector3.one * 0.9f;
        }
    }
}