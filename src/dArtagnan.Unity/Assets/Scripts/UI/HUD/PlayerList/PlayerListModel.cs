using dArtagnan.Shared;
using Game;
using Game.Player.Components;
using ObservableCollections;
using R3;
using UnityEngine;

namespace UI.HUD.PlayerList
{
    public static class PlayerListModel
    {
        public static readonly ObservableList<PlayerCore> PlayerList = new();

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            PacketChannel
                .On<UpdatePlayerAlive>(e =>
                {
                    // Alive가 Reactive하지 않으므로 대신 리스트에서 뺐다가 도로 넣는 방식으로 반응을 시킨다.
                    // TODO: PlayerHealth.Alive를 Subscribe하기.
                    PlayerList.Remove(PlayerGeneralManager.GetPlayer(e.PlayerId));
                    PlayerList.Add(PlayerGeneralManager.GetPlayer(e.PlayerId));
                });
            PacketChannel
                .On<UpdateAccuracyBroadcast>(e =>
                {
                    // 위와 같은 이유로 이렇게 한다.
                    PlayerList.Remove(PlayerGeneralManager.GetPlayer(e.PlayerId));
                    PlayerList.Add(PlayerGeneralManager.GetPlayer(e.PlayerId));
                });
            PlayerGeneralManager
                .Players
                .ObserveDictionaryAdd()
                .Subscribe(e => PlayerList.Add(e.Value));
            PlayerGeneralManager
                .Players
                .ObserveDictionaryRemove()
                .Subscribe(e => PlayerList.Remove(e.Value));
        }
    }
}