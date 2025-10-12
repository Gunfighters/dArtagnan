using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using dArtagnan.Shared;

namespace dArtagnan.Server;

public class Bot : Player
{
    private readonly Stopwatch _movementUpdateStopwatch = new();
    private readonly Stopwatch _actionChangeStopwatch = new();
    private Cell _destination;
    private int _nextDir;
    private Cell _nextCell;

    public Bot(int id, string nickname, Vector2 position, GameManager gameManager) : base(id, nickname, "BOT", 1, position, gameManager)
    {
    }

    public override async Task InitToRound()
    {
        await base.InitToRound();
        _nextCell = new Cell((int)Math.Floor(Position.X), (int)Math.Floor(Position.Y));
        SetNewDestination();
        _movementUpdateStopwatch.Restart();
        _actionChangeStopwatch.Restart();
    }

    /// <summary>
    /// 봇의 AI 로직을 업데이트합니다 (GameLoop에서 호출)
    /// </summary>
    public async Task UpdateAI(float deltaTime)
    {
        if (!Alive) return;
        //
        // 사격 가능하면 채굴 취소하고 사격
        if (IsNotReloading())
        {
            // 사격 대상이 있는지 확인
            var pool = _gameManager.Players.Values.Where(p =>
                p != this
                && p.Alive
                && Vector2.Distance(p.Position, Position) <= Range - 0.2
                && _gameManager.CanShootAt(Position, p.Position)).ToList();

            if (pool.Any())
            {
                // 채굴 중이면 취소
                if (IsMining)
                {
                    await StopMining();
                }
                await ShootRichestInRange();
            }
        }

        if (_actionChangeStopwatch.ElapsedMilliseconds >= Random.Shared.Next(3, 8) * 1000)
        {
            if (!IsMining && (Balance < NextDeductionAmount || Random.Shared.NextSingle() > 0.7f))
                await Mine();
            _actionChangeStopwatch.Restart();
        }

        if (!IsMining)
        {
            SetNextCellAndDirection();
            await Move(_nextDir, _nextCell, deltaTime);
        }
    }

    private async Task ShootRichestInRange()
    {
        var pool = _gameManager.Players.Values.Where(p =>
            p != this
            && p.Alive
            && Vector2.Distance(p.Position, Position) <= Range - 0.2
            && _gameManager.CanShootAt(Position, p.Position)).ToImmutableArray();
        if (pool.Any())
        {
            var target = pool.Aggregate((a, b) => a.Balance > b.Balance ? a : b)!;
            await _gameManager.EnqueueCommandAsync(new PlayerShootingCommand { ShooterId = Id, TargetId = target.Id });
        }
    }

    private async Task Mine()
    {
        await _gameManager.EnqueueCommandAsync(new PlayerMovementCommand
            { MovementData = new MovementData { Direction = 0, Position = Position, Speed = Speed }, PlayerId = Id });
        await StartMining();
    }
    
    private void SetNextCellAndDirection()
    {
        if (InCell(_nextCell))
        {
            if (_nextCell == _destination || _gameManager.Players.Values.Count(p => p.Alive) <= 3 || Random.Shared.NextSingle() > 0.8f) SetNewDestination();
            var d = ComputeNextCellAndDirection();
            _nextCell = d.Item1;
            _nextDir = d.Item2;
        }
    }

    private void SetNewDestination()
    {
        var otherAlivePlayers = _gameManager.Players.Values.Where(p => p != this && p.Alive).ToList();

        if (otherAlivePlayers.Count > 0 && (_gameManager.Players.Values.Count(p => p.Alive) <= 3 || Random.Shared.NextSingle() > 0.5f))
        {
            _destination = otherAlivePlayers.Aggregate((a, b) => a.Balance > b.Balance ? a : b).InWhichCell();
        }
        else
        {
            _destination = _gameManager.SpawnPointOnRoundStart.Keys.ToList()[
                Random.Shared.Next(0, _gameManager.SpawnPointOnRoundStart.Count)];
        }
    }

