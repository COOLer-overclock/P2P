using P2P.Core;
using System;
using System.ServiceModel;

namespace P2P.PeerClient
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    class P2PService : IP2PService
    {
        MainWindow _control;
        string _userName;

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
        }
    }
}
