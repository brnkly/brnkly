using System.ServiceModel;

namespace Brnkly.Framework.ServiceBus.Wcf
{
    [ServiceContract(Namespace = "http://Brnkly/ServiceBus/2009/11")]
    public interface IBusReceiver
    {
        [OperationContract(IsOneWay = true)]
        void Receive(TransportMessage transportMessage);

        [OperationContract(IsOneWay = true)]
        void ReceiveInTransaction(TransportMessage transportMessage);
    }
}
