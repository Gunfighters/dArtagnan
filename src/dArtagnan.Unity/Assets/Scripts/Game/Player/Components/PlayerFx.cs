using System.Collections.Generic;
using dArtagnan.Shared;
using Game.Player.UI.Fx;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerFx : MonoBehaviour
    {
        [SerializeField] private ActiveFx activeFx;

        public void Initialize(PlayerInformation info)
        {
            UpdateFx(info.ActiveEffects);
        }

        public void UpdateFx(List<int> activeEffects)
        {
            activeFx.Initialize(activeEffects);
        }
    }
}