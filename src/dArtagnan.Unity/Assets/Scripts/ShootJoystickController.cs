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
    public Player LocalPlayer;

    private float RemainingReloadTime => LocalPlayer.RemainingReloadTime;
    private float TotalReloadTime => LocalPlayer.TotalReloadTime;
    private bool Shootable => RemainingReloadTime <= 0;
    private bool _reloading = true;

    private readonly Color orange = new(1.0f, 0.64f, 0.0f);
    public Vector2 Direction => shootingJoystick.Direction;

    private void Update()
    {
        // shootButton.interactable = controlledPlayerCooldown <= 0;
        HandleOutline.color = Shootable ? LocalPlayer.TargetPlayer is null ? orange : Color.red : Color.grey;
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
        if (Shootable)
        {
            JoystickAxis.enabled = true;
        }
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        JoystickAxis.enabled = false;
        if (Shootable)
        {
            GameManager.Instance.ShootTarget();
        }
    }
}
