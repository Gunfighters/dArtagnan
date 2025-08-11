using System.Net;
using System.Net.Sockets;

namespace dArtagnan.Server;

/// <summary>
/// 메인 서버 클래스
/// </summary>
public class TcpServer
{
    private TcpListener tcpListener = null!;
    private GameLoop gameLoop = null!;
    private GameManager gameManager = null!;
    private AdminConsole adminConsole = null!;

    public TcpServer(int port)
    {
        gameManager = new GameManager();
        gameLoop = new GameLoop(gameManager);
        adminConsole = new AdminConsole(gameManager);
        tcpListener = new TcpListener(IPAddress.Any, port);

        tcpListener.Start();
        // 클라이언트 연결 대기 루프
        _ = Task.Run(() => StartServerAsync(port));
    }

    private async Task StartServerAsync(int port)
    {
        try
        {
            Console.WriteLine($"D'Artagnan TCP 서버가 포트 {port}에서 클라이언트 연결을 기다리는 중...");
            
            LobbyReporter.ReportState(0);
            Console.WriteLine("게임 서버 준비 완료 - 로비 서버에 신호 전송됨");
            
            while (true)
            {
                try
                {
                    var tcpClient = await tcpListener.AcceptTcpClientAsync();
                    
                    var createCommand = new CreateClientCommand
                    {
                        TcpClient = tcpClient
                    };

                    _ = gameManager.EnqueueCommandAsync(createCommand);
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
}