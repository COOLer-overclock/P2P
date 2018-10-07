using Newtonsoft.Json;
using NLog;
using P2P.Core;
using P2P.PeerClient.Models;
using System;
using System.Linq;
using System.ServiceModel;

namespace P2P.PeerClient
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    class P2PService : IP2PService
    {
        MainWindow _control;
        string _userName;
        ILogger _logger = LogManager.GetCurrentClassLogger();

        public P2PService(MainWindow control, string userName)
        {
            _control = control;
            if (string.IsNullOrEmpty(userName))
            {
                throw new ArgumentNullException($"{nameof(userName)} is null");
            }
            _userName = userName;
        }

        public string GetUserName()
        {
            return _userName;
        }

        public void SendMessage(string message)
        {
            var msg = JsonConvert.DeserializeObject<MessageOverNet>(message);
            var peers = _control.AvailablePeers;
            var peerFrom = peers.FirstOrDefault(x => x.PeerEntry.DisplayedName == msg.From);
            if (peerFrom == null)
                _logger.Warn($"Dead letter received. From {msg.From} to {GetUserName()}");

            var internalMessage = new Message(peerFrom.PeerEntry, msg.Content);
            peerFrom.Messages.Add(internalMessage);

            _control.OuputPeerMessages(peerFrom);
        }

        public override string ToString()
        {
            return $"userName = {GetUserName()}";
        }
    }
}
