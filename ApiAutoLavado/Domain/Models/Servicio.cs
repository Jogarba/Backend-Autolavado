namespace ApiAutoLavado.Domain.Models
{
    public class Servicio
    {
        public int Id { get; set; }

        public required string Nombre { get; set; }

        public decimal PrecioBase { get; set; }

        public int TiempoEstimadoMin { get; set; }

        /// <summary>
        /// Secuencia de fases del servicio (claves separadas por coma).
        /// Ej: "EN_COLA,ENJABONADO,ENJUAGADO,SECADO,LISTO".
        /// </summary>
        public string? Fases { get; set; }
    }
}
