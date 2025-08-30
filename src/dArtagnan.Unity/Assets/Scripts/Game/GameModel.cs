using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using dArtagnan.Shared;
using Game.Player.Components;
using Game.Player.Data;
using JetBrains.Annotations;
using ObservableCollections;
using R3;
using Unity.VisualScripting;
using UnityEngine;
using Utils;

namespace Game
{
    public class GameModel : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        public readonly ReactiveProperty<int> Round = new();
        public readonly Subject<RoundWinnerBroadcast> RoundWinners = new();
        public readonly Subject<GameWinnerBroadcast> GameWinners = new();
        public readonly ObservableDictionary<int, int> ShowdownStartData = new();
        public readonly ReactiveProperty<int> StateCountdown = new();
        public readonly ObservableList<AugmentId>  AugmentationOptionPool = new();
        public readonly ReactiveProperty<int> HostPlayerId = new();
        public readonly ReactiveProperty<int> LocalPlayerId = new();
        public readonly Subject<PlayerModel> LocalPlayerSet = new();

        public readonly ObservableDictionary<int, PlayerModel> PlayerModels = new();
        public readonly ReactiveProperty<GameState> State = new();
        
        public IEnumerable<PlayerModel> Survivors =>
            PlayerModels
                .Where(pair => pair.Value.Alive.CurrentValue)
                .Select(pair => pair.Value);

        public PlayerModel LocalPlayer => GetPlayerModel(LocalPlayerId.Value);
        public PlayerModel HostPlayer => GetPlayerModel(HostPlayerId.Value);

        private readonly Dictionary<int, PlayerView> _playerViews = new();
        public PlayerView GetPlayerView(int id) => _playerViews[id];

        public readonly ReactiveProperty<PlayerModel> CameraTarget = new();
        public readonly Subject<bool> ConnectionFailure = new();
        public readonly Subject<string> AlertMessage = new();
        public readonly Subject<PlayerModel> NewHost = new();

        public GameModel()
        {
            PacketChannel.On<JoinBroadcast>(OnJoin);
            PacketChannel.On<YouAreFromServer>(OnYouAre);
            PacketChannel.On<NewHostBroadcast>(OnNewHost);
            PacketChannel.On<LeaveBroadcast>(e => RemovePlayer(e.PlayerId));
            PacketChannel.On<WaitingStartFromServer>(e => UpdatePlayerModelsByInfoList(e.PlayersInfo));
            PacketChannel.On<RoundStartFromServer>(e => UpdatePlayerModelsByInfoList(e.PlayersInfo));
            PacketChannel.On<RoundStartFromServer>(_ => State.Value = GameState.Round);
            PacketChannel.On<WaitingStartFromServer>(_ => State.Value = GameState.Waiting);
            PacketChannel.On<ShowdownStartFromServer>(e =>
            {
                State.Value = GameState.Showdown;
                Debug.Log(e);
                StateCountdown.Value = e.Countdown;
                ShowdownStartData.Clear();
                ShowdownStartData.AddRange(e.AccuracyPool);
            });
            LocalPlayerSet.Subscribe(p => CameraTarget.Value = p);
            PacketChannel.On<MovementDataBroadcast>(OnPlayerMovementData);
            PacketChannel.On<PlayerIsTargetingBroadcast>(OnPlayerIsTargeting);
            PacketChannel.On<ShootingBroadcast>(OnPlayerShoot);
            PacketChannel.On<UpdatePlayerAlive>(OnUpdatePlayerAlive);
            PacketChannel.On<UpdateAccuracyStateBroadcast>(OnAccuracyStateBroadcast);
            PacketChannel.On<BalanceUpdateBroadcast>(OnBalanceUpdate);
            PacketChannel.On<UpdateCreatingStateBroadcast>(OnCreatingState);
            PacketChannel.On<UpdateAccuracyBroadcast>(OnAccuracyUpdate);
            PacketChannel.On<ItemAcquiredBroadcast>(OnItemAcquired);
            PacketChannel.On<UpdateRangeBroadcast>(OnRangeUpdate);
            PacketChannel.On<UpdateCurrentEnergyBroadcast>(OnCurrentEnergyUpdate);
            PacketChannel.On<UpdateMaxEnergyBroadcast>(OnMaxEnergyUpdate);
            PacketChannel.On<UpdateMinEnergyToShootBroadcast>(OnMinEnergyUpdate);
            PacketChannel.On<ItemUsedBroadcast>(OnItemUse);
            PacketChannel.On<UpdateSpeedBroadcast>(OnSpeedUpdate);
            PacketChannel.On<UpdateActiveEffectsBroadcast>(OnActiveFx);
            PacketChannel.On<AugmentStartFromServer>(OnAugmentationStart);
        }

        private void UpdatePlayerModelsByInfoList(List<PlayerInformation> list)
        {
            list.ForEach(info =>
            {
                if (PlayerModels.TryGetValue(info.PlayerId, out var model))
                    model.Initialize(info);
                else
                    CreatePlayer(info);
            });
            foreach (var pair in PlayerModels
                         .ToImmutableArray()
                         .Where(pair => !list.Exists(info => info.PlayerId == pair.Key)))
            {
                RemovePlayer(pair.Key);
            }
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }

        [CanBeNull]
        public PlayerModel GetPlayerModel(int id) => PlayerModels.GetValueOrDefault(id, null);

        private void OnJoin(JoinBroadcast e)
        {
            if (e.PlayerInfo.PlayerId != LocalPlayerId.Value)
            {
                CreatePlayer(e.PlayerInfo);
            }
        }

