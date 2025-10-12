using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace dArtagnan.Server;

public static class LobbyReporter
{
    private static readonly HttpClient http = new HttpClient();

    public static async void ReportState(int state)
    {
        try
        {
            var roomId = Environment.GetEnvironmentVariable("ROOM_ID");
            var lobbyUrl = Environment.GetEnvironmentVariable("LOBBY_URL");

            Logger.log($"[Lobby][System] 상태 리포트 호출: state={state}, roomId={roomId?.Substring(0, Math.Min(6, roomId.Length))}");

            if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(lobbyUrl))
            {
                Logger.log("[Lobby][System] 환경 변수 없음: 리포트 스킵 (로컬 모드)");
                return; // 로컬/개발 모드 또는 미설정
            }

            var url = $"{lobbyUrl.TrimEnd('/')}";
            var payload = JsonSerializer.Serialize(new { state });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await http.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                Logger.log($"[Lobby][System] 상태 리포트 성공: {response.StatusCode}");
            }
            else
            {
                Logger.log($"[Lobby][System] 상태 리포트 실패: {response.StatusCode}");
            }
        }
        catch (Exception e)
        {
            Logger.log($"[Lobby][System] 상태 리포트 예외: {e.Message}");
        }
    }

    public static async void ReportPlayerCount(int playerCount)
    {
        try
        {
            var roomId = Environment.GetEnvironmentVariable("ROOM_ID");
            var lobbyUrl = Environment.GetEnvironmentVariable("LOBBY_URL");

            Logger.log($"[Lobby][System] 인원수 리포트 호출: {playerCount}명");

            if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(lobbyUrl))
            {
                Logger.log("[Lobby][System] 환경 변수 없음: 리포트 스킵 (로컬 모드)");
                return; // 로컬/개발 모드 또는 미설정
            }

            var url = $"{lobbyUrl.TrimEnd('/')}";
            var payload = JsonSerializer.Serialize(new { playerCount });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            var response = await http.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                Logger.log($"[Lobby][System] 인원수 리포트 성공: {response.StatusCode}");
            }
            else
            {
                Logger.log($"[Lobby][System] 인원수 리포트 실패: {response.StatusCode}");
            }
        }
        catch (Exception e)
        {
            Logger.log($"[Lobby][System] 인원수 리포트 예외: {e.Message}");
        }
    }

    // [REMOVED FOR PORTFOLIO]
}


