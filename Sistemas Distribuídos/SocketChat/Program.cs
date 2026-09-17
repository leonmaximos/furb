using SocketChat;

if (args.Length < 2)
{
    Console.WriteLine("Uso: dotnet run -- <porta> <apelido> [peer1:porta] [peer2:porta] ...");
    return 1;
}

if (!int.TryParse(args[0], out var port))
{
    Console.WriteLine("Porta inválida.");
    return 1;
}

var alias = args[1];
var initialPeers = args.Skip(2).ToArray();

using var nodeManager = new NodeManager(alias, port);
await nodeManager.StartAsync(initialPeers);

Console.WriteLine("Comandos disponíveis: /list, /msg <apelido> <texto>, /quit");
Console.WriteLine("Digite sua mensagem e pressione Enter:");
Console.WriteLine();

while (true)
{
    var line = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(line)) 
        continue;

    if (line.Equals("/quit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    if (line.Equals("/list", StringComparison.OrdinalIgnoreCase))
    {
        nodeManager.PrintList();
        continue;
    }

    if (line.StartsWith("/msg ", StringComparison.OrdinalIgnoreCase))
    {
        var parts = line.Split(' ', 3);
        if (parts.Length == 3)
        {
            nodeManager.SendPrivate(parts[1], parts[2]);
        }
        else
        {
            Console.WriteLine("[Sistema] Uso correto: /msg <apelido> <mensagem>");
        }
        continue;
    }

    if (line.Equals("/spam", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Teste");
        var payloadPesado = new string('X', 50000);
        
        for (int i = 0; i < 150; i++)
        {
            nodeManager.Broadcast($"Spam {i} {payloadPesado}");
            await Task.Delay(20);
        }
        continue;
    }

    nodeManager.Broadcast(line);
}

nodeManager.Stop();
Console.WriteLine("Sessão encerrada.");
return 0;