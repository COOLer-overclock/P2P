using System;
using System.ServiceModel;

namespace P2P.Client
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    class P2PService : IP2PService
    {
        private MainWindow _mainWindow;
        public string UserName { get; }

        public P2PService(MainWindow mainWindow, string userNames)
        {
            _mainWindow = mainWindow;
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
