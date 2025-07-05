using Unity.VisualScripting;
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

    private Player LocalPlayer => GameManager.Instance.LocalPlayer;
    private float remainingReloadTime => GameManager.Instance.LocalPlayer.RemainingReloadTime;
    private float totalReloadTime => GameManager.Instance.LocalPlayer.TotalReloadTime;
    private bool shootable => remainingReloadTime <= 0;
    private bool reloading = true;

    private Color orange = new(1.0f, 0.64f, 0.0f);
    public Vector2 Direction => shootingJoystick.Direction;

    private void Update()
    {
        // shootButton.interactable = controlledPlayerCooldown <= 0;
        HandleOutline.color = shootable ? LocalPlayer.TargetPlayer is null ? orange : Color.red : Color.grey;
        shootingJoystick.enabled = shootable;
        cooldownImage.fillAmount = remainingReloadTime <= 0 ? 1 : 1f - remainingReloadTime / totalReloadTime;
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
            GameManager.Instance.ShootTarget();
        }
    }
}
