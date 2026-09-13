using ApiAutoLavado.Domain.Enums;

namespace ApiAutoLavado.Domain.Models
{
    public class Bahia
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public required string NombreBahia { get; set; }

        public TipoBahia Tipo { get; set; }

        public EstadoBahia Estado { get; set; } = EstadoBahia.Disponible;
    }
}
