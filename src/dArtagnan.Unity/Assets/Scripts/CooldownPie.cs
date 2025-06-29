using UnityEngine;
using UnityEngine.UI;

public class CooldownPie : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Image cooldownPieImage;
    void Update()
    {
        cooldownPieImage.fillAmount = player.cooldown <= 0 ? 1f : 1f - player.cooldown / player.cooldownDuration;
    }
}
