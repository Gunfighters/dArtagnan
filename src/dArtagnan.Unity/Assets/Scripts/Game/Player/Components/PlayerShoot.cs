using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using dArtagnan.Shared;
using Game.Player.Data;
using JetBrains.Annotations;
using R3;
using TMPro;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerShoot : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer targetHighlightCircle;
        [SerializeField] private TextMeshProUGUI hitMissText;
        [SerializeField] private Color hitTextColor;
        [SerializeField] private Color missTextColor;
        [SerializeField] private float hitMissShowingDuration;
        public Transform _rangeCircleTransform;

        private void Awake()
        {
            HighlightAsTarget(false);
        }

        public void Initialize(PlayerModel model)
        {
            model.Range.Subscribe(SetRange);
            model.Fire.Subscribe(ShowHitOrMiss);
        }

        private void SetRange(float newRange)
        {
            _rangeCircleTransform.localScale = new Vector3(newRange * 2, newRange * 2, 1);
        }

        private void HighlightAsTarget(bool show)
        {
            targetHighlightCircle.enabled = show;
        }

        public void ShowHitOrMiss(bool hit)
        {
            hitMissText.text = hit ? "HIT!" : "MISS!";
            hitMissText.color = hit ? hitTextColor : missTextColor;
            hitMissText.enabled = true;
            HideHitMissFx().Forget();
        }

        private async UniTask HideHitMissFx()
        {
            await UniTask.WaitForSeconds(1);
            hitMissText.enabled = false;
        }
    }
}