using System.Collections.Concurrent;
using dArtagnan.Shared;

namespace dArtagnan.Server.Game
{
    /// <summary>
    /// 게임 세션을 관리하는 클래스
    /// </summary>
    public class GameSession
    {
        private readonly ConcurrentDictionary<int, Player> players = new();
        private int nextPlayerId = 1;

        /// <summary>
        /// 현재 게임에 참여 중인 플레이어 수
        /// </summary>
        public int PlayerCount => players.Count;

        /// <summary>
        /// 게임에 참여 중인 모든 플레이어 목록
        /// </summary>
        public IEnumerable<Player> Players => players.Values.Where(p => p.IsInGame);

        /// <summary>
        /// 모든 플레이어 목록 (게임 참여 여부 무관)
        /// </summary>
        public IEnumerable<Player> AllPlayers => players.Values;

        /// <summary>
        /// 플레이어를 게임 세션에 추가합니다
        /// </summary>
        public Player AddPlayer(int clientId, string nickname)
        {
            var player = new Player(clientId, nextPlayerId++, nickname);
            players.TryAdd(clientId, player);
            return player;
        }

        /// <summary>
        /// 플레이어를 게임 세션에서 제거합니다
        /// </summary>
        public bool RemovePlayer(int clientId)
        {
            return players.TryRemove(clientId, out _);
        }

        /// <summary>
        /// 클라이언트 ID로 플레이어를 조회합니다
        /// </summary>
        public Player? GetPlayerByClientId(int clientId)
        {
            players.TryGetValue(clientId, out var player);
            return player;
        }

        /// <summary>
        /// 플레이어 ID로 플레이어를 조회합니다
        /// </summary>
        public Player? GetPlayerByPlayerId(int playerId)
        {
            return players.Values.FirstOrDefault(p => p.PlayerId == playerId);
        }

        /// <summary>
        /// 플레이어가 게임에 참여합니다
        /// </summary>
        public bool JoinPlayer(int clientId)
        {
            var player = GetPlayerByClientId(clientId);
            if (player != null)
            {
                player.JoinGame();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 플레이어가 게임에서 퇴장합니다
        /// </summary>
        public bool LeavePlayer(int clientId)
        {
            var player = GetPlayerByClientId(clientId);
            if (player != null)
            {
                player.LeaveGame();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 게임에 참여 중인 플레이어들의 정보를 PlayerInformation 리스트로 반환합니다
        /// </summary>
        public List<PlayerInformation> GetPlayersInformation()
        {
            return Players.Select(player => new PlayerInformation
            {
                playerId = player.PlayerId,
                nickname = player.Nickname,
                direction = player.Direction,
                x = player.X,
                y = player.Y,
                accuracy = player.Accuracy,
                totalReloadTime = player.TotalReloadTime,
                remainingReloadTime = player.RemainingReloadTime,
                speed = player.Speed,
                alive = player.Alive
            }).ToList();
        }

        /// <summary>
        /// 게임에 참여 중인 플레이어들의 위치 정보를 PlayerPosition 리스트로 반환합니다
        /// </summary>
        public List<PlayerPosition> GetPlayersPositions()
        {
            return Players.Select(player => new PlayerPosition
            {
                playerId = player.PlayerId,
                x = player.X,
                y = player.Y
            }).ToList();
        }

        /// <summary>
        /// 생존한 플레이어 수를 반환합니다
        /// </summary>
        public int GetAlivePlayerCount()
        {
            return Players.Count(p => p.Alive);
        }

        /// <summary>
        /// 게임이 종료되었는지 확인합니다 (생존자가 1명 이하)
        /// </summary>
        public bool IsGameOver()
        {
            return GetAlivePlayerCount() <= 1;
        }
    }
} 