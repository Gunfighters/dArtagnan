using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using dArtagnan.Shared;

namespace dArtagnan.ClientTest;

internal class Program
{
    private static TcpClient? client;
    private static NetworkStream? stream;
    private static bool isConnected = false;
    private static bool isRunning = true; // 프로그램 실행 상태
    private static Vector2 position;
    private static float speed = 40f; // 일정한 속도
    private static int direction;
    private static Stopwatch stopwatch = new();

    static void CalculatePositionSoFar()
    {
        position += speed * stopwatch.ElapsedMilliseconds / 1000f * DirectionHelper.IntToDirection(direction);
        stopwatch.Restart();
    }

    static async Task SendMovementData()
    {
        try
        {
            var playerDirection = new PlayerMovementDataFromClient { Direction = direction, MovementData = { Direction = direction, Position = position, Speed = speed} };
            await NetworkUtils.SendPacketAsync(stream, playerDirection);
            Console.WriteLine($"이동 데이터 패킷 전송: 방향 {playerDirection.Direction}, 위치 {playerDirection.MovementData.Position} 속도: {playerDirection.MovementData.Speed}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"이동 패킷 전송 실패: {ex.Message}");
        }
    }

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== D'Artagnan 테스트 클라이언트 ===");
        Console.WriteLine("명령어:");
        Console.WriteLine("  c/connect [0/1] - 서버 연결 (0: localhost, 1: 54.180.85.77, 기본: 0)");
        Console.WriteLine("  j/join [nickname] - 게임 참가");
        Console.WriteLine("  s/start - 게임 시작");
        Console.WriteLine("  d/dir [i] - 플레이어 이동 방향 변경");
        Console.WriteLine("  sh/shoot [targetId] - 플레이어 공격");
        Console.WriteLine("  a/accuracy [state] - 정확도 상태 변경 (-1: 감소, 0: 유지, 1: 증가)");
        Console.WriteLine("  ro/roulette [count] - 룰렛 돌리기 완료 패킷 전송 (기본: 1)");
        Console.WriteLine("  au/augment [index] - 증강 선택 (0, 1, 2 중 하나)");
        Console.WriteLine("  ic/item-create [true/false] - 아이템 제작 시작/취소 (기본: true)");
        Console.WriteLine("  iu/use-item [targetId] - 아이템 사용 (targetId는 선택적, 기본: -1)");
        Console.WriteLine("  chat/msg [message] - 채팅 메시지 전송");
        Console.WriteLine("  l/leave - 게임 나가기");
        Console.WriteLine("  q/quit - 종료");
        Console.WriteLine("=====================================");

        var receiveTask = Task.Run(ReceiveLoop);

        while (isRunning) // isConnected 대신 isRunning을 사용하도록 변경
        {
            Console.Write("> ");
            var input = Console.ReadLine();
            if (string.IsNullOrEmpty(input)) continue;

            await ProcessCommand(input);
        }

        await receiveTask;
    }

