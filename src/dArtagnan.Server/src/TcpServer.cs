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
            Logger.log($"[Server][System] TCP 서버 시작: 포트 {port}에서 대기 중");

            LobbyReporter.ReportState(0);
            Logger.log("[Server][System] 준비 완료: 로비 서버에 신호 전송");

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
                    Logger.log($"[Network][System] 클라이언트 연결 수락 오류: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.log($"[Server][System] 서버 시작 오류: {ex.Message}");
        }
    }
}