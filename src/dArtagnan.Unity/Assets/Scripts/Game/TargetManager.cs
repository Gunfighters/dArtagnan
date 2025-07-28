using System.Linq;
using dArtagnan.Shared;
using Game;
using Game.Player;
using Game.Player.Components;
using UnityEngine;

public class TargetManager : MonoBehaviour
{
    private ShootJoystickController _shootingJoystick;
    private static PlayerCore Aiming => PlayerGeneralManager.LocalPlayerCore;

    private void Awake()
    {
        _shootingJoystick = GetComponent<ShootJoystickController>();
    }

    private void Update()
    {
        if (Aiming is null) return;
        if (!Aiming.Health.Alive) return;
        var newTarget = GetAutoTarget();
        var changed = Aiming.Shoot.Target != newTarget;
        if (!changed) return;
        Aiming.Shoot.Target.Shoot?.HighlightAsTarget(false);
        Aiming.Shoot.SetTarget(newTarget);
        Aiming.Shoot.Target.Shoot?.HighlightAsTarget(true);
        PacketChannel.Raise(_shootingJoystick.Moving
            ? new PlayerIsTargetingFromClient { TargetId = newTarget?.ID ?? -1 }
            : new PlayerIsTargetingFromClient { TargetId = -1 });
    }
    
    private PlayerCore GetAutoTarget()
    {
        PlayerCore best = null;
        var targetPool =
            PlayerGeneralManager.Survivors.Where(target =>
                target != Aiming
                && Aiming!.Shoot.CanShoot(target));
        var aim = _shootingJoystick.Direction;
        if (aim == Vector2.zero) // 사거리 내 가장 가까운 적.
        {
            var minDistance = Aiming.Shoot.Range;
            foreach (var target in targetPool)
            {
                if (Vector2.Distance(target.Physics.Position, Aiming.Physics.Position) < minDistance)
                {
                    minDistance = Vector2.Distance(target.Physics.Position, Aiming.Physics.Position);
                    best = target;
                }
            }

            return best;
        }

        var minAngle = float.MaxValue;
        foreach (var target in targetPool)
        {
            var direction = target.Physics.Position - Aiming.Physics.Position;
            if (Vector2.Angle(aim, direction) < minAngle
                && Aiming.Shoot.CanShoot(target)
               )
            {
                minAngle = Vector2.Angle(aim, direction);
                best = target;
            }
        }
        return best;
    }
}