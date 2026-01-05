using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Rabbit.Models;
using Rabbit.Services;

namespace Rabbit.Controllers
{
    public class RabbitController : Controller
    {
        private readonly IConfiguration _configuration;

        public RabbitController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Vista
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Cargas()
        {
            List<CargaArchivo> cargas = new List<CargaArchivo>();

            string cs = _configuration.GetConnectionString("DefaultConnection");

            using SqlConnection conn = new SqlConnection(cs);
            conn.Open();

            using SqlCommand cmd = new SqlCommand(
                "SELECT * FROM CargaArchivos ORDER BY FechaCarga DESC", conn);

            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                cargas.Add(new CargaArchivo
                {
                    IdCarga = (int)reader["IdCarga"],
                    NombreArchivo = reader["NombreArchivo"].ToString(),
                    TotalRegistros = reader["TotalRegistros"] as int?,
                    RegistrosExitosos = reader["RegistrosExitosos"] as int?,
                    RegistrosError = reader["RegistrosError"] as int?,
                    Estado = reader["Estado"].ToString(),
                    FechaCarga = (DateTime)reader["FechaCarga"]
                });
            }

            return View(cargas); 
        }

        [HttpPost]
        public IActionResult EnviarArchivo(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                ViewBag.Mensaje = "Archivo vacío";
                return View("Index");
            }

            string cs = _configuration.GetConnectionString("DefaultConnection");
            int idCarga;

            // Registrar archivo
            using (var conn = new SqlConnection(cs))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
            INSERT INTO CargaArchivos (NombreArchivo, Estado)
            OUTPUT INSERTED.IdCarga
            VALUES (@Nombre, 'Procesando')", conn);

                cmd.Parameters.AddWithValue("@Nombre", archivo.FileName);
                idCarga = (int)cmd.ExecuteScalar();
            }

            var producer = new RabbitProducer();

            using var reader = new StreamReader(archivo.OpenReadStream());

            while (!reader.EndOfStream)
            {
                var linea = reader.ReadLine();
                var partes = linea.Split(',');

                if (partes.Length >= 2 && int.TryParse(partes[0], out int id))
                {
                    producer.Enviar(new RabbitMessage
                    {
                        IdCarga = idCarga, 
                        Id = id,
                        Nombre = partes[1]
                    });
                }
            }

            TempData["Mensaje"] = $"Archivo enviado correctamente. IdCarga: {idCarga}";
            return RedirectToAction("Index");

        }

    }
}
