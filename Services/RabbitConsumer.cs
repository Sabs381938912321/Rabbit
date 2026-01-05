using System.Text;
using System.Text.Json;
using Rabbit.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Data.SqlClient;

namespace Rabbit.Services
{
    public class RabbitConsumer : BackgroundService
    {
        private readonly string cs =
            "Server=LAPTOP-N92006A9;Database=RabbitDB;Trusted_Connection=True;TrustServerCertificate=True;";

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            channel.QueueDeclare("cola_usuarios", false, false, false, null);

            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += (model, ea) =>
            {
                RabbitModel usuario = null;

                try
                {
                    var mensaje = Encoding.UTF8.GetString(ea.Body.ToArray());
                    usuario = JsonSerializer.Deserialize<RabbitModel>(mensaje);

                    using var conn = new SqlConnection(cs);
                    conn.Open();

                    using var cmd = new SqlCommand(
                        "INSERT INTO Usuarios (Id, Nombre) VALUES (@Id, @Nombre)", conn);

                    cmd.Parameters.AddWithValue("@Id", usuario.Id);
                    cmd.Parameters.AddWithValue("@Nombre", usuario.Nombre);

                    cmd.ExecuteNonQuery();

                    ActualizarCarga(usuario.IdCarga, exitoso: true);
                }
                catch (Exception ex)
                {
                    GuardarErrorCSV(usuario, ex.Message);
                    ActualizarCarga(usuario?.IdCarga ?? 0, exitoso: false);
                }
            };

            channel.BasicConsume("cola_usuarios", true, consumer);
            return Task.CompletedTask;
        }

        // 🔹 Actualiza contadores
        private void ActualizarCarga(int idCarga, bool exitoso)
        {
            if (idCarga == 0) return;

            using var conn = new SqlConnection(cs);
            conn.Open();

            var sql = exitoso
                ? "UPDATE CargaArchivos SET RegistrosExitosos = ISNULL(RegistrosExitosos,0)+1 WHERE IdCarga=@Id"
                : "UPDATE CargaArchivos SET RegistrosError = ISNULL(RegistrosError,0)+1 WHERE IdCarga=@Id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", idCarga);
            cmd.ExecuteNonQuery();

            VerificarFinProceso(idCarga, conn);
        }

        // 🔹 Verifica si terminó
        private void VerificarFinProceso(int idCarga, SqlConnection conn)
        {
            var cmd = new SqlCommand(@"
                UPDATE CargaArchivos
                SET Estado = CASE 
                    WHEN RegistrosError > 0 THEN 'ConErrores'
                    ELSE 'Exitoso'
                END
                WHERE IdCarga = @Id
                AND (ISNULL(RegistrosExitosos,0) + ISNULL(RegistrosError,0)) >= TotalRegistros
            ", conn);

            cmd.Parameters.AddWithValue("@Id", idCarga);
            cmd.ExecuteNonQuery();
        }

        // 🔹 CSV POR CARGA (ESTO ES LO QUE QUERÍAS)
        private void GuardarErrorCSV(RabbitModel usuario, string error)
        {
            string carpeta = @"C:\RabbitErrores\";

            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);

            string ruta = Path.Combine(
                carpeta,
                $"errores_carga_{usuario?.IdCarga}.csv");

            bool existe = File.Exists(ruta);

            using var writer = new StreamWriter(ruta, true, Encoding.UTF8);

            if (!existe)
                writer.WriteLine("Documento,Nombre,Error,Fecha");

            writer.WriteLine(
                $"{usuario?.Id},{usuario?.Nombre},\"{error}\",{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        }
    }
}
