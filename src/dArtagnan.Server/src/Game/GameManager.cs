using System.Collections.Concurrent;
using dArtagnan.Shared;

namespace dArtagnan.Server
{
    /// <summary>
    /// 게임 상태를 나타내는 열거형
    /// </summary>
    public enum GameState
    {
        Waiting,    // 대기 중 (Ready 단계 포함)
        Playing     // 게임 진행 중
    }

    /// <summary>
    /// 게임 세션, 클라이언트 연결, 브로드캐스팅을 통합 관리하는 클래스
    /// </summary>
    public class GameManager
    {
        private readonly ConcurrentDictionary<int, Player> players = new();
        private readonly ConcurrentDictionary<int, ClientConnection> clients = new(); // 클라이언트 연결도 여기서 관리
        private int nextPlayerId = 1;
        private GameState currentGameState = GameState.Waiting;
        public readonly ConcurrentDictionary<int, float> ping = new();

        /// <summary>
        /// 현재 게임 상태
        /// </summary>
        public GameState CurrentGameState => currentGameState;

        /// <summary>
        /// 현재 게임에 참여 중인 플레이어 수
        /// </summary>
        public int PlayerCount => players.Count;

        /// <summary>
        /// 현재 연결된 클라이언트 수
        /// </summary>
        public int ClientCount => clients.Count;

        /// <summary>
        /// 게임에 참여 중인 모든 플레이어 목록
        /// </summary>
        public IEnumerable<Player> Players => players.Values.Where(p => p.IsInGame);

        /// <summary>
        /// 모든 플레이어 목록 (게임 참여 여부 무관)
        /// </summary>
        public IEnumerable<Player> AllPlayers => players.Values;

        /// <summary>
        /// 클라이언트 연결을 추가합니다
        /// </summary>
        public void AddClient(ClientConnection client)
        {
            clients.TryAdd(client.Id, client);
            Console.WriteLine($"클라이언트 {client.Id} 추가됨 (현재 접속자: {clients.Count})");
        }

        /// <summary>
        /// 플레이어를 게임에 추가합니다
        /// </summary>
        public Player AddPlayer(int clientId, string nickname)
        {
            var player = new Player(clientId, nextPlayerId++, nickname);
            players.TryAdd(clientId, player);
            return player;
        }

        /// <summary>
        /// 클라이언트 연결과 플레이어를 모두 제거합니다
        /// </summary>
        public async Task RemoveClient(int clientId)
        {
            var player = GetPlayerByClientId(clientId);
            
            // 게임 중인 플레이어면 다른 플레이어들에게 퇴장 알림
            if (player != null && player.IsInGame)
            {
                Console.WriteLine($"[게임] 플레이어 {player.PlayerId}({player.Nickname}) 퇴장 처리");
                
                await BroadcastToAllExcept(new PlayerLeaveBroadcast
                {
                    playerId = player.PlayerId
                }, clientId);
            }

            // 플레이어와 클라이언트 제거
            players.TryRemove(clientId, out _);
            clients.TryRemove(clientId, out _);
            
            if (player != null)
            {
                Console.WriteLine($"[게임] 플레이어 {player.PlayerId} 제거 완료 (현재 인원: {PlayerCount}, 접속자: {ClientCount})");
            }
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
        /// 모든 클라이언트에게 브로드캐스트
        /// </summary>
        public async Task BroadcastToAll(IPacket packet)
        {
            var tasks = clients.Values
                .Where(client => client.IsConnected)
                .Select(client => client.SendPacketAsync(packet));

            if (tasks.Any())
            {
                await Task.WhenAll(tasks);
            }
        }

        /// <summary>
        /// 특정 클라이언트를 제외하고 모든 클라이언트에게 브로드캐스트
        /// </summary>
        public async Task BroadcastToAllExcept(IPacket packet, int excludeClientId)
        {
            var tasks = clients.Values
                .Where(client => client.Id != excludeClientId && client.IsConnected)
                .Select(client => client.SendPacketAsync(packet));

            if (tasks.Any())
            {
                await Task.WhenAll(tasks);
            }
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
        /// 게임이 종료되어야 하는지 확인합니다
        /// </summary>
        public bool ShouldEndGame()
        {
            return false;
        }

        /// <summary>
        /// 플레이어의 Ready 상태를 업데이트합니다
        /// </summary>
        public void UpdatePlayerReady(int clientId, bool ready)
        {
            var player = GetPlayerByClientId(clientId);
            if (player != null && player.IsInGame)
            {
                player.UpdateReady(ready);
            }
        }

        /// <summary>
        /// 모든 플레이어가 Ready 상태인지 확인합니다
        /// </summary>
        public bool AreAllPlayersReady()
        {
            var inGamePlayers = Players.ToList();
            return inGamePlayers.Any() && inGamePlayers.All(p => p.IsReady);
        }

        /// <summary>
        /// 게임에 참여 중인 플레이어 중 Ready 상태인 플레이어 수를 반환합니다
        /// </summary>
        public int GetReadyPlayerCount()
        {
            return Players.Count(p => p.IsReady);
        }

        /// <summary>
        /// 게임을 시작합니다
        /// </summary>
        public async Task StartGame()
        {
            Console.WriteLine($"[게임] 게임 시작! (참가자: {PlayerCount}명)");
            
            // 게임 상태를 Playing으로 변경
            currentGameState = GameState.Playing;
            
            // 모든 플레이어에게 게임 시작 브로드캐스트
            await BroadcastToAll(new GameStart());
        }

        /// <summary>
        /// 게임 상태를 변경합니다
        /// </summary>
        public void SetGameState(GameState newState)
        {
            var oldState = currentGameState;
            currentGameState = newState;
            Console.WriteLine($"[게임] 게임 상태 변경: {oldState} -> {newState}");
        }

        /// <summary>
        /// 게임이 진행 중인지 확인합니다
        /// </summary>
        public bool IsGamePlaying()
        {
            return currentGameState == GameState.Playing;
        }
    }
} 