using System.Text;

namespace SocketChat
{
    public enum MessageType
    {
        Handshake,
        Peers,
        Broadcast,
        Private,
        Disconnect
    }

    public record Message(MessageType Type, string Sender, string Content)
    {
        public byte[] Serialize()
        {
            var text = $"{(int)Type}|{Sender}|{Content}";
            return Encoding.UTF8.GetBytes(text);
        }

        public static Message? Deserialize(byte[] payload)
        {
            var text = Encoding.UTF8.GetString(payload);
            var parts = text.Split('|', 3);
            
            if (parts.Length != 3) 
                return null;
                
            if (!Enum.TryParse<MessageType>(parts[0], out var type)) 
                return null;

            return new Message(type, parts[1], parts[2]);
        }
    }
}