    static async Task ProcessCommand(string input)
    {
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        var command = parts[0].ToLower();

        try
        {
            switch (command)
            {
                case "c":
                case "connect":
                    var serverChoice = parts.Length > 1 ? int.Parse(parts[1]) : 0;
                    var host = serverChoice == 1 ? "54.180.85.77" : "localhost";
                    var port = 7777;
                    await ConnectToServer(host, port);
                    break;

                case "j":
                case "join":
                    var nickname = parts.Length > 1 ? parts[1] : "TestPlayer";
                    await JoinGame(nickname);
                    break;
                    
                case "s":
                case "start":
                    await StartGame();
                    break;

                case "d":
                case "dir":
                    if (parts.Length >= 2)
                    {
                        var i = int.Parse(parts[1]);
                        await SendDirection(i);
                    }
                    else
                    {
                        Console.WriteLine("사용법: d/dir [i]");
                    }
                    break;
                
                case "sh":
                case "shoot":
                    if (parts.Length >= 2)
                    {
                        var targetId = int.Parse(parts[1]);
                        await SendShoot(targetId);
                    }
                    else
                    {
                        Console.WriteLine("사용법: sh/shoot [targetId]");
                    }
                    break;

                case "a":
                case "accuracy":
                    if (parts.Length >= 2)
                    {
                        var state = int.Parse(parts[1]);
                        await SendAccuracyState(state);
                    }
                    else
                    {
                        Console.WriteLine("사용법: a/accuracy [state] (-1: 감소, 0: 유지, 1: 증가)");
                    }
                    break;

                case "ro":
                case "roulette":
                    if (parts.Length >= 2)
                    {
                        var count = int.Parse(parts[1]);
                        await SendRoulette(count);
                    }
                    else
                    {
                        await SendRoulette(1);
                    }
                    break;

                case "au":
                case "augment":
                    if (parts.Length >= 2)
                    {
                        var index = int.Parse(parts[1]);
                        await SendAugmentSelection(index);
                    }
                    else
                    {
                        Console.WriteLine("사용법: au/augment [index] (0, 1, 2 중 하나)");
                    }
                    break;

                case "ic":
                case "item-create":
                    if (parts.Length >= 2)
                    {
                        var isCreating = bool.Parse(parts[1]);
                        await SendItemCreating(isCreating);
                    }
                    else
                    {
                        await SendItemCreating(true); // 기본값: 제작 시작
                    }
                    break;

                case "iu":
                case "use-item":
                    if (parts.Length >= 2)
                    {
                        var targetId = int.Parse(parts[1]);
                        await SendUseItem(targetId);
                    }
                    else
                    {
                        await SendUseItem(-1); // 기본값: 타겟 없음
                    }
                    break;

                case "chat":
                case "msg":
                    if (parts.Length >= 2)
                    {
                        var message = string.Join(" ", parts.Skip(1)); // 첫 번째 단어(명령어) 제외하고 나머지를 메시지로 합치기
                        await SendChat(message);
                    }
                    else
                    {
                        Console.WriteLine("사용법: chat/msg [message]");
                    }
                    break;

                case "l":
                case "leave":
                    await SendLeave();
                    break;

                case "q":
                case "quit":
                    await Disconnect();
                    isRunning = false; // isConnected 대신 isRunning을 사용하도록 변경
                    break;

                default:
                    Console.WriteLine("알 수 없는 명령어입니다.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"명령어 처리 오류: {ex.Message}");
        }
    }

    static async Task ConnectToServer(string host, int port)
    {
        try
        {
            if (isConnected)
            {
                Console.WriteLine("이미 서버에 연결되어 있습니다.");
                return;
            }

            client = new TcpClient();
            await client.ConnectAsync(host, port);
            
            // TCP NoDelay 설정 (Nagle's algorithm 비활성화)
            client.NoDelay = true;
            
            stream = client.GetStream();
            isConnected = true;

            Console.WriteLine($"서버에 연결되었습니다: {host}:{port}");
            
            // 연결 성공 후 자동으로 게임 참가
            await JoinGame("TestPlayer");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"서버 연결 실패: {ex.Message}");
        }
    }

    static async Task JoinGame(string nickname)
    {
        if (!isConnected || stream == null)
        {
            Console.WriteLine("먼저 서버에 연결해주세요.");
            return;
        }
            
        stopwatch.Start();

        try
        {
            var joinPacket = new PlayerJoinRequest();
            await NetworkUtils.SendPacketAsync(stream, joinPacket);
            Console.WriteLine($"게임 참가 요청을 보냈습니다: {nickname}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"게임 참가 실패: {ex.Message}");
        }
    }

    static async Task SendDirection(int dir)
    {
        if (!isConnected || stream == null)
        {
            Console.WriteLine("먼저 서버에 연결해주세요.");
            return;
        }

        CalculatePositionSoFar();
        direction = dir;
        await SendMovementData();
    }

    static async Task SendShoot(int targetId)
    {
        if (!isConnected || stream == null)
        {
            Console.WriteLine("먼저 서버에 연결해주세요.");
            return;
        }

        try
        {
            await NetworkUtils.SendPacketAsync(stream, new PlayerShootingFromClient
            {
                TargetId = targetId
            });
            Console.WriteLine($"공격 패킷 전송: 타겟 {targetId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"공격 패킷 전송 실패: {ex.Message}");
        }
    }

    static async Task SendAccuracyState(int state)
    {
        if (!isConnected || stream == null)
        {
            Console.WriteLine("먼저 서버에 연결해주세요.");
            return;
        }

        if (state < -1 || state > 1)
        {
            Console.WriteLine("정확도 상태는 -1, 0, 1 중 하나여야 합니다.");
            return;
        }

        try
        {
            await NetworkUtils.SendPacketAsync(stream, new SetAccuracyState
            {
                AccuracyState = state
            });
            
            string stateText = state switch
            {
                -1 => "감소",
                0 => "유지",
                1 => "증가",
                _ => "알 수 없음"
            };
            
            Console.WriteLine($"정확도 상태 변경 패킷 전송: {state} ({stateText})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"정확도 상태 패킷 전송 실패: {ex.Message}");
        }
    }

    static async Task SendRoulette(int count)
    {
        if (!isConnected || stream == null)
        {
            Console.WriteLine("먼저 서버에 연결해주세요.");
            return;
        }

        try
        {
            await NetworkUtils.SendPacketAsync(stream, new RouletteDone { TrialCount = count });
            Console.WriteLine($"룰렛 돌리기 완료 패킷 전송: 횟수 {count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"룰렛 돌리기 완료 패킷 전송 실패: {ex.Message}");
        }
    }

    static async Task SendAugmentSelection(int id)
    {
        if (!isConnected || stream == null)
        {
            Console.WriteLine("먼저 서버에 연결해주세요.");
            return;
        }

        try
        {
            await NetworkUtils.SendPacketAsync(stream, new AugmentDoneFromClient
            {
                SelectedAugmentID = id
            });
            Console.WriteLine($"증강 선택 패킷 전송: {id}번 선택");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"증강 선택 패킷 전송 실패: {ex.Message}");
        }
    }

    static async Task SendItemCreating(bool isCreating)
    {
        if (!isConnected || stream == null)
        {
            Console.WriteLine("먼저 서버에 연결해주세요.");
            return;
        }

        try
        {
            await NetworkUtils.SendPacketAsync(stream, new ItemCreatingStateFromClient
            {
                IsCreatingItem = isCreating
            });
            
            var action = isCreating ? "시작" : "취소";
            Console.WriteLine($"아이템 제작 {action} 패킷 전송");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"아이템 제작 패킷 전송 실패: {ex.Message}");
        }
    }

    static async Task SendUseItem(int targetId)
    {
        if (!isConnected || stream == null)
        {
            Console.WriteLine("먼저 서버에 연결해주세요.");
            return;
        }

        try
        {
            await NetworkUtils.SendPacketAsync(stream, new UseItemFromClient
            {
                TargetPlayerId = targetId
            });
            
            var targetText = targetId == -1 ? "타겟 없음" : $"타겟 {targetId}";
            Console.WriteLine($"아이템 사용 패킷 전송: {targetText}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"아이템 사용 패킷 전송 실패: {ex.Message}");
        }
    }

    static async Task SendLeave()
    {
        if (!isConnected || stream == null)
        {
            Console.WriteLine("먼저 서버에 연결해주세요.");
            return;
        }

        try
        {
            await NetworkUtils.SendPacketAsync(stream, new PlayerLeaveFromClient());
            Console.WriteLine("게임 나가기 패킷 전송");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"게임 나가기 패킷 전송 실패: {ex.Message}");
        }
    }

    static async Task SendChat(string message)
    {
        if (!isConnected || stream == null)
        {
            Console.WriteLine("먼저 서버에 연결해주세요.");
            return;
        }

        if (string.IsNullOrEmpty(message))
        {
            Console.WriteLine("메시지를 입력해주세요.");
            return;
        }

        try
        {
            await NetworkUtils.SendPacketAsync(stream, new ChatFromClient
            {
                Message = message
            });
            Console.WriteLine($"채팅 메시지 전송: {message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"채팅 메시지 전송 실패: {ex.Message}");
        }
    }

    static async Task ReceiveLoop()
    {
        while (isRunning) // isConnected 대신 isRunning을 사용하도록 변경
        {
            if (stream != null && isConnected)
            {
                try
                {
                    var packet = await NetworkUtils.ReceivePacketAsync(stream);
                    Console.WriteLine($"[수신] 패킷: {packet}");
                    await HandlePacket(packet);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("서버와의 연결이 끊어졌습니다.");
                    isConnected = false;
                    break;
                }
            }
        }
    }

    static async Task HandlePacket(IPacket packet)
    {
        try
        {
            switch (packet)
            {
                case YouAre youAre:
                    Console.WriteLine($"서버에서 플레이어 ID 할당: {youAre.PlayerId}");
                    break;
                        
                case PlayerJoinBroadcast joinBroadcast:
                    Console.WriteLine($"플레이어 {joinBroadcast.PlayerInfo.PlayerId} 참가!");
                    break;
                        
                case PlayerMovementDataBroadcast movementDataBroadcast:
                    Console.WriteLine($"{movementDataBroadcast.PlayerId}번 플레이어 이동 데이터 갱신: 방향 {movementDataBroadcast.MovementData.Direction}, 위치 {movementDataBroadcast.MovementData.Position} 속도 {movementDataBroadcast.MovementData.Speed}");
                    break;
                        
                case WaitingStartFromServer gameWaiting:
                    Console.WriteLine($"=== 현재 방 상태 ===");
                    foreach (var info in gameWaiting.PlayersInfo)
                    {
                        Console.WriteLine($"  플레이어 {info.PlayerId}: {info.Nickname}");
                        Console.WriteLine($"    소지금: {info.Balance}달러");
                        Console.WriteLine($"    위치: ({info.MovementData.Position.X:F2}, {info.MovementData.Position.Y:F2})");
                        Console.WriteLine($"    명중률: {info.Accuracy}%");
                        Console.WriteLine($"    정확도 상태: {info.AccuracyState} ({GetAccuracyStateText(info.AccuracyState)})");
                        Console.WriteLine($"    속도: {info.MovementData.Speed:F2}");
                        Console.WriteLine($"    에너지: {info.EnergyData.CurrentEnergy:F1}/{info.EnergyData.MaxEnergy} (최소필요: {info.MinEnergyToShoot})");
                        Console.WriteLine($"    생존: {(info.Alive ? "생존" : "사망")}");
                        if (info.Augments.Count > 0)
                        {
                            Console.WriteLine($"    증강: [{string.Join(", ", info.Augments)}]");
                        }
                        Console.WriteLine($"    아이템: {(info.CurrentItem == -1 ? "없음" : $"ID {info.CurrentItem}")}");
                        if (info.IsCreatingItem)
                        {
                            Console.WriteLine($"    제작 중: {info.CreatingRemainingTime:F1}초 남음");
                        }
                    }
                    break;
                        
                case RoundStartFromServer gamePlaying:
                    Console.WriteLine($"=== 게임 진행 중 (라운드 {gamePlaying.Round}) ===");
                    Console.WriteLine($"베팅금: {gamePlaying.BettingAmount}달러/10초");
                    foreach (var info in gamePlaying.PlayersInfo)
                    {
                        Console.WriteLine($"  플레이어 {info.PlayerId}: {info.Nickname}");
                        Console.WriteLine($"    소지금: {info.Balance}달러");
                        Console.WriteLine($"    위치: ({info.MovementData.Position.X:F2}, {info.MovementData.Position.Y:F2})");
                        Console.WriteLine($"    명중률: {info.Accuracy}%");
                        Console.WriteLine($"    정확도 상태: {info.AccuracyState} ({GetAccuracyStateText(info.AccuracyState)})");
                        Console.WriteLine($"    속도: {info.MovementData.Speed:F2}");
                        Console.WriteLine($"    에너지: {info.EnergyData.CurrentEnergy:F1}/{info.EnergyData.MaxEnergy} (최소필요: {info.MinEnergyToShoot})");
                        Console.WriteLine($"    생존: {(info.Alive ? "생존" : "사망")}");
                        if (info.Augments.Count > 0)
                        {
                            Console.WriteLine($"    증강: [{string.Join(", ", info.Augments)}]");
                        }
                        Console.WriteLine($"    아이템: {(info.CurrentItem == -1 ? "없음" : $"ID {info.CurrentItem}")}");
                        if (info.IsCreatingItem)
                        {
                            Console.WriteLine($"    제작 중: {info.CreatingRemainingTime:F1}초 남음");
                        }
                    }
                    break;
                        
                case PlayerShootingBroadcast shooting:
                    var hitMsg = shooting.Hit ? "명중!" : "빗나감";
                    Console.WriteLine($"플레이어 {shooting.ShooterId}가 플레이어 {shooting.TargetId}를 공격 - {hitMsg} (사격자 현재 에너지: {shooting.ShooterCurrentEnergy})");
                    break;
                        
                case UpdatePlayerAlive aliveUpdate:
                    var statusMsg = aliveUpdate.Alive ? "부활" : "사망";
                    Console.WriteLine($"플레이어 {aliveUpdate.PlayerId} {statusMsg}");
                    break;
                    
                case NewHostBroadcast newHost:
                    Console.WriteLine($"새로운 방장: {newHost.HostId}");
                    break;
                        
                case PlayerLeaveBroadcast leaveBroadcast:
                    Console.WriteLine($"플레이어 {leaveBroadcast.PlayerId}가 게임을 떠났습니다");
                    break;
                        
                case PlayerAccuracyStateBroadcast accuracyStateBroadcast:
                    Console.WriteLine($"플레이어 {accuracyStateBroadcast.PlayerId}의 정확도 상태 변경: {accuracyStateBroadcast.AccuracyState} ({GetAccuracyStateText(accuracyStateBroadcast.AccuracyState)})");
                    break;
                        
                case RouletteStartFromServer yourAccuracyAndPool:
                    Console.WriteLine($"=== 룰렛 정보 받음 ===");
                    Console.WriteLine($"당신의 정확도: {yourAccuracyAndPool.YourAccuracy}%");
                    Console.WriteLine($"정확도 풀: [{string.Join(", ", yourAccuracyAndPool.AccuracyPool)}]");
                    Console.WriteLine($"자동으로 룰렛 돌리기 완료 패킷 전송...");
                    
                    // 자동으로 룰렛 돌리기 완료 패킷 전송
                    await SendRoulette(1);
                    break;
                
                case BettingDeductionBroadcast bettingDeduction:
                    Console.WriteLine($"🎯 [베팅금 차감] {bettingDeduction.DeductedAmount}달러씩 차감됨");
                    Console.WriteLine($"💰 현재 총 판돈: {bettingDeduction.TotalPrizeMoney}달러");
                    break;
                    
                case PlayerBalanceUpdateBroadcast balanceUpdate:
                    Console.WriteLine($"💳 플레이어 {balanceUpdate.PlayerId}의 소지금 업데이트: {balanceUpdate.Balance}달러");
                    break;
                    
                case RoundWinnerBroadcast roundWinner:
                    if (roundWinner.PlayerIds != null && roundWinner.PlayerIds.Count > 0)
                    {
                        var winnerText = roundWinner.PlayerIds.Count == 1 
                            ? $"플레이어 {roundWinner.PlayerIds[0]}"
                            : $"플레이어 [{string.Join(", ", roundWinner.PlayerIds)}]";
                        Console.WriteLine($"🏆 [라운드 {roundWinner.Round} 승리] {winnerText}가 {roundWinner.PrizeMoney}달러 획득!");
                    }
                    else
                    {
                        Console.WriteLine($"🏆 [라운드 {roundWinner.Round}] 승리자 없음!");
                    }
                    break;
                    
                case GameWinnerBroadcast gameWinner:
                    if (gameWinner.PlayerIds != null && gameWinner.PlayerIds.Count > 0)
                    {
                        var winnerText = gameWinner.PlayerIds.Count == 1 
                            ? $"플레이어 {gameWinner.PlayerIds[0]}"
                            : $"플레이어 [{string.Join(", ", gameWinner.PlayerIds)}]";
                        Console.WriteLine($"🎊 [게임 최종 승리] {winnerText}가 게임에서 승리했습니다!");
                    }
                    else
                    {
                        Console.WriteLine($"🎊 [게임 종료] 승리자 없음!");
                    }
                    break;

                case AugmentStartFromServer augmentStart:
                    Console.WriteLine($"🔮 [증강 선택] 증강 옵션을 받았습니다:");
                    for (int i = 0; i < augmentStart.AugmentOptions.Count; i++)
                    {
                        Console.WriteLine($"  {i}: 증강 ID {augmentStart.AugmentOptions[i]}");
                    }
                    Console.WriteLine($"명령어 'au [ID]'로 증강을 선택하세요.");
                    break;

                case PlayerCreatingStateBroadcast creatingState:
                    var stateText = creatingState.IsCreatingItem ? "시작" : "중단";
                    Console.WriteLine($"🔨 [아이템 제작] 플레이어 {creatingState.PlayerId}가 아이템 제작을 {stateText}했습니다");
                    break;

                case ItemAcquiredBroadcast itemAcquired:
                    Console.WriteLine($"📦 [아이템 획득] 플레이어 {itemAcquired.PlayerId}가 아이템 ID {itemAcquired.ItemId}를 획득했습니다!");
                    break;

                case ItemUsedBroadcast itemUsed:
                    Console.WriteLine($"⚡ [아이템 사용] 플레이어 {itemUsed.PlayerId}가 아이템 ID {itemUsed.ItemId}를 사용했습니다");
                    break;

                case ChatBroadcast chatBroadcast:
                    if (chatBroadcast.PlayerId == -1)
                    {
                        Console.WriteLine($"💬 [시스템] {chatBroadcast.Message}");
                    }
                    else
                    {
                        Console.WriteLine($"💬 [플레이어 {chatBroadcast.PlayerId}] {chatBroadcast.Message}");
                    }
                    break;

                case UpdatePlayerCurrentEnergyBroadcast energyUpdate:
                    Console.WriteLine($"⚡ [에너지 업데이트] 플레이어 {energyUpdate.PlayerId}의 현재 에너지: {energyUpdate.CurrentEnergy:F1}");
                    break;

                case UpdatePlayerAccuracyBroadcast accuracyUpdate:
                    Console.WriteLine($"🎯 [정확도 업데이트] 플레이어 {accuracyUpdate.PlayerId}의 정확도: {accuracyUpdate.Accuracy}%");
                    break;

                case UpdatePlayerRangeBroadcast rangeUpdate:
                    Console.WriteLine($"📏 [사거리 업데이트] 플레이어 {rangeUpdate.PlayerId}의 사거리: {rangeUpdate.Range:F2}");
                    break;

                case UpdatePlayerMaxEnergyBroadcast maxEnergyUpdate:
                    Console.WriteLine($"🔋 [최대 에너지 업데이트] 플레이어 {maxEnergyUpdate.PlayerId}의 최대 에너지: {maxEnergyUpdate.MaxEnergy}");
                    break;

                case UpdatePlayerMinEnergyToShootBroadcast minEnergyUpdate:
                    Console.WriteLine($"💥 [사격 최소 필요 에너지 업데이트] 플레이어 {minEnergyUpdate.PlayerId}의 사격 최소 필요 에너지: {minEnergyUpdate.MinEnergyToShoot}");
                    break;
                        
                default:
                    Console.WriteLine($"처리되지 않은 패킷 타입: {packet}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"패킷 처리 오류: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    static string GetAccuracyStateText(int state)
    {
        return state switch
        {
            -1 => "감소",
            0 => "유지",
            1 => "증가",
            _ => "알 수 없음"
        };
    }

    static async Task Disconnect()
    {
        try
        {
            isConnected = false;
            stream?.Close();
            client?.Close();
            Console.WriteLine("서버와의 연결을 해제했습니다.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"연결 해제 오류: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    static async Task StartGame()
    {
        await NetworkUtils.SendPacketAsync(stream, new StartGameFromClient());
    }
}