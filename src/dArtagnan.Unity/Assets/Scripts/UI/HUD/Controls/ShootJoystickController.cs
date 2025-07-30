using dArtagnan.Shared;
using Game;
using Game.Player.Components;
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
        private PlayerCore LocalPlayer => PlayerGeneralManager.LocalPlayerCore;

        private float RemainingReloadTime => LocalPlayer.Reload.RemainingReloadTime;
        private float TotalReloadTime => LocalPlayer.Reload.TotalReloadTime;
        private bool Shootable => RemainingReloadTime <= 0;
        private bool _reloading = true;

        private readonly Color _orange = new(1.0f, 0.64f, 0.0f);

        private bool _aiming = false;

        private void Update()
        {
            if (!LocalPlayer) return;
            var target = _aiming ? LocalPlayer.Shoot.CalculateTarget(shootingJoystick.Direction) : null;
            if (target != LocalPlayer.Shoot.Target)
            {
                LocalPlayer.Shoot.SetTarget(target);
                PacketChannel.Raise(new PlayerIsTargetingFromClient { TargetId = target?.ID ?? -1 });
            }

            Color color;
            if (!Shootable)
                color = Color.grey;
            else if (LocalPlayer.Shoot.CalculateTarget(Vector2.zero) is null)
                color = _orange;
            else
                color = Color.red;
            HandleOutline.color = Icon.color = color;
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
            if (Shootable && PlayerGeneralManager.LocalPlayerCore.Shoot.Target)
                PacketChannel.Raise(new PlayerShootingFromClient { TargetId = PlayerGeneralManager.LocalPlayerCore.Shoot.Target.ID });
        }
    }
}
