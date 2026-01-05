using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Rabbit.Models;

namespace Rabbit.Services
{
    public class RabbitProducer
    {
        public void Enviar(RabbitMessage mensaje)
        {
            var factory = new ConnectionFactory { HostName = "localhost" };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(
                queue: "cola_usuarios",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var body = Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(mensaje));

            channel.BasicPublish(
                exchange: "",
                routingKey: "cola_usuarios",
                basicProperties: null,
                body: body);
        }
    }

    public class RabbitMessage
    {
        public long Id { get; set; }
        public required string Nombre { get; set; }
        public int IdCarga { get; set; }
    }
}
