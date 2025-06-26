using UnityEngine;
using UnityEngine.UI;

public class ShootButton : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private Button shootButton;
    [SerializeField] private Image cooldownImage;
    [SerializeField] private AudioSource reloadSound;
    private bool HasToPlayReloadSound = true;

    void Start()
    {
        shootButton.onClick.AddListener(OnPressed);
    }
    
    void Update()
    {
        var controlledPlayerCooldown = GameManager.Instance.cooldown[GameManager.Instance.ControlledPlayer.id];
        var controlledPlayerCooldownDuration = GameManager.Instance.ControlledPlayer.cooldownDuration;
        shootButton.interactable = controlledPlayerCooldown <= 0;
        cooldownImage.fillAmount = controlledPlayerCooldown <= 0 ? 1f : 1f - controlledPlayerCooldown / controlledPlayerCooldownDuration;
        if (controlledPlayerCooldown <= 0 && HasToPlayReloadSound)
        {
            HasToPlayReloadSound = false;
            reloadSound.Play();
        }
    }

    void OnPressed()
    {
        GameManager.Instance.ShootTarget();
        HasToPlayReloadSound = true;
    }
}
