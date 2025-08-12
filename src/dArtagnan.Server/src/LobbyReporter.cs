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
            
            Console.WriteLine($"[DEBUG] ReportState called - state: {state}, roomId: {roomId}, lobbyUrl: {lobbyUrl}");
            
            if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(lobbyUrl))
            {
                Console.WriteLine("[DEBUG] RoomId or LobbyUrl is null/empty - skipping report");
                return; // 로컬/개발 모드 또는 미설정
            }

            var url = $"{lobbyUrl.TrimEnd('/')}/internal/rooms/{roomId}/state";
            var payload = JsonSerializer.Serialize(new { state });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            Console.WriteLine($"[DEBUG] Sending HTTP POST to: {url}");
            Console.WriteLine($"[DEBUG] Payload: {payload}");

            var response = await http.PostAsync(url, content);
            Console.WriteLine($"[DEBUG] HTTP response: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[DEBUG] Response content: {responseContent}");
            }
            else
            {
                Console.WriteLine($"[DEBUG] HTTP request failed: {response.StatusCode}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"[DEBUG] Exception in ReportState: {e.Message}");
        }
    }
}


