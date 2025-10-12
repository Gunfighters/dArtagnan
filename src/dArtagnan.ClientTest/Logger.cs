using System;
using System.Linq;
using System.Reflection;
using dArtagnan.Shared;

namespace dArtagnan.ClientTest;

/// <summary>
/// 로거
/// </summary>
public static class Logger
{
    /// <summary>
    /// 현재 시각 프리픽스(예: [34:56.789])를 붙여 한 줄을 출력합니다.
    /// </summary>
    /// <param name="message">출력할 메시지</param>
    public static void log(string message = "")
    {
        var time = DateTime.Now.ToString("mm:ss.fff");
        Console.WriteLine($"[{time}] {message}");
    }

    /// <summary>
    /// 패킷 정보를 타입과 내용물로 나누어 출력합니다.
    /// </summary>
    /// <param name="direction">방향 (⬅️ 또는 ➡️)</param>
    /// <param name="packet">패킷</param>
    public static void log(string direction, IPacket packet)
    {
        var time = DateTime.Now.ToString("mm:ss.fff");
        Console.WriteLine($"[{time}][패킷 {direction} ] 패킷: {packet.GetType().Name}");
        
        try
        {
            var fields = packet.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            if (fields.Length == 0)
            {
                Console.WriteLine($"[{time}]   내용: (빈 패킷)");
            }
            else
            {
                foreach (var field in fields)
                {
                    var value = field.GetValue(packet);
                    PrintFieldValue(time, field.Name, value, "  ");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{time}]   내용: [필드 읽기 실패: {ex.Message}]");
        }
    }

    private static void PrintFieldValue(string time, string fieldName, object? value, string indent)
    {
        if (value == null)
        {
            Console.WriteLine($"[{time}] {indent}{fieldName}: null");
            return;
        }

        var type = value.GetType();

        // 기본 타입들은 바로 출력
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
        {
            Console.WriteLine($"[{time}] {indent}{fieldName}: {value}");
        }
        // 배열이나 리스트인 경우
        else if (value is System.Collections.IEnumerable enumerable && type != typeof(string))
        {
            var list = enumerable.Cast<object>().ToList();
            Console.WriteLine($"[{time}] {indent}{fieldName}: [{list.Count}개 항목]");
            
            int index = 0;
            foreach (var item in list)
            {
                PrintFieldValue(time, $"[{index}]", item, indent + "  ");
                index++;
            }
        }
        // 구조체나 클래스인 경우 (재귀적으로 분해)
        else if (type.IsValueType || type.IsClass)
        {
            Console.WriteLine($"[{time}] {indent}{fieldName}: {type.Name}");
            
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                var fieldValue = field.GetValue(value);
                PrintFieldValue(time, field.Name, fieldValue, indent + "  ");
            }
        }
        else
        {
            Console.WriteLine($"[{time}] {indent}{fieldName}: {value}");
        }
    }
}