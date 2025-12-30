using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Rabbit.Models;

namespace Rabbit.Controllers
{
    public class RabbitController : Controller
    {
        private readonly IConfiguration _configuration;

        public RabbitController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Muestra la vista
        public IActionResult Index()
        {
            return View();
        }

        // Recibe el archivo .txt
        [HttpPost]
        public IActionResult EnviarArchivo(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                ViewBag.Mensaje = "No se seleccionó ningún archivo.";
                return View("Index");
            }

            string cs = _configuration.GetConnectionString("DefaultConnection");
            int lineasProcesadas = 0;

            using (var conn = new SqlConnection(cs))
            {
                conn.Open();

                using (var reader = new StreamReader(archivo.OpenReadStream()))
                {
                    while (!reader.EndOfStream)
                    {
                        var linea = reader.ReadLine(); // cada línea: "Id,Nombre"
                        var partes = linea.Split(',');

                        if (partes.Length >= 2)
                        {
                            if (int.TryParse(partes[0], out int id))
                            {
                                string nombre = partes[1];

                                using var cmd = new SqlCommand(
                                    "INSERT INTO Usuarios (Id, Nombre) VALUES (@Id, @Nombre)", conn);

                                cmd.Parameters.AddWithValue("@Id", id);
                                cmd.Parameters.AddWithValue("@Nombre", nombre);

                                cmd.ExecuteNonQuery();
                                lineasProcesadas++;
                            }
                        }
                    }
                }
            }

            ViewBag.Mensaje = $"{lineasProcesadas} usuarios insertados correctamente desde el archivo.";

            return View("Index");
        }
    }
}
