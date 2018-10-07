using System.ServiceModel;

namespace P2P.Core
{
    public interface IPeerEntry
    {
        string Address { get; }
        string DisplayedName { get; }
        IP2PService ServiceProxy { get; }
    }

    public class PeerEntry : IPeerEntry
    {
        /// <summary>
        /// </summary>
        /// <exception cref="CommunicationException"></exception>
        /// <param name="serviceProxy"></param>
        public PeerEntry(IP2PService serviceProxy, string address)
        {
            ServiceProxy = serviceProxy;
            DisplayedName = serviceProxy.GetUserName();
            Address = address;
        }

        public string Address { get; }
        public string DisplayedName { get; }
        public IP2PService ServiceProxy { get; }

        public override string ToString()
        {
            return $"{nameof(DisplayedName)} = {DisplayedName}";
        }
    }
}
