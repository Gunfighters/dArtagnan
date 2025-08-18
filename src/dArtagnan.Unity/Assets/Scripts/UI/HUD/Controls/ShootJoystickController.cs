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

        private bool _aiming = false;
        private bool _reloading = true;
        private PlayerCore LocalPlayer => GameService.LocalPlayer;

        private float RemainingReloadTime => Mathf.Max(0,
            LocalPlayer.Energy.MinEnergyToShoot - LocalPlayer.Energy.EnergyData.CurrentEnergy);

        private float TotalReloadTime => LocalPlayer.Energy.MinEnergyToShoot;
        private bool Shootable => RemainingReloadTime <= 0;

        private void Update()
        {
            if (!LocalPlayer) return;
            var target = _aiming ? LocalPlayer.Shoot.CalculateTarget(shootingJoystick.Direction) : null;
            if (target != LocalPlayer.Shoot.Target)
            {
                LocalPlayer.Shoot.Target?.Shoot.HighlightAsTarget(false);
                LocalPlayer.Shoot.SetTarget(target);
                LocalPlayer.Shoot.Target?.Shoot.HighlightAsTarget(true);
                PacketChannel.Raise(new PlayerIsTargetingFromClient { TargetId = target?.ID ?? -1 });
            }

            if (!Shootable)
                HandleOutline.color = Icon.color = costEnergyIcon.color = costText.color = Color.grey;
            else if (LocalPlayer.Shoot.CalculateTarget(Vector2.zero) is null)
                HandleOutline.color = Icon.color = costEnergyIcon.color = costText.color = _orange;
            else
            {
                HandleOutline.color = Color.red;
                Icon.color = costEnergyIcon.color = costText.color = Color.white;
            }

            shootingJoystick.enabled = Shootable;
            cooldownImage.fillAmount = RemainingReloadTime <= 0 ? 1 : 1f - RemainingReloadTime / TotalReloadTime;
            if (_reloading && Shootable)
            {
                reloadSound.Play();
                _reloading = false;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Shootable) return;
            JoystickAxis.enabled = true;
            _aiming = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            JoystickAxis.enabled = false;
            _aiming = false;
            if (GameService.LocalPlayer.Energy.EnergyData.CurrentEnergy <
                LocalPlayer.Energy.MinEnergyToShoot)
                LocalEventChannel.InvokeOnAlertMessage("에너지가 부족합니다.", Color.yellow);
            if (Shootable && GameService.LocalPlayer.Shoot.Target)
                PacketChannel.Raise(new ShootingFromClient
                    { TargetId = GameService.LocalPlayer.Shoot.Target.ID });
        }
    }
}