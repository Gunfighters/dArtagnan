using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD
{
    public class StakeBoard : MonoBehaviour, IChannelListener
    {
        [SerializeField] private Image image;
        [SerializeField] private TextMeshProUGUI stakeText;
        [SerializeField] private List<Sprite> imageList;
        [SerializeField] private float beatDuration;
        private int _stake;
        public void Initialize()
        {
            PacketChannel.On<RoundStartFromServer>(_ => stakeText.text = "0");
            PacketChannel.On<BettingDeductionBroadcast>(OnStakeUpdate);
        }

        private void OnStakeUpdate(BettingDeductionBroadcast bettingDeduction)
        {
            _stake = bettingDeduction.TotalPrizeMoney;
            stakeText.text = _stake.ToString();
            image.sprite = _stake switch
            {
                <= 50 => imageList[0],
                <= 100 => imageList[1],
                <= 150 => imageList[2],
                <= 200 => imageList[3],
                <= 250 => imageList[4],
                _ => imageList[5],
            };
            Beat().Forget();
        }

        private async UniTask Beat()
        {
            float t = 0;
            while (t < beatDuration)
            {
                transform.localScale = Calc(t) * Vector3.one;
                t += Time.deltaTime;
                await UniTask.WaitForEndOfFrame();
            }
            transform.localScale = Vector3.one;
        }

        private float Calc(float t)
        {
            return -0.9f * t * (t - beatDuration) + 1;
        }
    }
}