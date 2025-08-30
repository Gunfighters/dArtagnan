using dArtagnan.Shared;
using Game;
using Game.Player.Components;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.HUD.Controls
{
    public class ShootJoystickController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public FixedJoystick shootingJoystick;
        public Image cooldownImage;
        public AudioSource reloadSound;
        public Image JoystickAxis;
        public Image HandleOutline;
        public Image Icon;
        public Image costEnergyIcon;
        public TextMeshProUGUI costText;

        private readonly Color _orange = Color.Lerp(Color.red, Color.yellow, 0.5f);
        private float ReloadRatio => GameService.LocalPlayer.EnergyData.CurrentValue.CurrentEnergy /
                                    GameService.LocalPlayer.MinEnergyToShoot.CurrentValue;
        private bool Shootable => ReloadRatio >= 1;
        private bool _aiming;
        private bool _reloading;

        private void Update()
        {
            if (GameService.LocalPlayer is null) return;
            var newTargetModel = _aiming ? GameService.LocalPlayer.CalculateTarget(shootingJoystick.Direction) : null;
            if ((newTargetModel?.ID.CurrentValue ?? -1) != GameService.LocalPlayer.Targeting.CurrentValue)
            {
                var targetModel = GameService.GetPlayerModel(GameService.LocalPlayer.Targeting.CurrentValue);
                if (targetModel != null) targetModel.Highlighted.Value = false;
                if (newTargetModel != null) newTargetModel.Highlighted.Value = true;
                GameService.LocalPlayer.Targeting.Value = newTargetModel?.ID.CurrentValue ?? -1;
                PacketChannel.Raise(new PlayerIsTargetingFromClient { TargetId = GameService.LocalPlayer.Targeting.CurrentValue });
            }
            
            if (!Shootable)
                HandleOutline.color = Icon.color = costEnergyIcon.color = costText.color = Color.grey;
            else if (GameService.LocalPlayer.CalculateTarget(Vector2.zero) is null)
                HandleOutline.color = Icon.color = costEnergyIcon.color = costText.color = _orange;
            else
            {
                HandleOutline.color = Color.red;
                Icon.color = costEnergyIcon.color = costText.color = Color.white;
            }

            shootingJoystick.enabled = Shootable;
            cooldownImage.fillAmount = ReloadRatio;
            if (_reloading && Shootable)
            {
                reloadSound.Play();
                _reloading = false;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (GameService.LocalPlayer.EnergyData.CurrentValue.CurrentEnergy <
                GameService.LocalPlayer.MinEnergyToShoot.CurrentValue) return;
            JoystickAxis.enabled = true;
            _aiming = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            JoystickAxis.enabled = false;
            _aiming = false;
            if (GameService.LocalPlayer.EnergyData.CurrentValue.CurrentEnergy <
                GameService.LocalPlayer.MinEnergyToShoot.CurrentValue)
                GameService.AlertMessage.OnNext("에너지가 부족합니다");
            else if (GameService.LocalPlayer.Targeting.CurrentValue != -1)
                PacketChannel.Raise(new ShootingFromClient
                    { TargetId = GameService.LocalPlayer.Targeting.CurrentValue });
        }
    }
}