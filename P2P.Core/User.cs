using System.Collections.Generic;
using System.Linq;

namespace P2P.Core
{
    public class User : Peer
    {
        public User(string name, int port) : base(name, port)
        {   }

        public List<string> GetOtherUsers()
        {
            return GetOtherPeers().Select(x => x.PeerName.Classifier).ToList();
        }
    }
}
