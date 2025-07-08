using System.Collections.Concurrent;
using System.Numerics;
using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 게임 세션, 클라이언트 연결, 브로드캐스팅을 통합 관리하는 클래스
/// </summary>
public class GameManager
{
    public readonly ConcurrentDictionary<int, Player> players = new();
    public readonly ConcurrentDictionary<int, ClientConnection> clients = new(); // 클라이언트 연결도 여기서 관리
    public Player? Host;
    public GameState CurrentGameState { get; private set; } = GameState.Waiting;
    public Player? LastManStanding => players.Values.SingleOrDefault(p => p.Alive);
    public int Round = 0;
    public const int MAX_ROUND = 4;
    
    public void AddClient(ClientConnection client)
    {
        clients.TryAdd(client.Id, client);
        Console.WriteLine($"클라이언트 {client.Id} 추가됨 (현재 접속자: {clients.Count})");
    }

    private async Task SetHost(Player? player)
    {
        Host = player;
        Console.WriteLine($"[게임] 새 방장: {Host?.Id}");
        if (Host != null)
        {
            await BroadcastToAll(new NewHost { HostId = Host.Id });
        }
    }

    public async Task<Player> AddPlayer(int clientId, string nickname)
    {
        var player = new Player(clientId, nickname, Vector2.Zero);
        players.TryAdd(player.Id, player);
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
        players.TryRemove(clientId, out _);
        clients.TryRemove(clientId, out _);
            
        if (player != null)
        {
            Console.WriteLine($"[게임] 플레이어 {player.Id} 제거 완료 (현재 인원: {players.Count}, 접속자: {clients.Count})");
        }

        if (player == Host)
        {
            var nextHost = players.Values.FirstOrDefault(p => p.Alive);
            await SetHost(nextHost);
        }

        if (players.IsEmpty && CurrentGameState == GameState.Playing)
        {
            await SetGameState(GameState.Waiting);
        }
    }

    public Player? GetPlayerById(int clientId)
    {
        players.TryGetValue(clientId, out var player);
        return player;
    }

    public async Task BroadcastToAll(IPacket packet)
    {
        var tasks = clients.Values
            .Where(client => client.IsConnected)
            .Select(client => client.SendPacketAsync(packet)).ToList();

        if (tasks.Count != 0)
        {
            await Task.WhenAll(tasks);
        }
    }

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

    public List<PlayerInformation> PlayersInRoom()
    {
        return players.Values.Select(player => player.PlayerInformation).ToList();
    }

    public int GetAlivePlayerCount()
    {
        return players.Values.Count(p => p.Alive);
    }

    public bool RoundOver()
    {
        return players.Values.Count(p => p.Alive) <= 1;
    }

    public bool GameOver()
    {
        return RoundOver() && Round >= MAX_ROUND;
    }

    private void ResetRespawn()
    {
        foreach (var player in players.Values)
        {
            player.Reset();
            player.UpdatePosition(Player.GetSpawnPosition(player.Id));
        }
    }

    public async Task StartGame()
    {
        Console.WriteLine($"[게임] 게임 시작! (참가자: {players.Count}명)");
        await StartRound(1);
    }

    private async Task SetGameState(GameState newState)
    {
        var oldState = CurrentGameState;
        CurrentGameState = newState;
        Console.WriteLine($"[게임] 게임 상태 변경: {oldState} -> {newState}");
        switch (newState)
        {
            case GameState.Waiting:
                await BroadcastToAll(new GameWaiting { PlayersInfo = PlayersInRoom() });
                break;
            case GameState.Playing:
                await BroadcastToAll(new GamePlaying { PlayersInfo = PlayersInRoom(), Round = Round });
                break;
        }
    }

    public bool IsGamePlaying()
    {
        return CurrentGameState == GameState.Playing;
    }

    public float GetPingById(int id)
    {
        return clients[id].Ping;
    }

    public async Task StartRound(int newRound)
    {
        Round = newRound;
        ResetRespawn();
        await SetGameState(GameState.Playing);
    }

    public async Task OnGameOver()
    {
        await BroadcastToAll(new Winner { PlayerId = LastManStanding!.Id });
        await Task.Delay(2500); // 2.5초 쉼
        ResetRespawn();
        Round = 0;
        await SetGameState(GameState.Waiting);
    }
}