using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShootJoystickController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private FixedJoystick shootingJoystick;
    [SerializeField] private Image cooldownImage;
    [SerializeField] private AudioSource reloadSound;
    [SerializeField] private Image JoystickAxis;
    [SerializeField] private Image HandleOutline;

    private RemotePlayerController target => LocalPlayerController.Instance.TargetPlayer;
    private float cooldown => LocalPlayerController.Instance.cooldown;
    private float cooldownDuration => LocalPlayerController.Instance.cooldownDuration;
    private bool shootable => cooldown <= 0;
    private bool reloading = true;

    private Color orange = new(1.0f, 0.64f, 0.0f);
    public Vector2 Direction => shootingJoystick.Direction;

    void Update()
    {
        // shootButton.interactable = controlledPlayerCooldown <= 0;
        HandleOutline.color = shootable ? target is null ? orange : Color.red : Color.grey;
        shootingJoystick.enabled = shootable;
        cooldownImage.fillAmount = cooldown <= 0 ? 1 : 1f - cooldown / cooldownDuration;
        if (reloading && shootable)
        {
            reloadSound.Play();
            reloading = false;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (shootable)
        {
            JoystickAxis.enabled = true;
        }
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        JoystickAxis.enabled = false;
        if (shootable)
        {
            LocalPlayerController.Instance.ShootTarget();
        }
    }
}
