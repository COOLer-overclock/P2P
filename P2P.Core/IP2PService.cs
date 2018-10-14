using System.ServiceModel;

namespace P2P.Core
{
    [ServiceContract]
    public interface IP2PService
    {
        [OperationContract]
        string GetUserName();

        [OperationContract(IsOneWay = true)]
        void SendMessage(string message);
        
        [OperationContract(IsOneWay = true)]
        void ExcludeFromList(string displayedName);
    }
}