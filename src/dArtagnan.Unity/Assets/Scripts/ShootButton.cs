using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShootButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private FixedJoystick shootingJoystick;
    [SerializeField] private Image cooldownImage;
    [SerializeField] private AudioSource reloadSound;
    [SerializeField] private Image JoystickAxis;
    [SerializeField] private Image HandleOutline;
    private Vector2 startPos;
    private bool dragging = false;
    private float controlledPlayerCooldown => GameManager.Instance.cooldown[GameManager.Instance.ControlledPlayer.id];
    private float controlledPlayerCooldownDuration => GameManager.Instance.ControlledPlayer.cooldownDuration;
    private bool shootable => controlledPlayerCooldown <= 0;
    private bool reloading = false;

    private Color orange = new(1.0f, 0.64f, 0.0f);

    void Update()
    {
        // shootButton.interactable = controlledPlayerCooldown <= 0;
        if (shootable)
        {
            HandleOutline.color = Color.red;
            shootingJoystick.enabled = true;
        }
        else
        {
            HandleOutline.color = orange;
        }
        cooldownImage.fillAmount = controlledPlayerCooldown <= 0 ? 0 : 1f - controlledPlayerCooldown / controlledPlayerCooldownDuration;
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
            reloading = true;
            GameManager.Instance.ShootTarget();
        }
    }
}
