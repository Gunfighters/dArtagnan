using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using MessagePack;

namespace dArtagnan.Shared
{
    public static class NetworkUtils
    {
        // 패킷 전송
        public static async Task SendPacketAsync(NetworkStream stream, IPacket packet)
        {
            var data = MessagePackSerializer.Serialize(packet);
            var size = BitConverter.GetBytes(data.Length);

            await stream.WriteAsync(size.AsMemory(0, 4));
            await stream.WriteAsync(data);
            await stream.FlushAsync();
        }

        public static void SendPacketSync(NetworkStream stream, IPacket packet)
        {
            var data = MessagePackSerializer.Serialize(packet);
            var size = BitConverter.GetBytes(data.Length);

            stream.Write(size, 0, 4);
            stream.Write(data, 0, data.Length);
            stream.Flush();
        }

        public static IPacket ReceivePacketSync(NetworkStream stream)
        {
            // 1. 패킷 크기(4바이트) 완전히 읽기
            var lengthBuffer = new byte[4];
            var totalBytesRead = 0;

            // 4바이트가 모두 올 때까지 반복
            while (totalBytesRead < 4)
            {
                var bytesRead = stream.Read(lengthBuffer.AsSpan(totalBytesRead, 4 - totalBytesRead));
                if (bytesRead == 0) throw new Exception("connection closed");
                totalBytesRead += bytesRead;
            }

            var packetLength = BitConverter.ToInt32(lengthBuffer, 0);
            if (packetLength <= 0 || packetLength > 1024 * 1024)
                throw new Exception($"invalid packet length: {packetLength}");

            // 2. 패킷 데이터도 완전히 읽기
            var packetBuffer = new byte[packetLength];
            totalBytesRead = 0;

            while (totalBytesRead < packetLength)
            {
                var bytesRead = stream.Read(packetBuffer.AsSpan(totalBytesRead, packetLength - totalBytesRead));
                if (bytesRead == 0) throw new Exception("part of packet didn't arrive.");
                totalBytesRead += bytesRead;
            }

            return MessagePackSerializer.Deserialize<IPacket>(packetBuffer);
        }

        // 패킷 수신
        public static async Task<IPacket> ReceivePacketAsync(NetworkStream stream)
        {
            // 1. 패킷 크기(4바이트) 완전히 읽기
            var lengthBuffer = new byte[4];
            var totalBytesRead = 0;

            // 4바이트가 모두 올 때까지 반복
            while (totalBytesRead < 4)
            {
                var bytesRead = await stream.ReadAsync(lengthBuffer.AsMemory(totalBytesRead, 4 - totalBytesRead));
                if (bytesRead == 0) throw new Exception("connection closed");
                totalBytesRead += bytesRead;
            }

            var packetLength = BitConverter.ToInt32(lengthBuffer, 0);
            if (packetLength <= 0 || packetLength > 1024 * 1024)
                throw new Exception($"invalid packet length: {packetLength}");

            // 2. 패킷 데이터도 완전히 읽기
            var packetBuffer = new byte[packetLength];
            totalBytesRead = 0;

            while (totalBytesRead < packetLength)
            {
                var bytesRead =
                    await stream.ReadAsync(packetBuffer.AsMemory(totalBytesRead, packetLength - totalBytesRead));
                if (bytesRead == 0) throw new Exception("part of packet didn't arrive.");
                totalBytesRead += bytesRead;
            }

            return MessagePackSerializer.Deserialize<IPacket>(packetBuffer);
        }
    }
}