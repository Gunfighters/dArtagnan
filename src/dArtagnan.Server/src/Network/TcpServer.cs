using System.Net;
using System.Net.Sockets;

namespace dArtagnan.Server;

/// <summary>
/// TCP 연결 관리만 담당하는 서버 클래스
/// </summary>
public class TcpServer
{
    private bool isRunning;
    private int nextClientId = 1;
    private TcpListener tcpListener = null!;
    private GameLoop gameLoop = null!;

    // 게임 매니저
    private GameManager gameManager = null!;

    public async Task StartAsync(int port)
    {
        try
        {
            // 게임 매니저 초기화
            gameManager = new GameManager();

            // TCP 리스너 시작
            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();
            isRunning = true;

            Console.WriteLine($"D'Artagnan TCP 서버가 포트 {port}에서 시작되었습니다.");
            Console.WriteLine("클라이언트 연결을 기다리는 중...");

            // 게임 루프 초기화 및 시작
            gameLoop = new GameLoop(this, gameManager);
            _ = Task.Run(() => gameLoop.StartAsync());

            // 클라이언트 연결 대기 루프
            while (isRunning)
            {
                try
                {
                    var tcpClient = await tcpListener.AcceptTcpClientAsync();
                    var client = new ClientConnection(nextClientId++, tcpClient, gameManager);
                    Console.WriteLine($"새 클라이언트 연결됨 (ID: {client.Id})");
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

    /// <summary>
    /// 서버 종료
    /// </summary>
    public Task StopAsync()
    {
        Console.WriteLine("TCP 서버를 종료합니다...");
        isRunning = false;

        try
        {
            // 게임 루프 중지
            gameLoop?.Stop();

            tcpListener?.Stop();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"서버 종료 오류: {ex.Message}");
        }

        Console.WriteLine("TCP 서버가 종료되었습니다.");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 현재 연결된 클라이언트 수 반환
    /// </summary>
    public int GetClientCount() => gameManager.ClientCount;

    /// <summary>
    /// 게임 매니저 반환 (CommandHandler에서 사용)
    /// </summary>
    public GameManager GetGameManager() => gameManager;


}