    private (Cell, int) ComputeNextCellAndDirection()
    {
        var visited = new HashSet<Cell>();
        var q = new Queue<Cell>();
        var prev = new Dictionary<Cell, Cell>();
        var prevDir = new Dictionary<Cell, int>();
        q.Enqueue(_nextCell);
        visited.Add(_nextCell);
        prev[_nextCell] = _nextCell;
        prevDir[_nextCell] = 0;
        while (q.Count != 0)
        {
            var c = q.Dequeue();
            if (c == _destination)
            {
                while (prev[c] != _nextCell) c = prev[c];
                return (c, prevDir[c]);
            }

            for (var dir = 1; dir <= 7; dir += 2)
            {
                var d = dir.IntToDirection();
                var next = c + new Cell((int)Math.Round(d.X), (int)Math.Round(d.Y));
                if (!visited.Contains(next) && _gameManager.CanMoveFromToAdjacent(c, next))
                {
                    q.Enqueue(next);
                    visited.Add(next);
                    prev[next] = c;
                    prevDir[next] = dir;
                }
            }
        }
        return (_nextCell, 0);
    }

    private async Task Move(int dir, Cell nextCell, float deltaTime)
    {
        if (dir != Direction || _movementUpdateStopwatch.ElapsedMilliseconds >= 1000)
        {
            _movementUpdateStopwatch.Restart();
            var length = Speed * deltaTime;
            var pos = Position +
                      length * Vector2.Normalize(nextCell.ToVec() + new Vector2(0.5f, 0.5f) - Position);
            await _gameManager.EnqueueCommandAsync(new PlayerMovementCommand
            {
                MovementData = new MovementData { Direction = dir, Position = Position, Speed = Speed },
                PlayerId = Id
            });
        }
    }

    public async Task HandlePacketAsync(IPacket packet)
    {
        // 대부분의 패킷은 무시하지만, 선택이 필요한 패킷들은 자동으로 처리
        switch (packet)
        {
            case InitialRouletteStartFromServer:
                // 이니셜 룰렛 자동 완료
                await HandleInitialRouletteCompletion();
                break;
                
            case ShopStartFromServer shopPacket:
                // 샵 자동 처리 (램덤 아이템 구매)
                await HandleShopPurchase(shopPacket);
                break;
        }
    }

    private async Task HandleInitialRouletteCompletion()
    {
        Logger.log($"[Bot][{Nickname}] 이니셜 룰렛 자동 완료");

        await _gameManager.EnqueueCommandAsync(new InitialRouletteDoneCommand
        {
            PlayerId = Id
        });
    }

    private async Task HandleShopPurchase(ShopStartFromServer shopPacket)
    {
        Logger.log($"[Bot][{Nickname}] 샵 자동 처리 시작");

        // 50% 확률로 아이템 구매 시도
        if (Random.Shared.NextDouble() <= 0.5f && shopPacket.ShopData.YourItems.Count > 0)
        {
            var randomItem = shopPacket.ShopData.YourItems[Random.Shared.Next(shopPacket.ShopData.YourItems.Count)];
            if (Balance - randomItem.Price > 0)
            {
                Logger.log($"[Bot][{Nickname}] 아이템 구매 시도: {randomItem.ItemId}");

                await _gameManager.EnqueueCommandAsync(new ShopPurchaseItemCommand
                {
                    PlayerId = Id,
                    ItemId = randomItem.ItemId
                });
                return;
            }
        }

        // 30% 확률로 샵룰렛 시도
        if (Random.Shared.NextDouble() <= 0.3f && Balance >= shopPacket.ShopData.ShopRoulettePrice)
        {
            Logger.log($"[Bot][{Nickname}] 샵룰렛 시도");

            await _gameManager.EnqueueCommandAsync(new ShopRouletteCommand
            {
                PlayerId = Id
            });
        }
    }
}