using System.Net.Sockets;
using System.Threading.Channels;

namespace SocketChat
{
    public sealed class PeerConnection : IDisposable
    {
        private readonly Socket _socket;
        private readonly Channel<Message> _sendQueue;
        private readonly CancellationTokenSource _cts;
        
        public string NodeId { get; private set; }
        public bool IsConnected => _socket.Connected && !_cts.IsCancellationRequested;

        public event Action<PeerConnection, Message>? OnMessageReceived;
        public event Action<PeerConnection>? OnDisconnected;

        public PeerConnection(Socket socket)
        {
            _socket = socket;
            NodeId = socket.RemoteEndPoint?.ToString() ?? Guid.NewGuid().ToString();
            _cts = new CancellationTokenSource();
            
            var options = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _sendQueue = Channel.CreateBounded<Message>(options);
        }

        public void SetNodeId(string id)
        {
            NodeId = id;
        }

        public void Start()
        {
            _ = ReceiveLoopAsync();
            _ = SendLoopAsync();
        }

        public void Enqueue(Message message)
        {
            if (!IsConnected) 
                return;
            
            if (!_sendQueue.Writer.TryWrite(message))
            {
                Disconnect();
            }
        }

        private async Task ReceiveLoopAsync()
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    var payload = await Frames.ReadAsync(_socket, _cts.Token);
                    if (payload == null) break;

                    var message = Message.Deserialize(payload);
                    if (message != null)
                    {
                        OnMessageReceived?.Invoke(this, message);
                    }
                }
            }
            catch
            {
            }
            finally
            {
                Disconnect();
            }
        }

        private async Task SendLoopAsync()
        {
            try
            {
                await foreach (var message in _sendQueue.Reader.ReadAllAsync(_cts.Token))
            {
                    var payload = message.Serialize();
                    await Frames.WriteAsync(_socket, payload, _cts.Token);
                }
            }
            catch
            {
            }
            finally
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (_cts.IsCancellationRequested) 
                return;
                
            _cts.Cancel();

            try { _socket.Shutdown(SocketShutdown.Both); } catch { }
            try { _socket.Close(); } catch { }
            
            OnDisconnected?.Invoke(this);
        }

        public void Dispose()
        {
            Disconnect();
            _cts.Dispose();
        }
    }
}