        private void OnYouAre(YouAreFromServer e)
        {
            LocalPlayerId.Value = e.PlayerId;
            NewHost.OnNext(HostPlayer);
        }

        private void OnNewHost(NewHostBroadcast e)
        {
            HostPlayerId.Value = e.HostId;
            NewHost.OnNext(HostPlayer);
        }

        private void CreatePlayer(PlayerInformation info)
        {
            var view = PlayerPoolManager.Instance.Pool.Get();
            var model = new PlayerModel(info, this);
            PlayerPresenter.Initialize(model, view);
            PlayerModels.Add(info.PlayerId, model);
            _playerViews.Add(info.PlayerId, view);
            if (LocalPlayer == model)
                LocalPlayerSet.OnNext(model);
        }

        private void RemovePlayer(int playerId)
        {
            if (PlayerModels.Remove(playerId, out var model))
                if (_playerViews.Remove(playerId, out var view))
                    PlayerPoolManager.Instance.Pool.Release(view);
        }
        
        private void OnAugmentationStart(AugmentStartFromServer e)
        {
            AugmentationOptionPool.Clear();
            AugmentationOptionPool.AddRange(e.AugmentOptions.Select(id => (AugmentId)id));
        }

        private void OnPlayerMovementData(MovementDataBroadcast e)
        {
            var targetPlayer = GetPlayerModel(e.PlayerId)!;
            if (targetPlayer == LocalPlayer) return;
            targetPlayer.PositionFromServer.Value = e.MovementData.Position.ToUnityVec();
            targetPlayer.Direction.Value = e.MovementData.Direction.IntToDirection();
            targetPlayer.Speed.Value = e.MovementData.Speed;
            targetPlayer.LastServerPositionUpdateTimestamp.Value = Time.time;
            targetPlayer.NeedToCorrect = true;
        }

        private void OnPlayerIsTargeting(PlayerIsTargetingBroadcast e)
        {
            var aiming = GetPlayerModel(e.ShooterId)!;
            if (aiming == LocalPlayer) return;
            aiming.Targeting.Value = e.TargetId;
        }

        private void OnPlayerShoot(ShootingBroadcast e)
        {
            var shooter = GetPlayerModel(e.ShooterId)!;
            shooter.Fire.OnNext(new FireInfo { Hit = e.Hit, Target = GetPlayerModel(e.TargetId) });
            var data = shooter.EnergyData.Value;
            data.CurrentEnergy = e.ShooterCurrentEnergy;
            shooter.EnergyData.Value = data;
        }

        private void OnCurrentEnergyUpdate(UpdateCurrentEnergyBroadcast e)
        {
            var updated = GetPlayerModel(e.PlayerId)!;
            var data = updated.EnergyData.Value;
            data.CurrentEnergy = e.CurrentEnergy;
            updated.EnergyData.Value = data;
        }

        private void OnMaxEnergyUpdate(UpdateMaxEnergyBroadcast e)
        {
            var updated = GetPlayerModel(e.PlayerId)!;
            var data = updated.EnergyData.Value;
            data.MaxEnergy = e.MaxEnergy;
            updated.EnergyData.Value = data;
        }

        private void OnMinEnergyUpdate(UpdateMinEnergyToShootBroadcast e)
        {
            var updated = GetPlayerModel(e.PlayerId)!;
            updated.MinEnergyToShoot.Value = e.MinEnergyToShoot;
        }

        private void OnUpdatePlayerAlive(UpdatePlayerAlive e)
        {
            var updated = GetPlayerModel(e.PlayerId)!;
            updated.Alive.Value = e.Alive;
        }

        private void OnCreatingState(UpdateCreatingStateBroadcast e)
        {
            var creating = GetPlayerModel(e.PlayerId)!;
            creating.Crafting.Value = e.IsCreatingItem;
        }

        private void OnItemAcquired(ItemAcquiredBroadcast e)
        {
            var acquiring = GetPlayerModel(e.PlayerId)!;
            acquiring.CurrentItem.Value = (ItemId) e.ItemId;
            acquiring.Crafting.Value = false;
        }

        private void OnItemUse(ItemUsedBroadcast e)
        {
            var losing = GetPlayerModel(e.PlayerId)!;
            losing.CurrentItem.Value = ItemId.None;
        }

        private void OnAccuracyStateBroadcast(UpdateAccuracyStateBroadcast e)
        {
            GetPlayerModel(e.PlayerId)!.AccuracyState.Value = e.AccuracyState;
        }

        private void OnBalanceUpdate(BalanceUpdateBroadcast e)
        {
            var updated = GetPlayerModel(e.PlayerId);
            updated!.Balance.Value = e.Balance;
        }

        private void OnAccuracyUpdate(UpdateAccuracyBroadcast e)
        {
            var updated = GetPlayerModel(e.PlayerId);
            updated!.Accuracy.Value = e.Accuracy;
        }

        private void OnRangeUpdate(UpdateRangeBroadcast e)
        {
            var updated = GetPlayerModel(e.PlayerId)!;
            updated.Range.Value = e.Range;
        }

        private void OnSpeedUpdate(UpdateSpeedBroadcast e)
        {
            var updated = GetPlayerModel(e.PlayerId)!;
            updated.Speed.Value = e.Speed;
        }

        private void OnActiveFx(UpdateActiveEffectsBroadcast e)
        {
            var updated = GetPlayerModel(e.PlayerId)!;
            updated.ActiveFx.Clear();
            updated.ActiveFx.AddRange(e.ActiveEffects);
        }
    }
}