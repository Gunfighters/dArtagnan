using System.Linq;
using dArtagnan.Shared;
using Game;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

public class TargetManager : MonoBehaviour
{
    [CanBeNull] private static Player LocalPlayer => PlayerGeneralManager.LocalPlayer;
    [CanBeNull] private Player _lastSentTarget;

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

        if (HUDManager.Instance.ShootJoystickVector() == Vector2.zero) return;
        if ((_lastSentTarget is null && newTarget is not null)
            || (newTarget is null && _lastSentTarget is not null)
            || changed)
        {
            LocalPlayer.Aim(newTarget);
            PacketChannel.Raise(new PlayerIsTargetingFromClient { TargetId = newTarget?.ID ?? -1 });
            _lastSentTarget = newTarget;
        }
    }

    
    private Player GetAutoTarget()
    {
        Player best = null;
        var targetPool =
            PlayerGeneralManager.Survivors.Where(target =>
                target != LocalPlayer
                && LocalPlayer!.CanShoot(target));
        if (HUDManager.Instance.ShootJoystickVector() == Vector2.zero) // 사거리 내 가장 가까운 적.
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
            var aim = HUDManager.Instance.ShootJoystickVector();
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