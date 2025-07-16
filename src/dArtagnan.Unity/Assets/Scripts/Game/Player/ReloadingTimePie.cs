using UnityEngine;
using UnityEngine.UI;

public class ReloadingTimePie : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Image cooldownPieImage;
    void Update()
    {
        cooldownPieImage.fillAmount = player.RemainingReloadTime <= 0 ? 1f : 1f - player.RemainingReloadTime / player.TotalReloadTime;
    }
}
