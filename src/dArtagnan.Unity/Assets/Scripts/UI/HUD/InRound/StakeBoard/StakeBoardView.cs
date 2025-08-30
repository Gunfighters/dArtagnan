using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD.InRound.StakeBoard
{
    public class StakeBoardView : MonoBehaviour
    {
        [Header("UI")]
        public Image image;
        public TextMeshProUGUI text;
        public float beatDuration;
        public List<StakeBoardIconMeta> iconPool;
        public Transform itemContainer;

        private void Start()
        {
            StakeBoardPresenter.Initialize(new StakeBoardModel(), this);
        }

        public async UniTask Beat()
        {
            float t = 0;
            while (t < beatDuration)
            {
                transform.localScale = Calc(beatDuration, t) * Vector3.one;
                t += Time.deltaTime;
                await UniTask.WaitForEndOfFrame();
            }
            transform.localScale = Vector3.one;
        }

        private static float Calc(float duration, float t)
        {
            return -0.9f * t * (t - duration) + 1;
        }

        public Sprite PickIconByAmount(int amount)
        {
            return iconPool
                .Where(meta => meta.threshold <= amount)
                .Aggregate((a, b) => a.threshold > b.threshold ? a : b)
                .icon;
        }
    }
}