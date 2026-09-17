using System.Buffers.Binary;
using System.Net.Sockets;

namespace SocketChat
{
    public static class Frames
    {
        public const int MaxFrameSize = 64 * 1024;
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

        public static async Task WriteAsync(Socket socket, ReadOnlyMemory<byte> payload, CancellationToken ct = default)
        {
            if (payload.Length > MaxFrameSize)
                throw new ArgumentException($"Payload of {payload.Length} bytes exceeds the limit.");

            var header = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(header, payload.Length);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(Timeout);

            await SendAllAsync(socket, header, timeoutCts.Token);
            await SendAllAsync(socket, payload, timeoutCts.Token);
        }

        public static async Task<byte[]?> ReadAsync(Socket socket, CancellationToken ct = default)
        {
            var header = new byte[4];
            if (!await ReadExactlyAsync(socket, header, ct))
                return null;

            var size = BinaryPrimitives.ReadInt32BigEndian(header);
            if (size < 0 || size > MaxFrameSize)
                throw new InvalidDataException($"Invalid frame size: {size}");

            if (size == 0)
                return [];

            var payload = new byte[size];
            
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(Timeout);

            if (!await ReadExactlyAsync(socket, payload, timeoutCts.Token))
                throw new EndOfStreamException("Connection closed in the middle of a frame.");

            return payload;
        }

        private static async Task SendAllAsync(Socket socket, ReadOnlyMemory<byte> data, CancellationToken ct)
        {
            var sent = 0;
            while (sent < data.Length)
            {
                var n = await socket.SendAsync(data[sent..], SocketFlags.None, ct);
                if (n == 0)
                    throw new SocketException((int)SocketError.ConnectionReset);
                sent += n;
            }
        }

        private static async Task<bool> ReadExactlyAsync(Socket socket, Memory<byte> destination, CancellationToken ct)
        {
            var read = 0;
            while (read < destination.Length)
            {
                var n = await socket.ReceiveAsync(destination[read..], SocketFlags.None, ct);
                if (n == 0)
                {
                    if (read == 0)
                        return false;

                    throw new EndOfStreamException(
                        $"Missing {destination.Length - read} bytes of the frame.");
                }
                read += n;
            }
            return true;
        }
    }
}