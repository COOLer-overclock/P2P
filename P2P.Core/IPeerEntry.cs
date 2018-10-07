using System.Net.PeerToPeer;
using System.ServiceModel;

namespace P2P.Core
{
    public interface IPeerEntry
    {
        string DisplayedName { get; }
        IP2PService ServiceProxy { get; }
    }

    public class PeerEntry : IPeerEntry
    {
        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="CommunicationException"></exception>
        /// <param name="serviceProxy"></param>
        public PeerEntry(IP2PService serviceProxy)
        {
            ServiceProxy = serviceProxy;
            DisplayedName = serviceProxy.GetUserName();
        }

        public string DisplayedName { get; }
        public IP2PService ServiceProxy { get; }
    }
}
