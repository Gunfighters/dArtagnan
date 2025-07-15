using System.Collections.Concurrent;
using System.Numerics;
using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 게임 세션, 클라이언트 연결, 브로드캐스팅을 통합 관리하는 클래스
/// </summary>
public class GameManager
{
    public readonly ConcurrentDictionary<int, Player> Players = new();
    public readonly ConcurrentDictionary<int, ClientConnection> Clients = new(); // 클라이언트 연결도 여기서 관리
    public Player? Host;
    public GameState CurrentGameState { get; private set; } = GameState.Waiting;
    public Player? LastManStanding => Players.Values.SingleOrDefault(p => p.Alive);
    public int Round = 0;
    public List<Player> Survivors => Players.Values.Where(p => p.Alive).ToList();
    public int MANDATORY_BET = 30;
    public HashSet<Player> rouletteDonePlayers = [];
    
    public void AddClient(ClientConnection client)
    {
        Clients.TryAdd(client.Id, client);
        Console.WriteLine($"클라이언트 {client.Id} 추가됨 (현재 접속자: {Clients.Count})");
    }

    private async Task SetHost(Player? player)
    {
        Host = player;
        Console.WriteLine($"[게임] 새 방장: {Host?.Id}");
        if (Host != null)
        {
            await BroadcastToAll(new NewHostBroadcast { HostId = Host.Id });
        }
    }

    public async Task<Player> AddPlayer(int clientId, string nickname)
    {
        var player = new Player(clientId, nickname, Vector2.Zero);
        Players.TryAdd(player.Id, player);
        if (Host == null)
        {
            await SetHost(player);
        }
        return player;
    }

    public async Task RemoveClient(int clientId)
    {
        var player = GetPlayerById(clientId);
            
        // 게임 중인 플레이어면 다른 플레이어들에게 퇴장 알림
        if (player != null)
        {
            Console.WriteLine($"[게임] 플레이어 {player.Id}({player.Nickname}) 퇴장 처리");
                
            await BroadcastToAllExcept(new PlayerLeaveBroadcast
            {
                PlayerId = player.Id
            }, clientId);
        }

        // 플레이어와 클라이언트 제거
        Players.TryRemove(clientId, out _);
        Clients.TryRemove(clientId, out _);
            
        if (player != null)
        {
            Console.WriteLine($"[게임] 플레이어 {player.Id} 제거 완료 (현재 인원: {Players.Count}, 접속자: {Clients.Count})");
        }

        if (player == Host)
        {
            var nextHost = Players.Values.FirstOrDefault(p => p.Alive);
            await SetHost(nextHost);
        }

        if (Players.IsEmpty && CurrentGameState == GameState.Playing)
        {
            await SetGameState(GameState.Waiting);
        }
    }

    public Player? GetPlayerById(int clientId)
    {
        Players.TryGetValue(clientId, out var player);
        return player;
    }

    public async Task BroadcastToAll(IPacket packet)
    {
        var tasks = Clients.Values
            .Where(client => client.IsConnected)
            .Select(client => client.SendPacketAsync(packet)).ToList();

        if (tasks.Count != 0)
        {
            await Task.WhenAll(tasks);
        }
    }

    public async Task BroadcastToAllExcept(IPacket packet, int excludeClientId)
    {
        var tasks = Clients.Values
            .Where(client => client.Id != excludeClientId && client.IsConnected)
            .Select(client => client.SendPacketAsync(packet));

        if (tasks.Any())
        {
            await Task.WhenAll(tasks);
        }
    }

    public List<PlayerInformation> PlayersInRoom()
    {
        return Players.Values.Select(player => player.PlayerInformation).ToList();
    }

    public int GetAlivePlayerCount()
    {
        return Players.Values.Count(p => p.Alive);
    }

    public bool RoundOver()
    {
        return Players.Values.Count(p => p.Alive) <= 1;
    }

    public bool GameOver()
    {
        return Players.Values.Count(p => !p.Bankrupt) <= 1;
    }

    public void ResetRespawnAll(bool includeBankrupts)
    {
        var pool = includeBankrupts ? Players.Values : Players.Values.Where(p => !p.Bankrupt).ToList();
        var size = pool.Count;
        for (var index = 0; index < size; index++)
        {
            var player = pool.ElementAt(index);
            player.ResetForNextRound();
            player.UpdatePosition(Player.GetSpawnPosition(index));
        }
    }

    public async Task StartGame()
    {
        Console.WriteLine($"[게임] 게임 시작! (참가자: {Players.Count}명)");
        await SetGameState(GameState.RouletteSpinning);
   }

    private async Task SetGameState(GameState newState)
    {
        var oldState = CurrentGameState;
        CurrentGameState = newState;
        Console.WriteLine($"[게임] 게임 상태 변경: {oldState} -> {newState}");
        switch (newState)
        {
            case GameState.Waiting:
                await BroadcastToAll(new GameInWaitingFromServer { PlayersInfo = PlayersInRoom() });
                break;
            case GameState.Playing:
                await BroadcastToAll(new GameInPlayingFromServer { PlayersInfo = PlayersInRoom(), Round = Round });
                break;
            case GameState.RouletteSpinning:
                rouletteDonePlayers.Clear();
                List<int> accuracyPool = [];
                for (var i = 0; i < 8; i++)
                {
                    accuracyPool.Add(Player.GenerateRandomAccuracy());
                }
                foreach (var p in Players.Values)
                {
                    p.ResetForInitialGame(accuracyPool[Random.Shared.Next(0, accuracyPool.Count)]);
                }
                ResetRespawnAll(true);
                await Task.WhenAll(
                    Players.Values.Select(p => Clients[p.Id].SendPacketAsync(new YourAccuracyAndPool
                        { AccuracyPool = accuracyPool, YourAccuracy = p.Accuracy })));
                foreach (var p in Players.Values)
                {
                    Console.WriteLine($"{p.Nickname}: {p.Accuracy}%");
                }
                break;
        }
    }

    public async Task ProcessRoundOver()
    {
        await Task.Delay(2500);
        TakeMandatoryBetAll();
        if (GameOver())
        {
            await AnnounceWinner();
            await BackToWaiting();
        }
        else
        {
            await StartRound(Round + 1);
        }
    }

    private void TakeMandatoryBetAll()
    {
        foreach (var p in Players.Values)
        {
            p.Withdraw(MANDATORY_BET);
        }
    }

    public bool IsGamePlaying()
    {
        return CurrentGameState != GameState.Waiting;
    }

    public async Task StartRound(int newRound)
    {
        ResetRespawnAll(false);
        Round = newRound;
        await SetGameState(GameState.Playing);
    }

    private async Task BackToWaiting()
    {
        foreach (var p in Players.Values)
        {
            p.ResetForInitialGame(0);
        }
        ResetRespawnAll(true);
        Round = 0;
        rouletteDonePlayers.Clear();
        await SetGameState(GameState.Waiting);
    }

    private async Task AnnounceWinner()
    {
        await BroadcastToAll(new WinnerBroadcast { PlayerId = LastManStanding?.Id ?? -1 });
    }
}