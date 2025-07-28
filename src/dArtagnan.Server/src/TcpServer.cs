using System.Net;
using System.Net.Sockets;
using dArtagnan.Shared;

namespace dArtagnan.Server;

/// <summary>
/// 메인 서버 클래스 - TCP와 UDP 소켓을 모두 관리
/// </summary>
public class TcpServer
{
    private TcpListener tcpListener = null!;
    private UdpClient udpListener = null!;
    private GameLoop gameLoop = null!;
    private GameManager gameManager = null!;
    private AdminConsole adminConsole = null!;
    private readonly int port;

    public TcpServer(int port)
    {
        this.port = port;
        gameManager = new GameManager();
        gameLoop = new GameLoop(gameManager);
        adminConsole = new AdminConsole(gameManager);
        
        // TCP 리스너 초기화
        tcpListener = new TcpListener(IPAddress.Any, port);
        tcpListener.Start();
        
        // UDP 리스너 초기화 (포트 + 1)
        udpListener = new UdpClient(port + 1);
        gameManager.UdpClient = udpListener;  // GameManager에서 사용할 수 있도록 설정
        
        // 서버 시작
        _ = Task.Run(() => StartTcpServerAsync(port));
        _ = Task.Run(() => StartUdpServerAsync(port + 1));
    }

    private async Task StartTcpServerAsync(int port)
    {
        try
        {
            Console.WriteLine($"D'Artagnan TCP 서버가 포트 {port}에서 클라이언트 연결을 기다리는 중...");
            while (true)
            {
                try
                {
                    var tcpClient = await tcpListener.AcceptTcpClientAsync();
                    tcpClient.NoDelay = true;
                    
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
            Console.WriteLine($"TCP 서버 시작 오류: {ex.Message}");
        }
    }

    private async Task StartUdpServerAsync(int port)
    {
        try
        {
            Console.WriteLine($"D'Artagnan UDP 서버가 포트 {port}에서 위치 패킷을 기다리는 중...");
            while (true)
            {
                try
                {
                    var result = await udpListener.ReceiveAsync();
                    var data = result.Buffer;
                    var clientEndpoint = result.RemoteEndPoint;
                    
                    // UDP 패킷 처리
                    _ = Task.Run(() => ProcessUdpPacket(data, clientEndpoint));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"UDP 패킷 수신 오류: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UDP 서버 시작 오류: {ex.Message}");
        }
    }

    private async Task ProcessUdpPacket(byte[] data, IPEndPoint clientEndpoint)
    {
        try
        {
            // UDP 패킷 역직렬화 및 처리
            var packet = NetworkUtils.DeserializeUdpPacket(data);
            
            if (packet is PlayerMovementDataFromClient movementData)
            {
                // 패킷에서 직접 PlayerId 가져오기
                var clientId = movementData.PlayerId;
                
                // UDP 엔드포인트 등록 (첫 패킷 시)
                if (!gameManager.ClientUdpEndpoints.ContainsKey(clientId))
                {
                    gameManager.ClientUdpEndpoints[clientId] = clientEndpoint;
                    Console.WriteLine($"[UDP] 클라이언트 {clientId} UDP 엔드포인트 등록: {clientEndpoint}");
                }
                
                var command = new PlayerMovementCommand
                {
                    PlayerId = clientId,
                    MovementData = movementData.MovementData,
                };
                
                await gameManager.EnqueueCommandAsync(command);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UDP 패킷 처리 오류: {ex.Message}");
        }
    }



    public async Task SendUdpPacketAsync(IPEndPoint clientEndpoint, byte[] data)
    {
        try
        {
            await udpListener.SendAsync(data, data.Length, clientEndpoint);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UDP 패킷 전송 오류: {ex.Message}");
        }
    }
}