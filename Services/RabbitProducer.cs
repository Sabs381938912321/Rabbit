using System.Text;
using System.Text.Json;
using Rabbit.Models;
using RabbitMQ.Client;

namespace Rabbit.Services
{
    public class RabbitProducer
    {
        public void Enviar(RabbitModel usuario)
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.HostName = "localhost";

            IConnection connection = factory.CreateConnection();
            IModel channel = connection.CreateModel();

            channel.QueueDeclare(
                "cola_usuarios",
                false,
                false,
                false,
                null);

            string mensaje = JsonSerializer.Serialize(usuario);

            ReadOnlyMemory<byte> body =
                new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(mensaje));

            channel.BasicPublish(
                exchange: "",
                routingKey: "cola_usuarios",
                mandatory: false,
                basicProperties: null,
                body: body);
        }
    }
}
