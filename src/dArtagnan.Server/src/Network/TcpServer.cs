using System.Net;
using System.Net.Sockets;

namespace dArtagnan.Server;

/// <summary>
/// TCP 연결 관리만 담당하는 서버 클래스
/// </summary>
public class TcpServer
{
    private bool isRunning;
    private TcpListener tcpListener = null!;
    private GameLoop gameLoop = null!;

    private GameManager gameManager = null!;

    public async Task StartAsync(int port)
    {
        try
        {
            gameManager = new GameManager();

            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            isRunning = true;

            Console.WriteLine($"D'Artagnan TCP 서버가 포트 {port}에서 시작되었습니다.");
            Console.WriteLine("클라이언트 연결을 기다리는 중...");

            // 게임 루프 초기화 및 시작
            gameLoop = new GameLoop(gameManager);
            _ = Task.Run(() => gameLoop.StartAsync());

            // 클라이언트 연결 대기 루프
            while (isRunning)
            {
                try
                {
                    var tcpClient = await tcpListener.AcceptTcpClientAsync();
                    
                    // CreateClientCommand를 통해 thread-safe하게 클라이언트 생성
                    var createCommand = new CreateClientCommand
                    {
                        TcpClient = tcpClient
                    };
                    
                    _ = gameManager.EnqueueCommandAsync(createCommand);
                }
                catch (ObjectDisposedException)
                {
                    // 서버 종료 시 발생하는 예외, 무시
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"클라이언트 연결 수락 오류: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"서버 시작 오류: {ex.Message}");
        }
    }

    public Task StopAsync()
    {
        Console.WriteLine("TCP 서버를 종료합니다...");
        isRunning = false;

        try
        {
            tcpListener.Stop();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"서버 종료 오류: {ex.Message}");
        }

        Console.WriteLine("TCP 서버가 종료되었습니다.");
        return Task.CompletedTask;
    }

    public int GetClientCount() => gameManager.Clients.Count;

    public GameManager GetGameManager() => gameManager;
}