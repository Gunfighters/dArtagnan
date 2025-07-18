using dArtagnan.Shared;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// 이동, 조준, 사격, 사망을 관리하는 매니저
    /// </summary>
    public class PlayerCorePlayManager : MonoBehaviour
    {
        [SerializeField] private StateManager stateManager;
        private void OnEnable()
        {
            PacketChannel.On<PlayerMovementDataBroadcast>(OnPlayerMovementData);
            PacketChannel.On<PlayerIsTargetingBroadcast>(OnPlayerIsTargeting);
            PacketChannel.On<PlayerShootingBroadcast>(OnPlayerShoot);
            PacketChannel.On<UpdatePlayerAlive>(OnUpdatePlayerAlive);
        }

        private static void OnPlayerMovementData(PlayerMovementDataBroadcast e)
        {
            var targetPlayer = PlayerGeneralManager.GetPlayer(e.PlayerId);
            if (targetPlayer == PlayerGeneralManager.LocalPlayer) return;
            var direction = DirectionHelperClient.IntToDirection(e.MovementData.Direction);
            var serverPosition = VecConverter.ToUnityVec(e.MovementData.Position);
            targetPlayer.UpdateMovementDataForReckoning(direction, serverPosition, e.MovementData.Speed);
            targetPlayer.SetRunning(e.Running);
        }
        
        private static void OnPlayerIsTargeting(PlayerIsTargetingBroadcast playerIsTargeting)
        {
            var aiming = PlayerGeneralManager.GetPlayer(playerIsTargeting.ShooterId);
            if (aiming == PlayerGeneralManager.LocalPlayer) return;
            aiming.Aim(PlayerGeneralManager.GetPlayer(playerIsTargeting.TargetId));
        }
        
        private void OnPlayerShoot(PlayerShootingBroadcast e)
        {
            var shooter = PlayerGeneralManager.GetPlayer(e.ShooterId);
            var target = PlayerGeneralManager.GetPlayer(e.TargetId);
            shooter.Fire(target);
            shooter.UpdateRemainingReloadTime(shooter.TotalReloadTime);
            if (stateManager.GameState == GameState.Playing)
            {
                shooter.ShowHitOrMiss(e.Hit);
            }
        }

        private static void OnUpdatePlayerAlive(UpdatePlayerAlive e)
        {
            var updated = PlayerGeneralManager.GetPlayer(e.PlayerId);
            updated.SetAlive(e.Alive);
            if (updated == PlayerGeneralManager.LocalPlayer)
            {
                LocalEventChannel.InvokeOnLocalPlayerAlive(updated.Alive);
            }
        }
    }
}