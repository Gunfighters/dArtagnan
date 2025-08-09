using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace dArtagnan.Server;

public static class LobbyReporter
{
    private static readonly HttpClient http = new HttpClient();

    public static void ReportState(int state)
    {
        try
        {
            var roomId = Environment.GetEnvironmentVariable("ROOM_ID");
            var lobbyUrl = Environment.GetEnvironmentVariable("LOBBY_URL");
            if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(lobbyUrl))
            {
                return; // 로컬/개발 모드 또는 미설정
            }

            var url = $"{lobbyUrl.TrimEnd('/')}/internal/rooms/{roomId}/state";
            var payload = JsonSerializer.Serialize(new { state });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            _ = http.PostAsync(url, content); // fire-and-forget
        }
        catch
        {
            // 실패 무시
        }
    }
}


