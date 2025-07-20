using dArtagnan.Shared;
using Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShootJoystickController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public FixedJoystick shootingJoystick;
    public Image cooldownImage;
    public AudioSource reloadSound;
    public Image JoystickAxis;
    public Image HandleOutline;
    public SpriteRenderer Icon;
    public Player LocalPlayer => PlayerGeneralManager.LocalPlayer;

    private float RemainingReloadTime => LocalPlayer.RemainingReloadTime;
    private float TotalReloadTime => LocalPlayer.TotalReloadTime;
    private bool Shootable => RemainingReloadTime <= 0;
    private bool _reloading = true;

    private readonly Color orange = new(1.0f, 0.64f, 0.0f);
    public Vector2 Direction => shootingJoystick.Direction;
    
    public bool Moving => Direction != Vector2.zero;

    private bool HasMoved = false;

    private bool _cancelling = false;

    private void Update()
    {
        if (!LocalPlayer) return;
        // shootButton.interactable = controlledPlayerCooldown <= 0;
        HandleOutline.color = Icon.color = Shootable ? LocalPlayer.TargetPlayer is null ? orange : Color.red : Color.grey;
        shootingJoystick.enabled = Shootable;
        cooldownImage.fillAmount = RemainingReloadTime <= 0 ? 1 : 1f - RemainingReloadTime / TotalReloadTime;
        if (_reloading && Shootable)
        {
            reloadSound.Play();
            _reloading = false;
        }
        if (Moving)
        {
            HasMoved = true;
            LocalPlayer.Aim(LocalPlayer.TargetPlayer);
        }

        _cancelling = HasMoved && !Moving; // cancelling
        if (_cancelling)
        {
            LocalPlayer.Aim(null);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (Shootable)
        {
            JoystickAxis.enabled = true;
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log(Direction);
        JoystickAxis.enabled = false;
        if (!Shootable || !PlayerGeneralManager.LocalPlayer.TargetPlayer || HasMoved && !Moving)
            HasMoved = false;
        else
            PacketChannel.Raise(new PlayerShootingFromClient { TargetId = PlayerGeneralManager.LocalPlayer.TargetPlayer.ID });
    }
}
