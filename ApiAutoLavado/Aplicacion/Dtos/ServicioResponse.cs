using System.Collections.Generic;

namespace ApiAutoLavado.Aplicacion.Dtos
{
    public class ServicioResponse
    {
        public int Id { get; set; }

        public required string Nombre { get; set; }

        public decimal PrecioBase { get; set; }

        public int TiempoEstimadoMin { get; set; }

        /// <summary>Secuencia de fases del servicio (RN-05).</summary>
        public List<string> Fases { get; set; } = new();
    }
}
