using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using MessagePack;

namespace dArtagnan.Shared
{
    public static class NetworkUtils
    {
        // 패킷 전송
        public static async Task SendPacketAsync(NetworkStream stream, Packet packet)
        {
            var data = MessagePackSerializer.Serialize(packet);
            var size = BitConverter.GetBytes(data.Length);
            
            await stream.WriteAsync(size, 0, 4);
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();
        }

        // 패킷 수신
        public static async Task<Packet?> ReceivePacketAsync(NetworkStream stream)
        {
            // 1. 패킷 크기(4바이트) 완전히 읽기
            byte[] lengthBuffer = new byte[4];
            int totalBytesRead = 0;
            
            // 4바이트가 모두 올 때까지 반복
            while (totalBytesRead < 4)
            {
                int bytesRead = await stream.ReadAsync(lengthBuffer, totalBytesRead, 4 - totalBytesRead);
                if (bytesRead == 0) return null; // 연결 끊어짐
                totalBytesRead += bytesRead;
            }
            
            int packetLength = BitConverter.ToInt32(lengthBuffer, 0);
            if (packetLength <= 0 || packetLength > 1024 * 1024) return null;
            
            // 2. 패킷 데이터도 완전히 읽기
            byte[] packetBuffer = new byte[packetLength];
            totalBytesRead = 0;
            
            while (totalBytesRead < packetLength)
            {
                int bytesRead = await stream.ReadAsync(packetBuffer, totalBytesRead, 
                    packetLength - totalBytesRead);
                if (bytesRead == 0) return null;
                totalBytesRead += bytesRead;
            }
            
            return MessagePackSerializer.Deserialize<Packet>(packetBuffer);
        }

        // 구조체를 패킷으로 변환
        public static Packet CreatePacket<T>(PacketType type, T data) where T : struct
        {
            var serializedData = MessagePackSerializer.Serialize(data);
            return new Packet(type, serializedData);
        }

        // 패킷에서 구조체 추출
        public static T GetData<T>(Packet packet) where T : struct
        {
            return MessagePackSerializer.Deserialize<T>(packet.Data);
        }
    }
} 