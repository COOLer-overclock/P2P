using System.ServiceModel;

namespace P2P.Core
{
    [ServiceContract]
    public interface IP2PService
    {
        [OperationContract]
        string GetUserName();

        [OperationContract]
        void SendMessage(string message);
    }
}
