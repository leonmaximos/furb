## Run

Terminal 1:

    dotnet run -- 9000 alice

Terminal 2:

    dotnet run -- 9001 bob 127.0.0.1:9000

Terminal 3:

    dotnet run -- 9002 charlie 127.0.0.1:9000
OU

    dotnet run -- 9002 charlie 127.0.0.1:9001
OU

    dotnet run -- 9002 charlie 127.0.0.1:9000 127.0.0.1:9001

## Files

| File | Role |
|---|---|
| `Program.cs` | Leitura de argumentos, inicialização do sistema e loop de interação com o terminal. |
| `NodeManager.cs` | Gerenciamento da malha P2P, propagação de contatos e roteamento de mensagens. |
| `PeerConnection.cs` | Gerenciamento de uma conexão individual, controlando leitura assíncrona e fila de envio. |
| `Message.cs` | Definição da estrutura do pacote, serialização e desserialização de mensagens. |
| `Frames.cs` | Delimitação de mensagens via rede usando prefixo de tamanho em bytes. |

## Documentação da política adotada

Para lidar com nós que param de consumir mensagens, o sistema utiliza uma política de enfileiramento com limite rígido seguido de desconexão. Cada conexão possui um canal próprio que armazena até 100 mensagens pendentes. Se um participante travar ou ficar lento demais, essa fila encherá rapidamente. Quando o limite é atingido, o envio interno falha e o sistema corta a conexão com esse nó específico, protegendo a memória da aplicação e evitando que a lentidão de um usuário trave a conversa dos demais.

## Notes

Requires .NET 8 or later. Not compiled in the environment where it was generated; run `dotnet build` first.