using P2P.Core;

namespace P2P.PeerClient.Models
{
    interface IMessage : IMessageContent
    {
        IPeerEntry From { get; }
    }

    class Message : IMessage
    {
        public IPeerEntry From { get; }
        public string Content { get; set; }

        public Message(IPeerEntry from, string content)
        {
            From = from as PeerEntry;
            Content = content;
        }
    }
}
