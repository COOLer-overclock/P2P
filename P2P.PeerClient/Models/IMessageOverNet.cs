using Newtonsoft.Json;
using P2P.Core;

namespace P2P.PeerClient.Models
{
    interface IMessageOverNet : IMessageContent
    {
        string From { get; set; }
    }

    class MessageOverNet : IMessageOverNet
    {
        public string From { get; set; }
        public string Content { get; set; }

        public MessageOverNet(string from, string сontent)
        {
            Content = сontent;
            From = from;
        }

        public MessageOverNet(IPeerEntry peerEntry, string content) : this (peerEntry.DisplayedName, content)
        {  }

        public MessageOverNet()
        {  }
    }
}
