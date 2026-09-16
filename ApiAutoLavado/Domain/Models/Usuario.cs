using ApiAutoLavado.Domain.Enums;

namespace ApiAutoLavado.Domain.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        public required string NombreUsuario { get; set; }

        public required string ContrasenaHash { get; set; }

        public RolUsuario Rol { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
