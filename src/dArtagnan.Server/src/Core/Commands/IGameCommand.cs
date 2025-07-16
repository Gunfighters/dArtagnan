namespace dArtagnan.Server;

/// <summary>
/// 게임 명령 인터페이스 - 모든 게임 상태 변경 명령의 기본 인터페이스
/// </summary>
public interface IGameCommand
{
    /// <summary>
    /// 명령을 실행합니다. 모든 게임 상태 변경은 이 메서드 내에서 이루어집니다.
    /// </summary>
    /// <param name="gameManager">게임 매니저 인스턴스</param>
    /// <returns>비동기 작업</returns>
    Task ExecuteAsync(GameManager gameManager);
} 