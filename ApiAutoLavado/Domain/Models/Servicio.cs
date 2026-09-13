namespace ApiAutoLavado.Domain.Models
{
    public class Servicio
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public required string Nombre { get; set; }

        public decimal PrecioBase { get; set; }

        public int TiempoEstimadoMin { get; set; }
    }
}
