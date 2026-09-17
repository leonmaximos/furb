using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace SocketChat
{
    public sealed class NodeManager : IDisposable
    {
        private readonly string _alias;
        private readonly int _port;
        private readonly ConcurrentDictionary<string, PeerConnection> _peers = new();
        private readonly ConcurrentDictionary<string, string> _peerEndpoints = new();
        private readonly CancellationTokenSource _cts = new();

        public NodeManager(string alias, int port)
        {
            _alias = alias;
            _port = port;
        }

        public async Task StartAsync(IEnumerable<string> initialPeers)
        {
            _ = ListenAsync();
            foreach (var peer in initialPeers)
            {
                _ = ConnectAsync(peer);
            }
        }

        private async Task ListenAsync()
        {
            try
            {
                using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listener.Bind(new IPEndPoint(IPAddress.Any, _port));
                listener.Listen(100);

                Console.WriteLine($"[Sistema] Escutando na porta {_port} como '{_alias}'.");

                while (!_cts.Token.IsCancellationRequested)
                {
                    var socket = await listener.AcceptAsync(_cts.Token);
                    socket.NoDelay = true;
                    SetupPeer(socket);
                }
            }
            catch { }
        }

        private async Task ConnectAsync(string endpoint)
        {
            try
            {
                var parts = endpoint.Split(':');
                if (parts.Length != 2 || !int.TryParse(parts[1], out var port)) 
                    return;

                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) 
                { 
                    NoDelay = true 
                };

                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
                timeout.CancelAfter(TimeSpan.FromSeconds(10));

                await socket.ConnectAsync(parts[0], port, timeout.Token);
                SetupPeer(socket);
            }
            catch { }
        }

        private void SetupPeer(Socket socket)
        {
            var peer = new PeerConnection(socket);
            peer.OnMessageReceived += HandleMessage;
            peer.OnDisconnected += HandleDisconnect;
            peer.Start();

            peer.Enqueue(new Message(MessageType.Handshake, _alias, _port.ToString()));
        }

        private void HandleMessage(PeerConnection peer, Message message)
        {
            switch (message.Type)
            {
                case MessageType.Handshake:
                    ProcessHandshake(peer, message);
                    break;
                case MessageType.Peers:
                    ProcessPeers(message.Content);
                    break;
                case MessageType.Broadcast:
                    Console.WriteLine($"{message.Sender}: {message.Content}");
                    break;
                case MessageType.Private:
                    Console.WriteLine($"[Privado de {message.Sender}]: {message.Content}");
                    break;
                case MessageType.Disconnect:
                    peer.Disconnect();
                    break;
            }
        }

        private void ProcessHandshake(PeerConnection peer, Message message)
        {
            var originalId = peer.NodeId;
            peer.SetNodeId(message.Sender);

            var ip = originalId.Split(':')[0];
            var peerListenEndpoint = $"{ip}:{message.Content}";
            _peerEndpoints[message.Sender] = peerListenEndpoint;

            if (_peers.TryAdd(message.Sender, peer))
            {
                Console.WriteLine($"[Sistema] {message.Sender} entrou.");
                
                var endpoints = string.Join(",", _peerEndpoints.Values);
                if (!string.IsNullOrEmpty(endpoints))
                {
                    peer.Enqueue(new Message(MessageType.Peers, _alias, endpoints));
                }
            }
            else
            {
                peer.Disconnect();
            }
        }

        private void ProcessPeers(string content)
        {
            var endpoints = content.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var known = _peerEndpoints.Values.ToHashSet();

            foreach (var ep in endpoints)
            {
                if (!known.Contains(ep))
                {
                    var localEp = $"127.0.0.1:{_port}";
                    if (ep != localEp && !ep.EndsWith($":{_port}"))
                    {
                        _ = ConnectAsync(ep);
                    }
                }
            }
        }

        private void HandleDisconnect(PeerConnection peer)
        {
            if (_peers.TryRemove(peer.NodeId, out _))
            {
                _peerEndpoints.TryRemove(peer.NodeId, out _);
                Console.WriteLine($"[Sistema] {peer.NodeId} saiu.");
            }
        }

        public void Broadcast(string content)
        {
            var message = new Message(MessageType.Broadcast, _alias, content);
            foreach (var peer in _peers.Values)
            {
                peer.Enqueue(message);
            }
        }

        public void SendPrivate(string target, string content)
        {
            if (_peers.TryGetValue(target, out var peer))
            {
                peer.Enqueue(new Message(MessageType.Private, _alias, content));
                Console.WriteLine($"[Privado para {target}]: {content}");
            }
            else
            {
                Console.WriteLine($"[Sistema] Participante '{target}' não encontrado.");
            }
        }

        public void PrintList()
        {
            Console.WriteLine("Participantes na conversa:");
            foreach (var p in _peers.Keys)
            {
                Console.WriteLine($"- {p}");
            }
        }

        public void Stop()
        {
            _cts.Cancel();
            var msg = new Message(MessageType.Disconnect, _alias, string.Empty);
            
            foreach (var peer in _peers.Values)
            {
                peer.Enqueue(msg);
                peer.Disconnect();
            }
            
            _peers.Clear();
            _peerEndpoints.Clear();
        }

        public void Dispose()
        {
            Stop();
            _cts.Dispose();
        }
    }
}