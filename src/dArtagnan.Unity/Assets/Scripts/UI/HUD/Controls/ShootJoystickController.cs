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
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        public FixedJoystick shootingJoystick;
        public Image cooldownImage;
        public AudioSource reloadSound;
        public Image JoystickAxis;
        public Image HandleOutline;
        public Image Icon;
        public PlayerCore LocalPlayerCore => PlayerGeneralManager.LocalPlayerCore;

        private float RemainingReloadTime => LocalPlayerCore.Reload.RemainingReloadTime;
        private float TotalReloadTime => LocalPlayerCore.Reload.TotalReloadTime;
        private bool Shootable => RemainingReloadTime <= 0;
        private bool _reloading = true;

        private readonly Color orange = new(1.0f, 0.64f, 0.0f);
        public Vector2 Direction => shootingJoystick.Direction;
    
        public bool Moving => Direction != Vector2.zero;

        private bool _isPointerDown;
        private Vector2 _lastDirection;
        private bool hasAimed;

        private void Update()
        {
            if (!LocalPlayerCore) return;
            // shootButton.interactable = controlledPlayerCooldown <= 0;
            HandleOutline.color =
                Icon.color = Shootable ? LocalPlayerCore.Shoot.Target is null ? orange : Color.red : Color.grey;
            shootingJoystick.enabled = Shootable;
            cooldownImage.fillAmount = RemainingReloadTime <= 0 ? 1 : 1f - RemainingReloadTime / TotalReloadTime;
            if (_reloading && Shootable)
            {
                reloadSound.Play();
                _reloading = false;
            }

            if (_isPointerDown)
            {
                _lastDirection = Direction;
            }

            hasAimed |= Moving;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (Shootable)
            {
                JoystickAxis.enabled = true;
                _isPointerDown = true;
            }
        }
    
        public void OnPointerUp(PointerEventData eventData)
        {
            Debug.Log(_lastDirection);
            JoystickAxis.enabled = false;
            _isPointerDown = false;
            hasAimed = false;
            if (Shootable && PlayerGeneralManager.LocalPlayerCore.Shoot.Target && (_lastDirection != Vector2.zero || _lastDirection == Vector2.zero && !hasAimed))
                PacketChannel.Raise(new PlayerShootingFromClient { TargetId = PlayerGeneralManager.LocalPlayerCore.Shoot.Target.ID });
        }
    }
}
