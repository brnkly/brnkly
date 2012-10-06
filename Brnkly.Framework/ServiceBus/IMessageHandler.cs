
namespace Brnkly.Framework.ServiceBus
{
    public interface IMessageHandler<T>
    {
        /// <summary>
        /// Handles a message.
        /// </summary>
        /// <param name="context">A MessageHandlingContext(T) instance.</param>
        void Handle(MessageHandlingContext<T> context);
    }
}
