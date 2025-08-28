using System.Collections.Generic;
using dArtagnan.Shared;
using Game.Player.Data;
using Game.Player.UI.Fx;
using UnityEngine;

namespace Game.Player.Components
{
    public class PlayerFx : MonoBehaviour
    {
        [SerializeField] private ActiveFx activeFx;

        public void Initialize(PlayerInfoModel model)
        {
            activeFx.Initialize(model.ActiveFx);
        }
    }
}