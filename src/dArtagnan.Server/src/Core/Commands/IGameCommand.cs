namespace dArtagnan.Server;

/// <summary>
/// 게임 명령 인터페이스 - 모든 게임 상태 변경 명령의 기본 인터페이스
/// </summary>
public interface IGameCommand
{
    /// <summary>
    /// 명령을 실행합니다. 모든 게임 상태 변경은 이 메서드 내에서 이루어집니다.
    /// Internal: admin, tcp연결 해제, gameloop 등 tcp 연결 패킷이 아닌 멀티쓰레드에서 게임상태에 영향을 줄때 발행되는 커맨드들
    /// Protocol: 연결된 클라이언트에서 tcp 패킷으로 발행되는 커맨드들
    /// </summary>
    /// <param name="gameManager">게임 매니저 인스턴스</param>
    /// <returns>비동기 작업</returns>
    Task ExecuteAsync(GameManager gameManager);
} 