using ApiAutoLavado.Domain.Enums;

namespace ApiAutoLavado.Domain.Models
{
    public class Operario
    {
        public int Id { get; set; }

        public required string Nombres { get; set; }

        public required string Apellidos { get; set; }

        public required string Documento { get; set; }

        public required string Telefono { get; set; }

        public bool Activo { get; set; } = true;

        public EstadoOperario Estado { get; set; } = EstadoOperario.Disponible;

        public bool EstaDisponible => Activo && Estado == EstadoOperario.Disponible;
    }
}
