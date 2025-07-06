using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
public class TargetManager : MonoBehaviour
{
    [CanBeNull] private Player LocalPlayer => GameManager.Instance.LocalPlayer;
    [CanBeNull] private Player LastSentTarget;

    private void Update()
    {
        if (!LocalPlayerActive()) return;
        var newTarget = GetAutoTarget();
        var changed = LocalPlayer?.TargetPlayer != newTarget;
        if (changed)
        {
            LocalPlayer!.TargetPlayer?.HighlightAsTarget(false);
            LocalPlayer.TargetPlayer = newTarget;
            LocalPlayer.TargetPlayer?.HighlightAsTarget(true);
        }

        if (UIManager.Instance.ShootJoystickVector() == Vector2.zero) return;
        if ((LastSentTarget is null && newTarget is not null)
            || (newTarget is null && LastSentTarget is not null)
            || changed)
        {
            LocalPlayer.Aim(newTarget);
            NetworkManager.Instance.SendPlayerNewTarget(newTarget?.ID ?? -1);
            LastSentTarget = newTarget;
        }
    }

    
    private Player GetAutoTarget()
    {
        Player best = null;
        var targetPool =
            GameManager.Instance.Survivors.Where(target =>
                target != LocalPlayer
                && LocalPlayer!.CanShoot(target));
        if (UIManager.Instance.ShootJoystickVector() == Vector2.zero) // 사거리 내 가장 가까운 적.
        {
            var minDistance = LocalPlayer.Range;
            foreach (var target in targetPool)
            {
                if (Vector2.Distance(target.Position, LocalPlayer.Position) < minDistance)
                {
                    minDistance = Vector2.Distance(target.Position, LocalPlayer.Position);
                    best = target;
                }
            }

            return best;
        }

        var minAngle = float.MaxValue;
        foreach (var target in targetPool)
        {
            var aim = UIManager.Instance.ShootJoystickVector();
            var direction = target.Position - LocalPlayer.Position;
            if (Vector2.Angle(aim, direction) < minAngle
                && LocalPlayer.CanShoot(target)
               )
            {
                minAngle = Vector2.Angle(aim, direction);
                best = target;
            }
        }
        return best;
    }

    private bool LocalPlayerActive()
    {
        return LocalPlayer && LocalPlayer.gameObject.activeInHierarchy && LocalPlayer.Alive;
    }
}