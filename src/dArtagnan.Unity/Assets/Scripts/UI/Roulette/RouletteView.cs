using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace UI.Roulette
{
    public class RouletteView : MonoBehaviour
    {
        [Header("UI")]
        public Transform roulette;
        public Button spinButton;
        public float padding;
        public int rotateSpeed;
        public float spinDuration;
        
        private List<RouletteSlot> _slots;

        public event Action OnSpinDone;
        private void Awake()
        {
            _slots = GetComponentsInChildren<RouletteSlot>().ToList();
            RoulettePresenter.Initialize(this);
        }

        public void SetupSlots(List<RouletteItem> items)
        {
            for (var i = 0; i < items.Count; i++)
            {
                _slots[i].Setup(items[i]);
            }
        }

        public void Spin(bool nowSpin)
        {
            if (!nowSpin) return;
            var slotAngle = 360f / _slots.Count;
            var halfSlotAngle = slotAngle / 2;
            var halfSlotAngleWithPadding = halfSlotAngle * (1 - padding);
            var selected = _slots.First(i => i.IsTarget);
            var selectedIndex = _slots.IndexOf(selected);
            var angle = slotAngle * selectedIndex * -1;
            var leftOffset = (angle - halfSlotAngleWithPadding) % 360;
            var rightOffset = (angle + halfSlotAngleWithPadding) % 360;
            var randomAngle = Random.Range(leftOffset, rightOffset);
            var targetAngle = randomAngle + 360 * spinDuration * rotateSpeed;
            OnSpin(targetAngle).Forget();
        }

        private async UniTask OnSpin(float end)
        {
            float current = 0;
            float progress = 0;
            while (progress < 1)
            {
                current += Time.deltaTime;
                progress = current / spinDuration;
                var z = Mathf.Lerp(0, end, RouletteProgressFormula(progress));
                roulette.rotation = Quaternion.Euler(0, 0, z);
                await UniTask.WaitForEndOfFrame();
            }
            OnSpinDone?.Invoke();
        }
        
        private static float RouletteProgressFormula(float progress) => 1 - Mathf.Pow(1 - progress, 4);
    }
}