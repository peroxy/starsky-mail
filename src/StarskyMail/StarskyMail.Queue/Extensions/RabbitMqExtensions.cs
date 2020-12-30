using RabbitMQ.Client;

namespace StarskyMail.Queue.Extensions
{
    public static class RabbitMqExtensions
    {
        public static IModel CreatePersistentChannel(this IConnection connection)
        {
            var channel = connection.CreateModel();
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            return channel;
        }
    }
}