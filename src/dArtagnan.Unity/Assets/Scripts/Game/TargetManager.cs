using System.Linq;
using dArtagnan.Shared;
using Game;
using UnityEngine;

public class TargetManager : MonoBehaviour
{
    private ShootJoystickController _shootingJoystick;
    private static Player Aiming => PlayerGeneralManager.LocalPlayer;

    private void Awake()
    {
        _shootingJoystick = GetComponent<ShootJoystickController>();
    }

    private void Update()
    {
        if (Aiming is null) return;
        if (!Aiming.Alive) return;
        var newTarget = GetAutoTarget();
        var changed = Aiming.TargetPlayer != newTarget;
        if (!changed) return;
        Aiming.TargetPlayer?.HighlightAsTarget(false);
        Aiming.TargetPlayer = newTarget;
        Aiming.TargetPlayer?.HighlightAsTarget(true);
        Aiming.Aim(newTarget);
        PacketChannel.Raise(new PlayerIsTargetingFromClient { TargetId = newTarget?.ID ?? -1 });
    }
    
    private Player GetAutoTarget()
    {
        Player best = null;
        var targetPool =
            PlayerGeneralManager.Survivors.Where(target =>
                target != Aiming
                && Aiming!.CanShoot(target));
        var aim = _shootingJoystick.Direction;
        if (aim == Vector2.zero) // 사거리 내 가장 가까운 적.
        {
            var minDistance = Aiming.Range;
            foreach (var target in targetPool)
            {
                if (Vector2.Distance(target.Position, Aiming.Position) < minDistance)
                {
                    minDistance = Vector2.Distance(target.Position, Aiming.Position);
                    best = target;
                }
            }

            return best;
        }

        var minAngle = float.MaxValue;
        foreach (var target in targetPool)
        {
            var direction = target.Position - Aiming.Position;
            if (Vector2.Angle(aim, direction) < minAngle
                && Aiming.CanShoot(target)
               )
            {
                minAngle = Vector2.Angle(aim, direction);
                best = target;
            }
        }
        return best;
    }
}