using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD.InRound.StakeBoard
{
    public class StakeBoardView : MonoBehaviour
    {
        private StakeBoardViewModel _viewModel;
        
        [Header("References")]
        [SerializeField] private StakeBoardModel model;

        [Header("UI")]
        [SerializeField] private Image image;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private float beatDuration;

        private void Awake()
        {
            _viewModel = new StakeBoardViewModel(model);
            _viewModel.Amount.Subscribe(a => text.text = $"${a}");
            _viewModel.Amount.Subscribe(_ => Beat(beatDuration).Forget());
            _viewModel.Icon.Subscribe(sprite => image.sprite = sprite);
        }

        private async UniTask Beat(float duration)
        {
            float t = 0;
            while (t < duration)
            {
                transform.localScale = Calc(duration, t) * Vector3.one;
                t += Time.deltaTime;
                await UniTask.WaitForEndOfFrame();
            }
            transform.localScale = Vector3.one;
        }

        private static float Calc(float duration, float t)
        {
            return -0.9f * t * (t - duration) + 1;
        }
    }
}