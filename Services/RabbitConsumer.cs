using System.Text;
using System.Text.Json;
using Rabbit.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Data.SqlClient;

namespace Rabbit.Services
{
    public class RabbitConsumer
    {
        public void Escuchar()
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

            EventingBasicConsumer consumer = new EventingBasicConsumer(channel);

            consumer.Received += (model, ea) =>
            {
                string mensaje = Encoding.UTF8.GetString(ea.Body.ToArray());
                RabbitModel usuario = JsonSerializer.Deserialize<RabbitModel>(mensaje);

                // 👇 SQL SERVER (FORMA EXPLÍCITA, SIN using var)
                SqlConnection conn = new SqlConnection(
                    "Server=.;Database=RabbitDB;Trusted_Connection=True;TrustServerCertificate=True;");

                conn.Open();

                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO Usuarios (Id, Nombre) VALUES (@Id, @Nombre)", conn);

                cmd.Parameters.AddWithValue("@Id", usuario.Id);
                cmd.Parameters.AddWithValue("@Nombre", usuario.Nombre);

                cmd.ExecuteNonQuery();

                conn.Close();
            };

            channel.BasicConsume("cola_usuarios", true, consumer);
        }
    }
}
