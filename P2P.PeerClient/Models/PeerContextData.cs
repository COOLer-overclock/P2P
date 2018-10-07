using P2P.Core;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Controls;

namespace P2P.PeerClient.Models
{
    sealed class PeerContextData
    {
        public Button PeerButton { get; }
        public IPeerEntry PeerEntry { get; }
        public MessageHistory Messages { get; }
        public string Address { get; set; }

        private PeerContextData()
        {
            Messages = new MessageHistory();
        }

        public PeerContextData(IPeerEntry peerEntry, Button peerButton) : this()
        {
            PeerEntry = peerEntry;
            PeerButton = peerButton;
        }
    }

    class MessageHistory : IEnumerable<IMessage>
    {
        private List<IMessage> _messages = new List<IMessage>();

        public void Add(IMessage message)
        {
            _messages.Add(message);
        }

        public MessageHistory()
        {
            _messages = new List<IMessage>();
        }

        #region IEnumerable

        public IEnumerator<IMessage> GetEnumerator()
        {
            return _messages.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
    }
}
