using System;
using System.IO;

namespace dArtagnan.Server;

/// <summary>
/// 로거
/// </summary>
public static class Logger
{
	private static readonly string? LogFilePath;

	static Logger()
	{
		// 환경변수에서 방 정보 읽기
		var roomName = Environment.GetEnvironmentVariable("ROOM_NAME");
		var roomId = Environment.GetEnvironmentVariable("ROOM_ID");

		// ROOM_NAME이나 ROOM_ID가 없으면 로그 파일을 생성하지 않음 (콘솔만 사용)
		if (!string.IsNullOrWhiteSpace(roomName) && !string.IsNullOrWhiteSpace(roomId))
		{
			// Docker 컨테이너에서 /app/logs에 마운트됨
			var baseLogDir = "/app/logs";

			// 현재 시간 (TZ=Asia/Seoul 설정으로 자동으로 한국 시간)
			var now = DateTime.Now;

			// 날짜별 폴더 (예: 2025-10-12)
			var dateFolder = now.ToString("yyyy-MM-dd");
			var logDir = Path.Combine(baseLogDir, dateFolder);

			// 로그 파일명 (예: 12-34-56_방제목_roomId.log)
			var timeStamp = now.ToString("HH-mm-ss");
			var logFileName = $"{timeStamp}_{roomName}_{roomId}.log";

			LogFilePath = Path.Combine(logDir, logFileName);

			// 디렉토리가 없으면 생성
			try
			{
				if (!Directory.Exists(logDir))
				{
					Directory.CreateDirectory(logDir);
				}
			}
			catch
			{
				// 디렉토리 생성 실패 시 무시 (콘솔 로그만 사용)
			}
		}
	}

	/// <summary>
	/// 현재 시각 프리픽스(예: [34:56.789])를 붙여 한 줄을 출력합니다.
	/// </summary>
	/// <param name="message">출력할 메시지</param>
	public static void log(string message = "")
	{
		var time = DateTime.Now.ToString("HH:mm:ss.fff");
		var logLine = $"[{time}] {message}";

		// 콘솔에 출력
		Console.WriteLine(logLine);

		// 파일에 저장
		if (!string.IsNullOrWhiteSpace(LogFilePath))
		{
			try
			{
				File.AppendAllText(LogFilePath, logLine + "\n");
			}
			catch
			{
				// 파일 쓰기 실패 시 무시 (콘솔 로그는 이미 출력됨)
			}
		}
	}
}


