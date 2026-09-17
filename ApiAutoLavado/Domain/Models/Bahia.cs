using ApiAutoLavado.Domain.Enums;

namespace ApiAutoLavado.Domain.Models
{
    public class Bahia
    {
        public int Id { get; set; }

        public required string Nombre { get; set; }

        public EstadoBahia Estado { get; set; } = EstadoBahia.Disponible;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
