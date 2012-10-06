
namespace Brnkly.Framework.ServiceBus.Core
{
    public enum BusEndpointType
    {
        Control,
        NonTx,
        Tx,
        TxDeadLetter
    }

    internal static class BusEndpointTypeExtensions
    {
        public static bool IsTransactional(this BusEndpointType busEndpointType)
        {
            if (busEndpointType == BusEndpointType.Control ||
                busEndpointType == BusEndpointType.Tx ||
                busEndpointType == BusEndpointType.TxDeadLetter)
            {
                return true;
            }

            return false;
        }
    }
}
