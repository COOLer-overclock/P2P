using System.Collections.Generic;
using System.Linq;
using System.Net.PeerToPeer;

namespace P2P.Core
{
    public abstract class Peer
    {
        PeerName _peerName;
        PeerNameRegistration _pnRegistration;
        PeerNameResolver _pnResolver;

        public int Port { get; set; }
        public string PeerName { get; set; }
        public Peer(string name, int port)
        {
            _peerName = new PeerName(name, PeerNameType.Unsecured);
            _pnRegistration = new PeerNameRegistration(_peerName, port);

            _pnResolver = new PeerNameResolver();
        }

        protected void Connect()
        {
            _pnRegistration.Start();
        }

        protected void Disconnect()
        {
            _pnRegistration.Stop(); 
        }

        protected List<PeerNameRecord> GetAllPeers()
        {
            return _pnResolver.Resolve(_peerName, Cloud.AllLinkLocal).ToList();
        }

        protected List<PeerNameRecord> GetOtherPeers()
        {
            return GetAllPeers().Where(x => x.PeerName.Classifier != this.PeerName).ToList();
        }
    }
}
