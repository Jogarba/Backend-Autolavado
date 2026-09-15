using ApiAutoLavado.Domain.Enums;

namespace ApiAutoLavado.Domain.Models
{
    public class Bahia
    {
        public int Id { get; set; }

        public required string NombreBahia { get; set; }

        public TipoBahia Tipo { get; set; }

        public EstadoBahia Estado { get; set; } = EstadoBahia.Disponible;

        public bool EstaDisponible => Estado == EstadoBahia.Disponible;

        public void Ocupar() => Estado = EstadoBahia.Ocupada;

        public void Liberar() => Estado = EstadoBahia.Disponible;
    }
}
