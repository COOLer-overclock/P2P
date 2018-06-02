using System;
using System.ServiceModel;

namespace P2P.Client
{
    class P2PService : IP2PService
    {
        private MainWindow _mainWindow;
        public string UserName { get; }
        public Guid UserId { get; }

        public P2PService(MainWindow mainWindow, string userName, Guid userId)
        {
            _mainWindow = mainWindow;
            UserName = userName;
            UserId = userId;
        }

        public void SendMessage(string message, string from)
        {
            throw new NotImplementedException();
        }

        public string GetName()
        {
            throw new NotImplementedException();
        }
    }

    [ServiceContract]
    interface IP2PService
    {
        [OperationContract]
        void SendMessage(string message, string from);
        string GetName();
    }
}
