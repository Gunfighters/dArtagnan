using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Roulette
{
    public class RouletteView : MonoBehaviour
    {
        private RouletteViewModel _viewModel;

        [Header("References")]
        [SerializeField] private RouletteModel model;
        [Header("UI")]
        [SerializeField] private Transform roulette;
        [SerializeField] private Button spinButton;
        [SerializeField] private float padding;
        [SerializeField] private int rotateSpeed;
        [SerializeField] private float spinDuration;
        private List<RouletteSlot> _slots;
        private float SlotAngle => 360f / _slots.Count;
        private float HalfSlotAngle => SlotAngle / 2;
        private float HalfSlotAngleWithPadding => HalfSlotAngle * (1 - padding);
        private void Awake()
        {
            _slots = GetComponentsInChildren<RouletteSlot>().ToList();
            _viewModel = new RouletteViewModel(model);
            _viewModel.NowSpin.Subscribe(Spin);
            spinButton.onClick.AddListener(_viewModel.Spin);
            _viewModel.Pool.Subscribe(SetupSlots);
        }

        private void SetupSlots(List<RouletteItem> items)
        {
            for (var i = 0; i < items.Count; i++)
            {
                _slots[i].Setup(items[i]);
            }
        }

        private void Spin(bool nowSpin)
        {
            if (!nowSpin) return;
            var selected = _viewModel.Pool.CurrentValue.Single(i => i.isTarget);
            var selectedIndex = _viewModel.Pool.CurrentValue.IndexOf(selected);
            var angle = SlotAngle * selectedIndex * -1;
            var leftOffset = (angle - HalfSlotAngleWithPadding) % 360;
            var rightOffset = (angle + HalfSlotAngleWithPadding) % 360;
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
            _viewModel.OnSpinDone();
        }
        
        private static float RouletteProgressFormula(float progress) => 1 - Mathf.Pow(1 - progress, 4);
    }
}