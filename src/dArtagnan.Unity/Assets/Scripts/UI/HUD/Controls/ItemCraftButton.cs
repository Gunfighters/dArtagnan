using dArtagnan.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.HUD.Controls
{
    public class ItemCraftButton : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField] private Image craftIcon;
        [SerializeField] private Image currentItemIcon;
        [SerializeField] private Image filler;

        private void Awake()
        {
            currentItemIcon.enabled = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            PacketChannel.Raise(new ItemCreatingStateFromClient { IsCreatingItem = false });
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            PacketChannel.Raise(new ItemCreatingStateFromClient { IsCreatingItem = true });
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.localScale = Vector3.one * 1.1f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = Vector3.one;
        }
    }
}