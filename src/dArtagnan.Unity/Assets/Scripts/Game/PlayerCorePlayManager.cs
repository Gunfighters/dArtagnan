using dArtagnan.Shared;
using UnityEngine;
using Utils;

namespace Game
{
    /// <summary>
    /// 이동, 조준, 사격, 사망, 증감상태, 잔고를 관리하는 매니저
    /// </summary>
    public class PlayerCorePlayManager : MonoBehaviour
    {
        public void Awake()
        {
            PacketChannel.On<PlayerMovementDataBroadcast>(OnPlayerMovementData);
            PacketChannel.On<PlayerIsTargetingBroadcast>(OnPlayerIsTargeting);
            PacketChannel.On<PlayerShootingBroadcast>(OnPlayerShoot);
            PacketChannel.On<UpdatePlayerAlive>(OnUpdatePlayerAlive);
            PacketChannel.On<PlayerAccuracyStateBroadcast>(OnAccuracyStateBroadcast);
            PacketChannel.On<PlayerBalanceUpdateBroadcast>(OnBalanceUpdate);
        }

        private static void OnPlayerMovementData(PlayerMovementDataBroadcast e)
        {
            var targetPlayer = PlayerGeneralManager.GetPlayer(e.PlayerId);
            if (targetPlayer == PlayerGeneralManager.LocalPlayer) return;
            var direction = e.MovementData.Direction.IntToDirection();
            var serverPosition = e.MovementData.Position.ToUnityVec();
            targetPlayer.UpdateMovementDataForReckoning(direction, serverPosition, e.MovementData.Speed);
        }
        
        private static void OnPlayerIsTargeting(PlayerIsTargetingBroadcast playerIsTargeting)
        {
            var aiming = PlayerGeneralManager.GetPlayer(playerIsTargeting.ShooterId);
            if (aiming == PlayerGeneralManager.LocalPlayer) return;
            aiming.Aim(PlayerGeneralManager.GetPlayer(playerIsTargeting.TargetId));
        }
        
        private static void OnPlayerShoot(PlayerShootingBroadcast e)
        {
            var shooter = PlayerGeneralManager.GetPlayer(e.ShooterId);
            var target = PlayerGeneralManager.GetPlayer(e.TargetId);
            shooter.Fire(target);
            shooter.UpdateRemainingReloadTime(shooter.TotalReloadTime);
            if (GameStateManager.GameState == GameState.Playing)
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
        
        private static void OnAccuracyStateBroadcast(PlayerAccuracyStateBroadcast e)
        {
            PlayerGeneralManager.GetPlayer(e.PlayerId).SetAccuracyState(e.AccuracyState);
        }

        private static void OnBalanceUpdate(PlayerBalanceUpdateBroadcast e)
        {
            PlayerGeneralManager.GetPlayer(e.PlayerId).SetBalance(e.Balance);
        }
    }
}