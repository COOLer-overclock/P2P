using P2P.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.PeerToPeer;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace P2P.Clientttt
{
    public class PeerEntry
    {
        public PeerName PeerName { get; set; }
        public IP2PService ServiceProxy { get; set; }
        public string DisplayString { get; set; }
        public bool ButtonsEnabled { get; set; }
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class P2PService : IP2PService
    {
        private MainWindow hostReference;
        private string username;

        public P2PService(MainWindow hostReference, string username)
        {
            this.hostReference = hostReference;
            this.username = username;
        }

        public string GetName()
        {
            return username;
        }

        public void SendMessage(string message, string from)
        {
            hostReference.DisplayMessage(message, from);
        }
    }
    [ServiceContract]
    public interface IP2PService
    {
        [OperationContract]
        string GetName();

        [OperationContract(IsOneWay = true)]
        void SendMessage(string message, string from);
    }
}
