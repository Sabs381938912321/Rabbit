namespace Rabbit.Models
{
    public class CargaArchivo
    {
        public int IdCarga { get; set; }
        public string NombreArchivo { get; set; }
        public int? TotalRegistros { get; set; }
        public int? RegistrosExitosos { get; set; }
        public int? RegistrosError { get; set; }
        public string Estado { get; set; }
        public DateTime FechaCarga { get; set; }
    }
}

